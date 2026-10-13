import { act } from 'react';
import { createRoot, type Root } from 'react-dom/client';
import { MemoryRouter, Route, Routes, useLocation } from 'react-router-dom';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { ErrorApi } from '../src/compartido/api/errores';
import type { ResumenEliminacionCuenta } from '../src/compartido/api/tiposCuenta';
import type { SesionDto } from '../src/compartido/api/tipos';
import { ContextoSesion, type ValorSesion } from '../src/compartido/sesion/contextoSesion';
import { ProveedorTema } from '../src/compartido/tema/ProveedorTema';
import { EliminarCuenta } from '../src/cuenta/EliminarCuenta';
import { MiPerfil } from '../src/cuenta/MiPerfil';

(globalThis as { IS_REACT_ACT_ENVIRONMENT?: boolean }).IS_REACT_ACT_ENVIRONMENT = true;

const api = vi.hoisted(() => ({ get: vi.fn(), post: vi.fn() }));
vi.mock('../src/compartido/api/cliente', () => ({ api }));

function sesion(parcial: Partial<SesionDto> = {}): SesionDto {
  return {
    usuarioId: 'u1',
    correo: 'familia@correo.test',
    esDesarrollador: false,
    versionFoto: 0,
    clubes: [
      {
        clubId: 'club-a',
        nombre: 'Club Alfa',
        rol: 'JUGADOR',
        usuarioRolId: 'j1',
        nombres: 'Lucía',
        apellidos: 'Familia',
        jugadores: [],
      },
    ],
    ...parcial,
  } as SesionDto;
}

function resumen(parcial: Partial<ResumenEliminacionCuenta> = {}): ResumenEliminacionCuenta {
  return {
    puedeEliminar: true,
    jugadores: [
      {
        nombreCompleto: 'Lucía Familia',
        clubNombre: 'Club Alfa',
        retirado: false,
        enEspera: false,
      },
      {
        nombreCompleto: 'Mateo Familia',
        clubNombre: 'Club Alfa',
        retirado: true,
        enEspera: false,
      },
      {
        nombreCompleto: 'Sara Familia',
        clubNombre: 'Club Beta',
        retirado: false,
        enEspera: true,
      },
    ],
    clubesQueLoImpiden: [],
    ...parcial,
  };
}

function Ubicacion() {
  const ubicacion = useLocation();
  return (
    <p data-testid="ubicacion">{`${ubicacion.pathname}|${(ubicacion.state as { aviso?: string } | null)?.aviso ?? ''}`}</p>
  );
}

