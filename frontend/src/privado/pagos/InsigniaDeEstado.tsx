import type { Tono } from '../../compartido/componentes/Aviso';
import { EtiquetaEstado } from '../../compartido/componentes/EtiquetaEstado';
import type { EstadoDeCuenta, EstadoPago } from '../../compartido/api/tiposPagos';
import { TEXTO_ESTADO_DE_CUENTA, TEXTO_ESTADO_DE_PAGO } from './textosPagos';

const TONO_DE_CUENTA: Record<EstadoDeCuenta, Tono> = {
  AL_DIA: 'exito',
  PAGO_PARCIAL: 'aviso',
  DEBE: 'error',
};

const TONO_DE_PAGO: Record<EstadoPago, Tono> = {
  EN_REVISION: 'info',
  CONFIRMADO: 'exito',
  RECHAZADO: 'error',
  ANULADO: 'neutro',
};

/** El estado de cuenta de un jugador con su texto ("Al día", "Pago parcial", "Debe") y su color. */
export function InsigniaDeEstadoDeCuenta({ estado }: { estado: EstadoDeCuenta }) {
  return <EtiquetaEstado tono={TONO_DE_CUENTA[estado]} texto={TEXTO_ESTADO_DE_CUENTA[estado]} />;
}

/** El estado de un pago con su texto y su color: el color nunca es lo único que lo dice. */
export function InsigniaDeEstadoDePago({ estado }: { estado: EstadoPago }) {
  return <EtiquetaEstado tono={TONO_DE_PAGO[estado]} texto={TEXTO_ESTADO_DE_PAGO[estado]} />;
}
