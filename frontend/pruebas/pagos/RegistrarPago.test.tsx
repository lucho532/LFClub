import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { api } from '../../src/compartido/api/cliente';
import { ErrorApi } from '../../src/compartido/api/errores';
import type { CuentaJugadorDto } from '../../src/compartido/api/tiposPagos';
import { CuentaDelJugador } from '../../src/privado/pagos/CuentaDelJugador';
import { Montaje, clubDe, simularGet } from './ayudas';

const cuenta: CuentaJugadorDto = {
  usuarioRolId: 'ana',
  nombreCompleto: 'Ana Arango',
  estado: 'DEBE',
  saldo: 130000,
  cargos: [
    { id: 'oct', tipo: 'MENSUALIDAD', mes: '2026-10-01', valor: 65000, pagado: 0, pendiente: 65000 },
    { id: 'sep', tipo: 'MENSUALIDAD', mes: '2026-09-01', valor: 65000, pagado: 0, pendiente: 65000 },
  ],
  pagos: [],
  mediosPago: [],
  puedeRegistrar: true,
  puedeInformar: false,
};

describe('RegistrarPago', () => {
  let montaje: Montaje;

  beforeEach(() => {
    montaje = new Montaje();
  });

  afterEach(() => montaje.desmontar());

  async function abrir(datos: CuentaJugadorDto = cuenta, rol: 'PRESIDENTE' | 'DIRECTIVO' = 'PRESIDENTE') {
    simularGet({ '/api/clubes/club/jugadores/ana/cuenta': datos });
    await montaje.pintar(clubDe(rol), '/c/ana', '/c/:usuarioRolId', <CuentaDelJugador />);
  }

  async function abrirFormulario() {
    await abrir();
    await montaje.clic(montaje.boton('Registrar pago')!);
  }

  it('el botón solo existe si la API dice que puede registrar', async () => {
    await abrir({ ...cuenta, puedeRegistrar: false }, 'DIRECTIVO');

    expect(montaje.boton('Registrar pago')).toBeUndefined();
  });

  it('no ofrece registrar cuando no hay nada pendiente', async () => {
    await abrir({ ...cuenta, estado: 'AL_DIA', saldo: 0, cargos: [] });

    expect(montaje.boton('Registrar pago')).toBeUndefined();
  });

  it('muestra lo pendiente y la entidad solo se pide en una transferencia', async () => {
    await abrirFormulario();

    expect(montaje.texto).toContain('Pendiente: $ 130.000');
    expect(montaje.campo('Entidad')).toBeNull();

    await montaje.escribir(montaje.campo('Medio de pago')!, 'TRANSFERENCIA');

    expect(montaje.campo('Entidad')).not.toBeNull();
  });

  it('rechaza más de lo que se debe sin llamar a la API', async () => {
    await abrirFormulario();
    const registrar = vi.spyOn(api, 'post');

    await montaje.escribir(montaje.campo('Valor recibido')!, '200000');
    await montaje.enviar(montaje.contenedor.querySelectorAll('form')[0]);

    expect(montaje.texto).toContain('El pago supera lo que se debe: pendiente $ 130.000');
    expect(registrar).not.toHaveBeenCalled();
  });

  it('con cargos elegidos el tope es lo pendiente de esos cargos', async () => {
    await abrirFormulario();
    const registrar = vi.spyOn(api, 'post');
    const casillas = [...montaje.contenedor.querySelectorAll('input[type="checkbox"]')] as HTMLInputElement[];

    await montaje.clic(casillas[0]);
    await montaje.escribir(montaje.campo('Valor recibido')!, '90000');
    await montaje.enviar(montaje.contenedor.querySelectorAll('form')[0]);

    expect(montaje.texto).toContain('pendiente $ 65.000');
    expect(registrar).not.toHaveBeenCalled();
  });

  it('registra el pago, refresca la cuenta y muestra el recibo', async () => {
    await abrirFormulario();
    const registrar = vi.spyOn(api, 'post').mockResolvedValue({
      id: 'p1',
      valor: 65000,
      numeroRecibo: 7,
    } as never);

    await montaje.escribir(montaje.campo('Valor recibido')!, '65.000');
    await montaje.escribir(montaje.campo('Referencia (opcional)')!, 'R-1');
    await montaje.enviar(montaje.contenedor.querySelectorAll('form')[0]);

    const [ruta, cuerpo] = registrar.mock.calls[0];
    expect(ruta).toBe('/api/clubes/club/jugadores/ana/pagos');
    expect(cuerpo).toMatchObject({ valor: 65000, medio: 'EFECTIVO', entidad: null, referencia: 'R-1', cargoIds: null });
    expect(montaje.texto).toContain('Recibo n.º 7');
    expect(montaje.boton('Registrar pago')).toBeDefined();
  });

  it('muestra el mensaje de la API cuando rechaza el pago', async () => {
    await abrirFormulario();
    vi.spyOn(api, 'post').mockRejectedValue(
      new ErrorApi({ title: 'El pago supera lo que se debe: pendiente $ 5.000.', status: 409, codigo: 'pago_excede_deuda' }),
    );

    await montaje.escribir(montaje.campo('Valor recibido')!, '10000');
    await montaje.enviar(montaje.contenedor.querySelectorAll('form')[0]);

    expect(montaje.texto).toContain('El pago supera lo que se debe: pendiente $ 5.000.');
  });
});
