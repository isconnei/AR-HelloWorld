// Parámetros de la URL, ajustables en terreno sin recompilar.
//   ?debug=1   panel de estado (FPS, permisos, GPS, giroscopio) y botones para probar animaciones
//   ?fov=63    campo de visión vertical (grados) de la cámara virtual, para igualar la cámara física
//   ?dist=0.85 distancia (m) a la que vuela el pájaro
//   ?size=1.5  tamaño del pájaro respecto al real (1 = unos 17 cm de pico a cola)
//   ?lag=0.45  retraso (s) con el que te alcanza cuando giras
//   ?test=1    muestra el cubo, las esferas y la cuadrícula de prueba del paso 2
//   ?i=<id>    instancia (paso 4: public/instances/<id>.json)

const params = new URLSearchParams(window.location.search);

export const config = {
  debug: params.get('debug') === '1',
  test: params.get('test') === '1',
  /** FOV vertical en grados. 63° aproxima la cámara trasera del iPhone en vertical. */
  fov: clampNumber(params.get('fov'), 63, 30, 100),
  distance: clampNumber(params.get('dist'), 0.85, 0.4, 3),
  size: clampNumber(params.get('size'), 1.5, 0.5, 5),
  lag: clampNumber(params.get('lag'), 0.45, 0.1, 2),
  instance: params.get('i') ?? 'parque-el-bosque',
};

function clampNumber(raw: string | null, fallback: number, min: number, max: number): number {
  const value = raw === null ? NaN : Number(raw);
  return Number.isFinite(value) ? Math.min(max, Math.max(min, value)) : fallback;
}
