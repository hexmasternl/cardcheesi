import { Component } from '@angular/core';
import { ButtonModule } from 'primeng/button';

@Component({
  selector: 'app-landing',
  imports: [ButtonModule],
  templateUrl: './landing.html',
  styleUrl: './landing.scss',
})
export class LandingPage {
  protected readonly bgSuits = [
    { suit: '♠', cls: 'p' }, { suit: '♥', cls: 's' },
    { suit: '♦', cls: 'p' }, { suit: '♣', cls: 's' },
    { suit: '♠', cls: 's' }, { suit: '♥', cls: 'p' },
    { suit: '♦', cls: 's' }, { suit: '♣', cls: 'p' },
    { suit: '♠', cls: 'p' }, { suit: '♥', cls: 's' },
    { suit: '♦', cls: 'p' }, { suit: '♣', cls: 's' },
  ];

  protected readonly features = [
    {
      suit: '♟',
      title: '4 Players, 2 Teams',
      description:
        'Partner up and coordinate your moves. Team A plays P1 & P3, Team B plays P2 & P4. Strategy runs deep.',
    },
    {
      suit: '🃏',
      title: 'Cards Decide Everything',
      description:
        'Aces & Kings enter pawns. Sevens split across two pawns. Jacks let you swap. Every card opens new possibilities.',
    },
    {
      suit: '🏆',
      title: 'Win as a Team',
      description:
        'Race all 8 of your team\'s pawns into the finish area before the other team does. Two win — or none do.',
    },
  ];

  protected readonly steps = [
    {
      num: '01',
      title: 'Deal the Cards',
      desc: 'Cards are dealt in three rounds: 5, 4, then 4. The dealer rotates after each full set of rounds.',
    },
    {
      num: '02',
      title: 'Enter the Board',
      desc: 'Play an Ace or King to bring a pawn from reserve onto your home position.',
    },
    {
      num: '03',
      title: 'Race Forward',
      desc: 'Move your pawns around the 64-space board using your cards. Special cards unlock special moves.',
    },
    {
      num: '04',
      title: 'Finish Together',
      desc: 'Guide all 4 of your pawns — and help your teammate do the same. First team done wins!',
    },
  ];
}
