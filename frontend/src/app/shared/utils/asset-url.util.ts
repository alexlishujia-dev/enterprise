import { environment } from '@env/environment';

/** 将后端返回的头像路径转为可访问 URL */
export function resolveAssetUrl(path?: string | null): string | undefined {
  if (!path) {
    return undefined;
  }

  if (path.startsWith('http://') || path.startsWith('https://')) {
    return path;
  }

  const normalized = path.startsWith('/') ? path : `/${path}`;
  const base = environment.assetBaseUrl?.replace(/\/$/, '') ?? '';
  return base ? `${base}${normalized}` : normalized;
}
