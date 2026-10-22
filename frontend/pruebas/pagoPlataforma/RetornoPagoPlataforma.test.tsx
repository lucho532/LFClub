import { act } from 'react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { api } from '../../src/compartido/api/cliente';
import { RetornoPagoPlataforma } from '../../src/privado/pagoPlataforma/RetornoPagoPlataforma';
import { Montaje, clubDe } from '../pagos/ayudas';
import { cobroDe } from './ayudasPagoPlataforma';

describe('RetornoPagoPlataforma', () => {
  let montaje: Montaje;

  beforeEach(() => {
    vi.useFakeTimers();
    montaje = new Montaje();
  });

  afterEach(() => {
    montaje.desmontar();
    vi.useRealTimers();
  });

  async function abrir(ruta = '/retorno?cobro=c1') {
    await montaje.pintar(clubDe('PRESIDENTE'), ruta, '/retorno', <RetornoPagoPlataforma />);
  }

  async function pasar(milisegundos: number) {
    await act(async () => {
      await vi.advanceTimersByTimeAsync(milisegundos);
    });
  }

  it('mientras el cobro sigue pendiente dice «En verificación» y vuelve a consultar cada 3 segundos', async () => {
    const consulta = vi.spyOn(api, 'get').mockResolvedValue(cobroDe() as never);

    await abrir();
    expect(montaje.texto).toContain('En verificación');
    expect(consulta).toHaveBeenCalledWith('/api/clubes/club/pago-plataforma/cobros/c1');
    expect(consulta).toHaveBeenCalledTimes(1);

    await pasar(3000);
    expect(consulta).toHaveBeenCalledTimes(2);
  });

  it('pasa a «Pago confirmado» con el comprobante cuando la consulta lo devuelve pagado', async () => {
    const consulta = vi.spyOn(api, 'get').mockResolvedValueOnce(cobroDe() as never);
    await abrir();

    consulta.mockResolvedValue(
      cobroDe({ estado: 'PAGADO', pagadoEn: '2026-11-05T15:00:00Z', origenDelPago: 'PASARELA', referencia: 'LFC-1' }) as never,
    );
    await pasar(3000);

    expect(montaje.texto).toContain('Pago confirmado');
    expect(montaje.texto).toContain('Referencia: LFC-1');
    await pasar(10000);
    expect(consulta).toHaveBeenCalledTimes(2);
  });

  it('pasados 30 segundos sin confirmación dice que se puede volver más tarde y deja de consultar', async () => {
    const consulta = vi.spyOn(api, 'get').mockResolvedValue(cobroDe() as never);
    await abrir();

    await pasar(31000);
    const llamadas = consulta.mock.calls.length;

    expect(montaje.texto).toContain('Todavía estamos verificando tu pago; puedes volver a esta pantalla más tarde');
    await pasar(10000);
    expect(consulta).toHaveBeenCalledTimes(llamadas);
  });

  it('nunca marca el cobro como pagado por sí sola: solo consulta, no envía nada', async () => {
    vi.spyOn(api, 'get').mockResolvedValue(cobroDe() as never);
    const enviar = vi.spyOn(api, 'post');
    const guardar = vi.spyOn(api, 'put');

    await abrir('/retorno?cobro=c1&transactionStatus=APPROVED&status=APPROVED');
    await pasar(31000);

    expect(enviar).not.toHaveBeenCalled();
    expect(guardar).not.toHaveBeenCalled();
    expect(montaje.texto).not.toContain('Pago confirmado');
  });

  it('si la pasarela ya rechazó el pago lo dice, ofrece intentar de nuevo y deja de consultar', async () => {
    const consulta = vi.spyOn(api, 'get').mockResolvedValue(cobroDe({ ultimoPagoNoAprobado: true }) as never);

    await abrir();
    await pasar(10000);

    expect(montaje.texto).toContain('Tu pago no fue aprobado. No se debitó ningún dinero');
    expect(montaje.texto).not.toContain('En verificación');
    expect(montaje.texto).toContain('Intentar de nuevo');
    expect(consulta).toHaveBeenCalledTimes(1);
  });

  it('sin el cobro en la dirección no consulta nada', async () => {
    const consulta = vi.spyOn(api, 'get');

    await abrir('/retorno');

    expect(consulta).not.toHaveBeenCalled();
    expect(montaje.texto).toContain('No encontramos el cobro');
  });
});
