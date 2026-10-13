import { Link } from 'react-router-dom';
import type { JugadorQueSeElimina } from '../compartido/api/tiposCuenta';
import type { ClubDeSesionDto } from '../compartido/api/tiposSesion';
import { useSesion } from '../compartido/sesion/useSesion';

interface Props {
  jugadores: JugadorQueSeElimina[];
}

/** Un jugador de la sesión al que se le puede ofrecer retirarlo del club como alternativa. */
interface Retirable {
  clubId: string;
  clubNombre: string;
  usuarioRolId: string;
  nombre: string;
}

/**
 * Los jugadores que se irían con la cuenta, por nombre y club, y el estado dicho en texto
 * (retirado, en espera). Debajo, como alternativa, el camino a la ficha de cada uno para retirarlo
 * del club sin eliminar la cuenta (spec 014, RF-003 y RF-016). Sin documento ni datos de contacto.
 */
export function JugadoresQueSeEliminan({ jugadores }: Props) {
  const { sesion } = useSesion();
  const retirables = retirablesDe(sesion?.clubes ?? []);

  if (jugadores.length === 0) {
    return null;
  }

  return (
    <section className="tarjeta columna" aria-labelledby="jugadores-que-se-eliminan">
      <h2 id="jugadores-que-se-eliminan">Jugadores que se eliminan con tu cuenta</h2>
      <p>
        Esta cuenta tiene {jugadores.length === 1 ? 'un jugador' : `${jugadores.length} jugadores`}. Se
        eliminan con ella, en todos los clubes:
      </p>
      <ul>
        {jugadores.map((jugador, posicion) => (
          <li key={`${jugador.nombreCompleto}-${jugador.clubNombre}-${posicion}`}>
            <strong>{jugador.nombreCompleto}</strong> — {jugador.clubNombre}
            {jugador.retirado && ' (retirado del club)'}
            {jugador.enEspera && ' (en espera de aprobación)'}
          </li>
        ))}
      </ul>
      {retirables.length > 0 && (
        <>
          <p>
            ¿Solo quieres que un jugador deje un club? Puedes retirarlo desde su ficha, sin eliminar la
            cuenta:
          </p>
          <ul>
            {retirables.map((jugador) => (
              <li key={jugador.usuarioRolId}>
                <Link to={`/club/${jugador.clubId}/jugadores/${jugador.usuarioRolId}/ficha`}>
                  Retirar del club
                </Link>{' '}
                a {jugador.nombre} de {jugador.clubNombre}
              </li>
            ))}
          </ul>
        </>
      )}
    </section>
  );
}

function retirablesDe(clubes: ClubDeSesionDto[]): Retirable[] {
  return clubes
    .filter((club) => club.rol === 'JUGADOR')
    .flatMap((club) =>
      (club.jugadores.length > 0
        ? club.jugadores
        : [{ usuarioRolId: club.usuarioRolId, nombres: club.nombres, apellidos: club.apellidos }]
      ).map((jugador) => ({
        clubId: club.clubId,
        clubNombre: club.nombre,
        usuarioRolId: jugador.usuarioRolId,
        nombre: `${jugador.nombres} ${jugador.apellidos}`.trim(),
      })),
    );
}
