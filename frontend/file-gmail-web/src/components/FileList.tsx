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
    <section aria-label="Danh sách file đã chọn">
      <div className="mb-2 flex items-center justify-between">
        <p className="text-[13px] font-semibold text-(--text)">
          {files.length} file đã chọn
          <span className="mono ml-2 font-normal text-(--text-3)">
            {formatBytes(totalSize)}
          </span>
        </p>
        <button
          type="button"
          className="btn btn-ghost btn-sm focusable"
          onClick={onClearAll}
          disabled={disabled}
        >
          <TrashSimple size={15} />
          Xoá tất cả
        </button>
      </div>
      <ul className="card divide-y divide-(--border)">
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