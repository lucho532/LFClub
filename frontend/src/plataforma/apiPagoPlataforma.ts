import { api } from '../compartido/api/cliente';
import type {
  AnularCobroDto,
  CobroPlataformaDto,
  EstadoDePagoClubDto,
  FijarInicioDelCobroDto,
  GuardarTarifasDto,
  NotificacionPasarelaDto,
  ResultadoNotificacion,
  TarifaDeClubDto,
  TarifasDto,
} from '../compartido/api/tiposPagoPlataforma';

/** Llamadas del panel del DESARROLLADOR para el cobro de la plataforma (spec 017). */
export const apiPagoPlataforma = {
  tarifas: {
    obtener: () => api.get<TarifasDto>('/api/plataforma/tarifas'),
    guardar: (datos: GuardarTarifasDto) => api.put<TarifasDto>('/api/plataforma/tarifas', datos),
    porClub: () => api.get<TarifaDeClubDto[]>('/api/plataforma/tarifas/clubes'),
  },

  inicioDelCobro: {
    fijar: (clubId: string, datos: FijarInicioDelCobroDto) =>
      api.put<EstadoDePagoClubDto>(`/api/plataforma/clubes/${clubId}/cobro`, datos),
  },

  cobros: {
    listar: (clubId: string) => api.get<CobroPlataformaDto[]>(`/api/plataforma/clubes/${clubId}/cobros`),
    marcarPagado: (clubId: string, cobroId: string) =>
      api.post<CobroPlataformaDto>(`/api/plataforma/clubes/${clubId}/cobros/${cobroId}/pago-manual`),
    anular: (clubId: string, cobroId: string, datos: AnularCobroDto) =>
      api.post<CobroPlataformaDto>(`/api/plataforma/clubes/${clubId}/cobros/${cobroId}/anulacion`, datos),
  },

  notificaciones: {
    listar: (resultado?: ResultadoNotificacion) =>
      api.get<NotificacionPasarelaDto[]>(
        `/api/plataforma/notificaciones${resultado ? `?resultado=${resultado}` : ''}`,
      ),
  },
};
