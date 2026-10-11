import { useLocation } from 'react-router-dom';

/** A dónde vuelve el enlace "← …" de una ficha y qué dice. */
export interface Volver {
  ruta: string;
  texto: string;
}

/** Estado de React Router con el que un enlace le dice a la ficha de dónde se llegó. */
export interface EstadoVolver {
  volver: Volver;
}

/** El estado que lleva quien arma un enlace a una ficha, para que volver regrese a su pantalla. */
export function estadoVolver(ruta: string, texto: string): EstadoVolver {
  return { volver: { ruta, texto } };
}

/**
 * El origen guardado en el estado, si está bien formado; si no está (la dirección se abrió
 * directamente) o no sirve, el predeterminado. Solo se admiten rutas propias de la aplicación.
 */
export function volverDesde(estado: unknown, predeterminado: Volver): Volver {
  const volver = (estado as Partial<EstadoVolver> | null)?.volver;
  const valido =
    typeof volver?.ruta === 'string' &&
    volver.ruta.startsWith('/') &&
    !volver.ruta.startsWith('//') &&
    typeof volver.texto === 'string' &&
    volver.texto.trim() !== '';

  return valido ? { ruta: volver.ruta, texto: volver.texto } : predeterminado;
}

/**
 * A dónde vuelve la ficha que se está viendo (RF-013 de la 008): a la pantalla desde la que se
 * pulsó el nombre, con su nombre en el enlace, o al predeterminado de hoy si no se llegó desde un
 * enlace de la aplicación.
 */
export function useVolver(predeterminado: Volver): Volver {
  return volverDesde(useLocation().state, predeterminado);
}
