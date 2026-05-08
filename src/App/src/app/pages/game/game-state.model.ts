export interface GamePlayer {
  id: string;
  name: string;
  pawns: unknown[];
}

export interface GameTeam {
  id: string;
  playerIds: string[];
}

export interface GameState {
  id: string;
  gameCode: string;
  /** 0 = Waiting, 1 = InProgress, 2 = Finished */
  status: 0 | 1 | 2;
  teams: GameTeam[];
  players: GamePlayer[];
  turn: unknown | null;
  deck: unknown | null;
  hands: unknown[] | null;
}

export const GameStatus = {
  Waiting: 0,
  InProgress: 1,
  Finished: 2,
} as const;

export const GameStatusLabel: Record<number, string> = {
  0: 'Waiting for players',
  1: 'In Progress',
  2: 'Finished',
};
