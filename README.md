# LandingCms

Landing page một trang viết bằng ASP.NET Core MVC (.NET 8), SQLite và ASP.NET Core Identity.

## Chức năng

- Landing page responsive với nội dung mẫu.
- Quản trị từng section: thêm, sửa, xóa, bật/tắt và sắp xếp.
- Cấu hình tên website, SEO, liên hệ, màu thương hiệu và footer.
- Nhiều tài khoản, ba vai trò:
  - `SuperAdministrator`: chọn template và quản trị toàn hệ thống.
  - `Administrator`: quản lý cấu hình, tài khoản và nội dung.
  - `Editor`: quản lý nội dung landing page.
- Template engine quan hệ: `PageTemplate`, `SectionDefinition`, `TemplateSection`, `SectionContent`.
- Hai template mẫu `Corporate` và `Minimal`; nội dung dùng chung theo `SectionKey` khi đổi template.
- SQLite được tự động tạo và nạp dữ liệu mẫu trong lần chạy đầu tiên.

## Chạy dự án

Yêu cầu .NET 8 SDK. Tại thư mục `LandingCms`, thiết lập tài khoản quản trị đầu tiên rồi chạy:

### PowerShell

```powershell
$env:LANDINGCMS_ADMIN_USERNAME="sadmin"
$env:LANDINGCMS_ADMIN_EMAIL="admin@example.com" # không bắt buộc
$env:LANDINGCMS_ADMIN_PASSWORD="ThayMatKhau!2026"
dotnet restore
dotnet run
```

### Linux / macOS

```bash
export LANDINGCMS_ADMIN_USERNAME="sadmin"
export LANDINGCMS_ADMIN_EMAIL="admin@example.com" # không bắt buộc
export LANDINGCMS_ADMIN_PASSWORD="ThayMatKhau!2026"
dotnet restore
dotnet run
```

Mở URL được hiển thị trong terminal. Khu vực quản trị ở `/admin`.

Khi nâng cấp từ phiên bản cũ, ứng dụng tự tạo các bảng template còn thiếu và chuyển dữ liệu `LandingSections` hiện tại sang `SectionContents`; không cần xóa file SQLite.

## Triển khai IIS

1. Cài .NET 8 Hosting Bundle trên Windows Server.
2. Chạy `dotnet publish -c Release -o ./publish`.
3. Tạo Application Pool với `.NET CLR Version = No Managed Code`.
4. Cho tài khoản Application Pool quyền Modify trên thư mục `App_Data`.
5. Cấu hình hai biến môi trường quản trị trước lần khởi động đầu tiên, sau đó có thể gỡ chúng.

Không lưu mật khẩu quản trị trong `appsettings.json` hoặc commit vào Git.
