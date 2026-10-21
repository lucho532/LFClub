import type { AvisoDeMoraDto, EstadoDeCuenta, EstadoPago } from '../../compartido/api/tiposPagos';
import { diaDeLaSemana } from '../../compartido/fechas';
import { formatoMoneda } from './formatoMoneda';

/** El estado de cuenta siempre con texto, no solo con color (§24). */
export const TEXTO_ESTADO_DE_CUENTA: Record<EstadoDeCuenta, string> = {
  AL_DIA: 'Al día',
  PAGO_PARCIAL: 'Pago parcial',
  DEBE: 'Debe',
};

export const TEXTO_ESTADO_DE_PAGO: Record<EstadoPago, string> = {
  EN_REVISION: 'En revisión',
  CONFIRMADO: 'Confirmado',
  RECHAZADO: 'Rechazado',
  ANULADO: 'Anulado',
};

const MESES = [
  'enero',
  'febrero',
  'marzo',
  'abril',
  'mayo',
  'junio',
  'julio',
  'agosto',
  'septiembre',
  'octubre',
  'noviembre',
  'diciembre',
];

/** "octubre de 2026" a partir de `2026-10-01`. */
export function nombreDelMes(mes: string): string {
  const [anio, numero] = mes.split('-').map(Number);
  return `${MESES[numero - 1]} de ${anio}`;
}

/** Lo que paga el cargo: "Mensualidad octubre de 2026" o el tipo si no tiene mes. */
export function descripcionDelCargo(tipo: string, mes: string | null): string {
  const base = tipo === 'MENSUALIDAD' ? 'Mensualidad' : 'Otro cargo';
  return mes ? `${base} ${nombreDelMes(mes)}` : base;
}

/** "martes 13 de octubre" para la fecha de un pago. */
export function fechaDelPago(fecha: string): string {
  return diaDeLaSemana(fecha);
}

/** Si ese rol ve la entrada "Pagos" del menú del club: el entrenador nunca (§16.9). */
export function puedeVerPagos(rol: string | undefined): boolean {
  return rol === 'PRESIDENTE' || rol === 'DIRECTIVO' || rol === 'JUGADOR';
}

/** Si ese rol ve las pantallas del club (mensualidad, medios, revisión y finanzas). */
export function veFinanzasDelClub(rol: string | undefined): boolean {
  return rol === 'PRESIDENTE' || rol === 'DIRECTIVO';
}

/** Ruta de la pantalla con el estado de cuenta de un jugador. */
export function rutaDeLaCuenta(clubId: string, usuarioRolId: string): string {
  return `/club/${clubId}/jugadores/${usuarioRolId}/cuenta`;
}

/** Ruta del recibo de un pago. */
export function rutaDelRecibo(clubId: string, pagoId: string): string {
  return `/club/${clubId}/recibos/${pagoId}`;
}

/** Aviso del inicio del presidente mientras el club no define su mensualidad (RF-011). */
export const AVISO_VALOR_MENSUALIDAD = 'Defina el valor de la mensualidad';

/** El texto del aviso fijo de mora según su estado; el color nunca es lo único que lo dice (§24). */
export function textoDelAvisoDeMora(aviso: AvisoDeMoraDto): string {
  switch (aviso.estado) {
    case 'EN_MORA':
      return `Estás en mora: debes ${formatoMoneda(aviso.valorVencido)} de ${aviso.mesesVencidos} ${
        aviso.mesesVencidos === 1 ? 'mensualidad vencida' : 'mensualidades vencidas'
      }.`;
    case 'PENDIENTE_EN_PLAZO':
      return `Tienes la mensualidad de este mes pendiente (${formatoMoneda(aviso.valorEnPlazo)}). Vence el día 5.`;
    default:
      return 'Estás al día.';
  }
}

/** "Tienes 1 pago en revisión." / "Tienes 2 pagos en revisión." */
export function textoDePagosEnRevision(cantidad: number): string {
  return `Tienes ${cantidad} ${cantidad === 1 ? 'pago en revisión' : 'pagos en revisión'}.`;
}
