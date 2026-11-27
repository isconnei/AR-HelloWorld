// Parámetros de la URL, ajustables en terreno sin recompilar.
//   ?debug=1  muestra el panel de estado (FPS, permisos, GPS…)
//   ?fov=63   campo de visión vertical (grados) de la cámara virtual, para igualar la cámara física

const params = new URLSearchParams(window.location.search);

export const config = {
  debug: params.get('debug') === '1',
  /** FOV vertical en grados. 63° aproxima la cámara trasera del iPhone en vertical. */
  fov: clampNumber(params.get('fov'), 63, 30, 100),
  /** Id de la instancia (se usará en el paso 4: public/instances/<id>.json). */
  instance: params.get('i') ?? 'parque-el-bosque',
};

function clampNumber(raw: string | null, fallback: number, min: number, max: number): number {
  const value = raw === null ? NaN : Number(raw);
  return Number.isFinite(value) ? Math.min(max, Math.max(min, value)) : fallback;
}
