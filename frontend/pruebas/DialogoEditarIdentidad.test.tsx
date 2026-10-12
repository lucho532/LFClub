import { act } from 'react';
import { createRoot, type Root } from 'react-dom/client';
import { MemoryRouter, Outlet, Route, Routes } from 'react-router-dom';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { api } from '../src/compartido/api/cliente';
import { ErrorApi } from '../src/compartido/api/errores';
import type { ClubDto } from '../src/compartido/api/tipos';
import type { FichaJugadorDto } from '../src/compartido/api/tiposFicha';
import { DialogoEditarIdentidad } from '../src/privado/ficha/DialogoEditarIdentidad';

(globalThis as { IS_REACT_ACT_ENVIRONMENT?: boolean }).IS_REACT_ACT_ENVIRONMENT = true;

// jsdom no implementa <dialog>: se simula lo mínimo que usa el diálogo de confirmación.
HTMLDialogElement.prototype.showModal = function showModal() {
  this.setAttribute('open', '');
};
HTMLDialogElement.prototype.close = function close() {
  this.removeAttribute('open');
};

function ficha(permisos: { puedeCambiar: boolean; puedeCorregirIdentidad: boolean }): FichaJugadorDto {
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
    equipos: [],
    contacto: {} as FichaJugadorDto['contacto'],
    contactoEmergencia: {} as FichaJugadorDto['contactoEmergencia'],
    seguridadSocial: {} as FichaJugadorDto['seguridadSocial'],
    permisos,
    fotoActualizadaEn: null,
  };
}

