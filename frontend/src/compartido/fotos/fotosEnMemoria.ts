import { useEffect, useState } from 'react';
import { api } from '../api/cliente';

/**
 * Fotos de personas que se piden a la API con la sesión y se muestran desde memoria, nunca desde una
 * dirección pública (§14.2): la de los jugadores (spec 007) y la de perfil de presidentes,
 * directivos y entrenadores ante su club (spec 008). Cada foto se guarda por su ruta y un sello que
 * cambia cuando la persona la reemplaza (la fecha de carga o la versión): mientras el sello no
 * cambie no se vuelve a pedir, y al cambiar se pide la nueva y se suelta la anterior. Un sello
 * `null`, vacío o `0` significa que no hay foto y no se pide nada. Al cerrar sesión se vacía todo.
 */

/** Lo que distingue una versión de una foto: la fecha de su carga o un número de versión. */
export type Sello = string | number | null;

/** Descarga los bytes de una foto con la sesión; se puede sustituir en las pruebas. */
export type Pedir = (ruta: string) => Promise<Blob>;

interface FotoGuardada {
  llave: string;
  url: string;
}

const enMemoria = new Map<string, FotoGuardada>();
const pidiendo = new Map<string, Promise<string | null>>();

/** Llave de la foto de esa ruta con ese sello, o `null` si no hay foto. */
export function llaveDeFotoEnMemoria(ruta: string, sello: Sello): string | null {
  return sello ? `${ruta}@${sello}` : null;
}

/** Dirección local de la foto de esa ruta ya descargada con esa llave, o `null`. */
export function fotoGuardada(ruta: string, llave: string | null): string | null {
  const guardada = enMemoria.get(ruta);
  return llave && guardada?.llave === llave ? guardada.url : null;
}

/**
 * Dirección local de la foto de esa ruta. Sin foto no pide nada; si la API no la entrega, devuelve
 * `null` y se muestran las iniciales.
 */
export function cargarFoto(ruta: string, sello: Sello, pedir: Pedir = api.getArchivo): Promise<string | null> {
  const llave = llaveDeFotoEnMemoria(ruta, sello);
  if (!llave) {
    return Promise.resolve(null);
  }

  const yaEsta = fotoGuardada(ruta, llave);
  if (yaEsta) {
    return Promise.resolve(yaEsta);
  }

  const enCurso = pidiendo.get(llave);
  if (enCurso) {
    return enCurso;
  }

  const peticion = pedir(ruta)
    .then((archivo) => {
      const anterior = enMemoria.get(ruta);
      if (anterior) {
        URL.revokeObjectURL(anterior.url);
      }

      const url = URL.createObjectURL(archivo);
      enMemoria.set(ruta, { llave, url });
      return url;
    })
    .catch(() => null)
    .finally(() => pidiendo.delete(llave));

  pidiendo.set(llave, peticion);
  return peticion;
}

/** Suelta todas las fotos en memoria; se llama al cerrar sesión. */
export function vaciarFotosEnMemoria(): void {
  for (const { url } of enMemoria.values()) {
    URL.revokeObjectURL(url);
  }

  enMemoria.clear();
  pidiendo.clear();
}

/** Cuántas fotos hay en memoria; solo para las pruebas. */
export function cuantasFotosEnMemoria(): number {
  return enMemoria.size;
}

/**
 * Dirección local de la foto de esa ruta, o `null` mientras llega, si no tiene o si falló. Con
 * `activa` en falso todavía no la pide (por ejemplo, hasta que la fila entra en pantalla).
 */
export function useFotoEnMemoria(ruta: string, sello: Sello, activa = true): string | null {
  const llave = llaveDeFotoEnMemoria(ruta, sello);
  const [cargada, setCargada] = useState<FotoGuardada | null>(null);

  useEffect(() => {
    if (!activa || !llave) {
      return;
    }

    let cancelado = false;
    void cargarFoto(ruta, sello).then((url) => {
      if (!cancelado && url) {
        setCargada({ llave, url });
      }
    });

    return () => {
      cancelado = true;
    };
  }, [activa, ruta, sello, llave]);

  return fotoGuardada(ruta, llave) ?? (cargada && cargada.llave === llave ? cargada.url : null);
}
