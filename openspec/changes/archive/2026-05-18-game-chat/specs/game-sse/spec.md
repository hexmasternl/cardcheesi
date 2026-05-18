## ADDED Requirements

### Requirement: SSE stream emits chat-message events
The SSE event stream for a game SHALL emit a `chat-message` event whenever a player in that game submits a new chat message. The event payload SHALL be a JSON object containing `senderId`, `senderName`, `text`, and `timestamp` (ISO 8601 string).

#### Scenario: chat-message event is emitted on new message
- **WHEN** a player successfully posts a chat message to `/games/{code}/chat`
- **THEN** all SSE subscribers for game `{code}` receive an event with `event: chat-message` and a JSON data payload `{ senderId, senderName, text, timestamp }`

#### Scenario: chat-message event is not emitted for invalid messages
- **WHEN** a player posts an invalid message (empty text or text exceeding 500 characters)
- **THEN** no `chat-message` SSE event is emitted
