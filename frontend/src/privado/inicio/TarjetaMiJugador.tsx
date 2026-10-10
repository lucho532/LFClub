import { Link } from 'react-router-dom';
import { api } from '../../compartido/api/cliente';
import type { FichaJugadorDto } from '../../compartido/api/tiposFicha';
import { useCarga } from '../../compartido/api/useCarga';
import { Aviso } from '../../compartido/componentes/Aviso';
import { Tarjeta } from '../../compartido/componentes/Tarjeta';
import { nombreCompleto, rutaDeFicha } from '../categorias/textos';
import { FotoJugador } from '../jugadores/FotoJugador';
import { SIN_CATEGORIA, textoDeEquipos } from '../jugadores/textos';

interface Props {
  clubId: string;
  /** El jugador con el que continúa la familia: el de la petición. */
  miUsuarioRolId: string;
}

/**
 * Tarjeta del inicio de la familia (spec 007, RF-013): la foto de su jugador, o sus iniciales, su
 * nombre, su categoría y sus equipos, y la entrada a su ficha. Lee la ficha del jugador elegido,
 * sin pedir nada nuevo a la API; con varios hermanos, es la del que eligió al entrar.
 */
export function TarjetaMiJugador({ clubId, miUsuarioRolId }: Props) {
  const ficha = useCarga(`${clubId}/ficha/${miUsuarioRolId}`, () =>
    api.get<FichaJugadorDto>(`/api/clubes/${clubId}/jugadores/${miUsuarioRolId}/ficha`),
  );
  const jugador = ficha.datos;

  return (
    <Tarjeta titulo="Mi jugador">
      {ficha.error && <Aviso tono="error">{ficha.error}</Aviso>}
      {!jugador && ficha.cargando && <p className="texto-suave">Cargando…</p>}
      {jugador && (
        <div className="foto-de-ficha">
          <FotoJugador clubId={clubId} jugador={jugador} tamano="grande" />
          <div className="columna">
            <strong>{nombreCompleto(jugador)}</strong>
            <span>{jugador.categoriaAnio ? `Categoría ${jugador.categoriaAnio}` : SIN_CATEGORIA}</span>
            <span className="texto-suave">{textoDeEquipos(jugador.equipos)}</span>
            <Link to={rutaDeFicha(clubId, miUsuarioRolId)}>Abrir mi ficha</Link>
          </div>
        </div>
      )}
    </Tarjeta>
  );
}
