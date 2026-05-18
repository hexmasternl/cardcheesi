## 1. Domain Model — Abstractions

- [ ] 1.1 Add `IsProtected` property to `IPawn` interface (`CardCheesi.Game.Abstractions`)
- [ ] 1.2 Add `HasPlayableCards(Guid playerId)` method to `IGameState` interface
- [ ] 1.3 Add `GetValidMoves(Guid playerId, Card card)` method to `IGameState` interface returning `IReadOnlyList<MoveOption>`
- [ ] 1.4 Add `SwapPawns(Guid pawnId1, Guid pawnId2)` method to `IGameState` interface (Jack move)
- [ ] 1.5 Add `MakeSplitMove(Guid pawnId1, int spaces1, Guid? pawnId2, int spaces2)` method to `IGameState` interface (Seven split)
- [ ] 1.6 Define `MoveOption` abstract record with subtypes `SingleMove(Guid PawnId, int Steps)`, `SplitMove(Guid PawnId1, int Steps1, Guid? PawnId2, int Steps2)`, `SwapMove(Guid PawnId1, Guid PawnId2)` in `CardCheesi.Game.Abstractions`

## 2. Domain Model — Concrete Records

- [ ] 2.1 Add `IsProtected` property to `Pawn` record in `CardCheesi.Game`
- [ ] 2.2 Implement `SwapPawns` and `MakeSplitMove` on the concrete `GameState` record (delegates to `BoardRules`)
- [ ] 2.3 Implement `HasPlayableCards` on `GameState` (iterates hand cards, calls `GetValidMoves`, returns true if any list is non-empty)
- [ ] 2.4 Implement `GetValidMoves` on `GameState` (dispatches by card rank to per-card enumeration logic)

## 3. Board Rules Engine

- [ ] 3.1 Create `CardCheesi.Game.Rules.BoardRules` static class
- [ ] 3.2 Implement `HomePosition(int playerIndex)` → 1 / 17 / 33 / 49
- [ ] 3.3 Implement `FinishEntryPosition(int playerIndex)` — board position just before each player's finish corridor
- [ ] 3.4 Implement `Advance(BoardLocation from, int steps, int playerIndex, IReadOnlyList<Pawn> allPawns)` → returns destination `PawnLocation` or throws `InvalidMoveException` if blocked
- [ ] 3.5 Implement `Retreat(BoardLocation from, int steps, IReadOnlyList<Pawn> allPawns)` → used by Four; returns destination or throws if blocked
- [ ] 3.6 Implement `IsPathClear(int fromPosition, int toPosition, bool forward, IReadOnlyList<Pawn> allPawns, Guid movingPawnOwnerId)` — returns false if any intermediate or target position holds a protected pawn of another player
- [ ] 3.7 Implement finish-area entry logic: detect when an advance overshoots the main loop into the finish corridor, place pawn in correct `FinishLocation` slot

## 4. Per-Card Move Enumeration

- [ ] 4.1 Ace: enumerate `SingleMove(pawnId, 1)` for each in-play pawn (forward +1) + `SingleMove(reservePawnId, 0)` enter-play option for each reserve pawn
- [ ] 4.2 King: enumerate enter-play option for each reserve pawn
- [ ] 4.3 Four: enumerate `SingleMove(pawnId, -4)` for each in-play pawn (backward) filtered by `IsPathClear`
- [ ] 4.4 Seven: enumerate all valid `SplitMove` combinations (single-pawn 7, plus all p1/s1/p2/s2 pairs where s1+s2=7 and each sub-move is individually valid)
- [ ] 4.5 Jack: enumerate valid `SwapMove(pawnId1, pawnId2)` pairs — different owners, neither in finish area, target not protected (unless own or proxy teammate)
- [ ] 4.6 Numbered cards (2,3,5,6,8,9,10) and Queen (12): enumerate `SingleMove(pawnId, value)` for each in-play pawn filtered by `IsPathClear`

## 5. GameState.MakeMove / PlayCard Implementation

- [ ] 5.1 Implement `GameState.MakeMove(Guid pawnId, int spaces)` — validates and applies forward/backward move, clears protection, resolves hit if target occupied, updates pawn location
- [ ] 5.2 Implement `GameState.SwapPawns(Guid pawnId1, Guid pawnId2)` — validates Jack rules (different owners, not finish, protection check), swaps locations, clears protection on both
- [ ] 5.3 Implement `GameState.MakeSplitMove` — validates Seven split (steps sum to 7, each sub-move individually valid), applies both moves in sequence
- [ ] 5.4 Implement `GameState.PlayCard(Guid playerId, Card card)` — verifies card is in hand, removes it from the player's hand, returns updated state
- [ ] 5.5 On entering finish area: set `IsProtected = true` on the pawn; on entering from reserve (Ace/King): set `IsProtected = true`

## 6. YourTurn SSE Wiring

- [ ] 6.1 Update the handler/service that broadcasts `YourTurnEvent` to call `gameState.HasPlayableCards(activePlayerId)` and set `canDispose = !result`

## 7. Unit Tests

- [ ] 7.1 `BoardRulesTests` — home positions, finish entry, wrap-around, path-clear checks
- [ ] 7.2 `MakeMoveTests` — forward moves for each numbered card, board wrap, hit resolution
- [ ] 7.3 `FourMoveTests` — backward 4, backward wrap, blocked by protected pawn
- [ ] 7.4 `SevenMoveTests` — single-pawn 7, all valid split combinations, invalid split sum rejected
- [ ] 7.5 `JackMoveTests` — valid swap, same-color rejected, finish-area rejected, protected-pawn rejected, own protected allowed, proxy teammate allowed
- [ ] 7.6 `ProtectionTests` — protection set on home entry, cleared on move, cleared on swap, finish area permanently protected, cannot pass/hit protected pawn
- [ ] 7.7 `PlayableCardDetectionTests` — HasPlayableCards true/false, GetValidMoves coverage per card type, canDispose computed correctly, teammate pawns used when own are finished
