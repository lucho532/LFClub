// Plano común de la carta del jugador: specs/010-ficha-resultado-y-carta/contracts/carta.md.
// Las medidas están en unidades del lienzo de la imagen (750 × 1050); la pantalla las usa como
// porcentajes. La API dibuja la imagen con los mismos números (PlanoCarta.cs): un cambio de diseño
// se hace en el contrato y después en los dos lados.

import type { PosicionJugador, RarezaCarta } from '../../compartido/api/tiposCarta';
import { contraste } from '../../compartido/tema/contraste';
import { mezclar } from '../../compartido/tema/identidadVisual';

export const LIENZO = { ancho: 750, alto: 1050, radio: 48, marco: 18 } as const;

/** Una caja del plano, en unidades del lienzo. */
export interface Caja {
  x: number;
  y: number;
  ancho: number;
  alto: number;
}

export const CAJAS = {
  valoracion: { x: 60, y: 70, ancho: 160, alto: 160 },
  posicion: { x: 60, y: 235, ancho: 160, alto: 50 },
  escudo: { x: 70, y: 300, ancho: 130, alto: 130 },
  foto: { x: 260, y: 60, ancho: 430, alto: 460 },
  dorsal: { x: 590, y: 440, ancho: 100, alto: 80 },
  nombre: { x: 60, y: 540, ancho: 630, alto: 100 },
  linea: { x: 120, y: 660, ancho: 510, alto: 3 },
  rareza: { x: 0, y: 960, ancho: 750, alto: 50 },
} as const satisfies Record<string, Caja>;

/** Dos columnas de tres estadísticas: RIT, TIR, PAS a la izquierda; REG, DEF, FÍS a la derecha. */
export const ESTADISTICAS = {
  columnas: [
    { x: 110, ancho: 250 },
    { x: 400, ancho: 250 },
  ],
  filas: [700, 790, 880],
  altoFila: 80,
  siglas: [
    ['RIT', 'TIR', 'PAS'],
    ['REG', 'DEF', 'FÍS'],
  ],
} as const;

/** Tamaños de letra (Barlow Condensed) en unidades del lienzo. */
export const LETRAS = {
  valoracion: 120,
  posicion: 44,
  dorsal: 72,
  nombreMaximo: 64,
  nombreMinimo: 36,
  valorEstadistica: 60,
  siglaEstadistica: 44,
  rareza: 36,
} as const;

export const COLORES_DE_RAREZA: Record<RarezaCarta, string> = {
  BRONCE: '#B0793F',
  PLATA: '#BFC5CC',
  ORO: '#D9A62E',
};

export const TEXTO_DE_RAREZA: Record<RarezaCarta, string> = { BRONCE: 'BRONCE', PLATA: 'PLATA', ORO: 'ORO' };

export const ABREVIATURAS: Record<PosicionJugador, string> = {
  PORTERO: 'POR',
  DEFENSA: 'DEF',
  MEDIOCAMPISTA: 'MED',
  DELANTERO: 'DEL',
};

/** Los dos colores posibles del texto de la carta. */
export const TEXTO_CLARO_CARTA = '#FFFFFF';
export const TEXTO_OSCURO_CARTA = '#141414';

/** Neutros del tema claro (variables.css), para un club sin colores. */
const NEUTRO_PRINCIPAL = '#231F1E';
const NEUTRO_ACENTO = '#5E5650';

/** Por debajo de este contraste con un extremo del degradado, el texto lleva sombra. */
export const CONTRASTE_SIN_SOMBRA = 4.5;

/** Halo del texto cuando hace falta: contorno, desenfoque y opacidad, en unidades del lienzo. */
export const SOMBRA = { contorno: 2, desenfoque: 6, opacidad: 0.85 } as const;

/** Óvalo suave del escudo y fundido de la foto, en proporciones de su caja. */
export const FUNDIDOS = { escudoOpaco: 0.86, fotoOpacaHasta: 0.62, fotoLados: 0.08 } as const;

/** Reflejo: dos bandas diagonales (ángulo CSS; inicio, máximo, fin y opacidad de cada una) y un halo arriba a la izquierda. */
export const BRILLO = {
  angulo: 115,
  bandas: [
    { inicio: 0.2, maximo: 0.38, fin: 0.55, opacidad: 0.35 },
    { inicio: 0.6, maximo: 0.68, fin: 0.76, opacidad: 0.2 },
  ],
  halo: { x: 225, y: 0, radio: 630, opacidad: 0.35 },
} as const;

/**
 * Destellos de luz (efecto premium): estrellas de cuatro puntas blancas con un resplandor suave,
 * en sitios que no tapan los datos. Centro y tamaño (medio largo de una punta) en unidades del lienzo.
 */
export const DESTELLOS = [
  { x: 120, y: 44, tamano: 22 },
  { x: 375, y: 10, tamano: 16 },
  { x: 712, y: 92, tamano: 30 },
  { x: 40, y: 512, tamano: 18 },
  { x: 726, y: 700, tamano: 20 },
  { x: 86, y: 1004, tamano: 26 },
  { x: 664, y: 1040, tamano: 16 },
] as const;

