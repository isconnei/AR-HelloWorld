// Parámetros de la URL, ajustables en terreno sin recompilar.
//   ?i=<id>       instancia: carga public/instances/<id>.json (por defecto parque-el-bosque)
//   ?debug=1      panel de estado (FPS, permisos, GPS, giroscopio, geocerca) y botones de prueba
//   ?fov=63       campo de visión vertical (grados) de la cámara virtual, para igualar la cámara física
//   ?dist=0.85    distancia (m) a la que vuela el pájaro (por defecto, la de la instancia)
//   ?size=1.5     tamaño del pájaro respecto al real (por defecto, el de la instancia)
//   ?lag=0.45     retraso (s) con el que te alcanza cuando giras
//   ?lat=&lon=&r= punto de geocerca de prueba (también se puede fijar con el botón del modo debug)
//   ?test=1       muestra el cubo, las esferas y la cuadrícula de prueba del paso 2

export const params = new URLSearchParams(window.location.search);

export const config = {
  debug: params.get('debug') === '1',
  test: params.get('test') === '1',
  /** FOV vertical en grados. 63° aproxima la cámara trasera del iPhone en vertical. */
  fov: clampNumber(params.get('fov'), 63, 30, 100),
  /** null = usar el valor de la instancia. */
  distance: optionalNumber(params.get('dist'), 0.4, 3),
  size: optionalNumber(params.get('size'), 0.5, 5),
  lag: clampNumber(params.get('lag'), 0.45, 0.1, 2),
  instance: params.get('i') ?? 'parque-el-bosque',
};

function clampNumber(raw: string | null, fallback: number, min: number, max: number): number {
  return optionalNumber(raw, min, max) ?? fallback;
}

function optionalNumber(raw: string | null, min: number, max: number): number | null {
  const value = raw === null ? NaN : Number(raw);
  return Number.isFinite(value) ? Math.min(max, Math.max(min, value)) : null;
}
