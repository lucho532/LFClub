import { act } from 'react';
import { createRoot, type Root } from 'react-dom/client';
import { MemoryRouter } from 'react-router-dom';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { ProveedorTema } from '../src/compartido/tema/ProveedorTema';
import {
  ENLACE_ACEPTAR_PRUEBA,
  ENLACE_GRUPO_PROBADORES,
  ENLACE_INSTALAR,
  Probadores,
} from '../src/cuenta/Probadores';

(globalThis as { IS_REACT_ACT_ENVIRONMENT?: boolean }).IS_REACT_ACT_ENVIRONMENT = true;

describe('Página de probadores', () => {
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

  it('muestra los tres pasos en orden, cada uno con su botón y su enlace', () => {
    act(() => {
      raiz.render(
        <ProveedorTema>
          <MemoryRouter>
            <Probadores />
          </MemoryRouter>
        </ProveedorTema>,
      );
    });

    const botones = Array.from(contenedor.querySelectorAll('a.boton'));
    expect(botones.map((boton) => boton.textContent)).toEqual([
      '1 · Unirme al grupo',
      '2 · Aceptar la prueba',
      '3 · Instalar LFClub',
    ]);
    expect(botones.map((boton) => boton.getAttribute('href'))).toEqual([
      ENLACE_GRUPO_PROBADORES,
      ENLACE_ACEPTAR_PRUEBA,
      ENLACE_INSTALAR,
    ]);
  });
});
