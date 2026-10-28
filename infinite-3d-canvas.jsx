import React, { useRef, useState, useEffect, useMemo, useCallback } from 'react';

// ============================================================================
// CONFIGURATION
// ============================================================================
const CONFIG = {
  // Chunk system (3D)
  CHUNK_SIZE: 600,
  RENDER_RADIUS_XY: 1,        // Chunks around camera in X/Y
  RENDER_RADIUS_Z: 3,         // Chunks ahead/behind in Z
  PLANES_PER_CHUNK: 4,
  
  // Camera
  FOV: 60,                    // Field of view in degrees
  NEAR: 10,
  FAR: 3000,
  
  // Movement
  VELOCITY_LERP: 0.08,
  VELOCITY_DECAY: 0.94,
  PAN_SENSITIVITY: 0.8,
  ZOOM_SENSITIVITY: 2.5,      // Z movement speed
  KEYBOARD_SPEED_XY: 12,
  KEYBOARD_SPEED_Z: 20,
  
  // Visuals
  PLANE_MIN_SIZE: 120,
  PLANE_MAX_SIZE: 220,
  DEPTH_FADE_START: 400,      // Z distance where fade begins
  DEPTH_FADE_END: 1200,       // Z distance where fully faded
};

// ============================================================================
// UTILITIES
// ============================================================================
function hashString(str) {
  let hash = 0;
  for (let i = 0; i < str.length; i++) {
    hash = ((hash << 5) - hash) + str.charCodeAt(i);
    hash = hash & hash;
  }
  return Math.abs(hash);
}

function seededRandom(seed) {
  const x = Math.sin(seed * 9999) * 10000;
  return x - Math.floor(x);
}

function lerp(a, b, t) {
  return a + (b - a) * t;
}

// ============================================================================
// 3D CHUNK GENERATION
// ============================================================================
function generateChunkPlanes(cx, cy, cz) {
  const planes = [];
  const seed = hashString(`chunk3d_${cx}_${cy}_${cz}`);
  
  for (let i = 0; i < CONFIG.PLANES_PER_CHUNK; i++) {
    const s = seed + i * 777;
    const r = (offset) => seededRandom(s + offset);
    
    const size = CONFIG.PLANE_MIN_SIZE + r(1) * (CONFIG.PLANE_MAX_SIZE - CONFIG.PLANE_MIN_SIZE);
    const aspectRatio = 0.7 + r(2) * 0.6;
    
    planes.push({
      id: `${cx}_${cy}_${cz}_${i}`,
      // 3D position within chunk
      localX: r(3) * CONFIG.CHUNK_SIZE - CONFIG.CHUNK_SIZE / 2,
      localY: r(4) * CONFIG.CHUNK_SIZE - CONFIG.CHUNK_SIZE / 2,
      localZ: r(5) * CONFIG.CHUNK_SIZE,
      width: size,
      height: size * aspectRatio,
      rotationY: (r(6) - 0.5) * 20,  // Slight Y rotation
      rotationX: (r(7) - 0.5) * 10,  // Slight X tilt
      imageIndex: Math.floor(r(8) * 1000000),
    });
  }
  
  return planes;
}

const chunkCache = new Map();
function getChunkPlanes(cx, cy, cz) {
  const key = `${cx},${cy},${cz}`;
  if (!chunkCache.has(key)) {
    chunkCache.set(key, generateChunkPlanes(cx, cy, cz));
    if (chunkCache.size > 200) {
      const firstKey = chunkCache.keys().next().value;
      chunkCache.delete(firstKey);
    }
  }
  return chunkCache.get(key);
}

