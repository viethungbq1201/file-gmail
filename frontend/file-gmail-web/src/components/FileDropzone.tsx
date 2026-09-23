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
        accept=".jpg,.jpeg,.png,.webp,.pdf,image/jpeg,image/png,image/webp,application/pdf"
        multiple
        onChange={handleInput}
        disabled={disabled}
        tabIndex={-1}
      />
      <span
        className="mx-auto mb-3 flex h-12 w-12 items-center justify-center rounded-full bg-(--accent-soft) text-(--accent)"
        aria-hidden="true"
      >
        <UploadSimple size={24} weight="bold" />
      </span>
      <p className="text-[15px] font-semibold text-(--text)">Chọn hoặc kéo thả file vào đây</p>
      <p className="mt-1 text-[13px] text-(--text-2)">
        Hỗ trợ ảnh JPG, PNG, WEBP và PDF · tối đa 20 MB/file · tổng tối đa 25 MB
      </p>
      <span className="btn btn-secondary btn-sm mt-4">Chọn file</span>
    </div>
  );
}