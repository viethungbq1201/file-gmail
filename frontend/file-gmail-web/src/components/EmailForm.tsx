interface EmailFormProps {
  recipient: string;
  subject: string;
  body: string;
  disabled: boolean;
  onSubjectChange: (value: string) => void;
  onBodyChange: (value: string) => void;
}

export function EmailForm({
  recipient,
  subject,
  body,
  disabled,
  onSubjectChange,
  onBodyChange,
}: EmailFormProps) {
  return (
    <section aria-label="Thông tin email" className="card card-pad flex flex-col gap-4">
      <div className="field-group">
        <label htmlFor="recipient" className="label">
          Hộp thư tiếp nhận
        </label>
        <input
          id="recipient"
          type="text"
          className="input input-readonly select-all"
          value={recipient || "Đang tải hoặc chưa cấu hình trên máy chủ"}
          readOnly
          aria-readonly="true"
          tabIndex={-1}
        />
        <p className="help-text">Địa chỉ người nhận được thiết lập cố định từ hệ thống máy chủ.</p>
      </div>

      <div className="field-group">
        <div className="flex items-center justify-between">
          <label htmlFor="subject" className="label mb-0">
            Tiêu đề thư
          </label>
          <span className="text-[11px] text-(--text-3)">{subject.length}/200</span>
        </div>
        <input
          id="subject"
          type="text"
          className="input mt-1"
          value={subject}
          onChange={(event) => onSubjectChange(event.target.value)}
          placeholder="Ví dụ: Tài liệu bổ sung hồ sơ..."
          maxLength={200}
          disabled={disabled}
        />
      </div>

      <div className="field-group">
        <div className="flex items-center justify-between">
          <label htmlFor="body" className="label mb-0">
            Nội dung thư
          </label>
          <span className="text-[11px] text-(--text-3)">{body.length}/2000</span>
        </div>
        <textarea
          id="body"
          className="input mt-1 min-h-[96px] resize-none leading-relaxed sm:resize-y"
          value={body}
          onChange={(event) => onBodyChange(event.target.value)}
          placeholder="Ghi chú thêm về các file gửi đính kèm..."
          rows={3}
          maxLength={2000}
          disabled={disabled}
        />
      </div>
    </section>
  );
}