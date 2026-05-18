import {
  ActionManager,
  Animation,
  AssetContainer,
  Color3,
  ExecuteCodeAction,
  Mesh,
  PBRMaterial,
  Scene,
  SceneLoader,
  TransformNode,
  Vector3,
} from '@babylonjs/core';
import { GamePlayer, GameStatus, Pawn } from '../game-state.model';
import {
  PLAYER_COLORS,
  RESERVE_POSITIONS,
  resolveWorldPosition,
} from './board-coordinates';

interface SpawnedPawn {
  root: TransformNode;
  meshes: Mesh[];
  playerIndex: number;
  baseColor: Color3;
  pawnId: string;
}

/**
 * Manages pawn meshes in the Babylon.js scene.
 *
 * Responsibilities:
 *  - Loads the pawn GLB asset container
 *  - Spawns / re-places pawn instances per game state
 *  - Updates selectable (scale) and blinking (emissive) highlights
 *  - Wires click / hover ActionManagers and emits pawn-click callbacks
 *
 * Usage:
 *   const layer = await PawnLayer.create(scene, id => emit(id));
 *   layer.placePawns(players, status, blinking, selectable);
 *   // in render loop (called every ~500 ms):
 *   layer.tickBlink(getBlinkingIds);
 *   // on destroy:
 *   layer.dispose();
 */
export class PawnLayer {
  private readonly spawnedPawns = new Map<string, SpawnedPawn>();
  private blinkOn = false;
  private blinkTimer = 0;

  private constructor(
    private readonly scene: Scene,
    private readonly container: AssetContainer,
    private readonly onPawnClicked: (pawnId: string) => void,
  ) {}

  /**
   * Loads the pawn GLB asset container and returns a ready `PawnLayer`.
   * Returns `undefined` when the component was destroyed before loading finished.
   */
  static async create(
    scene: Scene,
    onPawnClicked: (pawnId: string) => void,
    isDestroyed: () => boolean,
  ): Promise<PawnLayer | undefined> {
    const container = await SceneLoader.LoadAssetContainerAsync(
      '/models/',
      'pawn.glb',
      scene,
    );

    if (isDestroyed()) {
      container.dispose();
      return undefined;
    }

    return new PawnLayer(scene, container, onPawnClicked);
  }

