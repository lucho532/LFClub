import { api } from '../../compartido/api/cliente';
import type { PartidoResumenDto } from '../../compartido/api/tiposActividades';
import { useCarga } from '../../compartido/api/useCarga';
import { Aviso } from '../../compartido/componentes/Aviso';
import { Tarjeta } from '../../compartido/componentes/Tarjeta';
import { diaDeLaSemana } from '../../compartido/fechas';
import { TarjetaActividad } from '../calendario/TarjetaActividad';
import { useClub } from '../contextoClub';
import { textoDeMarcador } from './textos';

/**
 * Los partidos que la persona puede ver, en dos secciones: "Próximos" (también los cancelados que
 * todavía tienen fecha por delante, marcados) y "Jugados", con su marcador. Para cada uno, la
 * etiqueta con el estado escrito, el rival, la fecha, la hora y si es local o visitante. Los
 * próximos a los que no está convocado no llegan de la API (§17.2).
 */
export function Partidos() {
  const { club } = useClub();
  const ruta = `/api/clubes/${club.clubId}/partidos`;
  const carga = useCarga(ruta, () => api.get<PartidoResumenDto[]>(ruta));
  const partidos = carga.datos ?? [];
  const proximos = partidos.filter((partido) => partido.estado !== 'JUGADO');
  const jugados = partidos.filter((partido) => partido.estado === 'JUGADO');

  function lista(deLaSeccion: PartidoResumenDto[], vacia: string) {
    return deLaSeccion.length === 0 ? (
      <p className="texto-suave">{vacia}</p>
    ) : (
      <div className="lista-actividades">
        {deLaSeccion.map((partido) => (
          <TarjetaActividad
            key={partido.partidoId}
            clubId={club.clubId}
            tipo="PARTIDO"
            partido={partido}
            pie={
              <>
                <span className="texto-suave">{diaDeLaSemana(partido.fecha)}</span>
                {partido.estado === 'JUGADO' && (
                  <strong className="dato-largo">{textoDeMarcador(partido, club.nombre)}</strong>
                )}
              </>
            }
          />
        ))}
      </div>
    );
  }

  return (
    <>
      <div className="cabecera">
        <h1>Partidos</h1>
      </div>
      {carga.error && <Aviso tono="error">{carga.error}</Aviso>}
      {carga.cargando && !carga.datos && <p className="texto-suave">Cargando…</p>}
      {carga.datos && partidos.length === 0 && <p className="texto-suave">No hay partidos.</p>}
      {carga.datos && partidos.length > 0 && (
        <>
          <Tarjeta titulo="Próximos">{lista(proximos, 'No hay partidos próximos.')}</Tarjeta>
          <Tarjeta titulo="Jugados">{lista(jugados, 'Todavía no hay partidos jugados.')}</Tarjeta>
        </>
      )}
    </>
  );
}
