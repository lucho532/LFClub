import type { EstadoClub, EstadoEnvio, EstadoInvitacion } from '../api/tipos';
import type { EstadoDePagoClub } from '../api/tiposPagoPlataforma';
import type { Tono } from './Aviso';

interface Props {
  tono: Tono;
  texto: string;
}

/** Etiqueta de estado. El estado se dice siempre con texto, nunca solo con color (RF-034). */
export function EtiquetaEstado({ tono, texto }: Props) {
  return <span className={`etiqueta tono-${tono}`}>{texto}</span>;
}

const ESTADOS_CLUB: Record<EstadoClub, Props> = {
  ACTIVO: { tono: 'exito', texto: 'Activo' },
  SUSPENDIDO: { tono: 'aviso', texto: 'Suspendido' },
  DADO_DE_BAJA: { tono: 'error', texto: 'Dado de baja' },
};

/** Etiqueta con el estado de un club. */
export function EtiquetaEstadoClub({ estado }: { estado: EstadoClub }) {
  return <EtiquetaEstado {...ESTADOS_CLUB[estado]} />;
}

const ESTADOS_ENVIO: Record<EstadoEnvio, Props> = {
  PENDIENTE: { tono: 'neutro', texto: 'Envío pendiente' },
  ENVIADO: { tono: 'exito', texto: 'Correo enviado' },
  FALLIDO: { tono: 'error', texto: 'El correo falló' },
};

/** Etiqueta con el resultado del envío de una invitación. */
export function EtiquetaEstadoEnvio({ estado }: { estado: EstadoEnvio }) {
  return <EtiquetaEstado {...ESTADOS_ENVIO[estado]} />;
}

const ESTADOS_INVITACION: Record<EstadoInvitacion, Props> = {
  PENDIENTE: { tono: 'info', texto: 'Pendiente' },
  USADA: { tono: 'exito', texto: 'Usada' },
  VENCIDA: { tono: 'aviso', texto: 'Vencida' },
  CANCELADA: { tono: 'neutro', texto: 'Cancelada' },
};

/** Etiqueta con el estado de una invitación del club. */
export function EtiquetaEstadoInvitacion({ estado }: { estado: EstadoInvitacion }) {
  return <EtiquetaEstado {...ESTADOS_INVITACION[estado]} />;
}

const ESTADOS_DE_PAGO_CLUB: Record<EstadoDePagoClub, Props> = {
  SIN_COBRO: { tono: 'neutro', texto: 'Sin cobro' },
  AL_DIA: { tono: 'exito', texto: '✓ Al día' },
  PENDIENTE: { tono: 'info', texto: '! Pendiente' },
  VENCIDO: { tono: 'aviso', texto: '⚠ Vencido' },
  SUSPENDIDO_POR_IMPAGO: { tono: 'error', texto: '⚠ Suspendido por impago' },
};

/** Etiqueta con el estado de pago de un club frente a la plataforma (spec 017), siempre con texto e ícono. */
export function EtiquetaEstadoDePagoClub({ estado }: { estado: EstadoDePagoClub }) {
  return <EtiquetaEstado {...ESTADOS_DE_PAGO_CLUB[estado]} />;
}
