import { api } from '../../compartido/api/cliente';
import type {
  CobroPlataformaDto,
  IntentoDePagoDto,
  PagoPlataformaDto,
} from '../../compartido/api/tiposPagoPlataforma';

const base = (clubId: string) => `/api/clubes/${clubId}/pago-plataforma`;

/** Llamadas de la API del pago del club por el uso de la plataforma (spec 017); solo el PRESIDENTE. */
export const apiPagoPlataforma = {
  obtener: (clubId: string) => api.get<PagoPlataformaDto>(base(clubId)),

  obtenerCobro: (clubId: string, cobroId: string) =>
    api.get<CobroPlataformaDto>(`${base(clubId)}/cobros/${cobroId}`),

  /** Crea un intento de pago; la respuesta lleva la dirección de la pasarela, ya firmada. */
  iniciarPago: (clubId: string, cobroId: string) =>
    api.post<IntentoDePagoDto>(`${base(clubId)}/cobros/${cobroId}/intentos`),
};
