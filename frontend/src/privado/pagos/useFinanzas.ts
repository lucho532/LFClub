import { useCarga, type Carga } from '../../compartido/api/useCarga';
import type { FinanzasDto } from '../../compartido/api/tiposPagos';
import { apiFinanzas } from './apiFinanzas';

/** Las finanzas del club en un mes (el en curso si no se indica); se vuelven a pedir al cambiar de mes. */
export function useFinanzas(clubId: string, mes?: string): Carga<FinanzasDto> {
  return useCarga(`${clubId}/finanzas/${mes ?? 'actual'}`, () => apiFinanzas.obtener(clubId, mes));
}
