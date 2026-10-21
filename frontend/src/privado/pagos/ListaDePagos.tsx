import { Link } from 'react-router-dom';
import type { PagoDto } from '../../compartido/api/tiposPagos';
import { formatoMoneda } from './formatoMoneda';
import { InsigniaDeEstadoDePago } from './InsigniaDeEstado';
import { fechaDelPago, rutaDelRecibo } from './textosPagos';

/**
 * Los pagos de un jugador, del más reciente al más antiguo, con su estado en texto: el medio, la
 * entidad, la fecha y, si lo rechazaron, el motivo que ve la familia (spec 015, RF-001 y RF-022). Un
 * pago anulado sigue a la vista, tachado, con quién lo anuló y por qué (RF-019). Cada pago confirmado o
 * anulado enlaza a su recibo y su resguardo (RF-025).
 */
export function ListaDePagos({ clubId, pagos }: { clubId: string; pagos: PagoDto[] }) {
  if (pagos.length === 0) {
    return <p className="texto-suave">Todavía no hay pagos.</p>;
  }

  return (
    <ul className="lista-de-pagos">
      {pagos.map((pago) => (
        <li key={pago.id}>
          <span className="columna">
            <span className={pago.estado === 'ANULADO' ? 'pago-anulado' : undefined}>
              <strong>{formatoMoneda(pago.valor)}</strong>
              <span className="texto-suave">
                {' '}
                · {pago.medio === 'EFECTIVO' ? 'Efectivo' : (pago.entidad ?? 'Transferencia')} · {fechaDelPago(pago.fecha)}
              </span>
            </span>
            {pago.tieneResguardo && <Link to={rutaDelRecibo(clubId, pago.id)}>Ver recibo</Link>}
            {pago.estado === 'RECHAZADO' && pago.motivoRechazo && (
              <span className="texto-suave">Motivo del rechazo: {pago.motivoRechazo}</span>
            )}
            {pago.estado === 'ANULADO' && pago.motivoAnulacion && (
              <span className="texto-suave">
                Anulado por {pago.anuladoPorNombre}: {pago.motivoAnulacion}
              </span>
            )}
          </span>
          <InsigniaDeEstadoDePago estado={pago.estado} />
        </li>
      ))}
    </ul>
  );
}
