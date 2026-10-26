import { useState } from 'react';
import { Link } from 'react-router-dom';
import { formatoMoneda } from '../pagos/formatoMoneda';
import { AvisoSuspensionPorImpago } from './AvisoSuspensionPorImpago';
import { textoPagaHasta } from './textosPagoPlataforma';
import { usePagoPlataforma } from './usePagoPlataforma';

const CLAVE_FRANJA_CERRADA = 'franjaPagoPlataformaCerrada:';

/** Si la persona ya cerró la franja verde de ese club en esta pestaña. */
function franjaCerrada(clubId: string): boolean {
  try {
    return sessionStorage.getItem(CLAVE_FRANJA_CERRADA + clubId) === '1';
  } catch {
    return false;
  }
}

/** Recuerda, solo en esta pestaña, que la franja verde de ese club se cerró. */
function recordarFranjaCerrada(clubId: string): void {
  try {
    sessionStorage.setItem(CLAVE_FRANJA_CERRADA + clubId, '1');
  } catch {
    // Sin almacenamiento la franja volverá a verse al recargar: no es grave.
  }
}

/**
 * Franja del inicio del PRESIDENTE con su situación frente a la plataforma (spec 017, RF-020): verde si está
 * al día (aunque tenga un cobro pendiente dentro del plazo, que se dice) y roja si está en mora, con el
 * enlace a la pantalla de Plataforma. Si el club está suspendido por impago va, en su lugar, el aviso de
 * suspensión. La verde se puede cerrar con una X (queda cerrada mientras dure la pestaña); la roja y la de
 * suspensión no, porque avisan de una deuda. Sin cobro todavía, o mientras carga o si falla, no muestra nada y no bloquea el inicio. El
 * estado lo calcula el servidor; la franja solo lo pinta, con ícono y texto, no solo con color.
 * Solo se monta en la rama del PRESIDENTE: los demás roles ni siquiera lo piden.
 */
export function AvisoPagoPlataforma({ clubId }: { clubId: string }) {
  const { datos } = usePagoPlataforma(clubId);
  const [cerrada, setCerrada] = useState(() => franjaCerrada(clubId));

  if (!datos) {
    return null;
  }

  if (datos.suspendidoPorImpago) {
    return <AvisoSuspensionPorImpago clubId={clubId} totalPendiente={datos.totalPendiente} />;
  }

  const enlace = `/club/${clubId}/pago-plataforma`;

  if (datos.estadoDePago === 'VENCIDO') {
    return (
      <div className="aviso tono-error franja-pago" role="alert">
        <span>
          <span aria-hidden="true">⚠</span> En mora con la plataforma: debes{' '}
          <strong>{formatoMoneda(datos.totalPendiente)}</strong>.
        </span>
        <Link to={enlace}>Ver y pagar</Link>
      </div>
    );
  }

  if (!cerrada && (datos.estadoDePago === 'AL_DIA' || datos.estadoDePago === 'PENDIENTE')) {
    return (
      <div className="aviso tono-exito franja-pago" role="status">
        <span>
          <span aria-hidden="true">✓</span> Estás al día con la plataforma
          {datos.pendientes.length > 0 &&
            ` · próximo pago ${formatoMoneda(datos.totalPendiente)}, ${textoPagaHasta(datos.pendientes[0].suspendeDesde).toLowerCase()}`}
        </span>
        <span className="franja-pago-acciones">
          <Link to={enlace}>Ver suscripción</Link>
          <button
            type="button"
            className="franja-pago-cerrar"
            aria-label="Cerrar el aviso"
            title="Cerrar"
            onClick={() => {
              recordarFranjaCerrada(clubId);
              setCerrada(true);
            }}
          >
            <span aria-hidden="true">✕</span>
          </button>
        </span>
      </div>
    );
  }

  return null;
}
