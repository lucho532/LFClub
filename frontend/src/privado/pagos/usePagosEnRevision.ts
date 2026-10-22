import { useCarga, type Carga } from '../../compartido/api/useCarga';
import type { PagosEnRevisionDto } from '../../compartido/api/tiposPagos';
import { apiRevision } from './apiRevision';

/** Los pagos que esperan revisión; `recargar` los vuelve a pedir tras confirmar o rechazar uno. */
export function usePagosEnRevision(clubId: string): Carga<PagosEnRevisionDto> {
  return useCarga(`${clubId}/pagos-en-revision`, () => apiRevision.listarEnRevision(clubId));
}
