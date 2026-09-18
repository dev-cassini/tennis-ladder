export interface LadderSummary {
  id: string;
  name: string;
  status: 'Draft' | 'Active';
  roles: Array<'Organizer' | 'Player'>;
  playerCount: number;
}

export interface LadderPlayer {
  membershipId: string;
  userId: string;
  displayName: string;
  email: string;
  position: number;
}

export interface LadderSetup {
  id: string;
  name: string;
  status: 'Draft' | 'Active';
  players: LadderPlayer[];
}

export interface LadderStanding {
  membershipId: string;
  userId: string;
  displayName: string;
  position: number;
  isCurrentUser: boolean;
}

export interface LadderDetail {
  id: string;
  name: string;
  status: 'Draft' | 'Active';
  launchedUtc: string | null;
  roles: Array<'Organizer' | 'Player'>;
  standings: LadderStanding[];
}
