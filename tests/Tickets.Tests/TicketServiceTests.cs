using Moq;
using Tickets.Application.Abstractions;
using Tickets.Application.Common;
using Tickets.Application.Dtos;
using Tickets.Application.Services;
using Tickets.Domain.Entities;
using Tickets.Domain.Enums;
using Xunit;

namespace Tickets.Tests;

/// <summary>
/// Los servicios son testeables porque dependen de INTERFACES (DI), a diferencia
/// del legacy con métodos estáticos y acceso directo a BD.
/// </summary>
public class TicketServiceTests
{
    private readonly Mock<ITicketRepository> _tickets = new();
    private readonly Mock<ICatalogRepository> _catalog = new();
    private readonly Mock<IEmpleadoRepository> _empleados = new();
    private readonly Mock<IHistorialEstatusRepository> _historial = new();
    private readonly Mock<ITicketLogRepository> _log = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();

    private TicketService CreateSut()
    {
        _currentUser.SetupGet(x => x.UserCode).Returns("blozano");
        return new TicketService(_tickets.Object, _catalog.Object, _empleados.Object,
            _historial.Object, _log.Object, _currentUser.Object);
    }

    private void SetupValidCatalogs()
    {
        _empleados.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(new Empleado { Id = 1, Nombre = "Juan" });
        _catalog.Setup(x => x.GetClasificacionAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(new Clasificacion { Id = 1, Nombre = "SAP" });
        _catalog.Setup(x => x.GetCategoriaAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(new Categoria { Id = 1, ClasificacionId = 1, Nombre = "General" });
        _catalog.Setup(x => x.GetPrioridadAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(new Prioridad { Id = 2, Nombre = "Medio" });
    }

    private static CreateTicketRequest ValidCreate() => new()
    {
        SolicitanteId = 1,
        TipoSolicitud = TipoSolicitud.Incidencia,
        ClasificacionId = 1,
        CategoriaId = 1,
        PrioridadId = 2,
        Descripcion = "El sistema no responde."
    };

    [Fact]
    public async Task CreateAsync_WithoutDescription_Throws()
    {
        var sut = CreateSut();
        var req = ValidCreate();
        req.Descripcion = "   ";
        await Assert.ThrowsAsync<ValidationException>(() => sut.CreateAsync(req));
    }

    [Fact]
    public async Task CreateAsync_InvalidEmail_Throws()
    {
        var sut = CreateSut();
        SetupValidCatalogs();
        var req = ValidCreate();
        req.Correo = "correo-invalido";

        var ex = await Assert.ThrowsAsync<ValidationException>(() => sut.CreateAsync(req));
        Assert.Contains("correo", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateAsync_CategoriaDeOtraClasificacion_Throws()
    {
        var sut = CreateSut();
        SetupValidCatalogs();
        _catalog.Setup(x => x.GetCategoriaAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Categoria { Id = 1, ClasificacionId = 99, Nombre = "Otra" });

        await Assert.ThrowsAsync<ValidationException>(() => sut.CreateAsync(ValidCreate()));
    }

    [Fact]
    public async Task CreateAsync_Valid_InsertsWithDefaultsAndLogs()
    {
        var sut = CreateSut();
        SetupValidCatalogs();
        Ticket? captured = null;
        _tickets.Setup(x => x.InsertAsync(It.IsAny<Ticket>(), It.IsAny<CancellationToken>()))
            .Callback<Ticket, CancellationToken>((t, _) => captured = t).ReturnsAsync(99);

        var id = await sut.CreateAsync(ValidCreate());

        Assert.Equal(99, id);
        Assert.NotNull(captured);
        Assert.Equal(1, captured!.SolicitanteId);
        Assert.Equal((byte)1, captured.EstatusId); // Por asignar
        _historial.Verify(x => x.InsertAsync(It.IsAny<HistorialEstatus>(), It.IsAny<CancellationToken>()), Times.Once);
        _log.Verify(x => x.InsertAsync(It.IsAny<TicketLogEntry>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ChangeStatusAsync_WithoutComment_Throws()
    {
        var sut = CreateSut();
        var req = new ChangeStatusRequest { TicketId = 1, EstatusId = 2, Comentario = "  " };
        await Assert.ThrowsAsync<ValidationException>(() => sut.ChangeStatusAsync(req));
    }

    [Fact]
    public async Task ChangeStatusAsync_SameStatus_Throws()
    {
        var sut = CreateSut();
        _tickets.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(new Ticket { Id = 1, EstatusId = 3 });
        _catalog.Setup(x => x.GetEstatusAsync(3, It.IsAny<CancellationToken>())).ReturnsAsync(new Estatus { Id = 3, Nombre = "Análisis" });

        var req = new ChangeStatusRequest { TicketId = 1, EstatusId = 3, Comentario = "sin cambio" };
        await Assert.ThrowsAsync<ValidationException>(() => sut.ChangeStatusAsync(req));
    }
}
