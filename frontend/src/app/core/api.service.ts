import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, firstValueFrom } from 'rxjs';
import { ApiResponse } from './models';

export class ApiError extends Error {
  constructor(message: string, readonly status: number) {
    super(message);
  }
  /** The interceptor already told the user about these. */
  get handled(): boolean {
    return this.status === 401 || this.status === 429 || this.status === 503;
  }
}

/** Thin wrapper over HttpClient: unwraps the response envelope and turns every failure into an ApiError. */
@Injectable({ providedIn: 'root' })
export class ApiService {
  private readonly http = inject(HttpClient);

  get<T>(path: string, params?: Record<string, string | number | undefined | null>): Promise<T> {
    const clean: Record<string, string> = {};
    for (const [key, value] of Object.entries(params ?? {})) {
      if (value !== undefined && value !== null && value !== '') clean[key] = String(value);
    }
    return this.unwrap(this.http.get<ApiResponse<T>>(path, { params: clean }));
  }

  post<T>(path: string, body?: unknown): Promise<T> {
    return this.unwrap(this.http.post<ApiResponse<T>>(path, body ?? null));
  }

  patch<T>(path: string, body: unknown): Promise<T> {
    return this.unwrap(this.http.patch<ApiResponse<T>>(path, body));
  }

  private async unwrap<T>(request: Observable<ApiResponse<T>>): Promise<T> {
    try {
      const res = await firstValueFrom(request);
      if (res && !res.succeeded) throw new ApiError(res.message, 400);
      return res?.data as T;
    } catch (error) {
      if (error instanceof ApiError) throw error;
      if (error instanceof HttpErrorResponse) throw new ApiError(this.messageOf(error), error.status);
      throw error;
    }
  }

private messageOf(error: HttpErrorResponse): string {
  if (error.status === 0) return 'Cannot reach the server. Is the backend running on port 5107?';
  const body = error.error;
  if (body?.message) return body.message;
  if (body?.errors) return (Object.values(body.errors).flat() as string[])[0] ?? body.title;
  if (body?.title) return body.title;
  if (error.status === 401) return 'Sign in to continue';
  if (error.status === 429) return 'Too many requests. Wait a moment and try again.';
  return 'Something went wrong';
}
}
