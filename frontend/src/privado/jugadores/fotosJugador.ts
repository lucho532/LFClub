import { useEffect, useState, type RefObject } from 'react';
import { api } from '../../compartido/api/cliente';
import {
  cargarFoto,
  cuantasFotosEnMemoria,
  llaveDeFotoEnMemoria,
  useFotoEnMemoria,
  vaciarFotosEnMemoria,
  type Pedir,
} from '../../compartido/fotos/fotosEnMemoria';

/**
 * Fotos propias de los jugadores (spec 007). Son de menores: se piden a la API con la sesión, como
 * la foto de perfil, y se muestran desde memoria con `fotosEnMemoria`, nunca desde una dirección
 * pública (§14.2). El sello de cada foto es su fecha de carga: mientras no cambie no se vuelve a
 * pedir. Este módulo solo arma la ruta del jugador; guardar, soltar y vaciar es de `fotosEnMemoria`.
 */

export type { Pedir };

function rutaDeFoto(clubId: string, usuarioRolId: string): string {
  return `/api/clubes/${clubId}/jugadores/${usuarioRolId}/foto`;
}

/** Llave de la foto de un jugador en un momento dado, o `null` si no tiene foto. */
export function llaveDeFoto(usuarioRolId: string, fotoActualizadaEn: string | null): string | null {
  return llaveDeFotoEnMemoria(usuarioRolId, fotoActualizadaEn);
}

/**
 * Dirección local de la foto de ese jugador. Sin foto (`fotoActualizadaEn` nula) no pide nada; si
 * la API no la entrega, devuelve `null` y se muestran las iniciales.
 */
export function cargarFotoJugador(
  clubId: string,
  usuarioRolId: string,
  fotoActualizadaEn: string | null,
  pedir: Pedir = api.getArchivo,
): Promise<string | null> {
  return cargarFoto(rutaDeFoto(clubId, usuarioRolId), fotoActualizadaEn, pedir);
}

/** Suelta todas las fotos en memoria, también las de las personas; se llama al cerrar sesión. */
export function vaciarFotosJugador(): void {
  vaciarFotosEnMemoria();
}

/** Cuántas fotos hay en memoria; solo para las pruebas. */
export function fotosEnMemoria(): number {
  return cuantasFotosEnMemoria();
}

/**
 * Dirección local de la foto de ese jugador, o `null` mientras llega, si no tiene o si falló. Con
 * `activa` en falso todavía no la pide: la lista la activa cuando la fila entra en pantalla.
 */
export function useFotoJugador(
  clubId: string,
  usuarioRolId: string,
  fotoActualizadaEn: string | null,
  activa = true,
): string | null {
  return useFotoEnMemoria(rutaDeFoto(clubId, usuarioRolId), fotoActualizadaEn, activa);
}

/**
 * Verdadero desde que el elemento entra en pantalla (o queda a menos de una pantalla de hacerlo).
 * Sin `IntersectionObserver` se da por visible desde el principio.
 */
export function useEnPantalla(elemento: RefObject<Element | null>): boolean {
  const [visto, setVisto] = useState(() => typeof IntersectionObserver === 'undefined');

  useEffect(() => {
    const actual = elemento.current;
    if (visto || !actual) {
      return;
    }

    const observador = new IntersectionObserver(
      (entradas) => {
        if (entradas.some((entrada) => entrada.isIntersecting)) {
          setVisto(true);
          observador.disconnect();
        }
      },
      { rootMargin: '100% 0px' },
    );
    observador.observe(actual);
    return () => observador.disconnect();
  }, [elemento, visto]);

  return visto;
}
