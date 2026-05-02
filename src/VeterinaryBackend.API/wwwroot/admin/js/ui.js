// Toast + modal + helper UI primitives.
const UI = {
  toast(message, type = 'info', duration = 3500) {
    const container = document.getElementById('toast-container');
    const div = document.createElement('div');
    div.className = `toast toast-${type}`;

    const icons = {
      success: `<svg xmlns="http://www.w3.org/2000/svg" class="w-5 h-5 text-emerald-500 flex-shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2"><path stroke-linecap="round" stroke-linejoin="round" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z"/></svg>`,
      error:   `<svg xmlns="http://www.w3.org/2000/svg" class="w-5 h-5 text-rose-500 flex-shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2"><path stroke-linecap="round" stroke-linejoin="round" d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"/></svg>`,
      info:    `<svg xmlns="http://www.w3.org/2000/svg" class="w-5 h-5 text-blue-500 flex-shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2"><path stroke-linecap="round" stroke-linejoin="round" d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"/></svg>`
    };

    div.innerHTML = `
      ${icons[type] || icons.info}
      <div class="text-sm text-slate-700 flex-1 whitespace-pre-line">${escapeHtml(message)}</div>
      <button class="text-slate-400 hover:text-slate-600">
        <svg xmlns="http://www.w3.org/2000/svg" class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
          <path stroke-linecap="round" stroke-linejoin="round" d="M6 18L18 6M6 6l12 12"/>
        </svg>
      </button>
    `;

    const remove = () => {
      div.classList.add('removing');
      setTimeout(() => div.remove(), 150);
    };
    div.querySelector('button').addEventListener('click', remove);
    container.appendChild(div);
    setTimeout(remove, duration);
  },

  confirm({ title = 'Tasdiqlash', message = 'Davom etishni xohlaysizmi?', okText = "O'chirish", okClass = 'bg-rose-600 hover:bg-rose-700' }) {
    return new Promise(resolve => {
      const modal = document.getElementById('confirm-modal');
      document.getElementById('confirm-title').textContent = title;
      document.getElementById('confirm-message').textContent = message;
      const okBtn = document.getElementById('confirm-ok');
      const cancelBtn = document.getElementById('confirm-cancel');
      okBtn.textContent = okText;
      okBtn.className = `px-4 py-2 text-sm text-white rounded-lg ${okClass}`;

      const cleanup = (result) => {
        modal.classList.add('hidden');
        okBtn.removeEventListener('click', onOk);
        cancelBtn.removeEventListener('click', onCancel);
        modal.removeEventListener('click', onBackdrop);
        document.removeEventListener('keydown', onKey);
        resolve(result);
      };
      const onOk = () => cleanup(true);
      const onCancel = () => cleanup(false);
      const onBackdrop = (e) => { if (e.target === modal) cleanup(false); };
      const onKey = (e) => { if (e.key === 'Escape') cleanup(false); };

      okBtn.addEventListener('click', onOk);
      cancelBtn.addEventListener('click', onCancel);
      modal.addEventListener('click', onBackdrop);
      document.addEventListener('keydown', onKey);

      modal.classList.remove('hidden');
    });
  },

  formatBytes(bytes) {
    if (bytes < 1024) return bytes + ' B';
    if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(1) + ' KB';
    if (bytes < 1024 * 1024 * 1024) return (bytes / (1024 * 1024)).toFixed(1) + ' MB';
    return (bytes / (1024 * 1024 * 1024)).toFixed(2) + ' GB';
  },

  formatDate(iso) {
    if (!iso) return '—';
    const d = new Date(iso);
    return d.toLocaleString('ru-RU', { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit' });
  },

  isImage(contentType) {
    return contentType && contentType.startsWith('image/');
  },

  fileIconSvg(contentType, extension) {
    const ext = (extension || '').toLowerCase().replace('.', '');
    const ct = (contentType || '').toLowerCase();
    let cls = 'file-icon-default';
    if (ct.includes('pdf')) cls = 'file-icon-pdf';
    else if (ct.includes('word') || ['doc','docx','rtf','odt'].includes(ext)) cls = 'file-icon-doc';
    else if (ct.includes('sheet') || ct.includes('excel') || ['xls','xlsx','csv','ods'].includes(ext)) cls = 'file-icon-xls';
    else if (ct.includes('zip') || ['zip','rar','7z','tar','gz'].includes(ext)) cls = 'file-icon-zip';
    else if (ct.startsWith('video/')) cls = 'file-icon-video';
    else if (ct.startsWith('audio/')) cls = 'file-icon-audio';

    return `
      <svg xmlns="http://www.w3.org/2000/svg" class="w-12 h-12 ${cls}" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="1.5">
        <path stroke-linecap="round" stroke-linejoin="round" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"/>
      </svg>
      <div class="text-[10px] uppercase font-bold ${cls} mt-1">${ext || 'file'}</div>
    `;
  }
};

function escapeHtml(s) {
  if (s === null || s === undefined) return '';
  return String(s)
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&#039;');
}

function copyToClipboard(text) {
  if (navigator.clipboard) {
    navigator.clipboard.writeText(text).then(() => UI.toast('URL nusxalandi', 'success', 2000));
  } else {
    const ta = document.createElement('textarea');
    ta.value = text;
    document.body.appendChild(ta);
    ta.select();
    document.execCommand('copy');
    ta.remove();
    UI.toast('URL nusxalandi', 'success', 2000);
  }
}
