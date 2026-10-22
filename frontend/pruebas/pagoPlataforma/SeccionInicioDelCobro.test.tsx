import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { api } from '../../src/compartido/api/cliente';
import type { ClubDetalleDto } from '../../src/compartido/api/tipos';
import { SeccionInicioDelCobro } from '../../src/plataforma/SeccionInicioDelCobro';
import { Montaje, clubDe } from '../pagos/ayudas';

const club = (mes: string | null) =>
  ({ clubId: 'club', nombre: 'Club de prueba', mesDeInicioDelCobro: mes }) as ClubDetalleDto;

describe('SeccionInicioDelCobro', () => {
  let montaje: Montaje;

  beforeEach(() => {
    montaje = new Montaje();
  });

  afterEach(() => montaje.desmontar());

  async function abrir(mes: string | null) {
    await montaje.pintar(
      clubDe('PRESIDENTE'),
      '/c',
      '/c',
      <SeccionInicioDelCobro club={club(mes)} alActualizar={() => undefined} recargar={() => undefined} />,
    );
  }

  it('muestra el mes actual de inicio', async () => {
    await abrir('2026-12');

    expect(montaje.campo('Se cobra desde el mes')?.value).toBe('2026-12');
  });

  it('guarda el mes elegido', async () => {
    await abrir(null);
    const fijar = vi.spyOn(api, 'put').mockResolvedValue({} as never);

    await montaje.escribir(montaje.campo('Se cobra desde el mes')!, '2027-01');
    await montaje.enviar(montaje.contenedor.querySelector('form')!);

    expect(fijar).toHaveBeenCalledWith('/api/plataforma/clubes/club/cobro', { mesDeInicio: '2027-01' });
    expect(montaje.texto).toContain('Mes de inicio guardado.');
  });

  it('con el campo vacío envía nulo: no cobrar', async () => {
    await abrir('2026-12');
    const fijar = vi.spyOn(api, 'put').mockResolvedValue({} as never);

    await montaje.escribir(montaje.campo('Se cobra desde el mes')!, '');
    expect(montaje.boton('No cobrar')).toBeDefined();
    await montaje.enviar(montaje.contenedor.querySelector('form')!);

    expect(fijar).toHaveBeenCalledWith('/api/plataforma/clubes/club/cobro', { mesDeInicio: null });
  });
});
