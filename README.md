# FileGmail

Web ứng dụng cho phép người dùng chọn ảnh (JPG/PNG/WEBP) hoặc file PDF, nhập tiêu đề & nội dung, rồi gửi các file đó đến địa chỉ Gmail cố định thông qua Gmail API (chỉ dùng scope `gmail.send`). File không được lưu trữ trên máy chủ — chúng được gửi đi ngay và loại bỏ sau khi hoàn tất.

## Kiến trúc

| Thành phần | Công nghệ | Thư mục |
|---|---|---|
| Backend API | ASP.NET Core (.NET 8) | `src/FileGmail.Api` |
| Frontend | React 19 + TypeScript + Vite + Tailwind CSS 4 | `frontend/file-gmail-web` |
| Tests | xUnit (unit + integration WebApplicationFactory) | `tests/FileGmail.Tests` |

Backend phục vụ luôn bản build tĩnh của frontend (thư mục `wwwroot`) nên toàn hệ thống chạy gọn trong **một container**.

## Yêu cầu chức năng đã triển khai

- Validate file theo extension + content-type + magic bytes (chặn file giả mạo): chỉ chấp nhận `.jpg/.jpeg/.png/.webp/.pdf`.
- Giới hạn kích thước từng file (mặc định 20 MB) và tổng dung lượng (mặc định 25 MB).
- Email gửi qua Gmail API dạng MIME multipart/mixed tự dựng (subject mã hoá RFC 2047, tên file Unicode qua `filename*=UTF-8''`).
- Người nhận cố định cấu hình từ máy chủ (`GMAIL_RECIPIENT_EMAIL`), UI hiển thị dạng chỉ đọc.
- Đăng nhập Gmail qua OAuth 2.0 (authorization code flow), token lưu dạng file cục bộ hoặc cấu hình qua biến môi trường (stateless cho production).
- Cổng `/api/gmail/send` được bảo vệ bằng mã truy cập (`X-App-Key`), validate request trước khi gọi Gmail.
- Giới hạn tốc độ gửi theo IP (mặc định 10 yêu cầu/phút).
- CORS giới hạn origin frontend qua `ALLOWED_ORIGINS`.

## Chạy local

### 1. Backend

Đặt file OAuth client Google (tên `credentials.json`) vào **thư mục repository root** (hoặc `/project/src/FileGmail.Api`, hoặc dùng biến `GMAIL_CREDENTIALS_JSON`). File này **không được commit** (đã nằm trong `.gitignore`).

```bash
dotnet restore FileGmail.slnx
dotnet build FileGmail.slnx
dotnet run --project src/FileGmail.Api
# Backend chạy tại http://localhost:5244
```

### 2. Frontend (dev)

```bash
cd frontend/file-gmail-web
npm install
npm run dev
# Frontend chạy tại http://localhost:5173, proxy API về backend
```

### 3. Cấu hình lần đầu

Trong file `src/FileGmail.Api/appsettings.json` đặt:

```json
{
  "Gmail": {
    "SenderEmail": "banggia@anphatcorp.vn",
    "RecipientEmail": "quynhtrang.95@gmail.com",
    "FrontendBaseUrl": "http://localhost:5173",
    "RedirectUri": "http://localhost:5244/api/gmail/oauth/callback"
  },
  "Security": {
    "AccessKey": "MOTCHUOIKY",
    "AllowedOrigins": "http://localhost:5173"
  }
}
```

Luồng kết nối Gmail: mở trang web → bấm "Kết nối Gmail" → Google xác nhận → tự động quay về ứng dụng với trạng thái đã kết nối.

Luồng gửi file: chọn file (kéo thả hoặc bấm chọn) → nhập tiêu đề/nội dung → bấm **Gửi…** → màn hình hiển thị trạng thái gửi thành công/thất bại.

## Biến môi trường

Chạy trong Production, mọi cấu hình có thể ghi đè bằng biến môi trường:

