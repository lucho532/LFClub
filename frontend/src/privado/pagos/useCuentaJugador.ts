import { useCarga, type Carga } from '../../compartido/api/useCarga';
import type { CuentaJugadorDto } from '../../compartido/api/tiposPagos';
import { apiCuenta } from './apiCuenta';

/** El estado de cuenta de un jugador cargado de la API; `recargar` lo vuelve a pedir tras una acción. */
export function useCuentaJugador(clubId: string, usuarioRolId: string): Carga<CuentaJugadorDto> {
  return useCarga(`${clubId}/cuenta/${usuarioRolId}`, () => apiCuenta.obtener(clubId, usuarioRolId));
}
