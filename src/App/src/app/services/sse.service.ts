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

export type SseEventType = 'player-status' | 'player-joined';

@Injectable({ providedIn: 'root' })
export class SseService {
  private eventSource: EventSource | null = null;

  readonly lastPlayerStatus = signal<PlayerStatusEvent | null>(null);
  readonly lastPlayerJoined = signal<PlayerJoinedEvent | null>(null);
  readonly connectionError = signal<boolean>(false);

  connect(gameCode: string, playerId: string): void {
    this.disconnect();

    const url = `/api/games/${gameCode}/events?playerId=${encodeURIComponent(playerId)}`;
    this.eventSource = new EventSource(url);

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
  }
}
