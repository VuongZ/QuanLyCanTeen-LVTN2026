# LuanVanTotNghiep – Hệ thống Quản lý Nhân sự, Ca làm việc, Kho & Lương

Backend API viết bằng **ASP.NET Core 8 (Web API)**, phục vụ cho một hệ thống quản lý chuỗi cửa hàng/chi nhánh, bao gồm quản lý nhân viên & phân quyền, xếp ca làm việc, chấm công, quản lý kho hàng (nhập/xuất/tồn kho), và tính lương. Đây là đồ án/luận văn tốt nghiệp (Graduation Thesis Project).

## Mục lục

- [Tính năng chính](#tính-năng-chính)
- [Công nghệ sử dụng](#công-nghệ-sử-dụng)
- [Kiến trúc & cấu trúc thư mục](#kiến-trúc--cấu-trúc-thư-mục)
- [Yêu cầu hệ thống](#yêu-cầu-hệ-thống)
- [Cài đặt & chạy dự án](#cài-đặt--chạy-dự-án)
- [Cấu hình](#cấu-hình)
- [Danh sách API](#danh-sách-api)
- [Xác thực & phân quyền](#xác-thực--phân-quyền)
- [Đóng góp](#đóng-góp)
- [Giấy phép](#giấy-phép)

## Tính năng chính

- **Quản lý người dùng & phân quyền (Users & Roles):** đăng ký, đăng nhập JWT, đổi mật khẩu, quên mật khẩu qua OTP gửi email, quản lý vai trò (Role), thông tin tài khoản ngân hàng nhân viên.
- **Quản lý chi nhánh (Branch):** thêm/sửa/xóa/xem danh sách chi nhánh (`DmBranch`).
- **Quản lý ca làm việc (Shift):** định nghĩa ca làm việc, cấu hình ca theo từng chi nhánh (`BranchShiftConfig`).
- **Chu kỳ xếp lịch (Schedule Period):** tạo chu kỳ xếp lịch, đăng ký ca làm việc của nhân viên (`StaffRegistration`), công bố lịch làm việc chính thức (`Publish Schedule`), chấm công (`Attendance`).
- **Quản lý kho (Inventory/Kho):** quản lý sản phẩm, nhà cung cấp (Supplier), phiếu nhập kho (Import Ticket), phiếu xuất kho (Export Ticket), tồn kho theo chi nhánh (Branch Front Stock), báo cáo đóng ca kho (Shift Closing Report).
- **Tính lương (Salary):** quy tắc tính lương (Salary Rule), điều chỉnh lương, tính lương hàng tháng cho từng nhân viên (Monthly Salary).
- **Swagger UI** tích hợp sẵn để test API trực tiếp trong môi trường Development.

## Công nghệ sử dụng

| Thành phần        | Công nghệ |
|--------------------|-----------|
| Nền tảng           | .NET 8 / ASP.NET Core Web API |
| ORM                | Entity Framework Core |
| Cơ sở dữ liệu      | MySQL (qua Pomelo.EntityFrameworkCore.MySql), chạy trên WampServer |
| Xác thực           | JWT Bearer Authentication |
| Mã hoá mật khẩu    | BCrypt.Net |
| Tài liệu API       | Swagger / Swashbuckle |
| Gửi email          | EmailService (gửi OTP đặt lại mật khẩu) |
| Kiến trúc          | Controller → Service → Repository |

## Kiến trúc & cấu trúc thư mục

Dự án tổ chức theo mô hình 3 lớp: **Controller – Service – Repository**, sử dụng Entity Framework Core làm lớp truy xuất dữ liệu.

```
LuanVanTotNghiep/
├── Program.cs                     # Điểm khởi động, cấu hình DI, JWT, DbContext, Swagger
├── backend/
│   ├── Controllers/                # Các API Controller
│   │   ├── BranchController.cs
│   │   ├── BranchShiftConfigController.cs
│   │   ├── InventoryController.cs
│   │   ├── KhoImportController.cs
│   │   ├── SalaryController.cs
│   │   ├── SchedulePeriodController.cs
│   │   ├── ShiftController.cs
│   │   ├── StaffRegistrationController.cs
│   │   ├── SupplierController.cs
│   │   └── UserController.cs
│   ├── DTOs/                       # Data Transfer Objects cho request/response
│   ├── Models/
│   │   └── Entities/                # Entity Framework Core entities & AppDbContext
│   │       ├── AppDbContext.cs
│   │       ├── CaAttendance.cs, CaBranchShiftConfig.cs, CaFinalSchedule.cs, ...
│   │       ├── DmBranch.cs
│   │       ├── Kho*.cs               # Các entity liên quan tới quản lý kho
│   │       ├── Luong*.cs             # Các entity liên quan tới lương
│   │       └── Ns*.cs                # Các entity liên quan tới người dùng/vai trò
│   ├── Repositories/                # Lớp truy xuất dữ liệu (Repository Pattern)
│   └── Services/                    # Lớp xử lý nghiệp vụ (Business Logic)
```

### Quy ước đặt tên bảng/entity

- `Ns*` (Nhân sự): `NsUser`, `NsRole`, `NsUserBankAccount`
- `Dm*` (Danh mục): `DmBranch`
- `Ca*` (Ca làm việc): `CaShift`, `CaSchedulePeriod`, `CaBranchShiftConfig`, `CaStaffRegistration`, `CaFinalSchedule`, `CaAttendance`
- `Kho*` (Kho hàng): `KhoProduct`, `KhoSupplier`, `KhoImportTicket`, `KhoImportDetail`, `KhoExportTicket`, `KhoExportDetail`, `KhoBranchInventory`, `KhoBranchFrontStock`, `KhoShiftClosingReport`, `KhoShiftClosingDetail`
- `Luong*` (Lương): `LuongSalaryRule`, `LuongMonthlySalary`

## Yêu cầu hệ thống

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- MySQL Server (dự án đang cấu hình chạy với **WampServer**)
- IDE: Visual Studio 2022 / VS Code / Rider

## Cài đặt & chạy dự án

1. **Clone dự án**
   ```bash
   git clone <repository-url>
   cd LuanVanTotNghiep
   ```

2. **Cấu hình chuỗi kết nối & JWT** trong `appsettings.json` (hoặc User Secrets), xem mục [Cấu hình](#cấu-hình) bên dưới.

3. **Khởi tạo/migrate cơ sở dữ liệu** (nếu sử dụng EF Core Migrations)
   ```bash
   dotnet ef database update
   ```

4. **Chạy ứng dụng**
   ```bash
   dotnet run
   ```

5. Khi khởi động thành công ở môi trường Development, Swagger UI sẽ tự động hiển thị tại địa chỉ gốc (`/`), có thể dùng để test toàn bộ API.

   Ứng dụng cũng in ra console kết quả kiểm tra kết nối tới MySQL (WampServer) ngay khi start.

## Cấu hình

Các giá trị cấu hình bắt buộc trong `appsettings.json` (hoặc User Secrets khi phát triển):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=3306;Database=luanvantotnghiep;User=root;Password=;"
  },
  "Jwt": {
    "Issuer": "LuanVanTotNghiep",
    "Audience": "LuanVanTotNghiep",
    "Key": "chuỗi-bí-mật-đủ-dài-để-ký-JWT"
  }
}
```

> ⚠️ Nếu thiếu bất kỳ giá trị nào trong `Jwt:Issuer`, `Jwt:Audience`, `Jwt:Key`, ứng dụng sẽ ném lỗi `InvalidOperationException` khi khởi động.

Ngoài ra cần cấu hình thêm thông tin gửi email (SMTP) cho `EmailService` để tính năng gửi OTP quên mật khẩu hoạt động.

## Danh sách API

Toàn bộ endpoint theo chuẩn REST, prefix `api/[controller]`:

| Controller | Chức năng chính |
|---|---|
| `UserController` | Đăng ký, đăng nhập, đổi mật khẩu, quên mật khẩu (OTP), quản lý danh sách nhân viên/tài khoản ngân hàng |
| `BranchController` | CRUD chi nhánh |
| `ShiftController` | CRUD ca làm việc |
| `BranchShiftConfigController` | Cấu hình ca làm việc theo từng chi nhánh |
| `SchedulePeriodController` | Tạo/cập nhật chu kỳ xếp lịch, công bố lịch làm việc |
| `StaffRegistrationController` | Nhân viên đăng ký ca làm việc theo chu kỳ |
| `SupplierController` | CRUD nhà cung cấp |
| `InventoryController` | Quản lý tồn kho theo chi nhánh |
| `KhoImportController` | Tạo/quản lý phiếu nhập kho |
| `SalaryController` | Quy tắc lương, điều chỉnh lương, tính lương hàng tháng |

Chi tiết đầy đủ (tham số, schema request/response) xem trực tiếp trong **Swagger UI** khi chạy ứng dụng ở môi trường Development.

## Xác thực & phân quyền

- Hệ thống dùng **JWT Bearer Token**: người dùng đăng nhập qua `UserController` để nhận token, sau đó đính kèm header `Authorization: Bearer <token>` cho các request cần xác thực.
- Mật khẩu được băm bằng **BCrypt** trước khi lưu vào database.
- Vai trò người dùng được quản lý qua entity `NsRole`, cho phép phân quyền truy cập theo vai trò (admin, quản lý chi nhánh, nhân viên, ...).
- Middleware được cấu hình đúng thứ tự bắt buộc: `UseAuthentication()` trước `UseAuthorization()`.

## Đóng góp

Đây là dự án luận văn tốt nghiệp cá nhân. Nếu muốn đóng góp hoặc phát triển thêm:

1. Fork repository
2. Tạo nhánh mới (`git checkout -b feature/ten-tinh-nang`)
3. Commit thay đổi (`git commit -m "Add: mô tả thay đổi"`)
4. Push nhánh và tạo Pull Request

## Giấy phép

Dự án phục vụ mục đích học tập/luận văn tốt nghiệp. Vui lòng liên hệ tác giả trước khi sử dụng cho mục đích thương mại.