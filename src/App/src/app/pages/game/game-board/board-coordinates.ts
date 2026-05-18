import { Color3 } from '@babylonjs/core';

/** One PBR albedo colour per player slot (index 0–3). */
export const PLAYER_COLORS: Color3[] = [
  new Color3(0.12, 0.70, 0.20), // P1 – green
  new Color3(0.85, 0.12, 0.12), // P2 – red
  new Color3(0.88, 0.75, 0.02), // P3 – yellow
  new Color3(0.12, 0.28, 0.85), // P4 – blue
];

/**
 * Reserve spot world positions (X, Y, Z) per player slot.
 * Corner cylinder clusters at ±0.50/0.58 on X/Z, board surface at Y = 0.006.
 */
export const RESERVE_POSITIONS: [number, number, number][][] = [
  [[-0.58, 0.006, -0.58], [-0.50, 0.006, -0.58], [-0.50, 0.006, -0.50], [-0.58, 0.006, -0.50]],
  [[ 0.58, 0.006, -0.50], [ 0.50, 0.006, -0.50], [ 0.50, 0.006, -0.58], [ 0.58, 0.006, -0.58]],
  [[ 0.58, 0.006,  0.58], [ 0.50, 0.006,  0.58], [ 0.50, 0.006,  0.50], [ 0.58, 0.006,  0.50]],
  [[-0.58, 0.006,  0.50], [-0.50, 0.006,  0.50], [-0.50, 0.006,  0.58], [-0.58, 0.006,  0.58]],
];

/** Y-height for pawns on the board surface. */
export const BOARD_Y = 0.012;

/**
 * Maps a board position (1–64) to world XZ coordinates.
 *
 * Positions run clockwise:
 *   1–16  bottom row (left → right)
 *  17–32  right column (bottom → top)
 *  33–48  top row (right → left)
 *  49–64  left column (top → bottom)
 */
export function boardPositionToWorld(position: number): [number, number, number] {
  const INNER = 0.38;
  const OUTER = 0.43;
  const SPAN  = OUTER * 2;
  const t = (n: number) => n / 15; // normalised 0..1 over 16 squares (15 intervals)

  if (position >= 1 && position <= 16) {
    return [-OUTER + t(position - 1) * SPAN, BOARD_Y, -INNER];
  } else if (position >= 17 && position <= 32) {
    return [INNER, BOARD_Y, -OUTER + t(position - 17) * SPAN];
  } else if (position >= 33 && position <= 48) {
    return [OUTER - t(position - 33) * SPAN, BOARD_Y, INNER];
  } else {
    return [-INNER, BOARD_Y, OUTER - t(position - 49) * SPAN];
  }
}

/**
 * Maps a finish slot (1–4) for a given player index to world XZ coordinates.
 * Each player's finish track runs from the board edge toward the centre.
 */
export function finishPositionToWorld(
  playerIndex: number,
  slot: number,
): [number, number, number] {
  const s    = slot - 1; // 0-based
  const step = 0.075;
  switch (playerIndex) {
    case 0:  return [0,                   BOARD_Y, -0.26 + s * step]; // P1 bottom → inward
    case 1:  return [0.26 - s * step,     BOARD_Y, 0];                // P2 right  → inward
    case 2:  return [0,                   BOARD_Y,  0.26 - s * step]; // P3 top    → inward
    case 3:  return [-0.26 + s * step,    BOARD_Y, 0];                // P4 left   → inward
    default: return [0, BOARD_Y, 0];
  }
}
