import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import {
  map,
  Observable,
  tap,
} from 'rxjs';

import { AntiforgeryTokenStore } from './antiforgery-token-store';

interface AntiforgeryTokenResponse {
  requestToken: string;
}

@Injectable({
  providedIn: 'root',
})
export class AntiforgeryService {
  private readonly httpClient = inject(HttpClient);

  private readonly tokenStore =
    inject(AntiforgeryTokenStore);

  refreshToken(): Observable<void> {
    return this.httpClient
      .get<AntiforgeryTokenResponse>(
        '/api/antiforgery/token',
      )
      .pipe(
        tap((response) => {
          this.tokenStore.setToken(
            response.requestToken,
          );
        }),
        map(() => undefined),
      );
  }
}
