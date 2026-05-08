import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { catchError, Observable, throwError } from 'rxjs';
import { GameState } from './game-state.model';

@Injectable({ providedIn: 'root' })
export class GameService {
  private readonly http = inject(HttpClient);

  getByCode(code: string): Observable<GameState> {
    return this.http.get<GameState>(`/api/games/${code}`).pipe(
      catchError((err: HttpErrorResponse) => throwError(() => err))
    );
  }
}
