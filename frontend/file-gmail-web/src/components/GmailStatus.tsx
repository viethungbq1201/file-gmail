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
      <div className="flex items-center gap-1.5 text-(--text-2)">
        <CircleNotch size={14} className="animate-spin text-(--accent)" />
        <span className="text-[12px] sm:text-[13px]">
          <span className="hidden sm:inline">Đang kiểm tra Gmail…</span>
          <span className="sm:hidden">Đang kiểm tra…</span>
        </span>
      </div>
    );
  }

  if (error || !status) {
    return (
      <button
        type="button"
        className="btn btn-ghost btn-sm h-8 px-2.5 text-[12px] text-(--warning) focusable sm:h-9 sm:px-3 sm:text-[13px]"
        onClick={onRetry}
        aria-label="Thử kiểm tra lại trạng thái Gmail"
      >
        <WarningCircle size={15} />
        <span>Thử lại kết nối</span>
      </button>
    );
  }

  if (status.connected) {
    return (
      <div className="flex items-center gap-1.5 rounded-full bg-(--surface-2) px-2.5 py-1 text-[12px] font-medium text-(--success) sm:text-[13px]">
        <span className="status-dot status-dot-connected" aria-hidden="true" />
        <span className="hidden sm:inline">Gmail đã kết nối</span>
        <span className="sm:hidden">Đã kết nối</span>
      </div>
    );
  }

  return (
    <button
      type="button"
      className="btn btn-secondary btn-sm h-8 px-3 text-[12px] focusable sm:h-9 sm:px-4 sm:text-[13px]"
      onClick={onConnect}
      aria-label="Kết nối Gmail"
    >
      <Repeat size={14} weight="bold" />
      <span>Kết nối Gmail</span>
    </button>
  );
}