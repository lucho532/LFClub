import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import type { ClubDeSesionDto } from '../compartido/api/tiposSesion';
import { Aviso } from '../compartido/componentes/Aviso';
import { Boton } from '../compartido/componentes/Boton';
import { BotonTema } from '../compartido/componentes/BotonTema';
import { jugadorDe } from '../compartido/sesion/jugadorElegido';
import { useSesion } from '../compartido/sesion/useSesion';
import { Escudo, IdentidadClub } from '../compartido/tema/IdentidadClub';
import { AvisoClubNoDisponible } from './AvisoClubNoDisponible';
import { BotonCambiarJugador } from './BotonCambiarJugador';
import { DesplegableClubes } from './DesplegableClubes';

/**
 * Lo único que ve de un club quien tiene su ingreso en espera: el nombre y la identidad del club,
 * que ya trae la sesión, y el aviso de que su ingreso está pendiente. Quien se registra con una
 * invitación no pasa por aquí: entra directamente. Es lo que ve la familia cuando elige al
 * jugador que agregó desde la ficha de un hermano, hasta que el presidente lo apruebe. No tiene
 * menú y no pide nada al club: la API se lo negaría. Conserva el tema, el cambio de club, el
 * cierre de sesión y, si la familia entró con el correo, "Cambiar de jugador" (RF-013).
 * "Actualizar" recarga la sesión; si ya fue aprobado, entra al club.
 * Si el club está suspendido o dado de baja muestra ese aviso, como a cualquier otro integrante.
 */
export function SalaDeEspera({ club }: { club: ClubDeSesionDto }) {
  const { sesion, recargar, cerrar } = useSesion();
  const navegar = useNavigate();
  const [actualizando, setActualizando] = useState(false);
  const [sinCambios, setSinCambios] = useState(false);

  async function actualizar() {
    setActualizando(true);
    setSinCambios(false);
    const actual = await recargar();
    // Si la aprobaron, esta pantalla deja de mostrarse; si sigue aquí, se le dice que nada cambió.
    // Se mira al jugador elegido, no al más antiguo de la cuenta, que puede estar ya aprobado.
    const delClub = actual?.clubes.find((candidato) => candidato.clubId === club.clubId);
    const sigueEnEspera = delClub !== undefined && (jugadorDe(delClub) ?? delClub).estadoIngreso === 'EN_ESPERA';
    setSinCambios(sigueEnEspera);
    setActualizando(false);
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
        {club.estado === 'ACTIVO' ? (
          <>
            <h2>El ingreso de {club.nombres} está pendiente de aprobación</h2>
            <p>
              {club.nombres} ya está registrado en {club.nombre}. Cuando el club apruebe su ingreso podrá
              entrar; no hay que registrarlo de nuevo.
            </p>
            {sinCambios && <Aviso tono="info">Su ingreso sigue pendiente de aprobación.</Aviso>}
            <Boton onClick={() => void actualizar()} cargando={actualizando} textoCargando="Actualizando…">
              Actualizar
            </Boton>
          </>
        ) : (
          <AvisoClubNoDisponible
            codigo={club.estado === 'SUSPENDIDO' ? 'club_suspendido' : 'club_dado_de_baja'}
            tieneOtrosClubes={(sesion?.clubes.length ?? 0) > 1}
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
