import { CheckCircle, X, XCircle } from "@phosphor-icons/react";

export type ToastType = "success" | "error";

interface ToastProps {
  message: string;
  type: ToastType;
  onClose: () => void;
}

export function Toast({ message, type, onClose }: ToastProps) {
  const isSuccess = type === "success";

  return (
    <div
      className="toast fixed bottom-[calc(1rem+env(safe-area-inset-bottom,0px))] left-1/2 -translate-x-1/2 z-50 flex w-max max-w-[calc(100vw-32px)] items-center gap-3 bg-(--surface) text-(--text)"
      role="status"
      aria-live="polite"
    >
      {isSuccess ? (
        <CheckCircle size={20} className="shrink-0 text-(--success)" aria-hidden="true" />
      ) : (
        <XCircle size={20} className="shrink-0 text-(--error)" aria-hidden="true" />
      )}
      <p className="text-sm font-medium">{message}</p>
      <button
        type="button"
        className="btn btn-ghost btn-sm ml-1 h-8 w-8 shrink-0 p-0 focusable"
        onClick={onClose}
        aria-label="Đóng thông báo"
      >
        <X size={16} />
      </button>
    </div>
  );
}