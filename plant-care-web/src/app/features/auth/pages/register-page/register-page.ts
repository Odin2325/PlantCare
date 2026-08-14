import { HttpErrorResponse } from '@angular/common/http';
import {
  ChangeDetectionStrategy,
  Component,
  inject,
  signal,
} from '@angular/core';
import {
  FormControl,
  FormGroup,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import {
  Router,
  RouterLink,
} from '@angular/router';
import { finalize } from 'rxjs';

import {
  ValidationProblemDetails,
} from '../../../../core/auth/models/auth.models';
import { AuthService } from '../../../../core/auth/services/auth.service';

@Component({
  selector: 'app-register-page',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    RouterLink,
  ],
  templateUrl: './register-page.html',
  styleUrl: './register-page.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RegisterPage {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  readonly isSubmitting = signal(false);
  readonly errorMessage = signal<string | null>(null);

  readonly form = new FormGroup({
    email: new FormControl('', {
      nonNullable: true,
      validators: [
        Validators.required,
        Validators.email,
      ],
    }),
    password: new FormControl('', {
      nonNullable: true,
      validators: [
        Validators.required,
        Validators.minLength(10),
        Validators.pattern(
          /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$/,
        ),
      ],
    }),
    confirmPassword: new FormControl('', {
      nonNullable: true,
      validators: [
        Validators.required,
      ],
    }),
  });

  submit(): void {
    if (this.form.invalid || this.isSubmitting()) {
      this.form.markAllAsTouched();
      return;
    }

    const {
      email,
      password,
      confirmPassword,
    } = this.form.getRawValue();

    if (password !== confirmPassword) {
      this.errorMessage.set(
        'The password confirmation does not match.',
      );
      return;
    }

    this.isSubmitting.set(true);
    this.errorMessage.set(null);

    this.authService
      .register({
        email,
        password,
      })
      .pipe(
        finalize(() => {
          this.isSubmitting.set(false);
        }),
      )
      .subscribe({
        next: () => {
          void this.router.navigate(['/my-plants']);
        },
        error: (error: HttpErrorResponse) => {
          this.errorMessage.set(
            this.getErrorMessage(error),
          );
        },
      });
  }

  private getErrorMessage(
    error: HttpErrorResponse,
  ): string {
    const problem =
      error.error as ValidationProblemDetails | null;

    const firstValidationError =
      problem?.errors
        ? Object.values(problem.errors)
          .flat()
          .find((message) => message.length > 0)
        : undefined;

    if (firstValidationError) {
      return firstValidationError;
    }

    if (error.status === 0) {
      return 'The PlantCare server could not be reached.';
    }

    return 'The account could not be created.';
  }
}
