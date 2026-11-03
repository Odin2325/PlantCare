import { TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter, Router } from '@angular/router';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { AccountPage } from './account-page';
import { AuthService } from '../../core/auth/services/auth.service';

describe('AccountPage destructive action safeguards', () => {
  let http: HttpTestingController;
  const clearDeletedAccount = vi.fn();
  const user = signal({id:'a',email:'a@example.test'});
  beforeEach(() => {
    clearDeletedAccount.mockClear();
    user.set({id:'a',email:'a@example.test'});
    TestBed.configureTestingModule({ providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting(),
      { provide: AuthService, useValue: { clearDeletedAccount, currentUser:user } }] });
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => http.verify());
  it('uses the password-protected image export and drops a response after account changes',()=>{
    const fixture=TestBed.createComponent(AccountPage);fixture.componentInstance.password='SyntheticPassword9!';
    fixture.componentInstance.exportData(true);const request=http.expectOne('/api/account/export/photos');
    expect(request.request.body.password).toBe('SyntheticPassword9!');expect(fixture.componentInstance.password).toBe('');
    const create=vi.spyOn(document,'createElement');user.set({id:'b',email:'b@example.test'});
    request.flush(new Blob(['synthetic'],{type:'application/zip'}));
    expect(create.mock.calls.some(args=>args[0]==='a')).toBe(false);create.mockRestore();
    expect(fixture.componentInstance.busy()).toBe(false);
  });
  it('does not submit deletion without both password and exact confirmation', () => {
    const fixture = TestBed.createComponent(AccountPage);
    fixture.componentInstance.password = 'TestPassword9';
    fixture.componentInstance.confirmation = 'delete';
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('button.danger').disabled).toBe(true);
    fixture.componentInstance.deleteAccount();
    http.expectNone('/api/account/delete');
  });
  it('clears identity and navigates only after successful deletion', () => {
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
    const fixture = TestBed.createComponent(AccountPage);
    fixture.componentInstance.password = 'TestPassword9'; fixture.componentInstance.confirmation = 'DELETE';
    fixture.componentInstance.deleteAccount();
    const request = http.expectOne('/api/account/delete');
    expect(request.request.body.password).toBe('TestPassword9');
    expect(fixture.componentInstance.password).toBe('');
    expect(clearDeletedAccount).not.toHaveBeenCalled();
    request.flush(null);
    expect(clearDeletedAccount).toHaveBeenCalledOnce();
    expect(navigate).toHaveBeenCalledWith('/login?accountDeleted=true');
  });
  it('preserves the identity and explains provider cleanup when deletion is blocked', () => {
    const fixture = TestBed.createComponent(AccountPage);
    fixture.componentInstance.password = 'TestPassword9'; fixture.componentInstance.confirmation = 'DELETE';
    fixture.componentInstance.deleteAccount();
    http.expectOne('/api/account/delete').flush({}, { status: 409, statusText: 'Conflict' });
    fixture.detectChanges();
    expect(clearDeletedAccount).not.toHaveBeenCalled();
    expect(fixture.nativeElement.querySelector('[role="alert"]').textContent).toContain('Disconnect external calendars');
  });
});
