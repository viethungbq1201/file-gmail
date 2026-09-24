import { useCallback, useEffect, useState } from "react";
import type { FormEvent } from "react";
import { CheckCircle, WarningCircle, XCircle } from "@phosphor-icons/react";
import { ApiError } from "../services/api";
import { getGmailStatus, sendEmail, startGmailAuthorization } from "../services/gmailApi";
import type { SendState } from "../types/types";
import { useFileSelection } from "../hooks/useFileSelection";
import { Header } from "../components/Header";
import { GmailStatus as GmailStatusView } from "../components/GmailStatus";
import type { GmailStatusData } from "../components/GmailStatus";
import { FileDropzone } from "../components/FileDropzone";
import { FileList } from "../components/FileList";
import { EmailForm } from "../components/EmailForm";
import { SendButton } from "../components/SendButton";
import { Toast } from "../components/Toast";
import { ImagePreview } from "../components/ImagePreview";

const DEFAULT_SUBJECT = "File gửi từ Web App";
const DEFAULT_BODY = "Các file được gửi từ Web App.";

export function MainPage() {
  const { files, totalSize, addFiles, removeFile, clearAll } = useFileSelection();
  const [subject, setSubject] = useState(DEFAULT_SUBJECT);
  const [body, setBody] = useState(DEFAULT_BODY);
  const [selectedErrors, setSelectedErrors] = useState<string[]>([]);
  const [gmailStatus, setGmailStatus] = useState<GmailStatusData | null>(null);
  const [statusLoading, setStatusLoading] = useState(true);
  const [statusError, setStatusError] = useState(false);
  const [sendState, setSendState] = useState<SendState>({ status: "initial" });
  const [toast, setToast] = useState<{ message: string; type: "success" | "error" } | null>(null);
  const [preview, setPreview] = useState<{ url: string; name: string } | null>(null);

  const loadStatus = useCallback(async () => {
    setStatusLoading(true);
    setStatusError(false);
    try {
      const status = await getGmailStatus();
      setGmailStatus({
        connected: status.authorized,
        recipient: status.recipient,
        sender: status.sender,
      });
    } catch {
      setStatusError(true);
    } finally {
      setStatusLoading(false);
    }
  }, []);

  useEffect(() => {
    void loadStatus();

    const params = new URLSearchParams(window.location.search);
    const authorized = params.get("gmailAuthorized");
    if (authorized) {
      if (authorized === "true") {
        setToast({ message: "Gmail đã kết nối.", type: "success" });
        void loadStatus();
      } else if (params.get("error") !== "not_configured") {
        setToast({ message: "Không kết nối được Gmail. Vui lòng thử lại.", type: "error" });
      } else {
        setToast({ message: "Gmail chưa được cấu hình trên máy chủ.", type: "error" });
      }
      window.history.replaceState({}, "", window.location.pathname);
    }
  }, [loadStatus]);

  useEffect(() => {
    if (!toast) return;
    const timer = window.setTimeout(() => setToast(null), 5000);
    return () => window.clearTimeout(timer);
  }, [toast]);

  const handleFiles = useCallback(
    (incoming: File[]) => {
      const result = addFiles(incoming);
      if (result.errors.length > 0) {
        setSelectedErrors(result.errors);
      }
    },
    [addFiles],
  );

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault();
    if (sendState.status === "sending") return;

    if (files.length === 0) {
      setSelectedErrors(["Vui lòng chọn ít nhất một file."]);
      return;
    }

    if (!gmailStatus?.connected) {
      setSendState({ status: "error", message: "Gmail chưa được kết nối. Vui lòng kết nối trước." });
      return;
    }

    setSendState({ status: "sending" });
    try {
      await sendEmail(
        files.map((item) => item.file),
        subject,
        body,
      );
      const count = files.length;
      clearAll();
      setSubject(DEFAULT_SUBJECT);
      setBody(DEFAULT_BODY);
      setSendState({ status: "success", sentCount: count });
      setToast({ message: `Đã gửi ${count} file đến Gmail.`, type: "success" });
    } catch (err) {
      const message =
        err instanceof ApiError ? err.message : "Không thể gửi email lúc này. Vui lòng thử lại sau.";
      setSendState({ status: "error", message });
    }
  };

  const sending = sendState.status === "sending";

  return (
    <div className="app-shell">
      <Header>
        <GmailStatusView
          status={gmailStatus}
          loading={statusLoading}
          error={statusError}
          onConnect={startGmailAuthorization}
          onRetry={() => void loadStatus()}
        />
      </Header>

      <main className="app-main">
        <section className="text-center">
          <h1 className="text-[22px] font-bold tracking-tight text-(--text) sm:text-2xl">
            Gửi tài liệu qua Gmail
          </h1>
          <p className="mx-auto mt-1 max-w-[42ch] text-sm text-(--text-2)">
            Chọn ảnh, PDF, Word hoặc Excel, điền thông tin và gửi trực tiếp về hộp thư cố định.
          </p>
        </section>

        <FileDropzone onFiles={handleFiles} disabled={sending} />

        {selectedErrors.length > 0 && !sendStateIsActive(sendState) && (
          <div className="banner banner-error" role="alert">
            <WarningCircle size={18} className="banner-icon" />
            <ul className="flex flex-col gap-1">
              {selectedErrors.map((message) => (
                <li key={message}>{message}</li>
              ))}
              <li>
                <button
                  type="button"
                  className="mt-1.5 inline-block rounded text-[13px] font-semibold text-(--error) underline underline-offset-4 transition-opacity hover:opacity-80 focusable"
                  onClick={() => setSelectedErrors([])}
                >
                  Đã hiểu và bỏ qua
                </button>
              </li>
            </ul>
          </div>
        )}

        {sendState.status === "success" && (
          <div className="banner banner-success" role="status">
            <CheckCircle size={18} className="banner-icon text-(--success)" />
            <div>
              <p className="font-semibold">Đã gửi thành công</p>
              <p className="text-[13px] opacity-80">
                {sendState.sentCount} file đã được gửi đến Gmail.
              </p>
            </div>
          </div>
        )}

        {sendState.status === "error" && (
          <div className="banner banner-error" role="alert">
            <XCircle size={18} className="banner-icon" />
            <p>{sendState.message}</p>
          </div>
        )}

        <FileList
          files={files}
          totalSize={totalSize}
          disabled={sending}
          onRemove={removeFile}
          onClearAll={clearAll}
          onPreview={(url, name) => setPreview({ url, name })}
        />

        {gmailStatus && !gmailStatus.connected && (
          <div className="banner banner-warning" role="note">
            <WarningCircle size={18} className="banner-icon" />
            <div>
              <p className="font-semibold">Gmail chưa được kết nối</p>
              <p className="text-[13px] opacity-80">Vui lòng kết nối Gmail trước khi gửi file.</p>
            </div>
          </div>
        )}

        <form onSubmit={handleSubmit} className="flex flex-col gap-4">
          <EmailForm
            recipient={gmailStatus?.recipient ?? ""}
            subject={subject}
            body={body}
            disabled={sending}
            onSubjectChange={setSubject}
            onBodyChange={setBody}
          />

          {!gmailStatus?.recipient && (
            <p className="text-center text-[12px] text-(--text-3)" role="note">
              Địa chỉ người nhận sẽ hiển thị sau khi máy chủ được cấu hình.
            </p>
          )}

          <SendButton
            fileCount={files.length}
            sending={sending}
            disabled={!gmailStatus?.connected || files.length === 0}
          />
        </form>
      </main>

      <footer className="app-footer">
        FileGmail · Nhận ảnh (JPG/PNG/WEBP), PDF, DOCX, XLSX · File không được lưu trên máy chủ
      </footer>

      {toast && <Toast message={toast.message} type={toast.type} onClose={() => setToast(null)} />}
      {preview && (
        <ImagePreview
          src={preview.url}
          fileName={preview.name}
          onClose={() => setPreview(null)}
        />
      )}
    </div>
  );
}

function sendStateIsActive(state: SendState): boolean {
  return state.status === "sending";
}