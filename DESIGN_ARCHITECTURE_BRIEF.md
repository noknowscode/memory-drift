# Infinite 3D Canvas: Design & Architecture Brief

## A Seamless, Fly-Through Image Space
**Version 2.0 — January 2026**

---

## Executive Summary

The Infinite 3D Canvas is an immersive spatial gallery where users fly through an endless three-dimensional space filled with images. Unlike traditional galleries that scroll in one or two dimensions, this experience allows navigation along all three axes—panning left/right (X), up/down (Y), and crucially, flying forward/backward through depth (Z), creating the sensation of images rushing toward and past the viewer.

The technical foundation is **chunk-based world streaming**: space is divided into 3D cubic regions, and only chunks near the camera exist at any time. Deterministic procedural generation ensures chunks can be destroyed and recreated seamlessly, creating the illusion of infinity with bounded computational cost.

**Core Experience:**
- Scroll to fly forward through images rushing at you
- Pan to explore the XY plane
- Smooth inertia-based movement with momentum
- 60fps+ performance with hundreds of visible planes
- Works on desktop (mouse/keyboard) and mobile (touch/pinch)

---

## 1. Product Definition

### 1.1 Vision Statement

Create the feeling of floating through an infinite museum in space—images suspended all around, extending forever in every direction. The user is not scrolling a list; they are *piloting* through a universe of visual content.

### 1.2 User Experience Goals

| Goal | Description |
|------|-------------|
| **Infinite exploration** | No edges, no boundaries—fly forever in any direction |
| **Spatial immersion** | Depth perception through perspective, parallax, and fading |
| **Fluid movement** | Inertia and momentum make navigation feel physical |
| **Discovery** | Serendipitous encounters with content as you explore |
| **Performance** | Buttery smooth regardless of total content volume |

### 1.3 Target Platforms

| Platform | Input Methods | Performance Target |
|----------|---------------|-------------------|
| Desktop (Chrome, Firefox, Safari, Edge) | Mouse drag, scroll wheel, keyboard | 60fps, 120fps on high-refresh |
| Tablet | Touch drag, pinch zoom | 60fps |
| Mobile | Touch drag, pinch zoom | 30-60fps |

### 1.4 User Personas

**The Explorer**
- Wants to get lost in visual content
- Values the journey over specific destinations
- Enjoys ambient, meditative browsing experiences

**The Curator**
- Building mood boards or collections
- Needs to navigate to specific areas
- May want to bookmark or save positions

**The Showcase Viewer**
- Visiting a curated exhibition
- Following a guided or suggested path
- Appreciates the presentation aesthetic

---

## 2. Interaction Design

### 2.1 Navigation Model

The camera exists in 3D world space. All navigation is camera movement; the world is static.

```
                    Y+ (up)
                      │
                      │
                      │
        X- (left) ────┼──── X+ (right)
                     /│
                    / │
                   /  │
               Z+ /   │
           (forward)  │
                    Z- (backward/behind camera)
```

### 2.2 Control Mapping

#### Desktop — Mouse & Keyboard

| Input | Action | Axis |
|-------|--------|------|
| **Scroll wheel down** | Fly forward (into screen) | +Z |
| **Scroll wheel up** | Fly backward | -Z |
| **Click + drag** | Pan camera | X, Y |
| **W / ↑** | Pan up | -Y |
| **S / ↓** | Pan down | +Y |
| **A / ←** | Pan left | -X |
| **D / →** | Pan right | +X |
| **E / Space** | Fly forward | +Z |
| **Q / Shift** | Fly backward | -Z |
| **R** | Reset to origin | All |

#### Touch Devices

| Input | Action | Axis |
|-------|--------|------|
| **Single finger drag** | Pan camera | X, Y |
| **Pinch in** | Fly forward | +Z |
| **Pinch out** | Fly backward | -Z |
| **Two finger drag** | Pan camera (alternative) | X, Y |

### 2.3 Movement Physics

Movement uses an inertia system for fluid, physical-feeling navigation:

```
┌─────────────────┐
│  User Input     │ (drag delta, key press, scroll)
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│ Target Velocity │ += input × sensitivity
└────────┬────────┘
         │
         ▼ (each frame)
┌─────────────────┐
│ Target Velocity │ ×= decay (0.94)
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│ Actual Velocity │ = lerp(actual, target, 0.08)
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│ Camera Position │ += velocity
└─────────────────┘
```

**Key Parameters:**

| Parameter | Value | Effect |
|-----------|-------|--------|
| `VELOCITY_LERP` | 0.08 | Smoothing factor (lower = more lag) |
| `VELOCITY_DECAY` | 0.94 | Friction (higher = more glide) |
| `PAN_SENSITIVITY` | 0.8 | Mouse drag responsiveness |
| `ZOOM_SENSITIVITY` | 2.5 | Scroll wheel Z-speed |
| `KEYBOARD_SPEED_XY` | 12 | Pixels per frame for WASD |
| `KEYBOARD_SPEED_Z` | 20 | Depth units per frame for Q/E |

### 2.4 Depth Perception Cues

Multiple visual techniques reinforce the 3D space:

| Technique | Implementation |
|-----------|----------------|
| **Perspective projection** | Images scale with distance (larger when close) |
| **Depth fading** | Distant images fade to transparent |
| **Near fade** | Images rushing past camera fade out |
| **Parallax rotation** | Slight 3D rotation on planes |
| **Vignette** | Darkened edges focus attention forward |
| **Motion blur hint** | Color glow intensifies with Z-velocity |

---

## 3. Visual Design

### 3.1 Aesthetic Direction

**Tone:** Cinematic, immersive, museum-in-space
**Mood:** Contemplative exploration, ambient discovery
**Reference:** Planetarium, art vault, infinite library

### 3.2 Color System

#### Dark Theme (Default)

| Token | Value | Usage |
|-------|-------|-------|
| `--bg-void` | `#050507` | Deep space background |
| `--bg-panel` | `rgba(0,0,0,0.6)` | HUD panels |
| `--border-subtle` | `rgba(255,255,255,0.06)` | Panel borders |
| `--text-primary` | `#ffffff` | Primary text |
| `--text-muted` | `rgba(255,255,255,0.45)` | Secondary text |
| `--accent` | `#7c5cff` | Primary accent (purple) |
| `--accent-glow` | `rgba(124,92,255,0.3)` | Glow effects |
| `--success` | `#4ade80` | Positive indicators (Z coord) |
| `--warning` | `#fbbf24` | Caution states |
| `--danger` | `#f87171` | Error/low FPS |

### 3.3 Typography

| Role | Font | Size | Weight |
|------|------|------|--------|
| HUD Title | Space Grotesk | 16px | 600 |
| HUD Labels | Space Grotesk | 11px | 400 |
| Data/Coords | JetBrains Mono | 11px | 400 |
| Plane Title | Space Grotesk | 11px | 500 |
| Plane Meta | JetBrains Mono | 9px | 400 |

### 3.4 Image Plane Design

```
┌─────────────────────────────┐
│                             │
│                             │
│      [ Image Content ]      │  ← Gradient overlay for depth
│                             │
│                             │
├─────────────────────────────┤
│ Title of Artwork            │  ← Semi-transparent info bar
│ Artist, Year                │     with backdrop blur
└─────────────────────────────┘

Border radius: 6px
Shadow: 0 0 30px rgba(0,0,0,0.6)
Hover shadow: + 80px purple glow
```

**Plane States:**

| State | Visual Treatment |
|-------|------------------|
| Default | Base shadow, full opacity based on depth |
| Hover | Scale 1.03, enhanced shadow, purple glow |
| Distant | Faded opacity, no interaction |
| Passing (near) | Fading out as it passes camera |

### 3.5 HUD Layout