// ============================================================================
// ARTWORK DATA
// ============================================================================
const ARTWORKS = [
  { title: "The Night Watch", artist: "Rembrandt", year: 1642, hue: 35 },
  { title: "Girl with a Pearl Earring", artist: "Vermeer", year: 1665, hue: 195 },
  { title: "The Calling of St Matthew", artist: "Caravaggio", year: 1600, hue: 28 },
  { title: "Las Meninas", artist: "Velázquez", year: 1656, hue: 42 },
  { title: "The Garden of Earthly Delights", artist: "Bosch", year: 1510, hue: 95 },
  { title: "Judith Slaying Holofernes", artist: "Gentileschi", year: 1620, hue: 5 },
  { title: "The Anatomy Lesson", artist: "Rembrandt", year: 1632, hue: 32 },
  { title: "The Milkmaid", artist: "Vermeer", year: 1658, hue: 48 },
  { title: "Bacchus", artist: "Caravaggio", year: 1598, hue: 18 },
  { title: "The Rokeby Venus", artist: "Velázquez", year: 1650, hue: 345 },
  { title: "Saturn Devouring His Son", artist: "Goya", year: 1823, hue: 22 },
  { title: "Self Portrait", artist: "Dürer", year: 1500, hue: 38 },
  { title: "The Birth of Venus", artist: "Botticelli", year: 1485, hue: 180 },
  { title: "The Last Supper", artist: "da Vinci", year: 1498, hue: 45 },
  { title: "The Creation of Adam", artist: "Michelangelo", year: 1512, hue: 25 },
];

