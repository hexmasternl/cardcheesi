import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { Card } from '../../game-state.model';

const SUIT_SYMBOLS = ['♣', '♦', '♥', '♠'];
const RANK_LABELS = ['', 'A', '2', '3', '4', '5', '6', '7', '8', '9', '10', 'J', 'Q', 'K'];

@Component({
  selector: 'app-playing-card',
  standalone: true,
  host: { '[class.selected]': 'selected()' },
  template: `
    <div class="card" [class.card--red]="isRed()">
      <div class="card__corner card__corner--tl">
        <span class="card__rank">{{ rankLabel() }}</span>
        <span class="card__suit-small">{{ suitSymbol() }}</span>
      </div>
      <span class="card__center">{{ suitSymbol() }}</span>
      <div class="card__corner card__corner--br">
        <span class="card__rank">{{ rankLabel() }}</span>
        <span class="card__suit-small">{{ suitSymbol() }}</span>
      </div>
    </div>
  `,
  styleUrl: './playing-card.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PlayingCardComponent {
  readonly card = input.required<Card>();
  readonly selected = input(false);

  protected rankLabel = () => RANK_LABELS[this.card().rank] ?? '?';
  protected suitSymbol = () => SUIT_SYMBOLS[this.card().suit] ?? '?';
  protected isRed = () => this.card().suit === 1 || this.card().suit === 2;
}
