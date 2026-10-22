import { useCarga, type Carga } from '../../compartido/api/useCarga';
import type { PagoPlataformaDto } from '../../compartido/api/tiposPagoPlataforma';
import { apiPagoPlataforma } from './apiPagoPlataforma';

/** El resumen del pago a la plataforma del club cargado de la API. */
export function usePagoPlataforma(clubId: string): Carga<PagoPlataformaDto> {
  return useCarga(`${clubId}/pago-plataforma`, () => apiPagoPlataforma.obtener(clubId));
}
