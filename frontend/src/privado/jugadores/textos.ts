import type { Rol } from '../../compartido/api/tipos';
import type { GrupoDeJugadoresDto } from '../../compartido/api/tiposJugadores';

/**
 * Textos de la lista "Jugadores" (spec 007). "Al día" es un espacio reservado: los pagos todavía no
 * existen, así que el texto es el mismo para todos y no puede leerse como un estado (RF-006, CE-007).
 */

/** Lo que dice la columna "Al día" en todas las filas (supuesto 5 del plan). */
export const TEXTO_AL_DIA = 'Sin datos aún';

/** Explicación accesible de la columna "Al día". */
export const TITULO_AL_DIA = 'El estado de pago todavía no está disponible';

export const SIN_EQUIPO = 'Sin equipo';

export const SIN_CATEGORIA = 'Sin categoría';

/** Lo que ve un entrenador sin categorías asignadas, en la lista y en su inicio (RF-012). */
export const SIN_CATEGORIAS_ASIGNADAS = 'Todavía no tienes categorías asignadas.';

/** Roles que tienen el apartado "Jugadores" (RF-001); la API rechaza a los demás. */
export function puedeVerJugadores(rol: Rol): boolean {
  return rol === 'PRESIDENTE' || rol === 'DIRECTIVO' || rol === 'ENTRENADOR';
}

/** Los equipos de un jugador separados por coma, o "Sin equipo". */
export function textoDeEquipos(equipos: string[]): string {
  return equipos.length === 0 ? SIN_EQUIPO : equipos.join(', ');
}

/** "Categoría 2014" o "Sin categoría". */
export function tituloDeGrupo(grupo: GrupoDeJugadoresDto): string {
  return grupo.categoria ? `Categoría ${grupo.categoria.anio}` : SIN_CATEGORIA;
}
