import { TestBed } from '@angular/core/testing';
import { PlayerJoinedEvent, PlayerStatusEvent, SseService } from './sse.service';

// Minimal EventSource mock
class MockEventSource {
  static instances: MockEventSource[] = [];

  url: string;
  listeners: Record<string, ((event: MessageEvent) => void)[]> = {};
  onerror: ((event: Event) => void) | null = null;

  constructor(url: string) {
    this.url = url;
    MockEventSource.instances.push(this);
  }

  addEventListener(type: string, handler: (event: MessageEvent) => void): void {
    this.listeners[type] ??= [];
    this.listeners[type].push(handler);
  }

  close(): void {}

  emit(type: string, data: string): void {
    const event = new MessageEvent(type, { data });
    this.listeners[type]?.forEach((h) => h(event));
  }
}

describe('SseService', () => {
  let service: SseService;
  let originalEventSource: typeof EventSource;

  beforeEach(() => {
    MockEventSource.instances = [];
    originalEventSource = (globalThis as unknown as Record<string, unknown>)['EventSource'] as typeof EventSource;
    (globalThis as unknown as Record<string, unknown>)['EventSource'] = MockEventSource as unknown as typeof EventSource;

    TestBed.configureTestingModule({});
    service = TestBed.inject(SseService);
  });

  afterEach(() => {
    (globalThis as unknown as Record<string, unknown>)['EventSource'] = originalEventSource;
    service.disconnect();
  });

  it('lastPlayerStatus updates when player-status event is received', () => {
    service.connect('GAME01', 'player-1');
    const [mock] = MockEventSource.instances;

    const payload: PlayerStatusEvent = { playerId: 'abc', playerName: 'Alice', status: 'Connected' };
    mock.emit('player-status', JSON.stringify(payload));

    expect(service.lastPlayerStatus()).toEqual(payload);
  });

  it('lastPlayerJoined updates when player-joined event is received', () => {
    service.connect('GAME01', 'player-1');
    const [mock] = MockEventSource.instances;

    const payload: PlayerJoinedEvent = { playerId: 'xyz', playerName: 'Bob' };
    mock.emit('player-joined', JSON.stringify(payload));

    expect(service.lastPlayerJoined()).toEqual(payload);
  });

  it('malformed player-joined payload is ignored', () => {
    service.connect('GAME01', 'player-1');
    const [mock] = MockEventSource.instances;

    mock.emit('player-joined', 'not-json{{');

    expect(service.lastPlayerJoined()).toBeNull();
  });

  it('malformed player-status payload is ignored', () => {
    service.connect('GAME01', 'player-1');
    const [mock] = MockEventSource.instances;

    mock.emit('player-status', 'not-json{{');

    expect(service.lastPlayerStatus()).toBeNull();
  });

  it('lastPlayerJoined resets to null on disconnect', () => {
    service.connect('GAME01', 'player-1');
    const [mock] = MockEventSource.instances;

    mock.emit('player-joined', JSON.stringify({ playerId: 'x', playerName: 'X' }));
    expect(service.lastPlayerJoined()).not.toBeNull();

    service.disconnect();
    expect(service.lastPlayerJoined()).toBeNull();
  });
});
