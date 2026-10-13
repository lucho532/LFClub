import { act } from 'react';
import { createRoot, type Root } from 'react-dom/client';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { SelectorEquipos } from '../src/privado/actividades/SelectorEquipos';

(globalThis as { IS_REACT_ACT_ENVIRONMENT?: boolean }).IS_REACT_ACT_ENVIRONMENT = true;

const C2014 = {
  categoriaId: 'c14',
  anio: 2014,
  equipos: [
    { equipoId: 'a', nombre: 'A' },
    { equipoId: 'b', nombre: 'B' },
  ],
};
const C2015 = { categoriaId: 'c15', anio: 2015, equipos: [{ equipoId: 'x', nombre: 'Élite' }] };

describe('SelectorEquipos', () => {
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

  it('no aparece si ninguna categoría elegida tiene equipos', () => {
    act(() =>
      raiz.render(
        <SelectorEquipos
          categorias={[{ categoriaId: 'c16', anio: 2016, equipos: [] }]}
          elegidos={[]}
          alCambiar={() => undefined}
        />,
      ),
    );

    expect(contenedor.textContent).toBe('');
  });

  it('sin ninguno marcado dice "Toda la categoría"', () => {
    act(() => raiz.render(<SelectorEquipos categorias={[C2014]} elegidos={[]} alCambiar={() => undefined} />));

    expect(contenedor.textContent).toContain('Toda la categoría');
    expect(contenedor.textContent).toContain('Equipo A');
    expect(contenedor.textContent).toContain('Equipo B');
  });

  it('marcar y desmarcar un equipo avisa con la lista completa', () => {
    const alCambiar = vi.fn();
    act(() => raiz.render(<SelectorEquipos categorias={[C2014]} elegidos={['a']} alCambiar={alCambiar} />));
    const casillas = contenedor.querySelectorAll<HTMLInputElement>('input[type="checkbox"]');

    act(() => casillas[1].click());
    expect(alCambiar).toHaveBeenLastCalledWith(['a', 'b']);

    act(() => casillas[0].click());
    expect(alCambiar).toHaveBeenLastCalledWith([]);
    expect(contenedor.textContent).not.toContain('Toda la categoría');
  });

  it('agrupa los equipos de varias categorías por año', () => {
    act(() => raiz.render(<SelectorEquipos categorias={[C2014, C2015]} elegidos={[]} alCambiar={() => undefined} />));

    const grupos = [...contenedor.querySelectorAll('.grupo-de-equipos')].map((grupo) => grupo.textContent);
    expect(grupos).toEqual(['2014Equipo AEquipo B', '2015Equipo Élite']);
  });

  it('muestra los equipos desactivados que ya tenía, fijos y marcados, escritos', () => {
    act(() =>
      raiz.render(
        <SelectorEquipos
          categorias={[C2014]}
          elegidos={[]}
          alCambiar={() => undefined}
          desactivados={[{ equipoId: 'v', nombre: 'Viejo', anio: 2014 }]}
        />,
      ),
    );

    const fijo = [...contenedor.querySelectorAll<HTMLInputElement>('input')].find((casilla) => casilla.disabled);
    expect(fijo?.checked).toBe(true);
    expect(contenedor.textContent).toContain('Equipo Viejo (desactivado)');
  });
});
