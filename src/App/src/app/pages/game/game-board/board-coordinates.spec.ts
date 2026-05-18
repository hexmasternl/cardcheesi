import { describe, it, expect } from 'vitest';
import { Pawn } from '../game-state.model';
import {
  boardPositionToWorld,
  finishPositionToWorld,
  RESERVE_POSITIONS,
  resolveWorldPosition,
} from './board-coordinates';

function makePawn(location: Pawn['location']): Pawn {
  return { id: 'p1', ownerId: 'o1', status: 1, location };
}

describe('resolveWorldPosition', () => {
  it('returns the correct reserve position for a reserve pawn', () => {
    const pawn = makePawn({ $type: 'reserve' });
    const result = resolveWorldPosition(pawn, 0, 2);
    expect(result).toEqual(RESERVE_POSITIONS[0][2]);
  });

  it('returns the correct position for each player reserve slot', () => {
    for (let playerIndex = 0; playerIndex < 4; playerIndex++) {
      for (let reserveIndex = 0; reserveIndex < 4; reserveIndex++) {
        const pawn = makePawn({ $type: 'reserve' });
        const result = resolveWorldPosition(pawn, playerIndex, reserveIndex);
        expect(result).toEqual(RESERVE_POSITIONS[playerIndex][reserveIndex]);
      }
    }
  });

  it('returns the board world position for a board pawn', () => {
    const pawn = makePawn({ $type: 'board', position: 10 });
    const result = resolveWorldPosition(pawn, 1, 0);
    expect(result).toEqual(boardPositionToWorld(10));
  });

  it('returns the finish world position for a finish pawn', () => {
    const pawn = makePawn({ $type: 'finish', slot: 3 });
    const result = resolveWorldPosition(pawn, 2, 0);
    expect(result).toEqual(finishPositionToWorld(2, 3));
  });

  it('ignores reserveIndex for board pawns', () => {
    const pawn = makePawn({ $type: 'board', position: 33 });
    expect(resolveWorldPosition(pawn, 0, 0)).toEqual(resolveWorldPosition(pawn, 0, 3));
  });

  it('ignores reserveIndex for finish pawns', () => {
    const pawn = makePawn({ $type: 'finish', slot: 1 });
    expect(resolveWorldPosition(pawn, 3, 0)).toEqual(resolveWorldPosition(pawn, 3, 2));
  });
});
