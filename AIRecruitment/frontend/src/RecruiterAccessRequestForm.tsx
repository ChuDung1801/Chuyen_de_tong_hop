import { FormEvent, useCallback, useEffect, useState } from 'react'
import { api } from './api'
import './recruiter-verification.css'

type AccessRequest = {
  id: number; status: 'Pending' | 'Approved' | 'Rejected'; applicantFullName: string
  companyName: string; taxCode: string; documentOriginalFileName: string
  submittedAtUtc: string; reviewedAtUtc?: string; reviewNote?: string
}

export default function RecruiterAccessRequestForm() {
  const [current, setCurrent] = useState<AccessRequest | null>(null)
  const [loading, setLoading] = useState(true)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  const [message, setMessage] = useState('')

  const load = useCallback(async () => {
    setLoading(true); setError('')
    try { setCurrent(await api<AccessRequest | null>('/api/recruiter-access-requests/me')) }
    catch (e) { setError((e as Error).message) }
    finally { setLoading(false) }
  }, [])
  useEffect(() => { void load() }, [load])

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const form = event.currentTarget
    const file = (new FormData(form).get('document') as File | null)
    if (!file || file.size === 0) { setError('Vui lòng chọn giấy tờ xác minh.'); return }
    if (file.size > 10 * 1024 * 1024) { setError('Tài liệu không được vượt quá 10 MB.'); return }
    setBusy(true); setError(''); setMessage('')
    try {
      const result = await api<AccessRequest>('/api/recruiter-access-requests', { method: 'POST', body: new FormData(form) })
      setCurrent(result); setMessage('Đã gửi hồ sơ xác minh. Admin sẽ kiểm tra trước khi cấp quyền nhà tuyển dụng.'); form.reset()
    } catch (e) { setError((e as Error).message) }
    finally { setBusy(false) }
  }

  return <section className="recruiter-verify-page">
    <p className="eyebrow">XÁC MINH NHÀ TUYỂN DỤNG</p>
    <h2>Yêu cầu cấp quyền nhà tuyển dụng</h2>
    <p className="recruiter-verify-intro">Để hạn chế tài khoản tuyển dụng giả mạo, hệ thống yêu cầu thông tin doanh nghiệp và giấy tờ chứng minh. Quyền đăng tin chỉ được mở sau khi Admin phê duyệt.</p>
    {error && <div className="notice" role="alert">{error}</div>}
    {message && <div className="notice success" role="status">{message}</div>}
    {loading ? <div className="panel">Đang tải trạng thái hồ sơ…</div> : <>
      {current?.status === 'Pending' && <div className="panel recruiter-verify-status pending"><strong>Hồ sơ đang chờ xét duyệt</strong><p>Doanh nghiệp: {current.companyName} · Mã số thuế: {current.taxCode}</p><small>Gửi lúc {new Date(current.submittedAtUtc).toLocaleString('vi-VN')}. Bạn chưa thể đăng tin tuyển dụng trong thời gian chờ duyệt.</small></div>}
      {current?.status === 'Approved' && <div className="panel recruiter-verify-status approved"><strong>Hồ sơ đã được Admin phê duyệt</strong><p>Tài khoản đã được cấp quyền nhà tuyển dụng. Hãy đăng xuất và đăng nhập lại để nhận quyền mới.</p><small>{current.reviewNote || 'Không có ghi chú bổ sung.'}</small></div>}
      {current?.status === 'Rejected' && <div className="panel recruiter-verify-status rejected"><strong>Hồ sơ chưa được phê duyệt</strong><p>Lý do: {current.reviewNote || 'Admin chưa ghi chú.'}</p><small>Bạn có thể kiểm tra lại thông tin và gửi hồ sơ mới.</small></div>}
      {current?.status !== 'Pending' && current?.status !== 'Approved' && <form className="panel form-grid recruiter-verify-form" onSubmit={submit}>
        <h3 className="full">Thông tin người đại diện</h3>
        <label>Họ và tên<input name="applicantFullName" required maxLength={150}/></label>
        <label>Số điện thoại<input name="applicantPhone" required maxLength={40} type="tel"/></label>
        <label className="full">Chức vụ tại công ty<input name="applicantPosition" required maxLength={150} placeholder="Ví dụ: Giám đốc, Trưởng phòng nhân sự"/></label>
        <h3 className="full">Thông tin doanh nghiệp</h3>
        <label>Tên công ty<input name="companyName" required maxLength={200}/></label>
        <label>Mã số thuế<input name="taxCode" required maxLength={40}/></label>
        <label>Website<input name="website" type="url" placeholder="https://example.com" maxLength={500}/></label>
        <label>Ngành nghề<input name="industry" maxLength={150}/></label>
        <label className="full">Địa chỉ trụ sở<input name="companyAddress" required maxLength={300}/></label>
        <label className="full">Giới thiệu doanh nghiệp<textarea name="companyDescription" rows={3} maxLength={3000}/></label>
        <label className="full">Ghi chú cho Admin<textarea name="applicantNote" rows={3} maxLength={2000} placeholder="Thông tin bổ sung hoặc vai trò được ủy quyền…"/></label>
        <label className="full recruiter-document-field">Giấy tờ xác minh doanh nghiệp
          <input name="document" type="file" required accept=".pdf,.jpg,.jpeg,.png,application/pdf,image/jpeg,image/png"/>
          <small>Chấp nhận PDF, JPG hoặc PNG; tối đa 10 MB. Ví dụ: giấy chứng nhận đăng ký doanh nghiệp, đăng ký thuế hoặc giấy ủy quyền hợp lệ.</small>
        </label>
        <div className="full recruiter-verify-warning">Tài liệu chỉ Admin được quyền xem. Không tải lên giấy tờ cá nhân không liên quan hoặc tài liệu của doanh nghiệp khác.</div>
        <button className="primary-button" disabled={busy}>{busy ? 'Đang gửi hồ sơ…' : current?.status === 'Rejected' ? 'Gửi lại hồ sơ xác minh' : 'Gửi yêu cầu xét duyệt'}</button>
      </form>}
    </>}
  </section>
}
