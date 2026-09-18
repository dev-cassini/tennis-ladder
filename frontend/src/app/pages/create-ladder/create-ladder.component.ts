import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';

import { LadderService } from '../../services/ladder.service';

@Component({
  selector: 'app-create-ladder',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './create-ladder.component.html',
  styleUrl: './create-ladder.component.scss'
})
export class CreateLadderComponent {
  private readonly ladderService = inject(LadderService);
  private readonly router = inject(Router);

  protected readonly submitting = signal(false);
  protected readonly serverError = signal('');
  protected readonly form = new FormGroup({
    name: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(120)]
    })
  });

  protected submit(): void {
    this.serverError.set('');
    this.form.markAllAsTouched();

    if (this.form.invalid || this.submitting()) {
      return;
    }

    this.submitting.set(true);
    this.ladderService
      .createLadder(this.form.controls.name.value)
      .pipe(finalize(() => this.submitting.set(false)))
      .subscribe({
        next: (ladder) => void this.router.navigate(['/app/ladders', ladder.id, 'setup']),
        error: (error: HttpErrorResponse) => {
          const nameErrors = error.error?.errors?.name as string[] | undefined;
          this.serverError.set(nameErrors?.[0] ?? 'We could not create the ladder. Try again.');
        }
      });
  }
}
