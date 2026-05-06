// Lab heads tab — list, create, edit, delete laboratory department heads.
const LabHeads = {
  cache: [],

  init() {
    document.getElementById('new-lab-head-btn').addEventListener('click', () => this.openModal());
    document.querySelectorAll('#lab-head-modal .lh-close').forEach(b =>
      b.addEventListener('click', () => this.closeModal()));
    document.getElementById('lab-head-modal').addEventListener('click', (e) => {
      if (e.target.id === 'lab-head-modal') this.closeModal();
    });
    document.getElementById('lab-head-save-btn').addEventListener('click', () => this.save());

    document.getElementById('lh-photo-pick').addEventListener('click', () =>
      document.getElementById('lh-photo-input').click());
    document.getElementById('lh-photo-input').addEventListener('change', (e) => this.handlePhotoUpload(e));
    document.getElementById('lh-photo-remove').addEventListener('click', () => this.clearPhoto());
  },

  async load() {
    const tbody = document.getElementById('lab-heads-tbody');
    tbody.innerHTML = `<tr><td colspan="7" class="px-4 py-12 text-center text-slate-400">
      <svg class="inline animate-spin h-5 w-5 mr-2" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
        <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
        <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v4a4 4 0 00-4 4H4z"></path>
      </svg>Yuklanmoqda...</td></tr>`;
    try {
      const list = await Api.listLabHeads({ onlyActive: false });
      this.cache = list;
      this.renderRows(list);
    } catch (err) {
      tbody.innerHTML = `<tr><td colspan="7" class="px-4 py-12 text-center text-rose-500">${escapeHtml(err.message)}</td></tr>`;
    }
  },

  renderRows(items) {
    const tbody = document.getElementById('lab-heads-tbody');
    if (!items.length) {
      tbody.innerHTML = `<tr><td colspan="7" class="px-4 py-16 text-center">
        <div class="text-4xl mb-2">👥</div>
        <h3 class="font-display text-xl font-bold text-slate-900 dark:text-white mb-1">Hali boshliq qo'shilmagan</h3>
        <p class="text-sm text-slate-500 dark:text-slate-400 mb-4">"Yangi boshliq qo'shish" tugmasini bosing va birinchi rahbariyat ma'lumotini kiriting.</p>
      </td></tr>`;
      return;
    }

    const initials = (name) => (name || '?').trim().split(/\s+/).map(w => w[0] || '').slice(0, 2).join('').toUpperCase();

    tbody.innerHTML = items.map((h, i) => {
      const photoCell = h.photoUrl
        ? `<img src="${escapeHtml(h.photoUrl)}" alt="" class="w-12 h-12 rounded-lg object-cover" onerror="this.outerHTML='<div class=\\'row-avatar row-avatar-${(i % 8) + 1}\\'>${escapeHtml(initials(h.fullName))}</div>'" />`
        : `<div class="row-avatar row-avatar-${(i % 8) + 1}">${escapeHtml(initials(h.fullName))}</div>`;
      return `
        <tr class="fancy-row">
          <td class="px-4 py-3">${photoCell}</td>
          <td class="px-4 py-3">
            <div class="font-display font-bold text-slate-900 dark:text-white">${escapeHtml(h.fullName)}</div>
            ${h.department ? `<div class="text-xs text-slate-500 dark:text-slate-400 mt-0.5">${escapeHtml(h.department)}</div>` : ''}
          </td>
          <td class="px-4 py-3 hidden md:table-cell text-sm text-slate-700 dark:text-slate-300 font-mono">${escapeHtml(h.phone)}</td>
          <td class="px-4 py-3 hidden lg:table-cell text-sm text-slate-700 dark:text-slate-300">${escapeHtml(h.receptionHours)}</td>
          <td class="px-4 py-3 hidden md:table-cell">
            <span class="inline-block px-2 py-0.5 bg-slate-100 dark:bg-slate-800 text-slate-700 dark:text-slate-300 text-xs font-mono rounded">${h.sortOrder}</span>
          </td>
          <td class="px-4 py-3 hidden md:table-cell">
            ${h.isActive
              ? '<span class="inline-flex items-center gap-1 px-2 py-0.5 bg-emerald-50 dark:bg-emerald-900/40 text-emerald-700 dark:text-emerald-300 text-xs font-medium rounded-full"><span class="w-1.5 h-1.5 rounded-full bg-emerald-500"></span>Faol</span>'
              : '<span class="inline-flex items-center gap-1 px-2 py-0.5 bg-slate-100 dark:bg-slate-800 text-slate-600 dark:text-slate-400 text-xs font-medium rounded-full"><span class="w-1.5 h-1.5 rounded-full bg-slate-400"></span>Yashirin</span>'}
          </td>
          <td class="px-4 py-3 text-right">
            <div class="inline-flex gap-1">
              <button data-id="${h.id}" class="edit-btn p-1.5 text-slate-500 dark:text-slate-400 hover:text-brand-600 hover:bg-brand-50 dark:hover:bg-brand-900/30 rounded-lg transition" title="Tahrirlash">
                <svg xmlns="http://www.w3.org/2000/svg" class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2"><path stroke-linecap="round" stroke-linejoin="round" d="M15.232 5.232l3.536 3.536m-2.036-5.036a2.5 2.5 0 113.536 3.536L6.5 21.036H3v-3.572L16.732 3.732z"/></svg>
              </button>
              <button data-id="${h.id}" class="delete-btn p-1.5 text-slate-500 dark:text-slate-400 hover:text-rose-600 hover:bg-rose-50 dark:hover:bg-rose-900/30 rounded-lg transition" title="O'chirish">
                <svg xmlns="http://www.w3.org/2000/svg" class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2"><path stroke-linecap="round" stroke-linejoin="round" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6M1 7h22M9 7V4a1 1 0 011-1h4a1 1 0 011 1v3"/></svg>
              </button>
            </div>
          </td>
        </tr>`;
    }).join('');

    tbody.querySelectorAll('.edit-btn').forEach(b =>
      b.addEventListener('click', () => this.openModal(parseInt(b.dataset.id, 10))));
    tbody.querySelectorAll('.delete-btn').forEach(b =>
      b.addEventListener('click', () => this.deleteRow(parseInt(b.dataset.id, 10))));
  },

  async openModal(id = null) {
    const form = document.getElementById('lab-head-form');
    const saveBtn = document.getElementById('lab-head-save-btn');
    form.reset();
    document.getElementById('lab-head-form-error').classList.add('hidden');
    this.clearPhoto();

    document.getElementById('lab-head-modal-title').textContent = id ? 'Boshliqni tahrirlash' : 'Yangi boshliq';
    form.querySelector('[name="id"]').value = id || '';
    form.querySelector('[name="sortOrder"]').value = 0;
    form.querySelector('[name="isActive"]').checked = true;

    document.getElementById('lab-head-modal').classList.remove('hidden');

    if (id) {
      saveBtn.disabled = true;
      try {
        const detail = await Api.getLabHead(id);
        form.querySelector('[name="fullName"]').value = detail.fullName || '';
        form.querySelector('[name="department"]').value = detail.department || '';
        form.querySelector('[name="phone"]').value = detail.phone || '';
        form.querySelector('[name="receptionHours"]').value = detail.receptionHours || '';
        form.querySelector('[name="sortOrder"]').value = detail.sortOrder ?? 0;
        form.querySelector('[name="isActive"]').checked = !!detail.isActive;
        if (detail.photoUrl) this.setPhoto(detail.photoUrl);
      } catch (err) {
        UI.toast(err.message || 'Boshliq yuklanmadi', 'error');
        this.closeModal();
      } finally {
        saveBtn.disabled = false;
      }
    }
  },

  closeModal() {
    document.getElementById('lab-head-modal').classList.add('hidden');
  },

  setPhoto(url) {
    const form = document.getElementById('lab-head-form');
    const preview = document.getElementById('lh-photo-preview');
    const removeBtn = document.getElementById('lh-photo-remove');
    form.querySelector('[name="photoUrl"]').value = url;
    preview.innerHTML = `<img src="${url}" alt="" class="w-full h-full object-cover" />`;
    removeBtn.classList.remove('hidden');
  },

  clearPhoto() {
    const form = document.getElementById('lab-head-form');
    const preview = document.getElementById('lh-photo-preview');
    const removeBtn = document.getElementById('lh-photo-remove');
    const input = document.getElementById('lh-photo-input');
    form.querySelector('[name="photoUrl"]').value = '';
    preview.innerHTML = '<span>Rasm yo\'q</span>';
    removeBtn.classList.add('hidden');
    input.value = '';
  },

  async handlePhotoUpload(e) {
    const file = e.target.files?.[0];
    if (!file) return;

    const pickBtn = document.getElementById('lh-photo-pick');
    const original = pickBtn.innerHTML;
    pickBtn.disabled = true;
    pickBtn.innerHTML = '<svg class="animate-spin h-4 w-4" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24"><circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle><path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v4a4 4 0 00-4 4H4z"></path></svg>Yuklanmoqda...';

    try {
      const dto = await Api.uploadMedia(file);
      this.setPhoto(dto.url);
      UI.toast('Rasm yuklandi', 'success');
    } catch (err) {
      UI.toast(err.message || 'Rasm yuklanmadi', 'error');
    } finally {
      pickBtn.disabled = false;
      pickBtn.innerHTML = original;
      e.target.value = '';
    }
  },

  async save() {
    const form = document.getElementById('lab-head-form');
    const errorBox = document.getElementById('lab-head-form-error');
    const btn = document.getElementById('lab-head-save-btn');
    const btnText = btn.querySelector('.save-btn-text');
    const btnSpinner = btn.querySelector('.save-btn-spinner');
    errorBox.classList.add('hidden');

    if (!form.checkValidity()) { form.reportValidity(); return; }

    const fd = new FormData(form);
    const id = fd.get('id');
    const photoUrl = (fd.get('photoUrl') || '').trim();
    const department = (fd.get('department') || '').trim();
    const payload = {
      fullName: fd.get('fullName').trim(),
      phone: fd.get('phone').trim(),
      receptionHours: fd.get('receptionHours').trim(),
      photoUrl: photoUrl || null,
      department: department || null,
      sortOrder: parseInt(fd.get('sortOrder'), 10) || 0,
      isActive: fd.get('isActive') === 'on'
    };

    btn.disabled = true;
    btnText.textContent = 'Saqlanmoqda...';
    btnSpinner.classList.remove('hidden');

    try {
      if (id) {
        await Api.updateLabHead(parseInt(id, 10), payload);
        UI.toast('Boshliq yangilandi', 'success');
      } else {
        await Api.createLabHead(payload);
        UI.toast('Yangi boshliq qo\'shildi', 'success');
      }
      this.closeModal();
      this.load();
    } catch (err) {
      errorBox.textContent = err.formatErrors();
      errorBox.classList.remove('hidden');
    } finally {
      btn.disabled = false;
      btnText.textContent = 'Saqlash';
      btnSpinner.classList.add('hidden');
    }
  },

  async deleteRow(id) {
    const ok = await UI.confirm({
      title: 'Boshliqni o\'chirish',
      message: 'Ushbu boshliqni o\'chirmoqchimisiz?',
      okText: 'Ha, o\'chirish'
    });
    if (!ok) return;

    try {
      await Api.deleteLabHead(id);
      UI.toast('Boshliq o\'chirildi', 'success');
      this.load();
    } catch (err) {
      UI.toast(err.message, 'error');
    }
  }
};
