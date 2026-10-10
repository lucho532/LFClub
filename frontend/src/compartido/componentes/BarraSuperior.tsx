import type { ReactNode } from 'react';
import { BotonTema } from './BotonTema';

interface Props {
  /**
   * Escudo y nombre del club, o de la plataforma. Solo se ven en el teléfono, donde no hay menú
   * lateral que los muestre.
   */
  marca?: ReactNode;
  /** El perfil de la persona (`MenuPerfil`), a la derecha. */
  children: ReactNode;
}

/**
 * Barra superior de las pantallas con sesión: a la izquierda, en el teléfono, la marca; a la
 * derecha, el botón de tema y el perfil de la persona. No conoce el club ni la sesión: solo
 * coloca lo que le pasan.
 */
export function BarraSuperior({ marca, children }: Props) {
  return (
    <header className="barra-superior">
      {marca && <div className="barra-superior-marca">{marca}</div>}
      <div className="barra-superior-acciones">
        <BotonTema />
        {children}
      </div>
    </header>
  );
}
