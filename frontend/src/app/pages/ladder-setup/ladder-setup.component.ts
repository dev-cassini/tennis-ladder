import { AsyncPipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { catchError, EMPTY } from 'rxjs';

import { LadderService } from '../../services/ladder.service';

@Component({
  selector: 'app-ladder-setup',
  imports: [AsyncPipe, RouterLink],
  templateUrl: './ladder-setup.component.html',
  styleUrl: './ladder-setup.component.scss'
})
export class LadderSetupComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly ladderService = inject(LadderService);

  protected readonly error = signal('');
  protected readonly setup$ = this.ladderService
    .getSetup(this.route.snapshot.paramMap.get('ladderId') ?? '')
    .pipe(
      catchError((error: HttpErrorResponse) => {
        this.error.set(error.error?.title ?? 'We could not load this ladder.');
        return EMPTY;
      })
    );
}
