import { Capacitor } from '@capacitor/core';

/**
 * Registra el service worker que permite instalar la versión web de LFClub como aplicación de
 * escritorio (PWA) desde Chrome o Edge. Solo en la web publicada: ni en desarrollo (para no
 * interferir con la recarga de Vite) ni dentro de la aplicación de Android, que ya está instalada.
 */
export function registrarInstalacionEscritorio(): void {
  if (!import.meta.env.PROD || Capacitor.isNativePlatform() || !('serviceWorker' in navigator)) return;

  window.addEventListener('load', () => {
    navigator.serviceWorker.register('/sw.js').catch(() => {
      // Sin service worker la web funciona igual; solo no se ofrece instalarla.
    });
  });
}
