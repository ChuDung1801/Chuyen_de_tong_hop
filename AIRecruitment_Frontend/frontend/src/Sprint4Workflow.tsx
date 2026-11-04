import { FormEvent, useEffect, useState } from 'react'
import { api } from './api'

type Role = 'Candidate' | 'Recruiter' | 'Admin'
type Job = { id: number; title: string; description: string; location: string; employmentType: string; requiredSkillsCsv?: string }
type Cv = { id: number; originalFileName: string; contentType: string; sizeBytes: number }
type Application = { id: number; jobId: number; jobTitle: string; candidateName: string; candidateEmail: string; cvId?: number; cvFileName?: string; coverLetter?: string; status: string; appliedAtUtc: string }
type Match = { id: number; cvId: number; jobId: number; jobTitle: string; matchScore: number; matchingSkills: string[]; missingSkills: string[]; explanation?: string; modelVersion: string; createdAtUtc: string }
type Interview = { id: number; applicationId: number; scheduledAtUtc: string; durationMinutes: number; meetingUrl?: string; location?: string; notes?: string; status: string; jobTitle: string; candidateName: string }

// ============== 
// TV4 — Giao diện ứng tuyển, trạng thái và lịch phỏng vấn.
// TV5 — Kích hoạt phân tích CV/matching và hiển thị kết quả có nguồn/version.
// ==============

function ApplicationListForCandidate() {
  const [mine, setMine] = useState<Application[]>([])
  const [localError, setLocalError] = useState('')
  async function refresh() { try { setMine(await api<Application[]>('/api/applications/my')); setLocalError('') } catch (e) { setLocalError((e as Error).message) } }
  useEffect(() => { void refresh() }, [])
  return <><div className="action-row"><button className="secondary-button" onClick={() => void refresh()}>Làm mới danh sách hồ sơ</button></div>{localError && <p className="notice">{localError}</p>}{mine.length === 0 ? <p className="description">Chưa có hồ sơ ứng tuyển.</p> : mine.map(app => <article className="job-card" key={app.id}><div className="job-info"><h4>{app.jobTitle}</h4><p>Trạng thái: <strong>{app.status}</strong></p><small>Nộp {new Date(app.appliedAtUtc).toLocaleString('vi-VN')}</small></div></article>)}</>
}

