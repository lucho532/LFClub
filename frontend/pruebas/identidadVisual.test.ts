import { describe, expect, it } from 'vitest';
import { contraste, TEXTO_CLARO, TEXTO_OSCURO } from '../src/compartido/tema/contraste';
import { mezclar, variablesDeIdentidad } from '../src/compartido/tema/identidadVisual';

describe('identidad visual del club', () => {
  it('pinta el menú con el color principal y el ítem activo con el acento', () => {
    const variables = variablesDeIdentidad({ colorPrincipal: '#6E2E24', colorAcento: '#FFC72C', urlEscudo: null });

    expect(variables['--lateral-fondo']).toBe('#6E2E24');
    expect(variables['--lateral-texto']).toBe(TEXTO_CLARO);
    expect(variables['--lateral-activo-fondo']).toBe('#FFC72C');
    expect(variables['--lateral-activo-texto']).toBe(TEXTO_OSCURO);
    expect(variables['--fondo-club']).toBe('color-mix(in srgb, #6E2E24 7%, var(--fondo))');
    // Conserva las variables de siempre.
    expect(variables['--color-club-principal']).toBe('#6E2E24');
  });

  it('si el acento no se distingue del menú, el ítem activo usa el texto del menú', () => {
    const variables = variablesDeIdentidad({ colorPrincipal: '#6E2E24', colorAcento: '#7A3328', urlEscudo: null });

    expect(variables['--lateral-activo-fondo']).toBe(TEXTO_CLARO);
    expect(variables['--lateral-activo-texto']).toBe('#6E2E24');
  });

  it('sin acento, el ítem activo también usa el texto del menú', () => {
    const variables = variablesDeIdentidad({ colorPrincipal: '#FFC72C', colorAcento: null, urlEscudo: null });

    expect(variables['--lateral-texto']).toBe(TEXTO_OSCURO);
    expect(variables['--lateral-activo-fondo']).toBe(TEXTO_OSCURO);
    expect(variables['--lateral-activo-texto']).toBe('#FFC72C');
  });

  it('el texto suave del menú se lee sobre cualquier color principal', () => {
    for (const principal of ['#B8370F', '#6E2E24', '#FFC72C', '#1D4E89', '#808080', '#262626', '#F2F2F2']) {
      const variables = variablesDeIdentidad({ colorPrincipal: principal, colorAcento: null, urlEscudo: null });
      expect(contraste(variables['--lateral-texto-suave'], principal)).toBeGreaterThanOrEqual(4.5);
    }
  });

  it('sin color principal deja el menú y el fondo neutros', () => {
    expect(variablesDeIdentidad({ colorPrincipal: null, colorAcento: '#FFC72C', urlEscudo: null })).toEqual({
      '--color-club-acento': '#FFC72C',
      '--color-club-acento-texto': TEXTO_OSCURO,
    });
    expect(variablesDeIdentidad(null)).toEqual({});
  });

  it('mezcla dos colores por canal', () => {
    expect(mezclar('#FFFFFF', '#000000', 1)).toBe('#FFFFFF');
    expect(mezclar('#FFFFFF', '#000000', 0)).toBe('#000000');
    expect(mezclar('#FFFFFF', '#000000', 0.5)).toBe('#808080');
  });
});
