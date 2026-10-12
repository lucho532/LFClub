import { act } from 'react';
import { createRoot, type Root } from 'react-dom/client';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { api } from '../src/compartido/api/cliente';
import type { AvisoRecibidoDto } from '../src/compartido/api/tiposCampana';
import { FilaDeAviso } from '../src/privado/comunicados/FilaDeAviso';

(globalThis as { IS_REACT_ACT_ENVIRONMENT?: boolean }).IS_REACT_ACT_ENVIRONMENT = true;

function aviso(sobrescribir: Partial<AvisoRecibidoDto> = {}): AvisoRecibidoDto {
  return {
    id: 'av1',
    texto: 'Nuevo entrenamiento el sábado a las 9:00',
    generadoEn: '2026-10-12T15:30:00Z',
    actividad: { tipo: 'ENTRENAMIENTO', id: 'ent1', disponible: true },
    leido: false,
    ...sobrescribir,
  };
}

describe('FilaDeAviso', () => {
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

  async function pintar(recibido: AvisoRecibidoDto, alLeer = vi.fn()) {
    await act(async () => {
      raiz.render(
        <MemoryRouter initialEntries={['/club/c/comunicados']}>
          <Routes>
            <Route
              path="/club/c/comunicados"
              element={
                <ul>
                  <FilaDeAviso clubId="c" aviso={recibido} alLeer={alLeer} />
                </ul>
              }
            />
            <Route path="/club/c/entrenamientos/:id" element={<p>Detalle del entrenamiento</p>} />
            <Route path="/club/c/partidos/:id" element={<p>Detalle del partido</p>} />
          </Routes>
        </MemoryRouter>,
      );
    });
    return alLeer;
  }

  const boton = () => contenedor.querySelector('button.aviso-texto') as HTMLButtonElement;

  it('muestra el texto, la palabra Aviso y el estado en texto, sin título ni autor', async () => {
    await pintar(aviso());

    const fila = contenedor.querySelector('li')!;
    expect(boton().textContent).toBe('Nuevo entrenamiento el sábado a las 9:00');
    expect(fila.textContent).toContain('Aviso');
    expect(fila.textContent).toContain('Sin leer');
    expect(fila.className).toContain('sin-leer');
    expect(fila.querySelector('a')).toBeNull();
  });

  it('un aviso leído dice Leído y no va en negrita', async () => {
    await pintar(aviso({ leido: true, leidoEn: '2026-10-12T16:00:00Z' }));

    const fila = contenedor.querySelector('li')!;
    expect(fila.textContent).toContain('Leído');
    expect(fila.className).not.toContain('sin-leer');
  });

  it('al tocarlo lo marca leído y navega al entrenamiento si sigue disponible', async () => {
    const put = vi.spyOn(api, 'put').mockResolvedValue(aviso({ leido: true, leidoEn: '2026-10-12T17:00:00Z' }));
    const alLeer = await pintar(aviso());

    await act(async () => boton().click());

    expect(put).toHaveBeenCalledWith('/api/clubes/c/campana/avisos/av1/lectura', {});
    expect(alLeer).toHaveBeenCalledWith(expect.objectContaining({ leido: true }), true);
    expect(contenedor.textContent).toContain('Detalle del entrenamiento');
  });

  it('un partido disponible abre el detalle del partido', async () => {
    const partido = aviso({ actividad: { tipo: 'PARTIDO', id: 'p1', disponible: true } });
    vi.spyOn(api, 'put').mockResolvedValue({ ...partido, leido: true });
    await pintar(partido);

    await act(async () => boton().click());

    expect(contenedor.textContent).toContain('Detalle del partido');
  });

  it('si la actividad ya no está disponible lo deja leído, muestra el mensaje y no navega', async () => {
    const noDisponible = aviso({
      leido: true,
      leidoEn: '2026-10-12T17:00:00Z',
      actividad: { tipo: 'PARTIDO', id: 'p1', disponible: false },
    });
    vi.spyOn(api, 'put').mockResolvedValue(noDisponible);
    const alLeer = await pintar(aviso({ actividad: { tipo: 'PARTIDO', id: 'p1', disponible: true } }));

    await act(async () => boton().click());

    expect(contenedor.textContent).not.toContain('Detalle del partido');
    const mensaje = contenedor.querySelector('[role="status"]');
    expect(mensaje?.textContent).toBe('Esa actividad ya no está disponible para ti.');
    expect(alLeer).toHaveBeenCalledWith(expect.objectContaining({ leido: true }), true);
    expect(contenedor.querySelector('[role="dialog"]')).toBeNull();
  });

  it('no interpreta HTML ni enlaces del texto', async () => {
    await pintar(aviso({ texto: '<b>negrita</b> <a href="https://malo.test">enlace</a> <img src=x onerror=alert(1)>' }));

    expect(boton().textContent).toContain('<b>negrita</b>');
    expect(contenedor.querySelector('b')).toBeNull();
    expect(contenedor.querySelector('a')).toBeNull();
    expect(contenedor.querySelector('img')).toBeNull();
  });

  it('si la lectura falla muestra el error y no navega', async () => {
    vi.spyOn(api, 'put').mockRejectedValue(new Error('sin conexión'));
    const alLeer = await pintar(aviso());

    await act(async () => boton().click());

    expect(alLeer).not.toHaveBeenCalled();
    expect(contenedor.textContent).not.toContain('Detalle del entrenamiento');
    expect(contenedor.querySelector('[role="alert"]')?.textContent).toBe(
      'Ocurrió un error inesperado. Inténtalo de nuevo.',
    );
  });
});
