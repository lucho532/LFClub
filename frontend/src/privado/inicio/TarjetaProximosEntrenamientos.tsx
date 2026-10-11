import { Link } from 'react-router-dom';
import { api } from '../../compartido/api/cliente';
import type { EntrenamientoResumenDto } from '../../compartido/api/tiposActividades';
import { useCarga } from '../../compartido/api/useCarga';
import { Aviso } from '../../compartido/componentes/Aviso';
import { Tarjeta } from '../../compartido/componentes/Tarjeta';
import { diaDeLaSemana, rangoDeHoras } from '../../compartido/fechas';
import { EtiquetaActividad } from '../actividades/EtiquetaActividad';

/** Lo que dice la tarjeta cuando no hay entrenamientos que mostrar (escenario 1.9). */
export const SIN_ENTRENAMIENTOS = 'No hay entrenamientos próximos.';

interface Props {
  clubId: string;
}

/**
 * "Próximos entrenamientos" del inicio, para todos los roles (spec 009, RF-006): hasta cinco
 * entrenamientos que la persona puede ver y que no han terminado, con su fecha, sus horas, su
 * lugar y su estado escrito (un cancelado sale marcado), cada uno con enlace a su detalle. Sin
 * entrenamientos, lo dice, sin datos de ejemplo. Quien decide cuáles ve es la API.
 */
export function TarjetaProximosEntrenamientos({ clubId }: Props) {
  const ruta = `/api/clubes/${clubId}/entrenamientos/proximos`;
  const carga = useCarga(ruta, () => api.get<EntrenamientoResumenDto[]>(ruta));
  const proximos = carga.datos;

  return (
    <Tarjeta
      titulo="Próximos entrenamientos"
      acciones={<Link to={`/club/${clubId}/calendario`}>Ver calendario</Link>}
    >
      {carga.error && <Aviso tono="error">{carga.error}</Aviso>}
      {!proximos && carga.cargando && <p className="texto-suave">Cargando…</p>}
      {proximos?.length === 0 && <p className="texto-suave">{SIN_ENTRENAMIENTOS}</p>}
      {proximos && proximos.length > 0 && (
        <ul className="lista-de-inicio">
          {proximos.map((entrenamiento) => (
            <li key={entrenamiento.entrenamientoId}>
              <Link to={`/club/${clubId}/entrenamientos/${entrenamiento.entrenamientoId}`}>
                {diaDeLaSemana(entrenamiento.fecha)}
              </Link>
              <span>{rangoDeHoras(entrenamiento.horaInicio, entrenamiento.horaFin)}</span>
              <span className="dato-largo">{entrenamiento.lugar}</span>
              <EtiquetaActividad tipo="ENTRENAMIENTO" estado={entrenamiento.estado} />
            </li>
          ))}
        </ul>
      )}
    </Tarjeta>
  );
}
