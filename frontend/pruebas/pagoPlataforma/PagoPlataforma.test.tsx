import { act } from 'react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { api } from '../../src/compartido/api/cliente';
import { PagoPlataforma } from '../../src/privado/pagoPlataforma/PagoPlataforma';
import { Montaje, clubDe, simularGet } from '../pagos/ayudas';
import { cobroDe, resumenDe } from './ayudasPagoPlataforma';

describe('PagoPlataforma', () => {
  let montaje: Montaje;
  const asignar = vi.fn();
  const locationOriginal = window.location;

  beforeEach(() => {
    montaje = new Montaje();
    asignar.mockReset();
    Object.defineProperty(window, 'location', { value: { assign: asignar }, writable: true, configurable: true });
  });

  afterEach(() => {
    Object.defineProperty(window, 'location', { value: locationOriginal, writable: true, configurable: true });
    montaje.desmontar();
  });

  async function abrir(resumen = resumenDe()) {
    simularGet({ '/api/clubes/club/pago-plataforma': resumen });
    await montaje.pintar(clubDe('PRESIDENTE'), '/pago', '/pago', <PagoPlataforma />);
  }

  it('muestra el detalle por rol, el total y hasta cuándo se paga', async () => {
    await abrir();

    expect(montaje.texto).toContain('Uso de la plataforma · noviembre de 2026');
    expect(montaje.texto).toContain('Jugadores: 20 × $ 1.500 = $ 30.000');
    expect(montaje.texto).toContain('Entrenadores: 3 × $ 5.000 = $ 15.000');
    expect(montaje.texto).not.toContain('Directivos');
    expect(montaje.texto).toContain('Total: $ 45.000');
    expect(montaje.texto).toContain('Paga hasta el 7 de noviembre de 2026');
    expect(montaje.texto).toContain('! Pendiente');
  });

  it('un cobro por rango muestra los jugadores y el rango aplicado en vez del detalle por rol', async () => {
    await abrir(
      resumenDe({
        pendientes: [
          cobroDe({ detalle: [], rango: { desde: 21, hasta: 40, precio: 45000 }, jugadores: 25, total: 45000 }),
        ],
      }),
    );

    expect(montaje.texto).toContain('Jugadores activos: 25');
    expect(montaje.texto).toContain('rango de 21 a 40 jugadores');
    expect(montaje.texto).toContain('Total: $ 45.000');
    expect(montaje.texto).not.toContain('×');
  });

  it('muestra el último pago registrado y el próximo cobro como estimación', async () => {
    await abrir(
      resumenDe({
        ultimoPago: cobroDe({
          periodo: '2026-10',
          estado: 'PAGADO',
          total: 40000,
          pagadoEn: '2026-10-05T15:00:00Z',
          origenDelPago: 'PASARELA',
          medioDePago: 'PSE',
        }),
        proximoCobro: { periodo: '2026-12', jugadores: 25, rango: { desde: 21, hasta: 40, precio: 45000 }, valorEstimado: 45000 },
      }),
    );

    expect(montaje.texto).toContain('Último pago registrado');
    expect(montaje.texto).toContain('octubre de 2026');
    expect(montaje.texto).toContain('PSE');
    expect(montaje.texto).toContain('Próximo cobro');
    expect(montaje.texto).toContain('diciembre de 2026 · estimado $ 45.000');
    expect(montaje.texto).toContain('Con tus 25 jugadores de hoy (21 a 40 jugadores)');
  });

  it('sin pagos ni próximo cobro lo dice', async () => {
    await abrir(resumenDe({ pendientes: [] }));

    expect(montaje.texto).toContain('Todavía no hay pagos registrados.');
    expect(montaje.texto).toContain('No hay pagos próximos.');
  });

  it('el botón Pagar pide el intento y abre la dirección de la pasarela', async () => {
    await abrir();
    const iniciar = vi
      .spyOn(api, 'post')
      .mockResolvedValue({ urlDePago: 'https://checkout.wompi.co/p/?reference=LFC-1', referencia: 'LFC-1' } as never);

    await montaje.clic(montaje.boton('Pagar')!);

    expect(iniciar).toHaveBeenCalledWith('/api/clubes/club/pago-plataforma/cobros/c1/intentos');
    expect(asignar).toHaveBeenCalledWith('https://checkout.wompi.co/p/?reference=LFC-1');
  });

  it('si no se pudo crear el intento muestra el error y no abre nada', async () => {
    await abrir();
    vi.spyOn(api, 'post').mockRejectedValue(new Error('falló'));

    await montaje.clic(montaje.boton('Pagar')!);

    expect(asignar).not.toHaveBeenCalled();
    expect(montaje.contenedor.querySelector('[role="alert"]')).not.toBeNull();
  });

  it('sin la pasarela configurada no hay botón y se dice por qué, sin error en el resto de la pantalla', async () => {
    await abrir(resumenDe({ pagoEnLineaDisponible: false }));

    expect(montaje.boton('Pagar')).toBeUndefined();
    expect(montaje.texto).toContain('El pago en línea no está disponible');
    expect(montaje.texto).toContain('Total: $ 45.000');
    expect(montaje.contenedor.querySelector('[role="alert"]')).toBeNull();
  });

  it('sin cobros pendientes dice que está al día', async () => {
    await abrir(resumenDe({ pendientes: [] }));

    expect(montaje.texto).toContain('Estás al día con la plataforma');
    expect(montaje.boton('Pagar')).toBeUndefined();
  });

  it('muestra «En verificación» cuando hay un intento reciente', async () => {
    await abrir(resumenDe({ pendientes: [cobroDe({ enVerificacion: true })] }));

    expect(montaje.texto).toContain('En verificación');
  });

  it('al volver a la aplicación recarga el cobro y libera el botón de pago', async () => {
    await abrir();
    vi.spyOn(api, 'post').mockResolvedValue({ urlDePago: 'https://checkout.wompi.co/p/?x=1', referencia: 'LFC-1' } as never);
    await montaje.clic(montaje.boton('Pagar')!);
    expect(montaje.texto).toContain('Abriendo la pasarela');

    // El pago se confirmó mientras la aplicación estaba en segundo plano.
    simularGet({ '/api/clubes/club/pago-plataforma': resumenDe({ pendientes: [], historial: [cobroDe({ estado: 'PAGADO', pagadoEn: '2026-11-05T15:00:00Z', origenDelPago: 'PASARELA' })] }) });
    await act(async () => {
      document.dispatchEvent(new Event('visibilitychange'));
    });

    expect(montaje.texto).toContain('Estás al día con la plataforma');
    expect(montaje.texto).not.toContain('Abriendo la pasarela');
  });

  it('si el último pago no fue aprobado lo dice en la tarjeta y deja volver a pagar', async () => {
    await abrir(resumenDe({ pendientes: [cobroDe({ ultimoPagoNoAprobado: true })] }));

    expect(montaje.texto).toContain('Tu último pago no fue aprobado');
    expect(montaje.texto).not.toContain('En verificación');
    expect(montaje.boton('Pagar')).toBeDefined();
  });

  it('el historial trae el comprobante de un cobro pagado y el motivo de uno anulado', async () => {
    await abrir(
      resumenDe({
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
    );

    expect(montaje.texto).toContain('✓ Pagado');
    expect(montaje.texto).toContain('Referencia: LFC-abc');
    expect(montaje.texto).toContain('Medio de pago: PSE');
    expect(montaje.texto).toContain('Cobro anulado: Cobro duplicado');
  });

  it('un club suspendido por impago lo dice arriba', async () => {
    await abrir(resumenDe({ suspendidoPorImpago: true }));

    expect(montaje.texto).toContain('Tu club está suspendido por falta de pago');
  });
});
