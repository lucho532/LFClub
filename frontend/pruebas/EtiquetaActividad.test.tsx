import { act } from 'react';
import { createRoot, type Root } from 'react-dom/client';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { EtiquetaActividad } from '../src/privado/actividades/EtiquetaActividad';

(globalThis as { IS_REACT_ACT_ENVIRONMENT?: boolean }).IS_REACT_ACT_ENVIRONMENT = true;

describe('EtiquetaActividad', () => {
  let contenedor: HTMLDivElement;
  let raiz: Root;

  beforeEach(() => {
    contenedor = document.createElement('div');
    document.body.appendChild(contenedor);
    raiz = createRoot(contenedor);
  });

  afterEach(() => {
    act(() => raiz.unmount());
    contenedor.remove();
  });

  it.each([
    ['PROGRAMADO', 'Entreno', 'Programado'],
    ['CANCELADO', 'Entreno', 'Cancelado'],
  ] as const)('un entrenamiento %s se lee como texto', (estado, tipo, texto) => {
    act(() => raiz.render(<EtiquetaActividad tipo="ENTRENAMIENTO" estado={estado} />));

    expect(contenedor.textContent).toBe(`${tipo}${texto}`);
  });

  it.each([
    ['PROXIMO', 'Próximo'],
    ['CANCELADO', 'Cancelado'],
    ['JUGADO', 'Jugado'],
  ] as const)('un partido %s se lee como texto', (estado, texto) => {
    act(() => raiz.render(<EtiquetaActividad tipo="PARTIDO" estado={estado} />));

    expect(contenedor.textContent).toBe(`Partido${texto}`);
  });

  it('un partido próximo que ya empezó dice que falta el resultado', () => {
    act(() => raiz.render(<EtiquetaActividad tipo="PARTIDO" estado="PROXIMO" faltaResultado />));

    expect(contenedor.textContent).toBe('PartidoFalta el resultado');
  });

  it('un partido jugado no dice que falta el resultado aunque la marca llegue', () => {
    act(() => raiz.render(<EtiquetaActividad tipo="PARTIDO" estado="JUGADO" faltaResultado />));

    expect(contenedor.textContent).toBe('PartidoJugado');
  });
});
