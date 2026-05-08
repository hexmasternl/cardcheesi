import { Component, inject } from '@angular/core';
import { RulesLanguageService } from '../rules-language.service';

@Component({
  selector: 'app-rules-board',
  imports: [],
  templateUrl: './rules-board.html',
  styleUrl: '../chapter.scss',
})
export class RulesBoard {
  readonly lang = inject(RulesLanguageService).lang;

  readonly content = {
    en: {
      title: 'The Board',
      intro: 'The CardCheesi board consists of 64 positions arranged in a continuous loop, plus a dedicated finish area of 4 positions for each player. Understanding the layout is essential to planning your pawn routes.',
      homeTable: {
        caption: 'Home Positions',
        headers: ['Position', 'Player'],
        rows: [
          ['1',  'Player 1 home'],
          ['17', 'Player 2 home'],
          ['33', 'Player 3 home'],
          ['49', 'Player 4 home'],
        ],
      },
      sections: [
        {
          heading: 'Layout',
          body: 'The 64 positions form a closed loop. Between each pair of home positions there are exactly 15 regular positions, spacing the 4 players evenly around the board.',
          list: [
            '🔢 64 positions in a continuous loop',
            '🏠 4 home positions, one per player',
            '📏 15 regular positions between each home',
          ],
        },
        {
          heading: 'Finish Area',
          body: 'After completing a full loop of the board, a pawn approaches its finish area: 4 consecutive finish positions that only pawns of that colour may enter. Pawns must walk in from the loop — they cannot jump directly to the finish area.',
          list: [
            '🎯 4 finish positions per player (colour-exclusive)',
            '🔒 Pawns in the finish area can never leave',
            '🛡️ All pawns in the finish area are permanently protected',
            '📋 Fill finish positions in order (closest first) to keep the entry open',
          ],
        },
      ],
    },
    nl: {
      title: 'Het Bord',
      intro: 'Het CardCheesi-bord bestaat uit 64 posities in een gesloten lus, plus een eindzone van 4 posities voor elke speler. Inzicht in de indeling is essentieel om de routes van je pionnen te plannen.',
      homeTable: {
        caption: 'Startposities',
        headers: ['Positie', 'Speler'],
        rows: [
          ['1',  'Startpositie Speler 1'],
          ['17', 'Startpositie Speler 2'],
          ['33', 'Startpositie Speler 3'],
          ['49', 'Startpositie Speler 4'],
        ],
      },
      sections: [
        {
          heading: 'Indeling',
          body: 'De 64 posities vormen een gesloten lus. Tussen elk paar startposities bevinden zich precies 15 gewone posities, zodat de 4 spelers gelijkmatig rondom het bord verdeeld zijn.',
          list: [
            '🔢 64 posities in een gesloten lus',
            '🏠 4 startposities, één per speler',
            '📏 15 gewone posities tussen elke start',
          ],
        },
        {
          heading: 'Eindzone',
          body: 'Na het voltooien van een volledige ronde nadert een pion zijn eindzone: 4 aaneengesloten eindvelden die alleen pionnen van die kleur mogen betreden. Pionnen moeten via de lus binnenkomen — ze kunnen niet direct naar de eindzone springen.',
          list: [
            '🎯 4 eindvelden per speler (alleen voor eigen kleur)',
            '🔒 Pionnen in de eindzone kunnen nooit meer weg',
            '🛡️ Alle pionnen in de eindzone zijn permanent beschermd',
            '📋 Vul eindvelden op volgorde (dichtste eerst) zodat de ingang vrij blijft',
          ],
        },
      ],
    },
  };
}
