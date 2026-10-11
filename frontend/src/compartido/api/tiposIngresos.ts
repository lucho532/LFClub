// Tipos de la sala de espera y de los ingresos aprobados: los esquemas de
// specs/002-ingreso-club/contracts/api.yaml, con los cambios de la 004 y la 006 y los campos que
// añade specs/008-fichas-de-personas/contracts/api.yaml. Van aparte de tipos.ts para que ningún
// archivo supere las 250 líneas.

import type { RolDeIngreso, TipoDocumento } from './tipos';
import type { EnlaceFichaDto } from './tiposPersonas';

export interface IngresoEnEsperaDto {
  usuarioRolId: string;
  nombres: string;
  apellidos: string;
  tipoDocumento: TipoDocumento;
  numeroDocumento: string;
  fechaNacimiento: string;
  correo: string;
  celular: string;
  nombreResponsable: string | null;
  registradoEn: string;
  /** Jugador desde cuya ficha se agregó; `null` si ya no existe o no viene de un hermano. */
  hermanoDe: string | null;
  /** Si aprobarlo retirará a un jugador de otra cuenta en otro club; no dice cuál. */
  retiraDeOtroClub: boolean;
  /** Ficha del hermano si hoy es un jugador aprobado; si no, `null` (spec 008). */
  hermanoDeFicha: EnlaceFichaDto | null;
}

export interface IngresoAprobadoDto {
  usuarioRolId: string;
  nombres: string;
  apellidos: string;
  rolDeIngreso: RolDeIngreso;
  aprobadoPor: string;
  aprobadoEn: string;
  /** Ficha actual de la persona aprobada: la del jugador o la básica (spec 008). */
  ficha: EnlaceFichaDto | null;
  /** Ficha básica de quien aprobó; `null` si ya no está en el club (spec 008). */
  aprobadoPorFicha: EnlaceFichaDto | null;
}
