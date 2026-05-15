import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  ElementRef,
  inject,
  input,
  model,
  output,
  signal,
  viewChild,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ChatMessageEvent } from '../../../services/sse.service';
import { AuthService } from '../../../services/auth.service';

@Component({
  selector: 'app-chat-panel',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './chat-panel.html',
  styleUrl: './chat-panel.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ChatPanelComponent {
  private readonly authService = inject(AuthService);

  readonly messages = input<ChatMessageEvent[]>([]);
  readonly expanded = model(false);

  readonly sendMessage = output<string>();

  protected readonly inputText = signal('');
  protected readonly canSend = computed(() => this.inputText().trim().length > 0);
  protected readonly myPlayerId = computed(() => this.authService.getPlayerId());

  private readonly lastSeenCount = signal(0);

  protected readonly unreadCount = computed(() => {
    if (this.expanded()) return 0;
    return Math.max(0, this.messages().length - this.lastSeenCount());
  });

  private readonly listRef = viewChild<ElementRef<HTMLElement>>('messageList');

  constructor() {
    // When panel is expanded, mark all current messages as seen and scroll to bottom
    effect(() => {
      if (this.expanded()) {
        this.lastSeenCount.set(this.messages().length);
        queueMicrotask(() => this.scrollToBottom());
      }
    });

    // Scroll to bottom when new messages arrive while panel is already expanded
    effect(() => {
      const msgs = this.messages();
      if (this.expanded() && msgs.length > 0) {
        queueMicrotask(() => this.scrollToBottom());
      }
    });
  }

  protected toggle(): void {
    this.expanded.update((v) => !v);
  }

  protected onSend(): void {
    const text = this.inputText().trim();
    if (!text) return;
    this.sendMessage.emit(text);
    this.inputText.set('');
  }

  protected onKeydown(event: KeyboardEvent): void {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      this.onSend();
    }
  }

  protected formatTime(isoTimestamp: string): string {
    const date = new Date(isoTimestamp);
    return date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
  }

  protected isOwnMessage(senderId: string): boolean {
    return senderId === this.myPlayerId();
  }

  private scrollToBottom(): void {
    const el = this.listRef()?.nativeElement;
    if (el) el.scrollTop = el.scrollHeight;
  }
}
