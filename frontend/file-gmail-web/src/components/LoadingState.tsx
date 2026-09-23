import { CircleNotch } from "@phosphor-icons/react";

interface LoadingStateProps {
  message: string;
}

export function LoadingState({ message }: LoadingStateProps) {
  return (
    <div className="flex items-center justify-center gap-2 py-16 text-(--text-2)" role="status">
      <CircleNotch size={18} className="animate-spin" aria-hidden="true" />
      <span className="text-sm">{message}</span>
    </div>
  );
}