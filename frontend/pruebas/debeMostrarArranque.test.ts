import { describe, expect, it } from 'vitest';
import { CLAVE_ARRANQUE, debeMostrarArranque } from '../src/arranque/debeMostrarArranque';

function almacen(): Pick<Storage, 'getItem' | 'setItem'> & { datos: Map<string, string> } {
  const datos = new Map<string, string>();
  return { datos, getItem: (clave) => datos.get(clave) ?? null, setItem: (clave, valor) => void datos.set(clave, valor) };
}

describe('debeMostrarArranque', () => {
  it('no se muestra en el navegador', () => {
    const memoria = almacen();
    expect(debeMostrarArranque(false, memoria)).toBe(false);
    expect(memoria.datos.size).toBe(0);
  });

  it('en la aplicación se muestra una sola vez por apertura', () => {
    const memoria = almacen();
    expect(debeMostrarArranque(true, memoria)).toBe(true);
    expect(memoria.datos.get(CLAVE_ARRANQUE)).toBe('1');
    expect(debeMostrarArranque(true, memoria)).toBe(false);
  });

  it('sin almacenamiento se muestra igual', () => {
    expect(debeMostrarArranque(true, null)).toBe(true);
    const roto = { getItem: () => { throw new Error('bloqueado'); }, setItem: () => undefined };
    expect(debeMostrarArranque(true, roto)).toBe(true);
  });
});
