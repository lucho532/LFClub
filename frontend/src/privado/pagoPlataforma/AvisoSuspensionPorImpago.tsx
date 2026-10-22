import { Link } from 'react-router-dom';
import { Aviso } from '../../compartido/componentes/Aviso';
import { formatoMoneda } from '../pagos/formatoMoneda';

interface Props {
  clubId: string;
  totalPendiente: number;
}

/**
 * Aviso fijo del inicio del PRESIDENTE cuando su club está suspendido por falta de pago (spec 017,
 * RF-020): dice el valor y lleva a pagar. Va con ícono y texto, no solo con color. No se puede cerrar.
 * No decide la suspensión: la decide el servidor.
 */
export function AvisoSuspensionPorImpago({ clubId, totalPendiente }: Props) {
  return (
    <Aviso tono="error">
      <div className="aviso-con-accion">
        <span>
          <span aria-hidden="true">⚠</span> Tu club está suspendido por falta de pago
          {totalPendiente > 0 ? `: debes ${formatoMoneda(totalPendiente)}` : ''}. Mientras tanto, solo tú puedes entrar.
        </span>
        <Link to={`/club/${clubId}/pago-plataforma`}>Pagar ahora</Link>
      </div>
    </Aviso>
  );
}
