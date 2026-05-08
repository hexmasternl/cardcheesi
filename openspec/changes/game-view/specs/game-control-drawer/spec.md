# Spec: Game Control Drawer

## Overview

A bottom-anchored slide-up drawer gives the active player full control of their turn. When it is **not** the local player's turn the drawer is collapsed to a thin status bar. When it **is** the local player's turn the drawer slides up automatically, revealing the player's hand and all interaction controls needed to play or dispose cards.

The drawer is not accessible to spectators or players whose turn it is not.

---

## ADDED Requirements

### Requirement: Drawer reflects whose turn it is
The `GamePage` SHALL monitor `GameState.turn.activePlayerId` and compare it to the locally stored `playerId` (persisted in `localStorage` after joining a game).

#### Scenario: Not the local player's turn
- **WHEN** `turn.activePlayerId` does not match the local `playerId`
- **THEN** the drawer is shown collapsed (status bar only)
- **THEN** the status bar displays the name of the active player, e.g. "Alice's turn…"

#### Scenario: Local player's turn begins
- **WHEN** `turn.activePlayerId` matches the local `playerId`
- **THEN** the drawer slides up automatically with an easing transition
- **THEN** the player's hand of cards is revealed face-up

---

### Requirement: Drawer shows the player's hand
When the drawer is open (local player's turn), it SHALL display all cards currently held by the local player, sourced from `GameState.hands`.

#### Scenario: Cards are displayed
- **WHEN** the drawer is open
- **THEN** each card in the local player's hand is rendered as a face-up playing card showing its rank and suit
- **THEN** cards are arranged in a fan or horizontal row that is scrollable on small screens

#### Scenario: No cards in hand
- **WHEN** the player has no cards (edge case at round boundary)
- **THEN** a message "Waiting for cards to be dealt…" is shown

---

### Requirement: Card selection
The player SHALL be able to select exactly one card to play each turn.

#### Scenario: Selecting a card
- **WHEN** the player taps/clicks a card in their hand
- **THEN** that card is highlighted (elevated, outlined in primary colour)
- **THEN** previously selected card (if any) is deselected

#### Scenario: Deselecting a card
- **WHEN** the player taps/clicks the already-selected card
- **THEN** the card is deselected and pawn selection is reset

---

### Requirement: Pawn selection after card selection
After selecting a card, the page SHALL indicate which pawns are eligible and prompt the player to select the required number of pawns.

#### Scenario: Card requiring 1 pawn (Ace/+1, Two–Ten, Queen, Four, King)
- **WHEN** the player selects any card other than Jack or Seven with ≥2 pawns
- **THEN** exactly 1 pawn must be selected to proceed

#### Scenario: Jack — swap (2 pawns, different teams)
- **WHEN** the player selects a Jack
- **THEN** exactly 2 pawns must be selected
- **THEN** both selected pawns MUST belong to different teams; if the player selects two same-team pawns a validation message is shown and the selection is blocked

#### Scenario: Seven — split over 1 or 2 pawns
- **WHEN** the player selects a Seven
- **THEN** 1 or 2 pawns may be selected
- **IF** 1 pawn is selected: all 7 steps go to that pawn
- **IF** 2 pawns are selected: a split control appears showing two step counters (pawn A steps: [1–6], pawn B steps: auto-calculated = 7 − A steps); the split must sum to exactly 7

#### Scenario: Ace — enter or move +1
- **WHEN** the player selects an Ace
- **THEN** a choice is presented: "Enter reserve pawn" or "Move pawn +1"
- **THEN** the pawn selection adjusts accordingly (reserve pawn for enter, any InPlay pawn for +1)

#### Scenario: King — enter only
- **WHEN** the player selects a King
- **THEN** only reserve pawns are eligible for selection (pawn enters at home position)

---

### Requirement: Eligible pawn highlighting on the board
When a card is selected, eligible pawns SHALL be visually highlighted on the board overlay.

#### Scenario: Pawns highlighted
- **WHEN** a card is selected
- **THEN** pawns that are eligible for the selected card/move type are visually distinguished (glow or ring in primary colour)
- **THEN** ineligible pawns are dimmed

---

### Requirement: Play Move button
The top bar of the drawer SHALL contain a "Play Move" button.

#### Scenario: Button is disabled initially
- **WHEN** no card or insufficient pawns are selected
- **THEN** the "Play Move" button is disabled

#### Scenario: Button becomes enabled
- **WHEN** a valid card and the required pawn(s) are selected (and split total = 7 for Seven)
- **THEN** the "Play Move" button is enabled

#### Scenario: Player plays the move
- **WHEN** the player clicks "Play Move"
- **THEN** a `POST /api/games/{code}/move` request is sent with the selected card, pawn ID(s), and any split values
- **THEN** the drawer collapses (turn has been passed)
- **THEN** the game state is refreshed

---

### Requirement: Dispose Cards button
The top bar SHALL contain a "Dispose Cards" button that is enabled when the active player has no valid move available.

#### Scenario: Button is shown but disabled when moves exist
- **WHEN** the player holds at least one playable card
- **THEN** the "Dispose Cards" button is visible but disabled

#### Scenario: Button is enabled when no moves are possible
- **WHEN** the frontend determines (client-side) that none of the player's cards can be legally played given the current pawn positions
- **THEN** the "Dispose Cards" button becomes enabled

#### Scenario: Player disposes cards
- **WHEN** the player clicks "Dispose Cards"
- **THEN** a `POST /api/games/{code}/dispose` request is sent with the local player's ID
- **THEN** the drawer collapses (turn advances to next player)
- **THEN** the game state is refreshed

---

## Backend API

| Method | Path | Purpose |
|--------|------|---------|
| `POST` | `/api/games/{code}/move` | Play a card move |
| `POST` | `/api/games/{code}/dispose` | Dispose all cards for the active player |

### `PlayMoveRequest` body
```json
{
  "playerId": "uuid",
  "card": { "suit": 0, "rank": 7 },
  "pawnIds": ["uuid"],
  "sevenSplit": { "pawnASteps": 3, "pawnBSteps": 4 }
}
```
`sevenSplit` is only included when the card is a Seven and two pawns are selected.

### `DisposeCardsRequest` body
```json
{
  "playerId": "uuid"
}
```

Both endpoints return `204 No Content` on success, `400 Bad Request` with a validation error body on invalid move, and `404` if the game code is unknown.

---

## UX Notes

- The drawer uses the CardCheesi glass-morphic panel style with a dark semi-transparent background.
- Card components render the correct suit symbol (♠ ♥ ♦ ♣) using Unicode; red suits (`♥ ♦`) use the accent colour, black suits use `#e8f4ff`.
- The drawer top bar is always visible even when collapsed (shows status text + disabled buttons) so the player always has context.
- On mobile the drawer occupies the full viewport width and up to 60% of the viewport height when expanded.