describe('Eliminar mi cuenta', () => {
  let contenedor: HTMLDivElement;
  let raiz: Root;
  const cerrar = vi.fn();

  beforeEach(() => {
    contenedor = document.createElement('div');
    document.body.appendChild(contenedor);
    raiz = createRoot(contenedor);
    api.get.mockReset();
    api.post.mockReset();
    cerrar.mockReset();
  });

  afterEach(() => {
    act(() => raiz.unmount());
    contenedor.remove();
  });

  async function pintar(datos: SesionDto = sesion(), ruta = '/cuenta/eliminar') {
    const valor = {
      estado: 'con_sesion',
      sesion: datos,
      cerrar,
    } as unknown as ValorSesion;
    await act(async () => {
      raiz.render(
        <ProveedorTema>
          <ContextoSesion.Provider value={valor}>
            <MemoryRouter initialEntries={[ruta]}>
              <Routes>
                <Route path="/cuenta/eliminar" element={<EliminarCuenta />} />
                <Route path="/perfil" element={<MiPerfil />} />
                <Route path="/entrar" element={<Ubicacion />} />
              </Routes>
            </MemoryRouter>
          </ContextoSesion.Provider>
        </ProveedorTema>,
      );
    });
  }

  async function escribirYEnviar(contrasena: string) {
    const campo = contenedor.querySelector('input[type="password"]') as HTMLInputElement;
    const definir = Object.getOwnPropertyDescriptor(HTMLInputElement.prototype, 'value')!.set!;
    await act(async () => {
      definir.call(campo, contrasena);
      campo.dispatchEvent(new Event('input', { bubbles: true }));
    });
    await act(async () => {
      (contenedor.querySelector('form') as HTMLFormElement).requestSubmit();
    });
  }

  it('lista a los tres jugadores con su club y su estado dicho en texto', async () => {
    api.get.mockResolvedValue(resumen());
    await pintar();

    const texto = contenedor.textContent ?? '';
    expect(texto).toContain('Lucía Familia');
    expect(texto).toContain('Mateo Familia');
    expect(texto).toContain('(retirado del club)');
    expect(texto).toContain('Sara Familia');
    expect(texto).toContain('Club Beta');
    expect(texto).toContain('(en espera de aprobación)');
    expect(texto).toContain('Persona eliminada');
    expect(contenedor.querySelector('a[href="/club/club-a/jugadores/j1/ficha"]')?.textContent).toBe(
      'Retirar del club',
    );
    expect(contenedor.querySelector('button[type="submit"]')?.textContent).toBe(
      'Eliminar mi cuenta para siempre',
    );
  });

  it('una contraseña incorrecta muestra el error y no navega', async () => {
    api.get.mockResolvedValue(resumen());
    api.post.mockRejectedValue(
      new ErrorApi({
        title: 'La contraseña no es correcta.',
        status: 400,
        codigo: 'contrasena_incorrecta',
      }),
    );
    await pintar();

    await escribirYEnviar('mala');

    expect(api.post).toHaveBeenCalledWith('/api/cuenta/eliminacion', {
      contrasena: 'mala',
    });
    expect(contenedor.textContent).toContain('La contraseña no es correcta.');
    expect(contenedor.querySelector('[data-testid="ubicacion"]')).toBeNull();
    expect(cerrar).not.toHaveBeenCalled();
  });

  it('al eliminar limpia la sesión local y vuelve a /entrar con el aviso', async () => {
    api.get.mockResolvedValue(resumen());
    api.post.mockResolvedValue(undefined);
    await pintar();

    await escribirYEnviar('Contrasena-de-prueba-1');

    expect(cerrar).toHaveBeenCalledTimes(1);
    expect(contenedor.querySelector('[data-testid="ubicacion"]')?.textContent).toBe(
      '/entrar|Tu cuenta se eliminó',
    );
  });

  it('con un club donde es presidente único se ve el club y no hay campo ni botón', async () => {
    api.get.mockResolvedValue(
      resumen({
        puedeEliminar: false,
        clubesQueLoImpiden: [{ clubId: 'club-a', clubNombre: 'Club Alfa' }],
      }),
    );
    await pintar();

    expect(contenedor.textContent).toContain('Eres el único presidente de');
    expect(contenedor.textContent).toContain('Club Alfa');
    expect(contenedor.textContent).toContain('Nombra antes a otro presidente');
    expect(contenedor.querySelector('input[type="password"]')).toBeNull();
    expect(contenedor.querySelector('button[type="submit"]')).toBeNull();
  });

  it('un 409 presidente_unico del envío muestra el club y quita el formulario', async () => {
    api.get.mockResolvedValue(resumen());
    api.post.mockRejectedValue(
      new ErrorApi({
        title: 'Eres el único presidente de un club.',
        status: 409,
        codigo: 'presidente_unico',
        clubes: [{ clubId: 'club-b', clubNombre: 'Club Beta' }],
      }),
    );
    await pintar();

    await escribirYEnviar('Contrasena-de-prueba-1');

    expect(contenedor.textContent).toContain('Club Beta');
    expect(contenedor.querySelector('input[type="password"]')).toBeNull();
  });

  it('Mi perfil ofrece eliminar la cuenta, salvo a la cuenta DESARROLLADOR', async () => {
    await pintar(sesion(), '/perfil');
    expect(contenedor.querySelector('a[href="/cuenta/eliminar"]')).not.toBeNull();

    act(() => raiz.unmount());
    raiz = createRoot(contenedor);
    await pintar(sesion({ esDesarrollador: true, clubes: [] }), '/perfil');
    expect(contenedor.querySelector('a[href="/cuenta/eliminar"]')).toBeNull();
  });
});
