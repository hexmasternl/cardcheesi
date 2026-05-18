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
import {
  AbstractEngine,
  ActionManager,
  ArcRotateCamera,
  AssetContainer,
  BoundingInfo,
  Color3,
  Color4,
  DirectionalLight,
  Engine,
  ExecuteCodeAction,
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
import { GamePlayer, GameStatus, Pawn, PawnStatus } from '../game-state.model';

/** One PBR colour per player slot (index 0–3). */
const PLAYER_COLORS: Color3[] = [
  new Color3(0.12, 0.70, 0.20), // P1 – green
  new Color3(0.85, 0.12, 0.12), // P2 – red
  new Color3(0.88, 0.75, 0.02), // P3 – yellow
  new Color3(0.12, 0.28, 0.85), // P4 – blue
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

/** Y-height for pawns on the board surface. */
const BOARD_Y = 0.012;

/**
 * Maps board position 1–64 to world XZ coordinates.
 * Positions run clockwise: 1–16 bottom row (left→right),
 * 17–32 right column (bottom→top), 33–48 top row (right→left),
 * 49–64 left column (top→bottom).
 */
function boardPositionToWorld(position: number): [number, number, number] {
  const INNER = 0.38; // inner path edge
  const OUTER = 0.43; // center of path squares on outer edge
  const SPAN = OUTER * 2; // total span across the board face
  const t = (n: number, count = 15) => n / count; // 0..1

  if (position >= 1 && position <= 16) {
    return [-OUTER + t(position - 1) * SPAN, BOARD_Y, -INNER];
  } else if (position >= 17 && position <= 32) {
    return [INNER, BOARD_Y, -OUTER + t(position - 17) * SPAN];
  } else if (position >= 33 && position <= 48) {
    return [OUTER - t(position - 33) * SPAN, BOARD_Y, INNER];
  } else {
    return [-INNER, BOARD_Y, OUTER - t(position - 49) * SPAN];
  }
}

/**
 * Maps finish slot (1–4) for a given player index to world XZ coordinates.
 * Each player's finish track runs from the board edge toward the center.
 */
function finishPositionToWorld(playerIndex: number, slot: number): [number, number, number] {
  const s = slot - 1; // 0-based
  const step = 0.075;
  switch (playerIndex) {
    case 0: return [0,      BOARD_Y, -0.26 + s * step]; // P1: bottom center → inward
    case 1: return [0.26 - s * step, BOARD_Y, 0];       // P2: right center → inward
    case 2: return [0,      BOARD_Y,  0.26 - s * step]; // P3: top center → inward
    case 3: return [-0.26 + s * step, BOARD_Y, 0];      // P4: left center → inward
    default: return [0, BOARD_Y, 0];
  }
}

async function createEngine(canvas: HTMLCanvasElement): Promise<AbstractEngine> {
  if (await WebGPUEngine.IsSupportedAsync) {
    const engine = new WebGPUEngine(canvas);
    await engine.initAsync();
    return engine;
  }
  return new Engine(canvas, true);
}

interface SpawnedPawn {
  root: TransformNode;
  meshes: Mesh[];
  playerIndex: number;
  baseColor: Color3;
  pawnId: string;
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
  private readonly spawnedPawns: SpawnedPawn[] = [];
  private blinkTimer = 0;

  readonly players = input<GamePlayer[]>([]);
  readonly gameStatus = input<0 | 1 | 2>(0);
  readonly blinkingPawnIds = input<string[]>([]);
  readonly selectablePawnIds = input<string[]>([]);

  readonly pawnClicked = output<string>();

  constructor() {
    const destroyRef = inject(DestroyRef);
    let destroyed = false;

    // Re-place pawns reactively whenever the player list or game status changes.
    effect(() => {
      const players = this.players();
      const status = this.gameStatus();
      if (this.scene && this.pawnContainer) {
        this.placePawns(players, status);
      }
    });

    // Update blink/selectable highlight reactively
    effect(() => {
      const blinking = this.blinkingPawnIds();
      const selectable = this.selectablePawnIds();
      this.updatePawnHighlights(blinking, selectable);
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
      1.9,
      Vector3.Zero(),
      scene,
    );
    camera.lowerRadiusLimit = 1.9;
    camera.upperRadiusLimit = 6;
    camera.lowerBetaLimit = 0.2;
    camera.upperBetaLimit = Math.PI / 2.1;
    camera.wheelPrecision = 50;
    camera.attachControl(canvas, true);

    new HemisphericLight('ambientLight', new Vector3(0, 1, 0), scene).intensity = 0.7;
    const dirLight = new DirectionalLight('dirLight', new Vector3(-1, -2, -1), scene);
    dirLight.intensity = 0.8;

    await SceneLoader.ImportMeshAsync('', '/models/', 'board.glb', scene).then(
      ({ meshes, transformNodes }) => {
        const boardRoot = transformNodes.find(n => n.name === '__root__') ?? transformNodes[0];
        if (boardRoot) {
          boardRoot.position.y = -0.02;
        }

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

    // Blink timer: toggles emissive on blinking pawns each ~500ms
    this.engine.runRenderLoop(() => {
      scene.render();
      this.zoomLabelRef().nativeElement.textContent = `zoom: ${camera.radius.toFixed(2)}`;

      this.blinkTimer += scene.getEngine().getDeltaTime();
      if (this.blinkTimer >= 500) {
        this.blinkTimer = 0;
        this.tickBlink();
      }
    });

    window.addEventListener('resize', this.onResize);
  }

  private blinkOn = false;
  private tickBlink(): void {
    this.blinkOn = !this.blinkOn;
    const blinking = this.blinkingPawnIds();
    for (const spawned of this.spawnedPawns) {
      if (blinking.includes(spawned.pawnId)) {
        const emissive = this.blinkOn ? spawned.baseColor.scale(0.6) : Color3.Black();
        spawned.meshes.forEach(m => (m.material as PBRMaterial).emissiveColor = emissive);
      }
    }
  }

  private updatePawnHighlights(blinking: string[], selectable: string[]): void {
    for (const spawned of this.spawnedPawns) {
      const isBlinking = blinking.includes(spawned.pawnId);
      const isSelectable = selectable.includes(spawned.pawnId);

      if (!isBlinking) {
        // Reset emissive for non-blinking pawns
        spawned.meshes.forEach(m => {
          const mat = m.material as PBRMaterial;
          if (mat) mat.emissiveColor = Color3.Black();
        });
      }

      // Show ring/outline for selectable pawns (scale them up slightly)
      spawned.root.scaling = isSelectable
        ? new Vector3(1.25, 1.25, 1.25)
        : new Vector3(1, 1, 1);
    }
  }

  private placePawns(players: GamePlayer[], status: 0 | 1 | 2): void {
    this.spawnedPawns.forEach(sp => sp.root.dispose(false, true));
    this.spawnedPawns.length = 0;

    if (!this.pawnContainer || !this.scene) return;

    if (status === GameStatus.Waiting) {
      players.slice(0, 4).forEach((player, playerIndex) => {
        const positions = RESERVE_POSITIONS[playerIndex];
        for (let i = 0; i < 4; i++) {
          const [x, y, z] = positions[i];
          const pawnId = (player.pawns[i] as Pawn | undefined)?.id ?? `p${playerIndex}_${i}`;
          this.spawnPawn(playerIndex, pawnId, x, y, z);
        }
      });
    } else if (status === GameStatus.InProgress) {
      players.slice(0, 4).forEach((player, playerIndex) => {
        let reserveIndex = 0;
        for (const pawn of player.pawns) {
          let pos: [number, number, number];
          if (pawn.location.$type === 'reserve') {
            pos = RESERVE_POSITIONS[playerIndex][reserveIndex++] ?? [0, BOARD_Y, 0];
          } else if (pawn.location.$type === 'board') {
            pos = boardPositionToWorld(pawn.location.position);
          } else {
            pos = finishPositionToWorld(playerIndex, pawn.location.slot);
          }
          this.spawnPawn(playerIndex, pawn.id, ...pos);
        }
      });
    }

    // Re-apply highlights after re-placing
    this.updatePawnHighlights(this.blinkingPawnIds(), this.selectablePawnIds());
  }

  private spawnPawn(playerIndex: number, pawnId: string, x: number, y: number, z: number): void {
    const color = PLAYER_COLORS[playerIndex];
    const entries = this.pawnContainer!.instantiateModelsToScene(
      name => `pawn_p${playerIndex}_${pawnId}_${name}`,
    );
    const root = entries.rootNodes[0] as TransformNode;
    if (!root) return;

    root.position = new Vector3(x, y, z);

    const childMeshes = root.getChildMeshes() as Mesh[];
    childMeshes.forEach(mesh => {
      const mat = new PBRMaterial(`pawn_mat_${pawnId}`, this.scene!);
      mat.albedoColor = color;
      mat.metallic = 0.1;
      mat.roughness = 0.5;
      mesh.material = mat;
    });

    const spawned: SpawnedPawn = { root, meshes: childMeshes, playerIndex, baseColor: color, pawnId };
    this.spawnedPawns.push(spawned);

    // Click and hover actions
    childMeshes.forEach(mesh => {
      mesh.actionManager = new ActionManager(this.scene!);

      mesh.actionManager.registerAction(
        new ExecuteCodeAction(ActionManager.OnPickTrigger, () => {
          this.pawnClicked.emit(pawnId);
        }),
      );

      mesh.actionManager.registerAction(
        new ExecuteCodeAction(ActionManager.OnPointerOverTrigger, () => {
          const isBlinking = this.blinkingPawnIds().includes(pawnId);
          if (!isBlinking) {
            childMeshes.forEach(m => {
              (m.material as PBRMaterial).emissiveColor = color.scale(0.4);
            });
          }
        }),
      );

      mesh.actionManager.registerAction(
        new ExecuteCodeAction(ActionManager.OnPointerOutTrigger, () => {
          const isBlinking = this.blinkingPawnIds().includes(pawnId);
          if (!isBlinking) {
            childMeshes.forEach(m => {
              (m.material as PBRMaterial).emissiveColor = Color3.Black();
            });
          }
        }),
      );
    });
  }
}
