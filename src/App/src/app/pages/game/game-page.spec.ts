import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap } from '@angular/router';
import { signal, WritableSignal } from '@angular/core';
import { NEVER, of } from 'rxjs';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { GamePage } from './game-page';
import { GameService } from './game.service';
import { GameState } from './game-state.model';
import { AuthService } from '../../services/auth.service';
import { SseService, PlayerJoinedEvent } from '../../services/sse.service';
import { TurnFlowStore } from './turn-flow.store';

function makeGameState(players: GameState['players'] = []): GameState {
  return {
    id: 'game-1',
    gameCode: 'GAME01',
    status: 0,
    teams: [],
    players,
    turn: null,
    deck: null,
    hands: null,
  };
}

function makeSseStub() {
  return {
    lastPlayerStatus: signal(null),
    lastPlayerJoined: signal<PlayerJoinedEvent | null>(null),
    lastYourTurn: signal(null),
    lastGameUpdated: signal(0),
    chatMessages: signal([]),
    connectionError: signal(false),
    connect: () => {},
    disconnect: () => {},
  };
}

type GamePageInternal = {
  gameState: WritableSignal<GameState | null>;
};

describe('GamePage — player-joined effect', () => {
  let sseStub: ReturnType<typeof makeSseStub>;

  beforeEach(async () => {
    sseStub = makeSseStub();

    await TestBed.configureTestingModule({
      imports: [GamePage],
      providers: [
        {
          provide: ActivatedRoute,
          useValue: {
            paramMap: of(convertToParamMap({ gameCode: 'GAME01' })),
          },
        },
        {
          provide: GameService,
          useValue: { getByCode: () => NEVER, makeMove: () => NEVER, disposeHand: () => NEVER },
        },
        {
          provide: AuthService,
          useValue: { getPlayerId: () => 'player-1', accessToken: signal(null) },
        },
        { provide: SseService, useValue: sseStub },
        {
          provide: TurnFlowStore,
          useValue: {
            gameState: signal<GameState | null>(null),
            myPlayerId: signal<string | null>(null),
            selectPawn: () => {},
          },
        },
        provideHttpClientTesting(),
      ],
    })
      .overrideComponent(GamePage, { set: { imports: [], template: '' } })
      .compileComponents();
  });

  it('appends a new player when a player-joined event is received', () => {
    const fixture = TestBed.createComponent(GamePage);
    const component = fixture.componentInstance as unknown as GamePageInternal;

    component.gameState.set(makeGameState([{ id: 'player-1', name: 'Alice', pawns: [] }]));
    TestBed.flushEffects();

    sseStub.lastPlayerJoined.set({ playerId: 'player-2', playerName: 'Bob' });
    TestBed.flushEffects();

    const players = component.gameState()?.players;
    expect(players).toHaveLength(2);
    expect(players?.[1]).toEqual({ id: 'player-2', name: 'Bob', pawns: [] });
  });

  it('deduplicates: does not append a player already present in gameState', () => {
    const fixture = TestBed.createComponent(GamePage);
    const component = fixture.componentInstance as unknown as GamePageInternal;

    component.gameState.set(makeGameState([{ id: 'player-1', name: 'Alice', pawns: [] }]));
    TestBed.flushEffects();

    // player-1 already exists — should be ignored
    sseStub.lastPlayerJoined.set({ playerId: 'player-1', playerName: 'Alice' });
    TestBed.flushEffects();

    expect(component.gameState()?.players).toHaveLength(1);
  });

  it('is a no-op when gameState is null', () => {
    const fixture = TestBed.createComponent(GamePage);
    const component = fixture.componentInstance as unknown as GamePageInternal;

    // gameState remains null
    TestBed.flushEffects();

    expect(() => {
      sseStub.lastPlayerJoined.set({ playerId: 'player-2', playerName: 'Bob' });
      TestBed.flushEffects();
    }).not.toThrow();

    expect(component.gameState()).toBeNull();
  });
});
