import type { TokenSesionDto } from '../api/tipos';

const CLAVE = 'lfclub.sesion';

interface TokenGuardado {
  token: string;
  venceEn: string;
  guardadoEn: string;
}

function leerGuardado(): TokenGuardado | null {
  try {
    const texto = localStorage.getItem(CLAVE);
    return texto ? (JSON.parse(texto) as TokenGuardado) : null;
  } catch {
    return null;
  }
}

/** Token de la sesión guardada en el dispositivo, o `null` si no hay ninguna. */
export function leerToken(): string | null {
  return leerGuardado()?.token ?? null;
}

/** Guarda el token recibido de la API en el almacenamiento local. */
export function guardarToken(dto: TokenSesionDto): void {
  const guardado: TokenGuardado = { ...dto, guardadoEn: new Date().toISOString() };
  try {
    localStorage.setItem(CLAVE, JSON.stringify(guardado));
  } catch {
    // Sin almacenamiento disponible la sesión dura lo que dure la pestaña.
  }
}

/** Borra la sesión del dispositivo. */
export function borrarToken(): void {
  try {
    localStorage.removeItem(CLAVE);
  } catch {
    // Nada que borrar.
  }
}

/** Indica si al token le queda menos de la mitad de su vigencia y conviene renovarlo. */
export function convieneRenovar(ahora: Date = new Date()): boolean {
  const guardado = leerGuardado();
  if (!guardado) {
    return false;
  }

  const vence = Date.parse(guardado.venceEn);
  const inicio = Date.parse(guardado.guardadoEn);
  if (Number.isNaN(vence) || Number.isNaN(inicio)) {
    return false;
  }

  return vence - ahora.getTime() < (vence - inicio) / 2;
}
