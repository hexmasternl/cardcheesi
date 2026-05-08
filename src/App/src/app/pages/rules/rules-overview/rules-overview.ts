import { Component, inject } from '@angular/core';
import { RulesLanguageService } from '../rules-language.service';

@Component({
  selector: 'app-rules-overview',
  imports: [],
  templateUrl: './rules-overview.html',
  styleUrl: '../chapter.scss',
})
export class RulesOverview {
  readonly lang = inject(RulesLanguageService).lang;

  readonly content = {
    en: {
      title: 'Game Overview',
      intro: 'CardCheesi is a board game for 4 players, inspired by the classic Parcheesi. Instead of rolling dice to determine movement, players draw and play cards — making every turn a strategic choice.',
      sections: [
        {
          heading: 'Players & Teams',
          body: 'There are always exactly 4 players, competing as 2 teams of 2. Teammates sit opposite each other around the board.',
          list: [
            '👥 Team A: Player 1 + Player 3',
            '👥 Team B: Player 2 + Player 4',
            '♟ Each player has 4 pawns of their own colour',
          ],
        },
        {
          heading: 'Winning the Game',
          body: "A player advances their own 4 pawns until all are in the finish area. Once a player's own pawns are all finished, they may move their teammate's pawns. The game ends when both players of a team have all their pawns in the finish area — that team wins.",
          list: [
            '✅ Move all 4 of your own pawns into the finish area',
            '🤝 Then help your teammate get their 4 pawns finished',
            '🏆 First complete team wins',
          ],
        },
      ],
    },
    nl: {
      title: 'Speloverzicht',
      intro: 'CardCheesi is een bordspel voor 4 spelers, geïnspireerd op het klassieke Parcheesi. In plaats van dobbelstenen gooien, trekken en spelen spelers kaarten — elke beurt is een strategische keuze.',
      sections: [
        {
          heading: 'Spelers & Teams',
          body: 'Er zijn altijd precies 4 spelers, verdeeld in 2 teams van 2. Teamgenoten zitten tegenover elkaar aan het bord.',
          list: [
            '👥 Team A: Speler 1 + Speler 3',
            '👥 Team B: Speler 2 + Speler 4',
            '♟ Elke speler heeft 4 pionnen van hun eigen kleur',
          ],
        },
        {
          heading: 'Het Spel Winnen',
          body: 'Een speler brengt zijn eigen 4 pionnen naar het eindvak. Zodra alle eigen pionnen klaar zijn, mag de speler de pionnen van zijn teamgenoot verplaatsen. Het spel eindigt wanneer beide spelers van een team al hun pionnen in het eindvak hebben — dat team wint.',
          list: [
            '✅ Breng al jouw 4 pionnen naar het eindvak',
            '🤝 Help daarna je teamgenoot hun 4 pionnen te finishen',
            '🏆 Het eerste complete team wint',
          ],
        },
      ],
    },
  };
}
