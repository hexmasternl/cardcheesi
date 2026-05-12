import {
  afterNextRender,
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  ElementRef,
  inject,
  viewChild,
} from '@angular/core';
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

  constructor() {
    const destroyRef = inject(DestroyRef);

    afterNextRender(async () => {
      await this.initScene();
    });

    destroyRef.onDestroy(() => {
      this.engine?.dispose();
      window.removeEventListener('resize', this.onResize);
    });
  }

  private readonly onResize = (): void => this.engine?.resize();

  private async initScene(): Promise<void> {
    const canvas = this.canvasRef().nativeElement;
    this.engine = await createEngine(canvas);
    this.engine.setHardwareScalingLevel(1 / window.devicePixelRatio);

    const scene = new Scene(this.engine);
    scene.clearColor = new Color4(0.04, 0.12, 0.23, 1); // #0b1e3a

    const camera = new ArcRotateCamera(
      'camera',
      -Math.PI / 2,
      Math.PI / 3.5,
      1.4,
      Vector3.Zero(),
      scene
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
      }
    );

    this.engine.runRenderLoop(() => {
      scene.render();
      this.zoomLabelRef().nativeElement.textContent = `zoom: ${camera.radius.toFixed(2)}`;
    });

    window.addEventListener('resize', this.onResize);
  }
}
