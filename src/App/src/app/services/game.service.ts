import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

interface CreateGameResponse {
  gameId: string;
  gameCode: string;
}

@Injectable({ providedIn: 'root' })
export class GameService {
  private readonly http = inject(HttpClient);

  createGame(): Promise<CreateGameResponse> {
    return firstValueFrom(this.http.post<CreateGameResponse>('/api/games', {}));
  }
}
