import {
  HttpInterceptorFn,
} from '@angular/common/http';
import { inject } from '@angular/core';

import { AntiforgeryTokenStore } from '../services/antiforgery-token-store';

const safeMethods = new Set([
  'GET',
  'HEAD',
  'OPTIONS',
  'TRACE',
]);

export const antiforgeryInterceptor:
  HttpInterceptorFn = (
    request,
    next,
  ) => {
    const tokenStore =
      inject(AntiforgeryTokenStore);

    const isApiRequest =
      request.url.startsWith('/api/');

    const requiresAntiforgeryToken =
      !safeMethods.has(
        request.method.toUpperCase(),
      );

    const token = tokenStore.token();

    if (
      !isApiRequest ||
      !requiresAntiforgeryToken ||
      !token
    ) {
      return next(request);
    }

    return next(
      request.clone({
        setHeaders: {
          'X-XSRF-TOKEN': token,
        },
      }),
    );
  };
