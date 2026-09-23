import { X } from "@phosphor-icons/react";

interface ImagePreviewProps {
  src: string;
  fileName: string;
  onClose: () => void;
}

export function ImagePreview({ src, fileName, onClose }: ImagePreviewProps) {
  return (
    <div
      className="fixed inset-0 z-50 flex flex-col items-center justify-center bg-black/80 p-4"
      role="dialog"
      aria-modal="true"
      aria-label={`Xem trước ${fileName}`}
      onClick={onClose}
      onKeyDown={(event) => {
        if (event.key === "Escape") onClose();
      }}
    >
      <div className="relative flex max-h-full max-w-full flex-col items-center" onClick={(event) => event.stopPropagation()}>
        <button
          type="button"
          className="btn btn-ghost mb-3 self-end text-(--surface) hover:bg-white/10 hover:text-(--surface)"
          onClick={onClose}
          aria-label="Đóng xem trước"
        >
          <X size={18} />
        </button>
        <img
          src={src}
          alt={`Xem trước ${fileName}`}
          className="max-h-[80dvh] max-w-full rounded-lg object-contain"
        />
        <p className="mt-3 break-all text-center text-sm text-(--surface)">{fileName}</p>
      </div>
    </div>
  );
}