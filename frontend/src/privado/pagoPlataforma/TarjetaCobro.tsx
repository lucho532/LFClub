import type { ReactNode } from 'react';
import type { CobroPlataformaDto, EstadoCobroPlataforma } from '../../compartido/api/tiposPagoPlataforma';
import { Aviso, type Tono } from '../../compartido/componentes/Aviso';
import { EtiquetaEstado } from '../../compartido/componentes/EtiquetaEstado';
import { Tarjeta } from '../../compartido/componentes/Tarjeta';
import { fechaYHora } from '../../compartido/formato';
import { formatoMoneda } from '../pagos/formatoMoneda';
import {
  TEXTO_ESTADO_COBRO,
  TEXTO_ORIGEN_DEL_PAGO,
  TEXTO_ROL_PLURAL,
  textoDelPeriodo,
  textoPagaHasta,
} from './textosPagoPlataforma';

const TONO: Record<EstadoCobroPlataforma, Tono> = {
  PENDIENTE: 'aviso',
  PAGADO: 'exito',
  ANULADO: 'neutro',
};

interface Props {
  cobro: CobroPlataformaDto;
  /** El botón de pagar, si el cobro se puede pagar en línea; lo pone la pantalla. */
  accion?: ReactNode;
}

/**
 * Un cobro de la plataforma: el periodo, el detalle por rol con precio y subtotal tal como se generó,
 * el total y, según su estado, hasta cuándo se paga, el comprobante o el motivo de la anulación. Todo
 * lo que pinta viene del servidor: no calcula totales ni fechas. El estado va con texto e ícono.
 */
export function TarjetaCobro({ cobro, accion }: Props) {
  return (
    <Tarjeta
      titulo={`Uso de la plataforma · ${textoDelPeriodo(cobro.periodo)}`}
      acciones={<EtiquetaEstado tono={TONO[cobro.estado]} texto={TEXTO_ESTADO_COBRO[cobro.estado]} />}
    >
      <ul className="lista-de-inicio">
        {cobro.detalle
          .filter((linea) => linea.integrantes > 0 || linea.subtotal > 0)
          .map((linea) => (
            <li key={linea.rol}>
              {TEXTO_ROL_PLURAL[linea.rol]}: {linea.integrantes} × {formatoMoneda(linea.precio)} ={' '}
              <strong>{formatoMoneda(linea.subtotal)}</strong>
            </li>
          ))}
      </ul>
      <p>
        Total: <strong>{formatoMoneda(cobro.total)}</strong>
      </p>

      {cobro.estado === 'PENDIENTE' && (
        <>
          <p>{textoPagaHasta(cobro.suspendeDesde)}.</p>
          {cobro.ultimoPagoNoAprobado && (
            <Aviso tono="aviso">
              <span aria-hidden="true">!</span> Tu último pago no fue aprobado. No se debitó ningún dinero: puedes
              intentarlo de nuevo.
            </Aviso>
          )}
          {cobro.enVerificacion && (
            <p className="texto-suave">
              En verificación: estamos esperando la confirmación de la pasarela de pago.
            </p>
          )}
          {accion}
        </>
      )}

      {cobro.estado === 'PAGADO' && cobro.pagadoEn && (
        <ul className="lista-de-inicio">
          <li>Pagado el {fechaYHora(cobro.pagadoEn)}</li>
          <li>Valor: {formatoMoneda(cobro.total)}</li>
          {cobro.origenDelPago && <li>Origen: {TEXTO_ORIGEN_DEL_PAGO[cobro.origenDelPago]}</li>}
          {cobro.referencia && <li>Referencia: {cobro.referencia}</li>}
          {cobro.medioDePago && <li>Medio de pago: {cobro.medioDePago}</li>}
        </ul>
      )}

      {cobro.estado === 'ANULADO' && (
        <p className="texto-suave">Cobro anulado{cobro.motivoAnulacion ? `: ${cobro.motivoAnulacion}` : '.'}</p>
      )}
    </Tarjeta>
  );
}
