import { act } from 'react';
import { createRoot, type Root } from 'react-dom/client';
import { MemoryRouter, Outlet, Route, Routes } from 'react-router-dom';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { api } from '../src/compartido/api/cliente';
import { ErrorApi } from '../src/compartido/api/errores';
import type { ClubDto, Rol } from '../src/compartido/api/tipos';
import type { CategoriaDto } from '../src/compartido/api/tiposCategorias';
import { FormularioComunicado } from '../src/privado/comunicados/FormularioComunicado';

(globalThis as { IS_REACT_ACT_ENVIRONMENT?: boolean }).IS_REACT_ACT_ENVIRONMENT = true;

const categorias: CategoriaDto[] = [
  {
    categoriaId: 'c14',
    anio: 2014,
    activa: true,
    sePuedeBorrar: false,
    numeroJugadores: 4,
    equipos: [
      { equipoId: 'eA', nombre: 'A', numeroJugadores: 2, sePuedeBorrar: false },
      { equipoId: 'eB', nombre: 'B', numeroJugadores: 2, sePuedeBorrar: false },
    ],
    entrenadores: [{ usuarioRolId: 'yo', nombres: 'E', apellidos: 'Uno', rol: 'ENTRENADOR', equipos: [] }],
  },
  {
    categoriaId: 'c15',
    anio: 2015,
    activa: true,
    sePuedeBorrar: false,
    numeroJugadores: 3,
    equipos: [],
    entrenadores: [],
  },
] as unknown as CategoriaDto[];

describe('FormularioComunicado', () => {
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

  async function abrir(rol: Rol) {
    const club = { clubId: 'club', miRol: rol, miUsuarioRolId: 'yo' } as ClubDto;
    await act(async () => {
      raiz.render(
        <MemoryRouter initialEntries={['/nuevo']}>
          <Routes>
            <Route element={<Outlet context={{ club, fijarClub: () => undefined }} />}>
              <Route path="/nuevo" element={<FormularioComunicado />} />
            </Route>
            <Route path="*" element={<output>lista</output>} />
          </Routes>
        </MemoryRouter>,
      );
    });
  }

  function escribir(elemento: HTMLInputElement | HTMLTextAreaElement, valor: string) {
    const prototipo = elemento instanceof HTMLTextAreaElement ? HTMLTextAreaElement : HTMLInputElement;
    return act(async () => {
      Object.getOwnPropertyDescriptor(prototipo.prototype, 'value')!.set!.call(elemento, valor);
      elemento.dispatchEvent(new Event('input', { bubbles: true }));
    });
  }

  const campos = () => ({
    titulo: contenedor.querySelector('input') as HTMLInputElement,
    texto: contenedor.querySelector('textarea') as HTMLTextAreaElement,
  });
  const boton = (texto: string) =>
    Array.from(contenedor.querySelectorAll('button')).find((b) => b.textContent?.startsWith(texto)) as HTMLButtonElement;
  const enviar = () =>
    act(async () => {
      contenedor.querySelector('form')!.dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }));
    });

  it('limita el título a 100 y el texto a 1.000 y cuenta lo que queda', async () => {
    vi.spyOn(api, 'get').mockResolvedValue(categorias);
    await abrir('PRESIDENTE');
    const { titulo, texto } = campos();

    expect(titulo.maxLength).toBe(100);
    expect(texto.maxLength).toBe(1000);
    await escribir(titulo, 'Hola');
    await escribir(texto, 'Texto');

    expect(contenedor.textContent).toContain('96 caracteres restantes');
    expect(contenedor.textContent).toContain('995 caracteres restantes');
  });

  it('el presidente y el directivo ven "Todo el club"; el entrenador no', async () => {
    vi.spyOn(api, 'get').mockResolvedValue(categorias);

    await abrir('PRESIDENTE');
    expect(boton('Todo el club')).toBeDefined();
    expect(boton('Categorías')).toBeDefined();
    expect(boton('Equipos de una categoría')).toBeDefined();

    act(() => raiz.unmount());
    raiz = createRoot(contenedor);
    await abrir('ENTRENADOR');
    expect(boton('Todo el club')).toBeUndefined();
    expect(boton('Categorías')).toBeDefined();
  });

  it('el entrenador solo ve las categorías en las que figura como entrenador', async () => {
    vi.spyOn(api, 'get').mockResolvedValue(categorias);
    await abrir('ENTRENADOR');

    const botones = Array.from(contenedor.querySelectorAll('.selector-audiencia .boton-conmutable')).map(
      (b) => b.textContent,
    );

    expect(botones).toContain('2014');
    expect(botones).not.toContain('2015');
  });

  it('pide elegir al menos una categoría antes de publicar', async () => {
    vi.spyOn(api, 'get').mockResolvedValue(categorias);
    const post = vi.spyOn(api, 'post');
    await abrir('ENTRENADOR');
    await escribir(campos().titulo, 'Aviso');
    await escribir(campos().texto, 'Texto');

    await enviar();

    expect(post).not.toHaveBeenCalled();
    expect(contenedor.textContent).toContain('Elige al menos una categoría');
  });

  it('conserva la misma clave entre reintentos y deshabilita el botón mientras la petición va', async () => {
    vi.spyOn(api, 'get').mockResolvedValue(categorias);
    let resolver: (valor: unknown) => void = () => undefined;
    const post = vi
      .spyOn(api, 'post')
      .mockRejectedValueOnce(new ErrorApi({ title: 'Sin conexión', status: 0, codigo: 'sin_conexion' }))
      .mockImplementationOnce(() => new Promise((resolve) => (resolver = resolve)));
    await abrir('PRESIDENTE');
    await escribir(campos().titulo, 'Aviso');
    await escribir(campos().texto, 'Texto');

    await enviar();
    await enviar();

    expect(boton('Publicando').disabled).toBe(true);
    // Un doble toque con la petición en vuelo no envía otra.
    await enviar();
    expect(post).toHaveBeenCalledTimes(2);
    const claves = post.mock.calls.map((llamada) => (llamada[1] as { clave: string }).clave);
    expect(claves[0]).toBe(claves[1]);
    expect(claves[0]).toMatch(/^[0-9a-f-]{36}$/);

    await act(async () => resolver({ familiasAlcanzadas: 3 }));
    expect(contenedor.textContent).toBe('lista');
  });

  it('muestra "Esa categoría no tiene jugadores activos" cuando la API dice sin_destinatarios', async () => {
    vi.spyOn(api, 'get').mockResolvedValue(categorias);
    vi.spyOn(api, 'post').mockRejectedValue(
      new ErrorApi({ title: 'Esa audiencia no tiene jugadores activos', status: 400, codigo: 'sin_destinatarios' }),
    );
    await abrir('PRESIDENTE');
    await escribir(campos().titulo, 'Aviso');
    await escribir(campos().texto, 'Texto');

    await enviar();

    expect(contenedor.textContent).toContain('Esa categoría no tiene jugadores activos');
  });
});
