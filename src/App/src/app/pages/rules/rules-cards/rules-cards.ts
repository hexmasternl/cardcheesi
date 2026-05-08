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
          effect: 'Move backward 4. A Four must be played to move a pawn exactly 4 positions backward (counter-clockwise). Backward movement can bypass protected pawns.',
        },
        {
          label: '7',
          name: 'Seven',
          effect: 'Move +7, split across up to 2 pawns. The 7 steps may be distributed freely (e.g., 3+4, 1+6) across a maximum of two pawns. A pawn must land on a valid position; partial moves into the finish area are not allowed.',
        },
        {
          label: 'J',
          name: 'Jack',
          effect: 'Swap two different-colour pawns. Choose any two active pawns on the main loop of different colours and swap their board positions. Protected pawns (of other players) and finish-area pawns cannot be swapped.',
        },
        {
          label: 'Q',
          name: 'Queen',
          effect: 'Move +12. Move any active pawn exactly 12 positions forward.',
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
          effect: 'Beweeg 4 achteruit. Een Vier moet gespeeld worden om een pion precies 4 posities achteruit (tegen de klok in) te verplaatsen. Achterwaartse beweging kan beschermde pionnen passeren.',
        },
        {
          label: '7',
          name: 'Zeven',
          effect: 'Beweeg +7, verdeeld over maximaal 2 pionnen. De 7 stappen mogen vrij verdeeld worden (bijv. 3+4, 1+6) over maximaal twee pionnen. Een pion moet op een geldige positie belanden; gedeeltelijke verplaatsingen naar de eindzone zijn niet toegestaan.',
        },
        {
          label: 'B',
          name: 'Boer',
          effect: 'Ruil twee pionnen van verschillende kleuren. Kies twee actieve pionnen op de hoofdlus van verschillende kleuren en wissel hun bordposities. Beschermde pionnen (van andere spelers) en pionnen in de eindzone kunnen niet geruild worden.',
        },
        {
          label: 'V',
          name: 'Vrouw',
          effect: 'Beweeg +12. Beweeg een actieve pion precies 12 posities naar voren.',
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
      ],
    },
  };
}
