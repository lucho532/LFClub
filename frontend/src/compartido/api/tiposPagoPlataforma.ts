// Tipos de los esquemas de specs/017-pago-de-la-plataforma/contracts/api.yaml, con los mismos nombres.
// Los importes llegan como números en pesos enteros, los periodos como `AAAA-MM` y las fechas como
// `AAAA-MM-DD`. Ninguna respuesta lleva llaves ni secretos de la pasarela.

import type { Rol } from './tipos';

export type EstadoCobroPlataforma = 'PENDIENTE' | 'PAGADO' | 'ANULADO';
export type EstadoDePagoClub = 'SIN_COBRO' | 'AL_DIA' | 'PENDIENTE' | 'VENCIDO' | 'SUSPENDIDO_POR_IMPAGO';
export type MotivoDeSuspension = 'MANUAL' | 'IMPAGO';
export type OrigenDelPago = 'PASARELA' | 'MANUAL';
export type ResultadoNotificacion =
  | 'APLICADA'
  | 'REPETIDA'
  | 'NO_APROBADA'
  | 'PAGO_DUPLICADO'
  | 'PAGO_A_COBRO_ANULADO'
  | 'VALOR_NO_COINCIDE'
  | 'REFERENCIA_DESCONOCIDA'
  | 'FIRMA_INVALIDA';

/** Una línea del detalle de un cobro: un rol con sus integrantes, su precio y su subtotal. */
export interface DetalleDeRolDto {
  rol: Rol;
  integrantes: number;
  precio: number;
  subtotal: number;
}

/** El cobro mensual de la plataforma a un club, con su detalle congelado. */
export interface CobroPlataformaDto {
  cobroId: string;
  periodo: string;
  estado: EstadoCobroPlataforma;
  detalle: DetalleDeRolDto[];
  total: number;
  generadoEn: string;
  /** Primer día en que el club se suspende; el último día para pagar es el anterior. */
  suspendeDesde: string;
  pagadoEn: string | null;
  origenDelPago: OrigenDelPago | null;
  referencia: string | null;
  medioDePago: string | null;
  enVerificacion: boolean;
  /** La pasarela informó que el último intento no fue aprobado: no se debitó nada y se puede reintentar. */
  ultimoPagoNoAprobado: boolean;
  motivoAnulacion: string | null;
}

/** Lo que ve el PRESIDENTE en «Pago de la plataforma». */
export interface PagoPlataformaDto {
  pagoEnLineaDisponible: boolean;
  suspendidoPorImpago: boolean;
  pendientes: CobroPlataformaDto[];
  totalPendiente: number;
  historial: CobroPlataformaDto[];
}

/** Un intento de pago ya creado: la dirección de la pasarela, ya firmada. */
export interface IntentoDePagoDto {
  urlDePago: string;
  referencia: string;
}

export interface TarifasDto {
  precioJugador: number | null;
  precioEntrenador: number | null;
  precioDirectivo: number | null;
  precioPresidente: number | null;
  diasDeGracia: number;
  pasarelaConfigurada: boolean;
  actualizadoEn: string | null;
}

export interface GuardarTarifasDto {
  precioJugador: number | null;
  precioEntrenador: number | null;
  precioDirectivo: number | null;
  precioPresidente: number | null;
  diasDeGracia: number;
}

/** `mesDeInicio` es `AAAA-MM`; nulo para no cobrar. */
export interface FijarInicioDelCobroDto {
  mesDeInicio: string | null;
}

export interface EstadoDePagoClubDto {
  estadoDePago: EstadoDePagoClub;
  valorAdeudado: number;
  mesDeInicioDelCobro: string | null;
  motivoDeSuspension: MotivoDeSuspension | null;
}

export interface AnularCobroDto {
  motivo: string;
}

export interface NotificacionPasarelaDto {
  notificacionId: string;
  recibidaEn: string;
  resultado: ResultadoNotificacion;
  transaccionId: string | null;
  estadoDeLaPasarela: string | null;
  referencia: string | null;
  valor: number | null;
  clubId: string | null;
  periodo: string | null;
}
