import { act } from 'react';
import { createRoot, type Root } from 'react-dom/client';
import { MemoryRouter, Outlet, Route, Routes } from 'react-router-dom';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import type { ClubDto } from '../src/compartido/api/tipos';
import type { FichaJugadorDto } from '../src/compartido/api/tiposFicha';
import { SeccionIdentidad } from '../src/privado/ficha/SeccionIdentidad';

(globalThis as { IS_REACT_ACT_ENVIRONMENT?: boolean }).IS_REACT_ACT_ENVIRONMENT = true;

HTMLDialogElement.prototype.showModal = function showModal() {
  this.setAttribute('open', '');
};
HTMLDialogElement.prototype.close = function close() {
  this.removeAttribute('open');
};

function ficha(permisos: FichaJugadorDto['permisos']): FichaJugadorDto {
  return {
    usuarioRolId: 'j1',
    nombres: 'Mateo',
    apellidos: 'Pérez',
    tipoDocumento: 'TARJETA_IDENTIDAD',
    numeroDocumento: '1000000001',
    fechaNacimiento: '2014-05-20',
    esMenorDeEdad: true,
    retirado: false,
    categoriaId: 'c',
    categoriaAnio: 2014,
    equipos: ['A', 'B'],
    contacto: {} as FichaJugadorDto['contacto'],
    contactoEmergencia: {} as FichaJugadorDto['contactoEmergencia'],
    seguridadSocial: {} as FichaJugadorDto['seguridadSocial'],
    permisos,
    fotoActualizadaEn: null,
  };
}

describe('SeccionIdentidad', () => {
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

  async function pintar(base: FichaJugadorDto) {
    const club = { clubId: 'club', miRol: 'PRESIDENTE', miUsuarioRolId: 'yo' } as ClubDto;
    await act(async () => {
      raiz.render(
        <MemoryRouter>
          <Routes>
            <Route element={<Outlet context={{ club, fijarClub: () => undefined }} />}>
              <Route
                path="*"
                element={
                  <SeccionIdentidad ficha={base} rutaDeLaFicha="/api/clubes/club/jugadores/j1/ficha" alCambiar={vi.fn()} />
                }
              />
            </Route>
          </Routes>
        </MemoryRouter>,
      );
    });
  }

  const lapices = () => Array.from(contenedor.querySelectorAll('button[aria-label="Editar identidad"]'));
  const botonesDeTexto = () => Array.from(contenedor.querySelectorAll('button')).map((b) => b.textContent);

  it('con permisos hay un solo lápiz y ningún botón de texto de los dos de antes', async () => {
    await pintar(ficha({ puedeCambiar: true, puedeCorregirIdentidad: true }));

    expect(lapices()).toHaveLength(1);
    expect(contenedor.textContent).not.toContain('Cambiar documento');
    expect(contenedor.textContent).not.toContain('Corregir nombre y fecha');
    expect(botonesDeTexto().filter((texto) => texto !== '')).toEqual([]);
  });

  it('el lápiz abre el diálogo de edición', async () => {
    await pintar(ficha({ puedeCambiar: true, puedeCorregirIdentidad: true }));

    await act(async () => (lapices()[0] as HTMLButtonElement).click());

    expect(contenedor.querySelector('dialog')?.hasAttribute('open')).toBe(true);
    expect(contenedor.querySelector('dialog')?.textContent).toContain('Editar identidad');
  });

  it('con un solo permiso el lápiz aparece', async () => {
    await pintar(ficha({ puedeCambiar: false, puedeCorregirIdentidad: true }));
    expect(lapices()).toHaveLength(1);

    act(() => raiz.unmount());
    raiz = createRoot(contenedor);
    await pintar(ficha({ puedeCambiar: true, puedeCorregirIdentidad: false }));
    expect(lapices()).toHaveLength(1);
  });

  it('sin permisos la tarjeta es solo de vista, sin control de edición', async () => {
    await pintar(ficha({ puedeCambiar: false, puedeCorregirIdentidad: false }));

    expect(lapices()).toHaveLength(0);
    expect(contenedor.querySelectorAll('button')).toHaveLength(0);
    expect(contenedor.textContent).toContain('Mateo');
  });

  it('la categoría y los equipos siguen como texto y el lápiz no los edita', async () => {
    await pintar(ficha({ puedeCambiar: true, puedeCorregirIdentidad: true }));

    expect(contenedor.textContent).toContain('Categoría 2014');
    expect(contenedor.textContent).toContain('A, B');
    await act(async () => (lapices()[0] as HTMLButtonElement).click());
    const dialogo = contenedor.querySelector('dialog')!;
    expect(dialogo.textContent).not.toContain('Equipos');
    expect(dialogo.querySelectorAll('input, select')).toHaveLength(5);
  });
});
