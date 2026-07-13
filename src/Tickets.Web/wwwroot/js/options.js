/* Módulo de Opciones de Tickets (admin de catálogos) */
(function () {
    "use strict";
    const cfg = window.optionsConfig;
    const state = { clasif: [], cat: [], pri: [], est: [], modals: {} };

    // ---------- utilidades ----------
    async function getJson(url) { return handle(await fetch(url, { headers: { "X-Requested-With": "XMLHttpRequest" } })); }
    async function postForm(url, data) {
        return handle(await fetch(url, {
            method: "POST", headers: { "X-Requested-With": "XMLHttpRequest", "Content-Type": "application/x-www-form-urlencoded" },
            body: new URLSearchParams(data).toString()
        }));
    }
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
    function estadoBadge(activo) {
        return '<span class="badge-estado ' + (activo ? "activo" : "inactivo") + '">' + (activo ? "Activo" : "Inactivo") + '</span>';
    }
    function acciones(kind, id, activo) {
        const toggle = activo
            ? '<button class="btn-icon danger js-toggle" title="Desactivar" data-kind="' + kind + '" data-id="' + id + '" data-activo="false"><i class="bi bi-slash-circle"></i></button>'
            : '<button class="btn-icon primary js-toggle" title="Activar" data-kind="' + kind + '" data-id="' + id + '" data-activo="true"><i class="bi bi-check-lg"></i></button>';
        return '<div class="d-flex gap-2 justify-content-end">' +
            '<button class="btn-icon js-edit" title="Editar" data-kind="' + kind + '" data-id="' + id + '"><i class="bi bi-pencil"></i></button>' +
            toggle + '</div>';
    }

    // ---------- carga / render ----------
    async function loadClasif() {
        state.clasif = (await getJson(cfg.urls.getClasificaciones)).data;
        el("tblClasif").innerHTML = state.clasif.map(c =>
            '<tr><td>' + esc(c.nombre) + '</td><td>' + estadoBadge(c.activo) + '</td><td class="text-end">' + acciones("clasif", c.id, c.activo) + '</td></tr>'
        ).join("") || '<tr><td colspan="3" class="text-muted text-center">Sin registros.</td></tr>';
    }
    async function loadCat() {
        state.cat = (await getJson(cfg.urls.getCategorias)).data;
        el("tblCat").innerHTML = state.cat.map(c =>
            '<tr><td>' + esc(c.clasificacionNombre) + '</td><td>' + esc(c.nombre) + '</td><td>' + estadoBadge(c.activo) + '</td><td class="text-end">' + acciones("cat", c.id, c.activo) + '</td></tr>'
        ).join("") || '<tr><td colspan="4" class="text-muted text-center">Sin registros.</td></tr>';
    }
    async function loadPri() {
        state.pri = (await getJson(cfg.urls.getPrioridades)).data;
        el("tblPri").innerHTML = state.pri.map(p =>
            '<tr><td>' + esc(p.nombre) + '</td><td class="text-muted small">' + esc(p.descripcion) + '</td><td>' + p.orden + '</td><td>' + estadoBadge(p.activo) + '</td><td class="text-end">' + acciones("pri", p.id, p.activo) + '</td></tr>'
        ).join("") || '<tr><td colspan="5" class="text-muted text-center">Sin registros.</td></tr>';
    }
    async function loadEst() {
        state.est = (await getJson(cfg.urls.getEstatus)).data;
        el("tblEst").innerHTML = state.est.map(e =>
            '<tr><td>' + esc(e.nombre) + '</td><td>' + e.orden + '</td><td>' + (e.esFinal ? "Sí" : "No") + '</td><td>' + estadoBadge(e.activo) + '</td><td class="text-end">' + acciones("est", e.id, e.activo) + '</td></tr>'
        ).join("") || '<tr><td colspan="5" class="text-muted text-center">Sin registros.</td></tr>';
    }

    // ---------- modales (abrir add/edit) ----------
    function openClasif(item) {
        el("clasifId").value = item ? item.id : 0;
        el("clasifNombre").value = item ? item.nombre : "";
        el("clasifActivo").checked = item ? item.activo : true;
        el("clasifTitle").textContent = item ? "Editar clasificación" : "Nueva clasificación";
        state.modals.clasif.show();
    }
    function fillClasifSelect(selected) {
        const sel = el("catClasif");
        const activas = state.clasif.filter(c => c.activo || c.id === selected);
        sel.innerHTML = activas.map(c => '<option value="' + c.id + '"' + (c.id === selected ? " selected" : "") + '>' + esc(c.nombre) + '</option>').join("");
    }
    function openCat(item) {
        el("catId").value = item ? item.id : 0;
        fillClasifSelect(item ? item.clasificacionId : (state.clasif.find(c => c.activo)?.id));
        el("catNombre").value = item ? item.nombre : "";
        el("catActivo").checked = item ? item.activo : true;
        el("catTitle").textContent = item ? "Editar categoría" : "Nueva categoría";
        state.modals.cat.show();
    }
    function openPri(item) {
        el("priId").value = item ? item.id : 0;
        el("priNombre").value = item ? item.nombre : "";
        el("priDescripcion").value = item ? item.descripcion : "";
        el("priOrden").value = item ? item.orden : 0;
        el("priActivo").checked = item ? item.activo : true;
        el("priTitle").textContent = item ? "Editar prioridad" : "Nueva prioridad";
        state.modals.pri.show();
    }
    function openEst(item) {
        el("estId").value = item ? item.id : 0;
        el("estNombre").value = item ? item.nombre : "";
        el("estOrden").value = item ? item.orden : 0;
        el("estEsFinal").checked = item ? item.esFinal : false;
        el("estActivo").checked = item ? item.activo : true;
        el("estTitle").textContent = item ? "Editar estatus" : "Nuevo estatus";
        state.modals.est.show();
    }

    // ---------- init ----------
    document.addEventListener("DOMContentLoaded", async () => {
        state.modals.clasif = new bootstrap.Modal(el("clasifModal"));
        state.modals.cat = new bootstrap.Modal(el("catModal"));
        state.modals.pri = new bootstrap.Modal(el("priModal"));
        state.modals.est = new bootstrap.Modal(el("estModal"));

        el("btnAddClasif").addEventListener("click", () => openClasif(null));
        el("btnAddCat").addEventListener("click", () => openCat(null));
        el("btnAddPri").addEventListener("click", () => openPri(null));
        el("btnAddEst").addEventListener("click", () => openEst(null));

        // edición / toggle (delegado)
        document.addEventListener("click", async e => {
            const edit = e.target.closest(".js-edit");
            const tog = e.target.closest(".js-toggle");
            if (edit) {
                const id = Number(edit.dataset.id), kind = edit.dataset.kind;
                if (kind === "clasif") openClasif(state.clasif.find(x => x.id === id));
                if (kind === "cat") openCat(state.cat.find(x => x.id === id));
                if (kind === "pri") openPri(state.pri.find(x => x.id === id));
                if (kind === "est") openEst(state.est.find(x => x.id === id));
            } else if (tog) {
                const id = Number(tog.dataset.id), kind = tog.dataset.kind, activo = tog.dataset.activo;
                const map = { clasif: [cfg.urls.toggleClasificacion, loadClasif], cat: [cfg.urls.toggleCategoria, loadCat], pri: [cfg.urls.togglePrioridad, loadPri], est: [cfg.urls.toggleEstatus, loadEst] };
                try { await postForm(map[kind][0], { id, activo }); await map[kind][1](); toast("Actualizado.", "success"); }
                catch (err) { toast(err.message, "danger"); }
            }
        });

        // submits
        el("clasifForm").addEventListener("submit", async ev => {
            ev.preventDefault();
            try { await postForm(cfg.urls.upsertClasificacion, { Id: el("clasifId").value, Nombre: el("clasifNombre").value, Activo: el("clasifActivo").checked }); state.modals.clasif.hide(); await loadClasif(); toast("Guardado.", "success"); }
            catch (e) { toast(e.message, "danger"); }
        });
        el("catForm").addEventListener("submit", async ev => {
            ev.preventDefault();
            try { await postForm(cfg.urls.upsertCategoria, { Id: el("catId").value, ClasificacionId: el("catClasif").value, Nombre: el("catNombre").value, Activo: el("catActivo").checked }); state.modals.cat.hide(); await loadCat(); toast("Guardado.", "success"); }
            catch (e) { toast(e.message, "danger"); }
        });
        el("priForm").addEventListener("submit", async ev => {
            ev.preventDefault();
            try { await postForm(cfg.urls.upsertPrioridad, { Id: el("priId").value, Nombre: el("priNombre").value, Descripcion: el("priDescripcion").value, Orden: el("priOrden").value || 0, Activo: el("priActivo").checked }); state.modals.pri.hide(); await loadPri(); toast("Guardado.", "success"); }
            catch (e) { toast(e.message, "danger"); }
        });
        el("estForm").addEventListener("submit", async ev => {
            ev.preventDefault();
            try { await postForm(cfg.urls.upsertEstatus, { Id: el("estId").value, Nombre: el("estNombre").value, Orden: el("estOrden").value || 0, EsFinal: el("estEsFinal").checked, Activo: el("estActivo").checked }); state.modals.est.hide(); await loadEst(); toast("Guardado.", "success"); }
            catch (e) { toast(e.message, "danger"); }
        });

        try { await Promise.all([loadClasif(), loadCat(), loadPri(), loadEst()]); }
        catch (e) { toast(e.message, "danger"); }
    });
})();
