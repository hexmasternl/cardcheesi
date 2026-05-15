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
import { HttpErrorResponse } from '@angular/common/http';
import { toSignal } from '@angular/core/rxjs-interop';
import { map } from 'rxjs';
import { ButtonModule } from 'primeng/button';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { TagModule } from 'primeng/tag';
import { CardModule } from 'primeng/card';
import { GameService } from './game.service';
import { GameState, GameStatusLabel } from './game-state.model';
import { GameBoardComponent } from './game-board/game-board';
import { PlayerPresencePanelComponent } from './player-presence-panel/player-presence-panel';
import { AuthService } from '../../services/auth.service';
import { SseService } from '../../services/sse.service';


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
  private readonly destroyRef = inject(DestroyRef);

  protected readonly gameCode = toSignal(
    this.route.paramMap.pipe(map((p) => p.get('gameCode') ?? '')),
    { initialValue: '' },
  );

  protected readonly loading = signal(true);
  protected readonly gameState = signal<GameState | null>(null);
  protected readonly error = signal<{ is404: boolean; message: string } | null>(null);

  protected readonly statusLabel = computed(() => {
    const s = this.gameState();
    return s ? (GameStatusLabel[s.status] ?? 'Unknown') : '';
  });

  constructor() {
    // Connect SSE whenever gameCode and playerId are both available.
    // Re-runs automatically if the access token is refreshed.
    effect(() => {
      const code = this.gameCode();
      const playerId = this.authService.getPlayerId();
      if (!code || !playerId) return;
      this.sseService.connect(code, playerId);
    });

    this.destroyRef.onDestroy(() => this.sseService.disconnect());

    // Notify the server when the player leaves the page (tab close, navigation, browser close).
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

    // Reconnect SSE when the tab becomes visible again after an error.
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
  }

  ngOnInit(): void {
    this.fetchGame();
  }

  protected refresh(): void {
    this.fetchGame();
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