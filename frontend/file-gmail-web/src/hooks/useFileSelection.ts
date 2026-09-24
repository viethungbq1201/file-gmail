import { useCallback, useEffect, useRef, useState } from "react";
import type { SelectedFile } from "../types/types";

const MAX_FILE_SIZE_BYTES = 20 * 1024 * 1024;
const MAX_TOTAL_SIZE_BYTES = 25 * 1024 * 1024;
const ALLOWED_EXTENSIONS = [".jpg", ".jpeg", ".png", ".webp", ".pdf", ".docx", ".xlsx"];
const IMAGE_EXTENSIONS = [".jpg", ".jpeg", ".png", ".webp"];

export interface AddFilesResult {
  added: number;
  rejected: number;
  errors: string[];
}

export function isSupportedFile(file: File): boolean {
  return ALLOWED_EXTENSIONS.some((ext) => file.name.toLowerCase().endsWith(ext));
}

export function useFileSelection() {
  const [files, setFiles] = useState<SelectedFile[]>([]);
  const filesRef = useRef<SelectedFile[]>([]);

  const applyFiles = useCallback((next: SelectedFile[]) => {
    filesRef.current = next;
    setFiles(next);
  }, []);

  const revokeUrl = useCallback((url: string | undefined) => {
    if (!url) return;
    URL.revokeObjectURL(url);
  }, []);

  const clearAll = useCallback(() => {
    for (const item of filesRef.current) {
      revokeUrl(item.previewUrl);
    }
    applyFiles([]);
  }, [applyFiles, revokeUrl]);

  useEffect(() => {
    return () => {
      for (const item of filesRef.current) {
        revokeUrl(item.previewUrl);
      }
    };
  }, [revokeUrl]);

  const addFiles = useCallback(
    (incoming: File[]): AddFilesResult => {
      const errors: string[] = [];
      let addedCount = 0;

      const current = filesRef.current;
      const nextTotal = current.reduce((sum, item) => sum + item.file.size, 0);
      const seen = new Set(current.map((item) => `${item.file.name}:${item.file.size}`));
      const next = [...current];

      for (const file of incoming) {
        const ext = file.name.slice(file.name.lastIndexOf(".")).toLowerCase();
        const key = `${file.name}:${file.size}`;

        if (!ALLOWED_EXTENSIONS.includes(ext)) {
          errors.push(`File "${file.name}" không được hỗ trợ.`);
          continue;
        }

        if (file.size === 0) {
          errors.push(`File "${file.name}" trống.`);
          continue;
        }

        if (file.size > MAX_FILE_SIZE_BYTES) {
          errors.push(`File "${file.name}" vượt quá giới hạn 20 MB.`);
          continue;
        }

        if (seen.has(key)) {
          errors.push(`File "${file.name}" đã được chọn.`);
          continue;
        }

        if (nextTotal + file.size > MAX_TOTAL_SIZE_BYTES) {
          errors.push("Tổng dung lượng file vượt quá giới hạn 25 MB.");
          continue;
        }

        const isImage = IMAGE_EXTENSIONS.includes(ext);
        const previewUrl = isImage ? URL.createObjectURL(file) : undefined;

        next.push({ id: crypto.randomUUID(), file, previewUrl });
        seen.add(key);
        addedCount += 1;
      }

      applyFiles(next);
      return { added: addedCount, rejected: errors.length, errors };
    },
    [applyFiles],
  );

  const removeFile = useCallback(
    (id: string) => {
      const target = filesRef.current.find((item) => item.id === id);
      if (target) {
        revokeUrl(target.previewUrl);
      }
      applyFiles(filesRef.current.filter((item) => item.id !== id));
    },
    [applyFiles, revokeUrl],
  );

  const totalSize = files.reduce((sum, item) => sum + item.file.size, 0);

  return { files, totalSize, addFiles, removeFile, clearAll };
}