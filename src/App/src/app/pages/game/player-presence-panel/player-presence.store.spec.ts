import { TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { PlayerPresenceStore } from './player-presence.store';
import { PlayerJoinedEvent, PlayerStatusEvent, SseService } from '../../../services/sse.service';

// Minimal SseService stub using writable signals
function makeSseServiceStub() {
  const lastPlayerStatus = signal<PlayerStatusEvent | null>(null);
  const lastPlayerJoined = signal<PlayerJoinedEvent | null>(null);
  const connectionError = signal<boolean>(false);
  return { lastPlayerStatus, lastPlayerJoined, connectionError, connect: () => {}, disconnect: () => {} };
}

describe('PlayerPresenceStore', () => {
  let store: PlayerPresenceStore;
  let sseStub: ReturnType<typeof makeSseServiceStub>;

  beforeEach(() => {
    sseStub = makeSseServiceStub();

    TestBed.configureTestingModule({
      providers: [
        PlayerPresenceStore,
        { provide: SseService, useValue: sseStub },
      ],
    });

    store = TestBed.inject(PlayerPresenceStore);
  });

  it('player-joined event adds new player with Connected status', () => {
    TestBed.flushEffects();
    expect(store.players()).toHaveLength(0);

    sseStub.lastPlayerJoined.set({ playerId: 'p1', playerName: 'Alice' });
    TestBed.flushEffects();

    const players = store.players();
    expect(players).toHaveLength(1);
    expect(players[0]).toEqual({ playerId: 'p1', playerName: 'Alice', status: 'Connected', slotIndex: 0 });
  });

  it('player-joined event does not overwrite existing presence entry', () => {
    sseStub.lastPlayerStatus.set({ playerId: 'p1', playerName: 'Alice', status: 'Disconnected' });
    TestBed.flushEffects();

    sseStub.lastPlayerJoined.set({ playerId: 'p1', playerName: 'Alice' });
    TestBed.flushEffects();

    const players = store.players();
    expect(players).toHaveLength(1);
    // Status should remain Disconnected (set by player-status), not overwritten by player-joined
    expect(players[0].status).toBe('Disconnected');
  });

  it('player-status event updates presence entry', () => {
    sseStub.lastPlayerStatus.set({ playerId: 'p2', playerName: 'Bob', status: 'Connected' });
    TestBed.flushEffects();

    const players = store.players();
    expect(players).toHaveLength(1);
    expect(players[0].status).toBe('Connected');
  });

  it('seedPlayers adds players with Disconnected status', () => {
    store.seedPlayers([{ id: 'p3', name: 'Carol' }]);
    const players = store.players();
    expect(players).toHaveLength(1);
    expect(players[0].status).toBe('Disconnected');
  });

  it('seedPlayers does not overwrite existing entries', () => {
    sseStub.lastPlayerStatus.set({ playerId: 'p3', playerName: 'Carol', status: 'Connected' });
    TestBed.flushEffects();

    store.seedPlayers([{ id: 'p3', name: 'Carol' }]);

    const players = store.players();
    expect(players[0].status).toBe('Connected');
  });
});
