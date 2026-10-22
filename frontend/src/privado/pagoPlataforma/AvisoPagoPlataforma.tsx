import { Link } from 'react-router-dom';
import { Aviso } from '../../compartido/componentes/Aviso';
import { formatoMoneda } from '../pagos/formatoMoneda';
import { AvisoSuspensionPorImpago } from './AvisoSuspensionPorImpago';
import { textoPagaHasta } from './textosPagoPlataforma';
import { usePagoPlataforma } from './usePagoPlataforma';

/**
 * Aviso fijo del inicio del PRESIDENTE sobre el cobro de la plataforma (spec 017, RF-020): ámbar con el
 * valor y hasta cuándo se paga mientras haya un cobro pendiente, y rojo (en su lugar) si el club está
 * suspendido por impago. Mientras carga no muestra nada y un error no bloquea el resto del inicio.
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

  if (datos.pendientes.length === 0) {
    return null;
  }

  return (
    <Aviso tono="aviso">
      <div className="aviso-con-accion">
        <span>
          <span aria-hidden="true">!</span> Tienes un cobro de la plataforma por{' '}
          <strong>{formatoMoneda(datos.totalPendiente)}</strong>. {textoPagaHasta(datos.pendientes[0].suspendeDesde)}.
        </span>
        <Link to={`/club/${clubId}/pago-plataforma`}>Ver y pagar</Link>
      </div>
    </Aviso>
  );
}
