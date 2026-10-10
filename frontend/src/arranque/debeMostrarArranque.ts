import { Capacitor } from '@capacitor/core';

/** Clave del almacenamiento de la sesión que dice que la animación ya se vio en esta apertura. */
export const CLAVE_ARRANQUE = 'lapecosa.arranqueVisto';

type Almacen = Pick<Storage, 'getItem' | 'setItem'>;

function almacenDeLaSesion(): Almacen | null {
  try {
    return sessionStorage;
  } catch {
    return null;
  }
}

/**
 * Indica si hay que mostrar la animación de arranque, y la da por vista. Solo en la aplicación
 * Android (en el navegador no hay arranque que continuar) y una vez por apertura: el almacenamiento
 * de la sesión dura lo que la aplicación está abierta, así que recargar la página no la repite.
 * Sin almacenamiento disponible se muestra, porque no hay forma de saber si ya se vio.
 */
export function debeMostrarArranque(
  esAplicacionNativa: boolean = Capacitor.isNativePlatform(),
  almacen: Almacen | null = almacenDeLaSesion(),
): boolean {
  if (!esAplicacionNativa) {
    return false;
  }

  try {
    if (almacen?.getItem(CLAVE_ARRANQUE)) {
      return false;
    }
    almacen?.setItem(CLAVE_ARRANQUE, '1');
  } catch {
    // Sin almacenamiento se muestra igual.
  }

  return true;
}
