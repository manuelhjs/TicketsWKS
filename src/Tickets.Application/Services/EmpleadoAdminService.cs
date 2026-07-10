using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Tickets.Application.Abstractions;
using Tickets.Application.Common;
using Tickets.Application.Dtos;
using Tickets.Domain.Entities;

namespace Tickets.Application.Services;

public sealed partial class EmpleadoAdminService(IEmpleadoRepository empleados) : IEmpleadoAdminService
{
    private static readonly string[] Header =
        ["Codigo", "Nombre", "Correo", "Telefono", "Puesto", "Area", "FechaIngreso", "Activo"];

    public async Task<IReadOnlyList<EmpleadoDto>> GetAllAsync(CancellationToken ct = default)
        => (await empleados.GetAllAsync(ct)).Select(Map).ToList();

    public async Task<int> UpsertAsync(EmpleadoFormRequest r, CancellationToken ct = default)
    {
        Validate(r.Nombre, r.Correo, r.Telefono);

        if (r.Id == 0)
        {
            var nuevo = new Empleado
            {
                Codigo = Trim(r.Codigo),
                Nombre = r.Nombre.Trim(),
                Correo = Trim(r.Correo),
                Telefono = Trim(r.Telefono),
                Puesto = Trim(r.Puesto),
                Area = Trim(r.Area),
                FechaIngreso = r.FechaIngreso,
                Activo = r.Activo,
                FechaAlta = DateTime.UtcNow
            };
            return await empleados.InsertAsync(nuevo, ct);
        }

        var existing = await empleados.GetByIdAsync(r.Id, ct)
            ?? throw new NotFoundException("Empleado no encontrado.");
        existing.Codigo = Trim(r.Codigo);
        existing.Nombre = r.Nombre.Trim();
        existing.Correo = Trim(r.Correo);
        existing.Telefono = Trim(r.Telefono);
        existing.Puesto = Trim(r.Puesto);
        existing.Area = Trim(r.Area);
        existing.FechaIngreso = r.FechaIngreso;
        existing.Activo = r.Activo;
        await empleados.UpdateAsync(existing, ct);
        return existing.Id;
    }

    public Task SetActivoAsync(int id, bool activo, CancellationToken ct = default)
        => empleados.SetActivoAsync(id, activo, ct);

