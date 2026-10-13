import type { ReactNode } from 'react';
import type { IdentidadClubDto } from '../compartido/api/tipos';
import { BotonRecargar } from '../compartido/componentes/BotonRecargar';
import { BotonTema } from '../compartido/componentes/BotonTema';
import { Escudo, IdentidadClub } from '../compartido/tema/IdentidadClub';

interface Props {
  titulo: string;
  /** Texto bajo el título, por ejemplo el nombre del club que invita. */
  subtitulo?: string;
  /** Identidad del club que invita. Sin ella se usa la identidad neutra de la plataforma. */
  identidad?: IdentidadClubDto;
  /** Adorno que va detrás del contenido, como el fondo de balones de entrar. */
  fondo?: ReactNode;
  children: ReactNode;
}

/**
 * Disposición común de las pantallas sin sesión: entrar, recuperar, restablecer e invitación.
 * Lleva el botón de tema, que debe estar también donde no hay sesión (RF-032).
 */
export function DisposicionCuenta({ titulo, subtitulo, identidad, fondo, children }: Props) {
  return (
    <IdentidadClub identidad={identidad} className="centrado">
      {fondo}
      <header className="columna">
        <div className="cabecera">
          <p className="lateral-marca" style={{ padding: 0 }}>
            LFClub
          </p>
          <span className="barra-superior-acciones">
            <BotonRecargar />
            <BotonTema />
          </span>
        </div>
        {identidad && <Escudo identidad={identidad} nombre={titulo} />}
        <h1>{titulo}</h1>
        {subtitulo && <p className="texto-suave">{subtitulo}</p>}
      </header>
      <main className="tarjeta">{children}</main>
    </IdentidadClub>
  );
}
