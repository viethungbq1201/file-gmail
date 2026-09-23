import { useState } from "react";
import type { FormEvent } from "react";
import { Eye, EyeSlash, LockKey, ShieldCheck, ShieldWarning } from "@phosphor-icons/react";
import { ApiError, setAppKey } from "../services/api";
import { verifyAccessKey } from "../services/gmailApi";

interface AccessKeyFormProps {
  onSuccess: () => void;
}

export function AccessKeyForm({ onSuccess }: AccessKeyFormProps) {
  const [key, setKey] = useState("");
  const [showKey, setShowKey] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault();
    if (submitting || key.trim().length === 0) return;

    setSubmitting(true);
    setError(null);
    const trimmedKey = key.trim();
    setAppKey(trimmedKey);
    try {
      await verifyAccessKey();
      onSuccess();
    } catch (err) {
      const message =
        err instanceof ApiError ? err.message : "Đã xảy ra lỗi. Vui lòng thử lại.";
      setError(message);
    } finally {
      setSubmitting(false);
    }
  };

  const handleSkip = () => {
    setAppKey("");
    onSuccess();
  };

  return (
    <div className="flex min-h-[100dvh] items-center justify-center p-4">
      <div className="card card-pad w-full max-w-[420px]">
        <div className="mb-6 flex flex-col items-center text-center">
          <span
            className="mb-3 flex h-12 w-12 items-center justify-center rounded-xl bg-(--accent) text-(--on-accent)"
            aria-hidden="true"
          >
            <ShieldCheck size={26} weight="bold" />
          </span>
          <h1 className="text-xl font-bold tracking-tight text-(--text)">Nhập mã truy cập</h1>
          <p className="mt-1 text-sm text-(--text-2)">
            Nhập mã truy cập để sử dụng ứng dụng gửi tài liệu.
          </p>
        </div>

        <form onSubmit={handleSubmit} className="flex flex-col gap-4">
          <div className="field-group">
            <label htmlFor="access-key" className="label">
              Mã truy cập
            </label>
            <div className="relative">
              <span
                className="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 text-(--text-3)"
                aria-hidden="true"
              >
                <LockKey size={18} />
              </span>
              <input
                id="access-key"
                type={showKey ? "text" : "password"}
                className="input pl-10 pr-10"
                value={key}
                onChange={(event) => setKey(event.target.value)}
                placeholder="Nhập mã truy cập"
                autoComplete="off"
                autoFocus
                required
              />
              <button
                type="button"
                className="btn btn-ghost absolute right-1.5 top-1/2 -translate-y-1/2 h-8 w-8 p-0 text-(--text-3) hover:text-(--text) focusable"
                onClick={() => setShowKey((prev) => !prev)}
                aria-label={showKey ? "Ẩn mã truy cập" : "Hiện mã truy cập"}
                tabIndex={-1}
              >
                {showKey ? <EyeSlash size={18} /> : <Eye size={18} />}
              </button>
            </div>
          </div>

          {error && (
            <div role="alert" className="banner banner-error">
              <ShieldWarning size={18} className="banner-icon" />
              <span>{error}</span>
            </div>
          )}

          <button type="submit" className="btn btn-primary btn-lg btn-block" disabled={submitting}>
            {submitting ? (
              <>
                <span className="spinner" />
                Đang kiểm tra…
              </>
            ) : (
              "Vào ứng dụng"
            )}
          </button>
        </form>

        <div className="mt-4 text-center">
          <button
            type="button"
            className="btn btn-ghost btn-sm focusable"
            onClick={handleSkip}
            disabled={submitting}
          >
            Bỏ qua (dùng cho môi trường phát triển)
          </button>
        </div>
      </div>
    </div>
  );
}