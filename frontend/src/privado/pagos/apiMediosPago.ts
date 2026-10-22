import { api } from '../../compartido/api/cliente';
import type { GuardarMedioPagoDto, MedioPagoDto } from '../../compartido/api/tiposPagos';

const base = (clubId: string) => `/api/clubes/${clubId}/medios-de-pago`;

/** Llamadas de la API de los medios por los que el club recibe pagos (spec 015, RF-026 a RF-028). */
export const apiMediosPago = {
  listar: (clubId: string) => api.get<MedioPagoDto[]>(base(clubId)),

  crear: (clubId: string, datos: GuardarMedioPagoDto) => api.post<MedioPagoDto>(base(clubId), datos),

  editar: (clubId: string, medioId: string, datos: GuardarMedioPagoDto) =>
    api.put<MedioPagoDto>(`${base(clubId)}/${medioId}`, datos),

  quitar: (clubId: string, medioId: string) => api.delete(`${base(clubId)}/${medioId}`),
};
