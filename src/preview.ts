// Vista previa del avatar en el escritorio (sin cámara, sensores ni permisos).
// `npm run preview:avatar` y abrir http://localhost:5174/preview.html
// La cámara gira sola a un lado y a otro para ver cómo el pájaro la sigue.
import * as THREE from 'three';
import { ProceduralKingfisher } from './avatar/ProceduralKingfisher';
import { config } from './config';
import { FlightFollower } from './flight';
import { ArScene } from './scene';

const params = new URLSearchParams(window.location.search);
const ar = new ArScene(document.getElementById('scene') as HTMLCanvasElement);

const bird = new ProceduralKingfisher(config.size ?? 1.5);
bird.talking = params.get('talk') === '1';
bird.thinking = params.get('think') === '1';
ar.scene.add(bird.root);

const follower = new FlightFollower(bird, ar.camera, { distance: config.distance ?? 0.85, smoothTime: config.lag, heightOffset: -0.06 });
const still = params.get('still') === '1';
if (!still) {
  follower.arrive(1);
  bird.greet();
}

ar.onFrame((dt, time) => {
  if (!still) {
    // Mirar a un lado y a otro: pausa, giro rápido de 60°, pausa, vuelta.
    const cycle = (time % 8) / 8;
    const yaw = cycle < 0.5 ? smooth((cycle - 0.1) / 0.15) : 1 - smooth((cycle - 0.6) / 0.15);
    ar.camera.quaternion.setFromEuler(new THREE.Euler(0, yaw * THREE.MathUtils.degToRad(60), 0, 'YXZ'));
  }
  follower.update(dt, time);
  bird.update(dt, time, { effort: follower.currentEffort });
});
ar.start();

function smooth(x: number): number {
  const t = Math.min(1, Math.max(0, x));
  return t * t * (3 - 2 * t);
}
