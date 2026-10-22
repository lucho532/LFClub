import { useState } from 'react';
import { mensajeDe } from '../../compartido/api/errores';
import type { PagoDto } from '../../compartido/api/tiposPagos';
import { Boton } from '../../compartido/componentes/Boton';
import { Campo } from '../../compartido/componentes/Campo';
import { DialogoConfirmacion } from '../../compartido/componentes/DialogoConfirmacion';
import { apiPagos } from './apiPagos';
import { formatoMoneda } from './formatoMoneda';

interface Props {
  clubId: string;
  pago: PagoDto;
  /** Recibe el pago ya anulado, tal como lo devolvió la API. */
  alAnular: (pago: PagoDto) => void;
}

/**
 * Botón y diálogo con los que el PRESIDENTE anula un pago confirmado (spec 015, RF-019). El motivo es
 * obligatorio. El pago no se borra: sigue visible, marcado como anulado con quién, cuándo y por qué, y el
 * saldo del jugador se recalcula.
 */
export function AnularPago({ clubId, pago, alAnular }: Props) {
  const [abierto, setAbierto] = useState(false);
  const [motivo, setMotivo] = useState('');
  const [error, setError] = useState<string | undefined>();
  const [cargando, setCargando] = useState(false);

  async function anular() {
    setError(undefined);
    setCargando(true);
    try {
      alAnular(await apiPagos.anular(clubId, pago.id, motivo.trim()));
      setAbierto(false);
    } catch (fallo) {
      setError(mensajeDe(fallo));
    } finally {
      setCargando(false);
    }
  }

  return (
    <>
      <div className="fila">
        <Boton variante="peligro" onClick={() => setAbierto(true)}>
          Anular
        </Boton>
      </div>
      <DialogoConfirmacion
        abierto={abierto}
        titulo="Anular este pago"
        textoConfirmar="Anular pago"
        peligro
        deshabilitado={motivo.trim().length === 0}
        cargando={cargando}
        error={error}
        alConfirmar={() => void anular()}
        alCancelar={() => setAbierto(false)}
      >
        <p>
          El pago de {formatoMoneda(pago.valor)} no se borra: queda marcado como anulado y el saldo del jugador se
          recalcula.
        </p>
        <Campo etiqueta="Motivo de la anulación" valor={motivo} alCambiar={setMotivo} lineas={3} maxLength={300} required />
      </DialogoConfirmacion>
    </>
  );
}