```
┌─────────────────────────────────────────────────────────────┐
│ ∞ 3D CANVAS                                    X: 1234      │
│ Fly through infinite space                     Y: -567      │
│                                                Z: 8901 ←green│
│                                                             │
│                                                60 FPS       │
│                                                             │
│                         [ + ]  ← reticle                    │
│                                                             │
│                                                             │
│                                                             │
│                                                             │
│ ┌─────────────────┐                    ┌──────────────────┐ │
│ │ NAVIGATION      │                    │ Chunks: 63       │ │
│ │ WASD - Pan      │                    │ Planes: 147      │ │
│ │ E/Q - Fly Z     │                    │ Depth: 8901      │ │
│ │ Scroll - Fly    │                    └──────────────────┘ │
│ │ Drag - Look     │    ════════════                         │
│ └─────────────────┘    → Forward                            │
└─────────────────────────────────────────────────────────────┘
```

### 3.6 Visual Effects

| Effect | Purpose | Implementation |
|--------|---------|----------------|
| **Vignette** | Focus attention, add depth | Radial gradient overlay |
| **Motion blur hint** | Convey speed | Purple glow scales with Z velocity |
| **Center reticle** | Flight direction indicator | CSS crosshair |
| **Speed bar** | Velocity feedback | Gradient-filled progress bar |
| **Depth gradient** | Fade to void | Radial transparency mask |

---

## 4. Technical Architecture

### 4.1 Core Concept: Faking 3D Infinity

True infinite rendering is impossible. We create the illusion through:

1. **Spatial chunking** — Divide space into cubic regions
2. **Camera-relative streaming** — Only load chunks near camera
3. **Deterministic generation** — Same coordinates = same content
4. **Seamless recycling** — Destroy far chunks, create near ones

### 4.2 Coordinate Systems

```typescript
// World Space: Infinite 3D coordinates
interface WorldPosition {
  x: number;  // Left/right (negative = left)
  y: number;  // Up/down (negative = up in screen space)
  z: number;  // Depth (positive = forward/into screen)
}

// Chunk Space: Integer grid coordinates
interface ChunkCoord {
  cx: number;  // Math.floor(worldX / CHUNK_SIZE)
  cy: number;  // Math.floor(worldY / CHUNK_SIZE)
  cz: number;  // Math.floor(worldZ / CHUNK_SIZE)
}

// Screen Space: 2D pixel coordinates after projection
interface ScreenPosition {
  x: number;  // Pixels from left
  y: number;  // Pixels from top
}
```

### 4.3 Chunk System

#### Configuration

```typescript
const CONFIG = {
  CHUNK_SIZE: 600,        // World units per chunk edge
  RENDER_RADIUS_XY: 1,    // Chunks in X/Y directions
  RENDER_RADIUS_Z: 3,     // Chunks in Z direction (more depth)
  PLANES_PER_CHUNK: 4,    // Images per chunk
};
```

#### Active Chunk Calculation

```
Total active chunks = (2 × RENDER_RADIUS_XY + 1)² × (2 × RENDER_RADIUS_Z + 1)
                    = (2 × 1 + 1)² × (2 × 3 + 1)
                    = 9 × 7
                    = 63 chunks
                    
Total planes = 63 × 4 = 252 maximum
```

#### Chunk Neighborhood (Z-axis slice view)

```
Camera at chunk (0, 0, 5):

Z=2  Z=3  Z=4  Z=5  Z=6  Z=7  Z=8
 □    □    □    ■    □    □    □     ← Y = -1
 □    □    □    ■    □    □    □     ← Y = 0 (camera Y)
 □    □    □    ■    □    □    □     ← Y = 1
                ↑
           Camera Z

■ = camera chunk column
□ = loaded chunks
```

### 4.4 Deterministic Generation

Each chunk's content is generated from a hash of its coordinates:

