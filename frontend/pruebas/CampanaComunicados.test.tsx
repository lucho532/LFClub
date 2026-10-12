import { act } from 'react';
import { createRoot, type Root } from 'react-dom/client';
import { MemoryRouter } from 'react-router-dom';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { api } from '../src/compartido/api/cliente';
import { CampanaComunicados, nombreAccesibleDeCampana } from '../src/compartido/componentes/CampanaComunicados';
import {
  INTERVALO_DEL_CONTADOR_MS,
  useContadorComunicados,
} from '../src/privado/comunicados/useContadorComunicados';

(globalThis as { IS_REACT_ACT_ENVIRONMENT?: boolean }).IS_REACT_ACT_ENVIRONMENT = true;

describe('CampanaComunicados', () => {
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
    vi.useRealTimers();
  });

  function pintar(sinLeer: number) {
    act(() =>
      raiz.render(
        <MemoryRouter>
          <CampanaComunicados sinLeer={sinLeer} a="/club/c/comunicados" />
        </MemoryRouter>,
      ),
    );
  }

  const insignia = () => contenedor.querySelector('.campana-insignia');
  const enlace = () => contenedor.querySelector('a.campana') as HTMLAnchorElement;

  it('con 0 no hay insignia y el nombre dice que no hay novedades', () => {
    pintar(0);

    expect(insignia()).toBeNull();
    expect(enlace().getAttribute('aria-label')).toBe('Novedades, nada por leer');
    expect(enlace().getAttribute('href')).toBe('/club/c/comunicados');
  });

  it('con 5 la insignia dice 5 y el nombre dice cuántos sin leer', () => {
    pintar(5);

    expect(insignia()?.textContent).toBe('5');
    expect(enlace().getAttribute('aria-label')).toBe('Novedades, 5 sin leer');
  });

  it('con 99 dice 99 y desde 100 dice 99+', () => {
    pintar(99);
    expect(insignia()?.textContent).toBe('99');

    pintar(100);
    expect(insignia()?.textContent).toBe('99+');
    expect(enlace().getAttribute('aria-label')).toBe('Novedades, más de 99 sin leer');
    expect(nombreAccesibleDeCampana(1000)).toBe('Novedades, más de 99 sin leer');
  });

  it('el nombre accesible es "Novedades" con 0, 3 y 100', () => {
    expect(nombreAccesibleDeCampana(0)).toBe('Novedades, nada por leer');
    expect(nombreAccesibleDeCampana(3)).toBe('Novedades, 3 sin leer');
    expect(nombreAccesibleDeCampana(100)).toBe('Novedades, más de 99 sin leer');
  });

  it('anuncia el cambio con una región de estado educada y no usa texto visible en el botón', () => {
    pintar(3);

    const region = contenedor.querySelector('[aria-live="polite"]');
    expect(region?.textContent).toBe('Novedades, 3 sin leer');
    expect(enlace().textContent).toBe('3');
  });
});

describe('useContadorComunicados', () => {
  let contenedor: HTMLDivElement;
  let raiz: Root;

  beforeEach(() => {
    vi.useFakeTimers();
    contenedor = document.createElement('div');
    document.body.appendChild(contenedor);
    raiz = createRoot(contenedor);
  });

  afterEach(() => {
    act(() => raiz.unmount());
    contenedor.remove();
    vi.restoreAllMocks();
    vi.useRealTimers();
  });

  function Sonda({ activo }: { activo: boolean }) {
    const { sinLeer } = useContadorComunicados('c', activo);
    return <output>{sinLeer}</output>;
  }

  async function montar(activo: boolean) {
    await act(async () => {
      raiz.render(
        <MemoryRouter>
          <Sonda activo={activo} />
        </MemoryRouter>,
      );
    });
  }

  const numero = () => contenedor.querySelector('output')?.textContent;

  it('consulta al montar y otra vez cada 60 segundos', async () => {
    const get = vi.spyOn(api, 'get').mockResolvedValue({ sinLeer: 2 });
    await montar(true);

    expect(get).toHaveBeenCalledWith('/api/clubes/c/campana/contador');
    expect(numero()).toBe('2');

    get.mockResolvedValue({ sinLeer: 4 });
    await act(async () => {
      await vi.advanceTimersByTimeAsync(INTERVALO_DEL_CONTADOR_MS);
    });

    expect(get).toHaveBeenCalledTimes(2);
    expect(numero()).toBe('4');
  });

  it('conserva el último número si una consulta falla', async () => {
    const get = vi.spyOn(api, 'get').mockResolvedValue({ sinLeer: 7 });
    await montar(true);
    expect(numero()).toBe('7');

    get.mockRejectedValue(new Error('sin conexión'));
    await act(async () => {
      await vi.advanceTimersByTimeAsync(INTERVALO_DEL_CONTADOR_MS);
    });

    expect(get).toHaveBeenCalledTimes(2);
    expect(numero()).toBe('7');
  });

  it('vuelve a consultar cuando la aplicación vuelve a primer plano', async () => {
    const get = vi.spyOn(api, 'get').mockResolvedValue({ sinLeer: 1 });
    await montar(true);

    await act(async () => {
      document.dispatchEvent(new Event('visibilitychange'));
    });

    expect(get).toHaveBeenCalledTimes(2);
  });

  it('no consulta nada para quien no es la cuenta de un jugador', async () => {
    const get = vi.spyOn(api, 'get').mockResolvedValue({ sinLeer: 9 });
    await montar(false);

    await act(async () => {
      await vi.advanceTimersByTimeAsync(INTERVALO_DEL_CONTADOR_MS * 2);
    });

    expect(get).not.toHaveBeenCalled();
    expect(numero()).toBe('0');
  });
});
