import type { Rol } from '../../compartido/api/tipos';
import type { GrupoDeJugadoresDto } from '../../compartido/api/tiposJugadores';

/**
 * Textos de la lista "Jugadores" (spec 007). La columna "Al día" muestra el estado de cuenta real
 * del jugador, solo al PRESIDENTE y al DIRECTIVO (spec 015, RF-034): ya no es un espacio reservado.
 */

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
