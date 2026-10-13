/** Lo que acepta la API para una foto o un escudo: 1 MB. Se deja un margen para no rozar el límite. */
const LIMITE_BYTES = 900 * 1024;
/** Lado mayor de la imagen reducida: de sobra para un círculo de foto o un escudo. */
const LADO_MAXIMO = 1024;
const CALIDADES = [0.9, 0.8, 0.7, 0.6, 0.5];

function aBlob(lienzo: HTMLCanvasElement, tipo: string, calidad: number): Promise<Blob | null> {
  return new Promise((resolver) => lienzo.toBlob(resolver, tipo, calidad));
}

function conExtension(nombre: string, tipo: string): string {
  const base = nombre.replace(/\.[^.]+$/, '') || 'imagen';
  return `${base}.${tipo === 'image/webp' ? 'webp' : 'jpg'}`;
}

/**
 * Las fotos del teléfono pesan varios MB y la API no admite más de 1 MB. Si la imagen es más
 * liviana, se devuelve tal cual. Si no, se reduce en el navegador (lado mayor de 1024 px, WebP o
 * JPEG con la calidad más alta que quepa) y se conserva la transparencia de los escudos. Si el
 * navegador no puede procesarla, se devuelve el original para que la API explique el motivo.
 */
export async function reducirImagen(archivo: File): Promise<File> {
  if (archivo.size <= LIMITE_BYTES || typeof createImageBitmap !== 'function') {
    return archivo;
  }

  try {
    const imagen = await createImageBitmap(archivo);
    const escala = Math.min(1, LADO_MAXIMO / Math.max(imagen.width, imagen.height));
    const lienzo = document.createElement('canvas');
    lienzo.width = Math.max(1, Math.round(imagen.width * escala));
    lienzo.height = Math.max(1, Math.round(imagen.height * escala));
    lienzo.getContext('2d')?.drawImage(imagen, 0, 0, lienzo.width, lienzo.height);
    imagen.close();

    for (const calidad of CALIDADES) {
      // WebP admite transparencia; si el navegador no lo codifica devuelve PNG y se prueba JPEG.
      for (const tipo of ['image/webp', 'image/jpeg']) {
        const blob = await aBlob(lienzo, tipo, calidad);
        if (blob && blob.type === tipo && blob.size <= LIMITE_BYTES) {
          return new File([blob], conExtension(archivo.name, tipo), { type: tipo });
        }
      }
    }
  } catch {
    // Formato que el navegador no decodifica: se envía el original y la API dice por qué no.
  }

  return archivo;
}
