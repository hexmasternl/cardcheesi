## Purpose

Define in-game chat behavior across the API, SSE broadcasting, and game UI.

## Requirements

### Requirement: Player can send a chat message
Any authenticated player who is a member of an active game SHALL be able to send a text chat message to that game by calling `POST /games/{code}/chat` with a non-empty text body (max 500 characters).

#### Scenario: Player sends a valid chat message
- **WHEN** a player POSTs `{ "text": "Good move!" }` to `/games/{code}/chat` while the game is InProgress
- **THEN** the server returns HTTP 200 and the message is broadcast to all SSE subscribers of that game as a `chat-message` event

#### Scenario: Player sends an empty message
- **WHEN** a player POSTs `{ "text": "" }` to `/games/{code}/chat`
- **THEN** the server returns HTTP 400 with a validation error

#### Scenario: Player sends a message exceeding the character limit
- **WHEN** a player POSTs a message with text longer than 500 characters
- **THEN** the server returns HTTP 400 with a validation error

#### Scenario: Non-member attempts to send a chat message
- **WHEN** a player who is NOT a member of game `{code}` POSTs to `/games/{code}/chat`
- **THEN** the server returns HTTP 403 Forbidden

#### Scenario: Game does not exist
- **WHEN** a player POSTs to `/games/{code}/chat` for a game code that does not exist
- **THEN** the server returns HTTP 404 Not Found

### Requirement: Chat messages are broadcast to all connected players
When a chat message is successfully submitted, the server SHALL emit a `chat-message` SSE event to all players currently connected to the game's SSE stream.

#### Scenario: Message received by all connected players
- **WHEN** player A sends a chat message in game `{code}`
- **THEN** all players currently subscribed to the SSE stream for game `{code}` receive a `chat-message` event with `{ senderId, senderName, text, timestamp }`

#### Scenario: Disconnected player does not receive the message
- **WHEN** player B is not connected to the SSE stream when a message is sent
- **THEN** player B does NOT receive that message (no history on reconnect)

### Requirement: Chat panel is displayed in the game UI
The game page SHALL display a `ChatPanelComponent` anchored to the left side of the game view. The panel SHALL slide in horizontally from the left and include a toggle button on its right edge.

#### Scenario: Chat panel is collapsed by default
- **WHEN** the game page loads
- **THEN** the chat panel is in its collapsed state (only the toggle button tab is visible)

#### Scenario: Player expands the chat panel
- **WHEN** the player clicks the toggle button
- **THEN** the chat panel slides in from the left, revealing the message list and input area

#### Scenario: Player collapses the chat panel
- **WHEN** the chat panel is expanded and the player clicks the toggle button
- **THEN** the chat panel slides back out to the left

### Requirement: Chat panel shows a message list
The chat panel SHALL display all chat messages received during the session in a scrollable list. Each entry SHALL show sender name, message text, and a relative or absolute timestamp.

#### Scenario: Messages appear in chronological order
- **WHEN** multiple chat messages are received
- **THEN** they appear in the order they were received, oldest at the top

#### Scenario: Own messages are visually distinguished
- **WHEN** the current player's own message appears in the list
- **THEN** it is styled differently (e.g., right-aligned or distinct background) to distinguish it from others' messages

#### Scenario: Panel auto-scrolls to the latest message
- **WHEN** a new message is received
- **THEN** if the panel is expanded, it scrolls to show the newest message

### Requirement: Player can type and send a message from the chat panel
The chat panel SHALL include a text input field and a send button at the bottom. Pressing Enter or clicking the send button SHALL submit the message.

#### Scenario: Send button submits the message
- **WHEN** the player types text and clicks the send button
- **THEN** the message is POSTed to the API and the input is cleared

#### Scenario: Enter key submits the message
- **WHEN** the player types text and presses Enter
- **THEN** the message is POSTed to the API and the input is cleared

#### Scenario: Empty input cannot be submitted
- **WHEN** the input is empty or whitespace only
- **THEN** the send button is disabled and pressing Enter does nothing

### Requirement: Unread badge on collapsed chat panel
When the chat panel is collapsed and a new message arrives, the toggle button SHALL display an unread count badge. The badge SHALL clear when the panel is expanded.

#### Scenario: Badge appears on new message while collapsed
- **WHEN** the chat panel is collapsed and a `chat-message` SSE event is received
- **THEN** the toggle button shows a numeric badge with the count of unread messages

#### Scenario: Badge clears on panel expand
- **WHEN** the player expands the chat panel
- **THEN** the unread badge disappears and the count resets to zero
