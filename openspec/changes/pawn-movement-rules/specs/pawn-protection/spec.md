## ADDED Requirements

### Requirement: Pawn is protected when placed at home position
A pawn SHALL be marked as protected (`IsProtected = true`) immediately when it is brought into play at the player's home position via an Ace or King card.

#### Scenario: Newly entered pawn is protected
- **WHEN** a player plays an Ace or King and brings a pawn from reserve to home
- **THEN** the pawn's `IsProtected` property is `true`

### Requirement: Protection is lost on movement
A pawn that is on its home position SHALL lose its protection (`IsProtected = false`) as soon as it is moved to any other position.

#### Scenario: Moving a protected pawn removes protection
- **WHEN** a protected pawn at the home position is moved (e.g., via Ace +1)
- **THEN** the pawn's `IsProtected` property becomes `false`

### Requirement: Protected pawn cannot be hit by opponent
When an opponent's pawn moves to the same board position as a protected pawn, the landing SHALL be rejected. The protected pawn remains in place; the move is invalid.

#### Scenario: Opponent cannot land on protected pawn
- **WHEN** an opponent pawn advances to a position occupied by a protected pawn
- **THEN** the move is rejected and the protected pawn is not hit

### Requirement: No pawn may pass a protected pawn
A pawn moving through the board (forward or backward) SHALL be blocked if any position along its path is occupied by a protected pawn of another player. The move is rejected if the path is blocked.

#### Scenario: Path blocked by protected home pawn
- **WHEN** a pawn's forward path includes a board position occupied by a protected pawn of another player
- **THEN** the move is rejected

#### Scenario: Teammate cannot pass a protected pawn either
- **WHEN** a player's teammate's pawn is protected at their home position
- **THEN** neither opponents nor the player's own pawns may pass it

### Requirement: Protected pawn cannot be swapped by Jack unless owner consents
A Jack swap involving a protected pawn SHALL be rejected if the protected pawn belongs to another player. The owner of a protected pawn MAY move it (losing protection) or participate in its swap.

#### Scenario: Jack swap of opponent's protected pawn is rejected
- **WHEN** a player plays Jack selecting an opponent's protected pawn as one of the two pawns
- **THEN** the swap is rejected

#### Scenario: Player may swap their own protected pawn
- **WHEN** a player plays Jack selecting their own protected pawn and an unprotected opponent pawn
- **THEN** the swap succeeds; the formerly protected pawn is no longer protected after the swap

### Requirement: Teammate proxy player may swap a protected pawn belonging to their teammate
When a player has all 4 of their own pawns finished and is controlling their teammate's pawns, they SHALL be permitted to swap a protected pawn belonging to that teammate.

#### Scenario: Proxy player swaps teammate's protected pawn
- **WHEN** player A has all pawns finished and controls teammate B's pawns; player A plays Jack selecting a protected pawn of player B and an unprotected pawn of another player
- **THEN** the swap is permitted and the formerly protected pawn loses protection

### Requirement: Pawn is permanently protected inside the finish area
Once a pawn occupies any finish slot, it SHALL be treated as protected for the remainder of the game and cannot be swapped, hit, or have opponents pass over it.

#### Scenario: Finish-area pawn cannot be swapped
- **WHEN** a player plays Jack selecting a pawn that is in a finish slot
- **THEN** the swap is rejected regardless of ownership