/** Proporciones de un destello: cintura de la estrella, radio y opacidad del resplandor. */
export const FORMA_DESTELLO = { cintura: 0.16, resplandor: 1.1, opacidadResplandor: 0.55 } as const;

/** La estrella de cuatro puntas de un destello como trazado SVG (el mismo que dibuja la API). */
export function trazadoDestello(x: number, y: number, tamano: number): string {
  const c = tamano * FORMA_DESTELLO.cintura;
  return `M${x} ${y - tamano}L${x + c} ${y - c}L${x + tamano} ${y}L${x + c} ${y + c}L${x} ${y + tamano}L${x - c} ${y + c}L${x - tamano} ${y}L${x - c} ${y - c}Z`;
}

/** Los tres tonos del marco metálico de una rareza, de arriba abajo. */
export function tonosDelMarco(rareza: RarezaCarta): [string, string, string] {
  const color = COLORES_DE_RAREZA[rareza];
  return [mezclar(color, '#FFFFFF', 0.45), color, mezclar(color, '#000000', 0.6)];
}

const FORMATO = /^#[0-9a-f]{6}$/i;

/**
 * Los dos extremos del degradado, de arriba a la izquierda a abajo a la derecha: principal y
 * acento; sin acento, el principal y su versión 35 % más oscura; sin colores, los neutros.
 */
export function degradado(principal: string | null | undefined, acento: string | null | undefined): [string, string] {
  if (!principal || !FORMATO.test(principal)) {
    return [NEUTRO_PRINCIPAL, NEUTRO_ACENTO];
  }

  const inicio = principal.toUpperCase();
  return [inicio, acento && FORMATO.test(acento) ? acento.toUpperCase() : mezclar(inicio, '#000000', 0.65)];
}

/** El color del texto de la carta y, si hace falta, el de su sombra. */
export interface ColorDeTexto {
  texto: string;
  /** Color de la sombra (el contrario del texto); nulo si no hace falta. */
  sombra: string | null;
}

/**
 * Blanco o casi negro, el de más contraste con el color medio del degradado; con sombra del color
 * contrario si su contraste con alguno de los dos extremos baja de 4,5:1 (caso límite 11).
 */
export function colorDelTexto([desde, hasta]: [string, string]): ColorDeTexto {
  const medio = mezclar(desde, hasta, 0.5);
  const claro = contraste(medio, TEXTO_CLARO_CARTA) >= contraste(medio, TEXTO_OSCURO_CARTA);
  const texto = claro ? TEXTO_CLARO_CARTA : TEXTO_OSCURO_CARTA;
  const peor = Math.min(contraste(texto, desde), contraste(texto, hasta));
  return { texto, sombra: peor < CONTRASTE_SIN_SOMBRA ? (claro ? TEXTO_OSCURO_CARTA : TEXTO_CLARO_CARTA) : null };
}

/** Mide el ancho de un texto en unidades del lienzo con un tamaño de letra dado. */
export type Medidor = (texto: string, tamano: number) => number;

/**
 * Ancho aproximado en Barlow Condensed 700 en mayúsculas (media de 0,5 em por carácter). Lo usa la
 * pantalla si no puede medir con un lienzo; la API mide con la fuente de verdad.
 */
export const medirAproximado: Medidor = (texto, tamano) => texto.length * tamano * 0.5;

/** El nombre ya ajustado: una o dos líneas y su tamaño de letra. */
export interface NombreAjustado {
  lineas: string[];
  tamano: number;
}

/**
 * "Nombres Apellidos" en mayúsculas: empieza en 64 y baja de 2 en 2 hasta 36 para caber en una
 * línea; si aún no cabe, dos líneas a 36, y lo que sobre termina en "…".
 */
export function ajustarNombre(nombres: string, apellidos: string, medir: Medidor = medirAproximado): NombreAjustado {
  const texto = `${nombres} ${apellidos}`.trim().replace(/\s+/g, ' ').toUpperCase();
  const ancho = CAJAS.nombre.ancho;
  for (let tamano: number = LETRAS.nombreMaximo; tamano >= LETRAS.nombreMinimo; tamano -= 2) {
    if (medir(texto, tamano) <= ancho) {
      return { lineas: [texto], tamano };
    }
  }

  const tamano = LETRAS.nombreMinimo;
  const palabras = texto.split(' ');
  let primera = '';
  while (palabras.length > 0 && medir(primera ? `${primera} ${palabras[0]}` : palabras[0], tamano) <= ancho) {
    primera = primera ? `${primera} ${palabras.shift()}` : (palabras.shift() ?? '');
  }

  let segunda = palabras.join(' ');
  if (!primera) {
    // Una sola palabra más ancha que la caja: se corta como la segunda línea.
    primera = recortar(segunda, tamano, medir);
    return { lineas: [primera], tamano };
  }

  if (segunda && medir(segunda, tamano) > ancho) {
    segunda = recortar(segunda, tamano, medir);
  }

  return { lineas: segunda ? [primera, segunda] : [primera], tamano };
}

function recortar(texto: string, tamano: number, medir: Medidor): string {
  let corto = texto;
  while (corto.length > 1 && medir(`${corto}…`, tamano) > CAJAS.nombre.ancho) {
    corto = corto.slice(0, -1).trimEnd();
  }

  return `${corto}…`;
}
