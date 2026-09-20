import {
  inject,
  provideAppInitializer,
} from '@angular/core';
import {
  provideHttpClient,
  withInterceptors,
} from '@angular/common/http';
import { provideRouter } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { provideServiceWorker } from '@angular/service-worker';
import { environment } from '../environments/environment';

import { routes } from './app.routes';
import { credentialsInterceptor } from './core/auth/interceptors/credentials.interceptor';
import { AuthService } from './core/auth/services/auth.service';
import { antiforgeryInterceptor } from './core/security/interceptors/antiforgery.interceptor';

export const appConfig = {
  providers: [
    provideRouter(routes),

    provideServiceWorker('ngsw-worker.js', {
      enabled: environment.production,
      registrationStrategy: 'registerWhenStable:30000',
    }),

    provideHttpClient(
      withInterceptors([
        credentialsInterceptor,
        antiforgeryInterceptor,
      ]),
    ),

    provideAppInitializer(() => {
      const authService = inject(AuthService);

      return firstValueFrom(authService.initialize(),
      );
    }),
  ],
};
