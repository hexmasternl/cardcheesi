import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { AuthService } from '../../services/auth.service';

interface RegisterResponse {
  token: string;
}

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [FormsModule, ButtonModule, InputTextModule],
  templateUrl: './register.html',
  styleUrl: './register.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RegisterPage {
  private readonly http = inject(HttpClient);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly playerName = signal('');
  protected readonly errorMessage = signal('');
  protected readonly isLoading = signal(false);

  protected async onRegister(): Promise<void> {
    const name = this.playerName().trim();
    if (!name) {
      this.errorMessage.set('Please enter your display name.');
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set('');

    try {
      const response = await new Promise<RegisterResponse>((resolve, reject) => {
        this.http.post<RegisterResponse>('/api/players', { name }).subscribe({
          next: resolve,
          error: reject,
        });
      });
      this.authService.storeToken(response.token);
      await this.router.navigate(['/']);
    } catch (err) {
      const error = err as HttpErrorResponse;
      if (error.status === 400) {
        this.errorMessage.set('Invalid name. Please check and try again.');
      } else {
        this.errorMessage.set('Registration failed. Please try again.');
      }
    } finally {
      this.isLoading.set(false);
    }
  }
}
