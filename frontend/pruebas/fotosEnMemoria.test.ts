import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import {
  cargarFoto,
  cuantasFotosEnMemoria,
  llaveDeFotoEnMemoria,
  vaciarFotosEnMemoria,
} from '../src/compartido/fotos/fotosEnMemoria';

describe('fotos de personas en memoria', () => {
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
    vaciarFotosEnMemoria();
    vi.unstubAllGlobals();
  });

  it('la llave cambia con el sello', () => {
    expect(llaveDeFotoEnMemoria('/ruta', 1)).not.toBe(llaveDeFotoEnMemoria('/ruta', 2));
    expect(llaveDeFotoEnMemoria('/ruta', 'f1')).not.toBe(llaveDeFotoEnMemoria('/ruta', 'f2'));
  });

  it('con sello null o 0 no hay llave y no pide nada', async () => {
    const pedir = vi.fn(async () => new Blob());

    expect(llaveDeFotoEnMemoria('/ruta', null)).toBeNull();
    expect(llaveDeFotoEnMemoria('/ruta', 0)).toBeNull();
    expect(await cargarFoto('/ruta', null, pedir)).toBeNull();
    expect(await cargarFoto('/ruta', 0, pedir)).toBeNull();
    expect(pedir).not.toHaveBeenCalled();
  });

  it('pide la ruta una sola vez mientras el sello no cambie y suelta la anterior al cambiar', async () => {
    const pedir = vi.fn(async () => new Blob());

    const primera = await cargarFoto('/api/clubes/c/personas/p/foto', 1, pedir);
    const otraVez = await cargarFoto('/api/clubes/c/personas/p/foto', 1, pedir);
    const nueva = await cargarFoto('/api/clubes/c/personas/p/foto', 2, pedir);

    expect(pedir).toHaveBeenCalledTimes(2);
    expect(pedir).toHaveBeenCalledWith('/api/clubes/c/personas/p/foto');
    expect(otraVez).toBe(primera);
    expect(nueva).not.toBe(primera);
    expect(soltadas).toEqual([primera]);
  });

  it('rutas distintas con el mismo sello no se pisan', async () => {
    const pedir = vi.fn(async () => new Blob());

    const persona = await cargarFoto('/api/clubes/c/personas/x/foto', 1, pedir);
    const jugador = await cargarFoto('/api/clubes/c/jugadores/x/foto', 1, pedir);

    expect(pedir).toHaveBeenCalledTimes(2);
    expect(persona).not.toBe(jugador);
    expect(cuantasFotosEnMemoria()).toBe(2);
    expect(soltadas).toEqual([]);
  });

  it('si la API no la entrega devuelve null', async () => {
    const pedir = vi.fn(async () => {
      throw new Error('no_encontrado');
    });

    expect(await cargarFoto('/ruta', 3, pedir)).toBeNull();
    expect(cuantasFotosEnMemoria()).toBe(0);
  });

  it('al vaciarla la caché queda vacía y se sueltan todas', async () => {
    const pedir = vi.fn(async () => new Blob());
    await cargarFoto('/a', 1, pedir);
    await cargarFoto('/b', 'f1', pedir);

    vaciarFotosEnMemoria();

    expect(cuantasFotosEnMemoria()).toBe(0);
    expect(soltadas).toHaveLength(2);
    await cargarFoto('/a', 1, pedir);
    expect(pedir).toHaveBeenCalledTimes(3);
  });
});
