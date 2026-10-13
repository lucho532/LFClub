import { act } from 'react';
import { createRoot, type Root } from 'react-dom/client';
import { MemoryRouter } from 'react-router-dom';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { Entrar } from '../src/cuenta/Entrar';
import { EliminarCuentaPublica } from '../src/cuenta/EliminarCuentaPublica';
import { Privacidad, CORREO_PRIVACIDAD } from '../src/cuenta/Privacidad';
import { ContextoSesion, type ValorSesion } from '../src/compartido/sesion/contextoSesion';
import { ProveedorTema } from '../src/compartido/tema/ProveedorTema';

(globalThis as { IS_REACT_ACT_ENVIRONMENT?: boolean }).IS_REACT_ACT_ENVIRONMENT = true;

describe('Páginas públicas de eliminación de cuenta', () => {
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
  });

  function pintar(pantalla: React.ReactNode, estado: ValorSesion['estado'] = 'sin_sesion') {
    const valor = { estado, sesion: null } as unknown as ValorSesion;
    act(() => {
      raiz.render(
        <ProveedorTema>
          <ContextoSesion.Provider value={valor}>
            <MemoryRouter>{pantalla}</MemoryRouter>
          </ContextoSesion.Provider>
        </ProveedorTema>,
      );
    });
  }

  it('sin sesión muestra los pasos, lo que se borra, lo que se conserva y el correo', () => {
    pintar(<EliminarCuentaPublica />);

    const texto = contenedor.textContent ?? '';
    expect(texto).toContain('Mi perfil');
    expect(texto).toContain('Eliminar mi cuenta');
    expect(texto).toContain('Qué se borra');
    expect(texto).toContain('Qué se conserva');
    expect(texto).toContain('Persona eliminada');
    expect(texto).toContain('15 días');
    expect(contenedor.querySelector(`a[href="mailto:${CORREO_PRIVACIDAD}"]`)).not.toBeNull();
    expect(contenedor.querySelector('a[href="/recuperar"]')).not.toBeNull();
  });

  it('la pantalla de entrar lleva un fondo decorativo de balones oculto a los lectores de pantalla', () => {
    pintar(<Entrar />);

    const fondo = contenedor.querySelector('.fondo-balones');
    expect(fondo?.getAttribute('aria-hidden')).toBe('true');
    expect(fondo?.querySelectorAll('svg').length).toBeGreaterThan(10);
  });

  it('la pantalla de entrar enlaza a la página pública, en letra pequeña', () => {
    pintar(<Entrar />);

    expect(contenedor.querySelector('.texto-pequeno a[href="/eliminar-cuenta"]')).not.toBeNull();
  });

  it('la política de privacidad menciona la eliminación, lo que se conserva y enlaza a la página', () => {
    pintar(<Privacidad />);

    const texto = contenedor.textContent ?? '';
    expect(texto).toContain('Eliminar mi cuenta');
    expect(texto).toContain('Se conservan los comunicados y resultados');
    expect(texto).toContain('pagos');
    expect(contenedor.querySelector('a[href="/eliminar-cuenta"]')).not.toBeNull();
  });
});
