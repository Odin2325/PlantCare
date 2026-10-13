import { TestBed } from '@angular/core/testing';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { afterEach, expect, it, vi } from 'vitest';
import { AuthService } from './auth.service';
import { AntiforgeryService } from '../../security/services/antiforgery.service';

afterEach(() => { TestBed.resetTestingModule(); vi.restoreAllMocks(); });
it('boots the offline shell without network retries or cached identity', async () => {
  vi.spyOn(navigator, 'onLine', 'get').mockReturnValue(false);
  const refreshToken = vi.fn(); const get = vi.fn();
  TestBed.configureTestingModule({ providers: [{ provide: HttpClient, useValue: { get } }, { provide: AntiforgeryService, useValue: { refreshToken } }] });
  const service = TestBed.inject(AuthService);
  await firstValueFrom(service.initialize());
  expect(refreshToken).not.toHaveBeenCalled(); expect(get).not.toHaveBeenCalled();
  expect(service.isAuthenticated()).toBe(false); expect(service.isReconnecting()).toBe(false);
});
