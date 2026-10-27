import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { api } from '../../src/compartido/api/cliente';
import { DisposicionSuscripcion } from '../../src/privado/pagoPlataforma/DisposicionSuscripcion';
import { PagosRealizados } from '../../src/privado/pagoPlataforma/PagosRealizados';
import { PantallaComoSeCobra } from '../../src/privado/pagoPlataforma/PantallaComoSeCobra';
import { Montaje, clubDe, simularGet } from '../pagos/ayudas';
import { cobroDe, resumenDe } from './ayudasPagoPlataforma';

describe('Pestañas de la suscripción', () => {
  let montaje: Montaje;

  beforeEach(() => {
    montaje = new Montaje();
  });

  afterEach(() => montaje.desmontar());

  it('«Cómo se cobra» explica la tarifa con la tabla de rangos y marca el rango del club', async () => {
    simularGet({ '/api/clubes/club/pago-plataforma': resumenDe() });
    await montaje.pintar(clubDe('PRESIDENTE'), '/c', '/c', <PantallaComoSeCobra />);

    expect(montaje.texto).toContain('Cómo se cobra');
    expect(montaje.texto).toContain('1 a 20 jugadores');
    expect(montaje.texto).toContain('$ 25.000');
    expect(montaje.texto).toContain('41 jugadores o más');
    expect(montaje.texto).toContain('$ 75.000');
    expect(montaje.texto).toContain('Tu club hoy: 25');
    expect(montaje.texto).toContain('los entrenadores, directivos y presidentes no suman');
    expect(montaje.texto).toContain('Tienes 5 días desde que se genera para pagar');
  });

  it('«Cómo se cobra» sin tarifa definida lo dice en lugar de mostrar filas', async () => {
    simularGet({
      '/api/clubes/club/pago-plataforma': resumenDe({ tarifa: { rangos: [], diasDeGracia: 1, jugadores: 3, rangoDelClub: null } }),
    });
    await montaje.pintar(clubDe('PRESIDENTE'), '/c', '/c', <PantallaComoSeCobra />);

    expect(montaje.texto).toContain('La tarifa todavía no está definida');
    expect(montaje.texto).toContain('Tienes 1 día desde que se genera');
    expect(montaje.texto).not.toContain('Tu club hoy');
  });

  it('«Pagos realizados» trae el comprobante de un cobro pagado y el motivo de uno anulado', async () => {
    simularGet({
      '/api/clubes/club/pago-plataforma': resumenDe({
        pendientes: [],
        historial: [
          cobroDe({
            cobroId: 'c0',
            periodo: '2026-10',
            estado: 'PAGADO',
            pagadoEn: '2026-10-05T15:00:00Z',
            origenDelPago: 'PASARELA',
            referencia: 'LFC-abc',
            medioDePago: 'PSE',
          }),
          cobroDe({ cobroId: 'c9', periodo: '2026-09', estado: 'ANULADO', motivoAnulacion: 'Cobro duplicado' }),
        ],
      }),
    });
    await montaje.pintar(clubDe('PRESIDENTE'), '/p', '/p', <PagosRealizados />);

    expect(montaje.texto).toContain('✓ Pagado');
    expect(montaje.texto).toContain('Referencia: LFC-abc');
    expect(montaje.texto).toContain('Medio de pago: PSE');
    expect(montaje.texto).toContain('Cobro anulado: Cobro duplicado');
  });

  it('«Pagos realizados» sin historial lo dice', async () => {
    simularGet({ '/api/clubes/club/pago-plataforma': resumenDe({ pendientes: [], historial: [] }) });
    await montaje.pintar(clubDe('PRESIDENTE'), '/p', '/p', <PagosRealizados />);

    expect(montaje.texto).toContain('Todavía no hay pagos registrados.');
  });

  it('la sección tiene el título Suscripción y las tres pestañas', async () => {
    await montaje.pintar(clubDe('PRESIDENTE'), '/s', '/s', <DisposicionSuscripcion />);

    expect(montaje.texto).toContain('Suscripción');
    const pestanas = [...montaje.contenedor.querySelectorAll('nav.pestanas-pagos a')].map((a) => a.textContent);
    expect(pestanas).toEqual(['Pago', 'Cómo se cobra', 'Pagos realizados']);
  });

  it('los demás roles vuelven al inicio sin pedir nada', async () => {
    const pedir = vi.spyOn(api, 'get');

    for (const rol of ['DIRECTIVO', 'ENTRENADOR', 'JUGADOR'] as const) {
      await montaje.pintar(clubDe(rol), '/s', '/s', <DisposicionSuscripcion />);
      expect(montaje.texto).toBe('otra pantalla');
    }

    expect(pedir).not.toHaveBeenCalled();
  });
});