    // ---------- Export ----------
    public async Task<byte[]> ExportCsvAsync(CancellationToken ct = default)
    {
        var list = await empleados.GetAllAsync(ct);
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(",", Header));
        foreach (var e in list)
        {
            sb.AppendLine(string.Join(",", new[]
            {
                Csv(e.Codigo), Csv(e.Nombre), Csv(e.Correo), Csv(e.Telefono),
                Csv(e.Puesto), Csv(e.Area),
                Csv(e.FechaIngreso?.ToString("yyyy-MM-dd")),
                e.Activo ? "Sí" : "No"
            }));
        }
        // UTF-8 con BOM para que Excel respete los acentos.
        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
    }

    // ---------- Import ----------
    public async Task<ImportEmpleadosResultDto> ImportCsvAsync(string csvContent, CancellationToken ct = default)
    {
        var result = new ImportEmpleadosResultDto();
        if (string.IsNullOrWhiteSpace(csvContent))
        {
            result.Errores.Add("El archivo está vacío.");
            return result;
        }

        var lines = csvContent.Replace("\r\n", "\n").Replace("\r", "\n")
            .Split('\n').Where(l => l.Trim().Length > 0).ToList();
        if (lines.Count < 2)
        {
            result.Errores.Add("El archivo no tiene registros (solo encabezado o vacío).");
            return result;
        }

        var all = (await empleados.GetAllAsync(ct)).ToList();
        var porCodigo = all.Where(e => !string.IsNullOrWhiteSpace(e.Codigo))
            .GroupBy(e => e.Codigo!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
        var porNombreCorreo = all
            .GroupBy(e => NombreCorreoKey(e.Nombre, e.Correo), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        // Se asume que la primera fila es encabezado.
        for (var i = 1; i < lines.Count; i++)
        {
            var fila = i + 1;
            // Líneas de comentario (plantilla): se ignoran.
            if (lines[i].TrimStart().StartsWith('#')) continue;
            try
            {
                var f = ParseCsvLine(lines[i]);
                string Get(int idx) => idx < f.Count ? f[idx].Trim() : "";

                var codigo = Get(0);
                var nombre = Get(1);
                var correo = Get(2);
                var telefono = Get(3);
                if (string.IsNullOrWhiteSpace(nombre)) { result.Omitidos++; result.Errores.Add($"Fila {fila}: el nombre es obligatorio."); continue; }
                if (correo.Length > 0 && !EmailRegex().IsMatch(correo)) { result.Omitidos++; result.Errores.Add($"Fila {fila}: correo con formato inválido."); continue; }
                if (telefono.Length > 0 && !PhoneRegex().IsMatch(telefono)) { result.Omitidos++; result.Errores.Add($"Fila {fila}: el teléfono debe tener 10 dígitos."); continue; }

                DateOnly? fecha = null;
                var fechaTxt = Get(6);
                if (fechaTxt.Length > 0)
                {
                    if (!TryParseFecha(fechaTxt, out var fp)) { result.Omitidos++; result.Errores.Add($"Fila {fila}: fecha de ingreso inválida (use aaaa-mm-dd)."); continue; }
                    fecha = fp;
                }

                var target = (!string.IsNullOrWhiteSpace(codigo) && porCodigo.TryGetValue(codigo, out var byCod)) ? byCod
                    : porNombreCorreo.TryGetValue(NombreCorreoKey(nombre, correo), out var byNc) ? byNc : null;

                if (target is null)
                {
                    var nuevo = new Empleado
                    {
                        Codigo = NullIf(codigo),
                        Nombre = nombre,
                        Correo = NullIf(correo),
                        Telefono = NullIf(telefono),
                        Puesto = NullIf(Get(4)),
                        Area = NullIf(Get(5)),
                        FechaIngreso = fecha,
                        Activo = ParseActivo(Get(7)),
                        FechaAlta = DateTime.UtcNow
                    };
                    nuevo.Id = await empleados.InsertAsync(nuevo, ct);
                    all.Add(nuevo);
                    if (!string.IsNullOrWhiteSpace(nuevo.Codigo)) porCodigo[nuevo.Codigo!] = nuevo;
                    porNombreCorreo[NombreCorreoKey(nuevo.Nombre, nuevo.Correo)] = nuevo;
                    result.Insertados++;
                }
                else
                {
                    target.Codigo = NullIf(codigo) ?? target.Codigo;
                    target.Nombre = nombre;
                    target.Correo = NullIf(correo);
                    target.Telefono = NullIf(telefono);
                    target.Puesto = NullIf(Get(4));
                    target.Area = NullIf(Get(5));
                    target.FechaIngreso = fecha;
                    target.Activo = ParseActivo(Get(7));
                    await empleados.UpdateAsync(target, ct);
                    result.Actualizados++;
                }
            }
            catch (Exception ex)
            {
                result.Omitidos++;
                result.Errores.Add($"Fila {fila}: {ex.Message}");
            }
        }
        return result;
    }

    // ---------- Helpers ----------
    private static void Validate(string nombre, string? correo, string? telefono)
    {
        if (string.IsNullOrWhiteSpace(nombre)) throw new ValidationException("El nombre es obligatorio.");
        if (!string.IsNullOrWhiteSpace(correo) && !EmailRegex().IsMatch(correo.Trim())) throw new ValidationException("El correo no tiene un formato válido.");
        if (!string.IsNullOrWhiteSpace(telefono) && !PhoneRegex().IsMatch(telefono.Trim())) throw new ValidationException("El teléfono debe tener 10 dígitos.");
    }

    private static EmpleadoDto Map(Empleado e) => new()
    {
        Id = e.Id, Codigo = e.Codigo, Nombre = e.Nombre, Correo = e.Correo, Telefono = e.Telefono,
        Puesto = e.Puesto, Area = e.Area, FechaIngreso = e.FechaIngreso, Activo = e.Activo
    };

    private static string NombreCorreoKey(string nombre, string? correo) => $"{nombre?.Trim()}|{correo?.Trim()}".ToLowerInvariant();
    private static string? Trim(string? v) => string.IsNullOrWhiteSpace(v) ? null : v.Trim();
    private static string? NullIf(string v) => string.IsNullOrWhiteSpace(v) ? null : v.Trim();
    private static bool ParseActivo(string v) => v.Trim().ToLowerInvariant() is "" or "1" or "sí" or "si" or "true" or "verdadero" or "x" or "y" or "yes";

    private static bool TryParseFecha(string txt, out DateOnly fecha)
        => DateOnly.TryParseExact(txt, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out fecha)
           || DateOnly.TryParseExact(txt, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out fecha)
           || DateOnly.TryParse(txt, CultureInfo.GetCultureInfo("es-MX"), DateTimeStyles.None, out fecha);

    private static string Csv(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        return value.Contains(',') || value.Contains('"') || value.Contains('\n')
            ? "\"" + value.Replace("\"", "\"\"") + "\""
            : value;
    }

    private static List<string> ParseCsvLine(string line)
    {
        var result = new List<string>();
        var sb = new StringBuilder();
        var inQuotes = false;
        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"') { sb.Append('"'); i++; }
                    else inQuotes = false;
                }
                else sb.Append(c);
            }
            else if (c == '"') inQuotes = true;
            else if (c == ',') { result.Add(sb.ToString()); sb.Clear(); }
            else sb.Append(c);
        }
        result.Add(sb.ToString());
        return result;
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"^\d{10}$")]
    private static partial Regex PhoneRegex();
}