```typescript
function generateChunkPlanes(cx: number, cy: number, cz: number): Plane[] {
  const seed = hashString(`chunk3d_${cx}_${cy}_${cz}`);
  const planes = [];
  
  for (let i = 0; i < PLANES_PER_CHUNK; i++) {
    const s = seed + i * 777;
    const r = (offset: number) => seededRandom(s + offset);
    
    planes.push({
      id: `${cx}_${cy}_${cz}_${i}`,
      localX: r(0) * CHUNK_SIZE - CHUNK_SIZE / 2,
      localY: r(1) * CHUNK_SIZE - CHUNK_SIZE / 2,
      localZ: r(2) * CHUNK_SIZE,
      width: PLANE_MIN_SIZE + r(3) * (PLANE_MAX_SIZE - PLANE_MIN_SIZE),
      height: width * (0.7 + r(4) * 0.6),
      rotationY: (r(5) - 0.5) * 20,
      rotationX: (r(6) - 0.5) * 10,
      imageIndex: Math.floor(r(7) * 1_000_000),
    });
  }
  
  return planes;
}
```

**Key Properties:**

| Property | Purpose |
|----------|---------|
| `localX/Y/Z` | Position within chunk bounds |
| `width/height` | Plane dimensions with aspect variation |
| `rotationX/Y` | Slight 3D tilt for visual interest |
| `imageIndex` | Maps to media array via modulo |

### 4.5 Perspective Projection

Convert 3D world coordinates to 2D screen coordinates:

```typescript
function projectToScreen(
  worldPos: WorldPosition,
  camera: WorldPosition,
  viewport: { width: number; height: number },
  fov: number
): ScreenPosition | null {
  // Relative position from camera
  const relX = worldPos.x - camera.x;
  const relY = worldPos.y - camera.y;
  const relZ = worldPos.z - camera.z;
  
  // Cull if behind camera or too far
  if (relZ < NEAR || relZ > FAR) return null;
  
  // Perspective projection
  const fovRad = (fov * Math.PI) / 180;
  const focalLength = viewport.height / (2 * Math.tan(fovRad / 2));
  const scale = focalLength / relZ;
  
  return {
    x: viewport.width / 2 + relX * scale,
    y: viewport.height / 2 + relY * scale,
    scale: scale,  // For sizing the plane
    depth: relZ,   // For sorting and fading
  };
}
```

### 4.6 Depth-Based Fading

```typescript
function calculateOpacity(relativeZ: number): number {
  let opacity = 1;
  
  // Far fade (distant objects)
  if (relativeZ > DEPTH_FADE_START) {
    opacity = Math.max(0, 
      1 - (relativeZ - DEPTH_FADE_START) / (DEPTH_FADE_END - DEPTH_FADE_START)
    );
  }
  
  // Near fade (objects rushing past)
  if (relativeZ < 100) {
    opacity *= relativeZ / 100;
  }
  
  return opacity;
}
```

**Fade Zones:**

```
Camera                                              Far Clip
  │                                                    │
  │  Near    │        Full Opacity        │   Fade    │
  │  Fade    │                            │   Out     │
  │◄──100───►│◄────── 300 units ─────────►│◄──800───►│
  0         100                          400        1200
  
  Opacity:  0→1          1.0              1.0→0
```

### 4.7 Render Pipeline

```
┌──────────────────────────────────────────────────────────────┐
│                      Each Animation Frame                     │
└──────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌──────────────────────────────────────────────────────────────┐
│ 1. Process Input                                              │
│    • Accumulate keyboard state → target velocity              │
│    • Apply scroll accumulator → Z velocity                    │
└──────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌──────────────────────────────────────────────────────────────┐
│ 2. Update Physics                                             │
│    • Decay target velocity (× 0.94)                          │
│    • Lerp actual velocity toward target (0.08)               │
│    • Integrate position (pos += velocity)                    │
└──────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌──────────────────────────────────────────────────────────────┐
│ 3. Determine Active Chunks                                    │
│    • Calculate camera chunk coords                           │
│    • Generate 63-chunk neighborhood                          │
│    • Fetch/generate planes for each chunk (cached)           │
└──────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌──────────────────────────────────────────────────────────────┐
│ 4. Project & Cull                                             │
│    • For each plane in active chunks:                        │
│      - Calculate relative position                           │
│      - Skip if behind camera (relZ < NEAR)                   │
│      - Skip if too far (relZ > FAR)                          │
│      - Project to screen coordinates                         │
│      - Skip if outside viewport (frustum cull)               │
│      - Calculate opacity from depth                          │
│      - Skip if opacity < 0.02                                │
└──────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌──────────────────────────────────────────────────────────────┐
│ 5. Sort & Render                                              │
│    • Sort visible planes by depth (far to near)              │
│    • Render each plane with calculated transforms            │
│    • Apply CSS: position, size, opacity, 3D rotation         │
└──────────────────────────────────────────────────────────────┘
```

