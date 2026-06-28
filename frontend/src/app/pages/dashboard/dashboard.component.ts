import { AsyncPipe, JsonPipe } from '@angular/common';
import { Component, inject } from '@angular/core';
import { AuthService } from '@auth0/auth0-angular';

import { CurrentUserService } from '../../services/current-user.service';

@Component({
  selector: 'app-dashboard',
  imports: [AsyncPipe, JsonPipe],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss'
})
export class DashboardComponent {
  protected readonly auth = inject(AuthService);
  protected readonly currentUser$ = inject(CurrentUserService).currentUser$;

  protected logOut(): void {
    void this.auth.logout({
      logoutParams: {
        returnTo: window.location.origin
      }
    });
  }
}
