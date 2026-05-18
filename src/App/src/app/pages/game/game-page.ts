import {
  ChangeDetectionStrategy,
  Component,
  computed,
  DestroyRef,
  effect,
  inject,
  OnInit,
  signal,
} from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { toSignal } from '@angular/core/rxjs-interop';
import { map } from 'rxjs';
import { ButtonModule } from 'primeng/button';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { TagModule } from 'primeng/tag';
import { CardModule } from 'primeng/card';
import { GameService } from './game.service';
import { Card, GameState, GameStatusLabel, MakeMoveRequest } from './game-state.model';
import { GameBoardComponent } from './game-board/game-board';
import { PlayerPresencePanelComponent } from './player-presence-panel/player-presence-panel';
import { GameHudComponent } from './game-hud/game-hud';
import { ChatPanelComponent } from './chat-panel/chat-panel';
import { AuthService } from '../../services/auth.service';
import { SseService } from '../../services/sse.service';
import { TurnFlowStore } from './turn-flow.store';


@Component({
  selector: 'app-game-page',
  imports: [
    RouterLink,
    ButtonModule,
    ProgressSpinnerModule,
    TagModule,
    CardModule,
    GameBoardComponent,
    PlayerPresencePanelComponent,
    GameHudComponent,
    ChatPanelComponent,
  ],
  templateUrl: './game-page.html',
  styleUrl: './game-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GamePage implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly gameService = inject(GameService);
  private readonly authService = inject(AuthService);
  private readonly sseService = inject(SseService);
  protected readonly turnFlow = inject(TurnFlowStore);
  private readonly destroyRef = inject(DestroyRef);
  private readonly http = inject(HttpClient);

  protected readonly gameCode = toSignal(
    this.route.paramMap.pipe(map((p) => p.get('gameCode') ?? '')),
    { initialValue: '' },
  );

  protected readonly loading = signal(true);
  protected readonly gameState = signal<GameState | null>(null);
  protected readonly error = signal<{ is404: boolean; message: string } | null>(null);
  protected readonly hudExpanded = signal(false);
  protected readonly hudCanDispose = signal(false);
  protected readonly chatPanelExpanded = signal(false);
  protected readonly chatMessages = computed(() => this.sseService.chatMessages());

  protected readonly statusLabel = computed(() => {
    const s = this.gameState();
    return s ? (GameStatusLabel[s.status] ?? 'Unknown') : '';
  });

  protected readonly myHand = computed<Card[]>(() => {
    const state = this.gameState();
    const playerId = this.authService.getPlayerId();
    if (!state?.hands || !playerId) return [];
    return state.hands.find((h) => h.playerId === playerId)?.cards ?? [];
  });

  constructor() {
    // Connect SSE whenever gameCode and playerId are both available.
    effect(() => {
      const code = this.gameCode();
      const playerId = this.authService.getPlayerId();
      if (!code || !playerId) return;
      this.sseService.connect(code, playerId);
    });

    this.destroyRef.onDestroy(() => this.sseService.disconnect());

    const handleBeforeUnload = () => {
      const code = this.gameCode();
      const token = this.authService.accessToken();
      if (!code || !token) return;
      void fetch(`/api/games/${code}/leave`, {
        method: 'POST',
        keepalive: true,
        headers: { Authorization: `Bearer ${token}` },
      });
    };

    const handleVisibilityChange = () => {
      if (!document.hidden && this.sseService.connectionError()) {
        const code = this.gameCode();
        const playerId = this.authService.getPlayerId();
        if (code && playerId) {
          this.sseService.connect(code, playerId);
        }
      }
    };

    window.addEventListener('beforeunload', handleBeforeUnload);
    document.addEventListener('visibilitychange', handleVisibilityChange);

    this.destroyRef.onDestroy(() => {
      window.removeEventListener('beforeunload', handleBeforeUnload);
      document.removeEventListener('visibilitychange', handleVisibilityChange);
    });

    // Sync game state into TurnFlowStore
    effect(() => {
      this.turnFlow.gameState.set(this.gameState());
    });

    effect(() => {
      this.turnFlow.myPlayerId.set(this.authService.getPlayerId() ?? '');
    });

    // Refresh game state when the server broadcasts a game-updated event
    effect(() => {
      const updated = this.sseService.lastGameUpdated();
      if (updated > 0) this.fetchGame();
    });

    effect(() => {
      const joined = this.sseService.lastPlayerJoined();
      if (!joined) return;
      const state = this.gameState();
      if (!state) return;

      const alreadyPresent = state.players.some((p) => p.id === joined.playerId);
      if (!alreadyPresent) {
        this.gameState.set({
          ...state,
          players: [...state.players, { id: joined.playerId, name: joined.playerName, pawns: [] }],
        });
      }
    });

    effect(() => {
      const ev = this.sseService.lastYourTurn();
      if (!ev) return;
      // Only respond if this event is addressed to us
      if (ev.activePlayerId !== this.authService.getPlayerId()) return;
      this.hudCanDispose.set(ev.canDispose);
      this.hudExpanded.set(true);
    });
  }

  ngOnInit(): void {
    this.fetchGame();
  }

  protected refresh(): void {
    this.fetchGame();
  }

  protected onSendMessage(text: string): void {
    const code = this.gameCode();
    if (!code) return;
    this.http.post(`/api/chat/${code}`, { text }).subscribe();
  }

  protected onPawnClicked(pawnId: string): void {
    this.turnFlow.selectPawn(pawnId);
  }

  protected onPlayMove(request: MakeMoveRequest): void {
    const code = this.gameCode();
    if (!code) return;
    this.gameService.makeMove(code, request).subscribe({
      error: (err: HttpErrorResponse) => {
        console.error('Failed to make move', err);
      },
    });
  }

  protected onDisposeHand(): void {
    const code = this.gameCode();
    if (!code) return;
    this.gameService.disposeHand(code).subscribe({
      error: (err: HttpErrorResponse) => {
        console.error('Failed to dispose hand', err);
      },
    });
  }

  private fetchGame(): void {
    const code = this.gameCode();
    if (!code) return;

    this.loading.set(true);
    this.error.set(null);

    this.gameService.getByCode(code).subscribe({
      next: (state) => {
        this.gameState.set(state);
        this.loading.set(false);
      },
      error: (err: HttpErrorResponse) => {
        this.loading.set(false);
        this.error.set({
          is404: err.status === 404,
          message:
            err.status === 404
              ? `No game found with code "${code}".`
              : err.status === 403
                ? 'You are not a player in this game.'
                : 'Could not load the game. Please try again.',
        });
      },
    });
  }
}