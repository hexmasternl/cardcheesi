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
  private gameEventSource: EventSource | null = null;
  private chatEventSource: EventSource | null = null;

  readonly lastPlayerStatus = signal<PlayerStatusEvent | null>(null);
  readonly lastPlayerJoined = signal<PlayerJoinedEvent | null>(null);
  readonly lastYourTurn = signal<YourTurnEvent | null>(null);
  readonly chatMessages = signal<ChatMessageEvent[]>([]);
  readonly connectionError = signal<boolean>(false);

  connect(gameCode: string, playerId: string): void {
    this.disconnect();

    const pid = encodeURIComponent(playerId);

    this.gameEventSource = new EventSource(`/api/games/${gameCode}/events?playerId=${pid}`);

    this.gameEventSource.onopen = () => {
      this.connectionError.set(false);
    };

    this.gameEventSource.addEventListener('player-status', (event: MessageEvent) => {
      try {
        const data = JSON.parse(event.data) as PlayerStatusEvent;
        this.lastPlayerStatus.set(data);
        this.connectionError.set(false);
      } catch {
        // malformed payload — ignore
      }
    });

    this.gameEventSource.addEventListener('player-joined', (event: MessageEvent) => {
      try {
        const data = JSON.parse(event.data) as PlayerJoinedEvent;
        this.lastPlayerJoined.set(data);
        this.connectionError.set(false);
      } catch {
        // malformed payload — ignore
      }
    });

    this.gameEventSource.addEventListener('your-turn', (event: MessageEvent) => {
      try {
        const data = JSON.parse(event.data) as YourTurnEvent;
        this.lastYourTurn.set(data);
        this.connectionError.set(false);
      } catch {
        // malformed payload — ignore
      }
    });

    this.gameEventSource.onerror = () => {
      this.connectionError.set(true);
    };

    this.chatEventSource = new EventSource(`/api/chat/${gameCode}/events?playerId=${pid}`);

    this.chatEventSource.addEventListener('chat-message', (event: MessageEvent) => {
      try {
        const data = JSON.parse(event.data) as ChatMessageEvent;
        this.chatMessages.update((msgs) => [...msgs, data]);
        this.connectionError.set(false);
      } catch {
        // malformed payload — ignore
      }
    });

    this.chatEventSource.onerror = () => {
      this.connectionError.set(true);
    };
  }

  disconnect(): void {
    if (this.gameEventSource) {
      this.gameEventSource.close();
      this.gameEventSource = null;
    }
    if (this.chatEventSource) {
      this.chatEventSource.close();
      this.chatEventSource = null;
    }
    this.lastPlayerJoined.set(null);
    this.lastYourTurn.set(null);
    this.chatMessages.set([]);
  }
}
