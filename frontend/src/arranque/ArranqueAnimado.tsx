import { useEffect } from 'react';
import { BalonPecosa } from './BalonPecosa';

/** Lo que dura la animación completa, con la salida incluida (ver arranque.css). */
const DURACION_MS = 2100;

/** Con movimiento reducido solo se ve el balón quieto y el nombre, un instante. */
const DURACION_REDUCIDA_MS = 400;

interface Props {
  /** Se llama cuando la animación terminó y la pantalla ya es transparente. */
  alTerminar: () => void;
}

/**
 * Animación de arranque de la aplicación Android: continúa la pantalla de arranque nativa, con el
 * mismo fondo y el balón en el mismo sitio. El balón bota dos veces mientras sus paneles giran,
 * aparece "La Pecosa" y todo se desvanece sobre la aplicación, que ya se está cargando debajo.
 * Respeta la preferencia de movimiento reducido. No bloquea nada: solo tapa durante dos segundos.
 */
export function ArranqueAnimado({ alTerminar }: Props) {
  useEffect(() => {
    const reducido = window.matchMedia?.('(prefers-reduced-motion: reduce)').matches ?? false;
    const temporizador = window.setTimeout(() => {
      // El fondo oscuro que puso index.html para que no hubiera destello ya no hace falta.
      document.documentElement.removeAttribute('data-arranque');
      alTerminar();
    }, reducido ? DURACION_REDUCIDA_MS : DURACION_MS);
    return () => window.clearTimeout(temporizador);
  }, [alTerminar]);

  return (
    <div className="arranque" aria-hidden="true">
      <div className="arranque-escena">
        <BalonPecosa tamano={160} className="arranque-balon" />
        <span className="arranque-sombra" />
      </div>
      <p className="arranque-nombre">La Pecosa</p>
    </div>
  );
}
