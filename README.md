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
- Mỗi tài khoản tự đổi mật khẩu; SuperAdmin có thể reset Administrator/Editor, Administrator có thể reset Editor.
- Form liên hệ có chống spam cơ bản, lưu SQLite và gửi email SMTP tới email trong cấu hình website.
- Setting keys do developer đồng bộ; Administrator/SuperAdmin cập nhật values theo template đang sử dụng.
- Upload logo, ảnh chính của section và nhiều background cho Hero (PNG/JPEG/WebP, tối đa 5 MB mỗi file).
- Ảnh upload được kiểm tra tối đa 30 triệu pixel, tự sửa chiều EXIF, resize theo mục đích sử dụng và tối ưu thành WebP; PNG của logo/favicon được giữ để bảo toàn nền trong suốt.
- Schema SQLite được quản lý bằng EF Core migrations; ứng dụng tự migrate và nạp dữ liệu mẫu khi khởi động.

## Chạy dự án

Yêu cầu .NET 8 SDK. Tại thư mục `LandingCms`, thiết lập tài khoản quản trị đầu tiên rồi chạy:

### PowerShell

```powershell
$env:LANDINGCMS_ADMIN_USERNAME="sadmin"
$env:LANDINGCMS_ADMIN_EMAIL="admin@example.com" # không bắt buộc
$env:LANDINGCMS_ADMIN_PASSWORD="<strong-password>"
dotnet restore
dotnet run
```

### Linux / macOS

```bash
export LANDINGCMS_ADMIN_USERNAME="sadmin"
export LANDINGCMS_ADMIN_EMAIL="admin@example.com" # không bắt buộc
export LANDINGCMS_ADMIN_PASSWORD="<strong-password>"
dotnet restore
dotnet run
```

Mở URL được hiển thị trong terminal. Khu vực quản trị ở `/admin`.

Khi model database thay đổi, developer tạo migration mới và commit cùng source. Không xóa file SQLite trên production.

```bash
dotnet ef migrations add TenMigration
dotnet ef database update
```

## Cấu hình gửi email liên hệ

Email nhận liên hệ được lấy từ `Cấu hình website > Email`. Thông tin SMTP nên đặt bằng biến môi trường:

```text
Smtp__Host=smtp.example.com
Smtp__Port=587
Smtp__EnableSsl=true
Smtp__UserName=mailer@example.com
Smtp__Password=your-smtp-password
Smtp__FromEmail=mailer@example.com
Smtp__FromName=Website Contact
```

Không commit mật khẩu SMTP vào `appsettings.json`. Nếu gửi email thất bại, nội dung vẫn được lưu và có thể xem trong `Quản trị > Liên hệ khách hàng`.

## Template settings

Các key được developer khai báo trong `DbInitializer.SeedSettingsAsync`. Ứng dụng chỉ bổ sung/cập nhật definition và không ghi đè value mà quản trị viên đã nhập. Mỗi template được liên kết với các key qua `TemplateSetting`.

Administrator và SuperAdministrator cập nhật giá trị tại `Quản trị > Setting values`. Trang chủ tải một dictionary cho template đang kích hoạt. Các key mẫu:

```text
social.facebook_url
social.zalo_url
analytics.ga_measurement_id
analytics.gtm_container_id
branding.logo_primary
branding.logo_light
branding.favicon
```

Admin không thể tạo, đổi tên hoặc xóa key. Tính năng custom key dành cho page builder tương lai chưa được bật.

## Section schema và HTML Editor

Developer định nghĩa editor cho từng field trong `SectionDefinition.SchemaJson`; ứng dụng đồng bộ các schema mẫu từ `DbInitializer`. Ví dụ:

```json
{"fields":{"content":{"editor":"html","htmlPolicy":"RichContent"}}}
```

Các editor hiện hỗ trợ cho field `content` gồm `textarea`, `structured-list` và `html`. HTML policy (`BasicContent`, `RichContent`, `InlineOnly`) cùng danh sách tag/attribute an toàn được developer quản lý trong `ContentHtmlSanitizer`; Admin và Editor không thể thay đổi policy.

Trạng thái hiển thị của section được lưu tại `TemplateSection.IsEnabled`, vì vậy bật/tắt một section chỉ ảnh hưởng template tương ứng. Chỉ SuperAdministrator được thay đổi trạng thái này; Administrator và Editor chỉ cập nhật nội dung. `SectionDefinition.IsEnabled` là trạng thái available toàn hệ thống do developer quản lý; `SectionContent.IsPublished` được dành riêng cho quy trình draft/publish nội dung sau này.

SuperAdministrator quản lý cấu trúc từng template tại `Giao diện > Cấu hình section`: thêm nhiều instance từ definition có sẵn, đổi tên quản trị, bật/tắt, sắp xếp và gỡ section không bắt buộc. `SectionKey` được hệ thống tự sinh; SuperAdmin không thể sửa schema, view path hoặc HTML policy.

Developer có thể khai báo layout variant trong `SectionDefinition.SchemaJson`. Ví dụ definition `Content` cung cấp `image-left` và `image-right`; lựa chọn của từng instance được lưu trong `TemplateSection.SettingsJson` và chỉ được nhận các giá trị có trong schema.

## Triển khai IIS

1. Cài .NET 8 Hosting Bundle trên Windows Server.
2. Chạy `dotnet publish -c Release -o ./publish`.
3. Tạo Application Pool với `.NET CLR Version = No Managed Code`.
4. Cho tài khoản Application Pool quyền Modify trên hai thư mục `App_Data` và `wwwroot/uploads`.
5. Cấu hình hai biến môi trường quản trị trước lần khởi động đầu tiên, sau đó có thể gỡ chúng.

Không lưu mật khẩu quản trị trong `appsettings.json` hoặc commit vào Git.

## VNS Licensing

Production luôn kiểm tra license với VNS Licensing Server. Chỉ môi trường `Development` có thể bypass để developer chạy local. Cấu hình production bằng environment variables:

Client được cung cấp bởi NuGet package `VNS.Licensing.Client.AspNetCore`; VCMS không chứa
implementation licensing riêng.

```text
Licensing__ServerUrl=https://licensing.example.com/
Licensing__ProductCode=VCMS.LANDING
Licensing__LicenseKey=VCMSLAND-...
```

Không commit `LicenseKey`. License Server quyết định thời điểm kiểm tra kế tiếp và trả về
`canonicalUrl` cùng danh sách domain được phép; website không cấu hình canonical domain hay chu kỳ
kiểm tra. Website lưu cache tại `App_Data/license-cache.json`. Khi License Server tạm gián đoạn,
cache hợp lệ được dùng trong grace period. Trên domain không thuộc license, request public
`GET/HEAD` được redirect `308` về canonical URL; Admin và request ghi dữ liệu trả `403`.