describe('DialogoEditarIdentidad', () => {
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

  async function abrir(
    base: FichaJugadorDto,
    miRol: ClubDto['miRol'] = 'PRESIDENTE',
    alGuardar = vi.fn(),
    alCancelar = vi.fn(),
  ) {
    const club = { clubId: 'club', miRol, miUsuarioRolId: 'yo' } as ClubDto;
    await act(async () => {
      raiz.render(
        <MemoryRouter>
          <Routes>
            <Route element={<Outlet context={{ club, fijarClub: () => undefined }} />}>
              <Route
                path="*"
                element={
                  <DialogoEditarIdentidad
                    ficha={base}
                    rutaDeLaFicha="/api/clubes/club/jugadores/j1/ficha"
                    alGuardar={alGuardar}
                    alActualizar={vi.fn()}
                    alCancelar={alCancelar}
                  />
                }
              />
            </Route>
          </Routes>
        </MemoryRouter>,
      );
    });
    return { alGuardar, alCancelar };
  }

  const campo = (etiqueta: string) =>
    (Array.from(contenedor.querySelectorAll('label'))
      .find((l) => l.textContent?.includes(etiqueta))
      ?.closest('div')
      ?.querySelector('input, select') as HTMLInputElement | HTMLSelectElement | null | undefined) ?? null;

  async function escribir(etiqueta: string, valor: string) {
    const elemento = campo(etiqueta)!;
    const propiedad = elemento instanceof HTMLSelectElement ? HTMLSelectElement : HTMLInputElement;
    const asignar = Object.getOwnPropertyDescriptor(propiedad.prototype, 'value')!.set!;
    await act(async () => {
      asignar.call(elemento, valor);
      elemento.dispatchEvent(new Event(elemento instanceof HTMLSelectElement ? 'change' : 'input', { bubbles: true }));
    });
  }

  const guardar = () =>
    Array.from(contenedor.querySelectorAll('button')).find((b) => b.textContent === 'Guardar cambios') as HTMLButtonElement;

  const completa = ficha({ puedeCambiar: true, puedeCorregirIdentidad: true });

  it('con los dos permisos muestra todos los campos editables y el título "Editar identidad"', async () => {
    await abrir(completa);

    expect(contenedor.textContent).toContain('Editar identidad');
    for (const etiqueta of ['Nombres', 'Apellidos', 'Fecha de nacimiento', 'Tipo de documento', 'Número de documento']) {
      expect(campo(etiqueta), etiqueta).not.toBeNull();
    }
  });

  it('si solo cambia el nombre, solo se llama a identidad', async () => {
    const put = vi.spyOn(api, 'put').mockResolvedValue({ ...completa, nombres: 'Mateo José' });
    const { alGuardar } = await abrir(completa);

    await escribir('Nombres', 'Mateo José');
    await act(async () => guardar().click());

    expect(put).toHaveBeenCalledTimes(1);
    expect(put).toHaveBeenCalledWith('/api/clubes/club/jugadores/j1/ficha/identidad', {
      nombres: 'Mateo José',
      apellidos: 'Pérez',
      fechaNacimiento: '2014-05-20',
    });
    expect(alGuardar).toHaveBeenCalledWith(expect.objectContaining({ nombres: 'Mateo José' }), {
      documento: false,
      identidad: true,
    });
  });

  it('si solo cambia el documento, solo se llama a documento', async () => {
    const put = vi.spyOn(api, 'put').mockResolvedValue({ ...completa, numeroDocumento: '1000000002' });
    const { alGuardar } = await abrir(completa);

    await escribir('Número de documento', '1000000002');
    await act(async () => guardar().click());

    expect(put).toHaveBeenCalledTimes(1);
    expect(put).toHaveBeenCalledWith('/api/clubes/club/jugadores/j1/ficha/documento-identidad', {
      tipoDocumento: 'TARJETA_IDENTIDAD',
      numeroDocumento: '1000000002',
    });
    expect(alGuardar).toHaveBeenCalledWith(expect.anything(), { documento: true, identidad: false });
  });

  it('si cambian las dos cosas, primero se envía el documento y después la identidad', async () => {
    const put = vi
      .spyOn(api, 'put')
      .mockResolvedValueOnce({ ...completa, numeroDocumento: '1000000002' })
      .mockResolvedValueOnce({ ...completa, numeroDocumento: '1000000002', nombres: 'Mateo José' });
    const { alGuardar } = await abrir(completa);

    await escribir('Nombres', 'Mateo José');
    await escribir('Número de documento', '1000000002');
    await act(async () => guardar().click());

    expect(put.mock.calls.map((llamada) => llamada[0])).toEqual([
      '/api/clubes/club/jugadores/j1/ficha/documento-identidad',
      '/api/clubes/club/jugadores/j1/ficha/identidad',
    ]);
    expect(alGuardar).toHaveBeenCalledWith(expect.anything(), { documento: true, identidad: true });
  });

  it('si el documento está duplicado muestra el error en su campo y no envía la identidad', async () => {
    const put = vi
      .spyOn(api, 'put')
      .mockRejectedValueOnce(
        new ErrorApi({
          title: 'Revisa los datos marcados.',
          status: 400,
          codigo: 'datos_invalidos',
          errores: { numeroDocumento: ['Ya hay un jugador con ese documento en el club.'] },
        }),
      );
    const { alGuardar } = await abrir(completa);

    await escribir('Nombres', 'Mateo José');
    await escribir('Número de documento', '1000000099');
    await act(async () => guardar().click());

    expect(put).toHaveBeenCalledTimes(1);
    expect(contenedor.textContent).toContain('Ya hay un jugador con ese documento en el club.');
    expect(alGuardar).not.toHaveBeenCalled();
  });

  it('quien solo corrige la identidad (la familia) ve el documento como texto, sin campo', async () => {
    await abrir(ficha({ puedeCambiar: false, puedeCorregirIdentidad: true }), 'JUGADOR');

    expect(campo('Nombres')).not.toBeNull();
    expect(campo('Tipo de documento')).toBeNull();
    expect(campo('Número de documento')).toBeNull();
    expect(contenedor.textContent).toContain('1000000001');
    expect(contenedor.textContent).toContain('Tarjeta de identidad');
  });

  it('quien solo cambia el documento ve nombres, apellidos y fecha como texto', async () => {
    await abrir(ficha({ puedeCambiar: true, puedeCorregirIdentidad: false }));

    expect(campo('Nombres')).toBeNull();
    expect(campo('Número de documento')).not.toBeNull();
    expect(contenedor.textContent).toContain('Mateo');
    expect(contenedor.textContent).toContain('Pérez');
  });

  it('Escape cierra el diálogo', async () => {
    const { alCancelar } = await abrir(completa);

    await act(async () => {
      contenedor.querySelector('dialog')!.dispatchEvent(new Event('cancel', { cancelable: true }));
    });

    expect(alCancelar).toHaveBeenCalled();
  });
});