  /**
   * Removes all existing pawn meshes and re-spawns them according to the
   * current game state. Call on first render.
   */
  placePawns(
    players: GamePlayer[],
    status: 0 | 1 | 2,
    blinking: string[],
    selectable: string[],
  ): void {
    this.spawnedPawns.forEach(sp => sp.root.dispose(false, true));
    this.spawnedPawns.clear();

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
          const [x, y, z] = resolveWorldPosition(pawn, playerIndex, reserveIndex);
          if (pawn.location.$type === 'reserve') reserveIndex++;
          this.spawnPawn(playerIndex, pawn.id, x, y, z);
        }
      });
    }

    this.updateHighlights(blinking, selectable);
  }

  /**
   * Animates pawns from their current world positions to the positions described
   * in the new game state. Spawns new pawns and removes stale ones.
   * Call on subsequent state updates (after `placePawns` was called once).
   */
  movePawns(
    players: GamePlayer[],
    status: 0 | 1 | 2,
    blinking: string[],
    selectable: string[],
  ): void {
    if (status === GameStatus.Waiting) {
      this.placePawns(players, status, blinking, selectable);
      return;
    }

    const activePawnIds = new Set<string>();

    players.slice(0, 4).forEach((player, playerIndex) => {
      let reserveIndex = 0;
      for (const pawn of player.pawns) {
        const [x, y, z] = resolveWorldPosition(pawn, playerIndex, reserveIndex);
        if (pawn.location.$type === 'reserve') reserveIndex++;
        activePawnIds.add(pawn.id);

        const spawned = this.spawnedPawns.get(pawn.id);
        if (!spawned) {
          this.spawnPawn(playerIndex, pawn.id, x, y, z);
        } else {
          const target = new Vector3(x, y, z);
          if (!spawned.root.position.equalsWithEpsilon(target, 0.001)) {
            Animation.CreateAndStartAnimation(
              'pawnMove',
              spawned.root,
              'position',
              30,
              15,
              spawned.root.position.clone(),
              target,
              Animation.ANIMATIONLOOPMODE_CONSTANT,
            );
          }
        }
      }
    });

    // Remove pawns no longer in the game state
    for (const [id, sp] of this.spawnedPawns) {
      if (!activePawnIds.has(id)) {
        sp.root.dispose(false, true);
        this.spawnedPawns.delete(id);
      }
    }

    this.updateHighlights(blinking, selectable);
  }

  /**
   * Updates the visual state of all spawned pawns.
   * Selectable pawns are scaled up; non-blinking emissive is reset.
   */
  updateHighlights(blinking: string[], selectable: string[]): void {
    for (const spawned of this.spawnedPawns.values()) {
      const isBlinking = blinking.includes(spawned.pawnId);
      const isSelectable = selectable.includes(spawned.pawnId);

      if (!isBlinking) {
        spawned.meshes.forEach(m => {
          const mat = m.material as PBRMaterial;
          if (mat) mat.emissiveColor = Color3.Black();
        });
      }

      spawned.root.scaling = isSelectable
        ? new Vector3(1.25, 1.25, 1.25)
        : new Vector3(1, 1, 1);
    }
  }

  /**
   * Accumulates frame delta time and toggles the blinking emissive every 500 ms.
   * Call once per render-loop frame, passing the current frame delta in milliseconds.
   */
  tickBlink(deltaMs: number, getBlinkingIds: () => string[]): void {
    this.blinkTimer += deltaMs;
    if (this.blinkTimer < 500) return;

    this.blinkTimer = 0;
    this.blinkOn = !this.blinkOn;

    const blinking = getBlinkingIds();
    for (const spawned of this.spawnedPawns.values()) {
      if (blinking.includes(spawned.pawnId)) {
        const emissive = this.blinkOn ? spawned.baseColor.scale(0.6) : Color3.Black();
        spawned.meshes.forEach(m => ((m.material as PBRMaterial).emissiveColor = emissive));
      }
    }
  }

  dispose(): void {
    this.spawnedPawns.forEach(sp => sp.root.dispose(false, true));
    this.spawnedPawns.clear();
    this.container.dispose();
  }

  private spawnPawn(
    playerIndex: number,
    pawnId: string,
    x: number,
    y: number,
    z: number,
  ): void {
    const color = PLAYER_COLORS[playerIndex];
    const entries = this.container.instantiateModelsToScene(
      name => `pawn_p${playerIndex}_${pawnId}_${name}`,
    );
    const root = entries.rootNodes[0] as TransformNode;
    if (!root) return;

    root.position = new Vector3(x, y, z);

    const childMeshes = root.getChildMeshes() as Mesh[];
    childMeshes.forEach(mesh => {
      const mat = new PBRMaterial(`pawn_mat_${pawnId}`, this.scene);
      mat.albedoColor = color;
      mat.metallic = 0.1;
      mat.roughness = 0.5;
      mesh.material = mat;
    });

    const spawned: SpawnedPawn = {
      root,
      meshes: childMeshes,
      playerIndex,
      baseColor: color,
      pawnId,
    };
    this.spawnedPawns.set(pawnId, spawned);

    childMeshes.forEach(mesh => {
      mesh.actionManager = new ActionManager(this.scene);

      mesh.actionManager.registerAction(
        new ExecuteCodeAction(ActionManager.OnPickTrigger, () => {
          this.onPawnClicked(pawnId);
        }),
      );

      mesh.actionManager.registerAction(
        new ExecuteCodeAction(ActionManager.OnPointerOverTrigger, () => {
          const isBlinking = this.blinkOn;
          if (!isBlinking) {
            childMeshes.forEach(m => {
              (m.material as PBRMaterial).emissiveColor = color.scale(0.4);
            });
          }
        }),
      );

      mesh.actionManager.registerAction(
        new ExecuteCodeAction(ActionManager.OnPointerOutTrigger, () => {
          const isBlinking = this.blinkOn;
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
