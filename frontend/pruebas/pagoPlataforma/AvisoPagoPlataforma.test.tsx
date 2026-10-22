import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { api } from '../../src/compartido/api/cliente';
import type { ClubDto } from '../../src/compartido/api/tipos';
import { InicioClub } from '../../src/privado/InicioClub';
import { AvisoPagoPlataforma } from '../../src/privado/pagoPlataforma/AvisoPagoPlataforma';
import { Montaje, clubDe } from '../pagos/ayudas';
import { cobroDe, resumenDe } from './ayudasPagoPlataforma';

describe('AvisoPagoPlataforma', () => {
  let montaje: Montaje;

  beforeEach(() => {
    montaje = new Montaje();
  });

  afterEach(() => montaje.desmontar());

  async function abrir(resumen: unknown) {
    vi.spyOn(api, 'get').mockImplementation(async () => resumen as never);
    await montaje.pintar(clubDe('PRESIDENTE'), '/i', '/i', <AvisoPagoPlataforma clubId="club" />);
  }

  it('con un cobro pendiente muestra el valor, hasta cuándo se paga y el enlace', async () => {
    await abrir(resumenDe());

    expect(montaje.texto).toContain('Tienes un cobro de la plataforma por $ 45.000');
    expect(montaje.texto).toContain('Paga hasta el 7 de noviembre de 2026');
    expect(montaje.contenedor.querySelector('a')?.getAttribute('href')).toBe('/club/club/pago-plataforma');
  });

  it('sin cobros pendientes no muestra nada', async () => {
    await abrir(resumenDe({ pendientes: [] }));

    expect(montaje.texto).toBe('');
  });

  it('con el club suspendido por impago muestra el aviso rojo en lugar del ámbar', async () => {
    await abrir(resumenDe({ suspendidoPorImpago: true, pendientes: [cobroDe()] }));

    expect(montaje.texto).toContain('Tu club está suspendido por falta de pago');
    expect(montaje.texto).not.toContain('Tienes un cobro de la plataforma');
  });

  it('mientras carga, o si falla, no muestra nada ni rompe el inicio', async () => {
    vi.spyOn(api, 'get').mockRejectedValue(new Error('sin conexión'));
    await montaje.pintar(clubDe('PRESIDENTE'), '/i', '/i', <AvisoPagoPlataforma clubId="club" />);

    expect(montaje.texto).toBe('');
  });
});

describe('Inicio del club y el cobro de la plataforma', () => {
  let montaje: Montaje;

  beforeEach(() => {
    montaje = new Montaje();
  });

  afterEach(() => montaje.desmontar());

  it('solo el PRESIDENTE pide el resumen del cobro; los demás roles no', async () => {
    const pedidos: string[] = [];
    vi.spyOn(api, 'get').mockImplementation(async (ruta: string) => {
      pedidos.push(ruta);
      throw new Error('sin datos');
    });

    for (const rol of ['DIRECTIVO', 'ENTRENADOR', 'JUGADOR'] as const) {
      const club: ClubDto = clubDe(rol);
      await montaje.pintar(club, '/i', '/i', <InicioClub />);
    }
    expect(pedidos.some((ruta) => ruta.includes('pago-plataforma'))).toBe(false);

    await montaje.pintar(clubDe('PRESIDENTE'), '/i', '/i', <InicioClub />);
    expect(pedidos.some((ruta) => ruta.includes('pago-plataforma'))).toBe(true);
  });
});
