import { computed, Injectable, signal } from '@angular/core';
import { Card, GameState, MakeMoveRequest, Pawn, PawnStatus } from './game-state.model';

export type TurnPhase =
  | 'idle'
  | 'needs-ace-choice'
  | 'needs-pawn'
  | 'needs-seven-steps'
  | 'needs-pawn-2'
  | 'ready';

export type AceChoice = 'enter' | 'advance';

@Injectable({ providedIn: 'root' })
export class TurnFlowStore {
  // ── Inputs set by GamePage ──────────────────────────────────────────────────
  readonly gameState = signal<GameState | null>(null);
  readonly myPlayerId = signal<string | null>(null);

  // ── Private mutable state ───────────────────────────────────────────────────
  private readonly _phase = signal<TurnPhase>('idle');
  private readonly _selectedCard = signal<Card | null>(null);
  private readonly _selectedPawnId1 = signal<string | null>(null);
  private readonly _selectedPawnId2 = signal<string | null>(null);
  private readonly _sevenSteps1 = signal<number | null>(null);
  private readonly _aceChoice = signal<AceChoice | null>(null);

  // ── Public readable signals ─────────────────────────────────────────────────
  readonly phase = this._phase.asReadonly();
  readonly selectedCard = this._selectedCard.asReadonly();
  readonly showAcePopup = computed(() => this._phase() === 'needs-ace-choice');
  readonly showSevenPopup = computed(() => this._phase() === 'needs-seven-steps');
  readonly canPlay = computed(() => this._phase() === 'ready');

  readonly blinkingPawnIds = computed<string[]>(() => {
    const p1 = this._selectedPawnId1();
    const p2 = this._selectedPawnId2();
    return [p1, p2].filter((id): id is string => id !== null);
  });

  readonly selectablePawnIds = computed<string[]>(() => {
    const phase = this._phase();
    const card = this._selectedCard();
    const myId = this.myPlayerId();
    const state = this.gameState();
    if (!card || !myId || !state) return [];

    const myPlayer = state.players.find((p) => p.id === myId);
    if (!myPlayer) return [];

    const rank = card.rank;

    if (phase === 'needs-pawn') {
      const aceChoice = this._aceChoice();

      // King or Ace-enter: only reserve pawns
      if (rank === 13 || (rank === 1 && aceChoice === 'enter')) {
        return myPlayer.pawns
          .filter((p) => p.status === PawnStatus.Reserve)
          .map((p) => p.id);
      }

      // Jack: all board pawns from any player
      if (rank === 11) {
        return state.players
          .flatMap((p) => p.pawns)
          .filter((p) => p.status === PawnStatus.InPlay)
          .map((p) => p.id);
      }

      // All others (including Ace-advance): own board pawns
      return myPlayer.pawns
        .filter((p) => p.status === PawnStatus.InPlay)
        .map((p) => p.id);
    }

    if (phase === 'needs-pawn-2') {
      const pawn1Id = this._selectedPawnId1();

      // Seven split: own board pawns except pawn1
      if (rank === 7) {
        return myPlayer.pawns
          .filter((p) => p.status === PawnStatus.InPlay && p.id !== pawn1Id)
          .map((p) => p.id);
      }

      // Jack second: all board pawns except pawn1
      return state.players
        .flatMap((p) => p.pawns)
        .filter((p) => p.status === PawnStatus.InPlay && p.id !== pawn1Id)
        .map((p) => p.id);
    }

    return [];
  });

  readonly movePayload = computed<MakeMoveRequest | null>(() => {
    if (this._phase() !== 'ready') return null;
    const card = this._selectedCard()!;
    const pawnId = this._selectedPawnId1()!;
    const rank = card.rank;

    if (rank === 11) {
      // Jack
      return { cardSuit: card.suit, cardRank: rank, pawnId, pawnId2: this._selectedPawnId2()! };
    }

    if (rank === 7) {
      const steps = this._sevenSteps1()!;
      const pawnId2 = this._selectedPawnId2();
      if (pawnId2) {
        return { cardSuit: card.suit, cardRank: rank, pawnId, pawnId2, steps };
      }
      return { cardSuit: card.suit, cardRank: rank, pawnId, steps: 7 };
    }

    if (rank === 1) {
      // Ace: steps = 0 (enter) or 1 (advance)
      const steps = this._aceChoice() === 'enter' ? 0 : 1;
      return { cardSuit: card.suit, cardRank: rank, pawnId, steps };
    }

    if (rank === 4) {
      return { cardSuit: card.suit, cardRank: rank, pawnId, steps: -4 };
    }

    // All other cards: steps = rank value (2,3,5,6,8,9,10,12,13)
    return { cardSuit: card.suit, cardRank: rank, pawnId, steps: rank };
  });

