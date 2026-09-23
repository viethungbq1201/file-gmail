import { X } from "@phosphor-icons/react";

interface ImagePreviewProps {
  src: string;
  fileName: string;
  onClose: () => void;
}

export function ImagePreview({ src, fileName, onClose }: ImagePreviewProps) {
  return (
    <div
      className="fixed inset-0 z-50 flex flex-col items-center justify-center bg-black/85 p-4 backdrop-blur-md pt-[max(1rem,env(safe-area-inset-top))] pb-[max(1rem,env(safe-area-inset-bottom))]"
      role="dialog"
      aria-modal="true"
      aria-label={`Xem trước ${fileName}`}
      onClick={onClose}
      onKeyDown={(event) => {
        if (event.key === "Escape") onClose();
      }}
    >
      <div
        className="relative flex max-h-full w-full max-w-[560px] flex-col items-center"
        onClick={(event) => event.stopPropagation()}
      >
        <div className="mb-2.5 flex w-full items-center justify-between">
          <p className="mono truncate pr-4 text-xs font-medium text-white/80" title={fileName}>
            {fileName}
          </p>
          <button
            type="button"
            className="btn btn-ghost h-9 w-9 shrink-0 rounded-full bg-white/10 p-0 text-white hover:bg-white/20 active:scale-95 focusable"
            onClick={onClose}
            aria-label="Đóng xem trước"
          >
            <X size={18} weight="bold" />
          </button>
        </div>
        <div className="flex max-h-[75dvh] w-full items-center justify-center overflow-hidden rounded-xl border border-white/10 bg-black/40">
          <img
            src={src}
            alt={`Xem trước ${fileName}`}
            className="max-h-[75dvh] max-w-full object-contain"
          />
        </div>
      </div>
    </div>
  );
}