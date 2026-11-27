import { defineConfig } from 'vite';

// Servidor sin HTTPS solo para la vista previa del avatar en el escritorio (no usa cámara ni sensores).
export default defineConfig({
  server: { port: 5174, strictPort: true },
});
