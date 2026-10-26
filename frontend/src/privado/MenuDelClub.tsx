import type { ClubDto, Rol } from '../compartido/api/tipos';
import { EnlaceMenu } from '../compartido/componentes/EnlaceMenu';
import {
  IconoCalendario,
  IconoCampana,
  IconoCategorias,
  IconoDatosClub,
  IconoFicha,
  IconoGoleadores,
  IconoIngresos,
  IconoInicio,
  IconoJugadores,
  IconoPagos,
  IconoPartido,
} from '../compartido/componentes/Iconos';
import { puedeVerCategorias, rutaDeFicha } from './categorias/textos';
import { puedeVerJugadores } from './jugadores/textos';
import { puedeVerPagos, rutaDeLaCuenta } from './pagos/textosPagos';

interface Props {
  clubId: string;
  nombre: string;
  /** El club tal como lo trae la API; nulo mientras carga. */
  club: ClubDto | null | undefined;
  /** El rol de la API o, mientras carga, el de la sesión. */
  rol: Rol | undefined;
}

/**
 * Menú del club en el lateral: cada entrada solo para los roles que la usan. "Comunicados" es para los
 * cuatro roles: la familia ve lo que recibió y el equipo del club, lo que publicó (spec 011). Ocultar una entrada
 * es comodidad: la API vuelve a comprobar cada permiso. No decide nada del club ni de la sesión.
 */
export function MenuDelClub({ clubId, nombre, club, rol }: Props) {
  return (
    <nav className="lateral-menu" aria-label={`Menú de ${nombre}`}>
      <EnlaceMenu a={`/club/${clubId}`} icono={<IconoInicio />} exacto>
        Inicio
      </EnlaceMenu>
      {/* Solo la cuenta de un jugador tiene ficha propia (RF-033); los demás roles no ven el enlace. */}
      {club?.miRol === 'JUGADOR' && (
        <EnlaceMenu a={rutaDeFicha(clubId, club.miUsuarioRolId)} icono={<IconoFicha />}>
          Mi ficha
        </EnlaceMenu>
      )}
      <EnlaceMenu a={`/club/${clubId}/calendario`} icono={<IconoCalendario />}>
        Calendario
      </EnlaceMenu>
      <EnlaceMenu a={`/club/${clubId}/comunicados`} icono={<IconoCampana />}>
        Comunicados
      </EnlaceMenu>
      <EnlaceMenu a={`/club/${clubId}/partidos`} icono={<IconoPartido />}>
        Partidos
      </EnlaceMenu>
      <EnlaceMenu a={`/club/${clubId}/goleadores`} icono={<IconoGoleadores />}>
        Goleadores
      </EnlaceMenu>
      {rol === 'PRESIDENTE' && (
        <EnlaceMenu a={`/club/${clubId}/ingresos`} icono={<IconoIngresos />}>
          Nuevo jugador
        </EnlaceMenu>
      )}
      {rol && puedeVerJugadores(rol) && (
        <EnlaceMenu a={`/club/${clubId}/jugadores`} icono={<IconoJugadores />}>
          Jugadores
        </EnlaceMenu>
      )}
      {rol && puedeVerCategorias(rol) && (
        <EnlaceMenu a={`/club/${clubId}/categorias`} icono={<IconoCategorias />}>
          Categorías
        </EnlaceMenu>
      )}
      {/* Pagos: la familia va a la cuenta de su jugador; el presidente y el directivo, a la sección. El entrenador no la ve (§16.9). */}
      {rol && puedeVerPagos(rol) && (
        <EnlaceMenu
          a={rol === 'JUGADOR' && club ? rutaDeLaCuenta(clubId, club.miUsuarioRolId) : `/club/${clubId}/pagos`}
          icono={<IconoPagos />}
        >
          Mensualidades
        </EnlaceMenu>
      )}
      {/* Lo que el club paga a la plataforma lo ve solo el presidente (spec 017, RF-020). */}
      {rol === 'PRESIDENTE' && (
        <EnlaceMenu a={`/club/${clubId}/pago-plataforma`} icono={<IconoPagos />}>
          Suscripción
        </EnlaceMenu>
      )}
      {rol === 'PRESIDENTE' && (
        <EnlaceMenu a={`/club/${clubId}/configuracion`} icono={<IconoDatosClub />}>
          Datos del club
        </EnlaceMenu>
      )}
    </nav>
  );
}
