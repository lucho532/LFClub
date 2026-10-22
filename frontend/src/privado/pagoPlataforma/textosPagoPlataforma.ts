import type { Rol } from '../../compartido/api/tipos';
import type { EstadoCobroPlataforma, OrigenDelPago } from '../../compartido/api/tiposPagoPlataforma';
import { fechaLarga, sumarDias } from '../../compartido/fechas';
import { nombreDelMes } from '../pagos/textosPagos';

/** El estado del cobro siempre con texto e ícono, nunca solo con color (§24). */
export const TEXTO_ESTADO_COBRO: Record<EstadoCobroPlataforma, string> = {
  PENDIENTE: '! Pendiente',
  PAGADO: '✓ Pagado',
  ANULADO: '– Anulado',
};

export const TEXTO_ORIGEN_DEL_PAGO: Record<OrigenDelPago, string> = {
  PASARELA: 'Pasarela de pago',
  MANUAL: 'Registrado por la plataforma',
};

export const TEXTO_ROL_PLURAL: Partial<Record<Rol, string>> = {
  JUGADOR: 'Jugadores',
  ENTRENADOR: 'Entrenadores',
  DIRECTIVO: 'Directivos',
  PRESIDENTE: 'Presidentes',
};

export const AVISO_SIN_PAGO_EN_LINEA = 'El pago en línea no está disponible por ahora.';

/** El último día para pagar es el anterior al primero en que el club se suspende. */
export function textoPagaHasta(suspendeDesde: string): string {
  return `Paga hasta el ${fechaLarga(sumarDias(suspendeDesde, -1))}`;
}

/** "noviembre de 2026" a partir de `2026-11`. */
export function textoDelPeriodo(periodo: string): string {
  return nombreDelMes(periodo);
}
