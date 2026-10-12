// Geometría del balón de LFClub en un lienzo de 108 x 108, la misma que dibujan los recursos de
// Android (drawable/balon_icono.xml y drawable/balon_arranque.xml). Generado por
// scripts/generar-balon.py: si cambia el dibujo, se regeneran los dos lados a la vez.

/** Circunferencia del balón: radio 30 centrado en (54, 54). */
export const CONTORNO = 'M24,54 A30,30 0 1,1 84,54 A30,30 0 1,1 24,54 Z';

/** Los seis paneles azules en estrella; se recortan con el contorno. */
export const PANELES = 'M54,45 Q56.34,52.65 61.79,58.5 Q70.65,50.43 76.08,41.25 Q65.41,41.36 54,45 Z M61.79,58.5 Q54,56.7 46.21,58.5 Q48.76,70.2 54,79.5 Q59.24,70.2 61.79,58.5 Z M46.21,58.5 Q51.66,52.65 54,45 Q42.59,41.36 31.92,41.25 Q37.35,50.43 46.21,58.5 Z M37.07,32.33 Q54,34.13 70.93,32.33 Q64.15,22.02 54,13 Q43.85,22.02 37.07,32.33 Z M81.23,50.17 Q71.21,63.94 64.3,79.5 Q76.62,78.78 89.51,74.5 Q86.77,61.2 81.23,50.17 Z M43.7,79.5 Q36.79,63.94 26.77,50.17 Q21.23,61.2 18.49,74.5 Q31.38,78.78 43.7,79.5 Z';

/** Sombra abajo a la derecha: el contorno menos un círculo desplazado (relleno par-impar). */
export const SOMBRA = 'M24,54 A30,30 0 1,1 84,54 A30,30 0 1,1 24,54 Z M17,48 A31,31 0 1,1 79,48 A31,31 0 1,1 17,48 Z';

/** Brillo arriba a la izquierda. */
export const BRILLO = 'M37.5,42 A5.5,5.5 0 1,1 48.5,42 A5.5,5.5 0 1,1 37.5,42 Z';
