import { useEffect } from 'react';
import { Outlet, useNavigate, useParams } from 'react-router-dom';
import { api } from '../compartido/api/cliente';
import { ErrorApi } from '../compartido/api/errores';
import type { ClubDto } from '../compartido/api/tipos';
import type { ClubDeSesionDto } from '../compartido/api/tiposSesion';
import { useCarga } from '../compartido/api/useCarga';
import { Aviso } from '../compartido/componentes/Aviso';
import { inicialesDe } from '../compartido/componentes/Avatar';
import { BarraSuperior } from '../compartido/componentes/BarraSuperior';
import { CampanaComunicados } from '../compartido/componentes/CampanaComunicados';
import { MenuPerfil } from '../compartido/componentes/MenuPerfil';
import { nombreDeRol } from '../compartido/formato';
import { jugadorDe, olvidarJugadorElegido, useJugadorElegido } from '../compartido/sesion/jugadorElegido';
import { guardarUltimoClub } from '../compartido/sesion/ultimoClub';
import { useSesion } from '../compartido/sesion/useSesion';
import { Escudo, IdentidadClub } from '../compartido/tema/IdentidadClub';
import { AvisoClubNoDisponible, esClubNoDisponible } from './AvisoClubNoDisponible';
import { AvisoRetirado } from './AvisoRetirado';
import { BotonCambiarJugador } from './BotonCambiarJugador';
import type { ContextoDelClub } from './contextoClub';
import { DesplegableClubes } from './DesplegableClubes';
import { ProveedorContadorComunicados, useContadorDelClub } from './comunicados/ContadorComunicados';
import { ElegirJugador } from './ElegirJugador';
import { MenuDelClub } from './MenuDelClub';
import { SalaDeEspera } from './SalaDeEspera';

/** Códigos con los que la API dice que la sesión guardada ya no refleja la relación con el club. */
const SESION_DESACTUALIZADA = ['ingreso_en_espera', 'integrante_retirado', 'no_encontrado', 'jugador_sin_elegir'];

/** Códigos con los que la API dice que el jugador elegido ya no vale, o que ahora hay que elegir. */
const ELECCION_DESACTUALIZADA = ['no_encontrado', 'jugador_sin_elegir'];

/**
 * Entrada al club elegido. Si la cuenta tiene varios jugadores en él y todavía no eligió con cuál
 * continúa, muestra la lista antes que cualquier otra cosa y sin pedir nada al club (RF-021). Con
 * el jugador elegido, lo que la sesión dice del integrante pasa a ser lo de ese jugador. Después
 * mira su estado de ingreso: quien está en espera ve solo la sala de espera y no se llama a la API
 * del club, que se lo negaría. Lo mismo quien fue retirado del club: solo ve el aviso de que ya no
 * está en él. Los demás, también quien acaba de registrarse con una invitación, ven la aplicación
 * del club, que se monta de nuevo al cambiar de jugador para que no quede nada del anterior.
 */
export function DisposicionClub() {
  const { clubId = '' } = useParams();
  const { sesion } = useSesion();
  const deLaCuenta = sesion?.clubes.find((candidato) => candidato.clubId === clubId);

  // Solo para volver a pintar cuando la familia elige o cambia de jugador.
  useJugadorElegido(clubId);
  const elegido = deLaCuenta ? jugadorDe(deLaCuenta) : null;

  useEffect(() => {
    guardarUltimoClub(clubId);
  }, [clubId]);

  if (deLaCuenta && deLaCuenta.jugadores.length > 0 && !elegido) {
    return <ElegirJugador club={deLaCuenta} />;
  }

  const deSesion = deLaCuenta && elegido ? { ...deLaCuenta, ...elegido } : deLaCuenta;

  if (deSesion?.estadoIngreso === 'EN_ESPERA') {
    return <SalaDeEspera club={deSesion} />;
  }

  // Igual con quien fue retirado del club: solo ve el aviso, sin pedir nada al club (RF-043).
  if (deSesion?.retirado) {
    return <AvisoRetirado club={deSesion} />;
  }

  // El contador de comunicados es de la cuenta: sobrevive al cambio de jugador y lo comparten la
  // campana de la barra y las pantallas de comunicados.
  return (
    <ProveedorContadorComunicados clubId={clubId} activo={deSesion?.rol === 'JUGADOR'}>
      <AplicacionDelClub key={deSesion?.usuarioRolId} clubId={clubId} deSesion={deSesion} />
    </ProveedorContadorComunicados>
  );
}

interface Props {
  clubId: string;
  /** El club tal como lo trae la sesión, para que su nombre nunca falte mientras llega de la API. */
  deSesion: ClubDeSesionDto | undefined;
}

