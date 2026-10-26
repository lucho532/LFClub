import type { CSSProperties, ReactNode } from 'react';
import { api } from '../api/cliente';
import type { IdentidadClubDto } from '../api/tipos';
import { variablesDeIdentidad } from './identidadVisual';

interface Props {
  identidad: IdentidadClubDto | null | undefined;
  className?: string;
  /** Para listas de clubes: usa los colores de cada club pero no pone su escudo de fondo de la pantalla. */
  sinMarcaDeAgua?: boolean;
  children: ReactNode;
}

/**
 * Aplica la identidad de un club a todo lo que contiene: fija en su contenedor las variables de
 * color del club y las que de ellas se derivan (menú, ítem activo, fondo teñido; ver
 * identidadVisual.ts) y, si el club tiene escudo, lo pone de fondo como marca de agua clara
 * (`.con-marca-agua`). Con color principal añade `.con-identidad`, que identidad.css usa para
 * teñir el fondo y marcar tarjetas, títulos y barra superior. Sin identidad, lo de dentro se ve
 * con la identidad neutra de la plataforma y sin marca de agua.
 */
export function IdentidadClub({ identidad, className, sinMarcaDeAgua, children }: Props) {
  const escudo = sinMarcaDeAgua ? null : identidad?.urlEscudo;
  const estilo = variablesDeIdentidad(identidad);
  if (escudo) {
    estilo['--escudo-marca'] = `url("${api.url(escudo)}")`;
  }

  const clases = [
    className,
    escudo ? 'con-marca-agua' : undefined,
    estilo['--fondo-club'] ? 'con-identidad' : undefined,
  ]
    .filter(Boolean)
    .join(' ');

  return (
    <div className={clases || undefined} style={estilo as CSSProperties}>
      {children}
    </div>
  );
}

interface PropsEscudo {
  identidad: IdentidadClubDto | null | undefined;
  nombre: string;
}

/** Escudo del club o, si no tiene, un distintivo neutro con su inicial. */
export function Escudo({ identidad, nombre }: PropsEscudo) {
  if (identidad?.urlEscudo) {
    return <img className="distintivo" src={api.url(identidad.urlEscudo)} alt={`Escudo de ${nombre}`} />;
  }

  return (
    <span className="distintivo" aria-hidden="true">
      {nombre.trim().charAt(0).toUpperCase()}
    </span>
  );
}
