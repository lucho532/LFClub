// Tipos de los esquemas de specs/015-mensualidades-cargos-pagos/contracts/api.yaml, con los mismos
// nombres. Los importes llegan como números en pesos enteros y los meses como `AAAA-MM-01`.

export type EstadoDeCuenta = 'AL_DIA' | 'PAGO_PARCIAL' | 'DEBE';
export type EstadoPago = 'EN_REVISION' | 'CONFIRMADO' | 'RECHAZADO' | 'ANULADO';
export type MedioPago = 'EFECTIVO' | 'TRANSFERENCIA';
export type TipoCargo = 'MENSUALIDAD' | 'OTRO';

/** Un jugador al que el presidente le fijó un valor de mensualidad propio. */
export interface JugadorConValorPropioDto {
  usuarioRolId: string;
  nombreCompleto: string;
  valor: number;
}

/** El valor de la mensualidad del club (`null` mientras no se defina) y los valores propios. */
export interface MensualidadDto {
  valorClub: number | null;
  jugadoresConValorPropio: JugadorConValorPropioDto[];
}

export interface FijarValorMensualidadDto {
  valor: number;
}

/** Con `valor: null` los jugadores vuelven al valor del club. */
export interface FijarValorPropioDto {
  usuarioRolIds: string[];
  valor: number | null;
}

export interface MedioPagoDto {
  id: string;
  entidad: string;
  titular: string;
  numero: string;
  nota: string | null;
  activo: boolean;
}

export interface GuardarMedioPagoDto {
  entidad: string;
  titular: string;
  numero: string;
  nota: string | null;
  activo: boolean;
}

export interface CargoDto {
  id: string;
  tipo: TipoCargo;
  mes: string | null;
  valor: number;
  pagado: number;
  pendiente: number;
}

export interface PagoDto {
  id: string;
  usuarioRolId: string | null;
  jugadorNombre: string;
  estado: EstadoPago;
  valor: number;
  valorInformado: number | null;
  medio: MedioPago;
  entidad: string | null;
  referencia: string | null;
  fecha: string;
  concepto: string | null;
  numeroRecibo: number | null;
  saldoTrasPago: number | null;
  tieneSoporte: boolean;
  tieneResguardo: boolean;
  creadoEn: string;
  creadoPorNombre: string;
  confirmadoEn: string | null;
  confirmadoPorNombre: string | null;
  rechazadoEn: string | null;
  rechazadoPorNombre: string | null;
  motivoRechazo: string | null;
  anuladoEn: string | null;
  anuladoPorNombre: string | null;
  motivoAnulacion: string | null;
}

export interface CuentaJugadorDto {
  usuarioRolId: string;
  nombreCompleto: string;
  estado: EstadoDeCuenta;
  saldo: number;
  cargos: CargoDto[];
  pagos: PagoDto[];
  mediosPago: MedioPagoDto[];
  /** Verdadero solo para el presidente. */
  puedeRegistrar: boolean;
  /** Verdadero solo para la cuenta del propio jugador. */
  puedeInformar: boolean;
}

export interface RegistrarPagoDto {
  valor: number;
  medio: MedioPago;
  entidad: string | null;
  referencia: string | null;
  fecha: string;
  cargoIds: string[] | null;
}

export interface ConfirmarPagoDto {
  valor: number | null;
  cargoIds: string[] | null;
}

export interface MotivoDto {
  motivo: string;
}

export interface PagoEnRevisionDto {
  pago: PagoDto;
  jugadorNombre: string;
  cargosPendientes: CargoDto[];
}

export interface PagosEnRevisionDto {
  total: number;
  pagos: PagoEnRevisionDto[];
}

export interface TotalPorCategoriaDto {
  categoriaId: string | null;
  nombre: string;
  recaudoMes: number;
  cartera: number;
}

export interface JugadorQueDebeDto {
  usuarioRolId: string;
  nombreCompleto: string;
  saldo: number;
  estado: EstadoDeCuenta;
  retirado: boolean;
}

export interface FinanzasDto {
  mes: string;
  recaudoMes: number;
  carteraPendiente: number;
  pagosEnRevision: number;
  porCategoria: TotalPorCategoriaDto[];
  jugadoresQueDeben: JugadorQueDebeDto[];
}
