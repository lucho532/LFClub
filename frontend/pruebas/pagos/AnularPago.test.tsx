import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { api } from '../../src/compartido/api/cliente';
import type { PagoDto } from '../../src/compartido/api/tiposPagos';
import { DetalleDePago } from '../../src/privado/pagos/DetalleDePago';
import { Montaje, clubDe, simularGet } from './ayudas';

const confirmado: PagoDto = {
  id: 'p1',
  usuarioRolId: 'ana',
  jugadorNombre: 'Ana Arango',
  estado: 'CONFIRMADO',
  valor: 65000,
  valorInformado: null,
  medio: 'TRANSFERENCIA',
  entidad: 'Nequi',
  referencia: null,
  fecha: '2026-10-13',
  concepto: 'Mensualidad octubre de 2026',
  numeroRecibo: 12,
  saldoTrasPago: 0,
  tieneSoporte: true,
  tieneResguardo: true,
  creadoEn: '2026-10-13T15:00:00Z',
  creadoPorNombre: 'Pat Presidente',
  confirmadoEn: '2026-10-13T15:00:00Z',
  confirmadoPorNombre: 'Pat Presidente',
  rechazadoEn: null,
  rechazadoPorNombre: null,
  motivoRechazo: null,
  anuladoEn: null,
  anuladoPorNombre: null,
  motivoAnulacion: null,
};

describe('AnularPago', () => {
  let montaje: Montaje;

  beforeEach(() => {
    // jsdom no implementa el diálogo modal: se simula lo mínimo.
    HTMLDialogElement.prototype.showModal ??= function showModal(this: HTMLDialogElement) {
      this.setAttribute('open', '');
    };
    HTMLDialogElement.prototype.close ??= function close(this: HTMLDialogElement) {
      this.removeAttribute('open');
    };
    montaje = new Montaje();
    // El recibo también pide su resguardo: aquí no importa la imagen.
    vi.stubGlobal('URL', Object.assign(URL, { createObjectURL: vi.fn(() => 'blob:r'), revokeObjectURL: vi.fn() }));
    vi.spyOn(api, 'getArchivo').mockResolvedValue(new Blob(['png'], { type: 'image/png' }));
  });

  afterEach(() => {
    montaje.desmontar();
    vi.unstubAllGlobals();
  });

  async function abrir(rol: 'PRESIDENTE' | 'DIRECTIVO' | 'JUGADOR', pago: PagoDto = confirmado) {
    simularGet({ '/api/clubes/club/pagos/p1': pago });
    await montaje.pintar(clubDe(rol, 'ana'), '/r/p1', '/r/:pagoId', <DetalleDePago />);
  }

  it('muestra el recibo con su número, el jugador, el valor, lo que cubre y el saldo tras el pago', async () => {
    await abrir('JUGADOR');

    expect(montaje.texto).toContain('Recibo n.º 12');
    expect(montaje.texto).toContain('Ana Arango');
    expect(montaje.texto).toContain('$ 65.000');
    expect(montaje.texto).toContain('Transferencia (Nequi)');
    expect(montaje.texto).toContain('Mensualidad octubre de 2026');
    expect(montaje.texto).toContain('Confirmado por');
    expect(montaje.texto).toContain('Al día');
  });

  it('solo el presidente ve el botón Anular', async () => {
    await abrir('PRESIDENTE');
    expect(montaje.boton('Anular')).toBeDefined();
    montaje.desmontar();

    for (const rol of ['DIRECTIVO', 'JUGADOR'] as const) {
      montaje = new Montaje();
      await abrir(rol);
      expect(montaje.boton('Anular')).toBeUndefined();
      montaje.desmontar();
    }

    montaje = new Montaje();
  });

  it('no se puede anular un pago que no está confirmado', async () => {
    await abrir('PRESIDENTE', { ...confirmado, estado: 'ANULADO', anuladoPorNombre: 'Pat', motivoAnulacion: 'Error' });

    expect(montaje.boton('Anular')).toBeUndefined();
    expect(montaje.texto).toContain('Pago anulado por Pat');
    expect(montaje.texto).toContain('Error');
  });

  it('exige un motivo y marca el pago como anulado', async () => {
    await abrir('PRESIDENTE');
    const anular = vi.spyOn(api, 'post').mockResolvedValue({
      ...confirmado,
      estado: 'ANULADO',
      anuladoPorNombre: 'Pat Presidente',
      motivoAnulacion: 'Me equivoqué',
    } as never);

    await montaje.clic(montaje.boton('Anular')!);
    expect(montaje.boton('Anular pago')!.disabled).toBe(true);

    await montaje.escribir(montaje.campo('Motivo de la anulación')!, 'Me equivoqué');
    expect(montaje.boton('Anular pago')!.disabled).toBe(false);
    await montaje.enviar(montaje.contenedor.querySelector('dialog form')!);

    expect(anular).toHaveBeenCalledWith('/api/clubes/club/pagos/p1/anulacion', { motivo: 'Me equivoqué' });
    expect(montaje.texto).toContain('Pago anulado por Pat Presidente');
    expect(montaje.texto).toContain('Anulado');
  });
});
