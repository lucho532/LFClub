import { api } from '../../compartido/api/cliente';
import type { CifrasDto, EstadisticasJugadorDto } from '../../compartido/api/tiposEstadisticas';
import { useCarga } from '../../compartido/api/useCarga';
import { Aviso } from '../../compartido/componentes/Aviso';
import { Tarjeta } from '../../compartido/componentes/Tarjeta';

interface Props {
  /** Ruta de la API de la ficha, `/api/clubes/{clubId}/jugadores/{usuarioRolId}/ficha`. */
  rutaDeLaFicha: string;
}

/** "67 % (2 de 3)". */
export function textoDeAsistencia(cifras: CifrasDto): string {
  return `${cifras.porcentajeAsistencia} % (${cifras.entrenamientosAsistidos} de ${cifras.entrenamientosRegistrados})`;
}

function Fila({ titulo, cifras }: { titulo: string; cifras: CifrasDto }) {
  return (
    <div className="columna estadisticas-fila">
      <h3>{titulo}</h3>
      <dl className="ficha-datos">
        <div>
          <dt>Partidos jugados</dt>
          <dd>{cifras.partidosJugados}</dd>
        </div>
        <div>
          <dt>Goles</dt>
          <dd>{cifras.goles}</dd>
        </div>
        <div>
          <dt>Asistencia a entrenamientos</dt>
          <dd>{textoDeAsistencia(cifras)}</dd>
        </div>
      </dl>
    </div>
  );
}

/**
 * Las estadísticas del jugador en su ficha (spec 009, historia 6): partidos jugados, goles y
 * asistencia a entrenamientos, en total y por cada categoría con registros. Las calcula la API con
 * lo registrado; sin registros, todo en cero y lo dice. Solo la pide quien ya ve la ficha.
 */
export function SeccionEstadisticas({ rutaDeLaFicha }: Props) {
  const ruta = rutaDeLaFicha.replace(/\/ficha$/, '/estadisticas');
  const carga = useCarga(ruta, () => api.get<EstadisticasJugadorDto>(ruta));
  const estadisticas = carga.datos;

  return (
    <Tarjeta titulo="Estadísticas">
      {carga.error && <Aviso tono="error">{carga.error}</Aviso>}
      {!estadisticas && carga.cargando && <p className="texto-suave">Cargando…</p>}
      {estadisticas && (
        <>
          <Fila titulo="Total" cifras={estadisticas.total} />
          {estadisticas.porCategoria.length === 0 ? (
            <p className="texto-suave">Todavía no hay partidos ni entrenamientos registrados.</p>
          ) : (
            estadisticas.porCategoria.map((fila) => (
              <Fila key={fila.categoria.categoriaId} titulo={`Categoría ${fila.categoria.anio}`} cifras={fila} />
            ))
          )}
        </>
      )}
    </Tarjeta>
  );
}
