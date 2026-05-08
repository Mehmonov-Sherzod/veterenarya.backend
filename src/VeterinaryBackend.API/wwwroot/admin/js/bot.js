// Admin: Bot xabarlari (Telegram inbox)
// — Lists incoming bot messages
// — Mark as read / delete
// — Shows unread + subscriber stats and updates the sidebar badge
const Bot = {
  state: {
    onlyUnread: false,
    polling: null
  },

  init() {
    document.getElementById('bot-only-unread')?.addEventListener('change', (e) => {
      this.state.onlyUnread = e.target.checked;
      this.load();
    });
    document.getElementById('bot-refresh')?.addEventListener('click', () => this.load());

    // Refresh badge on a slow interval so admin sees new pings without manually opening the tab.
    if (this.state.polling) clearInterval(this.state.polling);
    this.state.polling = setInterval(() => this.refreshStats(), 30000);

    // Initial badge fetch (after login).
    setTimeout(() => this.refreshStats(), 800);
  },

  async load() {
    const tbody = document.getElementById('bot-tbody');
    if (!tbody) return;
    tbody.innerHTML = `<tr><td colspan="4" class="px-4 py-12 text-center text-slate-400 dark:text-slate-500">Yuklanmoqda...</td></tr>`;

    try {
      const qs = this.state.onlyUnread ? '?onlyUnread=true' : '';
      const rows = await Api.request(`/Bot/messages${qs}`);
      this.renderRows(rows);
      this.refreshStats();
    } catch (err) {
      tbody.innerHTML = `<tr><td colspan="4" class="px-4 py-12 text-center text-rose-500">Xato: ${this.escape(err?.message || err)}</td></tr>`;
    }
  },

  renderRows(rows) {
    const tbody = document.getElementById('bot-tbody');
    if (!tbody) return;

    if (!rows || rows.length === 0) {
      tbody.innerHTML = `<tr><td colspan="4" class="px-4 py-12 text-center text-slate-400 dark:text-slate-500">Hozircha xabarlar yo'q</td></tr>`;
      return;
    }

    tbody.innerHTML = rows.map(m => {
      const contactName = m.contactName ? this.escape(m.contactName) : (m.firstName ? this.escape(m.firstName) : '—');
      const tg = m.username ? `@${this.escape(m.username)}` : '';
      const phone = m.phone ? this.escape(m.phone) : '';
      const date = new Date(m.createdAt).toLocaleString('uz-UZ', { dateStyle: 'short', timeStyle: 'short' });
      const unreadDot = m.isRead ? '' : '<span class="inline-block w-2 h-2 rounded-full bg-rose-500 mr-2 align-middle"></span>';
      const statusBadge = this.renderStatusBadge(m.status);
      return `
        <tr data-id="${m.id}" data-chat="${m.chatId}" class="${m.isRead ? '' : 'bg-rose-50/40 dark:bg-rose-900/10'}">
          <td class="px-4 py-3 align-top">
            <div class="font-semibold text-slate-900 dark:text-white">${unreadDot}${contactName}</div>
            ${phone ? `<div class="text-xs text-emerald-600 dark:text-emerald-400 font-medium">📞 ${phone}</div>` : ''}
            ${tg ? `<div class="text-xs text-slate-500">${tg}</div>` : ''}
            <div class="text-[10px] text-slate-400 mt-0.5">chat ${m.chatId}</div>
          </td>
          <td class="px-4 py-3 align-top whitespace-pre-wrap text-slate-700 dark:text-slate-300 max-w-2xl">
            ${this.escape(m.text)}
            <div class="mt-2 flex items-center gap-2 flex-wrap">
              ${statusBadge}
              <select class="bot-status-select text-xs rounded-md border border-slate-200 dark:border-slate-700 bg-white dark:bg-slate-800 text-slate-700 dark:text-slate-300 px-2 py-1">
                <option value="0" ${m.status === 0 ? 'selected' : ''}>📥 Qabul qilingan</option>
                <option value="1" ${m.status === 1 ? 'selected' : ''}>⏳ Jarayonda</option>
                <option value="2" ${m.status === 2 ? 'selected' : ''}>✅ Muvaffaqiyatli</option>
                <option value="3" ${m.status === 3 ? 'selected' : ''}>❌ Rad etilgan</option>
              </select>
            </div>
          </td>
          <td class="px-4 py-3 hidden md:table-cell align-top text-xs text-slate-500">${date}</td>
          <td class="px-4 py-3 text-right align-top whitespace-nowrap">
            <button class="bot-reply text-brand-600 hover:text-brand-700 mr-2" title="Javob berish">
              <svg xmlns="http://www.w3.org/2000/svg" class="w-5 h-5 inline" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2"><path stroke-linecap="round" stroke-linejoin="round" d="M3 10h10a8 8 0 018 8v2M3 10l6 6m-6-6l6-6"/></svg>
            </button>
            ${m.isRead ? '' : `<button class="bot-mark-read text-emerald-600 hover:text-emerald-700 mr-2" title="O'qildi deb belgilash">
              <svg xmlns="http://www.w3.org/2000/svg" class="w-5 h-5 inline" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2"><path stroke-linecap="round" stroke-linejoin="round" d="M5 13l4 4L19 7"/></svg>
            </button>`}
            <button class="bot-delete text-rose-500 hover:text-rose-700" title="O'chirish">
              <svg xmlns="http://www.w3.org/2000/svg" class="w-5 h-5 inline" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2"><path stroke-linecap="round" stroke-linejoin="round" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6M1 7h22M9 7V4a1 1 0 011-1h4a1 1 0 011 1v3"/></svg>
            </button>
          </td>
        </tr>
      `;
    }).join('');

    tbody.querySelectorAll('.bot-status-select').forEach(sel => sel.addEventListener('change', (e) => {
      const id = parseInt(e.currentTarget.closest('tr').dataset.id, 10);
      this.changeStatus(id, parseInt(e.currentTarget.value, 10));
    }));

    tbody.querySelectorAll('.bot-reply').forEach(btn => btn.addEventListener('click', (e) => {
      const tr = e.currentTarget.closest('tr');
      this.openReply(parseInt(tr.dataset.id, 10), tr);
    }));
    tbody.querySelectorAll('.bot-mark-read').forEach(btn => btn.addEventListener('click', (e) => {
      const id = e.currentTarget.closest('tr').dataset.id;
      this.markRead(parseInt(id, 10));
    }));
    tbody.querySelectorAll('.bot-delete').forEach(btn => btn.addEventListener('click', (e) => {
      const id = e.currentTarget.closest('tr').dataset.id;
      this.delete(parseInt(id, 10));
    }));
  },

  openReply(id, tr) {
    const userText = tr.querySelector('td:nth-child(2)').textContent.trim().slice(0, 200);
    const replyText = window.prompt(`Foydalanuvchining xabari:\n"${userText}"\n\nJavobingizni yozing:`);
    if (replyText === null) return;
    const trimmed = replyText.trim();
    if (!trimmed) { alert('Javob matni bo\'sh bo\'lishi mumkin emas.'); return; }
    this.sendReply(id, trimmed);
  },

  async sendReply(id, text) {
    try {
      await Api.request(`/Bot/messages/${id}/reply`, { method: 'POST', body: { text } });
      this.load();
    } catch (err) {
      alert('Xato: ' + (err?.message || err));
    }
  },

  async changeStatus(id, status) {
    try {
      await Api.request(`/Bot/messages/${id}/status`, { method: 'PATCH', body: { status } });
      this.load();
    } catch (err) {
      alert('Xato: ' + (err?.message || err));
      this.load();
    }
  },

  renderStatusBadge(status) {
    const map = {
      0: { label: '📥 Qabul qilingan', cls: 'bg-slate-100 text-slate-700 dark:bg-slate-800 dark:text-slate-300' },
      1: { label: '⏳ Jarayonda',       cls: 'bg-amber-100 text-amber-700 dark:bg-amber-900/30 dark:text-amber-300' },
      2: { label: '✅ Muvaffaqiyatli', cls: 'bg-emerald-100 text-emerald-700 dark:bg-emerald-900/30 dark:text-emerald-300' },
      3: { label: '❌ Rad etilgan',    cls: 'bg-rose-100 text-rose-700 dark:bg-rose-900/30 dark:text-rose-300' }
    };
    const s = map[status] ?? map[0];
    return `<span class="inline-flex items-center gap-1 text-xs font-semibold px-2 py-1 rounded-full ${s.cls}">${s.label}</span>`;
  },

  async markRead(id) {
    try {
      await Api.request(`/Bot/messages/${id}/read`, { method: 'PATCH' });
      this.load();
    } catch (err) {
      alert('Xato: ' + (err?.message || err));
    }
  },

  async delete(id) {
    if (!confirm("Xabarni o'chirishni tasdiqlaysizmi?")) return;
    try {
      await Api.request(`/Bot/messages/${id}`, { method: 'DELETE' });
      this.load();
    } catch (err) {
      alert('Xato: ' + (err?.message || err));
    }
  },

  async refreshStats() {
    if (!Api.isAuthenticated()) return;
    try {
      const stats = await Api.request('/Bot/stats');
      const unreadEl = document.querySelector('[data-stat="bot-unread"]');
      const subEl = document.querySelector('[data-stat="bot-subscribers"]');
      if (unreadEl) unreadEl.textContent = stats.unreadCount ?? 0;
      if (subEl) subEl.textContent = stats.subscriberCount ?? 0;

      const badge = document.getElementById('bot-unread-badge');
      if (badge) {
        if (stats.unreadCount > 0) {
          badge.textContent = stats.unreadCount > 99 ? '99+' : stats.unreadCount;
          badge.classList.remove('hidden');
        } else {
          badge.classList.add('hidden');
        }
      }
    } catch { /* ignore — token may have expired */ }
  },

  escape(s) {
    return String(s ?? '').replace(/[&<>"']/g, c => ({ '&':'&amp;', '<':'&lt;', '>':'&gt;', '"':'&quot;', "'":'&#39;' }[c]));
  }
};
