import { Link } from 'react-router-dom';
import { api } from '../../compartido/api/cliente';
import type { FichaJugadorDto } from '../../compartido/api/tiposFicha';
import { useCarga } from '../../compartido/api/useCarga';
import { Aviso } from '../../compartido/componentes/Aviso';
import { NombrePersona } from '../../compartido/componentes/NombrePersona';
import { Tarjeta } from '../../compartido/componentes/Tarjeta';
import { nombreCompleto, rutaDeFicha } from '../categorias/textos';
import { SeccionFoto } from '../ficha/SeccionFoto';
import { SIN_CATEGORIA, textoDeEquipos } from '../jugadores/textos';

interface Props {
  clubId: string;
  /** El jugador con el que continúa la familia: el de la petición. */
  miUsuarioRolId: string;
}

/**
 * Tarjeta del inicio de la familia (spec 007, RF-013): la foto de su jugador, o sus iniciales, su
 * nombre, su categoría y sus equipos, y la entrada a su ficha. Quien puede cambiar la ficha cambia o
 * quita la foto desde el lápiz del círculo, igual que en la ficha (`SeccionFoto`). Lee la ficha del jugador elegido,
 * sin pedir nada nuevo a la API; con varios hermanos, es la del que eligió al entrar. El nombre
 * también abre "Mi ficha" (spec 008).
 */
export function TarjetaMiJugador({ clubId, miUsuarioRolId }: Props) {
  const ficha = useCarga(`${clubId}/ficha/${miUsuarioRolId}`, () =>
    api.get<FichaJugadorDto>(`/api/clubes/${clubId}/jugadores/${miUsuarioRolId}/ficha`),
  );
  const jugador = ficha.datos;
  const rutaDeLaFicha = `/api/clubes/${clubId}/jugadores/${miUsuarioRolId}/ficha`;

  return (
    <Tarjeta titulo="Mi jugador">
      {ficha.error && <Aviso tono="error">{ficha.error}</Aviso>}
      {!jugador && ficha.cargando && <p className="texto-suave">Cargando…</p>}
      {jugador && (
        <SeccionFoto clubId={clubId} ficha={jugador} rutaDeLaFicha={rutaDeLaFicha} alCambiar={ficha.fijar}>
          <strong>
            <NombrePersona
              nombre={nombreCompleto(jugador)}
              enlace={{ usuarioRolId: miUsuarioRolId, tipo: 'JUGADOR' }}
              clubId={clubId}
              textoVolver="Inicio"
            />
          </strong>
          <span>{jugador.categoriaAnio ? `Categoría ${jugador.categoriaAnio}` : SIN_CATEGORIA}</span>
          <span className="texto-suave">{textoDeEquipos(jugador.equipos)}</span>
          <Link to={rutaDeFicha(clubId, miUsuarioRolId)}>Abrir mi ficha</Link>
        </SeccionFoto>
      )}
    </Tarjeta>
  );
}
