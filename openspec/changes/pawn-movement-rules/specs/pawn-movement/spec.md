## ADDED Requirements

### Requirement: Board positions form a 64-position loop
The board SHALL consist of positions 1–64. After position 64 the loop wraps back to position 1. Pawns advance clockwise through the loop.

#### Scenario: Pawn wraps from 64 to 1
- **WHEN** a pawn on position 62 advances 4 steps
- **THEN** the pawn's new position is 2 (wraps through 64→1)

### Requirement: Each player has a fixed home position
Player home positions SHALL be: Player 1 → 1, Player 2 → 17, Player 3 → 33, Player 4 → 49 (determined by player index 0–3 in turn order).

#### Scenario: Player 2 home position is 17
- **WHEN** a Player 2 pawn is brought into play
- **THEN** the pawn is placed at board position 17

### Requirement: Pawn enters play only with Ace or King
A pawn in reserve SHALL only be brought onto the board when the active player plays an Ace or King. Any other card cannot place a reserve pawn.

#### Scenario: Ace brings pawn from reserve to home
- **WHEN** a player plays an Ace and chooses to enter a pawn
- **THEN** one reserve pawn is moved to the player's home position

#### Scenario: Numbered card cannot enter reserve pawn
- **WHEN** a player plays a Two with all pawns in reserve
- **THEN** the move is rejected and no state changes

### Requirement: Numbered cards advance a pawn by face value
Cards Two through Ten and Queen (value 12) SHALL advance an in-play pawn forward by the card's face value positions along the board loop.

#### Scenario: Queen advances pawn 12 positions
- **WHEN** a player plays a Queen and selects an in-play pawn
- **THEN** the pawn advances 12 positions forward

#### Scenario: Numbered card cannot move a reserve pawn
- **WHEN** a player plays a Six and chooses a pawn in reserve
- **THEN** the move is rejected

### Requirement: Four reverses a pawn by 4 positions
Playing a Four SHALL move one in-play pawn backward 4 positions along the board loop.

#### Scenario: Four moves pawn backward
- **WHEN** a player plays a Four and selects a pawn at position 20
- **THEN** the pawn moves to position 16

#### Scenario: Four wraps backward from position 2
- **WHEN** a player plays a Four and selects a pawn at position 2
- **THEN** the pawn moves to position 62 (wraps through 1→64)

### Requirement: Seven can be played as single or split move
Playing a Seven SHALL allow moving one pawn 7 positions, or splitting the 7 steps across at most two in-play pawns in any valid combination (e.g., 1+6, 2+5, 3+4).

#### Scenario: Seven moves one pawn 7 positions
- **WHEN** a player plays a Seven with a single-pawn move of 7
- **THEN** the selected pawn advances 7 positions

#### Scenario: Seven split 3+4 across two pawns
- **WHEN** a player plays a Seven with a split of 3 steps to pawn A and 4 steps to pawn B
- **THEN** pawn A advances 3 positions and pawn B advances 4 positions

#### Scenario: Seven split steps must total 7
- **WHEN** a player submits a split of 2+6 for a Seven card
- **THEN** the move is rejected (2+6=8, not 7)

### Requirement: Jack swaps two pawns of different colors
Playing a Jack SHALL swap the board positions of exactly two in-play pawns that belong to different players.

#### Scenario: Jack swaps positions of two opponent pawns
- **WHEN** a player plays a Jack selecting pawn A (their own) and pawn B (opponent's, unprotected)
- **THEN** pawn A moves to pawn B's former position and pawn B moves to pawn A's former position

#### Scenario: Jack cannot swap two pawns of the same color
- **WHEN** a player plays a Jack selecting two of their own pawns
- **THEN** the swap is rejected

#### Scenario: Jack cannot swap a pawn in the finish area
- **WHEN** a player plays a Jack selecting a pawn that is in the finish area
- **THEN** the swap is rejected

### Requirement: A pawn landing on an unprotected opponent pawn hits it
When a pawn advances to a board position occupied by an unprotected pawn of another player (or the opposing team), the occupying pawn SHALL be returned to its owner's reserve.

#### Scenario: Moving pawn hits unprotected opponent
- **WHEN** a pawn advances to position 25 where an unprotected opponent pawn sits
- **THEN** the opponent pawn is returned to its owner's reserve, and the moving pawn occupies position 25

### Requirement: A pawn cannot pass a protected pawn
A pawn moving forward (or backward for Four) SHALL be blocked if its path crosses a protected pawn's position. The move SHALL be rejected if the destination or any intermediate position contains a protected pawn belonging to another player.

#### Scenario: Forward move blocked by protected pawn
- **WHEN** a pawn at position 10 attempts to advance 5 steps and position 13 holds a protected pawn owned by another player
- **THEN** the move is rejected

#### Scenario: Backward Four blocked by protected pawn
- **WHEN** a pawn at position 18 attempts Four (backward 4) and position 15 holds a protected pawn owned by another player
- **THEN** the move is rejected

### Requirement: Pawn enters the finish area after completing a full loop
Once a pawn has travelled past its player's home position for the second time (completing the loop), the pawn SHALL enter the personal finish corridor (finish slots 1–4) in sequence. Finish slot 1 is the entry slot closest to the main board.

#### Scenario: Pawn enters finish area
- **WHEN** a pawn's advance calculation carries it past the main loop into the finish corridor
- **THEN** the pawn's location changes to a FinishLocation with the correct slot number

#### Scenario: Pawn cannot leave finish area
- **WHEN** a move is attempted on a pawn already in a finish slot
- **THEN** the pawn only advances deeper into the finish area; it cannot re-enter the main board

### Requirement: A pawn in the finish area is permanently protected
Once a pawn enters the finish area, it SHALL be protected for the remainder of the game. No pawn may pass or jump over a pawn occupying a finish slot.

#### Scenario: No pawn may pass a finish-area pawn
- **WHEN** another pawn's advance would carry it past a finish slot occupied by any pawn
- **THEN** the passing pawn is blocked and the move is rejected

### Requirement: Finish slots fill in order
Finish slots SHALL be filled from slot 1 (nearest board entry) upward to slot 4, so the entry path remains clear for subsequent pawns.

#### Scenario: Second pawn enters next available finish slot
- **WHEN** slot 1 is occupied and another pawn enters the finish corridor
- **THEN** the new pawn is placed in slot 2

### Requirement: Player may move teammate's pawns when own pawns are finished
Once a player's own 4 pawns are all in the finish area, that player SHALL be permitted to move their teammate's pawns for the remainder of the game.

#### Scenario: Player controls teammate pawns after finishing own
- **WHEN** a player's 4 pawns are all in the finish area and they play a card
- **THEN** they may select a valid move for any of their teammate's pawns
