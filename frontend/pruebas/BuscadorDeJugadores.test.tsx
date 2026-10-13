import { act } from 'react';
import { createRoot, type Root } from 'react-dom/client';
import { MemoryRouter } from 'react-router-dom';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { api } from '../src/compartido/api/cliente';
import type { JugadorSumableDto } from '../src/compartido/api/tiposAudiencia';
import { BuscadorDeJugadores } from '../src/privado/actividades/BuscadorDeJugadores';

(globalThis as { IS_REACT_ACT_ENVIRONMENT?: boolean }).IS_REACT_ACT_ENVIRONMENT = true;

const KAPELLIDO: JugadorSumableDto = {
  usuarioRolId: 'k1',
  nombres: 'Kevin',
  apellidos: 'Kapellido',
  categoria: { categoriaId: 'c15', anio: 2015 },
};
const MAPELLIDO: JugadorSumableDto = {
  usuarioRolId: 'm1',
  nombres: 'Mateo',
  apellidos: 'Mapellido',
  categoria: { categoriaId: 'c16', anio: 2016 },
};

describe('BuscadorDeJugadores', () => {
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
    vi.restoreAllMocks();
  });

  function montar(alElegir: (jugador: JugadorSumableDto) => void = () => undefined, excluidos: string[] = []) {
    act(() =>
      raiz.render(
        <MemoryRouter>
          <BuscadorDeJugadores clubId="club" excluidos={excluidos} alElegir={alElegir} />
        </MemoryRouter>,
      ),
    );
  }

  const campo = () => contenedor.querySelector('input') as HTMLInputElement;

  async function escribir(valor: string) {
    await act(async () => {
      Object.getOwnPropertyDescriptor(HTMLInputElement.prototype, 'value')!.set!.call(campo(), valor);
      campo().dispatchEvent(new Event('input', { bubbles: true }));
    });
    // Pasa la espera de la búsqueda y deja resolver la respuesta.
    await act(async () => {
      await new Promise((resolver) => setTimeout(resolver, 320));
    });
  }

  it('con menos de dos letras no busca y dice qué hacer', async () => {
    const buscar = vi.spyOn(api, 'get').mockResolvedValue([]);
    montar();

    await escribir('K');

    expect(buscar).not.toHaveBeenCalled();
    expect(contenedor.textContent).toContain('Escribe al menos 2 letras del nombre o apellido');
  });

  it('muestra los resultados con nombre y categoría, sin enlace a la ficha', async () => {
    const buscar = vi.spyOn(api, 'get').mockResolvedValue([KAPELLIDO, MAPELLIDO]);
    montar();

    await escribir('Ka');

    expect(buscar).toHaveBeenCalledWith('/api/clubes/club/actividades/jugadores-sumables?busqueda=Ka');
    const botones = [...contenedor.querySelectorAll('li button')].map((boton) => boton.textContent);
    expect(botones).toEqual(['Kevin Kapellido · Categoría 2015', 'Mateo Mapellido · Categoría 2016']);
    expect(contenedor.querySelector('a')).toBeNull();
  });

  it('se elige con un clic o con Enter, que elige el primero', async () => {
    vi.spyOn(api, 'get').mockResolvedValue([KAPELLIDO, MAPELLIDO]);
    const alElegir = vi.fn();
    montar(alElegir);

    await escribir('ap');
    act(() => {
      campo().dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', bubbles: true, cancelable: true }));
    });

    expect(alElegir).toHaveBeenCalledWith(KAPELLIDO);
    expect(campo().value).toBe('');
  });

  it('no ofrece a los excluidos y, sin coincidencias, lo dice con palabras', async () => {
    vi.spyOn(api, 'get').mockResolvedValue([KAPELLIDO]);
    montar(() => undefined, ['k1']);

    await escribir('Ka');

    expect(contenedor.querySelectorAll('li button')).toHaveLength(0);
    expect(contenedor.textContent).toContain('No encontramos jugadores con ese nombre');
  });
});
