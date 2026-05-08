import { Component, inject } from '@angular/core';
import { RulesLanguageService } from '../rules-language.service';

@Component({
  selector: 'app-rules-gameplay',
  imports: [],
  templateUrl: './rules-gameplay.html',
  styleUrl: '../chapter.scss',
})
export class RulesGameplay {
  readonly lang = inject(RulesLanguageService).lang;

  readonly content = {
    en: {
      title: 'Gameplay',
      intro: 'CardCheesi is played clockwise. A dealer deals three rounds per "hand"; after all three rounds are played, the deal passes to the left. Each turn you must either play a card or discard.',
      sections: [
        {
          heading: 'Dealing',
          body: 'The dealer distributes cards in three separate rounds before play begins. The order of rounds within one deal is fixed.',
          list: [
            '1️⃣ Round 1 — 5 cards each',
            '2️⃣ Round 2 — 4 cards each',
            '3️⃣ Round 3 — 4 cards each (13 cards total per player per deal)',
            '🔄 After all three rounds are played, the deal passes one seat clockwise',
          ],
          callout: 'A new set of three rounds starts from the next dealer. Players keep unplayed cards between rounds within the same deal.',
        },
        {
          heading: 'Turn Order',
          body: 'Within each round, turns proceed clockwise starting with the player to the left of the dealer.',
          list: [
            '⏩ Clockwise turn order',
            '👤 Player to left of dealer goes first each round',
          ],
        },
        {
          heading: 'On Your Turn',
          body: 'On your turn you must do exactly one of the following. You cannot pass without discarding.',
          list: [
            '▶️ Play a card — perform the card\'s full effect',
            '🗑️ Discard a card — if you cannot or choose not to play, discard one card face-up',
          ],
        },
        {
          heading: 'Playing Your Teammate\'s Pawns',
          body: 'Once all of your own pawns are in the finish area, you may move your teammate\'s pawns on your turn. Normal rules apply — you play cards from your own hand to move their pawns.',
          list: [
            '✅ All 4 of your own pawns must be in the finish area first',
            '🃏 You use your own hand to move teammate pawns',
            '♟ All pawn rules (protection, hitting, etc.) still apply',
          ],
        },
      ],
    },
    nl: {
      title: 'Spelverloop',
      intro: 'CardCheesi wordt met de klok mee gespeeld. Een deler deelt drie rondes per "hand"; na alle drie de rondes gaat het deelrecht door naar links. Elke beurt moet je een kaart spelen of weggooien.',
      sections: [
        {
          heading: 'Delen',
          body: 'De deler verdeelt kaarten in drie aparte rondes voordat het spel begint. De volgorde van rondes binnen één deal is vastgesteld.',
          list: [
            '1️⃣ Ronde 1 — 5 kaarten elk',
            '2️⃣ Ronde 2 — 4 kaarten elk',
            '3️⃣ Ronde 3 — 4 kaarten elk (totaal 13 kaarten per speler per deal)',
            '🔄 Na alle drie de rondes gaat het deelrecht één positie met de klok mee',
          ],
          callout: 'Een nieuwe set van drie rondes begint bij de volgende deler. Spelers houden niet-gespeelde kaarten tussen rondes binnen dezelfde deal.',
        },
        {
          heading: 'Beurtenvolgorde',
          body: 'Binnen elke ronde gaan de beurten met de klok mee, te beginnen bij de speler links van de deler.',
          list: [
            '⏩ Beurtenvolgorde met de klok mee',
            '👤 De speler links van de deler begint elke ronde',
          ],
        },
        {
          heading: 'Jouw Beurt',
          body: 'Op jouw beurt moet je precies één van de volgende acties uitvoeren. Je kunt niet passen zonder een kaart weg te gooien.',
          list: [
            '▶️ Speel een kaart — voer het volledige effect van de kaart uit',
            '🗑️ Gooi een kaart weg — als je niet kunt of wilt spelen, gooi je één kaart open weg',
          ],
        },
        {
          heading: 'Pionnen van je Teamgenoot Spelen',
          body: 'Zodra al jouw pionnen in de eindzone staan, mag je de pionnen van je teamgenoot verplaatsen. De normale regels zijn van toepassing — je speelt kaarten uit je eigen hand om hun pionnen te verplaatsen.',
          list: [
            '✅ Al jouw 4 eigen pionnen moeten eerst in de eindzone staan',
            '🃏 Je gebruikt je eigen hand om de pionnen van je teamgenoot te verplaatsen',
            '♟ Alle pionregels (bescherming, slaan, enz.) blijven van toepassing',
          ],
        },
      ],
    },
  };
}
