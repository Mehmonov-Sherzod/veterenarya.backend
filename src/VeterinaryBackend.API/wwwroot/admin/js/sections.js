// Sections tab — list, create, edit, delete navigation sections.
// Sections form a parent/child hierarchy (ota–bola). The list flattens that tree
// in depth-first order so children sit visually under their parent.
const Sections = {
  cache: [],
  // Existing head linked to the currently-edited section (null when creating, or
  // when the section has no head yet). Used to decide whether the inline
  // "section head" sub-form should issue a create or update on save.
  currentHead: null,

  init() {
    document.getElementById('new-section-btn').addEventListener('click', () => this.openModal());
    document.querySelectorAll('#section-modal .modal-close').forEach(b =>
      b.addEventListener('click', () => this.closeModal()));
    document.getElementById('section-modal').addEventListener('click', (e) => {
      if (e.target.id === 'section-modal') this.closeModal();
    });
    document.getElementById('section-save-btn').addEventListener('click', () => this.save());

    // Section-head sub-form: photo upload + remove
    const photoPick = document.getElementById('sh-photo-pick');
    const photoInput = document.getElementById('sh-photo-input');
    const photoRemove = document.getElementById('sh-photo-remove');
    if (photoPick && photoInput) {
      photoPick.addEventListener('click', () => photoInput.click());
      photoInput.addEventListener('change', (e) => this.handleHeadPhotoUpload(e));
    }
    if (photoRemove) photoRemove.addEventListener('click', () => this.clearHeadPhoto());
  },

  async load() {
    const tbody = document.getElementById('sections-tbody');
    tbody.innerHTML = `<tr><td colspan="5" class="px-4 py-12 text-center text-slate-400">
      <svg class="inline animate-spin h-5 w-5 mr-2" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
        <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
        <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v4a4 4 0 00-4 4H4z"></path>
      </svg>Yuklanmoqda...</td></tr>`;
    try {
      const list = await Api.listSections({ onlyActive: false });
      this.cache = list;
      this.renderRows(this.flattenHierarchy(list));
      this.updateStats(list);
    } catch (err) {
      tbody.innerHTML = `<tr><td colspan="5" class="px-4 py-12 text-center text-rose-500">${escapeHtml(err.message)}</td></tr>`;
    }
  },

  /**
   * Walk the flat list (which already carries `parentId`) and emit rows in depth-first order
   * with a `_depth` and `_parentTitle` annotation so the UI can render an indented hierarchy.
   */
  flattenHierarchy(flat) {
    const byParent = new Map();
    const byId = new Map();
    flat.forEach(s => byId.set(s.id, s));
    flat.forEach(s => {
      const key = s.parentId == null ? 0 : s.parentId;
      if (!byParent.has(key)) byParent.set(key, []);
      byParent.get(key).push(s);
    });
    byParent.forEach(arr => arr.sort((a, b) => (a.sortOrder - b.sortOrder) || (a.id - b.id)));

    const out = [];
    const visit = (parentId, depth) => {
      const children = byParent.get(parentId) || [];
      for (const node of children) {
        const parent = node.parentId != null ? byId.get(node.parentId) : null;
        out.push({ ...node, _depth: depth, _parentTitle: parent?.title ?? null });
        visit(node.id, depth + 1);
      }
    };
    visit(0, 0);

    // Orphans whose parent was filtered out — surface them at the root so they remain editable.
    const seen = new Set(out.map(s => s.id));
    flat.forEach(s => {
      if (!seen.has(s.id)) out.push({ ...s, _depth: 0, _parentTitle: null });
    });
    return out;
  },

  updateStats(list) {
    const setStat = (key, val) => {
      const el = document.querySelector(`[data-stat="${key}"]`);
      if (el) el.textContent = val;
    };
    setStat('sections-total', list.length);
    setStat('sections-active', list.filter(s => s.isActive).length);
    setStat('sections-content-total', list.reduce((s, x) => s + (x.contentCount || 0), 0));
    setStat('sections-empty', list.filter(s => (s.contentCount || 0) === 0).length);
  },

  renderRows(items) {
    const tbody = document.getElementById('sections-tbody');
    if (!items.length) {
      tbody.innerHTML = `<tr><td colspan="5" class="px-4 py-12">
        <div class="max-w-2xl mx-auto">
          <div class="text-center mb-6">
            <div class="text-4xl mb-2">👋</div>
            <h3 class="font-display text-xl font-bold text-slate-900 dark:text-white mb-1">Salom, Admin!</h3>
            <p class="text-sm text-slate-500 dark:text-slate-400">Saytga ma'lumot qo'shish uchun 3 ta oddiy qadam:</p>
          </div>

          <div class="space-y-3 mb-6">
            <div class="quickstart-step">
              <div class="quickstart-num">1</div>
              <div class="flex-1 text-left">
                <div class="font-semibold text-slate-900 dark:text-white text-sm">Bo'lim yarating</div>
                <div class="text-xs text-slate-500 dark:text-slate-400 mt-0.5">Saytdagi navigatsiya bandi (masalan: <em>Xizmatlar, Doktorlar, Galereya</em>)</div>
              </div>
            </div>
            <div class="quickstart-step">
              <div class="quickstart-num">2</div>
              <div class="flex-1 text-left">
                <div class="font-semibold text-slate-900 dark:text-white text-sm">Bo'lim ichiga blok qo'shing</div>
                <div class="text-xs text-slate-500 dark:text-slate-400 mt-0.5">Har bir blok = <strong>1 ta rasm + 3 tilda matn</strong>. Saytda navbat-navbat chiqadi.</div>
              </div>
            </div>
            <div class="quickstart-step">
              <div class="quickstart-num">3</div>
              <div class="flex-1 text-left">
                <div class="font-semibold text-slate-900 dark:text-white text-sm">Tayyor!</div>
                <div class="text-xs text-slate-500 dark:text-slate-400 mt-0.5">Saytga kiring va natijani ko'ring. Istalgan vaqtda tahrirlay olasiz.</div>
              </div>
            </div>
          </div>

          <div class="text-center">
            <button id="quickstart-cta" class="inline-flex items-center gap-2 bg-brand-600 hover:bg-brand-700 text-white px-6 py-3 rounded-xl text-sm font-medium transition shadow-lg shadow-brand-600/30">
              Birinchi bo'limni yarataylik
              <svg xmlns="http://www.w3.org/2000/svg" class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2.5"><path stroke-linecap="round" stroke-linejoin="round" d="M17 8l4 4m0 0l-4 4m4-4H3"/></svg>
            </button>
          </div>
        </div>
      </td></tr>`;
      const cta = document.getElementById('quickstart-cta');
      if (cta) cta.addEventListener('click', () => this.openModal());
      return;
    }

    tbody.innerHTML = items.map((s, i) => {
      const depth = s._depth || 0;
      const indent = depth * 24;
      const branchIcon = depth > 0
        ? `<svg xmlns="http://www.w3.org/2000/svg" class="w-3.5 h-3.5 text-slate-400 dark:text-slate-500 shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2"><path stroke-linecap="round" stroke-linejoin="round" d="M4 4v8a4 4 0 004 4h12M16 12l4 4-4 4"/></svg>`
        : '';
      const parentBadge = s._parentTitle
        ? `<span class="inline-flex items-center gap-1 px-1.5 py-0.5 mt-1 bg-brand-50 dark:bg-brand-900/40 text-brand-700 dark:text-brand-300 text-[10px] font-medium rounded">↳ ${escapeHtml(s._parentTitle)}</span>`
        : '';
      return `
      <tr class="fancy-row">
        <td class="px-4 py-3">
          <div class="flex items-center gap-3" style="padding-left:${indent}px">
            ${branchIcon}
            <div class="row-avatar row-avatar-${(i % 8) + 1}">${escapeHtml((s.title || '?').charAt(0).toUpperCase())}</div>
            <div>
              <div class="font-display font-bold text-slate-900 dark:text-white">${escapeHtml(s.title)}</div>
              <div class="text-xs text-slate-500 dark:text-slate-400 font-mono mt-0.5">/${escapeHtml(s.slug)}</div>
              ${parentBadge}
            </div>
          </div>
        </td>
        <td class="px-4 py-3 hidden md:table-cell">
          <span class="inline-block px-2 py-0.5 bg-slate-100 dark:bg-slate-800 text-slate-700 dark:text-slate-300 text-xs font-mono rounded">${s.sortOrder}</span>
        </td>
        <td class="px-4 py-3 hidden md:table-cell">
          <span class="inline-flex items-center gap-1 px-2 py-0.5 bg-brand-50 dark:bg-brand-900/40 text-brand-700 dark:text-brand-300 text-xs font-medium rounded-full">
            ${s.contentCount} ta
          </span>
          ${s.childrenCount ? `<span class="ml-1 inline-flex items-center gap-1 px-2 py-0.5 bg-warm-50 dark:bg-warm-700/30 text-warm-700 dark:text-warm-300 text-xs font-medium rounded-full">${s.childrenCount} sub</span>` : ''}
        </td>
        <td class="px-4 py-3 hidden md:table-cell">
          ${s.isActive
            ? '<span class="inline-flex items-center gap-1 px-2 py-0.5 bg-emerald-50 dark:bg-emerald-900/40 text-emerald-700 dark:text-emerald-300 text-xs font-medium rounded-full"><span class="w-1.5 h-1.5 rounded-full bg-emerald-500"></span>Faol</span>'
            : '<span class="inline-flex items-center gap-1 px-2 py-0.5 bg-slate-100 dark:bg-slate-800 text-slate-600 dark:text-slate-400 text-xs font-medium rounded-full"><span class="w-1.5 h-1.5 rounded-full bg-slate-400"></span>Yashirin</span>'}
        </td>
        <td class="px-4 py-3 text-right">
          <div class="inline-flex gap-1">
            <button data-id="${s.id}" class="edit-btn p-1.5 text-slate-500 dark:text-slate-400 hover:text-brand-600 hover:bg-brand-50 dark:hover:bg-brand-900/30 rounded-lg transition" title="Tahrirlash">
              <svg xmlns="http://www.w3.org/2000/svg" class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2"><path stroke-linecap="round" stroke-linejoin="round" d="M15.232 5.232l3.536 3.536m-2.036-5.036a2.5 2.5 0 113.536 3.536L6.5 21.036H3v-3.572L16.732 3.732z"/></svg>
            </button>
            <button data-id="${s.id}" data-count="${s.contentCount}" data-children="${s.childrenCount || 0}" class="delete-btn p-1.5 text-slate-500 dark:text-slate-400 hover:text-rose-600 hover:bg-rose-50 dark:hover:bg-rose-900/30 rounded-lg transition" title="O'chirish">
              <svg xmlns="http://www.w3.org/2000/svg" class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2"><path stroke-linecap="round" stroke-linejoin="round" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6M1 7h22M9 7V4a1 1 0 011-1h4a1 1 0 011 1v3"/></svg>
            </button>
          </div>
        </td>
      </tr>
    `;
    }).join('');

    tbody.querySelectorAll('.edit-btn').forEach(b =>
      b.addEventListener('click', () => this.openModal(parseInt(b.dataset.id, 10))));
    tbody.querySelectorAll('.delete-btn').forEach(b =>
      b.addEventListener('click', () => this.deleteRow(
        parseInt(b.dataset.id, 10),
        parseInt(b.dataset.count, 10),
        parseInt(b.dataset.children, 10)
      )));
  },

  /**
   * Build the parent dropdown options. When editing, the section itself and any of its
   * descendants are excluded — assigning a section as its own ancestor would create a cycle.
   */
  populateParentDropdown(currentId = null) {
    const select = document.querySelector('#section-form select[name="parentId"]');
    if (!select) return;

    const blocked = new Set();
    if (currentId != null) {
      blocked.add(currentId);
      // Block all descendants too.
      const queue = [currentId];
      while (queue.length) {
        const pid = queue.shift();
        this.cache.forEach(s => {
          if (s.parentId === pid && !blocked.has(s.id)) {
            blocked.add(s.id);
            queue.push(s.id);
          }
        });
      }
    }

    const ordered = this.flattenHierarchy(this.cache);
    const options = ['<option value="">— Yo\'q (asosiy bo\'lim) —</option>'];
    for (const s of ordered) {
      if (blocked.has(s.id)) continue;
      const indent = '— '.repeat(s._depth || 0);
      options.push(`<option value="${s.id}">${indent}${escapeHtml(s.title)}</option>`);
    }
    select.innerHTML = options.join('');
  },

  async openModal(id = null) {
    const form = document.getElementById('section-form');
    const saveBtn = document.getElementById('section-save-btn');
    form.reset();
    document.getElementById('section-form-error').classList.add('hidden');

    document.getElementById('section-modal-title').textContent = id ? 'Bo\'limni tahrirlash' : 'Yangi bo\'lim';
    form.querySelector('[name="id"]').value = id || '';
    // isActive is now a hidden input that always submits "on" — leave its value alone.
    form.querySelector('[name="sortOrder"]').value = 0;

    this.populateParentDropdown(id);
    this.resetHeadForm();

    document.getElementById('section-modal').classList.remove('hidden');

    if (id) {
      saveBtn.disabled = true;
      try {
        const detail = await Api.getSectionDetail(id);
        Object.entries(detail).forEach(([k, v]) => {
          // Skip isActive — UI no longer exposes it; the form always sends "on".
          if (k === 'isActive') return;
          const el = form.querySelector(`[name="${k}"]`);
          if (!el) return;
          if (el.type === 'checkbox') el.checked = !!v;
          else el.value = v ?? '';
        });
        // Look up the section's existing head (if any) and prefill the sub-form.
        await this.loadHeadForSection(id);
      } catch (err) {
        UI.toast(err.message || 'Bo\'lim yuklanmadi', 'error');
        this.closeModal();
      } finally {
        saveBtn.disabled = false;
      }
    }
  },

  closeModal() {
    document.getElementById('section-modal').classList.add('hidden');
  },

  /**
   * Reset the inline "section head" sub-form to a fresh empty state.
   * Called when opening the modal for a new section, or after the
   * current head reference is no longer relevant.
   */
  resetHeadForm() {
    const form = document.getElementById('section-form');
    if (!form) return;
    ['sectionHeadId', 'sectionHeadPhotoUrl', 'sectionHeadFullName',
     'sectionHeadDepartment', 'sectionHeadPhone', 'sectionHeadEmail',
     'sectionHeadReceptionHours']
      .forEach(name => {
        const el = form.querySelector(`[name="${name}"]`);
        if (el) el.value = '';
      });
    this.currentHead = null;
    this.clearHeadPhoto();
    this.updateHeadStatusBadge(null);
  },

  /**
   * Fetch the SectionHead linked to this section (new dedicated model — separate
   * from LabHead). Prefills the inline sub-form so the section editor can keep
   * a head record in sync without leaving the modal.
   */
  async loadHeadForSection(sectionId) {
    try {
      const list = await Api.request(`/section-heads?onlyActive=false&sectionId=${sectionId}`);
      const head = Array.isArray(list) && list.length > 0 ? list[0] : null;
      if (!head) {
        this.currentHead = null;
        this.updateHeadStatusBadge('none');
        return;
      }
      this.currentHead = head;
      const form = document.getElementById('section-form');
      form.querySelector('[name="sectionHeadId"]').value = head.id;
      form.querySelector('[name="sectionHeadFullName"]').value = head.fullName || '';
      const deptField = form.querySelector('[name="sectionHeadDepartment"]');
      if (deptField) deptField.value = '';
      form.querySelector('[name="sectionHeadPhone"]').value = head.phone || '';
      const emailInput = form.querySelector('[name="sectionHeadEmail"]');
      if (emailInput) emailInput.value = head.email || '';
      form.querySelector('[name="sectionHeadReceptionHours"]').value = head.workingHours || '';
      if (head.photoUrl) this.setHeadPhoto(head.photoUrl);
      this.updateHeadStatusBadge('linked');
    } catch (err) {
      console.warn('Section head lookup failed:', err.message);
      this.currentHead = null;
      this.updateHeadStatusBadge(null);
    }
  },

  updateHeadStatusBadge(state) {
    const badge = document.getElementById('sh-status-badge');
    if (!badge) return;
    badge.classList.add('hidden');
    badge.classList.remove('bg-emerald-50', 'text-emerald-700', 'bg-slate-100', 'text-slate-600',
      'dark:bg-emerald-900/40', 'dark:text-emerald-300', 'dark:bg-slate-800', 'dark:text-slate-400');
    if (state === 'linked') {
      badge.textContent = 'Biriktirilgan';
      badge.classList.remove('hidden');
      badge.classList.add('bg-emerald-50', 'text-emerald-700', 'dark:bg-emerald-900/40', 'dark:text-emerald-300');
    } else if (state === 'none') {
      badge.textContent = 'Yo\'q';
      badge.classList.remove('hidden');
      badge.classList.add('bg-slate-100', 'text-slate-600', 'dark:bg-slate-800', 'dark:text-slate-400');
    }
  },

  setHeadPhoto(url) {
    const form = document.getElementById('section-form');
    const preview = document.getElementById('sh-photo-preview');
    const removeBtn = document.getElementById('sh-photo-remove');
    if (!form || !preview) return;
    form.querySelector('[name="sectionHeadPhotoUrl"]').value = url;
    preview.innerHTML = `<img src="${escapeHtml(url)}" alt="" class="w-full h-full object-cover" />`;
    if (removeBtn) removeBtn.classList.remove('hidden');
  },

  clearHeadPhoto() {
    const form = document.getElementById('section-form');
    const preview = document.getElementById('sh-photo-preview');
    const removeBtn = document.getElementById('sh-photo-remove');
    const input = document.getElementById('sh-photo-input');
    if (form) {
      const f = form.querySelector('[name="sectionHeadPhotoUrl"]');
      if (f) f.value = '';
    }
    if (preview) preview.innerHTML = '<span>Rasm yo\'q</span>';
    if (removeBtn) removeBtn.classList.add('hidden');
    if (input) input.value = '';
  },

  async handleHeadPhotoUpload(e) {
    const file = e.target.files?.[0];
    if (!file) return;
    const pickBtn = document.getElementById('sh-photo-pick');
    const original = pickBtn ? pickBtn.innerHTML : '';
    if (pickBtn) {
      pickBtn.disabled = true;
      pickBtn.innerHTML = '<svg class="animate-spin h-3.5 w-3.5" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24"><circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle><path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v4a4 4 0 00-4 4H4z"></path></svg>';
    }
    try {
      const dto = await Api.uploadMedia(file);
      this.setHeadPhoto(dto.url);
      UI.toast('Rasm yuklandi', 'success');
    } catch (err) {
      UI.toast(err.message || 'Rasm yuklanmadi', 'error');
    } finally {
      if (pickBtn) {
        pickBtn.disabled = false;
        pickBtn.innerHTML = original;
      }
      e.target.value = '';
    }
  },

  async save() {
    const form = document.getElementById('section-form');
    const errorBox = document.getElementById('section-form-error');
    const btn = document.getElementById('section-save-btn');
    const btnText = btn.querySelector('.save-btn-text');
    const btnSpinner = btn.querySelector('.save-btn-spinner');
    errorBox.classList.add('hidden');

    if (!form.checkValidity()) { form.reportValidity(); return; }

    const fd = new FormData(form);
    const id = fd.get('id');
    const parentRaw = (fd.get('parentId') || '').toString().trim();
    const payload = {
      slug: (fd.get('slug') || '').trim() || null,
      titleUz: fd.get('titleUz').trim(),
      titleRu: fd.get('titleRu').trim(),
      titleEn: fd.get('titleEn').trim(),
      sortOrder: parseInt(fd.get('sortOrder'), 10) || 0,
      isActive: fd.get('isActive') === 'on',
      parentId: parentRaw === '' ? null : parseInt(parentRaw, 10)
    };

    // Sub-form: optional section head. Only acted on if the full name is provided
    // (or if there's an existing linked head being explicitly cleared).
    const headFullName = (fd.get('sectionHeadFullName') || '').toString().trim();
    const headDepartment = (fd.get('sectionHeadDepartment') || '').toString().trim();
    const headPhone = (fd.get('sectionHeadPhone') || '').toString().trim();
    const headEmail = (fd.get('sectionHeadEmail') || '').toString().trim();
    const headHours = (fd.get('sectionHeadReceptionHours') || '').toString().trim();
    const headPhoto = (fd.get('sectionHeadPhotoUrl') || '').toString().trim();
    const existingHeadId = (fd.get('sectionHeadId') || '').toString().trim();

    btn.disabled = true;
    btnText.textContent = 'Saqlanmoqda...';
    btnSpinner.classList.remove('hidden');

    try {
      let savedSectionId = id ? parseInt(id, 10) : null;
      if (id) {
        await Api.updateSection(savedSectionId, payload);
      } else {
        // Try to use the create response (most REST APIs return the created entity);
        // if it doesn't carry an id, fall back to looking up by slug in the fresh list.
        const created = await Api.createSection(payload);
        if (created && typeof created.id === 'number') {
          savedSectionId = created.id;
        } else {
          const fresh = await Api.listSections({ onlyActive: false });
          const match = fresh.find(s => s.slug === payload.slug || s.title === payload.titleUz);
          savedSectionId = match ? match.id : null;
        }
      }

      // Now reconcile the section head sub-form against this section.
      if (savedSectionId != null) {
        await this.persistHeadFor(savedSectionId, {
          fullName: headFullName,
          department: headDepartment,
          phone: headPhone,
          email: headEmail,
          receptionHours: headHours,
          photoUrl: headPhoto,
          existingId: existingHeadId ? parseInt(existingHeadId, 10) : null
        });
      }

      UI.toast(id ? 'Bo\'lim yangilandi' : 'Yangi bo\'lim qo\'shildi', 'success');
      this.closeModal();
      this.load();
      Contents.refreshSectionDropdown?.();
      LabHeads?.load?.();
    } catch (err) {
      errorBox.textContent = err.formatErrors ? err.formatErrors() : (err.message || 'Saqlab bo\'lmadi');
      errorBox.classList.remove('hidden');
    } finally {
      btn.disabled = false;
      btnText.textContent = 'Saqlash';
      btnSpinner.classList.add('hidden');
    }
  },

  /**
   * Reconcile the section's SectionHead record with what the sub-form contains:
   *   - fullName empty + existingId  → delete the linked head
   *   - fullName empty + no existing → no-op
   *   - fullName set   + no existing → create
   *   - fullName set   + existingId  → update
   * Errors here are surfaced as toasts (the section itself is already saved).
   * Note: the sub-form's "ReceptionHours" field maps to SectionHead.WorkingHours.
   */
  async persistHeadFor(sectionId, h) {
    const hasFullName = !!h.fullName;
    if (!hasFullName && !h.existingId) return;

    if (!hasFullName && h.existingId) {
      try {
        await Api.deleteSectionHead(h.existingId);
        UI.toast('Bo\'lim raxbari biriktirilishi olib tashlandi', 'info');
      } catch (err) {
        UI.toast('Raxbarni olib tashlab bo\'lmadi: ' + (err.message || ''), 'error');
      }
      return;
    }

    const payload = {
      fullName: h.fullName,
      phone: h.phone || null,
      email: h.email || null,
      workingHours: h.receptionHours || null,
      photoUrl: h.photoUrl || null,
      sectionId: sectionId,
      sortOrder: 0,
      isActive: true
    };
    try {
      if (h.existingId) await Api.updateSectionHead(h.existingId, payload);
      else await Api.createSectionHead(payload);
      // Refresh the dedicated tab if it's currently mounted.
      if (typeof SectionHeads !== 'undefined') SectionHeads.load?.();
    } catch (err) {
      UI.toast('Bo\'lim raxbarini saqlab bo\'lmadi: ' + (err.formatErrors ? err.formatErrors() : err.message), 'error');
    }
  },

  async deleteRow(id, count, childrenCount = 0) {
    if (childrenCount > 0) {
      await UI.confirm({
        title: 'O\'chirib bo\'lmaydi',
        message: `Bu bo'limning ${childrenCount} ta ichki bo'limi bor. Avval ularni o'chiring yoki boshqa bo'limga ko'chiring.`,
        okText: 'Tushunarli'
      });
      return;
    }

    const message = count > 0
      ? `Bu bo'lim ichida ${count} ta kontent bor. O'chirsangiz, ular ham birgalikda o'chiriladi. Davom etamizmi?`
      : 'Ushbu bo\'limni o\'chirmoqchimisiz?';

    const ok = await UI.confirm({
      title: 'Bo\'limni o\'chirish',
      message,
      okText: 'Ha, o\'chirish'
    });
    if (!ok) return;

    try {
      await Api.deleteSection(id);
      UI.toast('Bo\'lim o\'chirildi', 'success');
      this.load();
      Contents.refreshSectionDropdown?.();
    } catch (err) {
      UI.toast(err.formatErrors ? err.formatErrors() : err.message, 'error');
    }
  }
};
