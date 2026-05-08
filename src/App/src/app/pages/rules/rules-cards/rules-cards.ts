import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { Lang } from '../rules-shell/rules-shell';

@Component({
  selector: 'app-rules-cards',
  templateUrl: './rules-cards.html',
  styleUrl: '../chapter.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RulesCards {
  readonly lang = input.required<Lang>();
}
