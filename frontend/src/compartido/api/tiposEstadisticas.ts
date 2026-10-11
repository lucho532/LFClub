// Tipos de los esquemas de estadísticas y goleadores de
// specs/009-entrenamientos-y-partidos/contracts/api.yaml, con los mismos nombres.

import type { CategoriaDeActividadDto, GoleadorDePartidoDto } from './tiposActividades';

/** Cifras de un jugador, calculadas de lo registrado. */
export interface CifrasDto {
  partidosJugados: number;
  goles: number;
  entrenamientosRegistrados: number;
  entrenamientosAsistidos: number;
  /** De 0 a 100; 0 si no hay registros. */
  porcentajeAsistencia: number;
}

/** Estadísticas del jugador, en total y por cada categoría donde tenga registros. */
export interface EstadisticasJugadorDto {
  usuarioRolId: string;
  total: CifrasDto;
  /** Por año descendente; vacía si no hay registros. */
  porCategoria: (CifrasDto & { categoria: CategoriaDeActividadDto })[];
}

/** Goleadores del club por categoría. */
export interface GoleadoresDto {
  /** Categorías con al menos un gol, por año descendente. */
  categoriasConGoles: CategoriaDeActividadDto[];
  /** La elegida; ausente sin `categoriaId`. */
  categoria?: CategoriaDeActividadDto;
  /** De mayor a menor; vacía sin `categoriaId`. */
  goleadores: GoleadorDePartidoDto[];
}
