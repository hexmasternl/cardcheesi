import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { TranslateModule } from '@ngx-translate/core';

@Component({
  selector: 'app-landing',
  imports: [RouterLink, ButtonModule, TranslateModule],
  templateUrl: './landing.html',
  styleUrl: './landing.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
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
    { suit: '♟', key: 'teams' },
    { suit: '🃏', key: 'cards' },
    { suit: '🏆', key: 'win' },
  ];

  protected readonly steps = [
    { num: '01', key: 'deal' },
    { num: '02', key: 'enter' },
    { num: '03', key: 'race' },
    { num: '04', key: 'finish' },
  ];

  protected readonly cardMechanics = [
    { icon: '🂡', key: 'ace',      pillClass: 'mechanic-pill--primary' },
    { icon: '🂮', key: 'king',     pillClass: 'mechanic-pill--primary' },
    { icon: '🂭', key: 'queen',    pillClass: 'mechanic-pill--secondary' },
    { icon: '🂫', key: 'jack',     pillClass: 'mechanic-pill--secondary' },
    { icon: '🂧', key: 'seven',    pillClass: 'mechanic-pill--accent' },
    { icon: '🂤', key: 'four',     pillClass: 'mechanic-pill--accent' },
    { icon: '🃟', key: 'twoToTen', pillClass: 'mechanic-pill--neutral' },
  ];
}
