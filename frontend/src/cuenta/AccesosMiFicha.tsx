import { Link } from 'react-router-dom';
import type { ClubDeSesionDto } from '../compartido/api/tiposSesion';
import { jugadorDe } from '../compartido/sesion/jugadorElegido';

/**
 * Dónde está "Mi ficha" para la cuenta de un jugador (spec 008, RF-017): sus datos personales se
 * cambian en la ficha de cada jugador, no en "Mi perfil". Por cada club donde la cuenta es un
 * jugador aprobado y no retirado, un acceso a la ficha del jugador elegido o del único; si tiene
 * varios y todavía no eligió, lleva al club, que le pide elegir.
 */
export function AccesosMiFicha({ clubes }: { clubes: ClubDeSesionDto[] }) {
  const deJugador = clubes.filter(
    (club) => club.rol === 'JUGADOR' && club.estadoIngreso === 'APROBADO' && !club.retirado,
  );

  if (deJugador.length === 0) {
    return null;
  }

  return (
    <section className="tarjeta">
      <h2>Mi ficha</h2>
      <p className="texto-suave">Los datos del jugador se cambian en su ficha.</p>
      <ul className="lista-de-inicio">
        {deJugador.map((club) => {
          const elegido = club.jugadores.length === 0 ? club.usuarioRolId : jugadorDe(club)?.usuarioRolId;
          return (
            <li key={club.clubId}>
              <Link to={elegido ? `/club/${club.clubId}/jugadores/${elegido}/ficha` : `/club/${club.clubId}`}>
                Mi ficha
              </Link>
              <span className="texto-suave nombre-persona">{club.nombre}</span>
            </li>
          );
        })}
      </ul>
    </section>
  );
}
