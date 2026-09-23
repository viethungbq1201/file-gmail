import { PaperPlaneTilt } from "@phosphor-icons/react";

interface SendButtonProps {
  fileCount: number;
  disabled: boolean;
  sending: boolean;
}

export function SendButton({ fileCount, disabled, sending }: SendButtonProps) {
  const label = fileCount > 0 ? `Gửi ${fileCount} file` : "Gửi file";

  return (
    <button type="submit" className="btn btn-primary btn-lg btn-block" disabled={disabled || sending}>
      {sending ? (
        <>
          <span className="spinner" aria-hidden="true" />
          Đang gửi…
        </>
      ) : (
        <>
          <PaperPlaneTilt size={18} weight="bold" aria-hidden="true" />
          {label}
        </>
      )}
    </button>
  );
}