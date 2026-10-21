import { api } from '../../compartido/api/cliente';
import type { CuentaJugadorDto } from '../../compartido/api/tiposPagos';

/** Llamadas de la API del estado de cuenta de un jugador (spec 015, RF-001 a RF-004). */
export const apiCuenta = {
  obtener: (clubId: string, usuarioRolId: string) =>
    api.get<CuentaJugadorDto>(`/api/clubes/${clubId}/jugadores/${usuarioRolId}/cuenta`),
};
