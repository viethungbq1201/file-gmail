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
          Người nhận
        </label>
        <input
          id="recipient"
          type="text"
          className="input input-readonly"
          value={recipient}
          readOnly
          aria-readonly="true"
          tabIndex={-1}
        />
        <p className="help-text">Địa chỉ người nhận được cấu hình cố định từ máy chủ.</p>
      </div>

      <div className="field-group">
        <label htmlFor="subject" className="label">
          Tiêu đề
        </label>
        <input
          id="subject"
          type="text"
          className="input"
          value={subject}
          onChange={(event) => onSubjectChange(event.target.value)}
          placeholder="File gửi từ Web App"
          maxLength={200}
          disabled={disabled}
        />
      </div>

      <div className="field-group">
        <label htmlFor="body" className="label">
          Nội dung
        </label>
        <textarea
          id="body"
          className="input min-h-[96px] resize-y"
          value={body}
          onChange={(event) => onBodyChange(event.target.value)}
          placeholder="Các file được gửi từ Web App."
          rows={4}
          maxLength={2000}
          disabled={disabled}
        />
      </div>
    </section>
  );
}