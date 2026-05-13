import { computed, effect, Injectable, signal } from '@angular/core';
import { PlayerJoinedEvent, PlayerStatusEvent, SseService } from '../../../services/sse.service';

export type PlayerPresenceStatus = 'Connected' | 'Disconnected' | 'Left';

export interface PlayerPresenceEntry {
  playerId: string;
  playerName: string;
  status: PlayerPresenceStatus;
}

/**
 * Local state store for player presence.
 * Consumes SseService events and maintains a Map of playerId → presence entry.
 * Provided at the component level so it is scoped to the game page.
 */
@Injectable()
export class PlayerPresenceStore {
  private readonly _presenceMap = signal<Map<string, PlayerPresenceEntry>>(new Map());

  readonly players = computed(() => Array.from(this._presenceMap().values()));

  constructor(private readonly sseService: SseService) {
    effect(() => {
      const event = this.sseService.lastPlayerStatus();
      if (!event) return;
      this.applyStatusEvent(event);
    });

    effect(() => {
      const event = this.sseService.lastPlayerJoined();
      if (!event) return;
      this.applyJoinedEvent(event);
    });
  }

  private applyStatusEvent(event: PlayerStatusEvent): void {
    this._presenceMap.update((map) => {
      const next = new Map(map);
      next.set(event.playerId, {
        playerId: event.playerId,
        playerName: event.playerName,
        status: event.status as PlayerPresenceStatus,
      });
      return next;
    });
  }

  private applyJoinedEvent(event: PlayerJoinedEvent): void {
    this._presenceMap.update((map) => {
      if (map.has(event.playerId)) return map;
      const next = new Map(map);
      next.set(event.playerId, {
        playerId: event.playerId,
        playerName: event.playerName,
        status: 'Connected',
      });
      return next;
    });
  }

  /** Seed the store with known players before SSE events arrive. */
  seedPlayers(players: Array<{ id: string; name: string }>): void {
    this._presenceMap.update((map) => {
      const next = new Map(map);
      for (const p of players) {
        if (!next.has(p.id)) {
          next.set(p.id, {
            playerId: p.id,
            playerName: p.name,
            status: 'Disconnected',
          });
        }
      }
      return next;
    });
  }
}
