import type { ApiResponse } from "../types/types";

const APP_KEY_STORAGE_KEY = "filegmail.appKey";

export class ApiError extends Error {
  readonly status: number;

  constructor(message: string, status = 0) {
    super(message);
    this.name = "ApiError";
    this.status = status;
  }
}

export function getApiBaseUrl(): string {
  return (import.meta.env.VITE_API_BASE_URL as string | undefined) ?? "http://localhost:5244";
}

export function getAppKey(): string {
  return localStorage.getItem(APP_KEY_STORAGE_KEY) ?? "";
}

export function setAppKey(key: string): void {
  localStorage.setItem(APP_KEY_STORAGE_KEY, key);
}

async function requestJson<T>(path: string, init?: RequestInit): Promise<T> {
  let response: Response;
  try {
    response = await fetch(`${getApiBaseUrl()}${path}`, {
      headers: {
        "X-App-Key": getAppKey(),
        ...init?.headers,
      },
      ...init,
    });
  } catch {
    throw new ApiError("Không thể kết nối đến máy chủ. Vui lòng kiểm tra kết nối mạng.");
  }

  if (response.status === 429) {
    throw new ApiError("Bạn đang gửi quá nhiều yêu cầu. Vui lòng thử lại sau ít phút.", 429);
  }

  if (response.status === 401) {
    throw new ApiError("Mã truy cập không hợp lệ.", 401);
  }

  let body: T;
  try {
    body = (await response.json()) as T;
  } catch {
    throw new ApiError("Đã xảy ra lỗi. Vui lòng thử lại.", response.status);
  }

  if (!response.ok) {
    const apiError = body as ApiResponse;
    throw new ApiError(apiError.message ?? "Đã xảy ra lỗi. Vui lòng thử lại.", response.status);
  }

  return body;
}

export { requestJson };