### 4.8 Data Structures

```typescript
// Camera state
interface CameraState {
  position: { x: number; y: number; z: number };
  velocity: { x: number; y: number; z: number };
  targetVelocity: { x: number; y: number; z: number };
}

// Chunk definition
interface Chunk {
  cx: number;
  cy: number;
  cz: number;
  key: string;  // "${cx},${cy},${cz}"
  planes: Plane[];
}

// Plane definition (world space)
interface Plane {
  id: string;
  localX: number;
  localY: number;
  localZ: number;
  width: number;
  height: number;
  rotationX: number;
  rotationY: number;
  imageIndex: number;
}

// Projected plane (screen space)
interface ProjectedPlane extends Plane {
  screenX: number;
  screenY: number;
  screenSize: number;
  screenHeight: number;
  depth: number;
  opacity: number;
  transform3d: string;
  artwork: ArtworkData;
}

// Media item
interface ArtworkData {
  title: string;
  artist: string;
  year: number;
  hue: number;  // For placeholder color
  src?: string;  // Image URL when available
}
```

### 4.9 Caching Strategy

```typescript
// LRU cache for generated chunk planes
const chunkCache = new Map<string, Plane[]>();
const MAX_CACHE_SIZE = 200;

function getChunkPlanes(cx: number, cy: number, cz: number): Plane[] {
  const key = `${cx},${cy},${cz}`;
  
  if (chunkCache.has(key)) {
    // Move to end (most recently used)
    const planes = chunkCache.get(key)!;
    chunkCache.delete(key);
    chunkCache.set(key, planes);
    return planes;
  }
  
  // Generate new chunk
  const planes = generateChunkPlanes(cx, cy, cz);
  chunkCache.set(key, planes);
  
  // Evict oldest if over limit
  while (chunkCache.size > MAX_CACHE_SIZE) {
    const oldestKey = chunkCache.keys().next().value;
    chunkCache.delete(oldestKey);
  }
  
  return planes;
}
```

---

## 5. Performance

### 5.1 Targets

| Metric | Target | Critical Threshold |
|--------|--------|-------------------|
| Frame rate (desktop) | 60fps | 30fps |
| Frame rate (mobile) | 30fps | 20fps |
| Frame rate (120Hz displays) | 120fps | 60fps |
| Input latency | <16ms | <33ms |
| Chunk generation time | <5ms | <16ms |
| Memory usage | <200MB | <500MB |

### 5.2 Optimization Techniques

#### Frustum Culling
Skip planes outside the visible viewport:

```typescript
const margin = screenSize;
if (screenX < -margin || screenX > viewport.width + margin) continue;
if (screenY < -margin || screenY > viewport.height + margin) continue;
```

#### Depth Culling
Skip planes too close or too far:

```typescript
if (relZ < NEAR || relZ > FAR) continue;
if (opacity < 0.02) continue;
```

#### CSS Performance
Use transform-friendly properties:

```css
.plane-3d {
  will-change: transform, opacity;
  backface-visibility: hidden;
  transform: translate(-50%, -50%) perspective(...) rotateY(...) rotateX(...);
}
```

#### Scroll Accumulator
Smooth scroll input over multiple frames:

```typescript
// On scroll event
scrollAccumRef.current += deltaY * sensitivity;

// Each frame
const scrollZ = scrollAccumRef.current;
scrollAccumRef.current *= 0.85;  // Decay
targetVelocity.z += scrollZ;
```

#### Memoization
Cache expensive calculations:

```typescript
const visiblePlanes = useMemo(() => {
  // ... projection and culling logic
}, [camera.x, camera.y, camera.z, viewport.width, viewport.height]);
```

