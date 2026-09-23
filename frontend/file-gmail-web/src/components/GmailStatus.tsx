import { CircleNotch, Repeat, WarningCircle } from "@phosphor-icons/react";

export interface GmailStatusData {
  connected: boolean;
  recipient?: string;
  sender?: string;
}

interface GmailStatusProps {
  status: GmailStatusData | null;
  loading: boolean;
  error: boolean;
  onConnect: () => void;
  onRetry: () => void;
}

export function GmailStatus({ status, loading, error, onConnect, onRetry }: GmailStatusProps) {
  if (loading) {
    return (
      <div className="flex items-center gap-2 text-(--text-2)">
        <span className="flex items-center gap-1.5 text-[13px]">
          <CircleNotch size={14} className="animate-spin" />
          Đang kiểm tra Gmail…
        </span>
      </div>
    );
  }

  if (error || !status) {
    return (
      <button
        type="button"
        className="btn btn-ghost btn-sm focusable"
        onClick={onRetry}
        aria-label="Thử kiểm tra lại trạng thái Gmail"
      >
        <WarningCircle size={15} />
        Không kiểm tra được
      </button>
    );
  }

  if (status.connected) {
    return (
      <div className="flex items-center gap-2 text-[13px] font-medium text-(--success)">
        <span className="status-dot status-dot-connected" aria-hidden="true" />
        Gmail đã kết nối
      </div>
    );
  }

  return (
    <button
      type="button"
      className="btn btn-secondary btn-sm focusable"
      onClick={onConnect}
      aria-label="Kết nối Gmail"
    >
      <Repeat size={15} weight="bold" />
      Kết nối Gmail
    </button>
  );
}