import type { CSSProperties, ReactNode } from 'react';
import { api } from '../api/cliente';
import type { IdentidadClubDto } from '../api/tipos';
import { coloresClub } from './coloresClub';

interface Props {
  identidad: IdentidadClubDto | null | undefined;
  className?: string;
  children: ReactNode;
}

/**
 * Aplica la identidad de un club a todo lo que contiene: fija las variables de color del club en
 * su contenedor y, si el club tiene escudo, lo pone de fondo como marca de agua clara
 * (`.con-marca-agua`). Sin identidad, lo de dentro se ve con la identidad neutra de la plataforma
 * y sin marca de agua.
 */
export function IdentidadClub({ identidad, className, children }: Props) {
  const escudo = identidad?.urlEscudo;
  const estilo: Record<string, string> = { ...coloresClub(identidad) };
  if (escudo) {
    estilo['--escudo-marca'] = `url("${api.url(escudo)}")`;
  }

  const clases = [className, escudo ? 'con-marca-agua' : undefined].filter(Boolean).join(' ');

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
