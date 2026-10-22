import { useState } from 'react';
import { ErrorApi, mensajeDe } from '../../compartido/api/errores';
import type { PagoDto, RegistrarPagoDto } from '../../compartido/api/tiposPagos';
import { apiPagos } from './apiPagos';

/** Lo que dejó el último intento de registrar un pago. */
export interface ResultadoDeRegistro {
  /** El mensaje del error, o `null`. */
  error: string | null;
  /** Los errores por campo de `datos_invalidos`. */
  errores: Record<string, string[]>;
}

/**
 * Registra un pago recibido directamente (spec 015, RF-020). Devuelve el pago creado, o `null` si la API
 * lo rechazó, dejando en `resultado` el mensaje: un valor mayor que lo pendiente trae lo que se debe en
 * el propio mensaje (`pago_excede_deuda`).
 */
export function useRegistrarPago(clubId: string, usuarioRolId: string) {
  const [enviando, setEnviando] = useState(false);
  const [resultado, setResultado] = useState<ResultadoDeRegistro>({ error: null, errores: {} });

  async function registrar(datos: RegistrarPagoDto): Promise<PagoDto | null> {
    setEnviando(true);
    setResultado({ error: null, errores: {} });
    try {
      return await apiPagos.registrar(clubId, usuarioRolId, datos);
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

  return { registrar, enviando, resultado };
}
