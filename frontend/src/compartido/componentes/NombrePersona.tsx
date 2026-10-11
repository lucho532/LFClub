import { Link, useLocation } from 'react-router-dom';
import type { EnlaceFichaDto } from '../api/tiposPersonas';
import { estadoVolver } from '../navegacion/useVolver';

interface Props {
  /** Nombre ya armado, tal como se muestra (el copiado en su momento, si es un autor). */
  nombre: string;
  /** A dónde lleva, según la API o la lista que ya está limitada a quien la ve; `null`, a ningún lado. */
  enlace: EnlaceFichaDto | null;
  clubId: string;
  /** Nombre de la pantalla actual, para el enlace "← …" de la ficha. */
  textoVolver: string;
  /** Clase adicional del enlace o del texto. */
  className?: string;
}

/** Dirección de la ficha a la que lleva un enlace: la del jugador o la ficha básica. */
export function rutaDeEnlace(clubId: string, enlace: EnlaceFichaDto): string {
  return enlace.tipo === 'JUGADOR'
    ? `/club/${clubId}/jugadores/${enlace.usuarioRolId}/ficha`
    : `/club/${clubId}/personas/${enlace.usuarioRolId}`;
}

/**
 * Nombre de una persona del club (spec 008, RF-010 y RF-011). Con enlace, abre la ficha del jugador
 * o la ficha básica y le dice a la ficha que vuelva a esta pantalla; sin enlace es texto normal, sin
 * aspecto ni foco de enlace. No decide si quien mira puede abrir la ficha: eso lo dice `enlace`, y
 * la API lo vuelve a comprobar al abrirla. Un nombre largo se parte sin desbordar la pantalla.
 */
export function NombrePersona({ nombre, enlace, clubId, textoVolver, className }: Props) {
  const { pathname } = useLocation();
  const clase = className ? `nombre-persona ${className}` : 'nombre-persona';

  if (!enlace) {
    return <span className={clase}>{nombre}</span>;
  }

  return (
    <Link className={clase} to={rutaDeEnlace(clubId, enlace)} state={estadoVolver(pathname, textoVolver)}>
      {nombre}
    </Link>
  );
}