// ============================================================================
// MAIN COMPONENT
// ============================================================================
export default function Infinite3DCanvas() {
  // 3D Camera position
  const [camera, setCamera] = useState({ x: 0, y: 0, z: 0 });
  const [velocity, setVelocity] = useState({ x: 0, y: 0, z: 0 });
  const [targetVel, setTargetVel] = useState({ x: 0, y: 0, z: 0 });
  
  // Interaction
  const [isDragging, setIsDragging] = useState(false);
  const [lastPointer, setLastPointer] = useState({ x: 0, y: 0 });
  const [pinchDistance, setPinchDistance] = useState(null);
  
  // Viewport
  const [viewport, setViewport] = useState({ width: 800, height: 600 });
  const containerRef = useRef(null);
  
  // Input tracking
  const keysRef = useRef(new Set());
  const scrollAccumRef = useRef(0);
  
  // Stats
  const [stats, setStats] = useState({ fps: 60, chunks: 0, planes: 0 });
  const fpsRef = useRef({ count: 0, lastTime: performance.now() });

  // Viewport sizing
  useEffect(() => {
    const updateSize = () => {
      if (containerRef.current) {
        setViewport({
          width: containerRef.current.clientWidth,
          height: containerRef.current.clientHeight,
        });
      }
    };
    updateSize();
    window.addEventListener('resize', updateSize);
    return () => window.removeEventListener('resize', updateSize);
  }, []);

  // Keyboard
  useEffect(() => {
    const onKeyDown = (e) => {
      if (e.target.tagName === 'INPUT') return;
      keysRef.current.add(e.key.toLowerCase());
      if (e.key.toLowerCase() === 'r') {
        setCamera({ x: 0, y: 0, z: 0 });
        setVelocity({ x: 0, y: 0, z: 0 });
        setTargetVel({ x: 0, y: 0, z: 0 });
      }
    };
    const onKeyUp = (e) => keysRef.current.delete(e.key.toLowerCase());
    
    window.addEventListener('keydown', onKeyDown);
    window.addEventListener('keyup', onKeyUp);
    return () => {
      window.removeEventListener('keydown', onKeyDown);
      window.removeEventListener('keyup', onKeyUp);
    };
  }, []);

  // Animation loop
  useEffect(() => {
    let frameId;
    
    const animate = () => {
      // FPS
      fpsRef.current.count++;
      const now = performance.now();
      if (now - fpsRef.current.lastTime >= 1000) {
        setStats(s => ({ ...s, fps: fpsRef.current.count }));
        fpsRef.current.count = 0;
        fpsRef.current.lastTime = now;
      }
      
      // Keyboard input
      const keys = keysRef.current;
      let kx = 0, ky = 0, kz = 0;
      
      if (keys.has('a') || keys.has('arrowleft')) kx -= CONFIG.KEYBOARD_SPEED_XY;
      if (keys.has('d') || keys.has('arrowright')) kx += CONFIG.KEYBOARD_SPEED_XY;
      if (keys.has('w') || keys.has('arrowup')) ky -= CONFIG.KEYBOARD_SPEED_XY;
      if (keys.has('s') || keys.has('arrowdown')) ky += CONFIG.KEYBOARD_SPEED_XY;
      if (keys.has('q') || keys.has('shift')) kz -= CONFIG.KEYBOARD_SPEED_Z;  // Backward
      if (keys.has('e') || keys.has(' ')) kz += CONFIG.KEYBOARD_SPEED_Z;      // Forward
      
      // Apply scroll accumulator for smooth Z movement
      const scrollZ = scrollAccumRef.current;
      scrollAccumRef.current *= 0.85;
      
      setTargetVel(v => ({
        x: (v.x + kx) * CONFIG.VELOCITY_DECAY,
        y: (v.y + ky) * CONFIG.VELOCITY_DECAY,
        z: (v.z + kz + scrollZ) * CONFIG.VELOCITY_DECAY,
      }));
      
      setVelocity(v => ({
        x: lerp(v.x, targetVel.x, CONFIG.VELOCITY_LERP),
        y: lerp(v.y, targetVel.y, CONFIG.VELOCITY_LERP),
        z: lerp(v.z, targetVel.z, CONFIG.VELOCITY_LERP),
      }));
      
      setCamera(c => ({
        x: c.x + velocity.x,
        y: c.y + velocity.y,
        z: c.z + velocity.z,
      }));
      
      frameId = requestAnimationFrame(animate);
    };
    
    frameId = requestAnimationFrame(animate);
    return () => cancelAnimationFrame(frameId);
  }, [velocity, targetVel]);

  // Pointer handlers
  const handlePointerDown = useCallback((e) => {
    setIsDragging(true);
    setLastPointer({ x: e.clientX, y: e.clientY });
  }, []);

  const handlePointerMove = useCallback((e) => {
    if (!isDragging) return;
    
    const dx = e.clientX - lastPointer.x;
    const dy = e.clientY - lastPointer.y;
    
    setTargetVel(v => ({
      ...v,
      x: v.x - dx * CONFIG.PAN_SENSITIVITY,
      y: v.y - dy * CONFIG.PAN_SENSITIVITY,
    }));
    
    setLastPointer({ x: e.clientX, y: e.clientY });
  }, [isDragging, lastPointer]);

  const handlePointerUp = useCallback(() => {
    setIsDragging(false);
  }, []);

  // Wheel = Z movement (flying through)
  const handleWheel = useCallback((e) => {
    e.preventDefault();
    // Scroll down = move forward (into screen), scroll up = move backward
    scrollAccumRef.current += e.deltaY * CONFIG.ZOOM_SENSITIVITY * 0.1;
  }, []);

  // Touch pinch for Z
  const getTouchDistance = (touches) => {
    if (touches.length < 2) return null;
    const dx = touches[0].clientX - touches[1].clientX;
    const dy = touches[0].clientY - touches[1].clientY;
    return Math.sqrt(dx * dx + dy * dy);
  };

  const handleTouchStart = useCallback((e) => {
    if (e.touches.length === 2) {
      setPinchDistance(getTouchDistance(e.touches));
    } else if (e.touches.length === 1) {
      setIsDragging(true);
      setLastPointer({ x: e.touches[0].clientX, y: e.touches[0].clientY });
    }
  }, []);

  const handleTouchMove = useCallback((e) => {
    e.preventDefault();
    
    if (e.touches.length === 2 && pinchDistance !== null) {
      const newDist = getTouchDistance(e.touches);
      const delta = (pinchDistance - newDist) * 0.5;
      scrollAccumRef.current += delta;
      setPinchDistance(newDist);
    } else if (e.touches.length === 1 && isDragging) {
      const dx = e.touches[0].clientX - lastPointer.x;
      const dy = e.touches[0].clientY - lastPointer.y;
      
      setTargetVel(v => ({
        ...v,
        x: v.x - dx * CONFIG.PAN_SENSITIVITY * 0.5,
        y: v.y - dy * CONFIG.PAN_SENSITIVITY * 0.5,
      }));
      
      setLastPointer({ x: e.touches[0].clientX, y: e.touches[0].clientY });
    }
  }, [isDragging, lastPointer, pinchDistance]);

  const handleTouchEnd = useCallback(() => {
    setIsDragging(false);
    setPinchDistance(null);
  }, []);

  // 3D Projection & visible planes calculation
  const visiblePlanes = useMemo(() => {
    const planes = [];
    const fovRad = (CONFIG.FOV * Math.PI) / 180;
    const focalLength = viewport.height / (2 * Math.tan(fovRad / 2));
    
    // Current chunk coordinates
    const camChunkX = Math.floor(camera.x / CONFIG.CHUNK_SIZE);
    const camChunkY = Math.floor(camera.y / CONFIG.CHUNK_SIZE);
    const camChunkZ = Math.floor(camera.z / CONFIG.CHUNK_SIZE);
    
    let chunkCount = 0;
    
    // Iterate through nearby chunks
    for (let dz = -CONFIG.RENDER_RADIUS_Z; dz <= CONFIG.RENDER_RADIUS_Z; dz++) {
      for (let dx = -CONFIG.RENDER_RADIUS_XY; dx <= CONFIG.RENDER_RADIUS_XY; dx++) {
        for (let dy = -CONFIG.RENDER_RADIUS_XY; dy <= CONFIG.RENDER_RADIUS_XY; dy++) {
          const cx = camChunkX + dx;
          const cy = camChunkY + dy;
          const cz = camChunkZ + dz;
          
          chunkCount++;
          const chunkPlanes = getChunkPlanes(cx, cy, cz);
          
          chunkPlanes.forEach(plane => {
            // World position
            const worldX = cx * CONFIG.CHUNK_SIZE + plane.localX;
            const worldY = cy * CONFIG.CHUNK_SIZE + plane.localY;
            const worldZ = cz * CONFIG.CHUNK_SIZE + plane.localZ;
            
            // Relative to camera
            const relX = worldX - camera.x;
            const relY = worldY - camera.y;
            const relZ = worldZ - camera.z;
            
            // Only render planes in front of camera
            if (relZ < CONFIG.NEAR || relZ > CONFIG.FAR) return;
            
            // Perspective projection
            const scale = focalLength / relZ;
            const screenX = viewport.width / 2 + relX * scale;
            const screenY = viewport.height / 2 + relY * scale;
            const screenSize = plane.width * scale;
            
            // Frustum culling (with margin)
            const margin = screenSize;
            if (screenX < -margin || screenX > viewport.width + margin) return;
            if (screenY < -margin || screenY > viewport.height + margin) return;
            
            // Depth-based opacity
            let opacity = 1;
            if (relZ > CONFIG.DEPTH_FADE_START) {
              opacity = Math.max(0, 1 - (relZ - CONFIG.DEPTH_FADE_START) / (CONFIG.DEPTH_FADE_END - CONFIG.DEPTH_FADE_START));
            }
            // Also fade things very close (rushing past effect)
            if (relZ < 100) {
              opacity *= relZ / 100;
            }
            
            if (opacity < 0.02) return;
            
            const artwork = ARTWORKS[plane.imageIndex % ARTWORKS.length];
            
            planes.push({
              ...plane,
              screenX,
              screenY,
              screenSize,
              screenHeight: plane.height * scale,
              depth: relZ,
              opacity,
              artwork,
              // 3D rotation transforms
              transform3d: `
                perspective(${focalLength}px)
                rotateY(${plane.rotationY * (1 - relZ / CONFIG.FAR)}deg)
                rotateX(${plane.rotationX * (1 - relZ / CONFIG.FAR)}deg)
              `,
            });
          });
        }
      }
    }
    
    // Sort by depth (far to near for proper painter's algorithm)
    planes.sort((a, b) => b.depth - a.depth);
    
    setStats(s => ({ ...s, chunks: chunkCount, planes: planes.length }));
    
    return planes;
  }, [camera.x, camera.y, camera.z, viewport.width, viewport.height]);

  // Speed indicator
  const speed = Math.sqrt(velocity.x ** 2 + velocity.y ** 2 + velocity.z ** 2);
  const zSpeed = Math.abs(velocity.z);

  return (
    <div className="infinite-3d-app">
      <style>{`
        @import url('https://fonts.googleapis.com/css2?family=Space+Grotesk:wght@400;500;600&family=JetBrains+Mono:wght@400&display=swap');
        
        * { margin: 0; padding: 0; box-sizing: border-box; }
        
        .infinite-3d-app {
          width: 100vw;
          height: 100vh;
          background: #050507;
          overflow: hidden;
          font-family: 'Space Grotesk', sans-serif;
          color: #fff;
        }
        
        .canvas-3d {
          width: 100%;
          height: 100%;
          position: relative;
          cursor: ${isDragging ? 'grabbing' : 'grab'};
          overflow: hidden;
          perspective: 1000px;
        }
        
        /* Tunnel/depth lines effect */
        .depth-lines {
          position: absolute;
          inset: 0;
          overflow: hidden;
          pointer-events: none;
          opacity: 0.15;
        }
        
        .depth-line {
          position: absolute;
          background: linear-gradient(90deg, transparent, rgba(124,92,255,0.5), transparent);
          height: 1px;
          transform-origin: center;
        }
        
        /* Radial depth gradient */
        .depth-gradient {
          position: absolute;
          inset: 0;
          background: radial-gradient(
            ellipse at center,
            transparent 0%,
            transparent 30%,
            rgba(0,0,0,0.8) 100%
          );
          pointer-events: none;
        }
        
        /* 3D Plane styling */
        .plane-3d {
          position: absolute;
          transform-origin: center center;
          border-radius: 6px;
          overflow: hidden;
          box-shadow: 0 0 30px rgba(0,0,0,0.6);
          transition: box-shadow 0.2s ease;
          cursor: pointer;
          will-change: transform, opacity;
          backface-visibility: hidden;
        }
        
        .plane-3d:hover {
          box-shadow: 
            0 0 50px rgba(0,0,0,0.8),
            0 0 0 2px rgba(255,255,255,0.2),
            0 0 80px rgba(124, 92, 255, 0.3);
        }
        
        .plane-inner {
          width: 100%;
          height: 100%;
          display: flex;
          flex-direction: column;
        }
        
        .plane-image {
          flex: 1;
          display: flex;
          align-items: center;
          justify-content: center;
          position: relative;
        }
        
        .plane-image::before {
          content: '';
          position: absolute;
          inset: 0;
          background: linear-gradient(
            180deg,
            rgba(255,255,255,0.08) 0%,
            transparent 40%,
            rgba(0,0,0,0.3) 100%
          );
        }
        
        .plane-icon {
          font-size: 36px;
          opacity: 0.15;
        }
        
        .plane-info {
          padding: 10px 12px;
          background: rgba(0,0,0,0.75);
          backdrop-filter: blur(8px);
        }
        
        .plane-title {
          font-size: 11px;
          font-weight: 500;
          white-space: nowrap;
          overflow: hidden;
          text-overflow: ellipsis;
          margin-bottom: 2px;
        }
        
        .plane-meta {
          font-size: 9px;
          opacity: 0.5;
          font-family: 'JetBrains Mono', monospace;
        }
        
        /* HUD */
        .hud {
          position: fixed;
          inset: 0;
          pointer-events: none;
          z-index: 100;
        }
        
        .hud-panel {
          position: absolute;
          pointer-events: auto;
        }
        
        .hud-top-left {
          top: 24px;
          left: 24px;
        }
        
        .hud-title {
          font-size: 16px;
          font-weight: 600;
          letter-spacing: 3px;
          text-transform: uppercase;
          margin-bottom: 6px;
          background: linear-gradient(90deg, #fff, #a78bfa);
          -webkit-background-clip: text;
          -webkit-text-fill-color: transparent;
          background-clip: text;
        }
        
        .hud-subtitle {
          font-size: 11px;
          opacity: 0.4;
        }
        
        .hud-top-right {
          top: 24px;
          right: 24px;
          text-align: right;
        }
        
        .coords-panel {
          background: rgba(0,0,0,0.5);
          backdrop-filter: blur(10px);
          border: 1px solid rgba(255,255,255,0.08);
          border-radius: 10px;
          padding: 14px 18px;
          font-family: 'JetBrains Mono', monospace;
          font-size: 11px;
          line-height: 1.8;
        }
        
        .coord-label {
          opacity: 0.4;
          margin-right: 8px;
        }
        
        .coord-value {
          color: #a78bfa;
        }
        
        .coord-value.z-value {
          color: #4ade80;
          font-weight: 500;
        }
        
        .hud-fps {
          margin-top: 12px;
          padding: 6px 12px;
          background: rgba(255,255,255,0.05);
          border-radius: 6px;
          font-size: 11px;
        }
        
        .fps-good { color: #4ade80; }
        .fps-ok { color: #fbbf24; }
        .fps-bad { color: #f87171; }
        
        .hud-bottom-left {
          bottom: 24px;
          left: 24px;
        }
        
        .controls-panel {
          background: rgba(0,0,0,0.6);
          backdrop-filter: blur(16px);
          border: 1px solid rgba(255,255,255,0.06);
          border-radius: 12px;
          padding: 16px 20px;
        }
        
        .controls-title {
          font-size: 9px;
          text-transform: uppercase;
          letter-spacing: 1.5px;
          opacity: 0.35;
          margin-bottom: 12px;
        }
        
        .control-group {
          margin-bottom: 10px;
        }
        
        .control-group:last-child {
          margin-bottom: 0;
        }
        
        .control-row {
          display: flex;
          align-items: center;
          gap: 10px;
          margin-bottom: 6px;
        }
        
        .keys {
          display: flex;
          gap: 3px;
        }
        
        .key {
          background: rgba(255,255,255,0.08);
          border: 1px solid rgba(255,255,255,0.1);
          border-radius: 4px;
          padding: 4px 8px;
          font-family: 'JetBrains Mono', monospace;
          font-size: 9px;
          min-width: 24px;
          text-align: center;
        }
        
        .key.highlight {
          background: rgba(124, 92, 255, 0.2);
          border-color: rgba(124, 92, 255, 0.3);
          color: #a78bfa;
        }
        
        .control-label {
          font-size: 11px;
          opacity: 0.45;
        }
        
        .hud-bottom-right {
          bottom: 24px;
          right: 24px;
        }
        
        .stats-panel {
          background: rgba(0,0,0,0.4);
          backdrop-filter: blur(10px);
          border-radius: 10px;
          padding: 12px 16px;
          font-family: 'JetBrains Mono', monospace;
          font-size: 10px;
          opacity: 0.6;
          line-height: 1.7;
          text-align: right;
        }
        
        /* Speed indicator */
        .speed-indicator {
          position: fixed;
          bottom: 24px;
          left: 50%;
          transform: translateX(-50%);
          display: flex;
          flex-direction: column;
          align-items: center;
          gap: 8px;
          pointer-events: none;
        }
        
        .speed-bar-container {
          width: 200px;
          height: 4px;
          background: rgba(255,255,255,0.1);
          border-radius: 2px;
          overflow: hidden;
        }
        
        .speed-bar {
          height: 100%;
          background: linear-gradient(90deg, #4ade80, #a78bfa, #f472b6);
          border-radius: 2px;
          transition: width 0.1s ease-out;
        }
        
        .speed-label {
          font-family: 'JetBrains Mono', monospace;
          font-size: 10px;
          opacity: 0.5;
        }
        
        /* Center reticle */
        .reticle {
          position: fixed;
          top: 50%;
          left: 50%;
          transform: translate(-50%, -50%);
          width: 20px;
          height: 20px;
          pointer-events: none;
          opacity: 0.3;
        }
        
        .reticle::before,
        .reticle::after {
          content: '';
          position: absolute;
          background: #fff;
        }
        
        .reticle::before {
          top: 50%;
          left: 0;
          right: 0;
          height: 1px;
          transform: translateY(-50%);
        }
        
        .reticle::after {
          left: 50%;
          top: 0;
          bottom: 0;
          width: 1px;
          transform: translateX(-50%);
        }
        
        /* Vignette */
        .vignette {
          position: fixed;
          inset: 0;
          pointer-events: none;
          background: radial-gradient(
            ellipse at center,
            transparent 0%,
            transparent 50%,
            rgba(0,0,0,0.7) 100%
          );
        }
        
        /* Motion blur hint when moving fast */
        .motion-blur {
          position: fixed;
          inset: 0;
          pointer-events: none;
          background: radial-gradient(
            ellipse at center,
            transparent 40%,
            rgba(124, 92, 255, ${Math.min(zSpeed * 0.01, 0.15)}) 100%
          );
          transition: opacity 0.2s;
        }
      `}</style>

      <div 
        ref={containerRef}
        className="canvas-3d"
        onPointerDown={handlePointerDown}
        onPointerMove={handlePointerMove}
        onPointerUp={handlePointerUp}
        onPointerLeave={handlePointerUp}
        onWheel={handleWheel}
        onTouchStart={handleTouchStart}
        onTouchMove={handleTouchMove}
        onTouchEnd={handleTouchEnd}
      >
        {/* Depth gradient background */}
        <div className="depth-gradient" />
        
        {/* Render 3D planes */}
        {visiblePlanes.map(plane => (
          <div
            key={plane.id}
            className="plane-3d"
            style={{
              left: plane.screenX,
              top: plane.screenY,
              width: plane.screenSize,
              height: plane.screenHeight,
              transform: `translate(-50%, -50%) ${plane.transform3d}`,
              opacity: plane.opacity,
              zIndex: Math.floor(10000 - plane.depth),
              background: `hsl(${plane.artwork.hue}, 25%, 15%)`,
            }}
          >
            <div className="plane-inner">
              <div 
                className="plane-image"
                style={{
                  background: `linear-gradient(135deg, 
                    hsl(${plane.artwork.hue}, 35%, 20%) 0%, 
                    hsl(${plane.artwork.hue}, 20%, 10%) 100%)`,
                }}
              >
                <span className="plane-icon">🖼️</span>
              </div>
              <div className="plane-info">
                <div className="plane-title">{plane.artwork.title}</div>
                <div className="plane-meta">{plane.artwork.artist}, {plane.artwork.year}</div>
              </div>
            </div>
          </div>
        ))}
      </div>

      {/* Visual effects */}
      <div className="vignette" />
      <div className="motion-blur" />
      <div className="reticle" />

      {/* HUD */}
      <div className="hud">
        <div className="hud-panel hud-top-left">
          <div className="hud-title">∞ 3D Canvas</div>
          <div className="hud-subtitle">Fly through infinite space</div>
        </div>
        
        <div className="hud-panel hud-top-right">
          <div className="coords-panel">
            <span className="coord-label">X</span>
            <span className="coord-value">{Math.round(camera.x)}</span><br />
            <span className="coord-label">Y</span>
            <span className="coord-value">{Math.round(camera.y)}</span><br />
            <span className="coord-label">Z</span>
            <span className="coord-value z-value">{Math.round(camera.z)}</span>
          </div>
          <div className={`hud-fps ${stats.fps >= 55 ? 'fps-good' : stats.fps >= 30 ? 'fps-ok' : 'fps-bad'}`}>
            {stats.fps} FPS
          </div>
        </div>
        
        <div className="hud-panel hud-bottom-left">
          <div className="controls-panel">
            <div className="controls-title">Navigation</div>
            <div className="control-group">
              <div className="control-row">
                <div className="keys">
                  <span className="key">W</span>
                  <span className="key">A</span>
                  <span className="key">S</span>
                  <span className="key">D</span>
                </div>
                <span className="control-label">Pan X/Y</span>
              </div>
            </div>
            <div className="control-group">
              <div className="control-row">
                <div className="keys">
                  <span className="key highlight">E</span>
                  <span className="key highlight">Q</span>
                </div>
                <span className="control-label">Fly forward / backward</span>
              </div>
              <div className="control-row">
                <div className="keys">
                  <span className="key highlight">Scroll</span>
                </div>
                <span className="control-label">Fly through Z</span>
              </div>
            </div>
            <div className="control-group">
              <div className="control-row">
                <div className="keys">
                  <span className="key">Drag</span>
                </div>
                <span className="control-label">Look around</span>
              </div>
              <div className="control-row">
                <div className="keys">
                  <span className="key">R</span>
                </div>
                <span className="control-label">Reset</span>
              </div>
            </div>
          </div>
        </div>
        
        <div className="hud-panel hud-bottom-right">
          <div className="stats-panel">
            Chunks: {stats.chunks}<br />
            Planes: {stats.planes}<br />
            Depth: {Math.round(camera.z)}
          </div>
        </div>
        
        {/* Speed indicator */}
        <div className="speed-indicator">
          <div className="speed-bar-container">
            <div 
              className="speed-bar" 
              style={{ width: `${Math.min(speed * 2, 100)}%` }} 
            />
          </div>
          <div className="speed-label">
            {zSpeed > 1 ? (velocity.z > 0 ? '→ Forward' : '← Backward') : 'Stationary'}
          </div>
        </div>
      </div>
    </div>
  );
}
