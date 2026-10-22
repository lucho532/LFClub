import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { api } from '../../src/compartido/api/cliente';
import { ErrorApi } from '../../src/compartido/api/errores';
import type { TarifasDto } from '../../src/compartido/api/tiposPagoPlataforma';
import { PantallaTarifas } from '../../src/plataforma/PantallaTarifas';
import { Montaje, clubDe, simularGet } from '../pagos/ayudas';

const vigentes: TarifasDto = {
  precioJugador: 1500,
  precioEntrenador: null,
  precioDirectivo: 3000,
  precioPresidente: 0,
  diasDeGracia: 5,
  pasarelaConfigurada: true,
  actualizadoEn: '2026-10-21T15:00:00Z',
};

describe('PantallaTarifas', () => {
  let montaje: Montaje;

  beforeEach(async () => {
    // jsdom no implementa el diálogo modal: se simula lo mínimo.
    HTMLDialogElement.prototype.showModal ??= function showModal(this: HTMLDialogElement) {
      this.setAttribute('open', '');
    };
    HTMLDialogElement.prototype.close ??= function close(this: HTMLDialogElement) {
      this.removeAttribute('open');
    };
    montaje = new Montaje();
    simularGet({ '/api/plataforma/tarifas': vigentes });
    await montaje.pintar(clubDe('PRESIDENTE'), '/tarifas', '/tarifas', <PantallaTarifas />);
  });

  afterEach(() => montaje.desmontar());

  const lapiz = () => montaje.contenedor.querySelector('button[aria-label="Editar tarifas"]') as HTMLButtonElement;
  const formulario = () => montaje.contenedor.querySelector('dialog form') as HTMLFormElement;

  it('muestra los precios y los días de gracia como tarjeta de solo lectura', () => {
    expect(montaje.texto).toContain('$ 1.500');
    expect(montaje.texto).toContain('Sin definir');
    expect(montaje.texto).toContain('$ 3.000');
    expect(montaje.texto).toContain('$ 0');
    expect(montaje.texto).toContain('Días de gracia');
    expect(montaje.texto).toContain('Pasarela de pago configurada');
    expect(montaje.contenedor.querySelector('input')).toBeNull();
  });

  it('avisa que los cambios aplican hacia adelante al abrir la edición con el lápiz', async () => {
    await montaje.clic(lapiz());

    expect(montaje.campo('Precio por jugador')?.value).toBe('1500');
    expect(montaje.campo('Precio por entrenador')?.value).toBe('');
    expect(montaje.campo('Días de gracia')?.value).toBe('5');
    expect(montaje.texto).toContain('se generen desde ahora');
  });

  it('guarda los precios, cierra la edición y confirma; un campo vacío va como nulo y 0 como 0', async () => {
    const guardar = vi
      .spyOn(api, 'put')
      .mockResolvedValue({ ...vigentes, precioJugador: 2000, actualizadoEn: '2026-10-22T10:00:00Z' } as never);
    await montaje.clic(lapiz());

    await montaje.escribir(montaje.campo('Precio por jugador')!, '2.000');
    await montaje.enviar(formulario());

    expect(guardar).toHaveBeenCalledWith('/api/plataforma/tarifas', {
      precioJugador: 2000,
      precioEntrenador: null,
      precioDirectivo: 3000,
      precioPresidente: 0,
      diasDeGracia: 5,
    });
    expect(montaje.contenedor.querySelector('dialog form')).toBeNull();
    expect(montaje.texto).toContain('Tarifas guardadas.');
    expect(montaje.texto).toContain('$ 2.000');
  });

  it('no envía nada si un precio no es un entero de pesos o los días se pasan de 30', async () => {
    const guardar = vi.spyOn(api, 'put');
    await montaje.clic(lapiz());

    await montaje.escribir(montaje.campo('Precio por jugador')!, '12,5');
    await montaje.escribir(montaje.campo('Días de gracia')!, '45');
    await montaje.enviar(formulario());

    expect(guardar).not.toHaveBeenCalled();
    expect(montaje.texto).toContain('Escribe un valor entero de pesos');
    expect(montaje.texto).toContain('Los días de gracia van de 0 a 30.');
  });

  it('muestra el error de validación que devuelve la API y deja abierta la edición', async () => {
    vi.spyOn(api, 'put').mockRejectedValue(
      new ErrorApi({
        title: 'Revisa los datos marcados.',
        status: 400,
        codigo: 'datos_invalidos',
        errores: { precioDirectivo: ['El precio por directivo no puede ser negativo.'] },
      }),
    );
    await montaje.clic(lapiz());

    await montaje.enviar(formulario());

    expect(montaje.texto).toContain('El precio por directivo no puede ser negativo.');
    expect(formulario()).not.toBeNull();
  });
});
