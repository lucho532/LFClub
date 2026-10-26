import { Tarjeta } from '../compartido/componentes/Tarjeta';
import type { PropsSeccion } from './DetalleClub';

/**
 * Cantidad de integrantes activos del club por rol, en el detalle del club (la lista de clubes ya no
 * los muestra). Son solo números: no se ve a quién corresponde cada uno.
 */
export function SeccionIntegrantes({ club }: PropsSeccion) {
  // Una API anterior a este campo no lo envía: la sección se omite en vez de romper la pantalla.
  if (!club.integrantes) {
    return null;
  }

  const { jugadores, entrenadores, directivos, presidentes } = club.integrantes;

  return (
    <Tarjeta titulo="Integrantes">
      <ul className="lista-de-inicio">
        <li>
          Jugadores: <strong>{jugadores}</strong>
        </li>
        <li>
          Entrenadores: <strong>{entrenadores}</strong>
        </li>
        <li>
          Directivos: <strong>{directivos}</strong>
        </li>
        <li>
          Presidentes: <strong>{presidentes}</strong>
        </li>
      </ul>
      <p>
        Total: <strong>{jugadores + entrenadores + directivos + presidentes}</strong>
      </p>
    </Tarjeta>
  );
}
