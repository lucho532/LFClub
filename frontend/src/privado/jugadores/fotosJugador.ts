import { useEffect, useState, type RefObject } from 'react';
import { api } from '../../compartido/api/cliente';

/**
 * Fotos propias de los jugadores (spec 007). Son de menores: se piden a la API con la sesión, como
 * la foto de perfil, y se muestran desde memoria, nunca desde una dirección pública (§14.2). Cada
 * foto se guarda por jugador y fecha de carga: mientras la fecha no cambie no se vuelve a pedir, y
 * al cambiar se pide la nueva y se suelta la anterior. Al cerrar sesión se vacía todo.
 */

interface FotoEnMemoria {
  llave: string;
  url: string;
}

/** Descarga los bytes de una foto con la sesión; se puede sustituir en las pruebas. */
export type Pedir = (ruta: string) => Promise<Blob>;

const enMemoria = new Map<string, FotoEnMemoria>();
const pidiendo = new Map<string, Promise<string | null>>();

/** Llave de la foto de un jugador en un momento dado, o `null` si no tiene foto. */
export function llaveDeFoto(usuarioRolId: string, fotoActualizadaEn: string | null): string | null {
  return fotoActualizadaEn ? `${usuarioRolId}@${fotoActualizadaEn}` : null;
}

/** Dirección local de una foto ya descargada con esa llave, o `null`. */
export function fotoEnMemoria(usuarioRolId: string, llave: string | null): string | null {
  const guardada = enMemoria.get(usuarioRolId);
  return llave && guardada?.llave === llave ? guardada.url : null;
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
  const llave = llaveDeFoto(usuarioRolId, fotoActualizadaEn);
  if (!llave) {
    return Promise.resolve(null);
  }

  const yaEsta = fotoEnMemoria(usuarioRolId, llave);
  if (yaEsta) {
    return Promise.resolve(yaEsta);
  }

  const enCurso = pidiendo.get(llave);
  if (enCurso) {
    return enCurso;
  }

  const peticion = pedir(`/api/clubes/${clubId}/jugadores/${usuarioRolId}/foto`)
    .then((archivo) => {
      const anterior = enMemoria.get(usuarioRolId);
      if (anterior) {
        URL.revokeObjectURL(anterior.url);
      }

      const url = URL.createObjectURL(archivo);
      enMemoria.set(usuarioRolId, { llave, url });
      return url;
    })
    .catch(() => null)
    .finally(() => pidiendo.delete(llave));

  pidiendo.set(llave, peticion);
  return peticion;
}

/** Suelta todas las fotos en memoria; se llama al cerrar sesión. */
export function vaciarFotosJugador(): void {
  for (const { url } of enMemoria.values()) {
    URL.revokeObjectURL(url);
  }

  enMemoria.clear();
  pidiendo.clear();
}

/** Cuántas fotos hay en memoria; solo para las pruebas. */
export function fotosEnMemoria(): number {
  return enMemoria.size;
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
  const llave = llaveDeFoto(usuarioRolId, fotoActualizadaEn);
  const [cargada, setCargada] = useState<FotoEnMemoria | null>(null);

  useEffect(() => {
    if (!activa || !llave) {
      return;
    }

    let cancelado = false;
    void cargarFotoJugador(clubId, usuarioRolId, fotoActualizadaEn).then((url) => {
      if (!cancelado && url) {
        setCargada({ llave, url });
      }
    });

    return () => {
      cancelado = true;
    };
  }, [activa, clubId, usuarioRolId, fotoActualizadaEn, llave]);

  return fotoEnMemoria(usuarioRolId, llave) ?? (cargada && cargada.llave === llave ? cargada.url : null);
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
