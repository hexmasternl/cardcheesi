import {
  afterNextRender,
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  ElementRef,
  effect,
  inject,
  input,
  output,
  viewChild,
} from '@angular/core';
import { GamePlayer } from '../game-state.model';
import { BabylonSceneManager } from './babylon-scene-manager';
import { PawnLayer } from './pawn-layer';

/**
 * Hosts the Babylon.js canvas, orchestrates `BabylonSceneManager` (engine/scene/camera)
 * and `PawnLayer` (pawn lifecycle), and bridges Angular signal inputs/outputs to the 3D scene.
 */
@Component({
  selector: 'app-game-board',
  template: `
    <canvas #gameCanvas class="game-board__canvas"></canvas>
    <div #zoomLabel class="game-board__zoom-label">zoom: –</div>
  `,
  styleUrl: './game-board.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GameBoardComponent {
  private readonly canvasRef =
    viewChild.required<ElementRef<HTMLCanvasElement>>('gameCanvas');
  private readonly zoomLabelRef =
    viewChild.required<ElementRef<HTMLDivElement>>('zoomLabel');

  private sceneManager?: BabylonSceneManager;
  private pawnLayer?: PawnLayer;
  private boardInitialized = false;

  readonly players = input<GamePlayer[]>([]);
  readonly gameStatus = input<0 | 1 | 2>(0);
  readonly blinkingPawnIds = input<string[]>([]);
  readonly selectablePawnIds = input<string[]>([]);

  readonly pawnClicked = output<string>();

  constructor() {
    const destroyRef = inject(DestroyRef);
    let destroyed = false;

    effect(() => {
      const players = this.players();
      const status = this.gameStatus();
      const blinking = this.blinkingPawnIds();
      const selectable = this.selectablePawnIds();
      if (!this.pawnLayer) return;
      if (this.boardInitialized) {
        this.pawnLayer.movePawns(players, status, blinking, selectable);
      } else {
        this.pawnLayer.placePawns(players, status, blinking, selectable);
      }
    });

    effect(() => {
      const blinking = this.blinkingPawnIds();
      const selectable = this.selectablePawnIds();
      this.pawnLayer?.updateHighlights(blinking, selectable);
    });

    afterNextRender(async () => {
      await this.initScene(() => destroyed);
    });

    destroyRef.onDestroy(() => {
      destroyed = true;
      this.pawnLayer?.dispose();
      this.sceneManager?.dispose();
      window.removeEventListener('resize', this.onResize);
    });
  }

  private readonly onResize = (): void => this.sceneManager?.engine.resize();

  private async initScene(isDestroyed: () => boolean): Promise<void> {
    const canvas = this.canvasRef().nativeElement;

    this.sceneManager = await BabylonSceneManager.create(canvas, isDestroyed);
    if (!this.sceneManager) return;

    this.pawnLayer = await PawnLayer.create(
      this.sceneManager.scene,
      id => this.pawnClicked.emit(id),
      isDestroyed,
    );
    if (!this.pawnLayer) return;

    this.pawnLayer.placePawns(
      this.players(),
      this.gameStatus(),
      this.blinkingPawnIds(),
      this.selectablePawnIds(),
    );
    this.boardInitialized = true;

    const { camera, scene } = this.sceneManager;
    this.sceneManager.startRenderLoop(() => {
      this.zoomLabelRef().nativeElement.textContent = `zoom: ${camera.radius.toFixed(2)}`;
      this.pawnLayer!.tickBlink(scene.getEngine().getDeltaTime(), () => this.blinkingPawnIds());
    });

    window.addEventListener('resize', this.onResize);
  }
}
