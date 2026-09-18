import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';

import { environment } from '../../environments/environment';
import { LadderDetail, LadderSetup, LadderSummary } from '../models/ladder.model';

@Injectable({ providedIn: 'root' })
export class LadderService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/api/ladders`;

  getLadders() {
    return this.http.get<LadderSummary[]>(this.baseUrl);
  }

  createLadder(name: string) {
    return this.http.post<LadderSummary>(this.baseUrl, { name });
  }

  getSetup(ladderId: string) {
    return this.http.get<LadderSetup>(`${this.baseUrl}/${ladderId}/setup`);
  }

  replacePlayers(
    ladderId: string,
    players: Array<{ displayName: string; email: string }>
  ) {
    return this.http.put<LadderSetup>(`${this.baseUrl}/${ladderId}/players`, { players });
  }

  updatePlayerOrder(ladderId: string, membershipIds: string[]) {
    return this.http.put<LadderSetup>(`${this.baseUrl}/${ladderId}/order`, {
      membershipIds
    });
  }

  launchLadder(ladderId: string) {
    return this.http.post<LadderDetail>(`${this.baseUrl}/${ladderId}/launch`, {});
  }

  getLadder(ladderId: string) {
    return this.http.get<LadderDetail>(`${this.baseUrl}/${ladderId}`);
  }
}
