// Tipos del resultado publicado por la familia de
// specs/010-ficha-resultado-y-carta/contracts/api.yaml, con los mismos nombres.
// Van aparte de tiposActividades.ts para que ningún archivo supere las 250 líneas.

/** FAMILIA: la cuenta de un convocado. GESTION: quien entrena o el presidente. */
export type OrigenResultado = 'FAMILIA' | 'GESTION';

/** Lo que envía la familia; el marcador solo si el partido todavía no lo tiene. */
export interface ResultadoDeFamiliaDto {
  golesClub: number | null;
  golesRival: number | null;
  jugo: boolean;
  /** 0 si no jugó. */
  goles: number;
}

/** Solo lo del jugador de la petición; nunca de otro convocado. */
export interface MiParticipacionDto {
  jugo: boolean;
  goles: number;
  /** Ausente si nadie indicó nada. */
  indicadoPor?: OrigenResultado;
  /** Falso si quien entrena ya corrigió lo de su jugador. */
  puedeCambiar: boolean;
}

/** Quién puso el marcador por primera vez. Solo para quien ve la planilla. */
export interface PublicacionResultadoDto {
  fecha: string;
  /** Nombre copiado; el del jugador si fue su familia. */
  autor: string;
  porFamilia: boolean;
}

/** Quién indicó lo de un convocado y cuándo. */
export interface IndicacionDto {
  origen: OrigenResultado;
  autor: string;
  fecha: string;
}
