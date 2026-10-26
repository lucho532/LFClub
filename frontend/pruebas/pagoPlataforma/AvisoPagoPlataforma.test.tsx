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
    sessionStorage.clear();
    montaje = new Montaje();
  });

  afterEach(() => montaje.desmontar());

  async function abrir(resumen: unknown) {
    vi.spyOn(api, 'get').mockImplementation(async () => resumen as never);
    await montaje.pintar(clubDe('PRESIDENTE'), '/i', '/i', <AvisoPagoPlataforma clubId="club" />);
  }

  it('al día sin cobros pendientes es una franja verde con el enlace a Plataforma', async () => {
    await abrir(resumenDe({ pendientes: [], estadoDePago: 'AL_DIA' }));

    expect(montaje.texto).toContain('Estás al día con la plataforma');
    expect(montaje.contenedor.querySelector('.franja-pago.tono-exito')).not.toBeNull();
    expect(montaje.contenedor.querySelector('a')?.getAttribute('href')).toBe('/club/club/pago-plataforma');
  });

  it('con un cobro pendiente dentro del plazo sigue en verde y dice cuánto y hasta cuándo', async () => {
    await abrir(resumenDe({ estadoDePago: 'PENDIENTE' }));

    expect(montaje.contenedor.querySelector('.franja-pago.tono-exito')).not.toBeNull();
    expect(montaje.texto).toContain('próximo pago $ 45.000, paga hasta el 7 de noviembre de 2026');
  });

  it('la franja verde se cierra con la X y no vuelve a salir al recargar en la misma pestaña', async () => {
    sessionStorage.clear();
    await abrir(resumenDe({ pendientes: [], estadoDePago: 'AL_DIA' }));

    await montaje.clic(montaje.contenedor.querySelector('button[aria-label="Cerrar el aviso"]') as HTMLButtonElement);
    expect(montaje.texto).toBe('');

    montaje.desmontar();
    montaje = new Montaje();
    await abrir(resumenDe({ pendientes: [], estadoDePago: 'AL_DIA' }));
    expect(montaje.texto).toBe('');
    sessionStorage.clear();
  });

  it('la franja roja no se puede cerrar', async () => {
    sessionStorage.clear();
    await abrir(resumenDe({ estadoDePago: 'VENCIDO' }));

    expect(montaje.contenedor.querySelector('button[aria-label="Cerrar el aviso"]')).toBeNull();
  });

  it('en mora es una franja roja con el valor y el enlace', async () => {
    await abrir(resumenDe({ estadoDePago: 'VENCIDO' }));

    expect(montaje.contenedor.querySelector('.franja-pago.tono-error')).not.toBeNull();
    expect(montaje.texto).toContain('En mora con la plataforma: debes $ 45.000');
    expect(montaje.contenedor.querySelector('a')?.getAttribute('href')).toBe('/club/club/pago-plataforma');
  });

  it('sin cobro todavía no muestra nada', async () => {
    await abrir(resumenDe({ pendientes: [], estadoDePago: 'SIN_COBRO' }));

    expect(montaje.texto).toBe('');
  });

  it('con el club suspendido por impago muestra el aviso rojo en lugar del ámbar', async () => {
    await abrir(resumenDe({ suspendidoPorImpago: true, pendientes: [cobroDe()] }));

    expect(montaje.texto).toContain('Tu club está suspendido por falta de pago');
    expect(montaje.texto).not.toContain('Estás al día');
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
