import type { ClubDto, Rol } from '../compartido/api/tipos';
import { EnlaceMenu } from '../compartido/componentes/EnlaceMenu';
import {
  IconoCalendario,
  IconoCategorias,
  IconoDatosClub,
  IconoFicha,
  IconoIngresos,
  IconoInicio,
  IconoJugadores,
} from '../compartido/componentes/Iconos';
import { puedeVerCategorias, rutaDeFicha } from './categorias/textos';
import { puedeVerJugadores } from './jugadores/textos';

interface Props {
  clubId: string;
  nombre: string;
  /** El club tal como lo trae la API; nulo mientras carga. */
  club: ClubDto | null | undefined;
  /** El rol de la API o, mientras carga, el de la sesión. */
  rol: Rol | undefined;
}

/**
 * Menú del club en el lateral: cada entrada solo para los roles que la usan. Ocultar una entrada
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
      {rol === 'PRESIDENTE' && (
        <EnlaceMenu a={`/club/${clubId}/ingresos`} icono={<IconoIngresos />}>
          Ingresos
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
      {rol === 'PRESIDENTE' && (
        <EnlaceMenu a={`/club/${clubId}/configuracion`} icono={<IconoDatosClub />}>
          Datos del club
        </EnlaceMenu>
      )}
    </nav>
  );
}
