import { useCarga, type Carga } from '../../compartido/api/useCarga';
import type { AvisoDeMoraDto } from '../../compartido/api/tiposPagos';
import { apiAvisoDeMora } from './apiAvisoDeMora';

/** El aviso de mora de un jugador cargado de la API; `recargar` lo vuelve a pedir tras un error. */
export function useAvisoDeMora(clubId: string, usuarioRolId: string): Carga<AvisoDeMoraDto> {
  return useCarga(`${clubId}/aviso/${usuarioRolId}`, () => apiAvisoDeMora.obtener(clubId, usuarioRolId));
}
