import { Link } from 'react-router-dom';
import { api } from '../../compartido/api/cliente';
import type { PartidoResumenDto } from '../../compartido/api/tiposActividades';
import { useCarga } from '../../compartido/api/useCarga';
import { Aviso } from '../../compartido/componentes/Aviso';
import { Tarjeta } from '../../compartido/componentes/Tarjeta';
import { diaDeLaSemana, horaCorta } from '../../compartido/fechas';
import { EtiquetaActividad } from '../actividades/EtiquetaActividad';
import { nombreRival, textoDeMarcador } from '../partidos/textos';

/** Cuántos partidos se muestran en cada parte de la tarjeta. */
const CUANTOS = 3;

interface Props {
  clubId: string;
  nombreClub: string;
}

/**
 * "Partidos" del inicio, para todos los roles: los próximos que la persona puede ver (también los
 * cancelados y los que esperan resultado, con su estado escrito) y los últimos resultados, con su
 * marcador. Usa la misma lista que la pantalla "Partidos", así que un partido próximo al que la
 * cuenta no está convocada no llega de la API (§17.2). Sin partidos, lo dice.
 */
export function TarjetaPartidos({ clubId, nombreClub }: Props) {
  const ruta = `/api/clubes/${clubId}/partidos`;
  const carga = useCarga(ruta, () => api.get<PartidoResumenDto[]>(ruta));
  const partidos = carga.datos ?? [];
  const proximos = partidos.filter((partido) => partido.estado !== 'JUGADO').slice(0, CUANTOS);
  const jugados = partidos.filter((partido) => partido.estado === 'JUGADO').slice(0, CUANTOS);
  const enlace = (partido: PartidoResumenDto) => `/club/${clubId}/partidos/${partido.partidoId}`;

  return (
    <Tarjeta titulo="Partidos" acciones={<Link to={`/club/${clubId}/partidos`}>Ver partidos</Link>}>
      {carga.error && <Aviso tono="error">{carga.error}</Aviso>}
      {!carga.datos && carga.cargando && <p className="texto-suave">Cargando…</p>}
      {carga.datos && (
        <>
          <h3 className="subtitulo-de-inicio">Próximos</h3>
          {proximos.length === 0 ? (
            <p className="texto-suave">No hay partidos próximos.</p>
          ) : (
            <ul className="lista-de-inicio">
              {proximos.map((partido) => (
                <li key={partido.partidoId}>
                  <Link to={enlace(partido)}>{diaDeLaSemana(partido.fecha)}</Link>
                  <span>{horaCorta(partido.hora)}</span>
                  <span className="dato-largo">{`Contra ${nombreRival(partido)}`}</span>
                  <EtiquetaActividad tipo="PARTIDO" estado={partido.estado} faltaResultado={partido.faltaResultado} />
                </li>
              ))}
            </ul>
          )}
          <h3 className="subtitulo-de-inicio">Últimos resultados</h3>
          {jugados.length === 0 ? (
            <p className="texto-suave">Todavía no hay resultados.</p>
          ) : (
            <ul className="lista-de-inicio">
              {jugados.map((partido) => (
                <li key={partido.partidoId}>
                  <Link to={enlace(partido)}>{diaDeLaSemana(partido.fecha)}</Link>
                  <strong className="dato-largo">{textoDeMarcador(partido, nombreClub)}</strong>
                </li>
              ))}
            </ul>
          )}
        </>
      )}
    </Tarjeta>
  );
}
