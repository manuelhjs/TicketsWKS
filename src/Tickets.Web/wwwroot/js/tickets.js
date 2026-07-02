/* Módulo de Tickets — front-end (vanilla JS + Bootstrap 5) */
(function () {
    "use strict";

    const cfg = window.ticketsConfig;
    const STATUS = { 1: "Abierto", 2: "En Proceso", 3: "Cerrado" };
    const COL_COUNT = cfg.canManage ? 17 : 16;

    const state = { page: 1, pageSize: 50, total: 0, responsibles: [] };

    // ---------- Utilidades HTTP ----------
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
        if (!res.ok) {
            const msg = (body && body.message) || "Ocurrió un error (" + res.status + ").";
            throw new Error(msg);
        }
        return body;
    }

    // ---------- Toast simple ----------
    function toast(message, variant) {
        let c = document.getElementById("toastContainer");
        if (!c) {
            c = document.createElement("div");
            c.id = "toastContainer";
            c.className = "toast-container position-fixed top-0 end-0 p-3";
            document.body.appendChild(c);
        }
        const el = document.createElement("div");
        el.className = "toast align-items-center text-bg-" + (variant || "primary") + " border-0 show";
        el.innerHTML = '<div class="d-flex"><div class="toast-body">' + escapeHtml(message) +
            '</div><button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button></div>';
        c.appendChild(el);
        setTimeout(() => el.remove(), 4000);
    }

    // ---------- Helpers de formato ----------
    function escapeHtml(s) {
        if (s === null || s === undefined) return "";
        return String(s).replace(/[&<>"']/g, m =>
            ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[m]));
    }
    function fmtDate(v) { return v ? new Date(v).toLocaleDateString("es-MX") : "—"; }
    function fmtMoney(v) { return (v === null || v === undefined) ? "—" : Number(v).toLocaleString("es-MX", { style: "currency", currency: "MXN" }); }
    function fmtNum(v) { return (v === null || v === undefined) ? "—" : Number(v).toLocaleString("es-MX"); }

    // ---------- Filtros ----------
    function currentFilter() {
        return {
            Status: document.querySelector("input[name='statusFilter']:checked").value,
            Period: document.getElementById("filterPeriod").value,
            RequesterUserCode: document.getElementById("filterRequester").value,
            TicketTypeId: document.getElementById("filterType").value,
            DepartmentCode: document.getElementById("filterDepartment").value,
            ResponsibleUserCode: document.getElementById("filterResponsible").value,
            Page: state.page,
            PageSize: state.pageSize
        };
    }

    function fillSelect(id, options, keepFirst) {
        const sel = document.getElementById(id);
        const first = keepFirst ? sel.options[0] : null;
        sel.innerHTML = "";
        if (first) sel.appendChild(first);
        options.forEach(o => {
            const opt = document.createElement("option");
            opt.value = o.value;
            opt.textContent = o.text;
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
        document.getElementById("statOpen").textContent = r.data.totalOpen;
        document.getElementById("statInProgress").textContent = r.data.totalInProgress;
        document.getElementById("statClosed").textContent = r.data.totalClosed;
    }

    // ---------- Listado ----------
    async function loadTickets() {
        const body = document.getElementById("ticketsBody");
        body.innerHTML = '<tr><td colspan="' + COL_COUNT + '" class="text-center text-muted py-4">Cargando…</td></tr>';
        try {
            const r = await getJson(cfg.urls.getTickets, currentFilter());
            state.total = r.total;
            renderRows(r.items);
            renderPagination();
        } catch (e) {
            body.innerHTML = '<tr><td colspan="' + COL_COUNT + '" class="text-center text-danger py-4">' + escapeHtml(e.message) + '</td></tr>';
        }
    }

    function renderRows(items) {
        const body = document.getElementById("ticketsBody");
        if (!items.length) {
            body.innerHTML = '<tr><td colspan="' + COL_COUNT + '" class="text-center text-muted py-4">Sin tickets.</td></tr>';
            return;
        }
        body.innerHTML = "";
        items.forEach(t => body.appendChild(renderRow(t)));
    }

    function renderRow(t) {
        const tr = document.createElement("tr");
        tr.dataset.id = t.id;

        // Estatus (select editable)
        const statusOptions = Object.entries(STATUS)
            .map(([v, txt]) => '<option value="' + v + '"' + (t.status == v ? " selected" : "") + '>' + txt + '</option>').join("");
        const statusSel = '<select class="form-select form-select-sm js-status">' + statusOptions + '</select>';

        // Fecha estimada de cierre (habilitada si no está Abierto)
        const estVal = t.estimatedCloseDate || "";
        const estInput = '<input type="date" class="form-control form-control-sm js-estimate" value="' + estVal + '"' +
            (t.status == 1 ? " disabled" : "") + ' />';

        // Responsable
        let responsibleCell;
        if (cfg.canManage) {
            const opts = buildResponsibleOptions(t.responsibleUserCode, t.responsibleName);
            responsibleCell = '<select class="form-select form-select-sm js-responsible">' + opts + '</select>';
        } else {
            responsibleCell = escapeHtml(t.responsibleName || t.responsibleUserCode || "—");
        }

        // Anexo
        let anexo = "";
        if (t.attachmentFileName) {
            anexo = '<a href="' + cfg.urls.downloadAttachment + '?fileName=' + encodeURIComponent(t.attachmentFileName) +
                '" class="btn btn-sm btn-outline-secondary" title="Descargar"><i class="bi bi-download"></i>↓</a> ';
        }
        anexo += '<button class="btn btn-sm btn-outline-primary js-upload" title="Adjuntar">+</button>';

        const cells = [
            '<td>' + t.id + '</td>',
            '<td>' + escapeHtml(t.ticketTypeName) + '</td>',
            '<td>' + escapeHtml(t.requesterName || t.requesterUserCode) + '</td>',
            '<td>' + escapeHtml(t.departmentName || t.departmentCode || "—") + '</td>',
            '<td style="min-width:180px">' + responsibleCell + '</td>',
            '<td>' + fmtDate(t.createdAt) + '</td>',
            '<td style="min-width:200px" title="' + escapeHtml(t.description) + '">' + escapeHtml(t.description) + '</td>',
            '<td style="min-width:90px">' + anexo + '</td>',
            '<td style="min-width:130px">' + statusSel + '</td>',
            '<td style="min-width:150px">' + estInput + '</td>',
            '<td>' + fmtDate(t.closedAt) + '</td>',
            '<td>' + escapeHtml(t.qualityDepartment || "—") + '</td>',
            '<td>' + escapeHtml(t.machine || "—") + '</td>',
            '<td class="text-end">' + fmtMoney(t.amount) + '</td>',
            '<td class="text-end">' + fmtNum(t.quantity) + '</td>',
            '<td><button class="btn btn-sm btn-info js-comments">Ver</button></td>'
        ];
        if (cfg.canManage) {
            cells.push('<td><button class="btn btn-sm btn-outline-danger js-inactivate" title="Inactivar">✕</button></td>');
        }
        tr.innerHTML = cells.join("");
        wireRow(tr, t);
        return tr;
    }

    function buildResponsibleOptions(code, name) {
        let list = state.responsibles.slice();
        if (code && !list.some(o => o.value === code)) list.unshift({ value: code, text: name || code });
        return '<option value="">—</option>' + list
            .map(o => '<option value="' + escapeHtml(o.value) + '"' + (o.value === code ? " selected" : "") + '>' + escapeHtml(o.text) + '</option>')
            .join("");
    }

    function wireRow(tr, t) {
        const id = t.id;

        tr.querySelector(".js-status").addEventListener("change", async e => {
            const status = e.target.value;
            const estimate = tr.querySelector(".js-estimate").value;
            try {
                await postForm(cfg.urls.updateStatus, { TicketId: id, Status: status, EstimatedCloseDate: estimate || "" });
                toast("Estatus actualizado.", "success");
                await refreshDashboard();
                await loadTickets();
            } catch (err) { toast(err.message, "danger"); await loadTickets(); }
        });

        const est = tr.querySelector(".js-estimate");
        est.addEventListener("change", async e => {
            if (tr.querySelector(".js-status").value == 1) return;
            if (!e.target.value) return;
            try {
                await postForm(cfg.urls.updateEstimatedCloseDate, { TicketId: id, EstimatedCloseDate: e.target.value });
                toast("Fecha estimada actualizada.", "success");
            } catch (err) { toast(err.message, "danger"); }
        });

        const resp = tr.querySelector(".js-responsible");
        if (resp) resp.addEventListener("change", async e => {
            try {
                await postForm(cfg.urls.updateResponsible, { TicketId: id, ResponsibleUserCode: e.target.value });
                toast("Responsable actualizado.", "success");
            } catch (err) { toast(err.message, "danger"); await loadTickets(); }
        });

        tr.querySelector(".js-comments").addEventListener("click", () => openComments(id));

        tr.querySelector(".js-upload").addEventListener("click", () => uploadFor(id));

        const inact = tr.querySelector(".js-inactivate");
        if (inact) inact.addEventListener("click", async () => {
            if (!confirm("¿Inactivar el ticket #" + id + "?")) return;
            try {
                await postForm(cfg.urls.setInactive, { id: id });
                toast("Ticket inactivado.", "success");
                await refreshDashboard();
                await loadTickets();
            } catch (err) { toast(err.message, "danger"); }
        });
    }

    // ---------- Paginación ----------
    function renderPagination() {
        const pages = Math.max(1, Math.ceil(state.total / state.pageSize));
        const ul = document.getElementById("pagination");
        ul.innerHTML = "";
        const info = document.getElementById("paginationInfo");
        info.textContent = state.total + " ticket(s) — página " + state.page + " de " + pages;

        const add = (label, page, disabled, active) => {
            const li = document.createElement("li");
            li.className = "page-item" + (disabled ? " disabled" : "") + (active ? " active" : "");
            const a = document.createElement("a");
            a.className = "page-link";
            a.href = "#";
            a.textContent = label;
            a.addEventListener("click", ev => { ev.preventDefault(); if (!disabled && !active) { state.page = page; loadTickets(); } });
            li.appendChild(a);
            ul.appendChild(li);
        };

        add("«", state.page - 1, state.page === 1, false);
        const from = Math.max(1, state.page - 2), to = Math.min(pages, state.page + 2);
        for (let p = from; p <= to; p++) add(p, p, false, p === state.page);
        add("»", state.page + 1, state.page === pages, false);
    }

    // ---------- Comentarios ----------
    let commentsModal, createModal;

    async function openComments(ticketId) {
        document.getElementById("commentsTicketId").textContent = ticketId;
        document.getElementById("commentTicketIdInput").value = ticketId;
        const list = document.getElementById("commentsList");
        list.innerHTML = '<p class="text-muted text-center">Cargando…</p>';
        commentsModal.show();
        try {
            const r = await getJson(cfg.urls.getComments, { ticketId });
            renderComments(r.data);
        } catch (e) { list.innerHTML = '<p class="text-danger text-center">' + escapeHtml(e.message) + '</p>'; }
    }

    function renderComments(items) {
        const list = document.getElementById("commentsList");
        if (!items.length) { list.innerHTML = '<p class="text-muted text-center">Sin comentarios.</p>'; return; }
        list.innerHTML = items.map(c =>
            '<div class="comment"><div class="d-flex justify-content-between"><strong>' + escapeHtml(c.authorName) +
            '</strong><small class="text-muted">' + new Date(c.createdAt).toLocaleString("es-MX") + '</small></div>' +
            '<div>' + escapeHtml(c.body) + '</div></div>').join("");
    }

    // ---------- Adjuntar archivo ----------
    function uploadFor(ticketId) {
        const input = document.createElement("input");
        input.type = "file";
        input.onchange = async () => {
            if (!input.files.length) return;
            const fd = new FormData();
            fd.append("ticketId", ticketId);
            fd.append("file", input.files[0]);
            try {
                await postFile(cfg.urls.uploadAttachment, fd);
                toast("Archivo adjuntado.", "success");
                await loadTickets();
            } catch (err) { toast(err.message, "danger"); }
        };
        input.click();
    }

    // ---------- Modal de creación ----------
    async function loadAreas() {
        const r = await getJson(cfg.urls.getAreas);
        fillSelect("createArea", r.data, true);
    }

    async function onAreaChange() {
        const areaSel = document.getElementById("createArea");
        const areaCode = areaSel.value;
        // Mostrar/ocultar campos condicionales
        document.querySelectorAll("[data-area-field]").forEach(el =>
            el.classList.toggle("d-none", el.getAttribute("data-area-field") !== areaCode));

        const typeSel = document.getElementById("createType");
        typeSel.innerHTML = '<option value="">Seleccione…</option>';
        if (!areaCode) return;
        const r = await getJson(cfg.urls.getTicketTypes, { areaCode });
        r.data.forEach(t => {
            const opt = document.createElement("option");
            opt.value = t.id;
            opt.textContent = t.name;
            typeSel.appendChild(opt);
        });
    }

    async function submitCreate(ev) {
        ev.preventDefault();
        const form = document.getElementById("createTicketForm");
        const data = new URLSearchParams(new FormData(form)).toString();
        try {
            const r = await postForm(cfg.urls.create, data);
            const fileInput = document.getElementById("createFile");
            if (fileInput.files.length && r.id) {
                const fd = new FormData();
                fd.append("ticketId", r.id);
                fd.append("file", fileInput.files[0]);
                await postFile(cfg.urls.uploadAttachment, fd);
            }
            toast("Ticket creado.", "success");
            createModal.hide();
            form.reset();
            document.querySelectorAll("[data-area-field]").forEach(el => el.classList.add("d-none"));
            await refreshDashboard();
            await loadFilterOptions();
            state.page = 1;
            await loadTickets();
        } catch (err) { toast(err.message, "danger"); }
    }

    // ---------- Init ----------
    document.addEventListener("DOMContentLoaded", async () => {
        commentsModal = new bootstrap.Modal(document.getElementById("commentsModal"));
        createModal = new bootstrap.Modal(document.getElementById("createTicketModal"));

        document.querySelectorAll("input[name='statusFilter']").forEach(r =>
            r.addEventListener("change", () => { state.page = 1; loadTickets(); }));
        document.getElementById("btnApplyFilters").addEventListener("click", () => { state.page = 1; loadTickets(); });
        document.getElementById("btnClearFilters").addEventListener("click", () => {
            document.getElementById("filterRequester").value = "";
            document.getElementById("filterType").value = "";
            document.getElementById("filterDepartment").value = "";
            document.getElementById("filterResponsible").value = "";
            document.getElementById("filterPeriod").value = "last2Years";
            document.getElementById("stAll").checked = true;
            state.page = 1; loadTickets();
        });

        document.getElementById("createArea").addEventListener("change", onAreaChange);
        document.getElementById("createTicketForm").addEventListener("submit", submitCreate);
        document.getElementById("addCommentForm").addEventListener("submit", async ev => {
            ev.preventDefault();
            const form = ev.target;
            try {
                await postForm(cfg.urls.addComment, new URLSearchParams(new FormData(form)).toString());
                document.getElementById("commentBody").value = "";
                const r = await getJson(cfg.urls.getComments, { ticketId: document.getElementById("commentTicketIdInput").value });
                renderComments(r.data);
            } catch (err) { toast(err.message, "danger"); }
        });

        try {
            await loadFilterOptions();
            await loadAreas();
        } catch (e) { toast(e.message, "danger"); }
        await loadTickets();
    });
})();
