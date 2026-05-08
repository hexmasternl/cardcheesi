import { Component, inject } from '@angular/core';
import { RulesLanguageService } from '../rules-language.service';

@Component({
  selector: 'app-rules-pawns',
  imports: [],
  templateUrl: './rules-pawns.html',
  styleUrl: '../chapter.scss',
})
export class RulesPawns {
  readonly lang = inject(RulesLanguageService).lang;

  readonly content = {
    en: {
      title: 'Pawns',
      intro: 'Each player has 4 pawns. Pawns start in the reserve and must be brought into play one at a time using an Ace or King card. Understanding protection and hitting is key to both offence and defence.',
      sections: [
        {
          heading: 'Entering Play',
          body: 'A pawn enters the board at the player\'s home position by playing an Ace or a King. As soon as a pawn is placed on its home position, it is immediately protected.',
          list: [
            '🃏 Play an Ace or King to enter a pawn',
            '🏠 Pawn is placed on the player\'s home position',
            '🛡️ Newly entered pawn is immediately protected',
          ],
        },
        {
          heading: 'Protection',
          body: 'A pawn is protected when it is first placed on its home position. Protection prevents other players from interacting with it — but comes with trade-offs.',
          list: [
            '🛡️ Protected pawns cannot be hit (captured)',
            '🚧 No pawn may pass a protected pawn — not even a teammate',
            '👤 Only the owning player may move a protected pawn',
            '⚡ Protection is lost the moment the pawn moves or is swapped',
          ],
          callout: 'A protected pawn acts as a roadblock for everyone. Use this strategically to block opponents, but be aware that it also blocks your own teammates.',
        },
        {
          heading: 'Hitting a Pawn',
          body: 'When a pawn lands on a position occupied by an unprotected pawn of another player, the occupying pawn is hit and sent back to its owner\'s reserve. The attacking pawn takes that position.',
          list: [
            '💥 Land on an opponent\'s unprotected pawn to hit it',
            '📦 The hit pawn returns to the owner\'s reserve',
            '🔒 Protected pawns cannot be hit',
          ],
        },
        {
          heading: 'Finish Area',
          body: 'Once a pawn enters the finish area, it is permanently protected for the rest of the game.',
          list: [
            '🎯 Pawns in the finish area are permanently protected',
            '🚫 No pawn may pass or jump over a finish-area pawn',
            '🔒 A pawn cannot leave the finish area once it has entered',
            '📋 Place pawns in finish positions in order (closest first)',
          ],
        },
        {
          heading: 'Swapping Pawns (Jack)',
          body: 'When a Jack is played, the player swaps the board positions of two pawns. Several restrictions apply.',
          list: [
            '🔀 The two pawns must be of different colours',
            '🛡️ A protected pawn belonging to another player cannot be swapped',
            '✅ You may swap your own protected pawn',
            '🤝 Exception: if you are playing your teammate\'s pawns, you may swap their protected pawn',
            '🚫 Pawns in the finish area cannot be swapped',
          ],
        },
      ],
    },
    nl: {
      title: 'Pionnen',
      intro: 'Elke speler heeft 4 pionnen. Pionnen beginnen in de reserve en moeten één voor één op het bord worden gebracht met een Aas of een Heer. Bescherming en slaan zijn cruciaal voor aanval en verdediging.',
      sections: [
        {
          heading: 'Het Bord Opgaan',
          body: 'Een pion betreedt het bord op de startpositie van de speler door een Aas of een Heer te spelen. Zodra een pion op zijn startpositie staat, is hij direct beschermd.',
          list: [
            '🃏 Speel een Aas of Heer om een pion op het bord te zetten',
            '🏠 Pion wordt op de startpositie van de speler geplaatst',
            '🛡️ Nieuw geplaatste pion is direct beschermd',
          ],
        },
        {
          heading: 'Bescherming',
          body: 'Een pion is beschermd zodra hij op zijn startpositie staat. Bescherming verhindert dat andere spelers ermee kunnen interacteren — maar heeft ook nadelen.',
          list: [
            '🛡️ Beschermde pionnen kunnen niet geslagen worden',
            '🚧 Geen enkele pion mag een beschermde pion passeren — ook een teamgenoot niet',
            '👤 Alleen de eigenaar mag een beschermde pion verplaatsen',
            '⚡ Bescherming vervalt zodra de pion verplaatst of geruild wordt',
          ],
          callout: 'Een beschermde pion vormt een wegversperring voor iedereen. Gebruik dit strategisch om tegenstanders te blokkeren, maar bedenk dat het ook je eigen teamgenoten blokkeert.',
        },
        {
          heading: 'Een Pion Slaan',
          body: 'Wanneer een pion landt op een positie die bezet is door een onbeschermde pion van een andere speler, wordt die pion geslagen en teruggestuurd naar de reserve van de eigenaar. De aanvallende pion neemt die positie in.',
          list: [
            '💥 Land op de onbeschermde pion van een tegenstander om hem te slaan',
            '📦 De geslagen pion keert terug naar de reserve van de eigenaar',
            '🔒 Beschermde pionnen kunnen niet geslagen worden',
          ],
        },
        {
          heading: 'Eindzone',
          body: 'Zodra een pion de eindzone betreedt, is hij permanent beschermd voor de rest van het spel.',
          list: [
            '🎯 Pionnen in de eindzone zijn permanent beschermd',
            '🚫 Geen enkele pion mag een pion in de eindzone passeren of overspringen',
            '🔒 Een pion kan de eindzone niet meer verlaten',
            '📋 Plaats pionnen in volgorde in de eindvelden (dichtste eerst)',
          ],
        },
        {
          heading: 'Pionnen Ruilen (Boer)',
          body: 'Wanneer een Boer gespeeld wordt, ruilt de speler de bordposities van twee pionnen. Er gelden verschillende beperkingen.',
          list: [
            '🔀 De twee pionnen moeten van verschillende kleuren zijn',
            '🛡️ Een beschermde pion van een andere speler kan niet geruild worden',
            '✅ Je mag wel je eigen beschermde pion ruilen',
            '🤝 Uitzondering: als je de pionnen van je teamgenoot speelt, mag je hun beschermde pion ruilen',
            '🚫 Pionnen in de eindzone kunnen niet geruild worden',
          ],
        },
      ],
    },
  };
}
