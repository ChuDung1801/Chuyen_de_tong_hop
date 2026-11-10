/* ============== 
   TV4 — Dữ liệu demo tùy chọn. Chạy sau khi đã tạo migration/database.
   Không tạo tài khoản có mật khẩu mặc định; đăng ký tài khoản qua API/UI.
   ============== */
SET NOCOUNT ON;
IF OBJECT_ID('dbo.Jobs', 'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.Jobs WHERE Title = N'Backend Developer (Demo)')
BEGIN
    /* CreatedByUserId cần là User có role Recruiter/Admin; vì vậy seed job qua UI sau khi đăng ký Recruiter,
       tạo Company rồi đăng tin sẽ an toàn hơn. File này chỉ xác nhận kết nối database. */
    PRINT 'Database sẵn sàng. Tạo dữ liệu demo bằng UI để bảo đảm khóa ngoại CreatedByUserId hợp lệ.';
END;
