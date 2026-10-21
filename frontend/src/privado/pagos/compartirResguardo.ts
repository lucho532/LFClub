/**
 * Descargar y compartir el resguardo de pago (spec 015, RF-025). El resguardo es una imagen que la familia
 * guarda o envía por donde quiera; no hay enlaces públicos (§14.2): lo que se comparte es el archivo, no
 * una dirección. Compartir usa el menú del sistema con el archivo (Web Share) donde el navegador lo admite;
 * donde no, solo queda descargar.
 */

/** Nombre del archivo de un resguardo: `resguardo-12.png`. */
export function nombreDelResguardo(numeroRecibo: number | null): string {
  return `resguardo-${numeroRecibo ?? 'pago'}.png`;
}

/** Guarda la imagen en el dispositivo con ese nombre. */
export function descargarResguardo(imagen: Blob, nombre: string): void {
  const direccion = URL.createObjectURL(imagen);
  const enlace = document.createElement('a');
  enlace.href = direccion;
  enlace.download = nombre;
  document.body.appendChild(enlace);
  enlace.click();
  enlace.remove();
  URL.revokeObjectURL(direccion);
}

function comoArchivo(imagen: Blob, nombre: string): File {
  return new File([imagen], nombre, { type: 'image/png' });
}

/** Indica si el dispositivo puede abrir el menú de compartir con un archivo. */
export function puedeCompartirResguardo(imagen: Blob | null, nombre: string): boolean {
  if (!imagen || typeof navigator === 'undefined' || typeof navigator.share !== 'function') {
    return false;
  }

  return typeof navigator.canShare === 'function' && navigator.canShare({ files: [comoArchivo(imagen, nombre)] });
}

/**
 * Abre el menú de compartir con la imagen. Devuelve `false` si la persona lo cerró sin elegir nada, que
 * no es un error; cualquier otro fallo se propaga.
 */
export async function compartirResguardo(imagen: Blob, nombre: string, titulo: string): Promise<boolean> {
  try {
    await navigator.share({ files: [comoArchivo(imagen, nombre)], title: titulo });
    return true;
  } catch (fallo) {
    if (fallo instanceof DOMException && fallo.name === 'AbortError') {
      return false;
    }

    throw fallo;
  }
}
