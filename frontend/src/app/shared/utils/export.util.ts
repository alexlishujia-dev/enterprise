import { HttpResponse } from '@angular/common/http';
import { ApiBusinessError, ApiResult, ApiStatusCode } from '../../core/models/api-result.model';

export function downloadBlob(blob: Blob, filename: string): void {
  const url = URL.createObjectURL(blob);
  const anchor = document.createElement('a');
  anchor.href = url;
  anchor.download = filename;
  anchor.click();
  URL.revokeObjectURL(url);
}

export function extractFilename(contentDisposition: string | null): string | null {
  if (!contentDisposition) {
    return null;
  }

  const utf8Match = /filename\*=UTF-8''([^;]+)/i.exec(contentDisposition);
  if (utf8Match?.[1]) {
    return decodeURIComponent(utf8Match[1]);
  }

  const match = /filename="?([^";]+)"?/i.exec(contentDisposition);
  return match?.[1] ?? null;
}

export async function handleExportDownload(response: HttpResponse<Blob>, fallbackFilename: string): Promise<void> {
  const blob = response.body;
  if (!blob) {
    throw new ApiBusinessError('导出失败：空响应', ApiStatusCode.InternalError);
  }

  if (await isErrorBlob(blob)) {
    await throwExportError(blob);
  }

  const filename = extractFilename(response.headers.get('Content-Disposition')) ?? fallbackFilename;
  downloadBlob(blob, filename);
}

async function isErrorBlob(blob: Blob): Promise<boolean> {
  const type = blob.type.toLowerCase();
  if (type.includes('json')) {
    return true;
  }

  if (blob.size > 4096) {
    return false;
  }

  const text = (await blob.text()).trimStart();
  return text.startsWith('{') && text.includes('"code"');
}

async function throwExportError(blob: Blob): Promise<never> {
  const text = await blob.text();
  try {
    const json = JSON.parse(text) as ApiResult<unknown>;
    throw new ApiBusinessError(json.message || '导出失败', json.code, json.traceId);
  } catch (error) {
    if (error instanceof ApiBusinessError) {
      throw error;
    }
    throw new ApiBusinessError('导出失败', ApiStatusCode.InternalError);
  }
}
