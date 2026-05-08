import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { Lang } from '../rules-shell/rules-shell';

@Component({
  selector: 'app-rules-gameplay',
  templateUrl: './rules-gameplay.html',
  styleUrl: '../chapter.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RulesGameplay {
  readonly lang = input.required<Lang>();
}
