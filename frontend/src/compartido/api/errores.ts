import type { Problema } from './tipos';

/** Error de la API: lleva el código estable del contrato, el mensaje en español y los errores por campo. */
export class ErrorApi extends Error {
  readonly codigo: string;
  readonly status: number;
  readonly title: string;
  readonly errores: Record<string, string[]>;
  /** Campos propios del código (por ejemplo `clubes` en `presidente_unico`), tal como llegaron. */
  readonly extras: Record<string, unknown>;

  constructor(problema: Problema) {
    super(problema.title);
    this.name = 'ErrorApi';
    this.codigo = problema.codigo;
    this.status = problema.status;
    this.title = problema.title;
    this.errores = problema.errores ?? {};
    this.extras = { ...problema };
  }

  /** Primer mensaje de un campo, o `undefined` si ese campo no tiene errores. */
  errorDe(campo: string): string | undefined {
    return this.errores[campo]?.[0];
  }
}

const SIN_CONEXION: Problema = {
  title: 'No se pudo conectar con el servidor. Revisa tu conexión e inténtalo de nuevo.',
  status: 0,
  codigo: 'sin_conexion',
};

/** Convierte una respuesta de error en un `ErrorApi`, aunque el cuerpo no sea `problem+json`. */
export async function errorDeRespuesta(respuesta: Response): Promise<ErrorApi> {
  try {
    const cuerpo = (await respuesta.json()) as Partial<Problema>;
    if (typeof cuerpo.codigo === 'string' && typeof cuerpo.title === 'string') {
      return new ErrorApi({ ...cuerpo, status: respuesta.status } as Problema);
    }
  } catch {
    // El cuerpo no es JSON: se usa el mensaje genérico de abajo.
  }

  return new ErrorApi({
    title: 'Ocurrió un error inesperado. Inténtalo de nuevo.',
    status: respuesta.status,
    codigo: 'error_inesperado',
  });
}

/** Error para cuando la petición no llegó al servidor. */
export function errorSinConexion(): ErrorApi {
  return new ErrorApi(SIN_CONEXION);
}

/** Mensaje para mostrar de cualquier error capturado. */
export function mensajeDe(error: unknown): string {
  return error instanceof ErrorApi ? error.title : 'Ocurrió un error inesperado. Inténtalo de nuevo.';
}
