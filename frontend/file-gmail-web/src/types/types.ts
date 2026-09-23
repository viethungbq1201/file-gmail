export interface GmailStatus {
  authorized: boolean;
  sender: string;
  recipient: string;
}

export interface ApiResponse<T = unknown> {
  success: boolean;
  message?: string | null;
  data?: T | null;
}

export interface SendEmailResponse {
  success: boolean;
  message: string;
}

export interface SelectedFile {
  id: string;
  file: File;
  previewUrl?: string;
}

export type SendStatus = "initial" | "sending" | "success" | "error";

export interface SendState {
  status: SendStatus;
  message?: string;
  sentCount?: number;
}