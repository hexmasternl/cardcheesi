## ADDED Requirements

### Requirement: Rules route exists at /rules
The app SHALL expose a `/rules` route that lazy-loads the rules shell component. Navigating to `/rules` SHALL redirect to `/rules/overview`.

#### Scenario: Navigate to /rules
- **WHEN** the user navigates to `/rules`
- **THEN** the browser URL changes to `/rules/overview` and the overview chapter is displayed

#### Scenario: Direct deep-link to a chapter
- **WHEN** the user navigates directly to `/rules/cards`
- **THEN** the cards chapter is displayed without a full page reload

---

### Requirement: Rules shell with chapter navigation
The rules shell SHALL render a persistent chapter sidebar listing all five chapters (Overview, Board, Pawns, Cards, Gameplay) and a `<router-outlet>` for chapter content. The active chapter SHALL be visually highlighted in the sidebar.

#### Scenario: Sidebar shows all chapters
- **WHEN** the rules section is open on any chapter
- **THEN** five chapter links are visible in the sidebar

#### Scenario: Active chapter is highlighted
- **WHEN** the user is on the `/rules/board` page
- **THEN** the "Board" chapter link in the sidebar appears in the active/highlighted state

---

### Requirement: Language toggle (EN / NL)
The rules shell SHALL provide a language toggle button that switches all chapter content between English and Dutch. The selected language SHALL be stored in an Angular Signal and passed to child chapter components via `@Input`. Default language SHALL be English.

#### Scenario: Default language is English
- **WHEN** the user opens the rules section for the first time
- **THEN** all chapter text is displayed in English

#### Scenario: Toggle to Dutch
- **WHEN** the user clicks the NL toggle button
- **THEN** all visible chapter text switches to Dutch

#### Scenario: Toggle back to English
- **WHEN** the user clicks the EN toggle button after selecting Dutch
- **THEN** all visible chapter text switches back to English

---

### Requirement: Overview chapter
The overview chapter at `/rules/overview` SHALL explain the game concept, player count (4), team structure (Team A: P1+P3, Team B: P2+P4), pawn count (4 per player), and win condition. Content SHALL be available in both English and Dutch.

#### Scenario: Overview content is complete
- **WHEN** the user navigates to `/rules/overview`
- **THEN** the page shows game concept, teams, pawns per player, and win condition

---

### Requirement: Board chapter
The board chapter at `/rules/board` SHALL describe the 64-position loop, home positions for all four players (P1→1, P2→17, P3→33, P4→49), the finish area mechanics, and the rule that pawns in the finish area cannot leave. Content SHALL be available in both English and Dutch.

#### Scenario: Board chapter shows home positions
- **WHEN** the user navigates to `/rules/board`
- **THEN** a table or list of home positions for all four players is displayed

---

### Requirement: Pawns chapter
The pawns chapter at `/rules/pawns` SHALL explain: entering play via Ace/King, protection rules (newly entered pawn is protected, protected pawn cannot be hit or passed, loses protection on move), hitting mechanics (unprotected pawn sent to reserve), finish area permanence, and Jack swap restrictions. Content SHALL be available in both English and Dutch.

#### Scenario: Pawns chapter covers protection rules
- **WHEN** the user navigates to `/rules/pawns`
- **THEN** the page describes when a pawn is protected and what protection prevents

---

### Requirement: Cards chapter
The cards chapter at `/rules/cards` SHALL document every card type: Ace (enter or +1), King (enter), Four (−4 backwards), Seven (move one pawn 7 or split across ≤2 pawns), Jack (swap two different-colour pawns), Queen (+12), and all numbered cards (2,3,5,6,8,9,10 move forward by face value). Content SHALL be available in both English and Dutch.

#### Scenario: Cards chapter lists all card effects
- **WHEN** the user navigates to `/rules/cards`
- **THEN** every card type (Ace through King) is listed with its effect

---

### Requirement: Gameplay chapter
The gameplay chapter at `/rules/gameplay` SHALL describe the dealing sequence (rounds of 5, 4, 4 per dealer turn), clockwise turn order starting left of the dealer, the two possible actions per turn (play a card or discard all), and the rule that a player may move their teammate's pawns after finishing their own 4. Content SHALL be available in both English and Dutch.

#### Scenario: Gameplay chapter shows dealing sequence
- **WHEN** the user navigates to `/rules/gameplay`
- **THEN** the three dealing rounds (5, 4, 4 cards) are clearly shown

---

### Requirement: Landing page links to /rules
The "Learn the Rules" button on the landing page SHALL navigate to `/rules` using Angular's `routerLink`. The link SHALL work with the Angular router (no full page reload).

#### Scenario: Landing page button navigates to rules
- **WHEN** the user clicks the "Learn the Rules" button on the landing page
- **THEN** the Angular router navigates to `/rules/overview` without a page reload
