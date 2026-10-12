// Tipos de la carta del jugador de specs/010-ficha-resultado-y-carta/contracts/api.yaml, con los
// mismos nombres. Lo que el contrato omite cuando no hay es opcional.

import type { UltimoCambioDto } from './tiposFicha';

export type PosicionJugador = 'PORTERO' | 'DEFENSA' | 'MEDIOCAMPISTA' | 'DELANTERO';

/** Se calcula por la asistencia a los entrenamientos de la temporada. */
export type RarezaCarta = 'BRONCE' | 'PLATA' | 'ORO';

/** Las seis estadísticas de una carta que las tiene. */
export interface ValoresCartaDto {
  ritmo: number;
  tiro: number;
  pase: number;
  regate: number;
  defensa: number;
  fisico: number;
}

/** Lo que se envía: las seis enteras de 1 a 99, o las seis nulas para dejarla sin números. */
export interface EstadisticasCartaDto {
  ritmo: number | null;
  tiro: number | null;
  pase: number | null;
  regate: number | null;
  defensa: number | null;
  fisico: number | null;
}

/** Dorsal de 1 a 99 o nulo; posición de la lista o nula. */
export interface DorsalYPosicionDto {
  dorsal: number | null;
  posicion: PosicionJugador | null;
}

/** Lo que quien pregunta puede hacer con la carta. La API lo vuelve a comprobar. */
export interface PermisosDeCartaDto {
  cambiaEstadisticas: boolean;
  cambiaDorsalYPosicion: boolean;
  exporta: boolean;
}

/** La carta de un solo jugador. Escudo y colores son los de la identidad del club. */
export interface CartaDto {
  usuarioRolId: string;
  nombres: string;
  apellidos: string;
  /** Ausente si no tiene foto: figura genérica. */
  fotoActualizadaEn?: string;
  dorsal?: number;
  posicion?: PosicionJugador;
  estadisticas?: ValoresCartaDto;
  /** Ausente sin estadísticas. */
  valoracion?: number;
  rareza: RarezaCarta;
  permisos: PermisosDeCartaDto;
  /** Solo si `permisos.cambiaEstadisticas`. */
  ultimoCambioEstadisticas?: UltimoCambioDto;
}
