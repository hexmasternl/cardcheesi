import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { TranslateModule } from '@ngx-translate/core';
import { AuthService } from '../../services/auth.service';
import { GameService } from '../../services/game.service';

interface RegisterResponse {
  token: string;
}

@Component({
  selector: 'app-landing',
  imports: [RouterLink, FormsModule, ButtonModule, DialogModule, InputTextModule, TranslateModule],
  templateUrl: './landing.html',
  styleUrl: './landing.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LandingPage {
  private readonly authService = inject(AuthService);
  private readonly gameService = inject(GameService);
  private readonly router = inject(Router);
  private readonly http = inject(HttpClient);

  protected readonly dialogVisible = signal(false);
  protected readonly playerName = signal('');
  protected readonly errorMessage = signal('');
  protected readonly isLoading = signal(false);

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

  protected async onPlayNow(): Promise<void> {
    if (this.authService.isAuthenticated()) {
      await this.createGameAndNavigate();
    } else {
      this.playerName.set('');
      this.errorMessage.set('');
      this.dialogVisible.set(true);
    }
  }

  protected async onRegisterAndPlay(): Promise<void> {
    const name = this.playerName().trim();
    if (!name) {
      this.errorMessage.set('Please enter a display name.');
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set('');

    try {
      const reg = await new Promise<RegisterResponse>((resolve, reject) => {
        this.http.post<RegisterResponse>('/api/players', { name }).subscribe({ next: resolve, error: reject });
      });
      this.authService.storeToken(reg.token);
      this.dialogVisible.set(false);
      await this.createGameAndNavigate();
    } catch (err) {
      const error = err as HttpErrorResponse;
      this.errorMessage.set(error.status === 400
        ? 'That name is not valid. Please try another.'
        : 'Something went wrong. Please try again.');
    } finally {
      this.isLoading.set(false);
    }
  }

  private async createGameAndNavigate(): Promise<void> {
    this.isLoading.set(true);
    try {
      const game = await this.gameService.createGame();
      await this.router.navigate(['/game', game.gameCode]);
    } catch {
      this.errorMessage.set('Could not create a game. Please try again.');
      this.dialogVisible.set(true);
    } finally {
      this.isLoading.set(false);
    }
  }
}
