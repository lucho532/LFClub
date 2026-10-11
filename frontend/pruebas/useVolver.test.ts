import { describe, expect, it } from 'vitest';
import { estadoVolver, volverDesde } from '../src/compartido/navegacion/useVolver';

const inicio = { ruta: '/club/c', texto: 'Inicio' };

describe('volver a la pantalla de origen', () => {
  it('con estado vuelve a la pantalla de la que se llegó, con su nombre', () => {
    const estado = estadoVolver('/club/c/categorias', 'Categorías');

    expect(volverDesde(estado, inicio)).toEqual({ ruta: '/club/c/categorias', texto: 'Categorías' });
  });

  it('sin estado (dirección escrita) usa el predeterminado', () => {
    expect(volverDesde(null, inicio)).toEqual(inicio);
    expect(volverDesde(undefined, inicio)).toEqual(inicio);
  });

  it('con un estado mal formado usa el predeterminado', () => {
    expect(volverDesde({ desde: 'jugadores' }, inicio)).toEqual(inicio);
    expect(volverDesde({ volver: { ruta: 3, texto: 'Jugadores' } }, inicio)).toEqual(inicio);
    expect(volverDesde({ volver: { ruta: '/club/c', texto: '  ' } }, inicio)).toEqual(inicio);
    expect(volverDesde({ volver: { ruta: 'https://otro.sitio', texto: 'Otro' } }, inicio)).toEqual(inicio);
    expect(volverDesde({ volver: { ruta: '//otro.sitio', texto: 'Otro' } }, inicio)).toEqual(inicio);
    expect(volverDesde('texto', inicio)).toEqual(inicio);
  });
});
