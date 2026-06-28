import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { shareReplay } from 'rxjs';

import { environment } from '../../environments/environment';
import { CurrentUser } from '../models/current-user.model';

@Injectable({ providedIn: 'root' })
export class CurrentUserService {
  private readonly http = inject(HttpClient);

  readonly currentUser$ = this.http
    .get<CurrentUser>(`${environment.apiBaseUrl}/api/me`)
    .pipe(shareReplay(1));
}
