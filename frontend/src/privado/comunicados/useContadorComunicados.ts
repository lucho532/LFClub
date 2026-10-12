import { useCallback, useEffect, useRef, useState } from 'react';
import { useLocation } from 'react-router-dom';
import { api } from '../../compartido/api/cliente';
import type { ContadorDeCampanaDto } from '../../compartido/api/tiposCampana';

/** Cada cuánto se vuelve a consultar el contador mientras la aplicación está visible (research §10). */
export const INTERVALO_DEL_CONTADOR_MS = 60_000;

interface Contador {
  /** Comunicados y avisos sin leer; 0 mientras no llega el primer número. */
  sinLeer: number;
  /** Fija el número con el que trae una respuesta (la lista o "Marcar todos"), sin otra consulta. */
  fijar: (sinLeer: number) => void;
  /**
   * Baja el número en uno porque se acaba de leer un comunicado o un aviso que estaba sin leer. Si todavía no
   * hay un número del que restar, lo pide a la API (que ya cuenta la lectura).
   */
  restarUno: () => void;
}

/**
 * El contador de la campana: comunicados y avisos sin leer de la cuenta (spec 011, RF-013 y RF-017;
 * spec 012, RF-018). Lo consulta al
 * montarse, al volver la aplicación a primer plano, al cambiar de pantalla y cada 60 segundos
 * mientras está visible. Si una consulta falla conserva el último número, sin mostrar error. Con
 * `activo` en falso (quien no es la cuenta de un jugador) no consulta nada.
 */
export function useContadorComunicados(clubId: string, activo: boolean): Contador {
  const [sinLeer, setSinLeer] = useState(0);
  const { pathname } = useLocation();
  const vigente = useRef(0);
  const conNumero = useRef(false);

  const consultar = useCallback(async () => {
    const numero = ++vigente.current;
    try {
      const respuesta = await api.get<ContadorDeCampanaDto>(`/api/clubes/${clubId}/campana/contador`);
      // Una respuesta atrasada no pisa a otra más nueva ni a un número fijado por una lectura.
      if (numero === vigente.current) {
        conNumero.current = true;
        setSinLeer(respuesta.sinLeer);
      }
    } catch {
      // Se conserva el último número: la campana no muestra errores.
    }
  }, [clubId]);

  const fijar = useCallback((nuevo: number) => {
    vigente.current++;
    conNumero.current = true;
    setSinLeer(nuevo);
  }, []);

  const restarUno = useCallback(() => {
    if (!conNumero.current) {
      void consultar();
      return;
    }

    vigente.current++;
    setSinLeer((actual) => Math.max(0, actual - 1));
  }, [consultar]);

  // Al montarse y al cambiar de pantalla.
  useEffect(() => {
    if (activo) {
      // La consulta solo cambia el estado cuando llega la respuesta, no durante el efecto.
      // eslint-disable-next-line react-hooks/set-state-in-effect
      void consultar();
    }
  }, [activo, consultar, pathname]);

  // Al volver a primer plano y cada 60 segundos mientras la aplicación está visible.
  useEffect(() => {
    if (!activo) {
      return;
    }

    const alVolver = () => {
      if (document.visibilityState === 'visible') {
        void consultar();
      }
    };
    const intervalo = window.setInterval(alVolver, INTERVALO_DEL_CONTADOR_MS);
    document.addEventListener('visibilitychange', alVolver);

    return () => {
      window.clearInterval(intervalo);
      document.removeEventListener('visibilitychange', alVolver);
    };
  }, [activo, consultar]);

  return { sinLeer: activo ? sinLeer : 0, fijar, restarUno };
}
