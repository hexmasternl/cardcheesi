import {
  ChangeDetectionStrategy,
  Component,
  effect,
  inject,
  input,
} from '@angular/core';
import { TagModule } from 'primeng/tag';
import { PAWN_COLORS, PlayerPresenceStatus, PlayerPresenceStore } from './player-presence.store';
import { GamePlayer } from '../game-state.model';

@Component({
  selector: 'app-player-presence-panel',
  imports: [TagModule],
  providers: [PlayerPresenceStore],
  templateUrl: './player-presence-panel.html',
  styleUrl: './player-presence-panel.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PlayerPresencePanelComponent {
  readonly players = input<GamePlayer[]>([]);

  protected readonly store = inject(PlayerPresenceStore);

  constructor() {
    effect(() => this.store.seedPlayers(this.players()));
  }

  protected pawnColor(slotIndex: number): string {
    return PAWN_COLORS[slotIndex] ?? '#888';
  }

  protected initials(name: string): string {
    return name
      .split(' ')
      .filter(Boolean)
      .slice(0, 2)
      .map(w => w[0].toUpperCase())
      .join('');
  }

  protected statusSeverity(status: PlayerPresenceStatus): 'success' | 'warn' | 'danger' {
    switch (status) {
      case 'Connected':    return 'success';
      case 'Disconnected': return 'warn';
      case 'Left':         return 'danger';
    }
  }

  protected statusLabel(status: PlayerPresenceStatus): string {
    switch (status) {
      case 'Connected':    return 'Online';
      case 'Disconnected': return 'Away';
      case 'Left':         return 'Left';
    }
  }
}
