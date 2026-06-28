import { Routes } from '@angular/router';
import { authGuardFn } from '@auth0/auth0-angular';

import { DashboardComponent } from './pages/dashboard/dashboard.component';
import { LandingComponent } from './pages/landing/landing.component';

export const routes: Routes = [
  {
    path: '',
    component: LandingComponent
  },
  {
    path: 'app',
    component: DashboardComponent,
    canActivate: [authGuardFn]
  },
  {
    path: '**',
    redirectTo: ''
  }
];
