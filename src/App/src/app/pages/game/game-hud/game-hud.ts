import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  input,
  model,
  output,
  signal,
} from '@angular/core';
import { Card, MakeMoveRequest } from '../game-state.model';
import { PlayingCardComponent } from './playing-card/playing-card';
import { TurnFlowStore } from '../turn-flow.store';

@Component({
  selector: 'app-game-hud',
  standalone: true,
  imports: [PlayingCardComponent],
  templateUrl: './game-hud.html',
  styleUrl: './game-hud.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GameHudComponent {
  protected readonly turnFlow = inject(TurnFlowStore);

  readonly cards = input<Card[]>([]);
  readonly canDispose = input(false);
  readonly expanded = model(false);

  readonly playMove = output<MakeMoveRequest>();
  readonly disposeHand = output<void>();

  protected readonly selectedIndex = signal<number | null>(null);

  protected readonly sevenStepOptions = [1, 2, 3, 4, 5, 6, 7] as const;

  constructor() {
    // Reset selection when the hand changes
    effect(() => {
      const cards = this.cards();
      const idx = this.selectedIndex();
      if (idx !== null && idx >= cards.length) {
        this.selectedIndex.set(null);
      }
    });

    // Keep selected index in sync with store's selected card
    effect(() => {
      const selected = this.turnFlow.selectedCard();
      if (!selected) {
        this.selectedIndex.set(null);
        return;
      }
      const idx = this.cards().findIndex(
        (c) => c.suit === selected.suit && c.rank === selected.rank,
      );
      if (idx !== this.selectedIndex()) {
        this.selectedIndex.set(idx >= 0 ? idx : null);
      }
    });
  }

  protected toggle(): void {
    this.expanded.update((v) => !v);
  }

  protected selectCard(index: number): void {
    const card = this.cards()[index];
    if (!card) return;
    if (this.selectedIndex() === index) {
      this.selectedIndex.set(null);
      this.turnFlow.reset();
    } else {
      this.selectedIndex.set(index);
      this.turnFlow.selectCard(card);
    }
  }

  protected onPlay(): void {
    const payload = this.turnFlow.movePayload();
    if (!payload) return;
    this.playMove.emit(payload);
    this.selectedIndex.set(null);
    this.turnFlow.reset();
  }

  protected onDispose(): void {
    this.disposeHand.emit();
    this.selectedIndex.set(null);
    this.turnFlow.reset();
  }

  protected onSelectSevenSteps(steps: number): void {
    this.turnFlow.selectSevenSteps(steps);
  }

  protected onSelectAceChoice(choice: 'enter' | 'advance'): void {
    this.turnFlow.selectAceChoice(choice);
  }

  protected cardStyle(index: number): Record<string, string> {
    const total = this.cards().length;
    const center = (total - 1) / 2;
    const offset = (index - center) * 32;
    const angle = (index - center) * 5;
    return {
      '--card-offset': `${offset}px`,
      '--card-angle': `${angle}deg`,
    };
  }
}