### 5.3 Performance Monitoring

Track and display real-time metrics:

```typescript
// FPS counter
const fpsRef = useRef({ count: 0, lastTime: performance.now() });

// In animation loop
fpsRef.current.count++;
const now = performance.now();
if (now - fpsRef.current.lastTime >= 1000) {
  setFps(fpsRef.current.count);
  fpsRef.current.count = 0;
  fpsRef.current.lastTime = now;
}
```

---

## 6. Future Enhancements

### 6.1 Click-to-Focus

Navigate to a specific image:

```typescript
function focusOnPlane(plane: Plane) {
  const targetZ = plane.worldZ - 200;  // Stop 200 units in front
  animateCameraTo({
    x: plane.worldX,
    y: plane.worldY,
    z: targetZ,
  }, { duration: 1000, easing: 'easeOutCubic' });
}
```

### 6.2 Dynamic Content Loading

Fetch real images based on chunk position:

```typescript
async function fetchChunkMedia(cx: number, cy: number, cz: number) {
  const response = await fetch(
    `/api/images?chunk=${cx},${cy},${cz}&limit=${PLANES_PER_CHUNK}`
  );
  return response.json();
}
```

### 6.3 Texture LOD (Level of Detail)

Load thumbnail vs full resolution based on distance:

```typescript
const textureUrl = depth > LOD_THRESHOLD
  ? artwork.thumbnailUrl   // Small, fast-loading
  : artwork.fullUrl;       // High resolution
```

### 6.4 WebGL Upgrade Path

For 10,000+ images, migrate to React Three Fiber:

```typescript
// React Three Fiber version
<Canvas camera={{ fov: 60, near: 0.1, far: 3000 }}>
  <ChunkManager camera={camera}>
    {visiblePlanes.map(plane => (
      <ImagePlane key={plane.id} {...plane} />
    ))}
  </ChunkManager>
</Canvas>
```

### 6.5 Collision-Free Placement

Use Poisson disk sampling for non-overlapping layouts:

```typescript
import PoissonDiskSampling from 'poisson-disk-sampling';

function generateChunkPlanes(cx, cy, cz) {
  const pds = new PoissonDiskSampling({
    shape: [CHUNK_SIZE, CHUNK_SIZE, CHUNK_SIZE],
    minDistance: 150,
    maxDistance: 300,
    tries: 30,
  });
  
  return pds.fill().map((point, i) => ({
    localX: point[0] - CHUNK_SIZE / 2,
    localY: point[1] - CHUNK_SIZE / 2,
    localZ: point[2],
    // ... other properties
  }));
}
```

### 6.6 Audio Integration

Ambient soundscape that responds to movement:

```typescript
// Speed affects audio
const speed = Math.sqrt(vel.x² + vel.y² + vel.z²);
ambientAudio.setPlaybackRate(0.8 + speed * 0.01);
ambientAudio.setVolume(0.3 + Math.min(speed * 0.02, 0.5));
```

---

## 7. Configuration Reference

### 7.1 Full Configuration Object

