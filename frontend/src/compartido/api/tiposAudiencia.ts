// Tipos de la audiencia de los entrenamientos y de los jugadores que se suman, de
// specs/013-actividades-por-equipo/contracts/api.yaml, con los mismos nombres.
// Van aparte de tiposActividades.ts para que ningún archivo supere las 250 líneas.

import type { CategoriaDeActividadDto } from './tiposActividades';

/** Un equipo al que va dirigido un entrenamiento, o uno de los equipos de las categorías de un partido. */
export interface EquipoDeActividadDto {
  equipoId: string;
  nombre: string;
  categoriaId: string;
  anio: number;
  /** Falso si se desactivó después de dirigirle el entrenamiento (RF-026). */
  activo: boolean;
}

/** Un jugador que se puede sumar a una actividad: lo mínimo para elegirlo, sin enlace a su ficha. */
export interface JugadorSumableDto {
  usuarioRolId: string;
  nombres: string;
  apellidos: string;
  categoria: CategoriaDeActividadDto;
}

/** Lo que se envía al cambiar a quién va dirigido un entrenamiento; completo: lo que no esté, sale. */
export interface AudienciaDeEntrenamientoDto {
  /** Los equipos activos. Vacío deja el entrenamiento en toda la categoría. */
  equipoIds: string[];
  /** Los jugadores añadidos. */
  jugadorIds: string[];
}
