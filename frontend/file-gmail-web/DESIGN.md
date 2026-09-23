# DESIGN.md — FileGmail Web

Giao diện web là **công cụ gửi tài liệu single-task** (file transfer utility), không phải landing page, không phải dashboard dữ liệu.

## Design Read

- Type: tiện ích chuyển file tới Gmail, một task duy nhất: chọn file → nhập thông tin → gửi.
- Người dùng: nội bộ/cá nhân, thao tác nhanh trên mobile lẫn desktop.
- Ngôn ngữ: hiện đại, sạch, đáng tin, không kiểu cách.
- Dials: VARIANCE 5 · MOTION 3 · DENSITY 4 (tập trung vào độ rõ trạng thái, không cầu kỳ).

## Color tokens

| Token          | Light            | Dark             | Dùng cho                    |
|----------------|------------------|------------------|-----------------------------|
| `--bg`         | `#fafafa`        | `#161616`        | Nền trang                   |
| `--surface`    | `#ffffff`        | `#1f1f1f`        | Card, form, header          |
| `--surface-2`  | `#f4f4f5`        | `#262626`        | Dropzone, hint, chip        |
| `--border`     | `#e4e4e7`        | `#3a3a3a`        | Viền các vùng               |
| `--text`       | `#18181b`        | `#f4f4f5`        | Văn bản chính               |
| `--text-2`     | `#52525b`        | `#a1a1aa`        | Văn bản phụ, placeholder    |
| `--accent`     | `#0f766e`        | `#2dd4bf`        | CTA chính (teal)            |
| `--on-accent`  | `#ffffff`        | `#042f2e`        | Chữ trên accent             |
| `--success`    | `#16a34a`        | `#4ade80`        | Thành công                  |
| `--error`      | `#dc2626`        | `#f87171`        | Lỗi                         |
| `--warning`    | `#d97706`        | `#fbbf24`        | Cảnh báo                    |

Chỉ 1 accent (teal). Màu trạng thái chỉ dùng cho trạng thái thực tế, không dùng trang trí.
Bảng màu nhất quán toàn trang, không xen màu lạ.

## Type

- Display/Body: `ui-sans-serif, system-ui, -apple-system, "Segoe UI", Roboto, "Helvetica Neue", Arial, sans-serif`.
- Data/metadata (filename, size, trạng thái kỹ thuật): `ui-monospace, SFMono-Regular, Menlo, Consolas, monospace`.
- Scale: title 24/32px, body 14-16px, hint/label 12-13px, metadata 12-13px mono.
- Không dùng web font ngoài (self-hosted/generic stack) để load nhanh.

## Shape & elevation

- Nút chính: pill (`border-radius: 999px`), tinggi 44px.
- Card/vùng: radius 12px.
- Input: radius 8px.
- Shadow: rất nhẹ `0 1px 2px rgb(0 0 0 / 0.04)`, viền `1px` là cách phân tầng chính.
- Không dùng gradient, không dùng nhiều shadow, không glassmorphism.

## Layout

- Container: `max-width: 560px`, cột đơn, căn giữa (tool single-column).
- Desktop: section ngang dọc rõ ràng Header → Dropzone → File list → Email info → Send.
- Mobile: giữ luồng dọc, nút Gửi full-width dễ bấm.
- Breakpoints: `sm 640`, `md 768`.

## Component pattern

- Header: brand + trạng thái kết nối Gmail (chấm không/được kết nối).
- Dropzone: vùng viền dashed, hover/border highlight, hỗ trợ click + drag & drop.
- FileList: mỗi file là 1 row thẻ gọn (icon loại file, name, size, nút xoá icon-only có aria-label).
- Trạng thái: loading → spinner inline nhỏ + text; success/error → banner màu trạng thái.
- Nút icon-only luôn có `aria-label`.

## States

- Initial / Empty / Files selected / Validation error / Gmail disconnected / Gmail connected / Sending / Success / Error / Network error.
- Không hiển thị exception thô, luôn hiển thị thông báo tiếng Việt cụ thể.

## Motion

- Chỉ dùng hover/active (`transition 0.2s ease`), spinner khi sending, fade banner ngắn.
- Tôn trọng `prefers-reduced-motion`.