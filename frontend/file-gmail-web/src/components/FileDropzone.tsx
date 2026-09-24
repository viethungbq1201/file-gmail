import { useRef, useState } from "react";
import type { DragEvent, ChangeEvent } from "react";
import { UploadSimple } from "@phosphor-icons/react";

interface FileDropzoneProps {
  onFiles: (files: File[]) => void;
  disabled: boolean;
}

export function FileDropzone({ onFiles, disabled }: FileDropzoneProps) {
  const inputRef = useRef<HTMLInputElement>(null);
  const [isDragging, setIsDragging] = useState(false);

  const handleInput = (event: ChangeEvent<HTMLInputElement>) => {
    const incoming = event.target.files;
    if (incoming) {
      onFiles(Array.from(incoming));
    }
    event.target.value = "";
  };

  const handleDragOver = (event: DragEvent<HTMLDivElement>) => {
    event.preventDefault();
    if (!disabled) setIsDragging(true);
  };

  const handleDragLeave = (event: DragEvent<HTMLDivElement>) => {
    event.preventDefault();
    setIsDragging(false);
  };

  const handleDrop = (event: DragEvent<HTMLDivElement>) => {
    event.preventDefault();
    setIsDragging(false);
    if (disabled) return;

    const incoming = event.dataTransfer.files;
    if (incoming) {
      onFiles(Array.from(incoming));
    }
  };

  return (
    <div
      className="dropzone focusable"
      role="button"
      tabIndex={0}
      data-dragover={isDragging}
      onClick={() => inputRef.current?.click()}
      onKeyDown={(event) => {
        if (event.key === "Enter" || event.key === " ") {
          event.preventDefault();
          inputRef.current?.click();
        }
      }}
      onDragOver={handleDragOver}
      onDragLeave={handleDragLeave}
      onDrop={handleDrop}
      aria-label="Chọn hoặc kéo thả file ảnh hoặc PDF"
    >
      <input
        ref={inputRef}
        type="file"
        className="hidden"
        accept=".jpg,.jpeg,.png,.webp,.pdf,.docx,.xlsx,image/jpeg,image/png,image/webp,application/pdf,application/vnd.openxmlformats-officedocument.wordprocessingml.document,application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        multiple
        onChange={handleInput}
        disabled={disabled}
        tabIndex={-1}
      />
      <span
        className="mx-auto mb-2.5 flex h-11 w-11 items-center justify-center rounded-full bg-(--accent-soft) text-(--accent) sm:mb-3 sm:h-12 sm:w-12"
        aria-hidden="true"
      >
        <UploadSimple size={22} weight="bold" className="sm:size-6" />
      </span>
      <p className="text-[14px] font-semibold text-(--text) sm:text-[15px]">
        <span className="hidden sm:inline">Kéo thả file vào đây hoặc nhấn để chọn</span>
        <span className="sm:hidden">Chạm để chọn file hoặc chụp ảnh</span>
      </p>
      <p className="mx-auto mt-1 max-w-[40ch] text-[12px] text-(--text-2) sm:text-[13px]">
        Hỗ trợ JPG, PNG, WEBP, PDF, DOCX, XLSX · Tối đa 20 MB/file · Tổng 25 MB
      </p>
      <span className="btn btn-secondary btn-sm pointer-events-none mt-3.5 px-4 font-medium sm:mt-4">
        Chọn file từ thiết bị
      </span>
    </div>
  );
}