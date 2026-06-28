import { AsyncPipe } from '@angular/common';
import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthService } from '@auth0/auth0-angular';

@Component({
  selector: 'app-landing',
  imports: [AsyncPipe, RouterLink],
  templateUrl: './landing.component.html',
  styleUrl: './landing.component.scss'
})
export class LandingComponent {
  protected readonly auth = inject(AuthService);

  protected signUp(): void {
    void this.auth.loginWithRedirect({
      authorizationParams: {
        screen_hint: 'signup'
      },
      appState: {
        target: '/app'
      }
    });
  }

  protected logIn(): void {
    void this.auth.loginWithRedirect({
      appState: {
        target: '/app'
      }
    });
  }

  protected logOut(): void {
    void this.auth.logout({
      logoutParams: {
        returnTo: window.location.origin
      }
    });
  }
}
