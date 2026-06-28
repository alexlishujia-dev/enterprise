export class StorageUtil {
  static getJson<T>(key: string): T | null {
    const raw = localStorage.getItem(key);
    if (!raw) return null;
    try {
      return JSON.parse(raw) as T;
    } catch {
      return null;
    }
  }

  static setJson(key: string, value: unknown): void {
    localStorage.setItem(key, JSON.stringify(value));
  }

  static remove(key: string): void {
    localStorage.removeItem(key);
  }

  static getString(key: string): string | null {
    return localStorage.getItem(key);
  }

  static setString(key: string, value: string): void {
    localStorage.setItem(key, value);
  }
}
