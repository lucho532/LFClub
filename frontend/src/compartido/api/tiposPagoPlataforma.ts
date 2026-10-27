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
  /** Rango de la tarifa que se aplicó al generarlo; nulo en los cobros por precio de rol anteriores. */
  rango: RangoTarifaDto | null;
  /** Jugadores activos del club al generar el cobro. */
  jugadores: number;
}

/** Lo que ve el PRESIDENTE en «Pago de la plataforma». */
export interface PagoPlataformaDto {
  pagoEnLineaDisponible: boolean;
  suspendidoPorImpago: boolean;
  pendientes: CobroPlataformaDto[];
  totalPendiente: number;
  historial: CobroPlataformaDto[];
  /** El último cobro que se pagó, con su comprobante; nulo si nunca ha pagado. */
  ultimoPago: CobroPlataformaDto | null;
  /** Estimación del siguiente cobro, todavía sin generar; nula si no le corresponde cobro. */
  proximoCobro: ProximoCobroDto | null;
  /** Situación de pago calculada por el servidor. */
  estadoDePago: EstadoDePagoClub;
  /** La tarifa vigente explicada al presidente. */
  tarifa: TarifaVigenteDto;
}

/** La tarifa explicada al presidente: solo informativa; el valor de cada cobro queda fijo al generarse. */
export interface TarifaVigenteDto {
  rangos: RangoTarifaDto[];
  diasDeGracia: number;
  /** Jugadores activos del club hoy. */
  jugadores: number;
  /** El rango en el que cae su club hoy; nulo si ninguno cubre esa cantidad. */
  rangoDelClub: RangoTarifaDto | null;
}

/** El siguiente cobro estimado con los jugadores de hoy y la tarifa vigente; no se puede pagar. */
export interface ProximoCobroDto {
  periodo: string;
  jugadores: number;
  rango: RangoTarifaDto;
  valorEstimado: number;
}

/** Un intento de pago ya creado: la dirección de la pasarela, ya firmada. */
export interface IntentoDePagoDto {
  urlDePago: string;
  referencia: string;
}

/** Un rango de la tarifa: de `desde` a `hasta` jugadores (sin límite si `hasta` es nulo) el club paga `precio` al mes. */
export interface RangoTarifaDto {
  desde: number;
  hasta: number | null;
  precio: number;
}

export interface TarifasDto {
  rangos: RangoTarifaDto[];
  diasDeGracia: number;
  pasarelaConfigurada: boolean;
  actualizadoEn: string | null;
}

export interface GuardarTarifasDto {
  rangos: RangoTarifaDto[];
  diasDeGracia: number;
}

/** Lo que le corresponde pagar a un club con la tarifa vigente (vista previa, no un cobro). */
export interface TarifaDeClubDto {
  clubId: string;
  nombre: string;
  jugadores: number;
  rango: RangoTarifaDto | null;
  valorMensual: number;
  mesDeInicioDelCobro: string | null;
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
