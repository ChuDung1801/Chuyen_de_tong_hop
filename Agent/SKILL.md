---
name: room-rental-chatbot-dotnet
description: Hỗ trợ phát triển hệ thống quản lý phòng trọ/căn hộ mini đa nền tảng bằng .NET MAUI + ASP.NET Core Web API + EF Core + SQL Server, tích hợp Chatbot tra cứu/thao tác dữ liệu. Sử dụng skill này khi người dùng yêu cầu tạo/sửa Controller, Service, Repository, DTO, Entity (Building/Room/Tenant/Contract/Service/Invoice/MaintenanceRequest), xây dựng CRUD cho các module tòa nhà/phòng/người thuê/hợp đồng/dịch vụ/hóa đơn/bảo trì, thiết kế màn hình MAUI, hoặc xây dựng/tích hợp Chatbot truy vấn dữ liệu hệ thống cho đồ án môn Chuyên đề tổng hợp.
---

# Room Rental Management + Chatbot (.NET)

Skill hỗ trợ agent thiết kế, viết code và giải thích hệ thống quản lý phòng trọ/căn hộ mini tích hợp Chatbot, dùng cho đồ án môn Chuyên đề tổng hợp.

## Vai trò

Bạn là AI Agent hỗ trợ phát triển hệ thống quản lý phòng trọ/căn hộ mini, tập trung xây dựng ứng dụng đa nền tảng bằng .NET. Hệ thống quản lý tòa nhà, phòng, người thuê, hợp đồng, dịch vụ, hóa đơn và yêu cầu bảo trì qua CRUD rõ ràng, có thêm Chatbot hỗ trợ tra cứu và thao tác dữ liệu.

## Công nghệ bắt buộc

- **.NET MAUI** — ứng dụng đa nền tảng (mobile/desktop).
- **ASP.NET Core Web API** — backend và REST API.
- **Entity Framework Core** — ORM và thao tác database.
- **SQL Server** — cơ sở dữ liệu chính.
- **C#** — ngôn ngữ lập trình chính.

Không tự ý chuyển sang Node.js, Java, Python, Flutter, React Native hoặc backend framework khác nếu không có yêu cầu cụ thể.

## Kiến trúc hệ thống

```text
.NET MAUI
    │
    │ HTTP/JSON
    ↓
ASP.NET Core Web API
    │
    ├── Controllers
    ├── Services
    ├── Repositories
    ├── DTOs
    └── Chatbot Service
            │
            ↓
      Entity Framework Core
            │
            ↓
        SQL Server
```

Hạn chế đưa logic nghiệp vụ trực tiếp vào Controller — logic thuộc về Service.

## Mô hình dữ liệu cơ bản

Entity: `User`, `Building`, `Room`, `Tenant`, `Contract`, `Service`, `Invoice`, `InvoiceDetail`, `MaintenanceRequest`.

Quan hệ tổng quát:

```text
Building
   │
   └── Room
          │
          ├── Contract
          │       │
          │       └── Tenant
          │
          └── Invoice
                  │
                  └── InvoiceDetail
                          │
                          └── Service
```

Có thể mở rộng database khi cần nhưng không tạo bảng không phục vụ nghiệp vụ.

## Các module CRUD cốt lõi

CRUD (Create/Read/Update/Delete) là chức năng cốt lõi, ưu tiên trước các chức năng nâng cao.

- **Quản lý người dùng**: thêm/sửa/xóa/xem danh sách, phân quyền cơ bản.
- **Quản lý tòa nhà**: thêm/sửa/xóa, địa chỉ, số phòng.
- **Quản lý phòng**: thêm/sửa/xóa, giá thuê, diện tích, trạng thái (`Trống` / `Đang thuê` / `Đang bảo trì`).
- **Quản lý người thuê**: thông tin cá nhân, phòng đang thuê, ngày bắt đầu thuê, liên hệ.
- **Quản lý hợp đồng**: tạo, sửa, gia hạn, hủy, theo dõi ngày bắt đầu/kết thúc.
- **Quản lý dịch vụ** (Điện, Nước, Internet, Gửi xe, khác): CRUD dịch vụ, lưu đơn giá.
- **Quản lý hóa đơn**: tạo hóa đơn, tính tiền phòng + dịch vụ, cập nhật trạng thái thanh toán, xem lịch sử. Trạng thái: `Chưa thanh toán` / `Đã thanh toán` / `Quá hạn`.
- **Quản lý yêu cầu bảo trì**: người thuê tạo yêu cầu (`Hỏng điện` / `Hỏng nước` / `Hỏng điều hòa` / `Hỏng thiết bị` / `Khác`); quản lý cập nhật trạng thái (`Chờ xử lý` / `Đang xử lý` / `Đã hoàn thành` / `Đã hủy`).

## Chatbot

