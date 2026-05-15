export interface Card {
  /** 0 = Clubs, 1 = Diamonds, 2 = Hearts, 3 = Spades */
  suit: number;
  /** 1 = Ace, 2–10, 11 = Jack, 12 = Queen, 13 = King */
  rank: number;
}

export interface PlayerHand {
  playerId: string;
  cards: Card[];
}

export interface TurnState {
  activePlayerId: string;
  dealerId: string;
  roundNumber: number;
  cardsThisRound: number;
}

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
  turn: TurnState | null;
  deck: unknown | null;
  hands: PlayerHand[] | null;
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
