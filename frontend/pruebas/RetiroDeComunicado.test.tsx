import { act } from 'react';
import { createRoot, type Root } from 'react-dom/client';
import { MemoryRouter } from 'react-router-dom';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { api } from '../src/compartido/api/cliente';
import type { ComunicadoPublicadoDto } from '../src/compartido/api/tiposComunicados';
import { ListaPublicados } from '../src/privado/comunicados/ListaPublicados';
import { textoDeAudiencia } from '../src/privado/comunicados/textos';

(globalThis as { IS_REACT_ACT_ENVIRONMENT?: boolean }).IS_REACT_ACT_ENVIRONMENT = true;

function publicado(id: string, titulo: string, puedeRetirar: boolean): ComunicadoPublicadoDto {
  return {
    id,
    titulo,
    texto: 'Texto',
    publicadoEn: '2026-10-12T15:00:00Z',
    publicadoPor: { usuarioRolId: 'p', nombre: 'Pía Presidenta', rol: 'PRESIDENTE' },
    alcance: 'CATEGORIAS',
    destinos: [{ categoriaId: 'c', categoriaNombre: '2015' }],
    familiasAlcanzadas: 3,
    puedeRetirar,
  };
}

describe('Retiro de comunicados', () => {
  let contenedor: HTMLDivElement;
  let raiz: Root;

  beforeEach(() => {
    contenedor = document.createElement('div');
    document.body.appendChild(contenedor);
    raiz = createRoot(contenedor);
    // jsdom no implementa el diálogo modal: se simula lo mínimo.
    HTMLDialogElement.prototype.showModal ??= function showModal(this: HTMLDialogElement) {
      this.setAttribute('open', '');
    };
    HTMLDialogElement.prototype.close ??= function close(this: HTMLDialogElement) {
      this.removeAttribute('open');
    };
  });

  afterEach(() => {
    act(() => raiz.unmount());
    contenedor.remove();
    vi.restoreAllMocks();
  });

  function pintar(comunicados: ComunicadoPublicadoDto[], alRetirar = vi.fn()) {
    act(() =>
      raiz.render(
        <MemoryRouter>
          <ListaPublicados
            clubId="club"
            rol="PRESIDENTE"
            comunicados={comunicados}
            abierto={undefined}
            hayMas={false}
            cargandoMas={false}
            alVerMas={() => undefined}
            alRetirar={alRetirar}
          />
        </MemoryRouter>,
      ),
    );
    return alRetirar;
  }

  const botonesRetirar = () =>
    Array.from(contenedor.querySelectorAll('li button')).filter((b) => b.textContent === 'Retirar');
  const dialogo = () => contenedor.querySelector('dialog') as HTMLDialogElement;
  const botonDelDialogo = (texto: string) =>
    Array.from(dialogo().querySelectorAll('button')).find((b) => b.textContent?.includes(texto)) as HTMLButtonElement;

  it('el botón Retirar solo aparece donde puedeRetirar', () => {
    pintar([publicado('a', 'Mío', true), publicado('b', 'Ajeno', false)]);

    expect(botonesRetirar()).toHaveLength(1);
    expect(contenedor.querySelectorAll('li')[0].textContent).toContain('Retirar');
    expect(contenedor.querySelectorAll('li')[1].textContent).not.toContain('Retirar');
  });

  it('pide confirmación y, al confirmar, la fila pasa a Retirado con quién y cuándo', async () => {
    const retirado: ComunicadoPublicadoDto = {
      ...publicado('a', 'Mío', false),
      retirado: { en: '2026-10-12T18:00:00Z', porNombre: 'Pía Presidenta' },
    };
    const post = vi.spyOn(api, 'post').mockResolvedValue(retirado);
    const alRetirar = pintar([publicado('a', 'Mío', true)]);

    act(() => (botonesRetirar()[0] as HTMLButtonElement).click());
    expect(dialogo().textContent).toContain('Dejará de verse en la campana de las familias');
    expect(post).not.toHaveBeenCalled();

    await act(async () => {
      botonDelDialogo('Retirar comunicado').click();
    });

    expect(post).toHaveBeenCalledWith('/api/clubes/club/comunicados/a/retiro');
    expect(alRetirar).toHaveBeenCalledWith(retirado);
  });

  it('cancelar no llama a la API', () => {
    const post = vi.spyOn(api, 'post');
    pintar([publicado('a', 'Mío', true)]);

    act(() => (botonesRetirar()[0] as HTMLButtonElement).click());
    act(() => botonDelDialogo('Cancelar').click());

    expect(post).not.toHaveBeenCalled();
  });

  it('una fila retirada dice Retirado con el nombre y no ofrece Retirar', () => {
    pintar([
      {
        ...publicado('a', 'Viejo', false),
        retirado: { en: '2026-10-12T18:00:00Z', porNombre: 'Dani Directivo' },
      },
    ]);

    const fila = contenedor.querySelector('li')!.textContent!;
    expect(fila).toContain('Retirado');
    expect(fila).toContain('Retirado por Dani Directivo');
    expect(botonesRetirar()).toHaveLength(0);
  });

  it('escribe a quién iba el comunicado', () => {
    expect(textoDeAudiencia('CLUB', [])).toBe('Todo el club');
    expect(textoDeAudiencia('CATEGORIAS', [{ categoriaNombre: '2015' }, { categoriaNombre: '2016' }])).toBe(
      'Categorías 2015 y 2016',
    );
    expect(
      textoDeAudiencia('EQUIPOS', [
        { categoriaNombre: '2015', equipoNombre: 'A' },
        { categoriaNombre: '2015', equipoNombre: 'B' },
      ]),
    ).toBe('Equipos A y B de la 2015');
  });
});