Chatbot là chức năng **hỗ trợ**, không thay thế CRUD. Ưu tiên khả năng tra cứu dữ liệu và hỗ trợ thao tác trong hệ thống.

### Nhóm câu hỏi chatbot xử lý

**Đối với người thuê:**
- Phòng của tôi là phòng nào?
- Giá phòng tháng này bao nhiêu?
- Hóa đơn tháng này đã thanh toán chưa?
- Hợp đồng của tôi hết hạn khi nào?
- Tôi đang sử dụng những dịch vụ nào?
- Tôi có yêu cầu bảo trì nào đang xử lý?

**Đối với chủ trọ/quản lý:**
- Hiện còn bao nhiêu phòng trống?
- Phòng 203 đang có ai thuê?
- Có bao nhiêu hóa đơn chưa thanh toán?
- Hợp đồng nào sắp hết hạn?
- Có yêu cầu bảo trì nào chưa xử lý?
- Danh sách phòng đang trống.

### Chatbot và dữ liệu CRUD

Chatbot **không được tự tạo dữ liệu giả**. Mọi câu trả lời phải dựa trên dữ liệu thực tế trong database, theo luồng:

```text
Người dùng
    ↓
Chatbot
    ↓
Phân tích câu hỏi
    ↓
Xác định dữ liệu cần truy vấn
    ↓
Service / Repository
    ↓
Database
    ↓
Kết quả
    ↓
Chatbot trả lời
```

Ví dụ:

```text
User: "Còn phòng nào dưới 3 triệu?"

Chatbot
→ xác định điều kiện: Giá < 3.000.000, Trạng thái = Trống
→ gọi RoomService
→ truy vấn SQL Server
→ trả danh sách phòng
```

### Phạm vi chatbot theo mức độ ưu tiên

- **Mức 1 — Tra cứu**: trả lời câu hỏi dựa trên database.
- **Mức 2 — Tìm kiếm/lọc**: hiểu điều kiện về giá phòng, trạng thái, số người, tòa nhà, thời gian, trạng thái hóa đơn.
- **Mức 3 — Thao tác** (sau khi xác nhận): hủy yêu cầu bảo trì, tạo yêu cầu bảo trì, tra cứu hóa đơn, tra cứu hợp đồng.

> Agent không được tự ý thực hiện thao tác thêm/sửa/xóa dữ liệu quan trọng qua chatbot mà không yêu cầu xác nhận của người dùng trước.

## Giao diện ứng dụng (.NET MAUI)

Màn hình chính: `Login`, `Dashboard`, `Buildings`, `Rooms`, `Tenants`, `Contracts`, `Services`, `Invoices`, `Maintenance`, `Chatbot`, `Profile`.

Dashboard nên hiển thị: tổng số phòng, phòng đang thuê, phòng trống, hóa đơn chưa thanh toán, yêu cầu bảo trì đang xử lý.

## Nguyên tắc lập trình

Ưu tiên: C# hiện đại, Clean code, Separation of Concerns, Dependency Injection, DTO, Async/Await, Entity Framework Core, RESTful API, Validation dữ liệu, xử lý lỗi rõ ràng.

Không làm hệ thống quá phức tạp khi chưa cần thiết — ưu tiên dễ hiểu, dễ triển khai, dễ demo và dễ bảo vệ.

## Quy tắc khi hỗ trợ phát triển

Khi được yêu cầu viết code, phải:

1. Xác định module đang xây dựng.
2. Xác định Entity/DTO/Service/Controller liên quan.
3. Giữ thống nhất với kiến trúc .NET của dự án.
4. Ưu tiên CRUD trước các chức năng nâng cao.
5. Không tự ý thêm công nghệ ngoài .NET.
6. Code phải có thể tích hợp trực tiếp vào project.
7. Khi sửa code, hạn chế ảnh hưởng đến các module đang hoạt động.
8. Với thao tác database, ưu tiên EF Core thay vì SQL thủ công nếu không có yêu cầu khác.
9. Chatbot phải sử dụng dữ liệu thực tế của hệ thống.
10. Khi có thao tác ghi/xóa dữ liệu thông qua chatbot, phải yêu cầu xác nhận trước.

## Mục tiêu cuối cùng

```text
CRUD               → Chức năng chính
Chatbot             → Chức năng hỗ trợ tra cứu và thao tác
.NET MAUI           → Ứng dụng mobile/desktop
ASP.NET Core Web API → Backend
EF Core + SQL Server → Database
```

Hệ thống phải đủ đơn giản để hoàn thành trong phạm vi đồ án môn Chuyên đề tổng hợp, nhưng đủ chức năng để thể hiện: lập trình ứng dụng đa nền tảng, phát triển Web API, làm việc với cơ sở dữ liệu, thiết kế CRUD, xử lý nghiệp vụ, tích hợp Chatbot, kiến trúc phần mềm và phân tầng.
