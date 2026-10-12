import { describe, expect, it } from 'vitest';
import { contraste } from '../src/compartido/tema/contraste';
import {
  ABREVIATURAS,
  ajustarNombre,
  CAJAS,
  colorDelTexto,
  degradado,
  LETRAS,
  medirAproximado,
  TEXTO_CLARO_CARTA,
  TEXTO_OSCURO_CARTA,
} from '../src/privado/carta/plano';

// Los mismos casos y resultados que ContrasteDeColorPruebas.cs: pantalla e imagen eligen igual.
describe('plano de la carta', () => {
  it('con colores oscuros el texto es blanco y sin sombra', () => {
    expect(colorDelTexto(['#1B3A6B', '#0F2240'])).toEqual({ texto: TEXTO_CLARO_CARTA, sombra: null });
  });

  it('con colores claros el texto es casi negro y sin sombra', () => {
    expect(colorDelTexto(['#F5F5F5', '#E0E0E0'])).toEqual({ texto: TEXTO_OSCURO_CARTA, sombra: null });
  });

  it('con dos amarillos claros el texto oscuro se lee con los dos', () => {
    const color = colorDelTexto(['#FFF176', '#FFE082']);

    expect(color.texto).toBe(TEXTO_OSCURO_CARTA);
    expect(contraste(color.texto, '#FFF176')).toBeGreaterThanOrEqual(4.5);
    expect(contraste(color.texto, '#FFE082')).toBeGreaterThanOrEqual(4.5);
  });

  it('con un amarillo y un mostaza oscuro el texto lleva sombra del color contrario', () => {
    expect(colorDelTexto(['#FFE600', '#6B5B00'])).toEqual({ texto: TEXTO_OSCURO_CARTA, sombra: TEXTO_CLARO_CARTA });
  });

  it('sin acento el degradado va del principal a su versión 35 % más oscura', () => {
    expect(degradado('#1b3a6b', null)).toEqual(['#1B3A6B', '#122646']);
  });

  it('sin colores usa los neutros del tema claro', () => {
    expect(degradado(null, '#FFFFFF')).toEqual(['#231F1E', '#5E5650']);
  });

  it('abrevia las cuatro posiciones', () => {
    expect(Object.values(ABREVIATURAS)).toEqual(['POR', 'DEF', 'MED', 'DEL']);
  });

  it('un nombre corto va en una línea a 64', () => {
    expect(ajustarNombre('Ana', 'Pérez')).toEqual({ lineas: ['ANA PÉREZ'], tamano: LETRAS.nombreMaximo });
  });

  it('un nombre largo baja el tamaño antes de partirse', () => {
    const ajustado = ajustarNombre('María José', 'Rodríguez Castañeda');

    expect(ajustado.lineas).toHaveLength(1);
    expect(ajustado.tamano).toBeLessThan(LETRAS.nombreMaximo);
    expect(ajustado.tamano).toBeGreaterThanOrEqual(LETRAS.nombreMinimo);
  });

  it('un nombre de 60 caracteres no pasa de dos líneas ni del ancho', () => {
    const nombres = 'Juan Sebastián Alejandro';
    const apellidos = 'Montoya de los Ríos Villanueva Echever';
    expect(`${nombres} ${apellidos}`).toHaveLength(63);

    const ajustado = ajustarNombre(nombres, apellidos);

    expect(ajustado.lineas.length).toBeLessThanOrEqual(2);
    expect(ajustado.tamano).toBe(LETRAS.nombreMinimo);
    for (const linea of ajustado.lineas) {
      expect(medirAproximado(linea, ajustado.tamano)).toBeLessThanOrEqual(CAJAS.nombre.ancho);
    }
  });

  it('lo que no cabe en dos líneas termina en puntos suspensivos', () => {
    const ajustado = ajustarNombre('A'.repeat(30), `${'B'.repeat(30)} ${'C'.repeat(30)}`);

    expect(ajustado.lineas).toHaveLength(2);
    expect(ajustado.lineas[1].endsWith('…')).toBe(true);
  });
});
