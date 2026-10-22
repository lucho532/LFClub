import { describe, expect, it } from 'vitest';
import {
  SIN_EQUIPO,
  textoDeEquipos,
  tituloDeGrupo,
} from '../src/privado/jugadores/textos';

describe('textos de la lista de jugadores', () => {
  it('sin equipos dice "Sin equipo"', () => {
    expect(textoDeEquipos([])).toBe(SIN_EQUIPO);
  });

  it('varios equipos van separados por coma', () => {
    expect(textoDeEquipos(['A', 'B'])).toBe('A, B');
  });

  it('el grupo sin categoría se titula "Sin categoría"', () => {
    expect(tituloDeGrupo({ categoria: null, jugadores: [] })).toBe('Sin categoría');
    expect(tituloDeGrupo({ categoria: { categoriaId: 'x', anio: 2014 }, jugadores: [] })).toBe('Categoría 2014');
  });
});