| Biến | Mô tả | Mặc định |
|---|---|---|
| `GMAIL_CREDENTIALS_JSON` | Nội dung JSON credentials (thay cho file) | — |
| `GMAIL_CREDENTIALS_PATH` | Đường dẫn file credentials | `credentials.json` |
| `GMAIL_TOKEN_JSON` | Nội dung JSON token (thay cho file) | — |
| `GMAIL_TOKEN_PATH` | Đường dẫn file token | `token.json` |
| `GMAIL_REDIRECT_URI` | Redirect URI của OAuth | `http://localhost:5244/api/gmail/oauth/callback` |
| `GMAIL_SENDER_EMAIL` | Địa chỉ gửi (Google account đã cấp quyền) | — |
| `GMAIL_RECIPIENT_EMAIL` | Người nhận cố định | — |
| `GMAIL_FRONTEND_URL` | URL frontend để redirect sau OAuth | `http://localhost:5173` |
| `APP_ACCESS_KEY` | Mã truy cập cho `/api/gmail/send` và `/api/gmail/access-key/verify` | rỗng (tắt bảo vệ, có cảnh báo) |
| `ALLOWED_ORIGINS` | Danh sách origin cho phép (phân tách bằng dấu phẩy) | mọi origin (chỉ dev) |
| `MAX_FILE_SIZE_MB` | Dung lượng tối đa mỗi file | `20` |
| `MAX_TOTAL_SIZE_MB` | Tổng dung lượng tối đa | `25` |
| `RATE_LIMIT_PER_MINUTE` | Giới hạn yêu cầu `/api/gmail/send` theo IP | `10` |

## API

| Endpoint | Phương thức | Mô tả | Bảo vệ |
|---|---|---|---|
| `/api/health` | GET | Kiểm tra trạng thái | — |
| `/api/gmail/status` | GET | Trạng thái OAuth + địa chỉ gửi/nhận | — |
| `/api/gmail/access-key/verify` | GET | Kiểm tra mã truy cập | `X-App-Key` |
| `/api/gmail/auth` | GET | Bắt đầu OAuth (trả URL chuyển hướng) | — |
| `/api/gmail/oauth/callback` | GET | Nhận code xác nhận, lưu token, chuyển hướng FE | — |
| `/api/gmail/send` | POST | Gửi file multipart (`files`, `subject`, `body`) | `X-App-Key` + rate limit |

Mặc định mọi response dạng `{ "success": true, "message": "...", "data": ... }`; toàn bộ thông báo lỗi bằng tiếng Việt.

## Chạy tests

```bash
dotnet test FileGmail.slnx
```

Gồm 23 tests: validator (magic bytes, dung lượng, extension), builder MIME (subject RFC 2047, tên file Unicode, base64 wrap), và integration API (health, status, access key, gửi file sai định dạng).

## Triển khai Render (một container)

1. Đưa code lên GitHub. Trên [Render Dashboard](https://dashboard.render.com) tạo **New → Web Service → Connect repo**.
2. Render tự nhận `render.yaml`: chọn **New → Blueprint** (dùng `render.yaml` đã có) để khởi tạo service `file-gmail` với Dockerfile kèm sẵn.
3. Trong mục **Environment** của service, đặt các biến bắt buộc (đánh dấu `sync: false` trong Blueprint):
   - `APP_ACCESS_KEY` — mã truy cập tuỳ ý cho môi trường production.
   - `GMAIL_SENDER_EMAIL` — Gmail account đã tạo OAuth.
   - `GMAIL_RECIPIENT_EMAIL` — người nhận cố định.
   - `GMAIL_CREDENTIALS_JSON` — nội dung `credentials.json` (hoặc đặt file qua `GMAIL_CREDENTIALS_PATH` nếu dùng volume).
   - `GMAIL_TOKEN_JSON` — token sau lần kết nối OAuth đầu tiên (ứng dụng stateless, không lưu file).
   - `GMAIL_REDIRECT_URI` — `https://<service-url>/api/gmail/oauth/callback`.
   - `GMAIL_FRONTEND_URL` — `https://<service-url>`.
   - `ALLOWED_ORIGINS` — `https://<service-url>`.
4. Health check tự động dùng `/api/health`.

> Lưu ý OAuth: token được đăng ký theo Redirect URI + client. Sau khi deploy, thực hiện đăng nhập Gmail lần đầu (truy cập URL gửi) rồi lấy giá trị `token.json` đưa vào `GMAIL_TOKEN_JSON` để container stateless có thể gửi mail.