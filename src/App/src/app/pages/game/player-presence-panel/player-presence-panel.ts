import {
  ChangeDetectionStrategy,
  Component,
  inject,
  input,
  OnInit,
} from '@angular/core';
import { TagModule } from 'primeng/tag';
import { PlayerPresenceStatus, PlayerPresenceStore } from './player-presence.store';
import { GamePlayer } from '../game-state.model';

@Component({
  selector: 'app-player-presence-panel',
  imports: [TagModule],
  providers: [PlayerPresenceStore],
  templateUrl: './player-presence-panel.html',
  styleUrl: './player-presence-panel.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PlayerPresencePanelComponent implements OnInit {
  readonly players = input<GamePlayer[]>([]);

  protected readonly store = inject(PlayerPresenceStore);

  ngOnInit(): void {
    this.store.seedPlayers(this.players());
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
