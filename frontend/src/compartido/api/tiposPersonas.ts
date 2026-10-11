// Tipos de los esquemas de specs/008-fichas-de-personas/contracts/api.yaml, con los mismos nombres.
// Van aparte de tipos.ts para que ningún archivo supere las 250 líneas.

import type { Rol, TipoDocumento } from './tipos';

/** A qué ficha lleva un nombre: la del jugador (005 y 007) o la ficha básica. */
export type TipoFicha = 'JUGADOR' | 'BASICA';

/**
 * A dónde lleva un nombre. La API lo pone solo cuando quien mira puede abrir esa ficha; `null` si
 * no puede o si la persona ya no está en el club. No concede acceso: la ficha se vuelve a comprobar.
 */
export interface EnlaceFichaDto {
  usuarioRolId: string;
  tipo: TipoFicha;
}

/** Una categoría activa que entrena, con los nombres de los equipos activos que dirige en ella. */
export interface CategoriaDeFichaBasicaDto {
  anio: number;
  /** Ordenados por nombre; vacía si lo es de la categoría en general. */
  equipos: string[];
}

/**
 * Ficha básica de un presidente, directivo o entrenador: sin documento, fecha de nacimiento,
 * correo ni celular.
 */
export interface FichaBasicaDto {
  usuarioRolId: string;
  nombres: string;
  apellidos: string;
  /** PRESIDENTE, DIRECTIVO o ENTRENADOR; nunca JUGADOR. */
  rol: Exclude<Rol, 'DESARROLLADOR' | 'JUGADOR'>;
  /** 0 si no tiene foto de perfil; cambia cada vez que la reemplaza. */
  versionFoto: number;
  /** Ordenadas por año; vacía si no entrena. */
  categorias: CategoriaDeFichaBasicaDto[];
}

/** Datos propios de la cuenta con sesión, para "Mi perfil"; documento y correo de solo lectura. */
export interface DatosPropiosDto {
  nombres: string;
  apellidos: string;
  tipoDocumento: TipoDocumento;
  numeroDocumento: string;
  fechaNacimiento: string;
  correo: string;
  celular: string;
}

/** Lo único que la persona cambia de sí misma: sin documento ni correo. */
export interface CambiarDatosPropiosDto {
  nombres: string;
  apellidos: string;
  fechaNacimiento: string;
  celular: string;
}
