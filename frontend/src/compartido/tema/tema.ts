import type { Tema } from './contraste';

/**
 * Clave del almacenamiento local donde vive la elección de tema. El script de index.html usa la
 * misma para aplicar el tema antes de pintar.
 */
export const CLAVE_TEMA = 'lfclub.tema';

function esTema(valor: unknown): valor is Tema {
  return valor === 'claro' || valor === 'oscuro';
}

/** Tema que la persona eligió en este dispositivo, o `null` si no ha elegido ninguno. */
export function temaElegido(): Tema | null {
  try {
    const guardado = localStorage.getItem(CLAVE_TEMA);
    return esTema(guardado) ? guardado : null;
  } catch {
    return null;
  }
}

/** Tema del dispositivo según `prefers-color-scheme`. */
export function temaDelDispositivo(): Tema {
  try {
    return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'oscuro' : 'claro';
  } catch {
    return 'claro';
  }
}

/** Tema que corresponde ahora: el elegido y, si no hay elección, el del dispositivo (RF-033). */
export function temaActual(): Tema {
  return temaElegido() ?? temaDelDispositivo();
}

/** Aplica un tema a toda la interfaz con el atributo `data-tema` de `<html>`. */
export function aplicarTema(tema: Tema): void {
  document.documentElement.setAttribute('data-tema', tema);
}

/** Guarda la elección en el dispositivo y la aplica. Sin almacenamiento, solo la aplica. */
export function elegirTema(tema: Tema): void {
  try {
    localStorage.setItem(CLAVE_TEMA, tema);
  } catch {
    // La elección dura lo que dure la página.
  }

  aplicarTema(tema);
}

/** El otro tema. */
export function temaContrario(tema: Tema): Tema {
  return tema === 'claro' ? 'oscuro' : 'claro';
}
