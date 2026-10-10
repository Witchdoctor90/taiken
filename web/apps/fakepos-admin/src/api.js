const STORAGE_KEY = "fakepos_api_base";
const DEFAULT_API_BASE = "http://localhost:5080";

export function getApiBase() {
  return localStorage.getItem(STORAGE_KEY) || DEFAULT_API_BASE;
}

export function setApiBase(url) {
  localStorage.setItem(STORAGE_KEY, url);
}

async function request(path, options) {
  const response = await fetch(`${getApiBase()}${path}`, {
    headers: { "Content-Type": "application/json" },
    ...options,
  });

  if (!response.ok) {
    const body = await response.json().catch(() => ({}));
    throw new Error(body.error || `HTTP ${response.status}`);
  }

  return response.status === 204 ? null : response.json();
}

export const api = {
  locations: () => request("/admin/api/locations"),
  tables: (loc) => request(`/admin/api/locations/${loc}/tables`),
  table: (loc, no) => request(`/admin/api/locations/${loc}/tables/${no}`),
  addItem: (loc, no, item) =>
    request(`/admin/api/locations/${loc}/tables/${no}/items`, {
      method: "POST",
      body: JSON.stringify(item),
    }),
  reset: (loc, no) => request(`/admin/api/locations/${loc}/tables/${no}/reset`, { method: "POST" }),
};
