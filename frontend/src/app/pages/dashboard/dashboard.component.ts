import { AsyncPipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthService } from '@auth0/auth0-angular';
import { catchError, EMPTY } from 'rxjs';

import { LadderService } from '../../services/ladder.service';

@Component({
  selector: 'app-dashboard',
  imports: [AsyncPipe, RouterLink],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss'
})
export class DashboardComponent {
  protected readonly auth = inject(AuthService);
  private readonly ladderService = inject(LadderService);

  protected readonly loadError = signal('');
  protected readonly ladders$ = this.ladderService.getLadders().pipe(
    catchError((error: HttpErrorResponse) => {
      this.loadError.set(error.error?.title ?? 'We could not load your ladders.');
      return EMPTY;
    })
  );

  protected logOut(): void {
    this.auth
      .logout({
        logoutParams: {
          returnTo: window.location.origin
        }
      })
      .subscribe();
  }
}
