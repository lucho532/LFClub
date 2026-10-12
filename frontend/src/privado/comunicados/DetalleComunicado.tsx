import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { api } from '../../compartido/api/cliente';
import { mensajeDe } from '../../compartido/api/errores';
import type { ComunicadoPublicadoDto, ComunicadoRecibidoDto } from '../../compartido/api/tiposComunicados';
import { Aviso } from '../../compartido/componentes/Aviso';
import { NombrePersona } from '../../compartido/componentes/NombrePersona';
import { Tarjeta } from '../../compartido/componentes/Tarjeta';
import { fechaYHora, nombreDeRol } from '../../compartido/formato';
import { estadoDeComunicado, textoDeAudiencia, textoDeFamilias, textosComunicados } from './textos';

interface PropsRecibido {
  clubId: string;
  comunicadoId: string;
  /** Recibe el comunicado ya leído y si estaba sin leer antes de abrirlo. */
  alLeer: (comunicado: ComunicadoRecibidoDto, estabaSinLeer: boolean) => void;
}

/**
 * El detalle de un comunicado recibido por la cuenta de un jugador: título, texto completo con sus
 * saltos de línea y la fecha. Al abrirlo pide el comunicado y, si estaba sin leer, lo marca leído
 * para toda la cuenta (spec 011, RF-020 y RF-021); quien lo abrió recibe el aviso para bajar el
 * contador sin otra consulta. El texto se pinta como texto, nunca como HTML (RF-036).
 */
export function DetalleRecibido({ clubId, comunicadoId, alLeer }: PropsRecibido) {
  const [comunicado, setComunicado] = useState<ComunicadoRecibidoDto | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelado = false;
    async function abrir() {
      try {
        const base = `/api/clubes/${clubId}/comunicados/recibidos/${comunicadoId}`;
        const recibido = await api.get<ComunicadoRecibidoDto>(base);
        if (cancelado) {
          return;
        }

        setComunicado(recibido);
        if (!recibido.leido) {
          const leido = await api.put<ComunicadoRecibidoDto>(`${base}/lectura`, {});
          if (!cancelado) {
            setComunicado(leido);
            alLeer(leido, true);
          }
        }
      } catch (fallo) {
        if (!cancelado) {
          setError(mensajeDe(fallo));
        }
      }
    }

    // eslint-disable-next-line react-hooks/set-state-in-effect
    setComunicado(null);
    setError(null);
    void abrir();
    return () => {
      cancelado = true;
    };
    // `alLeer` cambia en cada pintado de quien lo usa; abrir un comunicado depende solo de su identificador.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [clubId, comunicadoId]);

  if (error) {
    return <Aviso tono="error">{error}</Aviso>;
  }

  if (!comunicado) {
    return <p className="texto-suave">Cargando…</p>;
  }

  return (
    <article className="comunicado-detalle">
      <Link to={`/club/${clubId}/comunicados`} className="volver-lista">
        ← {textosComunicados.tituloFamilia}
      </Link>
      <h2>{comunicado.titulo}</h2>
      <p className="comunicado-datos">
        <span>
          <NombrePersona
            nombre={comunicado.publicadoPor.nombre}
            enlace={
              comunicado.publicadoPor.usuarioRolId
                ? { usuarioRolId: comunicado.publicadoPor.usuarioRolId, tipo: 'BASICA' }
                : null
            }
            clubId={clubId}
            textoVolver="Comunicados"
          />{' '}
          · {nombreDeRol(comunicado.publicadoPor.rol)}
        </span>
        <span className="texto-suave">{fechaYHora(comunicado.publicadoEn)}</span>
      </p>
      <p className="comunicado-texto">{comunicado.texto}</p>
    </article>
  );
}

interface PropsPublicado {
  clubId: string;
  comunicado: ComunicadoPublicadoDto | undefined;
  /** Mientras la lista no ha cargado, el comunicado todavía puede estar por llegar. */
  cargando: boolean;
}

/**
 * El detalle de un comunicado publicado, para el equipo del club: título, texto, audiencia con sus
 * destinos, a cuántas familias llegó, quién lo publicó y el estado, con quién y cuándo si se retiró.
 */
export function DetallePublicado({ clubId, comunicado, cargando }: PropsPublicado) {
  if (!comunicado) {
    return cargando ? (
      <p className="texto-suave">Cargando…</p>
    ) : (
      <Aviso tono="info">
        Ese comunicado no está en la lista cargada. Búscalo en <Link to={`/club/${clubId}/comunicados`}>la lista</Link>.
      </Aviso>
    );
  }

  return (
    <Tarjeta>
      <article className="comunicado-detalle">
        <Link to={`/club/${clubId}/comunicados`} className="volver-lista">
          ← {textosComunicados.tituloEquipo}
        </Link>
        <h2>{comunicado.titulo}</h2>
        <p className="comunicado-datos">
          <span className={`etiqueta-leido ${comunicado.retirado ? 'retirado' : 'publicado'}`}>
            {estadoDeComunicado(comunicado)}
          </span>
          <span>{textoDeAudiencia(comunicado.alcance, comunicado.destinos)}</span>
          <span>{textoDeFamilias(comunicado.familiasAlcanzadas)}</span>
        </p>
        <p className="comunicado-datos">
          <span>
            Publicado por{' '}
            <NombrePersona
              nombre={comunicado.publicadoPor.nombre}
              enlace={
                comunicado.publicadoPor.usuarioRolId
                  ? { usuarioRolId: comunicado.publicadoPor.usuarioRolId, tipo: 'BASICA' }
                  : null
              }
              clubId={clubId}
              textoVolver="Comunicados publicados"
            />{' '}
            · {nombreDeRol(comunicado.publicadoPor.rol)}
          </span>
          <span className="texto-suave">{fechaYHora(comunicado.publicadoEn)}</span>
        </p>
        {comunicado.retirado && (
          <p className="texto-suave">
            Retirado por {comunicado.retirado.porNombre} el {fechaYHora(comunicado.retirado.en)}
          </p>
        )}
        <p className="comunicado-texto">{comunicado.texto}</p>
      </article>
    </Tarjeta>
  );
}
