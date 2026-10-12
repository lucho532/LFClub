import { useRef, useState } from 'react';
import { FotoAmpliable } from '../../compartido/componentes/FotoAmpliable';
import { inicialesDe } from '../../compartido/componentes/Avatar';
import { useEnPantalla, useFotoJugador } from './fotosJugador';

interface Props {
  clubId: string;
  jugador: { usuarioRolId: string; nombres: string; apellidos: string; fotoActualizadaEn: string | null };
  tamano: 'miniatura' | 'grande';
  /** Si la foto se pide solo cuando el elemento entra en pantalla (las filas de la lista). */
  perezosa?: boolean;
}

/**
 * Foto propia de un jugador o, si no tiene, si todavía no llegó o si falla, sus iniciales. La
 * grande se abre en tamaño completo al tocarla. Solo dibuja: cargarla, cambiarla y quitarla está en
 * la ficha. Sin foto no hace ninguna petición.
 */
export function FotoJugador({ clubId, jugador, tamano, perezosa = false }: Props) {
  const caja = useRef<HTMLSpanElement>(null);
  const enPantalla = useEnPantalla(caja);
  const foto = useFotoJugador(clubId, jugador.usuarioRolId, jugador.fotoActualizadaEn, !perezosa || enPantalla);
  const [fallida, setFallida] = useState<string | null>(null);
  const clase = `distintivo foto-jugador-${tamano}`;

  if (foto && foto !== fallida && tamano === 'grande') {
    return (
      <FotoAmpliable
        className={clase}
        src={foto}
        alt={`Foto de ${jugador.nombres} ${jugador.apellidos}`}
        onError={() => setFallida(foto)}
      />
    );
  }

  if (foto && foto !== fallida) {
    return (
      <img
        className={clase}
        src={foto}
        alt=""
        onError={() => setFallida(foto)}
      />
    );
  }

  return (
    <span ref={caja} className={clase} aria-hidden="true">
      {inicialesDe(jugador.nombres, jugador.apellidos)}
    </span>
  );
}
