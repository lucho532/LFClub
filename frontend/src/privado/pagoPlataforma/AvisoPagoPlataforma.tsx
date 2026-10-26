import { Link } from 'react-router-dom';
import { formatoMoneda } from '../pagos/formatoMoneda';
import { AvisoSuspensionPorImpago } from './AvisoSuspensionPorImpago';
import { textoPagaHasta } from './textosPagoPlataforma';
import { usePagoPlataforma } from './usePagoPlataforma';

/**
 * Franja del inicio del PRESIDENTE con su situación frente a la plataforma (spec 017, RF-020): verde si está
 * al día (aunque tenga un cobro pendiente dentro del plazo, que se dice) y roja si está en mora, con el
 * enlace a la pantalla de Plataforma. Si el club está suspendido por impago va, en su lugar, el aviso de
 * suspensión. Sin cobro todavía, o mientras carga o si falla, no muestra nada y no bloquea el inicio. El
 * estado lo calcula el servidor; la franja solo lo pinta, con ícono y texto, no solo con color.
 * Solo se monta en la rama del PRESIDENTE: los demás roles ni siquiera lo piden.
 */
export function AvisoPagoPlataforma({ clubId }: { clubId: string }) {
  const { datos } = usePagoPlataforma(clubId);

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

  if (datos.estadoDePago === 'AL_DIA' || datos.estadoDePago === 'PENDIENTE') {
    return (
      <div className="aviso tono-exito franja-pago" role="status">
        <span>
          <span aria-hidden="true">✓</span> Estás al día con la plataforma
          {datos.pendientes.length > 0 &&
            ` · próximo pago ${formatoMoneda(datos.totalPendiente)}, ${textoPagaHasta(datos.pendientes[0].suspendeDesde).toLowerCase()}`}
        </span>
        <Link to={enlace}>Ver plataforma</Link>
      </div>
    );
  }

  return null;
}
