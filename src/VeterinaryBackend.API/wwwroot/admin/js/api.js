// HTTP client with JWT auth interceptor.
const API_BASE = '/api/v1';
const TOKEN_KEY = 'admin_token';
const TOKEN_EXP_KEY = 'admin_token_exp';
const USERNAME_KEY = 'admin_username';

const Api = {
  getToken() {
    return localStorage.getItem(TOKEN_KEY);
  },
  setSession(payload) {
    localStorage.setItem(TOKEN_KEY, payload.accessToken);
    localStorage.setItem(TOKEN_EXP_KEY, payload.expiresAt);
    localStorage.setItem(USERNAME_KEY, payload.username || 'admin');
  },
  clearSession() {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(TOKEN_EXP_KEY);
    localStorage.removeItem(USERNAME_KEY);
  },
  getUsername() {
    return localStorage.getItem(USERNAME_KEY) || 'admin';
  },
  isAuthenticated() {
    const token = this.getToken();
    const exp = localStorage.getItem(TOKEN_EXP_KEY);
    if (!token) return false;
    if (exp && new Date(exp) < new Date()) {
      this.clearSession();
      return false;
    }
    return true;
  },

  async request(path, { method = 'GET', body, headers = {} } = {}) {
    const token = this.getToken();
    const finalHeaders = { ...headers };
    let finalBody = body;

    if (token) finalHeaders['Authorization'] = `Bearer ${token}`;
    if (body && !(body instanceof FormData)) {
      finalHeaders['Content-Type'] = 'application/json';
      finalBody = JSON.stringify(body);
    }

    let res;
    try {
      res = await fetch(`${API_BASE}${path}`, {
        method,
        headers: finalHeaders,
        body: finalBody
      });
    } catch (networkErr) {
      throw new ApiError({
        status: 0,
        title: 'Server bilan ulanib bo\'lmadi. Backend ishlayotganini tekshiring.'
      });
    }

    // Auth/Config endpoints: 401 means business-level error (wrong credentials, etc),
    // not session expiry. For other endpoints, 401 = token rejected → clear session.
    const isPublicEndpoint = path.startsWith('/auth/') || path.startsWith('/config/');
    if (res.status === 401 && !isPublicEndpoint) {
      this.clearSession();
      window.dispatchEvent(new CustomEvent('auth:expired'));
    }

    if (res.status === 204) return null;

    const text = await res.text();
    let data = null;
    if (text) {
      try { data = JSON.parse(text); } catch { /* not JSON, leave null */ }
    }

    if (!res.ok) {
      throw new ApiError(data || { status: res.status, title: res.statusText || 'Request failed' });
    }
    return data;
  },

  // Auth
  login(username, password) {
    return this.request('/auth/login', { method: 'POST', body: { username, password } });
  },
  changePassword(currentPassword, newPassword) {
    return this.request('/auth/change-password', { method: 'POST', body: { currentPassword, newPassword } });
  },
  resetPassword(recoveryKey, newPassword) {
    return this.request('/auth/reset-password', { method: 'POST', body: { recoveryKey, newPassword } });
  },

  // Public config
  getPublicConfig() {
    return this.request('/config/public');
  },

  // Sections
  listSections({ onlyActive = false } = {}) {
    return this.request(`/sections?onlyActive=${onlyActive}`);
  },
  getSectionDetail(id) {
    return this.request(`/sections/${id}/detail`);
  },
  createSection(data) {
    return this.request('/sections', { method: 'POST', body: data });
  },
  updateSection(id, data) {
    return this.request(`/sections/${id}`, { method: 'PUT', body: data });
  },
  deleteSection(id) {
    return this.request(`/sections/${id}`, { method: 'DELETE' });
  },

  // Contents
  listContents({ page = 1, pageSize = 20, onlyActive = false, sectionId = null } = {}) {
    let url = `/contents?page=${page}&pageSize=${pageSize}&onlyActive=${onlyActive}`;
    if (sectionId !== null && sectionId !== undefined && sectionId !== '') url += `&sectionId=${sectionId}`;
    return this.request(url);
  },
  getContentDetail(id) {
    return this.request(`/contents/${id}/detail`);
  },
  createContent(data) {
    return this.request('/contents', { method: 'POST', body: data });
  },
  updateContent(id, data) {
    return this.request(`/contents/${id}`, { method: 'PUT', body: data });
  },
  deleteContent(id) {
    return this.request(`/contents/${id}`, { method: 'DELETE' });
  },

  // Media
  listMedia({ page = 1, pageSize = 24 } = {}) {
    return this.request(`/media?page=${page}&pageSize=${pageSize}`);
  },
  uploadMedia(file) {
    const formData = new FormData();
    formData.append('file', file);
    return this.request('/media/upload', { method: 'POST', body: formData });
  },
  deleteMedia(id) {
    return this.request(`/media/${id}`, { method: 'DELETE' });
  },

  // Lab heads
  listLabHeads({ onlyActive = false } = {}) {
    return this.request(`/lab-heads?onlyActive=${onlyActive}`);
  },
  getLabHead(id) {
    return this.request(`/lab-heads/${id}`);
  },
  createLabHead(data) {
    return this.request('/lab-heads', { method: 'POST', body: data });
  },
  updateLabHead(id, data) {
    return this.request(`/lab-heads/${id}`, { method: 'PUT', body: data });
  },
  deleteLabHead(id) {
    return this.request(`/lab-heads/${id}`, { method: 'DELETE' });
  }
};

class ApiError extends Error {
  constructor(payload) {
    super(payload?.title || 'Request failed');
    this.status = payload?.status;
    this.errors = payload?.errors;
    this.detail = payload?.detail;
    this.payload = payload;
  }

  formatErrors() {
    if (!this.errors) return this.message;
    const lines = Object.entries(this.errors)
      .map(([field, msgs]) => `${field}: ${msgs.join(', ')}`);
    return [this.message, ...lines].join('\n');
  }
}
