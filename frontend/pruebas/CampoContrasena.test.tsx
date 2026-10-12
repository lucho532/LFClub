import { act } from 'react';
import { createRoot, type Root } from 'react-dom/client';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { Campo } from '../src/compartido/componentes/Campo';

(globalThis as { IS_REACT_ACT_ENVIRONMENT?: boolean }).IS_REACT_ACT_ENVIRONMENT = true;

describe('Campo de contraseña', () => {
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

  it('el ojo muestra y vuelve a ocultar lo escrito', () => {
    act(() => raiz.render(<Campo etiqueta="Contraseña" type="password" valor="secreta" />));
    const entrada = contenedor.querySelector('input')!;
    const ojo = contenedor.querySelector('button')!;

    expect(entrada.type).toBe('password');
    expect(ojo.getAttribute('aria-label')).toBe('Mostrar contraseña');

    act(() => ojo.click());
    expect(entrada.type).toBe('text');
    expect(ojo.getAttribute('aria-label')).toBe('Ocultar contraseña');
    expect(ojo.getAttribute('aria-pressed')).toBe('true');

    act(() => ojo.click());
    expect(entrada.type).toBe('password');
  });

  it('un campo que no es de contraseña no lleva ojo', () => {
    act(() => raiz.render(<Campo etiqueta="Correo" type="email" valor="" />));

    expect(contenedor.querySelector('button')).toBeNull();
  });
});
