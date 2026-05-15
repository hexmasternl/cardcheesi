import {
  afterNextRender,
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  ElementRef,
  effect,
  inject,
  input,
  viewChild,
} from '@angular/core';
import {
  AbstractEngine,
  ArcRotateCamera,
  AssetContainer,
  BoundingInfo,
  Color3,
  Color4,
  DirectionalLight,
  Engine,
  HemisphericLight,
  Mesh,
  PBRMaterial,
  Scene,
  SceneLoader,
  TransformNode,
  Vector3,
  WebGPUEngine,
} from '@babylonjs/core';
import '@babylonjs/loaders/glTF';
import { GamePlayer, GameStatus } from '../game-state.model';

/** One PBR colour per player slot (index 0–3). */
const PLAYER_COLORS: Color3[] = [
  new Color3(0.85, 0.12, 0.12), // P1 – red
  new Color3(0.12, 0.28, 0.85), // P2 – blue
  new Color3(0.12, 0.70, 0.20), // P3 – green
  new Color3(0.88, 0.75, 0.02), // P4 – yellow
];

/**
 * Reserve spot world positions (X, Y, Z) per player slot.
 * Derived from board.glb node translations (corner cylinder clusters
 * at ±0.50/0.58 on X/Z, board surface at Y = 0.006).
 */
const RESERVE_POSITIONS: [number, number, number][][] = [
  [[-0.58, 0.006, -0.58], [-0.50, 0.006, -0.58], [-0.50, 0.006, -0.50], [-0.58, 0.006, -0.50]],
  [[ 0.58, 0.006, -0.50], [ 0.50, 0.006, -0.50], [ 0.50, 0.006, -0.58], [ 0.58, 0.006, -0.58]],
  [[ 0.58, 0.006,  0.58], [ 0.50, 0.006,  0.58], [ 0.50, 0.006,  0.50], [ 0.58, 0.006,  0.50]],
  [[-0.58, 0.006,  0.50], [-0.50, 0.006,  0.50], [-0.50, 0.006,  0.58], [-0.58, 0.006,  0.58]],
];

async function createEngine(canvas: HTMLCanvasElement): Promise<AbstractEngine> {
  if (await WebGPUEngine.IsSupportedAsync) {
    const engine = new WebGPUEngine(canvas);
    await engine.initAsync();
    return engine;
  }
  return new Engine(canvas, true);
}

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

  private engine?: AbstractEngine;
  private scene?: Scene;
  private pawnContainer?: AssetContainer;
  private readonly spawnedPawnRoots: TransformNode[] = [];

  readonly players = input<GamePlayer[]>([]);
  readonly gameStatus = input<0 | 1 | 2>(0);

  constructor() {
    const destroyRef = inject(DestroyRef);
    let destroyed = false;

    // Re-place pawns reactively whenever the player list or game status changes.
    // The guard ensures the scene and pawn container are ready first; the
    // manual call inside initScene() handles the initial placement.
    effect(() => {
      const players = this.players();
      const status = this.gameStatus();
      if (this.scene && this.pawnContainer) {
        this.placePawns(players, status);
      }
    });

    afterNextRender(async () => {
      await this.initScene(() => destroyed);
    });

    destroyRef.onDestroy(() => {
      destroyed = true;
      this.engine?.dispose();
      window.removeEventListener('resize', this.onResize);
    });
  }

  private readonly onResize = (): void => this.engine?.resize();

  private async initScene(isDestroyed: () => boolean): Promise<void> {
    const canvas = this.canvasRef().nativeElement;
    this.engine = await createEngine(canvas);

    if (isDestroyed()) {
      this.engine.dispose();
      return;
    }

    this.engine.setHardwareScalingLevel(1 / window.devicePixelRatio);

    const scene = new Scene(this.engine);
    scene.clearColor = new Color4(0.04, 0.12, 0.23, 1); // #0b1e3a

    const camera = new ArcRotateCamera(
      'camera',
      -Math.PI / 2,
      Math.PI / 3.5,
      1.4,
      Vector3.Zero(),
      scene,
    );
    camera.lowerRadiusLimit = 1.4;
    camera.upperRadiusLimit = 6;
    camera.lowerBetaLimit = 0.2;
    camera.upperBetaLimit = Math.PI / 2.1;
    camera.wheelPrecision = 50;
    camera.attachControl(canvas, true);

    new HemisphericLight('ambientLight', new Vector3(0, 1, 0), scene).intensity = 0.7;
    const dirLight = new DirectionalLight('dirLight', new Vector3(-1, -2, -1), scene);
    dirLight.intensity = 0.8;

    await SceneLoader.ImportMeshAsync('', '/models/', 'board.glb', scene).then(
      ({ meshes }) => {
        const renderMeshes = meshes.filter((m): m is Mesh => m instanceof Mesh);
        if (renderMeshes.length > 0) {
          let min = renderMeshes[0].getBoundingInfo().boundingBox.minimumWorld;
          let max = renderMeshes[0].getBoundingInfo().boundingBox.maximumWorld;
          for (const mesh of renderMeshes) {
            const info: BoundingInfo = mesh.getBoundingInfo();
            min = Vector3.Minimize(min, info.boundingBox.minimumWorld);
            max = Vector3.Maximize(max, info.boundingBox.maximumWorld);
          }
          camera.target = Vector3.Center(min, max);
        }
        meshes.forEach(mesh => mesh.freezeWorldMatrix());
      },
    );

    if (isDestroyed()) {
      scene.dispose();
      return;
    }

    this.pawnContainer = await SceneLoader.LoadAssetContainerAsync(
      '/models/',
      'pawn.glb',
      scene,
    );

    if (isDestroyed()) {
      this.pawnContainer.dispose();
      scene.dispose();
      return;
    }

    this.scene = scene;
    this.placePawns(this.players(), this.gameStatus());

    this.engine.runRenderLoop(() => {
      scene.render();
      this.zoomLabelRef().nativeElement.textContent = `zoom: ${camera.radius.toFixed(2)}`;
    });

    window.addEventListener('resize', this.onResize);
  }

  private placePawns(players: GamePlayer[], status: 0 | 1 | 2): void {
    this.spawnedPawnRoots.forEach(root => root.dispose(false, true));
    this.spawnedPawnRoots.length = 0;

    if (status !== GameStatus.Waiting || !this.pawnContainer || !this.scene) return;

    players.slice(0, 4).forEach((_, playerIndex) => {
      const positions = RESERVE_POSITIONS[playerIndex];
      const color = PLAYER_COLORS[playerIndex];

      for (let i = 0; i < 4; i++) {
        const entries = this.pawnContainer!.instantiateModelsToScene(
          name => `pawn_p${playerIndex}_${i}_${name}`,
        );
        const root = entries.rootNodes[0] as TransformNode;
        if (!root) continue;

        const [x, y, z] = positions[i];
        root.position = new Vector3(x, y, z);

        root.getChildMeshes().forEach(mesh => {
          const mat = new PBRMaterial(`pawn_mat_p${playerIndex}_${i}`, this.scene!);
          mat.albedoColor = color;
          mat.metallic = 0.1;
          mat.roughness = 0.5;
          mesh.material = mat;
        });

        this.spawnedPawnRoots.push(root);
      }
    });
  }
}
