import { Link } from 'react-router-dom';
import { Aviso } from '../../compartido/componentes/Aviso';
import { usePagosEnRevision } from '../pagos/usePagosEnRevision';

/**
 * Aviso del inicio del PRESIDENTE con cuántas transferencias esperan su revisión (spec 015, RF-022).
 * Es un contador interno de trabajo pendiente, no un recordatorio de cobro (§16.8): no habla de lo que
 * deben los jugadores. Sin transferencias en revisión no pinta nada.
 */
export function TarjetaPagosEnRevision({ clubId }: { clubId: string }) {
  const revision = usePagosEnRevision(clubId);
  const total = revision.datos?.total ?? 0;

  if (total === 0) {
    return null;
  }

  return (
    <Aviso tono="info">
      <div className="aviso-con-accion">
        <span>
          {total === 1 ? 'Hay 1 transferencia esperando su revisión.' : `Hay ${total} transferencias esperando su revisión.`}
        </span>
        <Link to={`/club/${clubId}/pagos/en-revision`}>Revisar</Link>
      </div>
    </Aviso>
  );
}
