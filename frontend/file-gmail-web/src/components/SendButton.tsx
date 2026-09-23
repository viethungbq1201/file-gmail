import { PaperPlaneTilt } from "@phosphor-icons/react";

interface SendButtonProps {
  fileCount: number;
  disabled: boolean;
  sending: boolean;
}

export function SendButton({ fileCount, disabled, sending }: SendButtonProps) {
  const label = fileCount > 0 ? `Gửi ${fileCount} tệp đính kèm` : "Gửi tài liệu";

  return (
    <button
      type="submit"
      className="btn btn-primary btn-lg btn-block shadow-sm transition-all focusable active:scale-[0.98]"
      disabled={disabled || sending}
      aria-label={sending ? "Đang gửi email..." : label}
    >
      {sending ? (
        <>
          <span className="spinner" aria-hidden="true" />
          <span>Đang gửi thư…</span>
        </>
      ) : (
        <>
          <PaperPlaneTilt size={18} weight="bold" aria-hidden="true" />
          <span>{label}</span>
        </>
      )}
    </button>
  );
}