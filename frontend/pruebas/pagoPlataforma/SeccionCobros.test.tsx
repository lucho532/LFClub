import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { api } from '../../src/compartido/api/cliente';
import type { ClubDetalleDto } from '../../src/compartido/api/tipos';
import { SeccionCobros } from '../../src/plataforma/SeccionCobros';
import { Montaje, clubDe, simularGet } from '../pagos/ayudas';
import { cobroDe } from './ayudasPagoPlataforma';

const club = { clubId: 'club', nombre: 'Club de prueba' } as ClubDetalleDto;

describe('SeccionCobros', () => {
  let montaje: Montaje;
  const recargarElClub = vi.fn();

  beforeEach(() => {
    // jsdom no implementa el diálogo modal: se simula lo mínimo.
    HTMLDialogElement.prototype.showModal ??= function showModal(this: HTMLDialogElement) {
      this.setAttribute('open', '');
    };
    HTMLDialogElement.prototype.close ??= function close(this: HTMLDialogElement) {
      this.removeAttribute('open');
    };
    recargarElClub.mockReset();
    montaje = new Montaje();
  });

  afterEach(() => montaje.desmontar());

  async function abrir(cobros = [cobroDe()]) {
    simularGet({ '/api/plataforma/clubes/club/cobros': cobros });
    await montaje.pintar(
      clubDe('PRESIDENTE'),
      '/c',
      '/c',
      <SeccionCobros club={club} alActualizar={() => undefined} recargar={recargarElClub} />,
    );
  }

  it('lista los cobros con su detalle y sus acciones cuando están pendientes', async () => {
    await abrir([cobroDe(), cobroDe({ cobroId: 'c0', periodo: '2026-10', estado: 'PAGADO', pagadoEn: '2026-10-05T15:00:00Z', origenDelPago: 'MANUAL' })]);

    expect(montaje.texto).toContain('noviembre de 2026');
    expect(montaje.texto).toContain('octubre de 2026');
    expect(montaje.texto).toContain('Origen: Registrado por la plataforma');
    expect(montaje.contenedor.querySelectorAll('button').length).toBeGreaterThanOrEqual(2);
    expect(montaje.boton('Marcar pagado')).toBeDefined();
  });

  it('sin cobros lo dice', async () => {
    await abrir([]);

    expect(montaje.texto).toContain('todavía no tiene cobros');
  });

  it('marcar pagado pide confirmación y no envía nada hasta confirmar', async () => {
    await abrir();
    const publicar = vi.spyOn(api, 'post').mockResolvedValue({} as never);

    await montaje.clic(montaje.boton('Marcar pagado')!);
    expect(publicar).not.toHaveBeenCalled();
    expect(montaje.texto).toContain('hecho por fuera de la pasarela');

    const confirmar = [...montaje.contenedor.querySelectorAll('dialog button')].find(
      (b) => b.textContent === 'Marcar pagado',
    ) as HTMLButtonElement;
    await montaje.clic(confirmar);

    expect(publicar).toHaveBeenCalledWith('/api/plataforma/clubes/club/cobros/c1/pago-manual');
    expect(recargarElClub).toHaveBeenCalled();
  });

  it('anular exige un motivo de al menos 3 caracteres', async () => {
    await abrir();
    const publicar = vi.spyOn(api, 'post').mockResolvedValue({} as never);

    await montaje.clic(montaje.boton('Anular')!);
    const confirmar = [...montaje.contenedor.querySelectorAll('dialog button')].find(
      (b) => b.textContent === 'Anular cobro',
    ) as HTMLButtonElement;
    expect(confirmar.disabled).toBe(true);

    await montaje.escribir(montaje.campo('Motivo de la anulación')!, 'Club piloto');
    expect(confirmar.disabled).toBe(false);
    await montaje.clic(confirmar);

    expect(publicar).toHaveBeenCalledWith('/api/plataforma/clubes/club/cobros/c1/anulacion', { motivo: 'Club piloto' });
  });
});
