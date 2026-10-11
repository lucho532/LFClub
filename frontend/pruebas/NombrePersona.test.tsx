import { act } from 'react';
import { createRoot, type Root } from 'react-dom/client';
import { MemoryRouter, Route, Routes, useLocation } from 'react-router-dom';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { NombrePersona } from '../src/compartido/componentes/NombrePersona';
import type { EnlaceFichaDto } from '../src/compartido/api/tiposPersonas';

(globalThis as { IS_REACT_ACT_ENVIRONMENT?: boolean }).IS_REACT_ACT_ENVIRONMENT = true;

/** Muestra a dónde se llegó y con qué estado, para comprobar el enlace pulsado. */
function Destino() {
  const ubicacion = useLocation();
  return <output>{`${ubicacion.pathname} ${JSON.stringify(ubicacion.state)}`}</output>;
}

describe('NombrePersona', () => {
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

  function pintar(nombre: string, enlace: EnlaceFichaDto | null) {
    act(() =>
      raiz.render(
        <MemoryRouter initialEntries={['/club/c/categorias']}>
          <Routes>
            <Route
              path="/club/c/categorias"
              element={<NombrePersona nombre={nombre} enlace={enlace} clubId="c" textoVolver="Categorías" />}
            />
            <Route path="*" element={<Destino />} />
          </Routes>
        </MemoryRouter>,
      ),
    );
  }

  function pulsar() {
    const enlace = contenedor.querySelector('a');
    expect(enlace).not.toBeNull();
    act(() => enlace!.dispatchEvent(new MouseEvent('click', { bubbles: true, cancelable: true, button: 0 })));
    return contenedor.querySelector('output')?.textContent ?? '';
  }

  it('lleva a la ficha básica con el estado para volver a la pantalla de origen', () => {
    pintar('Ernesto Entrenador', { usuarioRolId: 'e1', tipo: 'BASICA' });

    expect(contenedor.querySelector('a')?.getAttribute('href')).toBe('/club/c/personas/e1');
    expect(pulsar()).toBe('/club/c/personas/e1 {"volver":{"ruta":"/club/c/categorias","texto":"Categorías"}}');
  });

  it('lleva a la ficha del jugador', () => {
    pintar('Ana Arango', { usuarioRolId: 'ana', tipo: 'JUGADOR' });

    expect(contenedor.querySelector('a')?.getAttribute('href')).toBe('/club/c/jugadores/ana/ficha');
    expect(pulsar()).toContain('"texto":"Categorías"');
  });

  it('sin enlace es texto normal, sin elemento de enlace', () => {
    pintar('Pedro Presidente', null);

    expect(contenedor.querySelector('a')).toBeNull();
    expect(contenedor.querySelector('span.nombre-persona')?.textContent).toBe('Pedro Presidente');
  });

  it('un nombre muy largo se pinta entero', () => {
    const largo = `Maximiliano ${'Alejandro '.repeat(20)}de la Santísima Trinidad`;
    pintar(largo, { usuarioRolId: 'x', tipo: 'BASICA' });

    expect(contenedor.querySelector('a.nombre-persona')?.textContent).toBe(largo);
  });
});
