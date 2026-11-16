import {
  afterNextRender, ChangeDetectionStrategy, Component, computed, DestroyRef, ElementRef,
  inject, Injector, output, signal, viewChild,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';

import { AuthService } from '../../core/auth/services/auth.service';

const steps = [
  {
    title: 'Welcome to PlantCare',
    text: 'Start in the Plant catalog. Search for a plant by name, open it for care tips, then add it to My plants.',
  },
  {
    title: 'See what needs care',
    text: 'Use Dashboard to see what’s due and mark tasks complete. Open a plant in My plants to adjust its care schedule.',
  },
  {
    title: 'Look ahead in Calendar',
    text: 'See upcoming tasks by day and tap an event to open that plant. Find reminder settings under More → Notifications.',
  },
  {
    title: 'Plan your growing space',
    text: 'Open More → Garden to track growing plans and harvests. Use Space planner to draw a layout and get ideas for which plants fit each light zone.',
  },
];

@Component({
  selector: 'app-welcome-tour',
  templateUrl: './welcome-tour.html',
  styleUrl: './welcome-tour.css',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WelcomeTour {
  private readonly auth = inject(AuthService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly injector = inject(Injector);
  private readonly router = inject(Router);
  private readonly dialog = viewChild.required<ElementRef<HTMLDialogElement>>('dialog');
  private readonly heading = viewChild.required<ElementRef<HTMLHeadingElement>>('heading');
  readonly deferred = output<void>();
  readonly index = signal(0);
  readonly stepCount = steps.length;
  readonly step = computed(() => steps[this.index()]);
  readonly saving = signal(false);
  readonly failedAction = signal<'skip' | 'browse' | null>(null);

  constructor() {
    afterNextRender(() => {
      this.dialog().nativeElement.showModal();
      this.heading().nativeElement.focus();
    });
  }

  move(direction: number): void {
    this.index.update(index => index + direction);
    afterNextRender(() => this.heading().nativeElement.focus(), { injector: this.injector });
  }

  cancel(event: Event): void {
    event.preventDefault();
    if (this.saving() || this.failedAction()) {
      this.closeForNow();
    } else {
      this.finish(false);
    }
  }

  finish(openCatalog: boolean): void {
    if (this.saving()) return;
    this.saving.set(true);
    this.failedAction.set(null);
    this.auth.dismissWelcomeTour().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.dialog().nativeElement.close();
        if (openCatalog) void this.router.navigate(['/plants']);
      },
      error: () => {
        this.saving.set(false);
        this.failedAction.set(openCatalog ? 'browse' : 'skip');
      },
    });
  }

  closeForNow(): void {
    this.dialog().nativeElement.close();
    this.deferred.emit();
  }
}
