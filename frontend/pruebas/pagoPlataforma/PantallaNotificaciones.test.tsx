import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { api } from '../../src/compartido/api/cliente';
import type { NotificacionPasarelaDto } from '../../src/compartido/api/tiposPagoPlataforma';
import { PantallaNotificaciones } from '../../src/plataforma/PantallaNotificaciones';
import { Montaje, clubDe } from '../pagos/ayudas';

const notificacion = (cambios: Partial<NotificacionPasarelaDto>): NotificacionPasarelaDto => ({
  notificacionId: 'n1',
  recibidaEn: '2026-11-05T15:00:00Z',
  resultado: 'APLICADA',
  transaccionId: 'tx-1',
  estadoDeLaPasarela: 'APPROVED',
  referencia: 'LFC-1',
  valor: 45000,
  clubId: 'club',
  periodo: '2026-11',
  ...cambios,
});

describe('PantallaNotificaciones', () => {
  let montaje: Montaje;
  let pedidos: string[];

  beforeEach(async () => {
    montaje = new Montaje();
    pedidos = [];
    vi.spyOn(api, 'get').mockImplementation(async (ruta: string) => {
      pedidos.push(ruta);
      return (ruta.includes('PAGO_DUPLICADO')
        ? [notificacion({ notificacionId: 'n2', resultado: 'PAGO_DUPLICADO', transaccionId: 'tx-2' })]
        : [notificacion({}), notificacion({ notificacionId: 'n2', resultado: 'PAGO_DUPLICADO', transaccionId: 'tx-2' })]) as never;
    });
    await montaje.pintar(clubDe('DESARROLLADOR'), '/n', '/n', <PantallaNotificaciones />);
  });

  afterEach(() => montaje.desmontar());

  it('lista las notificaciones y resalta los casos que hay que revisar', () => {
    expect(pedidos).toEqual(['/api/plataforma/notificaciones']);
    expect(montaje.texto).toContain('Aplicada');
    expect(montaje.texto).toContain('⚠ Pago duplicado');
    expect(montaje.texto).toContain('LFC-1');
    expect(montaje.texto).toContain('$ 45.000');
  });

  it('filtra por resultado pidiéndolo a la API', async () => {
    await montaje.escribir(montaje.campo('Filtrar por resultado')!, 'PAGO_DUPLICADO');

    expect(pedidos.at(-1)).toBe('/api/plataforma/notificaciones?resultado=PAGO_DUPLICADO');
    expect(montaje.contenedor.querySelectorAll('tbody tr')).toHaveLength(1);
    expect(montaje.texto).toContain('⚠ Pago duplicado');
  });
});
