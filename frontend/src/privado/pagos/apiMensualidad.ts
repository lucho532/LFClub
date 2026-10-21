import { api } from '../../compartido/api/cliente';
import type { FijarValorPropioDto, MensualidadDto } from '../../compartido/api/tiposPagos';

const base = (clubId: string) => `/api/clubes/${clubId}/mensualidad`;

/** Llamadas de la API del valor de la mensualidad (spec 015, RF-011 y RF-012). */
export const apiMensualidad = {
  obtener: (clubId: string) => api.get<MensualidadDto>(base(clubId)),

  fijarValorDelClub: (clubId: string, valor: number) => api.put<MensualidadDto>(base(clubId), { valor }),

  fijarValorPropio: (clubId: string, datos: FijarValorPropioDto) =>
    api.put<MensualidadDto>(`${base(clubId)}/jugadores`, datos),
};
