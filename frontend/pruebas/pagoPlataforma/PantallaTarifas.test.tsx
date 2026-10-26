import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { api } from '../../src/compartido/api/cliente';
import { ErrorApi } from '../../src/compartido/api/errores';
import type { TarifaDeClubDto, TarifasDto } from '../../src/compartido/api/tiposPagoPlataforma';
import { PantallaTarifas } from '../../src/plataforma/PantallaTarifas';
import { Montaje, clubDe, simularGet } from '../pagos/ayudas';

const vigentes: TarifasDto = {
  rangos: [
    { desde: 1, hasta: 20, precio: 25000 },
    { desde: 21, hasta: 40, precio: 45000 },
    { desde: 41, hasta: null, precio: 75000 },
  ],
  diasDeGracia: 5,
  pasarelaConfigurada: true,
  actualizadoEn: '2026-10-21T15:00:00Z',
};

const porClub: TarifaDeClubDto[] = [
  { clubId: 'a', nombre: 'Valfor FC', jugadores: 25, rango: { desde: 21, hasta: 40, precio: 45000 }, valorMensual: 45000, mesDeInicioDelCobro: '2026-11' },
  { clubId: 'b', nombre: 'Club Nuevo', jugadores: 0, rango: null, valorMensual: 0, mesDeInicioDelCobro: null },
];

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
    simularGet({ '/api/plataforma/tarifas': vigentes, '/api/plataforma/tarifas/clubes': porClub });
    await montaje.pintar(clubDe('PRESIDENTE'), '/tarifas', '/tarifas', <PantallaTarifas />);
  });

  afterEach(() => montaje.desmontar());

  const lapiz = () => montaje.contenedor.querySelector('button[aria-label="Editar tarifas"]') as HTMLButtonElement;
  const formulario = () => montaje.contenedor.querySelector('dialog form') as HTMLFormElement;

  it('muestra los rangos y los días de gracia como tarjeta de solo lectura', () => {
    expect(montaje.texto).toContain('1 a 20 jugadores');
    expect(montaje.texto).toContain('$ 25.000 al mes');
    expect(montaje.texto).toContain('21 a 40 jugadores');
    expect(montaje.texto).toContain('41 jugadores o más');
    expect(montaje.texto).toContain('$ 75.000 al mes');
    expect(montaje.texto).toContain('Días de gracia');
    expect(montaje.texto).toContain('Pasarela de pago configurada');
    expect(montaje.contenedor.querySelector('input')).toBeNull();
  });

  it('muestra lo que le corresponde pagar a cada club', () => {
    expect(montaje.texto).toContain('Tarifa de cada club');
    expect(montaje.texto).toContain('Valfor FC');
    expect(montaje.texto).toContain('21 a 40 jugadores');
    expect(montaje.texto).toContain('$ 45.000');
    expect(montaje.texto).toContain('Club Nuevo');
    expect(montaje.texto).toContain('Sin rango');
    expect(montaje.texto).toContain('No se cobra');
  });

  it('sin rangos avisa que no se genera ningún cobro', async () => {
    montaje.desmontar();
    montaje = new Montaje();
    simularGet({ '/api/plataforma/tarifas': { ...vigentes, rangos: [] }, '/api/plataforma/tarifas/clubes': [] });
    await montaje.pintar(clubDe('PRESIDENTE'), '/tarifas', '/tarifas', <PantallaTarifas />);

    expect(montaje.texto).toContain('No hay rangos definidos');
  });

  it('al abrir la edición con el lápiz muestra cada rango con su límite y su precio y avisa que aplica hacia adelante', async () => {
    await montaje.clic(lapiz());

    expect(montaje.campo('Hasta (rango 1)')?.value).toBe('20');
    expect(montaje.campo('Precio mensual (rango 1)')?.value).toBe('25000');
    expect(montaje.campo('Hasta (rango 2)')?.value).toBe('40');
    expect(montaje.campo('Hasta (rango 3)')).toBeNull();
    expect(montaje.campo('Precio mensual (rango 3)')?.value).toBe('75000');
    expect(montaje.campo('Días de gracia')?.value).toBe('5');
    expect(montaje.texto).toContain('desde 21 jugadores');
    expect(montaje.texto).toContain('se generen desde ahora');
  });

  it('guarda los rangos, cierra la edición y confirma; cada rango empieza donde termina el anterior', async () => {
    const guardar = vi.spyOn(api, 'put').mockResolvedValue({
      ...vigentes,
      rangos: [
        { desde: 1, hasta: 30, precio: 30000 },
        { desde: 31, hasta: null, precio: 75000 },
      ],
    } as never);
    await montaje.clic(lapiz());
    await montaje.clic(montaje.boton('Quitar rango 3')!);

    await montaje.escribir(montaje.campo('Hasta (rango 1)')!, '30');
    await montaje.escribir(montaje.campo('Precio mensual (rango 1)')!, '30.000');
    await montaje.escribir(montaje.campo('Precio mensual (rango 2)')!, '75000');
    await montaje.enviar(formulario());

    expect(guardar).toHaveBeenCalledWith('/api/plataforma/tarifas', {
      rangos: [
        { desde: 1, hasta: 30, precio: 30000 },
        { desde: 31, hasta: null, precio: 75000 },
      ],
      diasDeGracia: 5,
    });
    expect(montaje.contenedor.querySelector('dialog form')).toBeNull();
    expect(montaje.texto).toContain('Tarifas guardadas.');
    expect(montaje.texto).toContain('1 a 30 jugadores');
  });

  it('permite agregar un rango y usar los rangos sugeridos', async () => {
    await montaje.clic(lapiz());

    await montaje.clic(montaje.boton('Agregar rango')!);
    expect(montaje.campo('Precio mensual (rango 4)')).not.toBeNull();

    await montaje.clic(montaje.boton('Usar rangos sugeridos')!);
    expect(montaje.campo('Precio mensual (rango 5)')).toBeNull();
    expect(montaje.campo('Precio mensual (rango 4)')?.value).toBe('110000');
    expect(montaje.campo('Hasta (rango 1)')?.value).toBe('20');
  });

  it('no envía nada si un límite no es un entero mayor que donde empieza, un precio no es un entero o los días se pasan de 30', async () => {
    const guardar = vi.spyOn(api, 'put');
    await montaje.clic(lapiz());

    await montaje.escribir(montaje.campo('Hasta (rango 2)')!, '15');
    await montaje.escribir(montaje.campo('Precio mensual (rango 1)')!, '12,5');
    await montaje.escribir(montaje.campo('Días de gracia')!, '45');
    await montaje.enviar(formulario());

    expect(guardar).not.toHaveBeenCalled();
    expect(montaje.texto).toContain('Escribe hasta cuántos jugadores llega');
    expect(montaje.texto).toContain('Escribe un valor entero de pesos');
    expect(montaje.texto).toContain('Los días de gracia van de 0 a 30.');
  });

  it('muestra el error de validación que devuelve la API y deja abierta la edición', async () => {
    vi.spyOn(api, 'put').mockRejectedValue(
      new ErrorApi({
        title: 'Revisa los datos marcados.',
        status: 400,
        codigo: 'datos_invalidos',
        errores: { rangos: ['El precio del rango 2 no puede ser negativo.'] },
      }),
    );
    await montaje.clic(lapiz());

    await montaje.enviar(formulario());

    expect(montaje.texto).toContain('El precio del rango 2 no puede ser negativo.');
    expect(formulario()).not.toBeNull();
  });
});
