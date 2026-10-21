import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import type { JugadorDeListaDto } from '../../src/compartido/api/tiposJugadores';
import { TablaDeGrupo } from '../../src/privado/jugadores/TablaDeGrupo';
import { Montaje, clubDe } from './ayudas';

function jugador(id: string, nombres: string, extra: Partial<JugadorDeListaDto>): JugadorDeListaDto {
  return { usuarioRolId: id, nombres, apellidos: 'Pérez', equipos: [], fotoActualizadaEn: null, ...extra };
}

describe('columna "Al día" de la lista de jugadores', () => {
  let montaje: Montaje;

  beforeEach(() => {
    montaje = new Montaje();
  });

  afterEach(() => montaje.desmontar());

  async function abrir(jugadores: JugadorDeListaDto[]) {
    await montaje.pintar(
      clubDe('PRESIDENTE'),
      '/j',
      '/j',
      <TablaDeGrupo clubId="club" titulo="Categoría 2014" jugadores={jugadores} />,
    );
  }

  it('muestra al presidente y al directivo el estado real de cada jugador, con texto', async () => {
    await abrir([
      jugador('a', 'Ana', { estadoCuenta: 'AL_DIA' }),
      jugador('b', 'Beto', { estadoCuenta: 'PAGO_PARCIAL' }),
      jugador('c', 'Caro', { estadoCuenta: 'DEBE' }),
    ]);

    const titulos = [...montaje.contenedor.querySelectorAll('th')].map((celda) => celda.textContent);
    expect(titulos).toContain('Al día');
    const estados = [...montaje.contenedor.querySelectorAll('td[data-titulo="Al día"]')].map((celda) => celda.textContent);
    expect(estados).toEqual(['Al día', 'Pago parcial', 'Debe']);
  });

  it('cada estado abre la cuenta del jugador', async () => {
    await abrir([jugador('a', 'Ana', { estadoCuenta: 'DEBE' })]);

    const enlace = montaje.contenedor.querySelector('td[data-titulo="Al día"] a');
    expect(enlace?.getAttribute('href')).toBe('/club/club/jugadores/a/cuenta');
  });

  it('no se pinta para el entrenador, que recibe el estado nulo', async () => {
    await abrir([jugador('a', 'Ana', { estadoCuenta: null }), jugador('b', 'Beto', {})]);

    const titulos = [...montaje.contenedor.querySelectorAll('th')].map((celda) => celda.textContent);
    expect(titulos).toEqual(['Jugador', 'Equipos']);
    expect(montaje.texto).not.toContain('Debe');
  });
});
