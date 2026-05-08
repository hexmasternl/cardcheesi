# Tasks: create-game-domain-model

## Group 1: Project References

- [x] **1.1** Add `<ProjectReference>` from `CardCheesi.Game` → `CardCheesi.Game.Abstractions` in `CardCheesi.Game.csproj`
- [x] **1.2** Add `<ProjectReference>` from `CardCheesi.Game.Tests` → `CardCheesi.Game` in `CardCheesi.Game.Tests.csproj`; add Moq and Bogus package references
- [x] **1.3** Add `<ProjectReference>` from `CardCheesi.Game.Api` → `CardCheesi.Game` in `CardCheesi.Game.Api.csproj`

## Group 2: Domain Types — `CardCheesi.Game.Abstractions`

- [x] **2.1** Create `PawnStatus.cs` (enum: Reserve, InPlay, Finished) and `PawnLocation.cs` (sealed hierarchy: ReserveLocation, BoardLocation, FinishLocation)
- [x] **2.2** Create `Pawn.cs` and `Player.cs` records
- [x] **2.3** Create `Team.cs` record
- [x] **2.4** Create `CardSuit.cs`, `CardRank.cs` enums and `Card.cs` record
- [x] **2.5** Create `Deck.cs` record with `Standard()` factory and `Shuffle(IRandom rng)` method
- [x] **2.6** Create `PlayerHand.cs` record
- [x] **2.7** Create `TurnState.cs` record
- [x] **2.8** Create `GameState.cs` record

## Group 3: Game Logic — `CardCheesi.Game`

- [x] **3.1** Create `IRandom.cs` interface and `SystemRandom.cs` wrapper
- [x] **3.2** Create `GameFactory.cs` with `Create(IReadOnlyList<string> playerNames, IRandom? rng = null)` method

## Group 4: Unit Tests — `CardCheesi.Game.Tests`

- [x] **4.1** Create `GameFactoryTests.cs` covering: 4-player happy path, too few/too many players throws, pawns start in reserve, teams assigned correctly
- [x] **4.2** Create `DeckTests.cs` covering: `Standard()` returns 52 cards, no duplicates, `Shuffle` returns new deck with same cards in different order (seeded RNG), `Shuffle` does not mutate original
- [x] **4.3** Create `TurnStateTests.cs` covering: initial round = 1 cards = 5, round 2 and 3 cards = 4

## Group 5: API Cleanup — `CardCheesi.Game.Api`

- [x] **5.1** Remove WeatherForecast template code from `Program.cs` and add a stub `GET /game` endpoint that returns a 501 Not Implemented response

## Group 6: Verification

- [x] **6.1** Run `dotnet build src/card-cheesi.slnx` — confirm zero errors
- [x] **6.2** Run `dotnet test src/card-cheesi.slnx` — confirm all tests pass
