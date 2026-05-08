## ADDED Requirements

### Requirement: Game page route exists
The Angular application SHALL register a lazy-loaded route at `/game/:gameCode` that loads the `GamePage` component from `src/app/pages/game/game-page.ts`.

#### Scenario: Valid game code navigates to game page
- **WHEN** the user navigates to `/game/ABC123`
- **THEN** the `GamePage` component is rendered

#### Scenario: Route parameter is accessible
- **WHEN** the `GamePage` component initialises
- **THEN** the `:gameCode` route parameter is available and used to fetch game state

---

### Requirement: Full-screen 3D board background
The `GamePage` SHALL display a full-screen background using a pre-rendered image of the CardCheesi board (`assets/board-render.webp`). The image SHALL cover the entire viewport, be centred, and not repeat.

#### Scenario: Background fills the viewport
- **WHEN** the game page is displayed at any viewport size
- **THEN** the board render image covers the full width and height of the viewport without letterboxing or tiling

#### Scenario: Fallback background colour
- **WHEN** the board render image fails to load
- **THEN** a dark background colour consistent with the CardCheesi theme is displayed instead

---

### Requirement: Game state is fetched on init
On initialisation the `GamePage` SHALL call `GET /api/games/{gameCode}` and display the returned `GameState`. While loading, a spinner SHALL be shown. If the API returns 404, a user-friendly "Game not found" message SHALL be displayed.

#### Scenario: Successful game state fetch
- **WHEN** the component initialises with a valid `:gameCode`
- **THEN** `GET /api/games/{gameCode}` is called once
- **THEN** the loading spinner is replaced with game content

#### Scenario: Game not found
- **WHEN** the API returns HTTP 404
- **THEN** a "Game not found" error message is displayed
- **THEN** a navigation link back to the home page is provided

#### Scenario: Network error
- **WHEN** the API call fails with a non-404 error
- **THEN** an error message is shown with a retry option

---

### Requirement: Player list is displayed
The `GamePage` SHALL display all players currently in the game, showing each player's name and their pawn colour / team assignment.

#### Scenario: Players are listed
- **WHEN** the game state is successfully loaded
- **THEN** all players in `GameState.Players` are shown with their names

---

### Requirement: Refresh action
The `GamePage` SHALL provide a visible refresh button that re-fetches the game state from the API without a full page reload.

#### Scenario: User triggers refresh
- **WHEN** the user clicks the refresh button
- **THEN** `GET /api/games/{gameCode}` is called again
- **THEN** the displayed game state is updated with the latest data
