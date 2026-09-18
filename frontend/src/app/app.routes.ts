import { Routes } from '@angular/router';
import { authGuardFn } from '@auth0/auth0-angular';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./pages/landing/landing.component').then((module) => module.LandingComponent)
  },
  {
    path: 'app',
    loadComponent: () =>
      import('./pages/dashboard/dashboard.component').then((module) => module.DashboardComponent),
    canActivate: [authGuardFn]
  },
  {
    path: 'app/ladders/new',
    loadComponent: () =>
      import('./pages/create-ladder/create-ladder.component').then(
        (module) => module.CreateLadderComponent
      ),
    canActivate: [authGuardFn]
  },
  {
    path: 'app/ladders/:ladderId/setup',
    loadComponent: () =>
      import('./pages/ladder-setup/ladder-setup.component').then(
        (module) => module.LadderSetupComponent
      ),
    canActivate: [authGuardFn]
  },
  {
    path: 'app/ladders/:ladderId',
    loadComponent: () =>
      import('./pages/ladder-detail/ladder-detail.component').then(
        (module) => module.LadderDetailComponent
      ),
    canActivate: [authGuardFn]
  },
  {
    path: '**',
    redirectTo: ''
  }
];
