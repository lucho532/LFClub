import { describe, expect, it } from 'vitest';
import {
  SIN_EQUIPO,
  TEXTO_AL_DIA,
  TITULO_AL_DIA,
  textoDeEquipos,
  tituloDeGrupo,
} from '../src/privado/jugadores/textos';

describe('columna "Al día" de la lista de jugadores', () => {
  // Los pagos no existen: nada puede leerse como un estado de pago real (CE-007).
  const prohibidas = ['al día', 'debe', 'deuda', 'pendiente', 'pagado', 'moroso'];

  it.each(prohibidas)('ni el texto ni su explicación dicen "%s"', (palabra) => {
    expect(TEXTO_AL_DIA.toLowerCase()).not.toContain(palabra);
    expect(TITULO_AL_DIA.toLowerCase()).not.toContain(palabra);
  });

  it('es un texto fijo, el mismo para cualquier jugador', () => {
    expect(TEXTO_AL_DIA).toBe('Sin datos aún');
    expect(TITULO_AL_DIA).toBe('El estado de pago todavía no está disponible');
  });
});

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
