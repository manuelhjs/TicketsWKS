/* Módulo de Empleados */
(function () {
    "use strict";
    const cfg = window.empleadosConfig;
    const state = { table: null, empleados: [], modal: null, importModal: null };

    async function getJson(url) { return handle(await fetch(url, { headers: { "X-Requested-With": "XMLHttpRequest" } })); }
    async function postForm(url, data) {
        return handle(await fetch(url, {
            method: "POST", headers: { "X-Requested-With": "XMLHttpRequest", "Content-Type": "application/x-www-form-urlencoded" },
            body: new URLSearchParams(data).toString()
        }));
    }
    async function postFile(url, formData) { return handle(await fetch(url, { method: "POST", headers: { "X-Requested-With": "XMLHttpRequest" }, body: formData })); }
    async function handle(res) { let b = null; try { b = await res.json(); } catch { } if (!res.ok) throw new Error((b && b.message) || "Error (" + res.status + ")."); return b; }
    function el(id) { return document.getElementById(id); }
    function esc(s) { return s == null ? "" : String(s).replace(/[&<>"']/g, m => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[m])); }
    function toast(msg, variant) {
        let c = el("toastContainer");
        if (!c) { c = document.createElement("div"); c.id = "toastContainer"; c.className = "toast-container position-fixed top-0 end-0 p-3"; document.body.appendChild(c); }
        const e = document.createElement("div");
        e.className = "toast align-items-center text-bg-" + (variant || "primary") + " border-0 show";
        e.innerHTML = '<div class="d-flex"><div class="toast-body">' + esc(msg) + '</div><button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button></div>';
        c.appendChild(e); setTimeout(() => e.remove(), 4000);
    }
    function fmtDate(v) { if (!v) return "—"; const [y, m, d] = String(v).substring(0, 10).split("-"); return d + "/" + m + "/" + y; }
    function estadoBadge(a) { return '<span class="badge-estado ' + (a ? "activo" : "inactivo") + '">' + (a ? "Activo" : "Inactivo") + '</span>'; }
    function acciones(e) {
        const t = e.activo
            ? '<button class="btn-icon danger js-toggle" title="Desactivar" data-id="' + e.id + '" data-activo="false">⛔</button>'
            : '<button class="btn-icon primary js-toggle" title="Activar" data-id="' + e.id + '" data-activo="true">✓</button>';
        return '<div class="d-flex gap-2 justify-content-end"><button class="btn-icon js-edit" title="Editar" data-id="' + e.id + '">✎</button>' + t + '</div>';
    }

    const DT_LANG = {
        emptyTable: "Sin empleados.", info: "Mostrando _START_ a _END_ de _TOTAL_", infoEmpty: "0 registros",
        infoFiltered: "(filtrado de _MAX_)", lengthMenu: "Mostrar _MENU_", search: "Buscar:", zeroRecords: "Sin coincidencias",
        paginate: { first: "Primero", last: "Último", next: "Siguiente", previous: "Anterior" }
    };

    function initTable() {
        state.table = $("#empleadosTable").DataTable({
            data: [], language: DT_LANG, order: [[1, "asc"]], pageLength: 25,
            scrollX: true, scrollY: "55vh", scrollCollapse: true, fixedColumns: { start: 1 },
            columnDefs: [
                { targets: 0, width: "120px" },  // Código
                { targets: 1, width: "210px" },  // Nombre
                { targets: 2, width: "240px" },  // Correo
                { targets: 3, width: "130px" },  // Teléfono
                { targets: 4, width: "180px" },  // Puesto
                { targets: 5, width: "160px" },  // Área
                { targets: 6, width: "120px" },  // Ingreso
                { targets: 7, width: "110px" },  // Estado
                { targets: 8, width: "190px" }   // Acciones
            ],
            columns: [
                { data: "codigo", render: d => esc(d || "—") },
                { data: "nombre", render: esc },
                { data: "correo", render: d => esc(d || "—") },
                { data: "telefono", render: d => esc(d || "—") },
                { data: "puesto", render: d => esc(d || "—") },
                { data: "area", render: d => esc(d || "—") },
                { data: "fechaIngreso", render: fmtDate },
                { data: "activo", render: estadoBadge },
                { data: null, orderable: false, searchable: false, className: "text-end", render: (d, t, row) => acciones(row) }
            ]
        });
    }
    async function load() {
        state.empleados = (await getJson(cfg.urls.getEmpleados)).data;
        state.table.clear().rows.add(state.empleados).draw();
    }

    function openModal(item) {
        el("empId").value = item ? item.id : 0;
        el("empNombre").value = item ? item.nombre : "";
        el("empCodigo").value = item ? (item.codigo || "") : "";
        el("empCorreo").value = item ? (item.correo || "") : "";
        el("empTelefono").value = item ? (item.telefono || "") : "";
        el("empPuesto").value = item ? (item.puesto || "") : "";
        el("empArea").value = item ? (item.area || "") : "";
        el("empFechaIngreso").value = item && item.fechaIngreso ? String(item.fechaIngreso).substring(0, 10) : "";
        el("empActivo").checked = item ? item.activo : true;
        el("empTitle").textContent = item ? "Editar empleado" : "Nuevo empleado";
        state.modal.show();
    }

    document.addEventListener("DOMContentLoaded", async () => {
        state.modal = new bootstrap.Modal(el("empleadoModal"));
        state.importModal = new bootstrap.Modal(el("importModal"));
        initTable();

        el("btnNuevoEmpleado").addEventListener("click", () => openModal(null));
        el("btnImportar").addEventListener("click", () => { el("importResult").innerHTML = ""; el("importFile").value = ""; state.importModal.show(); });
        el("btnPlantilla").addEventListener("click", ev => {
            ev.preventDefault();
            const contenido =
                "Codigo,Nombre,Correo,Telefono,Puesto,Area,FechaIngreso,Activo\n" +
                "# Fila de ejemplo (las líneas que inician con # se ignoran al importar):\n" +
                "# ,Juan Pérez,jperez@empresa.com,5512345678,Analista,Sistemas,2024-01-15,Sí\n";
            const blob = new Blob(["﻿" + contenido], { type: "text/csv;charset=utf-8;" });
            const url = URL.createObjectURL(blob);
            const a = document.createElement("a");
            a.href = url; a.download = "plantilla_empleados.csv";
            document.body.appendChild(a); a.click(); a.remove();
            URL.revokeObjectURL(url);
        });

        $("#empleadosTable tbody").on("click", ".js-edit", function () {
            const id = Number(this.dataset.id);
            openModal(state.empleados.find(e => e.id === id));
        });
        $("#empleadosTable tbody").on("click", ".js-toggle", async function () {
            const id = Number(this.dataset.id), activo = this.dataset.activo;
            try { await postForm(cfg.urls.toggle, { id, activo }); await load(); toast("Actualizado.", "success"); }
            catch (e) { toast(e.message, "danger"); }
        });

        el("empleadoForm").addEventListener("submit", async ev => {
            ev.preventDefault();
            try {
                await postForm(cfg.urls.upsert, {
                    Id: el("empId").value, Codigo: el("empCodigo").value, Nombre: el("empNombre").value,
                    Correo: el("empCorreo").value, Telefono: el("empTelefono").value, Puesto: el("empPuesto").value,
                    Area: el("empArea").value, FechaIngreso: el("empFechaIngreso").value, Activo: el("empActivo").checked
                });
                state.modal.hide(); await load(); toast("Guardado.", "success");
            } catch (e) { toast(e.message, "danger"); }
        });

        el("btnDoImport").addEventListener("click", async () => {
            const f = el("importFile").files[0];
            if (!f) { toast("Selecciona un archivo CSV.", "danger"); return; }
            const fd = new FormData(); fd.append("file", f);
            try {
                const r = (await postFile(cfg.urls.import, fd)).data;
                const errs = r.errores && r.errores.length
                    ? '<div class="mt-2"><strong class="text-danger">Errores (' + r.errores.length + '):</strong><ul class="mb-0">' + r.errores.map(e => '<li>' + esc(e) + '</li>').join("") + '</ul></div>'
                    : "";
                el("importResult").innerHTML =
                    '<div class="alert alert-info mb-0">Insertados: <strong>' + r.insertados + '</strong> · Actualizados: <strong>' + r.actualizados + '</strong> · Omitidos: <strong>' + r.omitidos + '</strong>' + errs + '</div>';
                await load();
                toast("Importación completada.", "success");
            } catch (e) { toast(e.message, "danger"); }
        });

        try { await load(); } catch (e) { toast(e.message, "danger"); }
    });
})();
