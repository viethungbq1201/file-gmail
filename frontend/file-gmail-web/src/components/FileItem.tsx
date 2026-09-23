import { FileImage, FilePdf, Trash } from "@phosphor-icons/react";
import type { SelectedFile } from "../types/types";
import { formatBytes } from "../utils/format";

interface FileItemProps {
  item: SelectedFile;
  disabled: boolean;
  onRemove: (id: string) => void;
  onPreview: (url: string, name: string) => void;
}

function extensionOf(name: string): string {
  return name.slice(name.lastIndexOf(".")).toLowerCase();
}

export function FileItem({ item, disabled, onRemove, onPreview }: FileItemProps) {
  const ext = extensionOf(item.file.name);
  const isPdf = ext === ".pdf";

  return (
    <li className="file-row group">
      {item.previewUrl ? (
        <button
          type="button"
          className="focusable flex h-10 w-10 shrink-0 items-center justify-center overflow-hidden rounded-lg border border-(--border) transition-all hover:border-(--accent) hover:opacity-90 active:scale-95"
          onClick={() => onPreview(item.previewUrl!, item.file.name)}
          aria-label={`Xem trước ${item.file.name}`}
          title="Xem trước ảnh"
        >
          <img
            src={item.previewUrl}
            alt=""
            className="h-full w-full object-cover"
            loading="lazy"
          />
        </button>
      ) : (
        <span className="file-icon h-10 w-10 shrink-0 rounded-lg" aria-hidden="true">
          {isPdf ? <FilePdf size={22} weight="fill" /> : <FileImage size={22} weight="fill" />}
        </span>
      )}

      <div className="min-w-0 flex-1 px-1">
        <p className="mono truncate text-[13px] font-medium leading-tight text-(--text)" title={item.file.name}>
          {item.file.name}
        </p>
        <div className="mt-0.5 flex items-center gap-2">
          <span className="text-[12px] text-(--text-3)">{formatBytes(item.file.size)}</span>
          <span className="rounded bg-(--surface-2) px-1.5 py-0.2 text-[10px] font-medium uppercase text-(--text-2)">
            {ext.replace(".", "")}
          </span>
        </div>
      </div>

      <button
        type="button"
        className="btn btn-danger focusable"
        onClick={() => onRemove(item.id)}
        disabled={disabled}
        aria-label={`Xoá ${item.file.name}`}
        title="Xoá file"
      >
        <Trash size={18} />
      </button>
    </li>
  );
}