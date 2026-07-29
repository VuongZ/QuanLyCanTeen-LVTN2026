# Luận Văn Tốt Nghiệp – Hệ Thống Quản Lý Nhân Sự Và Vận Hành Căn Tin

Backend API xây dựng bằng **ASP.NET Core (.NET 8)** phục vụ đồ án/luận văn tốt nghiệp: *"Xây Dựng Hệ Thống Quản Lý Nhân Sự cho Mô Hình Kinh Doanh Căn Tin"*. Hệ thống quản lý toàn bộ nghiệp vụ vận hành một chuỗi căn tin nhiều chi nhánh: nhân sự, ca làm việc, chấm công, kho hàng, tồn quầy, lương và báo cáo kết ca.

## Tính năng chính

- **Xác thực & phân quyền**: đăng nhập bằng JWT, phân quyền theo vai trò `ADMIN`, `MANAGER`, `STAFF`; quên/đặt lại mật khẩu qua OTP gửi email.
- **Quản lý chi nhánh** (`Branch`): CRUD danh sách chi nhánh căn tin.
- **Quản lý ca làm việc** (`Shift`, `BranchShiftConfig`): định nghĩa ca, cấu hình ca theo từng chi nhánh.
- **Đợt đăng ký ca** (`SchedulePeriod`): tạo đợt đăng ký, tự động khóa đợt quá hạn bằng background worker.
- **Đăng ký ca & lịch làm chính thức** (`StaffRegistration`, `FinalSchedule`): nhân viên đăng ký ca, quản lý duyệt và xuất lịch làm chính thức.
- **Chấm công** (`Attendance`): quét chấm công vào/ra; yêu cầu bù chấm công (`CheckoutRequest`) khi nhân viên quên checkout, kèm worker tự động phát hiện.
- **Quản lý kho** (`Inventory`, `KhoImport`, `KhoExport`): nhập kho, xuất kho, theo dõi tồn kho theo từng chi nhánh; hỗ trợ **OCR hóa đơn** (Tesseract) để tự động nhận diện sản phẩm/số lượng từ ảnh hóa đơn.
- **Tồn quầy** (`FrontStock`): theo dõi hàng tồn tại quầy bán.
- **Báo cáo kết ca** (`ShiftClosing`): nhân viên lập báo cáo kết ca, quản lý duyệt/kiểm tra.
- **Nhà cung cấp** (`Supplier`): quản lý danh sách nhà cung cấp.
- **Tính lương** (`Salary`): tính lương hàng tháng dựa trên chấm công, quy tắc lương và các điều chỉnh (thưởng/phạt).
- **Background jobs**: tự động khóa đợt đăng ký ca quá hạn (`SchedulePeriodDeadlineWorker`), tự động xử lý chấm công thiếu checkout (`MissingCheckoutWorker`).

## Công nghệ sử dụng

| Thành phần | Công nghệ |
|---|---|
| Framework | ASP.NET Core (.NET 8), Web API |
| ORM | Entity Framework Core (Pomelo MySQL provider) |
| Cơ sở dữ liệu | MySQL (WampServer khi phát triển local) |
| Xác thực | JWT Bearer Authentication |
| Mã hóa mật khẩu | BCrypt.Net |
| OCR hóa đơn | Tesseract (ngôn ngữ `vie+eng`) |
| Gửi email | SMTP (OTP quên mật khẩu) |
| API docs | Swagger / Swashbuckle |
| Kiến trúc | Controller – Service – Repository (theo domain: nhân sự, ca làm, kho, lương...) |

## Cấu trúc thư mục

```
LuanVanTotNghiep/
├── Program.cs                     # Cấu hình JWT, DbContext, DI, CORS, pipeline
└── backend/
    ├── Controllers/                # API endpoints (api/[controller])
    ├── DTOs/                       # Data Transfer Objects
    ├── Models/
    │   └── Entities/                # Entity Framework entities + AppDbContext
    ├── Repositories/               # Tầng truy cập dữ liệu
    └── Services/                   # Tầng xử lý nghiệp vụ
        └── BackgroundJobs/          # Các worker chạy nền
```

### Danh sách Controllers (API)

| Controller | Chức năng |
|---|---|
| `BranchController` | Quản lý chi nhánh |
| `BranchShiftConfigController` | Cấu hình ca theo chi nhánh |
| `CheckoutRequestController` | Yêu cầu bù chấm công |
| `FrontStockController` | Tồn quầy |
| `InventoryController` | Tồn kho chi nhánh |
| `KhoExportController` | Phiếu xuất kho |
| `KhoImportController` | Phiếu nhập kho (kèm OCR hóa đơn) |
| `SalaryController` | Tính lương, điều chỉnh lương |
| `SchedulePeriodController` | Đợt đăng ký ca |
| `ShiftClosingController` | Báo cáo kết ca |
| `ShiftController` | Danh mục ca làm việc |
| `StaffRegistrationController` | Đăng ký ca của nhân viên |
| `SupplierController` | Nhà cung cấp |
| `UserController` | Người dùng, xác thực, phân quyền |

## Yêu cầu hệ thống

- .NET SDK 8.0 trở lên
- MySQL Server (hoặc WampServer khi chạy local)
- Tesseract tessdata (thư mục `tessdata/` với dữ liệu ngôn ngữ `vie` và `eng`) đặt tại thư mục gốc project để dùng chức năng OCR hóa đơn

## Cài đặt & chạy dự án

1. **Clone dự án**
   ```bash
   git clone <repository-url>
   cd LuanVanTotNghiep
   ```

2. **Cấu hình `appsettings.json`**
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=localhost;Port=3306;Database=luanvantotnghiep;User=root;Password="
     },
     "Jwt": {
       "Issuer": "your-issuer",
       "Audience": "your-audience",
       "Key": "your-secret-key-min-32-chars"
     }
   }
   ```
   > Có thể dùng `dotnet user-secrets` thay vì lưu trực tiếp thông tin nhạy cảm trong `appsettings.json`.

3. **Khôi phục package & tạo database**
   ```bash
   dotnet restore
   dotnet ef database update
   ```

4. **Chạy ứng dụng**
   ```bash
   dotnet run
   ```
   Ứng dụng sẽ kiểm tra kết nối MySQL khi khởi động và in log kết quả kết nối ra console.

5. **Truy cập Swagger UI**
   Mặc định Swagger UI được cấu hình ở route gốc (`/`) khi chạy ở môi trường Development.

## Kết nối Frontend

Backend đã cấu hình CORS policy `AllowReactDev` cho phép frontend React chạy tại:
- `http://localhost:5173`
- `http://127.0.0.1:5173`

(phù hợp với Vite dev server mặc định)

## Phân quyền (Roles)

| Role | Mô tả |
|---|---|
| `ADMIN` | Quản trị toàn hệ thống (nhà cung cấp, người dùng...) |
| `MANAGER` | Quản lý chi nhánh: duyệt phiếu kho, đợt đăng ký ca, kết ca... |
| `STAFF` | Nhân viên: đăng ký ca, chấm công, lập báo cáo kết ca |

## Ghi chú

- Các worker nền (`SchedulePeriodDeadlineWorker`, `MissingCheckoutWorker`) chạy định kỳ để tự động hóa việc khóa đợt đăng ký quá hạn và xử lý chấm công thiếu checkout.
- Chức năng OCR hóa đơn dùng để hỗ trợ nhập nhanh phiếu nhập kho từ ảnh hóa đơn giấy, cần cấu hình đúng thư mục `tessdata`.
