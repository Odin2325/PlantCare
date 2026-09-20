import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';

import { AuthService } from '../../../../core/auth/services/auth.service';

@Component({
  selector: 'app-resend-confirmation-page',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './resend-confirmation-page.html',
  styleUrl: './resend-confirmation-page.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ResendConfirmationPage {
  private readonly authService = inject(AuthService);

  readonly isSubmitting = signal(false);
  readonly submitted = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly form = new FormGroup({
    email: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.email],
    }),
  });

  submit(): void {
    if (this.form.invalid || this.isSubmitting()) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSubmitting.set(true);
    this.errorMessage.set(null);

    this.authService
      .resendConfirmationEmail(this.form.getRawValue().email)
      .pipe(finalize(() => this.isSubmitting.set(false)))
      .subscribe({
        next: () => this.submitted.set(true),
        error: () => this.errorMessage.set(
          'The request could not be completed. Please try again.',
        ),
      });
  }
}
