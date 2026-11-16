import { useEffect, useState } from 'react'
import { api } from './api'

type Cv = { id: number; originalFileName: string; sizeBytes: number; uploadedAtUtc: string }
type Recommendation = {
  rank: number; matchResultId: number; cvId: number; jobId: number; jobTitle: string
  location: string; employmentType: string; matchScore: number; matchingSkills: string[]
  missingSkills: string[]; explanation: string; modelVersion: string; mode: string; createdAtUtc: string
}

// TV5 — Candidate xem các công việc được xếp hạng từ CV của chính mình; backend kiểm tra quyền sở hữu CV.
export default function Sprint5Recommendations() {
  const [cvs, setCvs] = useState<Cv[]>([])
  const [cvId, setCvId] = useState('')
  const [topK, setTopK] = useState('5')
  const [items, setItems] = useState<Recommendation[]>([])
  const [error, setError] = useState('')
  const [message, setMessage] = useState('')
  const [busy, setBusy] = useState(false)

  useEffect(() => {
    api<Cv[]>('/api/cvs/my').then(result => {
      setCvs(result)
      if (result.length > 0) setCvId(String(result[0].id))
    }).catch(e => setError((e as Error).message))
  }, [])

  async function loadRecommendations() {
    if (!cvId) { setError('Hãy tải CV lên trong mục Hồ sơ của tôi trước.'); return }
    setBusy(true); setError(''); setMessage('')
    try {
      const result = await api<Recommendation[]>(`/api/cvs/${cvId}/recommendations?topK=${topK}`)
      setItems(result)
      setMessage(result.length ? `Đã xếp hạng ${result.length} công việc theo mức độ phù hợp.` : 'Chưa có tin tuyển dụng đang mở để đề xuất.')
    } catch (e) { setError((e as Error).message) }
    finally { setBusy(false) }
  }

  return <div className="sprint5-recommendations">
    <div className="section-heading"><div><p className="eyebrow">AI MATCHING</p><h2>Việc làm phù hợp với bạn</h2><p className="description">Hệ thống xếp hạng tin đang tuyển dựa trên kỹ năng, kinh nghiệm và mức độ tương đồng nội dung CV–Job.</p></div></div>
    {error && <p className="notice" role="alert">{error}</p>}
    {message && <p className="notice success" role="status">{message}</p>}
    <section className="panel recommendation-controls">
      <label>Chọn CV
        <select value={cvId} onChange={e => setCvId(e.target.value)}>
          <option value="">Chọn CV để phân tích</option>
          {cvs.map(cv => <option value={cv.id} key={cv.id}>{cv.originalFileName}</option>)}
        </select>
      </label>
      <label>Số gợi ý
        <select value={topK} onChange={e => setTopK(e.target.value)}>
          <option value="3">3 công việc</option><option value="5">5 công việc</option><option value="10">10 công việc</option><option value="20">20 công việc</option>
        </select>
      </label>
      <button className="primary-button" disabled={busy || !cvId} onClick={() => void loadRecommendations()}>{busy ? 'Đang phân tích…' : 'Tìm việc phù hợp'}</button>
      <small>Chỉ CV thuộc tài khoản hiện tại mới được chấp nhận. Điểm AI là thông tin tham khảo, không phải xác suất chắc chắn được tuyển.</small>
    </section>
    {items.length > 0 && <div className="recommendation-list">{items.map(item => <article className="panel recommendation-card" key={`${item.matchResultId}-${item.jobId}`}>
      <div className="recommendation-rank">#{item.rank}</div>
      <div className="recommendation-main"><div className="recommendation-title"><div><h3>{item.jobTitle}</h3><p>{item.location || 'Chưa xác định địa điểm'} · {item.employmentType}</p></div><div className="recommendation-score"><strong>{item.matchScore.toFixed(1)}%</strong><span>Mức phù hợp</span></div></div>
        <div className="score-track" aria-label={`Điểm phù hợp ${item.matchScore.toFixed(1)} phần trăm`}><span style={{ width: `${Math.max(0, Math.min(100, item.matchScore))}%` }} /></div>
        <div className="skill-columns"><div><b>Kỹ năng phù hợp</b><p>{item.matchingSkills.length ? item.matchingSkills.join(', ') : 'Chưa có kỹ năng trùng'}</p></div><div><b>Kỹ năng còn thiếu</b><p>{item.missingSkills.length ? item.missingSkills.join(', ') : 'Không có kỹ năng bị thiếu theo danh sách hiện tại'}</p></div></div>
        <details><summary>Vì sao được đề xuất?</summary><p>{item.explanation}</p><small>Model: {item.modelVersion} · Chế độ: {item.mode === 'ml' ? 'ML' : 'Fallback'} · Lưu lúc {new Date(item.createdAtUtc).toLocaleString('vi-VN')}</small></details>
      </div>
    </article>)}</div>}
    {cvs.length === 0 && <div className="empty"><h3>Chưa có CV</h3><p>Vào Hồ sơ của tôi, tải lên CV PDF hoặc DOCX rồi quay lại đây.</p></div>}
  </div>
}
