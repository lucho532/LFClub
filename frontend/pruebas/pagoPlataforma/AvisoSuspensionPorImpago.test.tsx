import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { api } from '../../src/compartido/api/cliente';
import { AvisoSuspensionPorImpago } from '../../src/privado/pagoPlataforma/AvisoSuspensionPorImpago';
import { AvisoPagoPlataforma } from '../../src/privado/pagoPlataforma/AvisoPagoPlataforma';
import { Montaje, clubDe } from '../pagos/ayudas';
import { resumenDe } from './ayudasPagoPlataforma';

describe('AvisoSuspensionPorImpago', () => {
  let montaje: Montaje;

  beforeEach(() => {
    montaje = new Montaje();
  });

  afterEach(() => montaje.desmontar());

  it('dice que el club está suspendido por falta de pago, el valor y lleva a pagar', async () => {
    await montaje.pintar(
      clubDe('PRESIDENTE'),
      '/i',
      '/i',
      <AvisoSuspensionPorImpago clubId="club" totalPendiente={45000} />,
    );

    expect(montaje.texto).toContain('Tu club está suspendido por falta de pago: debes $ 45.000');
    const enlace = [...montaje.contenedor.querySelectorAll('a')].find((a) => a.textContent === 'Pagar ahora');
    expect(enlace?.getAttribute('href')).toBe('/club/club/pago-plataforma');
    expect(montaje.contenedor.querySelector('[role="alert"]')).not.toBeNull();
  });

  it('en el inicio no aparece mientras el resumen no diga que está suspendido por impago', async () => {
    vi.spyOn(api, 'get').mockResolvedValue(resumenDe({ suspendidoPorImpago: false }) as never);
    await montaje.pintar(clubDe('PRESIDENTE'), '/i', '/i', <AvisoPagoPlataforma clubId="club" />);

    expect(montaje.texto).not.toContain('suspendido por falta de pago');
  });

  it('en el inicio aparece cuando el resumen dice que está suspendido por impago', async () => {
    vi.spyOn(api, 'get').mockResolvedValue(resumenDe({ suspendidoPorImpago: true }) as never);
    await montaje.pintar(clubDe('PRESIDENTE'), '/i', '/i', <AvisoPagoPlataforma clubId="club" />);

    expect(montaje.texto).toContain('suspendido por falta de pago');
  });
});
