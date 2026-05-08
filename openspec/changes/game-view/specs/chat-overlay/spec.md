# Spec: Chat Overlay

## Overview

A collapsible chat panel anchored to the right edge of the game board page allows players in the same game to exchange messages during play. The overlay is always present but occupies minimal space when collapsed.

---

## ADDED Requirements

### Requirement: Chat overlay is always accessible
The `GamePage` SHALL render a chat toggle button anchored to the right side of the viewport at all times during an active game.

#### Scenario: Chat is collapsed by default
- **WHEN** the game page first loads
- **THEN** the chat panel is collapsed, showing only the toggle icon button

#### Scenario: Player expands chat
- **WHEN** the player clicks the toggle button
- **THEN** the chat panel slides open (≥300px wide) from the right edge
- **THEN** the message history is visible and scrolled to the bottom

#### Scenario: Player collapses chat
- **WHEN** the chat panel is open and the player clicks the toggle button again
- **THEN** the panel slides closed, reverting to the icon-only state

---

### Requirement: Chat messages are displayed
When the chat panel is open, it SHALL display all messages for the current game in chronological order.

#### Scenario: Messages are listed
- **WHEN** the panel is expanded and messages exist
- **THEN** each message shows the sender's player name, the message text, and a relative or formatted timestamp
- **THEN** messages are ordered oldest-to-newest, scrolled to the latest

#### Scenario: No messages yet
- **WHEN** the panel is expanded and no messages exist
- **THEN** a placeholder text "No messages yet — say hi! 👋" is displayed

---

### Requirement: Player can send a message
The chat panel SHALL include a text input and send button at the bottom of the panel.

#### Scenario: Player sends a message
- **GIVEN** the player has typed a non-empty message
- **WHEN** the player presses Enter or clicks the send button
- **THEN** a `POST /api/games/{code}/chat` request is made with the message body and the local player's ID
- **THEN** the input field is cleared
- **THEN** the new message appears in the message list

#### Scenario: Empty message is not sent
- **WHEN** the player submits an empty or whitespace-only message
- **THEN** no request is made and no message is added

---

### Requirement: Chat is refreshed with game state
Chat messages SHALL be re-fetched whenever the game state is refreshed (manual refresh or polling).

#### Scenario: Refresh also updates chat
- **WHEN** the player triggers a game state refresh
- **THEN** `GET /api/games/{code}/chat` is called
- **THEN** the chat message list is updated with any new messages

---

### Requirement: Unread badge
When the chat panel is collapsed and a new message arrives (detected on refresh), a badge counter SHALL appear on the toggle button.

#### Scenario: Badge increments on new message while collapsed
- **GIVEN** the chat panel is collapsed
- **WHEN** a refresh returns a message not previously seen
- **THEN** a numeric badge is shown on the toggle button

#### Scenario: Badge clears on open
- **WHEN** the player opens the chat panel
- **THEN** the badge is removed

---

## Backend API

| Method | Path | Purpose |
|--------|------|---------|
| `GET` | `/api/games/{code}/chat` | Returns `ChatMessage[]` ordered by timestamp ascending |
| `POST` | `/api/games/{code}/chat` | Adds a new message; body: `{ playerId, text }` |

### `ChatMessage` shape
```json
{
  "id": "uuid",
  "gameCode": "ABC123",
  "playerId": "uuid",
  "playerName": "Alice",
  "text": "Good luck!",
  "sentAt": "2026-05-08T15:00:00Z"
}
```

---

## UX Notes

- The panel overlaps the game board (it does not reflow/push board content).
- On mobile, the panel opens full-screen width to maximise readability.
- The toggle button uses the `pi pi-comments` PrimeNG icon and follows the CardCheesi glass-morphic style.
- The player name in messages is colour-coded: the local player's messages appear right-aligned with the primary colour (`#009ccc`); others left-aligned.
