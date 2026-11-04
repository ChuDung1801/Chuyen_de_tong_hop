// ============== 
// TV4 — HTTP helper; JWT chỉ lưu trong sessionStorage để giới hạn thời gian tồn tại.
// ==============
export const API_BASE = import.meta.env.VITE_API_BASE_URL || 'http://localhost:8080'
export type AuthUser = { accessToken: string; expiresAtUtc: string; userId: number; email: string; role: 'Candidate' | 'Recruiter' | 'Admin' }
export function getToken() { return sessionStorage.getItem('aiRecruitToken') || '' }
export function setSession(user: AuthUser) { sessionStorage.setItem('aiRecruitToken', user.accessToken); sessionStorage.setItem('aiRecruitUser', JSON.stringify({ userId: user.userId, email: user.email, role: user.role })) }
export function clearSession() { sessionStorage.removeItem('aiRecruitToken'); sessionStorage.removeItem('aiRecruitUser') }
export function getSession(): { userId: number; email: string; role: AuthUser['role'] } | null { try { return JSON.parse(sessionStorage.getItem('aiRecruitUser') || 'null') } catch { return null } }
export async function api<T>(path: string, options: RequestInit = {}): Promise<T> {
  const headers = new Headers(options.headers)
  if (!(options.body instanceof FormData) && options.body && !headers.has('Content-Type')) headers.set('Content-Type', 'application/json')
  const token = getToken(); if (token) headers.set('Authorization', `Bearer ${token}`)
  const response = await fetch(`${API_BASE}${path}`, { ...options, headers })
  if (!response.ok) {
    let message = `HTTP ${response.status}`
    try { const data = await response.json(); message = data.message || data.title || message } catch { /* empty error body */ }
    throw new Error(message)
  }
  if (response.status === 204) return undefined as T
  return response.json() as Promise<T>
}
