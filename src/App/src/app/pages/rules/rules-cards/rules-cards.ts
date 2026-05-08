import { Component, inject } from '@angular/core';
import { RulesLanguageService } from '../rules-language.service';

@Component({
  selector: 'app-rules-cards',
  imports: [],
  templateUrl: './rules-cards.html',
  styleUrl: '../chapter.scss',
})
export class RulesCards {
  readonly lang = inject(RulesLanguageService).lang;

  readonly content = {
    en: {
      title: 'Cards',
      intro: 'A standard 52-card deck is used. Every card value has a distinct effect. Special cards (Ace, King, Four, Seven, Jack, Queen) have unique abilities; numbered cards simply move a pawn forward.',
      specialHeading: 'Special Cards',
      numberedHeading: 'Numbered Cards',
      specials: [
        {
          label: 'A',
          name: 'Ace',
          effect: 'Enter reserve or move +1. Play an Ace to bring a pawn out of reserve onto your home position, or move any active pawn exactly 1 position forward.',
        },
        {
          label: 'K',
          name: 'King',
          effect: 'Enter reserve only. A King may only be used to bring a pawn out of reserve onto your home position. It cannot be used to move an already-active pawn.',
        },
        {
          label: '4',
          name: 'Four',
          effect: 'Move backward 4. Move a pawn exactly 4 positions backward (counter-clockwise). Like all moves, backward movement cannot pass a protected pawn.',
        },
        {
          label: '7',
          name: 'Seven',
          effect: 'Move +7, optionally split across up to 2 pawns. Move one pawn 7 positions forward, or freely split the 7 steps across at most two pawns (e.g., 3+4, 1+6). Partial moves — including moves that bring a pawn into the finish area mid-split — are allowed.',
        },
        {
          label: 'J',
          name: 'Jack',
          effect: 'Swap two different-colour pawns. Choose any two active pawns on the main loop of different colours and swap their board positions. Protected pawns (of other players) and finish-area pawns cannot be swapped.',
        },
      ],
      numbered: [
        { label: '2',  effect: 'Move +2 forward.' },
        { label: '3',  effect: 'Move +3 forward.' },
        { label: '5',  effect: 'Move +5 forward.' },
        { label: '6',  effect: 'Move +6 forward.' },
        { label: '8',  effect: 'Move +8 forward.' },
        { label: '9',  effect: 'Move +9 forward.' },
        { label: '10', effect: 'Move +10 forward.' },
        { label: 'Q',  effect: 'Move +12 forward.' },
      ],
    },
    nl: {
      title: 'Kaarten',
      intro: 'Er wordt gespeeld met een standaard kaartspel van 52 kaarten. Elke kaartwaarde heeft een eigen effect. Bijzondere kaarten (Aas, Heer, Vier, Zeven, Boer, Vrouw) hebben unieke vaardigheden; genummerde kaarten bewegen een pion simpelweg naar voren.',
      specialHeading: 'Bijzondere Kaarten',
      numberedHeading: 'Genummerde Kaarten',
      specials: [
        {
          label: 'A',
          name: 'Aas',
          effect: 'Reserve opgaan of +1 bewegen. Speel een Aas om een pion uit de reserve op je startpositie te plaatsen, of beweeg een actieve pion precies 1 positie naar voren.',
        },
        {
          label: 'H',
          name: 'Heer',
          effect: 'Alleen reserve opgaan. Een Heer mag alleen gebruikt worden om een pion uit de reserve op de startpositie te plaatsen. Hij kan niet gebruikt worden om een al actieve pion te verplaatsen.',
        },
        {
          label: '4',
          name: 'Vier',
          effect: 'Beweeg 4 achteruit. Beweeg een pion precies 4 posities achteruit (tegen de klok in). Net als alle andere zetten kan achterwaartse beweging een beschermde pion niet passeren.',
        },
        {
          label: '7',
          name: 'Zeven',
          effect: 'Beweeg +7, optioneel verdeeld over maximaal 2 pionnen. Beweeg één pion 7 posities naar voren, of verdeel de 7 stappen vrij over maximaal twee pionnen (bijv. 3+4, 1+6). Gedeeltelijke zetten — inclusief zetten waarbij een pion halverwege de eindzone betreedt — zijn toegestaan.',
        },
        {
          label: 'B',
          name: 'Boer',
          effect: 'Ruil twee pionnen van verschillende kleuren. Kies twee actieve pionnen op de hoofdlus van verschillende kleuren en wissel hun bordposities. Beschermde pionnen (van andere spelers) en pionnen in de eindzone kunnen niet geruild worden.',
        },
      ],
      numbered: [
        { label: '2',  effect: 'Beweeg +2 naar voren.' },
        { label: '3',  effect: 'Beweeg +3 naar voren.' },
        { label: '5',  effect: 'Beweeg +5 naar voren.' },
        { label: '6',  effect: 'Beweeg +6 naar voren.' },
        { label: '8',  effect: 'Beweeg +8 naar voren.' },
        { label: '9',  effect: 'Beweeg +9 naar voren.' },
        { label: '10', effect: 'Beweeg +10 naar voren.' },
        { label: 'V',  effect: 'Beweeg +12 naar voren.' },
      ],
    },
  };
}
