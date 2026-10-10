import { borrarToken, leerToken } from '../sesion/almacenToken';
import { CABECERA_JUGADOR_ELEGIDO, leerJugadorElegido } from '../sesion/jugadorElegido';
import { URL_API } from './configuracion';
import { errorDeRespuesta, errorSinConexion } from './errores';

type Metodo = 'GET' | 'POST' | 'PUT' | 'DELETE';

const RUTA_DE_CLUB = /^\/api\/clubes\/([^/?]+)/;

/**
 * El jugador con el que la familia continúa en el club de esa ruta, o `null` si la ruta no es de
 * un club o no hay elección: entonces la petición es la de siempre, sin cabecera.
 */
function jugadorElegidoPara(ruta: string): string | null {
  const clubId = RUTA_DE_CLUB.exec(ruta)?.[1];
  return clubId ? leerJugadorElegido(clubId) : null;
}

/** Si la API dice que la sesión ya no sirve, se borra y se vuelve al inicio de sesión. */
function alPerderLaSesion(): void {
  borrarToken();
  if (window.location.pathname !== '/entrar') {
    window.location.assign('/entrar');
  }
}

async function enviar(metodo: Metodo, ruta: string, cuerpo?: unknown): Promise<Response> {
  const cabeceras: Record<string, string> = {};
  const token = leerToken();
  if (token) {
    cabeceras.Authorization = `Bearer ${token}`;
  }

  const jugadorElegido = jugadorElegidoPara(ruta);
  if (jugadorElegido) {
    cabeceras[CABECERA_JUGADOR_ELEGIDO] = jugadorElegido;
  }

  let contenido: BodyInit | undefined;
  if (cuerpo instanceof FormData) {
    contenido = cuerpo;
  } else if (cuerpo !== undefined) {
    cabeceras['Content-Type'] = 'application/json';
    contenido = JSON.stringify(cuerpo);
  }

  let respuesta: Response;
  try {
    respuesta = await fetch(`${URL_API}${ruta}`, { method: metodo, headers: cabeceras, body: contenido });
  } catch {
    throw errorSinConexion();
  }

  if (!respuesta.ok) {
    const error = await errorDeRespuesta(respuesta);
    if (respuesta.status === 401 && error.codigo === 'sin_sesion') {
      alPerderLaSesion();
    }

    throw error;
  }

  return respuesta;
}

async function comoJson<T>(respuesta: Response): Promise<T> {
  if (respuesta.status === 204 || respuesta.status === 202) {
    return undefined as T;
  }

  return (await respuesta.json()) as T;
}

/**
 * Cliente de la API sobre `fetch`: añade la sesión y, en las rutas de un club, el jugador elegido,
 * y convierte los errores en `ErrorApi`.
 */
export const api = {
  get: async <T>(ruta: string) => comoJson<T>(await enviar('GET', ruta)),
  post: async <T = void>(ruta: string, cuerpo?: unknown) => comoJson<T>(await enviar('POST', ruta, cuerpo)),
  put: async <T = void>(ruta: string, cuerpo: unknown) => comoJson<T>(await enviar('PUT', ruta, cuerpo)),
  delete: async <T = void>(ruta: string) => comoJson<T>(await enviar('DELETE', ruta)),

  /** Envía un archivo en el campo `archivo` de un formulario `multipart/form-data`. */
  putArchivo: async <T>(ruta: string, archivo: File) => {
    const formulario = new FormData();
    formulario.append('archivo', archivo);
    return comoJson<T>(await enviar('PUT', ruta, formulario));
  },

  /** Descarga un archivo que exige sesión (por ejemplo, la foto de perfil). */
  getArchivo: async (ruta: string) => (await enviar('GET', ruta)).blob(),

  /** Dirección completa de un recurso público de la API, como el escudo. */
  url: (ruta: string) => `${URL_API}${ruta}`,
};
