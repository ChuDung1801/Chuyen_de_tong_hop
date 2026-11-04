import { useCallback, useEffect, useState } from 'react'
import { api } from './api'
import './sprint6.css'

type CountByStatus = { status: string; count: number }
type ActiveModel = { version: string; algorithm: string; createdAtUtc: string }
type Dashboard = {
  generatedAtUtc: string
  users: { total: number; candidates: number; recruiters: number }
  jobs: { total: number; published: number }
  applications: { total: number; byStatus: CountByStatus[] }
  cvs: { total: number; analysesCompleted: number; analysesFailed: number }
  interviews: { total: number; upcoming: number }
  ai: { totalMatches: number; averageMatchScore: number; activeModelCount: number }
  activeModels: ActiveModel[]
}

// ============== TV5 — Sprint 6: màn hình quản trị chỉ hiển thị số liệu tổng hợp. ==============
export default function AdminDashboard() {
  const [data, setData] = useState<Dashboard | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  const load = useCallback(async () => {
    setLoading(true)
    setError('')
    try {
      setData(await api<Dashboard>('/api/admin/dashboard'))
    } catch (e) {
      setError((e as Error).message || 'Không thể tải dashboard.')
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => { void load() }, [load])

  return <section className="admin6-dashboard">
    <div className="admin6-heading">
      <div>
        <p className="eyebrow">QUẢN TRỊ HỆ THỐNG</p>
        <h2>Dashboard tổng quan</h2>
        <p className="admin6-subtitle">Số liệu tổng hợp phục vụ theo dõi hoạt động nền tảng.</p>
      </div>
      <button className="secondary-button" onClick={() => void load()} disabled={loading}>
        {loading ? 'Đang tải…' : 'Làm mới số liệu'}
      </button>
    </div>

    {error && <div className="notice" role="alert">{error}<p>Kiểm tra tài khoản có role Admin và backend đã áp dụng schema Sprint 4/5.</p></div>}
    {loading && !data && <div className="panel">Đang tải dữ liệu tổng hợp…</div>}
    {!loading && !data && !error && <div className="panel">Chưa có dữ liệu dashboard.</div>}

    {data && <>
      <div className="admin6-stat-grid">
        <article className="panel admin6-stat"><span>Tài khoản</span><strong>{data.users.total}</strong><small>{data.users.candidates} ứng viên · {data.users.recruiters} nhà tuyển dụng</small></article>
        <article className="panel admin6-stat"><span>Tin tuyển dụng</span><strong>{data.jobs.total}</strong><small>{data.jobs.published} tin đang công khai</small></article>
        <article className="panel admin6-stat"><span>Hồ sơ ứng tuyển</span><strong>{data.applications.total}</strong><small>{data.interviews.upcoming} lịch phỏng vấn sắp tới</small></article>
        <article className="panel admin6-stat"><span>CV đã tải lên</span><strong>{data.cvs.total}</strong><small>{data.cvs.analysesCompleted} phân tích hoàn tất · {data.cvs.analysesFailed} thất bại</small></article>
        <article className="panel admin6-stat"><span>Lượt matching</span><strong>{data.ai.totalMatches}</strong><small>Điểm trung bình {data.ai.averageMatchScore.toFixed(2)}/100</small></article>
        <article className="panel admin6-stat"><span>Mô hình AI đang hoạt động</span><strong>{data.ai.activeModelCount}</strong><small>Từ metadata phiên bản mô hình trong database</small></article>
      </div>

      <div className="admin6-lower-grid">
        <section className="panel admin6-panel">
          <h3>Hồ sơ theo trạng thái</h3>
          {data.applications.byStatus.length === 0 ? <p className="admin6-muted">Chưa có hồ sơ ứng tuyển.</p> : <div className="admin6-status-list">
            {data.applications.byStatus.map(item => <div className="admin6-status-row" key={item.status}><span>{item.status}</span><strong>{item.count}</strong></div>)}
          </div>}
        </section>
        <section className="panel admin6-panel">
          <h3>Mô hình AI hoạt động</h3>
          {data.activeModels.length === 0 ? <p className="admin6-muted">Chưa có phiên bản nào được đánh dấu đang hoạt động.</p> : <div className="admin6-model-list">
            {data.activeModels.map(model => <div className="admin6-model-row" key={model.version}><div><strong>{model.version}</strong><span>{model.algorithm}</span></div><small>{new Date(model.createdAtUtc).toLocaleString('vi-VN')}</small></div>)}
          </div>}
        </section>
      </div>
      <p className="admin6-footnote">Cập nhật lúc {new Date(data.generatedAtUtc).toLocaleString('vi-VN')}. Điểm AI chỉ hỗ trợ tham khảo; không thay thế đánh giá của con người.</p>
    </>}
  </section>
}
