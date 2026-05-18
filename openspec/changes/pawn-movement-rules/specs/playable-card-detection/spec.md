## ADDED Requirements

### Requirement: Card is playable if at least one legal move exists
A card in a player's hand SHALL be considered playable if and only if there exists at least one legal move for that card given the current board state. A card with no legal moves is not playable.

#### Scenario: Ace is playable when a reserve pawn exists
- **WHEN** a player has at least one pawn in reserve
- **THEN** an Ace card in their hand is playable (enter move exists)

#### Scenario: Numbered card is not playable when all pawns are in reserve
- **WHEN** a player has all pawns in reserve and no pawns in play
- **THEN** a Two card is not playable

#### Scenario: Four is not playable when all forward-adjacent positions are blocked
- **WHEN** every in-play pawn would be blocked (path crosses a protected pawn) when reversed 4
- **THEN** the Four card is not playable

### Requirement: GetValidMoves enumerates all legal options for a card
`IGameState.GetValidMoves(Guid playerId, Card card)` SHALL return the complete list of legal `MoveOption` instances for the given card. An empty list means the card is not playable.

#### Scenario: GetValidMoves returns single-pawn options for numbered card
- **WHEN** a player has two in-play pawns and plays a Three
- **THEN** GetValidMoves returns up to two `SingleMove` options (one per unblocked pawn)

#### Scenario: GetValidMoves returns split options for Seven
- **WHEN** a player has two in-play pawns
- **THEN** GetValidMoves for a Seven card returns all valid (pawn, steps) and (pawn1, steps1, pawn2, steps2) combinations where steps total 7

#### Scenario: GetValidMoves returns swap options for Jack
- **WHEN** a player has one in-play pawn and opponents have unprotected, non-finish in-play pawns
- **THEN** GetValidMoves for a Jack returns SwapMove options for each valid pair

### Requirement: HasPlayableCards returns true only when at least one card is playable
`IGameState.HasPlayableCards(Guid playerId)` SHALL return `true` if any card in the player's hand is playable, and `false` if no card has any legal move.

#### Scenario: HasPlayableCards is true when one card is playable
- **WHEN** a player has five cards and one of them has at least one legal move
- **THEN** HasPlayableCards returns true

#### Scenario: HasPlayableCards is false when no card has a legal move
- **WHEN** every card in the player's hand has no legal move given the current board state
- **THEN** HasPlayableCards returns false

### Requirement: HasPlayableCards considers teammate pawns when player has finished
When a player's own 4 pawns are all in the finish area, `HasPlayableCards` SHALL evaluate moves against the teammate's in-play pawns instead.

#### Scenario: Finished player's playability based on teammate's pawns
- **WHEN** a player has all 4 own pawns finished and a teammate has moveable pawns
- **THEN** HasPlayableCards returns true if any card can move a teammate pawn

### Requirement: canDispose in the YourTurn event reflects HasPlayableCards
The `YourTurnEvent` SSE message sent to the active player SHALL set `canDispose = !HasPlayableCards(activePlayerId)`.

#### Scenario: canDispose is false when player has playable cards
- **WHEN** the active player has at least one playable card
- **THEN** the YourTurn SSE event carries `canDispose: false`

#### Scenario: canDispose is true when player has no playable cards
- **WHEN** the active player has no playable cards
- **THEN** the YourTurn SSE event carries `canDispose: true`
