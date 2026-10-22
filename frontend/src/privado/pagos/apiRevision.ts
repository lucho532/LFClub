import { api } from '../../compartido/api/cliente';
import type { ConfirmarPagoDto, PagoDto, PagosEnRevisionDto } from '../../compartido/api/tiposPagos';

/** Lo que informa la familia al avisar de una transferencia, con la imagen del comprobante. */
export interface TransferenciaInformada {
  valor: number;
  entidad: string;
  referencia: string | null;
  fecha: string;
  archivo: File;
}

const rutaDelClub = (clubId: string) => `/api/clubes/${clubId}`;

/** Llamadas de la API de las transferencias informadas y su revisión (spec 015, RF-021 y RF-022). */
export const apiRevision = {
  informar: (clubId: string, usuarioRolId: string, datos: TransferenciaInformada) => {
    const formulario = new FormData();
    formulario.append('valor', String(datos.valor));
    formulario.append('entidad', datos.entidad);
    if (datos.referencia) {
      formulario.append('referencia', datos.referencia);
    }

    formulario.append('fecha', datos.fecha);
    formulario.append('archivo', datos.archivo);
    return api.post<PagoDto>(`${rutaDelClub(clubId)}/jugadores/${usuarioRolId}/pagos/informados`, formulario);
  },

  listarEnRevision: (clubId: string) => api.get<PagosEnRevisionDto>(`${rutaDelClub(clubId)}/pagos/en-revision`),

  confirmar: (clubId: string, pagoId: string, datos: ConfirmarPagoDto) =>
    api.post<PagoDto>(`${rutaDelClub(clubId)}/pagos/${pagoId}/confirmacion`, datos),

  rechazar: (clubId: string, pagoId: string, motivo: string) =>
    api.post<PagoDto>(`${rutaDelClub(clubId)}/pagos/${pagoId}/rechazo`, { motivo }),

  /** La imagen del soporte, que exige sesión: nunca hay una dirección pública. */
  soporte: (clubId: string, pagoId: string) => api.getArchivo(`${rutaDelClub(clubId)}/pagos/${pagoId}/soporte`),
};
