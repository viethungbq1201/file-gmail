import { ApiError, getApiBaseUrl, requestJson } from "./api";
import type { ApiResponse, GmailStatus, SendEmailResponse } from "../types/types";

export async function getGmailStatus(): Promise<GmailStatus> {
  const response = await requestJson<ApiResponse<GmailStatus>>("/api/gmail/status");
  if (!response.data) {
    throw new ApiError("Không đọc được trạng thái Gmail.");
  }
  return response.data;
}

export async function verifyAccessKey(): Promise<void> {
  await requestJson<ApiResponse>("/api/gmail/access-key/verify");
}

export async function sendEmail(
  files: File[],
  subject: string,
  body: string,
): Promise<SendEmailResponse> {
  const formData = new FormData();
  for (const file of files) {
    formData.append("files", file, file.name);
  }
  formData.append("subject", subject);
  formData.append("body", body);

  return requestJson<SendEmailResponse>("/api/gmail/send", {
    method: "POST",
    body: formData,
  });
}

export function startGmailAuthorization(): void {
  window.location.href = `${getApiBaseUrl()}/api/gmail/auth`;
}