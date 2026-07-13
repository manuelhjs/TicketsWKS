/* Módulo de Tickets — front-end (jQuery + DataTables + Bootstrap 5) */
(function () {
    "use strict";

    const cfg = window.ticketsConfig;
    const NEW = "__new__";
    const state = { table: null, detailModal: null, modals: {}, empleados: [], estatus: [], prioridades: [], clasificaciones: [] };

    // ---------- HTTP ----------
    async function getJson(url, params) {
        const qs = params ? "?" + new URLSearchParams(params).toString() : "";
        const res = await fetch(url + qs, { headers: { "X-Requested-With": "XMLHttpRequest" } });
        return handle(res);
    }
    async function postForm(url, data) {
        const res = await fetch(url, {
            method: "POST",
            headers: { "X-Requested-With": "XMLHttpRequest", "Content-Type": "application/x-www-form-urlencoded" },
            body: new URLSearchParams(data).toString()
        });
        return handle(res);
    }
    async function postFile(url, formData) {
        const res = await fetch(url, { method: "POST", headers: { "X-Requested-With": "XMLHttpRequest" }, body: formData });
        return handle(res);
    }
    async function handle(res) {
        let body = null;
        try { body = await res.json(); } catch { }
        if (!res.ok) throw new Error((body && body.message) || "Ocurrió un error (" + res.status + ").");
        return body;
    }

    // ---------- Utilidades ----------
    function el(id) { return document.getElementById(id); }
    function toast(message, variant) {
        let c = el("toastContainer");
        if (!c) { c = document.createElement("div"); c.id = "toastContainer"; c.className = "toast-container position-fixed top-0 end-0 p-3"; document.body.appendChild(c); }
        const e = document.createElement("div");
        e.className = "toast align-items-center text-bg-" + (variant || "primary") + " border-0 show";
        e.innerHTML = '<div class="d-flex"><div class="toast-body">' + escapeHtml(message) + '</div><button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button></div>';
        c.appendChild(e); setTimeout(() => e.remove(), 4500);
    }
    function escapeHtml(s) {
        if (s === null || s === undefined) return "";
        return String(s).replace(/[&<>"']/g, m => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[m]));
    }
    function fmtDate(v) {
        if (!v) return "—";
        if (typeof v === "string" && /^\d{4}-\d{2}-\d{2}/.test(v) && v.length === 10) { const [y, m, d] = v.split("-"); return d + "/" + m + "/" + y; }
        return new Date(v).toLocaleDateString("es-MX");
    }
    function fmtDateTime(v) { return v ? new Date(v).toLocaleString("es-MX") : "—"; }
    function fmtSize(b) { return b >= 1048576 ? (b / 1048576).toFixed(1) + " MB" : Math.ceil(b / 1024) + " KB"; }
    function isoLocal(d) { const y = d.getFullYear(), m = String(d.getMonth() + 1).padStart(2, "0"), day = String(d.getDate()).padStart(2, "0"); return `${y}-${m}-${day}`; }
    function initTooltips(scope) { (scope || document).querySelectorAll('[data-bs-toggle="tooltip"]').forEach(e => { if (!e._tt) e._tt = new bootstrap.Tooltip(e); }); }
    function fillSelect(id, options, firstText) {
        const sel = el(id); if (!sel) return;
        sel.innerHTML = firstText !== undefined ? '<option value="">' + firstText + '</option>' : "";
        options.forEach(o => { const opt = document.createElement("option"); opt.value = o.value; opt.textContent = o.text; sel.appendChild(opt); });
    }
    function addOption(sel, opt, beforeValue) {
        const o = document.createElement("option"); o.value = opt.value; o.textContent = opt.text;
        const ref = beforeValue ? [...sel.options].find(x => x.value === beforeValue) : null;
        if (ref) sel.insertBefore(o, ref); else sel.appendChild(o);
    }

    // ---------- Catálogos ----------
    async function loadPrioridades() {
        state.prioridades = (await getJson(cfg.urls.getPrioridades)).data;
        const box = el("prioridadRadios");
        box.innerHTML = state.prioridades.map((p, i) =>
            '<div class="form-check form-check-inline">' +
            '<input class="form-check-input" type="radio" name="PrioridadId" id="prio' + p.id + '" value="' + p.id + '"' + (i === 1 ? " checked" : "") + '>' +
            '<label class="form-check-label" for="prio' + p.id + '">' + escapeHtml(p.nombre) +
            ' <i class="bi bi-info-circle text-muted" data-bs-toggle="tooltip" title="' + escapeHtml(p.descripcion) + '"></i></label></div>'
        ).join("");
        initTooltips(box);
        const opts = state.prioridades.map(p => ({ value: p.id, text: p.nombre }));
        fillSelect("dtlPrioridad", opts);
        fillSelect("filterPrioridad", opts, "Prioridad");
    }
    async function loadEstatus() {
        state.estatus = (await getJson(cfg.urls.getEstatus)).data;
        const opts = state.estatus.map(e => ({ value: e.id, text: e.nombre }));
        fillSelect("filterEstatus", opts, "Estatus");
        fillSelect("dtlEstatus", opts);
    }
    async function loadClasificaciones() {
        state.clasificaciones = (await getJson(cfg.urls.getClasificaciones)).data;
        // Dropdown personalizado del formulario (con "Otro…")
        ddSetItems("createClasificacionMenu", state.clasificaciones, onSelectClasif, () => state.modals.clasificacion.show());
        // Selects nativos (detalle + filtro)
        fillSelect("dtlClasificacion", state.clasificaciones);
        fillSelect("filterClasificacion", state.clasificaciones, "Clasificación");
    }

    // ---------- Dropdown personalizado ----------
    function ddSetItems(menuId, items, onSelect, onOtro) {
        const menu = el(menuId);
        menu.innerHTML = "";
        items.forEach(it => {
            const d = document.createElement("div");
            d.className = "tk-dd-opt";
            d.textContent = it.text;
            d.addEventListener("click", () => onSelect(it.value, it.text));
            menu.appendChild(d);
        });
        if (onOtro) {
            const o = document.createElement("div");
            o.className = "tk-dd-opt tk-dd-otro";
            o.textContent = "＋ Otro…";
            o.addEventListener("click", onOtro);
            menu.appendChild(o);
        }
    }
    function ddSelect(prefix, value, text) {
        el(prefix + "Value").value = value || "";
        const lbl = el(prefix + "Label");
        lbl.textContent = text;
        lbl.classList.toggle("tk-placeholder", !value);
    }
    function ddOpen(prefix, open) {
        el(prefix + "Menu").classList.toggle("show", open);
        el(prefix + "Toggle").classList.toggle("open", open);
    }
    function onSelectClasif(value, text) {
        ddSelect("createClasificacion", value, text);
        ddOpen("createClasificacion", false);
        ddSelect("createCategoria", "", "Seleccione…");
        loadCategoriasDropdown(value);
    }
    function onSelectCat(value, text) { ddSelect("createCategoria", value, text); ddOpen("createCategoria", false); }
    function onOtroCat() {
        ddOpen("createCategoria", false);
        const clasId = el("createClasificacionValue").value;
        if (!clasId) { toast("Primero selecciona una clasificación.", "danger"); return; }
        el("catClasificacionId").value = clasId;
        el("catClasificacionNombre").textContent = el("createClasificacionLabel").textContent;
        state.modals.categoria.show();
    }
    async function loadCategoriasDropdown(clasId) {
        if (!clasId) { ddSetItems("createCategoriaMenu", [], onSelectCat, onOtroCat); return; }
        const data = (await getJson(cfg.urls.getCategorias, { clasificacionId: clasId })).data;
        ddSetItems("createCategoriaMenu", data, onSelectCat, onOtroCat);
    }
    async function loadEmpleados() {
        state.empleados = (await getJson(cfg.urls.getEmpleados)).data;
        const opts = state.empleados.map(e => ({ value: e.id, text: e.nombre }));
        fillSelect("createSolicitante", opts, "Seleccione…");
        fillSelect("dtlResponsable", opts, "— Sin responsable —");
        fillSelect("filterSolicitante", opts, "Solicitante");
        selectCurrentSolicitante();
    }
    async function loadCategorias(clasId, targetId, includeOtro) {
        const sel = el(targetId);
        sel.innerHTML = '<option value="">Seleccione…</option>';
        if (!clasId || clasId === NEW) return;
        const data = (await getJson(cfg.urls.getCategorias, { clasificacionId: clasId })).data;
        data.forEach(c => addOption(sel, c));
        if (includeOtro) addOption(sel, { value: NEW, text: "➕ Otro…" });
    }

    // ---------- Alta de catálogos (modales, con auto-selección) ----------
    function appendEmpleado(emp) {
        ["createSolicitante", "dtlResponsable", "filterSolicitante"].forEach(id => addOption(el(id), { value: emp.id, text: emp.nombre }));
    }

    // ---------- Formulario de creación ----------
    function autofillCorreo() {
        const id = el("createSolicitante").value;
        const emp = state.empleados.find(e => String(e.id) === String(id));
        if (emp) el("createCorreo").value = emp.correo || "";
    }
    function selectCurrentSolicitante() {
        const sel = el("createSolicitante");
        sel.value = (cfg.currentEmpleadoId && state.empleados.some(e => String(e.id) === String(cfg.currentEmpleadoId)))
            ? cfg.currentEmpleadoId : "";
        autofillCorreo();
    }
    function resetCreateDropdowns() {
        ddSelect("createClasificacion", "", "Seleccione…");
        ddSelect("createCategoria", "", "Seleccione…");
        ddSetItems("createCategoriaMenu", [], onSelectCat, onOtroCat);
    }
    async function submitCreate(ev) {
        ev.preventDefault();
        const form = el("createTicketForm");
        if (!el("createClasificacionValue").value || !el("createCategoriaValue").value) { toast("Selecciona clasificación y categoría.", "danger"); return; }
        try {
            const r = await postForm(cfg.urls.create, new URLSearchParams(new FormData(form)).toString());
            const files = el("createFiles").files;
            if (files.length) await uploadFiles(r.id, files);
            toast("Ticket creado.", "success");
            form.reset();
            el("descCount").textContent = "0";
            resetCreateDropdowns();
            selectCurrentSolicitante();
            await Promise.all([refreshDashboard(), loadEmpleados()]);
            await loadTickets();
        } catch (e) { toast(e.message, "danger"); }
    }
    async function uploadFiles(ticketId, fileList) {
        const fd = new FormData();
        fd.append("ticketId", ticketId);
        [...fileList].forEach(f => fd.append("files", f));
        const r = await postFile(cfg.urls.uploadAdjuntos, fd);
        if (r.errores && r.errores.length) r.errores.forEach(m => toast(m, "danger"));
    }

    // ---------- Filtros (recargan solos) ----------
    function currentFilter() {
        const v = id => { const e = el(id); return e ? e.value : ""; };
        return {
            Desde: v("filterDesde"), Hasta: v("filterHasta"),
            EstatusId: v("filterEstatus"), ClasificacionId: v("filterClasificacion"),
            PrioridadId: v("filterPrioridad"), SolicitanteId: v("filterSolicitante"), TipoSolicitud: v("filterTipo")
        };
    }
    function setDefaultDateRange() {
        const hasta = new Date();
        const desde = new Date(); desde.setMonth(desde.getMonth() - 3);
        el("filterDesde").value = isoLocal(desde);
        el("filterHasta").value = isoLocal(hasta);
    }
    async function refreshDashboard() {
        const d = (await getJson(cfg.urls.getDashboard)).data;
        el("statTotal").textContent = d.total;
        el("statPorAsignar").textContent = d.porAsignar;
        el("statEnCurso").textContent = d.enCurso;
        el("statFinalizados").textContent = d.finalizados;
    }

    // ---------- Tabla ----------
    const DT_LANG = {
        emptyTable: "Sin tickets.", info: "Mostrando _START_ a _END_ de _TOTAL_", infoEmpty: "0 registros",
        infoFiltered: "(filtrado de _MAX_)", lengthMenu: "Mostrar _MENU_", loadingRecords: "Cargando…",
        processing: "Procesando…", search: "Buscar:", zeroRecords: "Sin coincidencias",
        paginate: { first: "Primero", last: "Último", next: "Siguiente", previous: "Anterior" }
    };
    function initTable() {
        state.table = $("#ticketsTable").DataTable({
            data: [], language: DT_LANG, order: [[0, "desc"]], pageLength: 25,
            scrollX: true, scrollY: "55vh", scrollCollapse: true, fixedColumns: { start: 1 },
            columnDefs: [
                { targets: 0, width: "70px" },   // Id
                { targets: 1, width: "200px" },  // Solicitante
                { targets: 2, width: "130px" },  // Tipo
                { targets: 3, width: "170px" },  // Clasificación
                { targets: 4, width: "170px" },  // Categoría
                { targets: 5, width: "120px" },  // Prioridad
                { targets: 6, width: "150px" },  // Estatus
                { targets: 7, width: "200px" },  // Responsable
                { targets: 8, width: "130px" }   // Creación
            ],
            columns: [
                { data: "id", render: d => '<span class="fw-semibold text-primary">#' + d + '</span>' },
                { data: "solicitanteNombre" },
                { data: "tipoSolicitudNombre" },
                { data: "clasificacionNombre" },
                { data: "categoriaNombre" },
                { data: "prioridadNombre" },
                { data: "estatusNombre", render: d => '<span class="badge bg-secondary">' + escapeHtml(d) + '</span>' },
                { data: "responsableNombre", render: d => escapeHtml(d || "—") },
                { data: "createdAt", render: fmtDate }
            ]
        });
        $("#ticketsTable tbody").on("click", "tr", function () { const d = state.table.row(this).data(); if (d) openDetail(d.id); });
    }
    async function loadTickets() {
        try { const r = await getJson(cfg.urls.getTickets, currentFilter()); state.table.clear().rows.add(r.items).draw(); }
        catch (e) { toast(e.message, "danger"); }
    }

    // ---------- Detalle ----------
    async function openDetail(id) {
        try {
            const t = (await getJson(cfg.urls.getTicket, { id })).data;
            await populateDetail(t);
            await Promise.all([loadAdjuntos(id), loadComments(id), loadHistorial(id), loadLog(id)]);
            state.detailModal.show();
        } catch (e) { toast(e.message, "danger"); }
    }
    async function populateDetail(t) {
        state.currentTicket = t;
        el("ticketDetailModal").dataset.id = t.id;
        el("dtlId").textContent = "#" + t.id;
        el("dtlSolicitante").textContent = t.solicitanteNombre;
        el("dtlEstatusActual").textContent = t.estatusNombre;
        el("dtlCreated").textContent = fmtDateTime(t.createdAt);
        // Modo lectura
        el("dtlCorreoView").textContent = t.correo || "—";
        el("dtlCelularView").textContent = t.celular || "—";
        el("dtlTipoView").textContent = t.tipoSolicitudNombre;
        el("dtlPrioridadView").textContent = t.prioridadNombre;
        el("dtlClasificacionView").textContent = t.clasificacionNombre;
        el("dtlCategoriaView").textContent = t.categoriaNombre;
        el("dtlDescripcionView").textContent = t.descripcion;
        // Modo edición
        el("dtlCorreo").value = t.correo || "";
        el("dtlCelular").value = t.celular || "";
        el("dtlTipo").value = t.tipoSolicitud;
        el("dtlPrioridad").value = t.prioridadId;
        el("dtlClasificacion").value = t.clasificacionId;
        el("dtlDescripcion").value = t.descripcion;
        // Gestión
        el("dtlEstatus").value = t.estatusId;
        el("dtlEstatusComentario").value = "";
        el("dtlResponsable").value = t.responsableEmpleadoId || "";
        el("dtlCommentTicketId").value = t.id;
        await loadCategorias(t.clasificacionId, "dtlCategoria", false);
        el("dtlCategoria").value = t.categoriaId;
        setEditMode(false);
    }
    function setEditMode(on) {
        document.querySelectorAll("#tabDetalle .dtl-view").forEach(e => e.classList.toggle("d-none", on));
        document.querySelectorAll("#tabDetalle .dtl-edit").forEach(e => e.classList.toggle("d-none", !on));
        el("btnDtlEdit").classList.toggle("d-none", on);
        el("btnDtlCancelEdit").classList.toggle("d-none", !on);
    }
    function ticketId() { return el("ticketDetailModal").dataset.id; }
    async function saveDetail() {
        const id = ticketId();
        try {
            await postForm(cfg.urls.update, {
                TicketId: id, Correo: el("dtlCorreo").value, Celular: el("dtlCelular").value,
                TipoSolicitud: el("dtlTipo").value, ClasificacionId: el("dtlClasificacion").value,
                CategoriaId: el("dtlCategoria").value, PrioridadId: el("dtlPrioridad").value, Descripcion: el("dtlDescripcion").value
            });
            toast("Cambios guardados.", "success");
            const t = (await getJson(cfg.urls.getTicket, { id })).data;
            await populateDetail(t);      // refresca vista y vuelve a modo lectura
            await loadTickets();
        } catch (e) { toast(e.message, "danger"); }
    }
    async function changeStatus() {
        const id = ticketId();
        const comentario = el("dtlEstatusComentario").value.trim();
        if (!comentario) { toast("El comentario es obligatorio al cambiar de estatus.", "danger"); return; }
        try {
            await postForm(cfg.urls.changeStatus, { TicketId: id, EstatusId: el("dtlEstatus").value, Comentario: comentario });
            toast("Estatus actualizado.", "success");
            const t = (await getJson(cfg.urls.getTicket, { id })).data;
            el("dtlEstatusActual").textContent = t.estatusNombre;
            el("dtlEstatusComentario").value = "";
            await Promise.all([loadHistorial(id), refreshDashboard(), loadTickets()]);
        } catch (e) { toast(e.message, "danger"); }
    }
    async function assignResponsable() {
        const id = ticketId(); const resp = el("dtlResponsable").value;
        if (!resp) { toast("Selecciona un responsable.", "danger"); return; }
        try { await postForm(cfg.urls.assignResponsable, { TicketId: id, ResponsableEmpleadoId: resp }); toast("Responsable asignado.", "success"); await Promise.all([loadTickets(), loadLog(id)]); }
        catch (e) { toast(e.message, "danger"); }
    }
    async function inactivate() {
        const id = ticketId();
        if (!confirm("¿Inactivar el ticket #" + id + "?")) return;
        try { await postForm(cfg.urls.setInactive, { id }); toast("Ticket inactivado.", "success"); state.detailModal.hide(); await Promise.all([refreshDashboard(), loadTickets()]); }
        catch (e) { toast(e.message, "danger"); }
    }
    async function loadAdjuntos(id) {
        const data = (await getJson(cfg.urls.getAdjuntos, { ticketId: id })).data;
        el("dtlAdjuntos").innerHTML = data.length
            ? data.map(a => '<li><i class="bi bi-paperclip"></i> <a href="' + cfg.urls.downloadAdjunto + '?id=' + a.id + '">' + escapeHtml(a.nombreOriginal) + '</a> <span class="text-muted">(' + fmtSize(a.tamanoBytes) + ')</span></li>').join("")
            : '<li class="text-muted">Sin evidencias.</li>';
    }
    async function uploadDetailFiles() {
        const id = ticketId(); const files = el("dtlFiles").files;
        if (!files.length) return;
        try { await uploadFiles(id, files); el("dtlFiles").value = ""; toast("Evidencias subidas.", "success"); await Promise.all([loadAdjuntos(id), loadLog(id)]); }
        catch (e) { toast(e.message, "danger"); }
    }
    async function loadComments(id) {
        const data = (await getJson(cfg.urls.getComments, { ticketId: id })).data;
        const list = el("dtlComments");
        list.innerHTML = data.length
            ? data.map(c => '<div class="comment"><div class="d-flex justify-content-between align-items-baseline gap-2"><span class="cmt-author">' + escapeHtml(c.autorNombre) + '</span><span class="cmt-date">' + fmtDateTime(c.createdAt) + '</span></div><div class="cmt-text">' + escapeHtml(c.comentario) + '</div></div>').join("")
            : '<p class="text-muted text-center small">Sin comentarios.</p>';
        // Baja al último comentario (estilo chat) solo si la pestaña está activa.
        if (el("tabComentarios").classList.contains("active")) {
            const sc = document.querySelector("#ticketDetailModal .dtl-content");
            if (sc) sc.scrollTop = sc.scrollHeight;
        }
    }
    async function loadHistorial(id) {
        const data = (await getJson(cfg.urls.getHistorial, { ticketId: id })).data;
        el("dtlHistorial").innerHTML = data.length
            ? data.map(h => '<tr><td class="text-nowrap">' + fmtDateTime(h.fecha) + '</td><td class="text-muted">' + escapeHtml(h.estatusAnterior || "—") + '</td><td><span class="badge-estatus">' + escapeHtml(h.estatusNuevo) + '</span></td><td>' + escapeHtml(h.comentario) + '</td><td>' + escapeHtml(h.usuarioCodigo) + '</td></tr>').join("")
            : '<tr><td colspan="5" class="text-muted text-center">Sin registros.</td></tr>';
    }
    async function loadLog(id) {
        const data = (await getJson(cfg.urls.getLog, { ticketId: id })).data;
        el("dtlLog").innerHTML = data.length
            ? data.map(l => '<tr><td class="text-nowrap">' + fmtDateTime(l.fechaHora) + '</td><td><span class="badge-accion">' + escapeHtml(l.accion) + '</span></td><td>' + escapeHtml(l.descripcion || "") + '</td><td class="text-muted">' + escapeHtml(l.valorAnterior || "—") + '</td><td>' + escapeHtml(l.valorNuevo || "—") + '</td><td>' + escapeHtml(l.usuarioCodigo || "") + '</td></tr>').join("")
            : '<tr><td colspan="6" class="text-muted text-center">Sin registros.</td></tr>';
    }

    // ---------- Init ----------
    $(async function () {
        state.detailModal = new bootstrap.Modal(el("ticketDetailModal"));
        state.modals.empleado = new bootstrap.Modal(el("empleadoModal"));
        state.modals.clasificacion = new bootstrap.Modal(el("clasificacionModal"));
        state.modals.categoria = new bootstrap.Modal(el("categoriaModal"));
        initTable();
        initTooltips();
        setDefaultDateRange();

        // Creación
        el("createSolicitante").addEventListener("change", autofillCorreo);
        // Dropdowns personalizados (Clasificación / Categoría)
        el("createClasificacionToggle").addEventListener("click", () => {
            ddOpen("createCategoria", false);
            ddOpen("createClasificacion", !el("createClasificacionMenu").classList.contains("show"));
        });
        el("createCategoriaToggle").addEventListener("click", () => {
            ddOpen("createClasificacion", false);
            ddOpen("createCategoria", !el("createCategoriaMenu").classList.contains("show"));
        });
        document.addEventListener("click", e => {
            if (!e.target.closest("#ddClasificacion")) ddOpen("createClasificacion", false);
            if (!e.target.closest("#ddCategoria")) ddOpen("createCategoria", false);
        });
        el("createDescripcion").addEventListener("input", e => el("descCount").textContent = e.target.value.length);
        el("createTicketForm").addEventListener("submit", submitCreate);
        el("createResetBtn").addEventListener("click", () => setTimeout(() => {
            el("descCount").textContent = "0";
            resetCreateDropdowns();
            selectCurrentSolicitante();
        }, 0));

        // Modales de catálogo
        el("btnNuevoSolicitante").addEventListener("click", () => state.modals.empleado.show());
        el("empleadoForm").addEventListener("submit", async ev => {
            ev.preventDefault();
            try {
                const r = await postForm(cfg.urls.addEmpleado, { Nombre: el("empNombre").value, Correo: el("empCorreo").value, Telefono: el("empTelefono").value });
                state.empleados.push(r.data); appendEmpleado(r.data);
                el("createSolicitante").value = r.data.id; autofillCorreo();
                state.modals.empleado.hide(); el("empleadoForm").reset();
                toast("Solicitante agregado.", "success");
            } catch (e) { toast(e.message, "danger"); }
        });
        el("clasificacionForm").addEventListener("submit", async ev => {
            ev.preventDefault();
            try {
                const r = await postForm(cfg.urls.addClasificacion, { Nombre: el("clasNombre").value });
                state.clasificaciones.push(r.data);
                addOption(el("dtlClasificacion"), r.data);
                addOption(el("filterClasificacion"), r.data);
                ddSetItems("createClasificacionMenu", state.clasificaciones, onSelectClasif, () => state.modals.clasificacion.show());
                onSelectClasif(r.data.value, r.data.text);
                state.modals.clasificacion.hide(); el("clasificacionForm").reset();
                toast("Clasificación agregada.", "success");
            } catch (e) { toast(e.message, "danger"); }
        });
        el("categoriaForm").addEventListener("submit", async ev => {
            ev.preventDefault();
            try {
                const clasId = el("catClasificacionId").value;
                const r = await postForm(cfg.urls.addCategoria, { ClasificacionId: clasId, Nombre: el("catNombre").value });
                await loadCategoriasDropdown(clasId);
                onSelectCat(r.data.value, r.data.text);
                state.modals.categoria.hide(); el("categoriaForm").reset();
                toast("Categoría agregada.", "success");
            } catch (e) { toast(e.message, "danger"); }
        });

        // Filtros: recarga automática al cambiar cualquiera
        ["filterDesde", "filterHasta", "filterEstatus", "filterClasificacion", "filterPrioridad", "filterSolicitante", "filterTipo"]
            .forEach(id => el(id).addEventListener("change", loadTickets));

        // Al cambiar de pestaña: Comentarios abre abajo (chat); las demás, arriba.
        document.querySelectorAll('#ticketDetailModal button[data-bs-toggle="tab"]').forEach(btn => {
            btn.addEventListener("shown.bs.tab", e => {
                const sc = document.querySelector("#ticketDetailModal .dtl-content");
                if (!sc) return;
                sc.scrollTop = e.target.getAttribute("data-bs-target") === "#tabComentarios" ? sc.scrollHeight : 0;
            });
        });

        // Detalle
        el("dtlClasificacion").addEventListener("change", e => loadCategorias(e.target.value, "dtlCategoria", false));
        el("btnDtlEdit").addEventListener("click", () => setEditMode(true));
        el("btnDtlCancelEdit").addEventListener("click", () => { if (state.currentTicket) populateDetail(state.currentTicket); });
        el("btnDtlSave").addEventListener("click", saveDetail);
        el("btnDtlChangeStatus").addEventListener("click", changeStatus);
        el("btnDtlAssign").addEventListener("click", assignResponsable);
        el("btnDtlInactivate").addEventListener("click", inactivate);
        el("btnDtlUpload").addEventListener("click", uploadDetailFiles);
        el("dtlCommentForm").addEventListener("submit", async ev => {
            ev.preventDefault();
            const id = el("dtlCommentTicketId").value;
            try { await postForm(cfg.urls.addComment, { TicketId: id, Comentario: el("dtlCommentBody").value }); el("dtlCommentBody").value = ""; await Promise.all([loadComments(id), loadLog(id)]); }
            catch (e) { toast(e.message, "danger"); }
        });

        try { await Promise.all([loadPrioridades(), loadEstatus(), loadClasificaciones(), loadEmpleados()]); }
        catch (e) { toast(e.message, "danger"); }
        await loadTickets();
    });
})();
