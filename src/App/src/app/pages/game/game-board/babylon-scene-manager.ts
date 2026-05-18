import {
  AbstractEngine,
  ArcRotateCamera,
  BoundingInfo,
  Color4,
  DirectionalLight,
  Engine,
  HemisphericLight,
  Mesh,
  Scene,
  SceneLoader,
  Vector3,
  WebGPUEngine,
} from '@babylonjs/core';
import '@babylonjs/loaders/glTF';

async function createEngine(canvas: HTMLCanvasElement): Promise<AbstractEngine> {
  if (await WebGPUEngine.IsSupportedAsync) {
    const engine = new WebGPUEngine(canvas);
    await engine.initAsync();
    return engine;
  }
  return new Engine(canvas, true);
}

/**
 * Manages the Babylon.js engine, scene, camera, lighting, and static board mesh.
 *
 * Usage:
 *   const manager = await BabylonSceneManager.create(canvas);
 *   manager.startRenderLoop(() => { ... per-frame work ... });
 *   // on destroy:
 *   manager.dispose();
 */
export class BabylonSceneManager {
  private constructor(
    readonly engine: AbstractEngine,
    readonly scene: Scene,
    readonly camera: ArcRotateCamera,
  ) {}

  /**
   * Creates the engine, scene, camera, lighting, and loads the board GLB.
   * Returns `null` when the component was destroyed before the async work finished.
   */
  static async create(
    canvas: HTMLCanvasElement,
    isDestroyed: () => boolean,
  ): Promise<BabylonSceneManager | null> {
    const engine = await createEngine(canvas);

    if (isDestroyed()) {
      engine.dispose();
      return null;
    }

    engine.setHardwareScalingLevel(1 / window.devicePixelRatio);

    const scene = new Scene(engine);
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
        const boardRoot =
          transformNodes.find(n => n.name === '__root__') ?? transformNodes[0];
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
      return null;
    }

    return new BabylonSceneManager(engine, scene, camera);
  }

  /** Starts the Babylon render loop. `onTick` is called once per frame after scene.render(). */
  startRenderLoop(onTick: () => void): void {
    this.engine.runRenderLoop(() => {
      this.scene.render();
      onTick();
    });
  }

  dispose(): void {
    this.engine.dispose();
  }
}
