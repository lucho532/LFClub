// Tipos de los esquemas de entrenamientos, partidos y calendario de
// specs/009-entrenamientos-y-partidos/contracts/api.yaml, con los mismos nombres.
// Van aparte de tipos.ts para que ningún archivo supere las 250 líneas.

import type { UltimoCambioDto } from './tiposFicha';
import type { EnlaceFichaDto } from './tiposPersonas';

/** Un entrenamiento sigue en pie o se canceló; cancelado, se puede reactivar si no ha empezado. */
export type EstadoEntrenamiento = 'PROGRAMADO' | 'CANCELADO';

/** Un partido próximo solo lo ven sus convocados, quienes lo entrenan y el presidente. */
export type EstadoPartido = 'PROXIMO' | 'CANCELADO' | 'JUGADO';

export type CondicionPartido = 'LOCAL' | 'VISITANTE';

export type TipoPartido = 'AMISTOSO' | 'TORNEO';

/** Lo que responde la familia; no responder es no tener respuesta. */
export type RespuestaFamilia = 'ASISTIRA' | 'NO_ASISTIRA';

/** Una categoría de una actividad, con su año. */
export interface CategoriaDeActividadDto {
  categoriaId: string;
  anio: number;
}

/** Lo que quien pregunta puede hacer con la actividad. La API lo vuelve a comprobar. */
export interface PermisosDeActividadDto {
  /** Cambiar, cancelar, reactivar, convocar y registrar el resultado. */
  gestiona: boolean;
  /** Pedir la planilla (`.../jugadores`). */
  vePlanilla: boolean;
  /** Es la cuenta de un jugador que todavía puede responder. */
  responde: boolean;
}

/** Lo que se envía al programar o cambiar un entrenamiento. Fechas `AAAA-MM-DD` y horas `HH:mm`. */
export interface GuardarEntrenamientoDto {
  fecha: string;
  horaInicio: string;
  horaFin: string;
  lugar: string;
  nota: string | null;
  categoriaIds: string[];
}

/** Un entrenamiento en una lista, en el calendario o en "Próximos entrenamientos". */
export interface EntrenamientoResumenDto {
  entrenamientoId: string;
  fecha: string;
  horaInicio: string;
  horaFin: string;
  lugar: string;
  estado: EstadoEntrenamiento;
  categorias: CategoriaDeActividadDto[];
  /** Solo para la cuenta de un jugador; ausente si no respondió. */
  miRespuesta?: RespuestaFamilia;
}

/** El detalle de un entrenamiento. */
export interface EntrenamientoDto extends EntrenamientoResumenDto {
  nota?: string | null;
  permisos: PermisosDeActividadDto;
  /** Solo para la cuenta de un jugador con asistencia registrada. */
  miAsistencia?: boolean;
}

/** Un jugador en la planilla de un entrenamiento. */
export interface JugadorDePlanillaEntrenamientoDto {
  usuarioRolId: string;
  nombres: string;
  apellidos: string;
  enlace?: EnlaceFichaDto | null;
  categoria: CategoriaDeActividadDto;
  respuesta?: RespuestaFamilia | null;
  /** `null` si nadie registró su asistencia. */
  presente?: boolean | null;
  marcable: boolean;
}

/** La planilla de un entrenamiento: respuestas y asistencia, por categoría y apellidos. */
export interface PlanillaEntrenamientoDto {
  entrenamientoId: string;
  asistiran: number;
  noAsistiran: number;
  sinResponder: number;
  jugadores: JugadorDePlanillaEntrenamientoDto[];
}

/** La asistencia de los jugadores enviados; las filas no enviadas no cambian. */
export interface RegistrarAsistenciaDto {
  jugadores: { usuarioRolId: string; presente: boolean }[];
}

/** Lo que se envía al crear o cambiar un partido. */
export interface GuardarPartidoDto {
  rival: string;
  fecha: string;
  hora: string;
  lugar: string;
  condicion: CondicionPartido;
  tipo: TipoPartido;
  /** Obligatorio si es de torneo; se ignora en un amistoso. */
  nombreTorneo: string | null;
  categoriaIds: string[];
}

/** Un goleador de un partido jugado: solo nombre y goles. */
export interface GoleadorDePartidoDto {
  nombres: string;
  apellidos: string;
  goles: number;
  enlace?: EnlaceFichaDto | null;
}

/** Un partido en una lista o en el calendario. */
export interface PartidoResumenDto {
  partidoId: string;
  rival: string;
  fecha: string;
  hora: string;
  lugar: string;
  condicion: CondicionPartido;
  tipo: TipoPartido;
  nombreTorneo?: string | null;
  estado: EstadoPartido;
  /** Próximo que ya empezó y todavía no tiene resultado. */
  faltaResultado: boolean;
  golesClub?: number | null;
  golesRival?: number | null;
  categorias: CategoriaDeActividadDto[];
  /** Solo para la cuenta de un jugador convocado que respondió. */
  miRespuesta?: RespuestaFamilia;
}

/** El detalle de un partido. */
export interface PartidoDto extends PartidoResumenDto {
  permisos: PermisosDeActividadDto;
  /** Solo en un partido jugado, para todos. */
  goleadores?: GoleadorDePartidoDto[];
  /** Solo en un partido jugado y para quien ve la planilla. */
  ultimoCambioResultado?: UltimoCambioDto;
}

/** Un convocado en la planilla de un partido. */
export interface ConvocadoDto {
  usuarioRolId: string;
  nombres: string;
  apellidos: string;
  enlace?: EnlaceFichaDto | null;
  /** La copiada al convocarlo. */
  categoria: CategoriaDeActividadDto;
  respuesta?: RespuestaFamilia | null;
  jugo: boolean;
  goles: number;
}

/** Un jugador activo de las categorías del partido que todavía no está convocado. */
export interface ConvocableDto {
  usuarioRolId: string;
  nombres: string;
  apellidos: string;
  categoria: CategoriaDeActividadDto;
}

/** La planilla de un partido: convocados, respuestas y quiénes jugaron. Nunca para un jugador. */
export interface PlanillaPartidoDto {
  partidoId: string;
  asistiran: number;
  noAsistiran: number;
  sinResponder: number;
  convocados: ConvocadoDto[];
  /** Vacía si el partido no está próximo. */
  convocables: ConvocableDto[];
}

/** La convocatoria completa; puede ir vacía. */
export interface GuardarConvocatoriaDto {
  usuarioRolIds: string[];
}

/** El marcador y cada convocado; los no enviados quedan como que no jugaron. */
export interface RegistrarResultadoDto {
  golesClub: number;
  golesRival: number;
  jugadores: { usuarioRolId: string; jugo: boolean; goles: number }[];
}

/** La respuesta de la familia. */
export interface ResponderDto {
  respuesta: RespuestaFamilia;
}

/** Lo que quien pregunta puede ver de un rango de días, por fecha y hora. */
export interface CalendarioDto {
  desde: string;
  hasta: string;
  entrenamientos: EntrenamientoResumenDto[];
  partidos: PartidoResumenDto[];
}
