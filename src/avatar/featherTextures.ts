import * as THREE from 'three';

/**
 * Texturas de plumaje generadas en un <canvas> (sin imágenes), portadas de FeatherTextures.cs de la app Unity.
 * Pensadas para esferas: U da la vuelta alrededor y V va de abajo (0) a arriba (1).
 * Los colores se dan en sRGB (0–1).
 */

type RGB = [number, number, number];
type Pixel = (u: number, v: number, rnd: number) => RGB;

const SIZE = 128;

/** Barras horizontales finas e irregulares (píleo y bigotera del martín pescador). */
export function barred(base: RGB, bar: RGB, bars: number, barWidth: number, seed: number): THREE.CanvasTexture {
  return generate('barred', seed, (u, v, rnd) => {
    const wobble = 0.08 * (noise(u * 6 + rnd, v * 2) - 0.5);
    const phase = fract((v + wobble) * bars);
    // Cada barra está partida en trocitos, como las puntas claras de las plumas.
    const broken = noise(u * 24 + rnd, Math.floor((v + wobble) * bars) * 3.1);
    const t = phase < barWidth && broken > 0.42 ? 1 : 0;
    return shade(mix(base, bar, t), u, v, rnd, 0.12);
  });
}

/** Vetas verticales suaves (pecho, alas, cola, garganta). */
export function streaked(base: RGB, streak: RGB, streaks: number, strength: number, seed: number): THREE.CanvasTexture {
  return generate('streaked', seed, (u, v, rnd) => {
    const n = noise(u * streaks + rnd, v * 3);
    const fine = noise(u * streaks * 3 + rnd, v * 8);
    const t = clamp01((n * 0.7 + fine * 0.3 - 0.35) * 1.6) * strength;
    return shade(mix(base, streak, t), u, v, rnd, 0.08);
  });
}

/** Lomo brillante: azul eléctrico con reflejos y vetas. */
export function shimmer(deep: RGB, bright: RGB, seed: number): THREE.CanvasTexture {
  return generate('shimmer', seed, (u, v, rnd) => {
    const glow = noise(u * 3 + rnd, v * 5);
    const streak = noise(u * 30 + rnd, v * 4);
    return shade(mix(deep, bright, clamp01(glow * 1.2 + (streak - 0.5) * 0.6)), u, v, rnd, 0.06);
  });
}

export const darken = (c: RGB, k: number): RGB => [c[0] * k, c[1] * k, c[2] * k];
export const lighten = (c: RGB, k: number): RGB => mix(c, [1, 1, 1], k);

function generate(name: string, seed: number, pixel: Pixel): THREE.CanvasTexture {
  const canvas = document.createElement('canvas');
  canvas.width = canvas.height = SIZE;
  const ctx = canvas.getContext('2d')!;
  const image = ctx.createImageData(SIZE, SIZE);
  const rnd = seed * 17.31;
  for (let y = 0; y < SIZE; y++) {
    // Fila 0 del canvas = parte de arriba de la textura (V = 1), porque CanvasTexture voltea en Y.
    const v = 1 - (y + 0.5) / SIZE;
    for (let x = 0; x < SIZE; x++) {
      const u = (x + 0.5) / SIZE;
      const [r, g, b] = pixel(u, v, rnd);
      const i = (y * SIZE + x) * 4;
      image.data[i] = to255(r);
      image.data[i + 1] = to255(g);
      image.data[i + 2] = to255(b);
      image.data[i + 3] = 255;
    }
  }
  ctx.putImageData(image, 0, 0);

  const texture = new THREE.CanvasTexture(canvas);
  texture.name = `feathers-${name}-${seed}`;
  texture.colorSpace = THREE.SRGBColorSpace;
  texture.wrapS = texture.wrapT = THREE.RepeatWrapping;
  texture.anisotropy = 2;
  return texture;
}

/** Variación sutil de luminosidad para que no se vea plano. */
function shade(c: RGB, u: number, v: number, rnd: number, amount: number): RGB {
  const k = 1 + (noise(u * 40 + rnd * 0.5, v * 40) - 0.5) * amount * 2;
  return [c[0] * k, c[1] * k, c[2] * k];
}

function mix(a: RGB, b: RGB, t: number): RGB {
  return [a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t, a[2] + (b[2] - a[2]) * t];
}

/** Ruido de valor 2D suave en [0, 1] (equivalente práctico a Mathf.PerlinNoise). */
function noise(x: number, y: number): number {
  const xi = Math.floor(x);
  const yi = Math.floor(y);
  const xf = x - xi;
  const yf = y - yi;
  const u = xf * xf * (3 - 2 * xf);
  const v = yf * yf * (3 - 2 * yf);
  const a = hash(xi, yi);
  const b = hash(xi + 1, yi);
  const c = hash(xi, yi + 1);
  const d = hash(xi + 1, yi + 1);
  return a + (b - a) * u + (c - a) * v + (a - b - c + d) * u * v;
}

function hash(x: number, y: number): number {
  const h = Math.sin(x * 127.1 + y * 311.7) * 43758.5453;
  return h - Math.floor(h);
}

const fract = (x: number) => x - Math.floor(x);
const clamp01 = (x: number) => Math.min(1, Math.max(0, x));
const to255 = (x: number) => Math.round(clamp01(x) * 255);
