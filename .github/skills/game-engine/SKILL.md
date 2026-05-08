---
name: game-engine
description: 'Expert skill for building web-based game engines and games using HTML5, Canvas, WebGL, and JavaScript. Use when asked to create games, build game engines, implement game physics, handle collision detection, set up game loops, manage sprites, add game controls, or work with 2D/3D rendering. Covers techniques for platformers, breakout-style games, maze games, tilemaps, audio, multiplayer via WebRTC, and publishing games.'
---

# Game Engine Skill

Build web-based games and game engines using HTML5 Canvas, WebGL, and JavaScript. This skill includes starter templates, reference documentation, and step-by-step workflows for 2D and 3D game development with frameworks such as Phaser, Three.js, Babylon.js, and A-Frame.

## When to Use This Skill

- Building a game engine or game from scratch using web technologies
- Implementing game loops, physics, collision detection, or rendering
- Working with HTML5 Canvas, WebGL, or SVG for game graphics
- Adding game controls (keyboard, mouse, touch, gamepad)
- Creating 2D platformers, breakout-style games, maze games, or 3D experiences
- Working with tilemaps, sprites, or animations
- Adding audio to web games
- Implementing multiplayer features with WebRTC or WebSockets
- Optimizing game performance
- Publishing and distributing web games

## Prerequisites

- Basic knowledge of HTML, CSS, and JavaScript
- A modern web browser with Canvas/WebGL support
- A text editor or IDE
- Optional: Node.js for build tooling and local development servers

## Core Concepts

### Game Loop

Every game engine revolves around the game loop -- a continuous cycle of:

1. **Process Input** - Read keyboard, mouse, touch, or gamepad input
2. **Update State** - Update game object positions, physics, AI, and logic
3. **Render** - Draw the current game state to the screen

Use `requestAnimationFrame` for smooth, browser-optimized rendering.

### Rendering

- **Canvas 2D** - Best for 2D games, sprite-based rendering, and tilemaps
- **WebGL** - Hardware-accelerated 3D and advanced 2D rendering
- **SVG** - Vector-based graphics, good for UI elements
- **CSS** - Useful for DOM-based game elements and transitions

### Physics and Collision Detection

- **2D Collision Detection** - AABB, circle, and SAT-based collision
- **3D Collision Detection** - Bounding box, bounding sphere, and raycasting
- **Velocity and Acceleration** - Basic Newtonian physics for movement
- **Gravity** - Constant downward acceleration for platformers

### Controls

- **Keyboard** - Arrow keys, WASD, and custom key bindings
- **Mouse** - Click, move, and pointer lock for FPS-style controls
- **Touch** - Mobile touch events and virtual joysticks
- **Gamepad** - Gamepad API for controller support

### Audio

- **Web Audio API** - Programmatic sound generation and spatial audio
- **HTML5 Audio** - Simple audio playback for music and sound effects

## Step-by-Step Workflows

### Building a 3D Game

1. Choose a framework (Three.js, Babylon.js, A-Frame, or PlayCanvas)
2. Set up the scene, camera, and renderer
3. Load or create 3D models and textures
4. Implement lighting and shaders
5. Add physics and collision detection
6. Implement player controls and camera movement
7. Add audio and visual effects

### Publishing a Game

1. Optimize assets (compress images, minify code)
2. Test across browsers and devices
3. Choose distribution platform (web, app stores, game portals)
4. Implement monetization if needed
5. Promote through game communities and social media

## Troubleshooting

| Issue | Solution |
|-------|----------|
| Canvas is blank | Check that you are calling drawing methods after getting the context and inside the game loop |
| Game runs at different speeds | Use delta time in update calculations instead of fixed values |
| Collision detection is inconsistent | Use continuous collision detection or reduce time steps for fast-moving objects |
| Audio does not play | Browsers require user interaction before playing audio; trigger playback from a click handler |
| Performance is poor | Profile with browser dev tools, reduce draw calls, use object pooling, and optimize asset sizes |
| WebGL context lost | Handle the `webglcontextlost` event and restore state on `webglcontextrestored` |
