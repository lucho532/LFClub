import { useNavigate } from 'react-router-dom';
import type { ClubDeSesionDto, JugadorDeSesionDto } from '../compartido/api/tiposSesion';
import type { Tono } from '../compartido/componentes/Aviso';
import { Boton } from '../compartido/componentes/Boton';
import { BotonTema } from '../compartido/componentes/BotonTema';
import { EtiquetaEstado } from '../compartido/componentes/EtiquetaEstado';
import { guardarJugadorElegido } from '../compartido/sesion/jugadorElegido';
import { useSesion } from '../compartido/sesion/useSesion';
import { Escudo, IdentidadClub } from '../compartido/tema/IdentidadClub';
import { DesplegableClubes } from './DesplegableClubes';

/** El estado de un jugador de la cuenta, dicho con texto y no solo con color (RF-022). */
function estadoDe(jugador: JugadorDeSesionDto): { tono: Tono; texto: string } {
  if (jugador.retirado) {
    return { tono: 'aviso', texto: 'Retirado' };
  }

  return jugador.estadoIngreso === 'EN_ESPERA'
    ? { tono: 'info', texto: 'Pendiente de aprobación' }
    : { tono: 'exito', texto: 'Activo' };
}

/**
 * Lo primero que ve en un club la familia que entró con el correo y tiene varios jugadores en él
 * (RF-021): la lista de sus jugadores, con el estado de cada uno, para elegir con cuál continúa.
 * No pide nada al club: la lista ya viene en la sesión. Conserva el tema, el cambio de club y el
 * cierre de sesión. Al elegir, guarda la elección en la pestaña y entra al inicio del club.
 */
export function ElegirJugador({ club }: { club: ClubDeSesionDto }) {
  const { cerrar } = useSesion();
  const navegar = useNavigate();

  function elegir(jugador: JugadorDeSesionDto) {
    guardarJugadorElegido(club.clubId, jugador.usuarioRolId);
    navegar(`/club/${club.clubId}`, { replace: true });
  }

  function cerrarSesion() {
    cerrar();
    navegar('/entrar', { replace: true });
  }

  return (
    <IdentidadClub identidad={club.identidad} className="centrado">
      <header className="columna">
        <div className="cabecera">
          <Escudo identidad={club.identidad} nombre={club.nombre} />
          <BotonTema />
        </div>
        <h1>{club.nombre}</h1>
      </header>
      <main className="tarjeta">
        <h2>¿Con cuál jugador quieres continuar?</h2>
        <p className="texto-suave">Verás solo la información del jugador que elijas. Puedes cambiar cuando quieras.</p>
        <ul className="jugadores-a-elegir">
          {club.jugadores.map((jugador) => (
            <li key={jugador.usuarioRolId}>
              <button type="button" className="jugador-a-elegir" onClick={() => elegir(jugador)}>
                <span>
                  {jugador.nombres} {jugador.apellidos}
                </span>
                <EtiquetaEstado {...estadoDe(jugador)} />
              </button>
            </li>
          ))}
        </ul>
        <DesplegableClubes clubId={club.clubId} />
        <Boton variante="secundario" onClick={cerrarSesion}>
          Cerrar sesión
        </Boton>
      </main>
    </IdentidadClub>
  );
}
