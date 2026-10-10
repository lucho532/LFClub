import type { IdentidadClubDto } from '../api/tipos';
import { coloresClub } from './coloresClub';
import { CONTRASTE_MINIMO_DE_RELLENO, contraste, esColor, textoSobre } from './contraste';

/** Contraste mínimo de WCAG para texto normal, como el de las pestañas inactivas del menú. */
const CONTRASTE_MINIMO_DE_TEXTO = 4.5;

/** Cuánto del color principal tiñe el fondo de la pantalla: apenas se nota y no resta lectura. */
const TINTE_DEL_FONDO = '7%';

/**
 * Mezcla dos colores #RRGGBB: con `peso` 1 queda el primero y con 0 el segundo.
 */
export function mezclar(uno: string, otro: string, peso: number): string {
  const canal = (color: string, inicio: number) => parseInt(color.slice(inicio, inicio + 2), 16);
  const hex = [1, 3, 5]
    .map((inicio) => Math.round(canal(uno, inicio) * peso + canal(otro, inicio) * (1 - peso)))
    .map((valor) => valor.toString(16).padStart(2, '0'))
    .join('');
  return `#${hex.toUpperCase()}`;
}

/**
 * Todas las variables CSS con las que un club pinta la interfaz: las de `coloresClub` y, si tiene
 * color principal, el menú lateral (y la barra de pestañas del teléfono) con ese color, el ítem
 * activo del menú con el acento y el fondo de la pantalla apenas teñido.
 *
 * El texto sobre cada relleno se elige por contraste. Si el acento no se distingue del principal
 * (menos de 3:1), el ítem activo usa el color del texto del menú, para que siempre se vea cuál es.
 * Sin color principal no devuelve nada del menú ni del fondo: se queda el menú neutro de
 * variables.css, que funciona en los dos temas.
 */
export function variablesDeIdentidad(identidad: IdentidadClubDto | null | undefined): Record<string, string> {
  const variables: Record<string, string> = { ...coloresClub(identidad) };
  const principal = identidad?.colorPrincipal;
  if (!esColor(principal)) {
    return variables;
  }

  const texto = textoSobre(principal);
  const suave = mezclar(texto, principal, 0.8);
  variables['--lateral-fondo'] = principal;
  variables['--lateral-texto'] = texto;
  variables['--lateral-texto-suave'] = contraste(suave, principal) >= CONTRASTE_MINIMO_DE_TEXTO ? suave : texto;
  variables['--lateral-suave'] = mezclar(texto, principal, 0.14);

  const acento = identidad?.colorAcento;
  if (esColor(acento) && contraste(acento, principal) >= CONTRASTE_MINIMO_DE_RELLENO) {
    variables['--lateral-activo-fondo'] = acento;
    variables['--lateral-activo-texto'] = textoSobre(acento);
  } else {
    variables['--lateral-activo-fondo'] = texto;
    variables['--lateral-activo-texto'] = principal;
  }

  // `var(--fondo)` se resuelve en el contenedor con el tema vigente, así que el tinte sigue al tema.
  variables['--fondo-club'] = `color-mix(in srgb, ${principal} ${TINTE_DEL_FONDO}, var(--fondo))`;
  return variables;
}
