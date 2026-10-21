import { api } from '../../compartido/api/cliente';
import type { AvisoDeMoraDto } from '../../compartido/api/tiposPagos';

/** Llamada de la API del aviso fijo de mora del inicio de la familia (spec 016, RF-006 a RF-017). */
export const apiAvisoDeMora = {
  obtener: (clubId: string, usuarioRolId: string) =>
    api.get<AvisoDeMoraDto>(`/api/clubes/${clubId}/jugadores/${usuarioRolId}/cuenta/aviso`),
};
