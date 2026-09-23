import { TrashSimple } from "@phosphor-icons/react";
import type { SelectedFile } from "../types/types";
import { formatBytes } from "../utils/format";
import { FileItem } from "./FileItem";

interface FileListProps {
  files: SelectedFile[];
  totalSize: number;
  disabled: boolean;
  onRemove: (id: string) => void;
  onClearAll: () => void;
  onPreview: (url: string, name: string) => void;
}

export function FileList({
  files,
  totalSize,
  disabled,
  onRemove,
  onClearAll,
  onPreview,
}: FileListProps) {
  if (files.length === 0) return null;

  return (
    <section aria-label="Danh sách file đã chọn" className="flex flex-col gap-2">
      <div className="flex items-center justify-between px-1">
        <div className="flex items-center gap-2">
          <span className="text-[13px] font-semibold text-(--text)">
            {files.length} tệp đã chọn
          </span>
          <span className="mono rounded bg-(--surface-2) px-2 py-0.5 text-[11px] font-normal text-(--text-2)">
            {formatBytes(totalSize)}
          </span>
        </div>
        <button
          type="button"
          className="btn btn-ghost btn-sm text-[12px] text-(--text-2) transition-colors hover:text-(--error) focusable sm:text-[13px]"
          onClick={onClearAll}
          disabled={disabled}
        >
          <TrashSimple size={14} className="sm:size-[15px]" />
          <span>Xoá tất cả</span>
        </button>
      </div>
      <ul className="card divide-y divide-(--border) overflow-hidden">
        {files.map((item) => (
          <FileItem
            key={item.id}
            item={item}
            disabled={disabled}
            onRemove={onRemove}
            onPreview={onPreview}
          />
        ))}
      </ul>
    </section>
  );
}