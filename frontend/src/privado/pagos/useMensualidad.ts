import { useCarga, type Carga } from '../../compartido/api/useCarga';
import type { MensualidadDto } from '../../compartido/api/tiposPagos';
import { apiMensualidad } from './apiMensualidad';

/** La mensualidad del club cargada de la API; `fijar` toma la respuesta de cada cambio. */
export function useMensualidad(clubId: string): Carga<MensualidadDto> {
  return useCarga(`${clubId}/mensualidad`, () => apiMensualidad.obtener(clubId));
}
