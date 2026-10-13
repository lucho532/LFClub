import { act, useState } from 'react';
import { createRoot, type Root } from 'react-dom/client';
import { MemoryRouter } from 'react-router-dom';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import type { EquipoDeActividadDto } from '../src/compartido/api/tiposAudiencia';
import { EditorConvocatoria, type JugadorConvocable } from '../src/privado/partidos/EditorConvocatoria';

(globalThis as { IS_REACT_ACT_ENVIRONMENT?: boolean }).IS_REACT_ACT_ENVIRONMENT = true;

const A: EquipoDeActividadDto = { equipoId: 'a', nombre: 'A', categoriaId: 'c', anio: 2014, activo: true };
const B: EquipoDeActividadDto = { equipoId: 'b', nombre: 'B', categoriaId: 'c', anio: 2014, activo: true };

function jugador(id: string, apellidos: string, equipoIds: string[], extra: Partial<JugadorConvocable> = {}): JugadorConvocable {
  return { usuarioRolId: id, nombres: 'Nombre', apellidos, anio: 2014, equipoIds, ...extra };
}

const JUGADORES = [
  jugador('j3', 'Tres', ['a']),
  jugador('j4', 'Cuatro', ['a']),
  jugador('j5', 'Cinco', ['b']),
  jugador('j6', 'Seis', ['a', 'b']),
  jugador('j7', 'Siete', []),
  jugador('k1', 'Kappa', [], { anio: 2015, anadido: true }),
];

/** Lleva la lista de elegidos como lo hace la pantalla y la escribe en `data-elegidos`. */
function Caso({ equipos, inicial = [] }: { equipos: EquipoDeActividadDto[]; inicial?: string[] }) {
  const [elegidos, setElegidos] = useState<string[]>(inicial);
  return (
    <MemoryRouter>
      <div data-elegidos={[...elegidos].sort().join(',')}>
        <EditorConvocatoria clubId="club" jugadores={JUGADORES} equipos={equipos} elegidos={elegidos} alCambiar={setElegidos} />
      </div>
    </MemoryRouter>
  );
}

describe('EditorConvocatoria agrupado por equipo', () => {
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

  const elegidos = () => contenedor.querySelector('[data-elegidos]')!.getAttribute('data-elegidos');
  const grupo = (titulo: string) =>
    [...contenedor.querySelectorAll('fieldset')].find((campo) => campo.querySelector('legend')?.textContent === titulo)!;
  const boton = (dentro: Element, texto: string) =>
    [...dentro.querySelectorAll('button')].find((candidato) => candidato.textContent === texto)!;

  it('agrupa por equipo, con "Sin equipo" y "Añadidos" aparte', () => {
    act(() => raiz.render(<Caso equipos={[B, A]} />));

    const titulos = [...contenedor.querySelectorAll('legend')].map((legenda) => legenda.textContent);
    expect(titulos).toEqual(['Equipo A · 2014', 'Equipo B · 2014', 'Sin equipo', 'Añadidos']);
    expect(grupo('Añadidos').textContent).toContain('Añadido');
  });

  it('"Marcar a todos" marca a todo el equipo y "Quitar a todos" lo suelta', () => {
    act(() => raiz.render(<Caso equipos={[A, B]} />));

    act(() => boton(grupo('Equipo A · 2014'), 'Marcar a todos').click());
    expect(elegidos()).toBe('j3,j4,j6');

    act(() => boton(grupo('Equipo A · 2014'), 'Quitar a todos').click());
    expect(elegidos()).toBe('');
  });

  it('desmarcar a uno deja a los demás del equipo', () => {
    act(() => raiz.render(<Caso equipos={[A, B]} inicial={['j3', 'j4', 'j6']} />));

    const casillas = grupo('Equipo A · 2014').querySelectorAll<HTMLInputElement>('input[type="checkbox"]');
    // Ordenados por apellidos: Cuatro, Seis, Tres.
    act(() => casillas[0].click());

    expect(elegidos()).toBe('j3,j6');
  });

  it('un jugador de dos equipos aparece en ambos con la misma marca y se cuenta una sola vez', () => {
    act(() => raiz.render(<Caso equipos={[A, B]} />));

    act(() => boton(grupo('Equipo B · 2014'), 'Marcar a todos').click());

    const enA = grupo('Equipo A · 2014').querySelectorAll<HTMLInputElement>('input[type="checkbox"]');
    const marcadosEnA = [...enA].filter((casilla) => casilla.checked).length;
    expect(marcadosEnA).toBe(1); // j6, que está en los dos equipos
    expect(elegidos()).toBe('j5,j6');
    expect(contenedor.textContent).toContain('2 de 6 seleccionados');
  });

  it('sin equipos agrupa por categoría, como antes', () => {
    act(() => raiz.render(<Caso equipos={[]} />));

    const titulos = [...contenedor.querySelectorAll('legend')].map((legenda) => legenda.textContent);
    expect(titulos).toEqual(['Categoría 2014', 'Añadidos']);
  });
});
