/* Módulo de Tickets — front-end (jQuery + DataTables + Bootstrap 5) */
(function () {
    "use strict";

    const cfg = window.ticketsConfig;
    const STATUS = { 1: "Creado", 2: "En Proceso", 3: "Cerrado" };
    const STATUS_BADGE = { 1: "bg-warning text-dark", 2: "bg-info text-dark", 3: "bg-success" };

    const state = { responsibles: [], table: null, detailModal: null };

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
        try { body = await res.json(); } catch { /* sin cuerpo */ }
        if (!res.ok) throw new Error((body && body.message) || "Ocurrió un error (" + res.status + ").");
        return body;
    }

    // ---------- Utilidades ----------
    function toast(message, variant) {
        let c = document.getElementById("toastContainer");
        if (!c) {
            c = document.createElement("div");
            c.id = "toastContainer";
            c.className = "toast-container position-fixed top-0 end-0 p-3";
            document.body.appendChild(c);
        }
        const e = document.createElement("div");
        e.className = "toast align-items-center text-bg-" + (variant || "primary") + " border-0 show";
        e.innerHTML = '<div class="d-flex"><div class="toast-body">' + escapeHtml(message) +
            '</div><button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button></div>';
        c.appendChild(e);
        setTimeout(() => e.remove(), 4000);
    }
    function escapeHtml(s) {
        if (s === null || s === undefined) return "";
        return String(s).replace(/[&<>"']/g, m => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[m]));
    }
    function fmtDate(v) {
        if (!v) return "—";
        if (typeof v === "string" && /^\d{4}-\d{2}-\d{2}/.test(v)) {
            const [y, m, d] = v.substring(0, 10).split("-");
            return d + "/" + m + "/" + y;
        }
        return new Date(v).toLocaleDateString("es-MX");
    }
    function fmtDateTime(v) { return v ? new Date(v).toLocaleString("es-MX") : "—"; }
    function fmtMoney(v) { return (v === null || v === undefined) ? "—" : Number(v).toLocaleString("es-MX", { style: "currency", currency: "MXN" }); }
    function fmtNum(v) { return (v === null || v === undefined) ? "—" : Number(v).toLocaleString("es-MX"); }
    function el(id) { return document.getElementById(id); }
    function statusBadge(status) {
        return '<span class="badge ' + (STATUS_BADGE[status] || "bg-secondary") + '">' + (STATUS[status] || "—") + '</span>';
    }

    // ---------- Filtros ----------
    function val(id) { const e = el(id); return e ? e.value : ""; }
    function currentFilter() {
        return {
            Status: document.querySelector("input[name='statusFilter']:checked").value,
            Period: val("filterPeriod"),
            RequesterUserCode: val("filterRequester"),
            TicketTypeId: val("filterType"),
            DepartmentCode: val("filterDepartment"),
            ResponsibleUserCode: val("filterResponsible")
        };
    }
    function fillSelect(id, options, keepFirst) {
        const sel = el(id);
        if (!sel) return;
        const first = keepFirst ? sel.options[0] : null;
        sel.innerHTML = "";
        if (first) sel.appendChild(first);
        options.forEach(o => {
            const opt = document.createElement("option");
            opt.value = o.value; opt.textContent = o.text;
            sel.appendChild(opt);
        });
    }
    async function loadFilterOptions() {
        const r = await getJson(cfg.urls.getFilterOptions);
        const d = r.data;
        state.responsibles = d.responsibles || [];
        fillSelect("filterRequester", d.requesters, true);
        fillSelect("filterType", d.ticketTypes, true);
        fillSelect("filterDepartment", d.departments, true);
        fillSelect("filterResponsible", d.responsibles, true);
    }
    async function refreshDashboard() {
        const r = await getJson(cfg.urls.getDashboard);
        el("statOpen").textContent = r.data.totalOpen;
        if (el("statInProgress")) el("statInProgress").textContent = r.data.totalInProgress;
        el("statClosed").textContent = r.data.totalClosed;
    }

    // ---------- Tabla (DataTables) ----------
    const DT_LANG = {
        emptyTable: "Sin tickets.",
        info: "Mostrando _START_ a _END_ de _TOTAL_",
        infoEmpty: "0 registros",
        infoFiltered: "(filtrado de _MAX_)",
        lengthMenu: "Mostrar _MENU_",
        loadingRecords: "Cargando…",
        processing: "Procesando…",
        search: "Buscar:",
        zeroRecords: "Sin coincidencias",
        paginate: { first: "Primero", last: "Último", next: "Siguiente", previous: "Anterior" }
    };

    function initTable() {
        state.table = $("#ticketsTable").DataTable({
            data: [],
            language: DT_LANG,
            order: [[0, "desc"]],
            pageLength: 25,
            columns: [
                { data: "id" },
                { data: "ticketTypeName", defaultContent: "—" },
                { data: "requesterName", render: (d, t, row) => escapeHtml(d || row.requesterUserCode) },
                { data: "departmentName", render: (d, t, row) => escapeHtml(d || row.departmentCode || "—") },
                { data: "responsibleName", render: (d, t, row) => escapeHtml(d || row.responsibleUserCode || "—") },
                { data: "createdAt", render: fmtDate },
                { data: "status", render: statusBadge },
                { data: "estimatedCloseDate", render: fmtDate },
                { data: "closedAt", render: fmtDate }
            ]
        });

        $("#ticketsTable tbody").on("click", "tr", function () {
            const data = state.table.row(this).data();
            if (data) openDetail(data.id);
        });
    }

    async function loadTickets() {
        try {
            const r = await getJson(cfg.urls.getTickets, currentFilter());
            state.table.clear().rows.add(r.items).draw();
        } catch (e) {
            toast(e.message, "danger");
        }
    }

    // ---------- Modal de detalle ----------
    function setText(id, value) { el(id).textContent = (value === null || value === undefined || value === "") ? "—" : value; }

    async function openDetail(id) {
        try {
            const r = await getJson(cfg.urls.getTicket, { id });
            populateDetail(r.data);
            await loadComments(id);
            state.detailModal.show();
        } catch (e) { toast(e.message, "danger"); }
    }

    function populateDetail(t) {
        setText("dtlId", "#" + t.id);
        setText("dtlType", t.ticketTypeName);
        setText("dtlArea", t.areaName);
        el("dtlStatusBadge").className = "badge " + (STATUS_BADGE[t.status] || "bg-secondary");
        el("dtlStatusBadge").textContent = STATUS[t.status] || "—";
        setText("dtlRequester", t.requesterName || t.requesterUserCode);
        setText("dtlDepartment", t.departmentName || t.departmentCode);
        setText("dtlCreated", fmtDateTime(t.createdAt));
        setText("dtlClosed", t.closedAt ? fmtDateTime(t.closedAt) : "—");
        setText("dtlQuality", t.qualityDepartment);
        setText("dtlMachine", t.machine);
        setText("dtlAmount", fmtMoney(t.amount));
        setText("dtlQuantity", fmtNum(t.quantity));
        setText("dtlDescription", t.description);

        const att = el("dtlAttachment");
        if (t.attachmentFileName) {
            att.innerHTML = '<a href="' + cfg.urls.downloadAttachment + '?fileName=' +
                encodeURIComponent(t.attachmentFileName) + '">' + escapeHtml(t.attachmentFileName) + '</a>';
        } else {
            att.textContent = "Sin archivo";
        }
        el("btnDtlUpload").dataset.id = t.id;
        el("dtlCommentTicketId").value = t.id;

        // Gestión (solo TI)
        const manage = el("dtlManage");
        if (cfg.isIt) {
            manage.classList.remove("d-none");
            manage.dataset.id = t.id;
            el("dtlStatus").value = t.status;
            el("dtlEstimate").value = t.estimatedCloseDate || "";
            el("dtlCategory").value = t.category || "";
            fillResponsibleSelect(t.responsibleUserCode, t.responsibleName);
        } else {
            manage.classList.add("d-none");
        }
    }

    function fillResponsibleSelect(code, name) {
        const sel = el("dtlResponsible");
        let list = state.responsibles.slice();
        if (code && !list.some(o => o.value === code)) list.unshift({ value: code, text: name || code });
        sel.innerHTML = '<option value="">—</option>' + list
            .map(o => '<option value="' + escapeHtml(o.value) + '"' + (o.value === code ? " selected" : "") + '>' + escapeHtml(o.text) + '</option>')
            .join("");
    }

    async function saveDetail() {
        const id = el("dtlManage").dataset.id;
        const status = el("dtlStatus").value;
        const estimate = el("dtlEstimate").value;
        const responsible = el("dtlResponsible").value;
        const category = el("dtlCategory").value;
        try {
            await postForm(cfg.urls.updateStatus, { TicketId: id, Status: status, EstimatedCloseDate: estimate || "" });
            if (responsible) await postForm(cfg.urls.updateResponsible, { TicketId: id, ResponsibleUserCode: responsible });
            if (category) await postForm(cfg.urls.updateCategory, { TicketId: id, Category: category });
            toast("Cambios guardados.", "success");
            state.detailModal.hide();
            await refreshDashboard();
            await loadTickets();
        } catch (e) { toast(e.message, "danger"); }
    }

    async function inactivateDetail() {
        const id = el("dtlManage").dataset.id;
        if (!confirm("¿Inactivar el ticket #" + id + "?")) return;
        try {
            await postForm(cfg.urls.setInactive, { id });
            toast("Ticket inactivado.", "success");
            state.detailModal.hide();
            await refreshDashboard();
            await loadTickets();
        } catch (e) { toast(e.message, "danger"); }
    }

    function uploadAttachment(id) {
        const input = document.createElement("input");
        input.type = "file";
        input.onchange = async () => {
            if (!input.files.length) return;
            const fd = new FormData();
            fd.append("ticketId", id);
            fd.append("file", input.files[0]);
            try {
                await postFile(cfg.urls.uploadAttachment, fd);
                toast("Archivo adjuntado.", "success");
                await openDetail(id);
                await loadTickets();
            } catch (e) { toast(e.message, "danger"); }
        };
        input.click();
    }

    // ---------- Comentarios ----------
    async function loadComments(ticketId) {
        const list = el("dtlComments");
        list.innerHTML = '<p class="text-muted text-center small">Cargando…</p>';
        try {
            const r = await getJson(cfg.urls.getComments, { ticketId });
            renderComments(r.data);
        } catch (e) { list.innerHTML = '<p class="text-danger text-center small">' + escapeHtml(e.message) + '</p>'; }
    }
    function renderComments(items) {
        const list = el("dtlComments");
        if (!items.length) { list.innerHTML = '<p class="text-muted text-center small">Sin comentarios.</p>'; return; }
        list.innerHTML = items.map(c =>
            '<div class="comment"><div class="d-flex justify-content-between"><strong>' + escapeHtml(c.authorName) +
            '</strong><small class="text-muted">' + fmtDateTime(c.createdAt) + '</small></div>' +
            '<div>' + escapeHtml(c.body) + '</div></div>').join("");
    }

    // ---------- Formulario de creación ----------
    async function loadAreas() {
        const r = await getJson(cfg.urls.getAreas);
        fillSelect("createArea", r.data, true);
    }
    async function onAreaChange() {
        const areaCode = el("createArea").value;
        document.querySelectorAll("[data-area-field]").forEach(e =>
            e.classList.toggle("d-none", e.getAttribute("data-area-field") !== areaCode));

        const typeSel = el("createType");
        typeSel.innerHTML = '<option value="">Seleccione…</option>';
        if (!areaCode) return;
        const r = await getJson(cfg.urls.getTicketTypes, { areaCode });
        r.data.forEach(t => {
            const opt = document.createElement("option");
            opt.value = t.id; opt.textContent = t.name;
            typeSel.appendChild(opt);
        });
    }
    async function submitCreate(ev) {
        ev.preventDefault();
        const form = el("createTicketForm");
        const data = new URLSearchParams(new FormData(form)).toString();
        try {
            const r = await postForm(cfg.urls.create, data);
            const fileInput = el("createFile");
            if (fileInput.files.length && r.id) {
                const fd = new FormData();
                fd.append("ticketId", r.id);
                fd.append("file", fileInput.files[0]);
                await postFile(cfg.urls.uploadAttachment, fd);
            }
            toast("Ticket creado.", "success");
            form.reset();
            document.querySelectorAll("[data-area-field]").forEach(e => e.classList.add("d-none"));
            await refreshDashboard();
            if (cfg.isIt) await loadFilterOptions();
            await loadTickets();
        } catch (e) { toast(e.message, "danger"); }
    }

    // ---------- Init ----------
    $(async function () {
        state.detailModal = new bootstrap.Modal(el("ticketDetailModal"));
        initTable();

        document.querySelectorAll("input[name='statusFilter']").forEach(r => r.addEventListener("change", loadTickets));
        el("btnApplyFilters").addEventListener("click", loadTickets);
        el("btnClearFilters").addEventListener("click", () => {
            ["filterRequester", "filterType", "filterDepartment", "filterResponsible"].forEach(id => { if (el(id)) el(id).value = ""; });
            el("filterPeriod").value = "last2Years";
            el("stAll").checked = true;
            loadTickets();
        });

        el("createArea").addEventListener("change", onAreaChange);
        el("createTicketForm").addEventListener("submit", submitCreate);
        el("createResetBtn").addEventListener("click", () =>
            document.querySelectorAll("[data-area-field]").forEach(e => e.classList.add("d-none")));

        el("btnDtlUpload").addEventListener("click", e => uploadAttachment(e.target.dataset.id));
        if (cfg.isIt) {
            el("btnDtlSave").addEventListener("click", saveDetail);
            el("btnDtlInactivate").addEventListener("click", inactivateDetail);
        }
        el("dtlCommentForm").addEventListener("submit", async ev => {
            ev.preventDefault();
            const ticketId = el("dtlCommentTicketId").value;
            try {
                await postForm(cfg.urls.addComment, { TicketId: ticketId, Body: el("dtlCommentBody").value });
                el("dtlCommentBody").value = "";
                await loadComments(ticketId);
            } catch (e) { toast(e.message, "danger"); }
        });

        try {
            if (cfg.isIt) await loadFilterOptions();
            await loadAreas();
        } catch (e) { toast(e.message, "danger"); }
        await loadTickets();
    });
})();
