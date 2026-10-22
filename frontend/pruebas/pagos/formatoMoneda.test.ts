import { describe, expect, it } from 'vitest';
import { formatoMoneda, leerPesos } from '../../src/privado/pagos/formatoMoneda';
import { descripcionDelCargo, nombreDelMes, puedeVerPagos } from '../../src/privado/pagos/textosPagos';

describe('formato de moneda colombiana', () => {
  it('escribe los pesos con puntos de miles y sin decimales', () => {
    expect(formatoMoneda(65000)).toBe('$ 65.000');
    expect(formatoMoneda(0)).toBe('$ 0');
    expect(formatoMoneda(1250000)).toBe('$ 1.250.000');
  });

  it('lee solo enteros de pesos mayores que cero', () => {
    expect(leerPesos('65000')).toBe(65000);
    expect(leerPesos('$ 65.000')).toBe(65000);
    expect(leerPesos('0')).toBeNull();
    expect(leerPesos('')).toBeNull();
    expect(leerPesos('65,5')).toBeNull();
    expect(leerPesos('abc')).toBeNull();
  });
});

describe('textos de pagos', () => {
  it('nombra el mes de cobro', () => {
    expect(nombreDelMes('2026-10-01')).toBe('octubre de 2026');
    expect(descripcionDelCargo('MENSUALIDAD', '2026-01-01')).toBe('Mensualidad enero de 2026');
  });

  it('el entrenador no ve la entrada de pagos', () => {
    expect(puedeVerPagos('PRESIDENTE')).toBe(true);
    expect(puedeVerPagos('DIRECTIVO')).toBe(true);
    expect(puedeVerPagos('JUGADOR')).toBe(true);
    expect(puedeVerPagos('ENTRENADOR')).toBe(false);
  });
});
