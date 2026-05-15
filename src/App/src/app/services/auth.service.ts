import { HttpClient } from '@angular/common/http';
import { inject, Injectable, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';

interface TokenResponse {
  token: string;
}

interface JwtPayload {
  sub: string;
  name: string;
  exp: number;
  iat: number;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);

  private readonly _accessToken = signal<string | null>(null);
  private refreshTimerId: ReturnType<typeof setTimeout> | null = null;

  readonly isAuthenticated = () => this._accessToken() !== null;
  readonly accessToken = this._accessToken.asReadonly();

  async tryRestoreSession(): Promise<void> {
    try {
      const response = await firstValueFrom(
        this.http.post<TokenResponse>('/api/players/refresh', {})
      );
      this.storeToken(response.token);
    } catch {
      this._accessToken.set(null);
    }
  }

  async refreshToken(): Promise<string | null> {
    try {
      const response = await firstValueFrom(
        this.http.post<TokenResponse>('/api/players/refresh', {})
      );
      this.storeToken(response.token);
      return response.token;
    } catch {
      this._accessToken.set(null);
      return null;
    }
  }

  storeToken(token: string): void {
    this._accessToken.set(token);
    this.scheduleProactiveRefresh(token);
  }

  /** Returns the authenticated player's ID (JWT `sub`), or null if not authenticated or token expired. */
  getPlayerId(): string | null {
    const token = this._accessToken();
    if (!token) return null;
    const payload = this.parseJwtPayload(token);
    if (!payload?.sub) return null;
    if (payload.exp * 1000 <= Date.now()) return null;
    return payload.sub;
  }

  clearSession(): void {
    this._accessToken.set(null);
    if (this.refreshTimerId !== null) {
      clearTimeout(this.refreshTimerId);
      this.refreshTimerId = null;
    }
  }

  private scheduleProactiveRefresh(token: string): void {
    if (this.refreshTimerId !== null) {
      clearTimeout(this.refreshTimerId);
    }

    const payload = this.parseJwtPayload(token);
    if (!payload) return;

    const expiresInMs = payload.exp * 1000 - Date.now();
    const refreshInMs = expiresInMs - 60_000;

    if (refreshInMs <= 0) {
      void this.refreshToken();
      return;
    }

    this.refreshTimerId = setTimeout(() => void this.refreshToken(), refreshInMs);
  }

  private parseJwtPayload(token: string): JwtPayload | null {
    try {
      const parts = token.split('.');
      if (parts.length !== 3) return null;
      const payload = JSON.parse(atob(parts[1].replace(/-/g, '+').replace(/_/g, '/'))) as JwtPayload;
      return payload;
    } catch {
      return null;
    }
  }
}
