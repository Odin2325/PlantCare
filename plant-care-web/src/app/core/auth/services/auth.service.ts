import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import {
  computed,
  inject,
  Injectable,
  signal,
} from '@angular/core';
import {
  catchError,
  map,
  Observable,
  of,
  retry,
  switchMap,
  tap,
  throwError,
  timer,
} from 'rxjs';

import {Credentials,CurrentUser} from '../models/auth.models';
import { AntiforgeryService } from '../../security/services/antiforgery.service';

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private readonly httpClient = inject(HttpClient);

  private readonly antiforgeryService =
    inject(AntiforgeryService);

  private readonly currentUserState =
    signal<CurrentUser | null>(null);

  readonly currentUser =
    this.currentUserState.asReadonly();

  readonly isAuthenticated = computed(
    () => this.currentUserState() !== null,
  );

  initialize(): Observable<void> {
    return this.antiforgeryService
      .refreshToken()
      .pipe(
        switchMap(() => this.loadCurrentUser()),

        map(() => undefined),

        retry({
          count: 60,

          delay: (
            error: unknown,
            retryCount: number,
          ) => {
            if (
              error instanceof HttpErrorResponse &&
              this.isTemporaryStartupError(error)
            ) {
              console.warn(
                `PlantCare API is not ready yet. ` +
                `Retrying startup (${retryCount}/30)...`,
              );

              return timer(500);
            }

            return throwError(() => error);
          },
        }),

        catchError((error: HttpErrorResponse) => {
          if (error.status !== 401) {
            console.error(
              'Unable to initialize authentication.',
              error,
            );
          }

          this.currentUserState.set(null);

          return of(undefined);
        }),
      );
  }

  private isTemporaryStartupError(
    error: HttpErrorResponse,
  ): boolean {
    return [
      0,
      502,
      503,
      504,
    ].includes(error.status);
  }

  register(credentials: Credentials): Observable<void> {
    return this.httpClient
      .post<void>(
        '/api/auth/register',
        credentials,
      )
      .pipe(
        switchMap(() => this.login(credentials)),
      );
  }

  login(credentials: Credentials): Observable<void> {
    return this.httpClient
      .post<void>(
        '/api/auth/login?useCookies=true',
        credentials,
      )
      .pipe(
        switchMap(() =>
          this.antiforgeryService.refreshToken(),
        ),
        switchMap(() => this.loadCurrentUser()),
        map(() => undefined),
      );
  }

  logout(): Observable<void> {
    return this.httpClient
      .post<void>(
        '/api/auth/logout',
        {},
      )
      .pipe(
        tap(() => {
          this.currentUserState.set(null);
        }),
        switchMap(() =>
          this.antiforgeryService.refreshToken(),
        ),
      );
  }

  loadCurrentUser(): Observable<CurrentUser> {
    return this.httpClient
      .get<CurrentUser>('/api/account/me')
      .pipe(
        tap((currentUser) => {
          this.currentUserState.set(currentUser);
        }),
      );
  }
}
