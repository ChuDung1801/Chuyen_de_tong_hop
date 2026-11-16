import { useCallback, useEffect, useState } from 'react'
import { API_BASE, api, getToken } from './api'
import './recruiter-verification.css'

type RequestItem = {
  id: number; userId: number; email: string; applicantFullName: string; applicantPhone: string; applicantPosition: string
  companyName: string; taxCode: string; website?: string; industry?: string; companyAddress: string
  companyDescription?: string; applicantNote?: string; documentOriginalFileName: string; documentContentType: string
  documentSizeBytes: number; status: string; submittedAtUtc: string; reviewNote?: string
}

export default function AdminRecruiterAccessRequests() {
  const [items, setItems] = useState<RequestItem[]>([])
  const [filter, setFilter] = useState('Pending')
  const [notes, setNotes] = useState<Record<number, string>>({})
  const [loading, setLoading] = useState(true)
  const [busyId, setBusyId] = useState<number | null>(null)
  const [error, setError] = useState('')
  const [message, setMessage] = useState('')

  const load = useCallback(async () => {
    setLoading(true); setError('')
    try { setItems(await api<RequestItem[]>(`/api/admin/recruiter-access-requests?status=${filter}`)) }
    catch (e) { setError((e as Error).message) }
    finally { setLoading(false) }
  }, [filter])
  useEffect(() => { void load() }, [load])

  async function openDocument(item: RequestItem) {
    setError('')
    try {
      const response = await fetch(`${API_BASE}/api/admin/recruiter-access-requests/${item.id}/document`, { headers: { Authorization: `Bearer ${getToken()}` } })
      if (!response.ok) throw new Error(`Không tải được tài liệu (HTTP ${response.status}).`)
      const blob = await response.blob()
      const url = URL.createObjectURL(blob)
      const link = document.createElement('a'); link.href = url; link.download = item.documentOriginalFileName; document.body.appendChild(link); link.click(); link.remove()
      window.setTimeout(() => URL.revokeObjectURL(url), 60_000)
    } catch (e) { setError((e as Error).message) }
  }

  async function review(item: RequestItem, approve: boolean) {
    const note = (notes[item.id] || '').trim()
    if (!approve && !note) { setError(`Cần nhập lý do từ chối hồ sơ của ${item.companyName}.`); return }
    setBusyId(item.id); setError(''); setMessage('')
    try {
      await api(`/api/admin/recruiter-access-requests/${item.id}/review`, { method: 'POST', body: JSON.stringify({ approve, note }) })
      setMessage(approve ? `Đã phê duyệt ${item.companyName}; tài khoản đã được cấp quyền Recruiter.` : `Đã từ chối hồ sơ ${item.companyName}.`)
      await load()
    } catch (e) { setError((e as Error).message) }
    finally { setBusyId(null) }
  }

  return <section className="recruiter-admin-page">
    <p className="eyebrow">QUẢN TRỊ XÁC MINH DOANH NGHIỆP</p>
    <div className="recruiter-admin-heading"><div><h2>Yêu cầu cấp quyền nhà tuyển dụng</h2><p>Chỉ Admin có thể xem giấy tờ xác minh và quyết định phê duyệt.</p></div><select value={filter} onChange={e => setFilter(e.target.value)}><option value="Pending">Đang chờ</option><option value="Approved">Đã duyệt</option><option value="Rejected">Đã từ chối</option></select></div>
    {error && <div className="notice" role="alert">{error}</div>}
    {message && <div className="notice success" role="status">{message}</div>}
    {loading ? <div className="panel">Đang tải hồ sơ…</div> : items.length === 0 ? <div className="panel">Không có hồ sơ ở trạng thái này.</div> : items.map(item => <article className="panel recruiter-admin-card" key={item.id}>
      <div className="recruiter-admin-card-heading"><div><h3>{item.companyName}</h3><small>{item.email} · Gửi lúc {new Date(item.submittedAtUtc).toLocaleString('vi-VN')}</small></div><span className={`recruiter-status-badge ${item.status.toLowerCase()}`}>{item.status}</span></div>
      <dl className="recruiter-admin-details">
        <div><dt>Mã số thuế</dt><dd>{item.taxCode}</dd></div><div><dt>Đại diện</dt><dd>{item.applicantFullName} · {item.applicantPosition}</dd></div><div><dt>Liên hệ</dt><dd>{item.applicantPhone}</dd></div><div><dt>Địa chỉ</dt><dd>{item.companyAddress}</dd></div><div><dt>Ngành nghề</dt><dd>{item.industry || 'Chưa cung cấp'}</dd></div><div><dt>Website</dt><dd>{item.website || 'Chưa cung cấp'}</dd></div>
      </dl>
      {item.companyDescription && <p>{item.companyDescription}</p>}{item.applicantNote && <p><strong>Ghi chú:</strong> {item.applicantNote}</p>}
      <div className="recruiter-document-row"><span>Giấy tờ: {item.documentOriginalFileName} ({(item.documentSizeBytes / 1024 / 1024).toFixed(2)} MB)</span><button className="secondary-button" onClick={() => void openDocument(item)}>Tải tài liệu xác minh</button></div>
      {item.status === 'Pending' ? <div className="recruiter-review-actions"><label>Ghi chú xét duyệt / lý do từ chối<textarea rows={2} maxLength={2000} value={notes[item.id] || ''} onChange={e => setNotes(old => ({ ...old, [item.id]: e.target.value }))}/></label><div><button className="primary-button" disabled={busyId === item.id} onClick={() => void review(item, true)}>Phê duyệt</button><button className="secondary-button" disabled={busyId === item.id} onClick={() => void review(item, false)}>Từ chối</button></div></div> : item.reviewNote && <p><strong>Ghi chú Admin:</strong> {item.reviewNote}</p>}
    </article>)}
  </section>
}
