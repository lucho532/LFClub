import { useNavigate } from 'react-router-dom';
import type { ClubDeSesionDto } from '../compartido/api/tiposSesion';
import { Aviso } from '../compartido/componentes/Aviso';
import { Boton } from '../compartido/componentes/Boton';
import { BotonTema } from '../compartido/componentes/BotonTema';
import { useSesion } from '../compartido/sesion/useSesion';
import { Escudo, IdentidadClub } from '../compartido/tema/IdentidadClub';
import { AvisoClubNoDisponible } from './AvisoClubNoDisponible';
import { BotonCambiarJugador } from './BotonCambiarJugador';
import { DesplegableClubes } from './DesplegableClubes';

/**
 * Lo único que ve de un club el jugador retirado de él (RF-043), lo haya retirado el club, su
 * familia o la aprobación de un hermano en otro club: el nombre y la identidad del club, que ya
 * trae la sesión, y el aviso de que ya no está en él. No tiene menú y no pide
 * nada al club: la API se lo negaría. Conserva el tema, el cambio de club, porque el retiro es de
 * cada club, el cierre de sesión y, si la familia tiene más jugadores y entró con el correo,
 * "Cambiar de jugador", porque el retiro es de cada jugador. Si el club está suspendido o dado de baja muestra ese aviso,
 * como a cualquier otro integrante.
 */
export function AvisoRetirado({ club }: { club: ClubDeSesionDto }) {
  const { sesion, cerrar } = useSesion();
  const navegar = useNavigate();
  const tieneOtrosClubes = (sesion?.clubes.length ?? 0) > 1;

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
        {club.estado === 'ACTIVO' ? (
          <>
            <h2>Ya no estás en este club</h2>
            <Aviso tono="aviso">
              {club.nombres} ya no está en {club.nombre}. Si crees que es un error, comunícate con el presidente del
              club.
            </Aviso>
            {tieneOtrosClubes && <p className="texto-suave">Puedes pasar a otro de tus clubes con el desplegable.</p>}
          </>
        ) : (
          <AvisoClubNoDisponible
            codigo={club.estado === 'SUSPENDIDO' ? 'club_suspendido' : 'club_dado_de_baja'}
            tieneOtrosClubes={tieneOtrosClubes}
          />
        )}
        <BotonCambiarJugador club={club} />
        <DesplegableClubes clubId={club.clubId} />
        <Boton variante="secundario" onClick={cerrarSesion}>
          Cerrar sesión
        </Boton>
      </main>
    </IdentidadClub>
  );
}
