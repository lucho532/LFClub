import { useCarga, type Carga } from '../../compartido/api/useCarga';
import type { MedioPagoDto } from '../../compartido/api/tiposPagos';
import { apiMediosPago } from './apiMediosPago';

/** Los medios de pago del club cargados de la API; `recargar` los vuelve a pedir tras un cambio. */
export function useMediosPago(clubId: string): Carga<MedioPagoDto[]> {
  return useCarga(`${clubId}/medios-de-pago`, () => apiMediosPago.listar(clubId));
}
