import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import {
  cargarFotoJugador,
  fotosEnMemoria,
  llaveDeFoto,
  vaciarFotosJugador,
} from '../src/privado/jugadores/fotosJugador';

describe('fotos de los jugadores en memoria', () => {
  let creadas = 0;
  const soltadas: string[] = [];

  beforeEach(() => {
    creadas = 0;
    soltadas.length = 0;
    vi.stubGlobal('URL', {
      ...URL,
      createObjectURL: () => `blob:foto-${++creadas}`,
      revokeObjectURL: (url: string) => soltadas.push(url),
    });
  });

  afterEach(() => {
    vaciarFotosJugador();
    vi.unstubAllGlobals();
  });

  it('la llave cambia con la fecha de la foto y no existe sin foto', () => {
    expect(llaveDeFoto('j1', '2026-10-10T10:00:00Z')).not.toBe(llaveDeFoto('j1', '2026-10-10T11:00:00Z'));
    expect(llaveDeFoto('j1', null)).toBeNull();
  });

  it('sin foto no pide nada', async () => {
    const pedir = vi.fn(async () => new Blob());

    expect(await cargarFotoJugador('c', 'j1', null, pedir)).toBeNull();
    expect(pedir).not.toHaveBeenCalled();
  });

  it('pide la foto con la ruta del club una sola vez mientras la fecha no cambie', async () => {
    const pedir = vi.fn(async () => new Blob());

    const primera = await cargarFotoJugador('c', 'j1', 'f1', pedir);
    const segunda = await cargarFotoJugador('c', 'j1', 'f1', pedir);

    expect(pedir).toHaveBeenCalledTimes(1);
    expect(pedir).toHaveBeenCalledWith('/api/clubes/c/jugadores/j1/foto');
    expect(segunda).toBe(primera);
  });

  it('con otra fecha pide la nueva y suelta la anterior', async () => {
    const pedir = vi.fn(async () => new Blob());

    const vieja = await cargarFotoJugador('c', 'j1', 'f1', pedir);
    const nueva = await cargarFotoJugador('c', 'j1', 'f2', pedir);

    expect(pedir).toHaveBeenCalledTimes(2);
    expect(nueva).not.toBe(vieja);
    expect(soltadas).toEqual([vieja]);
    expect(fotosEnMemoria()).toBe(1);
  });

  it('si la API no la entrega devuelve null para mostrar las iniciales', async () => {
    const pedir = vi.fn(async () => {
      throw new Error('no_encontrado');
    });

    expect(await cargarFotoJugador('c', 'j1', 'f1', pedir)).toBeNull();
    expect(fotosEnMemoria()).toBe(0);
  });

  it('al vaciarla no queda ninguna foto y se sueltan todas', async () => {
    const pedir = vi.fn(async () => new Blob());
    await cargarFotoJugador('c', 'j1', 'f1', pedir);
    await cargarFotoJugador('c', 'j2', 'f1', pedir);

    vaciarFotosJugador();

    expect(fotosEnMemoria()).toBe(0);
    expect(soltadas).toHaveLength(2);
    await cargarFotoJugador('c', 'j1', 'f1', pedir);
    expect(pedir).toHaveBeenCalledTimes(3);
  });
});