/**
 * Armazón de la aplicación del club elegido: menú lateral con el escudo, el nombre del club
 * siempre visibles (constitución §7.2) y la navegación; barra superior con el tema y el perfil,
 * desde donde se cambia de jugador y se cierra sesión; y el escudo de fondo como marca de agua. Al cambiar de club en el
 * desplegable cambia la identidad. La cuenta de un jugador tiene además en la barra la campana de los
 * comunicados (spec 011), con el contador de sin leer que comparte con las pantallas de comunicados. Pide
 * el club a la API en cada entrada: es la API
 * la que decide si la persona pertenece a él y si el club está disponible.
 */
function AplicacionDelClub({ clubId, deSesion }: Props) {
  const { sesion, cerrar, recargar } = useSesion();
  const navegar = useNavigate();
  const { datos: club, error, fallo, cargando, fijar } = useCarga(clubId, () => api.get<ClubDto>(`/api/clubes/${clubId}`));
  const codigo = fallo instanceof ErrorApi ? fallo.codigo : undefined;
  const sesionDesactualizada = codigo !== undefined && SESION_DESACTUALIZADA.includes(codigo);

  // La API manda: si dice que la persona está en espera o que el club ya no es suyo (la
  // rechazaron con la pantalla abierta), se recarga la sesión. Con ella al día, quien sigue en
  // espera ve la sala de espera y quien ya no está en el club pasa a uno de los suyos, o a
  // iniciar sesión si su cuenta ya no existe. Si lo que ya no vale es el jugador elegido, o la
  // cuenta pasó a tener varios, se olvida la elección: vuelve a la lista o entra con el que queda.
  useEffect(() => {
    if (sesionDesactualizada) {
      if (codigo && ELECCION_DESACTUALIZADA.includes(codigo)) {
        olvidarJugadorElegido(clubId);
      }

      void recargar();
    }
  }, [sesionDesactualizada, codigo, clubId, recargar]);

  const nombre = club?.nombre ?? deSesion?.nombre ?? '';
  const rol = club?.miRol ?? deSesion?.rol;
  const identidad = club?.identidad ?? deSesion?.identidad;

  function cerrarSesion() {
    cerrar();
    navegar('/entrar', { replace: true });
  }

  const contexto: ContextoDelClub | null = club ? { club, fijarClub: fijar } : null;

  return (
    <IdentidadClub identidad={identidad} className="disposicion">
      <aside className="lateral">
        <div className="lateral-marca">
          <Escudo identidad={identidad} nombre={nombre} />
          <span>{nombre}</span>
        </div>
        <div className="solo-escritorio">
          <DesplegableClubes clubId={clubId} />
        </div>
        <MenuDelClub clubId={clubId} nombre={nombre} club={club} rol={rol} />
      </aside>
      <div className="contenido">
        <BarraSuperior
          marca={
            <>
              <Escudo identidad={identidad} nombre={nombre} />
              <span>{nombre}</span>
            </>
          }
        >
          {/* La campana de los comunicados: solo la cuenta de un jugador la tiene (spec 011, RF-019). */}
          {rol === 'JUGADOR' && <CampanaDelClub clubId={clubId} />}
          {/* El nombre es el del jugador elegido cuando la cuenta tiene varios (RF-024): entonces
              se queda a la vista también en el teléfono. */}
          <MenuPerfil
            nombre={deSesion ? `${deSesion.nombres} ${deSesion.apellidos}` : ''}
            detalle={rol ? nombreDeRol(rol) : undefined}
            iniciales={deSesion ? inicialesDe(deSesion.nombres, deSesion.apellidos) : ''}
            alCerrarSesion={cerrarSesion}
            nombreSiempreVisible={(deSesion?.jugadores.length ?? 0) > 0}
            opciones={
              <>
                {/* En el teléfono no hay menú lateral: el desplegable de clubes va aquí. */}
                <div className="solo-movil">
                  <DesplegableClubes clubId={clubId} />
                </div>
                {deSesion && <BotonCambiarJugador club={deSesion} variante="menu" />}
              </>
            }
          />
        </BarraSuperior>
        <main className="principal">
          {esClubNoDisponible(codigo) ? (
            <AvisoClubNoDisponible codigo={codigo} tieneOtrosClubes={(sesion?.clubes.length ?? 0) > 1} />
          ) : (
            error && !sesionDesactualizada && <Aviso tono="error">{error}</Aviso>
          )}
          {!contexto && (cargando || sesionDesactualizada) && <p className="texto-suave">Cargando…</p>}
          {club?.estado === 'SUSPENDIDO' && (
            <Aviso tono="aviso">
              <strong>Club suspendido.</strong> Solo tú, como presidente, puedes entrar; los demás integrantes
              ven un aviso de incidencia temporal.
            </Aviso>
          )}
          {contexto && <Outlet context={contexto} />}
        </main>
      </div>
    </IdentidadClub>
  );
}

/** La campana de la barra superior, con el contador que comparten las pantallas de comunicados. */
function CampanaDelClub({ clubId }: { clubId: string }) {
  const { sinLeer } = useContadorDelClub();
  return <CampanaComunicados sinLeer={sinLeer} a={`/club/${clubId}/comunicados`} />;
}