```typescript
const CONFIG = {
  // ═══════════════════════════════════════════
  // CHUNK SYSTEM
  // ═══════════════════════════════════════════
  CHUNK_SIZE: 600,           // World units per chunk
  RENDER_RADIUS_XY: 1,       // Horizontal chunk radius
  RENDER_RADIUS_Z: 3,        // Depth chunk radius
  PLANES_PER_CHUNK: 4,       // Images per chunk
  MAX_CACHE_SIZE: 200,       // LRU cache limit
  
  // ═══════════════════════════════════════════
  // CAMERA
  // ═══════════════════════════════════════════
  FOV: 60,                   // Field of view (degrees)
  NEAR: 10,                  // Near clipping plane
  FAR: 3000,                 // Far clipping plane
  
  // ═══════════════════════════════════════════
  // MOVEMENT PHYSICS
  // ═══════════════════════════════════════════
  VELOCITY_LERP: 0.08,       // Smoothing (0-1, lower = smoother)
  VELOCITY_DECAY: 0.94,      // Friction (0-1, higher = more glide)
  PAN_SENSITIVITY: 0.8,      // Mouse drag multiplier
  ZOOM_SENSITIVITY: 2.5,     // Scroll wheel Z-speed
  KEYBOARD_SPEED_XY: 12,     // Arrow/WASD speed
  KEYBOARD_SPEED_Z: 20,      // Q/E fly speed
  
  // ═══════════════════════════════════════════
  // VISUALS
  // ═══════════════════════════════════════════
  PLANE_MIN_SIZE: 120,       // Minimum plane dimension
  PLANE_MAX_SIZE: 220,       // Maximum plane dimension
  DEPTH_FADE_START: 400,     // Z distance where fade begins
  DEPTH_FADE_END: 1200,      // Z distance where fully transparent
  NEAR_FADE_DISTANCE: 100,   // Distance for near-camera fade
  OPACITY_THRESHOLD: 0.02,   // Below this, don't render
};
```

### 7.2 Tuning Guide

| Want to... | Adjust |
|------------|--------|
| See more depth | Increase `RENDER_RADIUS_Z`, `FAR` |
| Smoother movement | Decrease `VELOCITY_LERP`, increase `VELOCITY_DECAY` |
| Snappier response | Increase `VELOCITY_LERP`, decrease `VELOCITY_DECAY` |
| More images | Increase `PLANES_PER_CHUNK` |
| Better performance | Decrease `RENDER_RADIUS_*`, `PLANES_PER_CHUNK` |
| Wider view | Increase `FOV` |
| Longer fade | Increase `DEPTH_FADE_END - DEPTH_FADE_START` |

---

## 8. File Structure

```
src/
├── components/
│   └── Infinite3DCanvas/
│       ├── index.tsx              # Main component & lazy loader
│       ├── Canvas3D.tsx           # Viewport and event handling
│       ├── PlaneRenderer.tsx      # Individual plane component
│       ├── HUD.tsx                # Overlay UI components
│       ├── config.ts              # Configuration constants
│       ├── physics.ts             # Velocity and movement logic
│       ├── projection.ts          # 3D to 2D math
│       ├── chunks.ts              # Chunk generation & caching
│       ├── types.ts               # TypeScript interfaces
│       └── styles.module.css      # Component styles
├── data/
│   └── artworks.ts                # Demo artwork metadata
├── hooks/
│   ├── useAnimationFrame.ts       # RAF loop hook
│   ├── useKeyboard.ts             # Keyboard input tracking
│   └── usePointer.ts              # Mouse/touch handling
├── utils/
│   ├── hash.ts                    # Deterministic hashing
│   ├── random.ts                  # Seeded random functions
│   └── math.ts                    # lerp, clamp, etc.
├── App.tsx
└── main.tsx
```

---

## 9. References

### Inspiration
- [Codrops: Infinite Canvas Tutorial](https://tympanus.net/codrops/2026/01/07/infinite-canvas-building-a-seamless-pan-anywhere-image-space/) by Edoardo Lunardi
- [Art Institute of Chicago Open Access API](https://www.artic.edu/open-access/public-api)

### Technologies
- [React](https://react.dev/) — UI framework
- [React Three Fiber](https://docs.pmnd.rs/react-three-fiber) — For WebGL upgrade path
- [Three.js](https://threejs.org/) — 3D graphics library
- [Vite](https://vitejs.dev/) — Build tool

### Further Reading
- [Spatial Hashing for Broad-Phase Collision Detection](https://www.gamedev.net/tutorials/programming/general-and-gameplay-programming/spatial-hashing-r2697/)
- [Frustum Culling](https://learnopengl.com/Guest-Articles/2021/Scene/Frustum-Culling)
- [CSS 3D Transforms](https://developer.mozilla.org/en-US/docs/Web/CSS/CSS_Transforms/Using_CSS_transforms)

---

*Document Version 2.0 — January 2026*
*For the Infinite 3D Canvas: Fly-Through Image Space*
