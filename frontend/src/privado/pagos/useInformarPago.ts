import { useState } from 'react';
import { ErrorApi, mensajeDe } from '../../compartido/api/errores';
import type { PagoDto } from '../../compartido/api/tiposPagos';
import { apiRevision, type TransferenciaInformada } from './apiRevision';
import type { ResultadoDeRegistro } from './useRegistrarPago';

/**
 * Informa una transferencia con su soporte (spec 015, RF-021). Devuelve el pago que quedó en revisión,
 * o `null` si la API lo rechazó, dejando en `resultado` el mensaje o los errores por campo. No cambia
 * ningún saldo: el pago espera la revisión del presidente.
 */
export function useInformarPago(clubId: string, usuarioRolId: string) {
  const [enviando, setEnviando] = useState(false);
  const [resultado, setResultado] = useState<ResultadoDeRegistro>({ error: null, errores: {} });

  async function informar(datos: TransferenciaInformada): Promise<PagoDto | null> {
    setEnviando(true);
    setResultado({ error: null, errores: {} });
    try {
      return await apiRevision.informar(clubId, usuarioRolId, datos);
    } catch (fallo) {
      if (fallo instanceof ErrorApi && fallo.codigo === 'datos_invalidos') {
        setResultado({ error: null, errores: fallo.errores });
      } else {
        setResultado({ error: mensajeDe(fallo), errores: {} });
      }

      return null;
    } finally {
      setEnviando(false);
    }
  }

  return { informar, enviando, resultado };
}
