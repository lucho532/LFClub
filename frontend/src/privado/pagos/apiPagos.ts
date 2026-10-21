import { api } from '../../compartido/api/cliente';
import type { PagoDto, RegistrarPagoDto } from '../../compartido/api/tiposPagos';

/** Llamadas de la API de los pagos (spec 015): registrar uno recibido directamente, consultar su recibo y anularlo. */
export const apiPagos = {
  registrar: (clubId: string, usuarioRolId: string, datos: RegistrarPagoDto) =>
    api.post<PagoDto>(`/api/clubes/${clubId}/jugadores/${usuarioRolId}/pagos`, datos),

  anular: (clubId: string, pagoId: string, motivo: string) =>
    api.post<PagoDto>(`/api/clubes/${clubId}/pagos/${pagoId}/anulacion`, { motivo }),

  obtener: (clubId: string, pagoId: string) => api.get<PagoDto>(`/api/clubes/${clubId}/pagos/${pagoId}`),
};