  // ── Actions ─────────────────────────────────────────────────────────────────

  selectCard(card: Card): void {
    this._selectedCard.set(card);
    this._selectedPawnId1.set(null);
    this._selectedPawnId2.set(null);
    this._sevenSteps1.set(null);
    this._aceChoice.set(null);

    const myId = this.myPlayerId();
    const state = this.gameState();
    if (!myId || !state) return;

    const nextPhase = this.determineInitialPhase(card, state, myId);
    this._phase.set(nextPhase);
  }

  selectPawn(pawnId: string): void {
    const phase = this._phase();
    const card = this._selectedCard();
    if (!card) return;

    if (phase === 'needs-pawn') {
      this._selectedPawnId1.set(pawnId);

      if (card.rank === 7) {
        this._phase.set('needs-seven-steps');
      } else if (card.rank === 11) {
        this._phase.set('needs-pawn-2');
      } else {
        this._phase.set('ready');
      }
    } else if (phase === 'needs-pawn-2') {
      this._selectedPawnId2.set(pawnId);
      this._phase.set('ready');
    }
  }

  selectSevenSteps(steps: number): void {
    this._sevenSteps1.set(steps);
    if (steps === 7) {
      this._phase.set('ready');
    } else {
      this._phase.set('needs-pawn-2');
    }
  }

  selectAceChoice(choice: AceChoice): void {
    this._aceChoice.set(choice);
    const myId = this.myPlayerId();
    const state = this.gameState();
    if (!myId || !state) return;

    const myPlayer = state.players.find((p) => p.id === myId);
    if (!myPlayer) return;

    if (choice === 'enter') {
      const reservePawns = myPlayer.pawns.filter((p) => p.status === PawnStatus.Reserve);
      if (reservePawns.length === 0) return;
      this._selectedPawnId1.set(reservePawns[0].id);
      this._phase.set('ready');
    } else {
      const boardPawns = myPlayer.pawns.filter((p) => p.status === PawnStatus.InPlay);
      if (boardPawns.length === 1) {
        this._selectedPawnId1.set(boardPawns[0].id);
        this._phase.set('ready');
      } else {
        this._phase.set('needs-pawn');
      }
    }
  }

  reset(): void {
    this._phase.set('idle');
    this._selectedCard.set(null);
    this._selectedPawnId1.set(null);
    this._selectedPawnId2.set(null);
    this._sevenSteps1.set(null);
    this._aceChoice.set(null);
  }

  // ── Private helpers ─────────────────────────────────────────────────────────

  private determineInitialPhase(card: Card, state: GameState, myId: string): TurnPhase {
    const rank = card.rank;
    const myPlayer = state.players.find((p) => p.id === myId);
    if (!myPlayer) return 'idle';

    const reservePawns = myPlayer.pawns.filter((p) => p.status === PawnStatus.Reserve);
    const boardPawns = myPlayer.pawns.filter((p) => p.status === PawnStatus.InPlay);

    if (rank === 13) {
      // King: auto-select first reserve pawn
      if (reservePawns.length === 0) return 'idle';
      this._selectedPawnId1.set(reservePawns[0].id);
      return 'ready';
    }

    if (rank === 1) {
      // Ace: enter OR advance OR choose
      const hasReserve = reservePawns.length > 0;
      const hasBoard = boardPawns.length > 0;

      if (hasReserve && hasBoard) return 'needs-ace-choice';

      if (hasReserve) {
        this._aceChoice.set('enter');
        this._selectedPawnId1.set(reservePawns[0].id);
        return 'ready';
      }

      if (hasBoard) {
        this._aceChoice.set('advance');
        if (boardPawns.length === 1) {
          this._selectedPawnId1.set(boardPawns[0].id);
          return 'ready';
        }
        return 'needs-pawn';
      }

      return 'idle';
    }

    // All other cards need a board pawn to be selected
    return 'needs-pawn';
  }

  private getMyPawns(): Pawn[] {
    const myId = this.myPlayerId();
    const state = this.gameState();
    if (!myId || !state) return [];
    return state.players.find((p) => p.id === myId)?.pawns ?? [];
  }
}
