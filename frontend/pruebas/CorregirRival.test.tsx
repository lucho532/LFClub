import { act } from 'react';
import { createRoot, type Root } from 'react-dom/client';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { api } from '../src/compartido/api/cliente';
import type { PartidoDto } from '../src/compartido/api/tiposActividades';
import { CorregirRival } from '../src/privado/partidos/CorregirRival';

(globalThis as { IS_REACT_ACT_ENVIRONMENT?: boolean }).IS_REACT_ACT_ENVIRONMENT = true;

const jugado: PartidoDto = {
  partidoId: 'x',
  rival: 'Equipo C',
  fecha: '2026-10-10',
  hora: '09:00',
  lugar: 'Cancha de Minitas',
  condicion: 'LOCAL',
  tipo: 'AMISTOSO',
  estado: 'JUGADO',
  faltaResultado: false,
  golesClub: 2,
  golesRival: 1,
  categorias: [{ categoriaId: 'c', anio: 2014 }],
  permisos: { gestiona: true, vePlanilla: true, responde: false, indicaResultado: false },
};

describe('CorregirRival', () => {
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

  it('abre el campo con el rival actual y envía el nombre corregido a la ruta del rival', async () => {
    const corregido = { ...jugado, rival: 'Los Tigres' };
    const put = vi.spyOn(api, 'put').mockResolvedValue(corregido);
    const alCorregir = vi.fn();
    act(() => raiz.render(<CorregirRival ruta="/api/p" partido={jugado} alCorregir={alCorregir} />));

    act(() => (contenedor.querySelector('button') as HTMLButtonElement).click());
    const campo = contenedor.querySelector('input') as HTMLInputElement;
    expect(campo.value).toBe('Equipo C');

    await act(async () => {
      const asignar = Object.getOwnPropertyDescriptor(HTMLInputElement.prototype, 'value')!.set!;
      asignar.call(campo, 'Los Tigres');
      campo.dispatchEvent(new Event('input', { bubbles: true }));
    });
    await act(async () => {
      contenedor.querySelector('form')!.dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }));
    });

    expect(put).toHaveBeenCalledWith('/api/p/rival', { rival: 'Los Tigres' });
    expect(alCorregir).toHaveBeenCalledWith(corregido);
  });
});
