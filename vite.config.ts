import { defineConfig } from 'vite';
import basicSsl from '@vitejs/plugin-basic-ssl';

// HTTPS también en desarrollo: la cámara, el GPS y los sensores solo funcionan en contextos seguros.
// `npm run dev` arranca con --host para poder abrirlo desde el iPhone en la misma red Wi-Fi.
export default defineConfig({
  plugins: [basicSsl()],
  server: {
    host: true,
    port: 5173,
  },
  build: {
    target: 'es2020',
  },
});