export default function Sprint4Workflow({ role }: { role: Role }) {
  const [jobs, setJobs] = useState<Job[]>([])
  const [cvs, setCvs] = useState<Cv[]>([])
  const [applications, setApplications] = useState<Application[]>([])
  const [interviews, setInterviews] = useState<Interview[]>([])
  const [matches, setMatches] = useState<Match[]>([])
  const [jobId, setJobId] = useState('')
  const [cvId, setCvId] = useState('')
  const [coverLetter, setCoverLetter] = useState('')
  const [message, setMessage] = useState('')
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(false)
  const [selectedApplication, setSelectedApplication] = useState<Application | null>(null)

  async function loadJobs() {
    if (role === 'Candidate') {
      const result = await api<{items: Job[]}>('/api/jobs?page=1&pageSize=100')
      setJobs(result.items)
    } else {
      setJobs(await api<Job[]>('/api/jobs/mine'))
    }
  }
  async function loadCvs() { if (role === 'Candidate') setCvs(await api<Cv[]>('/api/cvs/my')) }
  async function loadInterviews() { setInterviews(await api<Interview[]>('/api/interviews/my')) }
  async function loadApplicationsForJob(nextJobId = jobId) {
    if (!nextJobId) return
    setApplications(await api<Application[]>(`/api/jobs/${nextJobId}/applications`))
  }
  async function loadMatches(nextCvId = cvId) {
    if (!nextCvId) return
    setMatches(await api<Match[]>(`/api/cvs/${nextCvId}/matches`))
  }
  useEffect(() => {
    Promise.all([loadJobs(), loadCvs(), loadInterviews()]).catch(e => setError((e as Error).message))
  }, [role])

  async function apply(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); if (!jobId) return
    setBusy(true); setError(''); setMessage('')
    try {
      await api(`/api/jobs/${jobId}/applications`, { method: 'POST', body: JSON.stringify({ cvId: cvId ? Number(cvId) : null, coverLetter: coverLetter.trim() || null }) })
      setMessage('Ứng tuyển thành công. Bạn có thể theo dõi trạng thái trong danh sách hồ sơ.'); setCoverLetter('')
    } catch (e) { setError((e as Error).message) } finally { setBusy(false) }
  }
  async function analyzeCv() {
    if (!cvId) { setError('Hãy chọn CV trước.'); return }
    setBusy(true); setError(''); setMessage('')
    try { const result = await api<{status:string; extractedSkills:string[]; yearsExperience?:number}>(`/api/cvs/${cvId}/analyze`, { method: 'POST' }); setMessage(`Phân tích CV: ${result.status}. Kỹ năng: ${(result.extractedSkills || []).join(', ') || 'chưa trích xuất được'}.`); await loadMatches() }
    catch (e) { setError(`Chưa phân tích được CV: ${(e as Error).message}. Kiểm tra AI Service, định dạng PDF/DOCX và thử lại.`) }
    finally { setBusy(false) }
  }
  async function matchJob() {
    if (!cvId || !jobId) { setError('Hãy chọn CV và công việc.'); return }
    setBusy(true); setError(''); setMessage('')
    try { const result = await api<Match>(`/api/cvs/${cvId}/match/${jobId}`, { method: 'POST' }); setMatches(old => [result, ...old.filter(x => x.id !== result.id)]); setMessage(`Đã tính điểm phù hợp ${result.matchScore.toFixed(2)}% bằng ${result.modelVersion}.`) }
    catch (e) { setError((e as Error).message) } finally { setBusy(false) }
  }
  async function updateStatus(application: Application, status: string) {
    setError(''); setMessage('')
    try { await api(`/api/applications/${application.id}/status`, { method: 'PATCH', body: JSON.stringify({ status }) }); await loadApplicationsForJob(); setMessage('Đã cập nhật trạng thái hồ sơ.') }
    catch (e) { setError((e as Error).message) }
  }
  async function scheduleInterview(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); if (!selectedApplication) return
    const form = new FormData(event.currentTarget)
    setBusy(true); setError(''); setMessage('')
    try {
      const localTime = String(form.get('scheduledAtUtc') || '')
      await api(`/api/applications/${selectedApplication.id}/interviews`, { method: 'POST', body: JSON.stringify({ scheduledAtUtc: new Date(localTime).toISOString(), durationMinutes: Number(form.get('durationMinutes')), meetingUrl: form.get('meetingUrl') || null, location: form.get('location') || null, notes: form.get('notes') || null }) })
      setMessage('Đã tạo lịch phỏng vấn và cập nhật trạng thái hồ sơ.'); setSelectedApplication(null); await Promise.all([loadApplicationsForJob(), loadInterviews()])
    } catch (e) { setError((e as Error).message) } finally { setBusy(false) }
  }

  return <div className="sprint4-workflow">
    <div className="section-heading"><div><h2>Ứng tuyển & phỏng vấn</h2><p className="description">AI chỉ hỗ trợ tham khảo. Nhà tuyển dụng vẫn quyết định dựa trên đánh giá của con người.</p></div></div>
    {message && <p className="notice success" role="status">{message}</p>}{error && <p className="notice" role="alert">{error}</p>}

    {role === 'Candidate' && <>
      <section className="panel"><h3>Nộp hồ sơ ứng tuyển</h3><form className="form-grid" onSubmit={apply}>
        <label className="full">Công việc<select required value={jobId} onChange={e => setJobId(e.target.value)}><option value="">Chọn tin tuyển dụng</option>{jobs.map(j => <option key={j.id} value={j.id}>{j.title} · #{j.id}</option>)}</select></label>
        <label className="full">CV đính kèm<select value={cvId} onChange={e => setCvId(e.target.value)}><option value="">Không đính kèm CV</option>{cvs.map(cv => <option key={cv.id} value={cv.id}>{cv.originalFileName}</option>)}</select></label>
        <label className="full">Thư ứng tuyển<textarea rows={4} maxLength={4000} value={coverLetter} onChange={e => setCoverLetter(e.target.value)} placeholder="Giới thiệu ngắn gọn vì sao bạn phù hợp với vị trí…" /></label>
        <button className="primary-button" disabled={busy || !jobId}>{busy ? 'Đang xử lý…' : 'Gửi hồ sơ'}</button>
      </form></section>
      <section className="panel"><h3>Phân tích CV và so khớp công việc</h3><p className="description">Hỗ trợ PDF/DOCX có thể trích xuất văn bản; PDF scan cần OCR và chưa được hỗ trợ ở bản này.</p><div className="form-grid"><label>CV<select value={cvId} onChange={e => setCvId(e.target.value)}><option value="">Chọn CV</option>{cvs.map(cv => <option key={cv.id} value={cv.id}>{cv.originalFileName}</option>)}</select></label><label>Công việc<select value={jobId} onChange={e => setJobId(e.target.value)}><option value="">Chọn công việc</option>{jobs.map(j => <option key={j.id} value={j.id}>{j.title} · #{j.id}</option>)}</select></label><div className="action-row"><button className="secondary-button" disabled={busy || !cvId} onClick={() => void analyzeCv()}>Phân tích CV</button><button className="primary-button" disabled={busy || !cvId || !jobId} onClick={() => void matchJob()}>Tính điểm phù hợp</button><button className="secondary-button" disabled={!cvId} onClick={() => void loadMatches()}>Tải lịch sử điểm</button></div></div>
        {matches.length > 0 && <div className="company-list">{matches.map(m => <article className="job-card" key={m.id}><div className="job-info"><h4>{m.jobTitle}</h4><p><strong>{m.matchScore.toFixed(2)}%</strong> · phiên bản {m.modelVersion}</p><p>Kỹ năng phù hợp: {m.matchingSkills.join(', ') || 'Chưa có'}</p><p>Kỹ năng còn thiếu: {m.missingSkills.join(', ') || 'Không có'}</p><small>{m.explanation}</small></div></article>)}</div>}
      </section>
      <section className="panel"><div className="section-heading"><h3>Hồ sơ đã ứng tuyển</h3><button className="secondary-button" onClick={() => void loadInterviews()}>Làm mới lịch phỏng vấn</button></div><ApplicationListForCandidate key={message} /></section>
    </>}

    {(role === 'Recruiter' || role === 'Admin') && <section className="panel"><h3>Quản lý hồ sơ ứng tuyển</h3><div className="form-grid"><label className="full">Tin tuyển dụng của tôi<select value={jobId} onChange={async e => { setJobId(e.target.value); try { await loadApplicationsForJob(e.target.value) } catch (err) { setError((err as Error).message) } }}><option value="">Chọn tin tuyển dụng</option>{jobs.map(j => <option key={j.id} value={j.id}>{j.title} · #{j.id}</option>)}</select></label><button className="secondary-button" disabled={!jobId} onClick={() => void loadApplicationsForJob()}>Tải hồ sơ</button></div>
      {applications.map(app => <article className="job-card" key={app.id}><div className="job-info"><h4>{app.candidateName} · {app.jobTitle}</h4><p>{app.candidateEmail} · Nộp {new Date(app.appliedAtUtc).toLocaleString('vi-VN')}</p><p>CV: {app.cvFileName || 'Không đính kèm'} · {app.coverLetter || 'Không có thư ứng tuyển'}</p><div className="action-row"><label>Trạng thái<select value={app.status} onChange={e => void updateStatus(app, e.target.value)}><option value="Pending">Pending</option><option value="Reviewing">Reviewing</option><option value="Shortlisted">Shortlisted</option><option value="InterviewScheduled">InterviewScheduled</option><option value="Rejected">Rejected</option><option value="Hired">Hired</option></select></label><button className="primary-button" onClick={() => setSelectedApplication(app)}>Đặt lịch phỏng vấn</button></div></div></article>)}
      {selectedApplication && <form className="panel form-grid" onSubmit={scheduleInterview}><h4 className="full">Lịch phỏng vấn: {selectedApplication.candidateName}</h4><label>Ngày giờ<input name="scheduledAtUtc" type="datetime-local" required min={new Date(Date.now() + 10 * 60 * 1000 - new Date().getTimezoneOffset() * 60000).toISOString().slice(0,16)} /></label><label>Thời lượng (phút)<select name="durationMinutes" defaultValue="60"><option value="15">15</option><option value="30">30</option><option value="45">45</option><option value="60">60</option><option value="90">90</option><option value="120">120</option></select></label><label className="full">Link họp<input name="meetingUrl" type="url" placeholder="https://..." /></label><label>Địa điểm<input name="location" /></label><label>Ghi chú<input name="notes" /></label><div className="action-row"><button className="primary-button" disabled={busy}>Lưu lịch</button><button type="button" className="secondary-button" onClick={() => setSelectedApplication(null)}>Hủy</button></div></form>}
    </section>}

    <section className="panel"><div className="section-heading"><h3>Lịch phỏng vấn của tôi</h3><button className="secondary-button" onClick={() => void loadInterviews()}>Làm mới</button></div>{interviews.length === 0 ? <p className="description">Chưa có lịch phỏng vấn.</p> : interviews.map(item => <article className="job-card" key={item.id}><div className="job-info"><h4>{item.jobTitle} · {item.candidateName}</h4><p>{new Date(item.scheduledAtUtc).toLocaleString('vi-VN')} · {item.durationMinutes} phút</p>{item.meetingUrl && <p><a href={item.meetingUrl} target="_blank" rel="noreferrer">Mở link họp</a></p>}{item.location && <p>{item.location}</p>}{item.notes && <small>{item.notes}</small>}</div><span className="job-label">{item.status}</span></article>)}</section>
  </div>

}
