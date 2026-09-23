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
          className="focusable flex h-9 w-9 shrink-0 items-center justify-center overflow-hidden rounded-lg"
          onClick={() => onPreview(item.previewUrl!, item.file.name)}
          aria-label={`Xem trước ${item.file.name}`}
        >
          <img
            src={item.previewUrl}
            alt=""
            className="h-full w-full object-cover"
            loading="lazy"
          />
        </button>
      ) : (
        <span className="file-icon" aria-hidden="true">
          {isPdf ? <FilePdf size={20} weight="fill" /> : <FileImage size={20} weight="fill" />}
        </span>
      )}

      <div className="min-w-0 flex-1">
        <p className="mono truncate text-[13px] font-medium text-(--text)" title={item.file.name}>
          {item.file.name}
        </p>
        <p className="text-[12px] text-(--text-3)">{formatBytes(item.file.size)}</p>
      </div>

      <button
        type="button"
        className="btn btn-danger focusable"
        onClick={() => onRemove(item.id)}
        disabled={disabled}
        aria-label={`Xoá ${item.file.name}`}
      >
        <Trash size={18} />
      </button>
    </li>
  );
}