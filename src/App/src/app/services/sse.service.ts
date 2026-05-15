import { Injectable, signal } from '@angular/core';

export interface PlayerStatusEvent {
  playerId: string;
  playerName: string;
  status: 'Connected' | 'Disconnected' | 'Left';
}

export interface PlayerJoinedEvent {
  playerId: string;
  playerName: string;
}

export interface YourTurnEvent {
  canDispose: boolean;
}

export interface ChatMessageEvent {
  senderId: string;
  senderName: string;
  text: string;
  timestamp: string;
}

export type SseEventType = 'player-status' | 'player-joined' | 'your-turn' | 'chat-message';

@Injectable({ providedIn: 'root' })
export class SseService {
  private eventSource: EventSource | null = null;

  readonly lastPlayerStatus = signal<PlayerStatusEvent | null>(null);
  readonly lastPlayerJoined = signal<PlayerJoinedEvent | null>(null);
  readonly lastYourTurn = signal<YourTurnEvent | null>(null);
  readonly chatMessages = signal<ChatMessageEvent[]>([]);
  readonly connectionError = signal<boolean>(false);

  connect(gameCode: string, playerId: string): void {
    this.disconnect();

    const url = `/api/games/${gameCode}/events?playerId=${encodeURIComponent(playerId)}`;
    this.eventSource = new EventSource(url);

    this.eventSource.onopen = () => {
      this.connectionError.set(false);
    };

    this.eventSource.addEventListener('player-status', (event: MessageEvent) => {
      try {
        const data = JSON.parse(event.data) as PlayerStatusEvent;
        this.lastPlayerStatus.set(data);
        this.connectionError.set(false);
      } catch {
        // malformed payload — ignore
      }
    });

    this.eventSource.addEventListener('player-joined', (event: MessageEvent) => {
      try {
        const data = JSON.parse(event.data) as PlayerJoinedEvent;
        this.lastPlayerJoined.set(data);
        this.connectionError.set(false);
      } catch {
        // malformed payload — ignore
      }
    });

    this.eventSource.addEventListener('your-turn', (event: MessageEvent) => {
      try {
        const data = JSON.parse(event.data) as YourTurnEvent;
        this.lastYourTurn.set(data);
        this.connectionError.set(false);
      } catch {
        // malformed payload — ignore
      }
    });

    this.eventSource.addEventListener('chat-message', (event: MessageEvent) => {
      try {
        const data = JSON.parse(event.data) as ChatMessageEvent;
        this.chatMessages.update((msgs) => [...msgs, data]);
        this.connectionError.set(false);
      } catch {
        // malformed payload — ignore
      }
    });

    this.eventSource.onerror = () => {
      this.connectionError.set(true);
    };
  }

  disconnect(): void {
    if (this.eventSource) {
      this.eventSource.close();
      this.eventSource = null;
    }
    this.lastPlayerJoined.set(null);
    this.lastYourTurn.set(null);
    this.chatMessages.set([]);
  }
}
