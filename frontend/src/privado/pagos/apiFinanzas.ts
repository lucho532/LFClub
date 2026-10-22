import { api } from '../../compartido/api/cliente';
import type { FinanzasDto } from '../../compartido/api/tiposPagos';

/** Llamadas de la API de las finanzas del club (spec 015, RF-031 a RF-034). `mes` es `AAAA-MM-01`. */
export const apiFinanzas = {
  obtener: (clubId: string, mes?: string) =>
    api.get<FinanzasDto>(`/api/clubes/${clubId}/finanzas${mes ? `?mes=${mes}` : ''}`),
};
