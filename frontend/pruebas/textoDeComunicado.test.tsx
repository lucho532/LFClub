import { act } from 'react';
import { createRoot, type Root } from 'react-dom/client';
import { MemoryRouter } from 'react-router-dom';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { api } from '../src/compartido/api/cliente';
import type { ComunicadoRecibidoDto } from '../src/compartido/api/tiposComunicados';
import { DetalleRecibido } from '../src/privado/comunicados/DetalleComunicado';

(globalThis as { IS_REACT_ACT_ENVIRONMENT?: boolean }).IS_REACT_ACT_ENVIRONMENT = true;

const TEXTO = '<script>alert(1)</script> **negrita** [enlace](http://x.test)\nsegunda línea';

describe('texto de un comunicado', () => {
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

  it('se ve tal cual: ni HTML ni formato se interpretan, y se conservan los saltos de línea', async () => {
    const leido: ComunicadoRecibidoDto = {
      id: 'a',
      titulo: '<b>Título</b>',
      texto: TEXTO,
      publicadoEn: '2026-10-12T15:00:00Z',
      publicadoPor: { usuarioRolId: null, nombre: 'Pía Presidenta', rol: 'PRESIDENTE' },
      leido: true,
    };
    vi.spyOn(api, 'get').mockResolvedValue(leido);
    await act(async () => {
      raiz.render(
        <MemoryRouter>
          <DetalleRecibido clubId="c" comunicadoId="a" alLeer={() => undefined} />
        </MemoryRouter>,
      );
    });

    const texto = contenedor.querySelector('.comunicado-texto')!;
    expect(texto.textContent).toBe(TEXTO);
    expect(texto.querySelector('script, strong, b, a')).toBeNull();
    expect(contenedor.querySelector('h2')!.textContent).toBe('<b>Título</b>');
    // Sin ficha a la que ir (ya no es del club), el nombre es texto, no enlace.
    expect(contenedor.querySelector('.nombre-persona')!.tagName).toBe('SPAN');
  });
});
