import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  input,
  model,
  output,
  signal,
} from '@angular/core';
import { Card } from '../game-state.model';
import { PlayingCardComponent } from './playing-card/playing-card';

@Component({
  selector: 'app-game-hud',
  standalone: true,
  imports: [PlayingCardComponent],
  templateUrl: './game-hud.html',
  styleUrl: './game-hud.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GameHudComponent {
  readonly cards = input<Card[]>([]);
  readonly canDispose = input(false);
  readonly expanded = model(false);

  readonly playCard = output<Card>();
  readonly disposeCard = output<Card>();

  protected readonly selectedIndex = signal<number | null>(null);
  protected readonly canPlay = computed(() => this.selectedIndex() !== null);

  constructor() {
    // Reset selection when the hand changes (e.g. card is played or new hand dealt)
    effect(() => {
      const cards = this.cards();
      const idx = this.selectedIndex();
      if (idx !== null && idx >= cards.length) {
        this.selectedIndex.set(null);
      }
    });
  }

  protected toggle(): void {
    this.expanded.update((v) => !v);
  }

  protected selectCard(index: number): void {
    this.selectedIndex.update((prev) => (prev === index ? null : index));
  }

  protected onPlay(): void {
    const idx = this.selectedIndex();
    if (idx === null) return;
    const card = this.cards()[idx];
    if (!card) return;
    this.playCard.emit(card);
    this.selectedIndex.set(null);
  }

  protected onDispose(): void {
    const idx = this.selectedIndex();
    if (idx === null) return;
    const card = this.cards()[idx];
    if (!card) return;
    this.disposeCard.emit(card);
    this.selectedIndex.set(null);
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
