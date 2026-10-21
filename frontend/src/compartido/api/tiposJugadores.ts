// Tipos de los esquemas de specs/007-jugadores-inicio-por-rol/contracts/api.yaml, con los mismos
// nombres. Van aparte de tipos.ts para que ningún archivo supere las 250 líneas.
import type { EstadoDeCuenta } from './tiposPagos';

/** La categoría de un grupo de la lista de jugadores. */
export interface CategoriaDeGrupoDto {
  categoriaId: string;
  anio: number;
}

/**
 * Un jugador en la lista. No lleva documento, contacto, salud, documentación ni su saldo. Lleva su
 * estado de cuenta solo para el presidente y el directivo (spec 015): para el entrenador va `null`.
 */
export interface JugadorDeListaDto {
  usuarioRolId: string;
  nombres: string;
  apellidos: string;
  /** Equipos activos en los que juega, por nombre; vacía es "Sin equipo". */
  equipos: string[];
  /** Fecha UTC de la última carga de su foto; `null` si no tiene. */
  fotoActualizadaEn: string | null;
  /** Al día, pago parcial o debe; `null` (o ausente) si quien pregunta no ve las finanzas. */
  estadoCuenta?: EstadoDeCuenta | null;
}

/** Un grupo de la lista: una categoría o, con `categoria: null`, "Sin categoría". */
export interface GrupoDeJugadoresDto {
  categoria: CategoriaDeGrupoDto | null;
  jugadores: JugadorDeListaDto[];
}

/** Las cuatro cifras del inicio del presidente y de los directivos. */
export interface ResumenClubDto {
  jugadores: number;
  equipos: number;
  entrenadores: number;
  categorias: number;
}
