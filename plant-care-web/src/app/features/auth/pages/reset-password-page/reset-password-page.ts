import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';

import { ValidationProblemDetails } from '../../../../core/auth/models/auth.models';
import { AuthService } from '../../../../core/auth/services/auth.service';

@Component({
  selector: 'app-reset-password-page',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './reset-password-page.html',
  styleUrl: './reset-password-page.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ResetPasswordPage {
  private readonly authService = inject(AuthService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly email = this.route.snapshot.queryParamMap.get('email') ?? '';
  readonly code = this.route.snapshot.queryParamMap.get('code') ?? '';
  readonly hasValidLink = this.email.length > 0 && this.code.length > 0;
  readonly isSubmitting = signal(false);
  readonly errorMessage = signal<string | null>(null);

  readonly form = new FormGroup({
    password: new FormControl('', {
      nonNullable: true,
      validators: [
        Validators.required,
        Validators.minLength(10),
        Validators.pattern(/^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$/),
      ],
    }),
    confirmPassword: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required],
    }),
  });

  submit(): void {
    if (!this.hasValidLink || this.form.invalid || this.isSubmitting()) {
      this.form.markAllAsTouched();
      return;
    }

    const { password, confirmPassword } = this.form.getRawValue();
    if (password !== confirmPassword) {
      this.errorMessage.set('The password confirmation does not match.');
      return;
    }

    this.isSubmitting.set(true);
    this.errorMessage.set(null);

    this.authService
      .resetPassword(this.email, this.code, password)
      .pipe(finalize(() => this.isSubmitting.set(false)))
      .subscribe({
        next: () => void this.router.navigate(
          ['/login'],
          { queryParams: { passwordReset: true } },
        ),
        error: (error: HttpErrorResponse) =>
          this.errorMessage.set(this.getErrorMessage(error)),
      });
  }

  private getErrorMessage(error: HttpErrorResponse): string {
    const problem = error.error as ValidationProblemDetails | null;
    const firstError = problem?.errors
      ? Object.values(problem.errors).flat().find(Boolean)
      : undefined;

    return firstError ??
      'This reset link is invalid or expired. Request a new one.';
  }
}
