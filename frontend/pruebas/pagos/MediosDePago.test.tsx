import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { api } from '../../src/compartido/api/cliente';
import type { CuentaJugadorDto, MedioPagoDto } from '../../src/compartido/api/tiposPagos';
import { ComoPagar } from '../../src/privado/pagos/ComoPagar';
import { CuentaDelJugador } from '../../src/privado/pagos/CuentaDelJugador';
import { PantallaMediosDePago } from '../../src/privado/pagos/PantallaMediosDePago';
import { Montaje, clubDe, simularGet } from './ayudas';

const nequi: MedioPagoDto = {
  id: 'm1',
  entidad: 'Nequi',
  titular: 'Club Valfor',
  numero: '3001234567',
  nota: 'Poner el nombre del jugador',
  activo: true,
};

describe('PantallaMediosDePago', () => {
  let montaje: Montaje;

  beforeEach(() => {
    montaje = new Montaje();
  });

  afterEach(() => montaje.desmontar());

  async function abrir(rol: 'PRESIDENTE' | 'DIRECTIVO', medios: MedioPagoDto[] = [nequi]) {
    simularGet({ '/api/clubes/club/medios-de-pago': medios });
    await montaje.pintar(clubDe(rol), '/m', '/m', <PantallaMediosDePago />);
  }

  it('lista los medios con su entidad, número, titular y si se ven', async () => {
    await abrir('PRESIDENTE', [nequi, { ...nequi, id: 'm2', entidad: 'Bancolombia', activo: false }]);

    expect(montaje.texto).toContain('Nequi');
    expect(montaje.texto).toContain('3001234567 · Club Valfor');
    expect(montaje.texto).toContain('Poner el nombre del jugador');
    expect(montaje.texto).toContain('Visible');
    expect(montaje.texto).toContain('Oculto');
  });

  it('el directivo solo consulta', async () => {
    await abrir('DIRECTIVO');

    expect(montaje.boton('Añadir medio')).toBeUndefined();
    expect(montaje.boton('Editar')).toBeUndefined();
    expect(montaje.boton('Quitar')).toBeUndefined();
  });

  it('el presidente añade un medio y la lista se vuelve a pedir', async () => {
    await abrir('PRESIDENTE', []);
    const crear = vi.spyOn(api, 'post').mockResolvedValue(nequi as never);

    await montaje.clic(montaje.boton('Añadir medio')!);
    await montaje.escribir(montaje.campo('Entidad')!, 'Nequi');
    await montaje.escribir(montaje.campo('Titular')!, 'Club Valfor');
    await montaje.escribir(montaje.campo('Número de cuenta')!, '3001234567');
    await montaje.enviar(montaje.contenedor.querySelector('form')!);

    expect(crear).toHaveBeenCalledWith('/api/clubes/club/medios-de-pago', {
      entidad: 'Nequi',
      titular: 'Club Valfor',
      numero: '3001234567',
      nota: null,
      activo: true,
    });
    expect(vi.mocked(api.get).mock.calls.length).toBeGreaterThanOrEqual(2);
  });

  it('el presidente edita y desactiva un medio', async () => {
    await abrir('PRESIDENTE');
    const editar = vi.spyOn(api, 'put').mockResolvedValue(nequi as never);

    await montaje.clic(montaje.boton('Editar')!);
    await montaje.clic(montaje.contenedor.querySelector('input[type="checkbox"]')!);
    await montaje.enviar(montaje.contenedor.querySelector('form')!);

    expect(editar).toHaveBeenCalledWith('/api/clubes/club/medios-de-pago/m1', expect.objectContaining({ activo: false }));
  });

  it('el presidente quita un medio', async () => {
    await abrir('PRESIDENTE');
    const quitar = vi.spyOn(api, 'delete').mockResolvedValue(undefined as never);

    await montaje.clic(montaje.boton('Quitar')!);

    expect(quitar).toHaveBeenCalledWith('/api/clubes/club/medios-de-pago/m1');
  });
});

describe('Cómo pagar', () => {
  let montaje: Montaje;

  beforeEach(() => {
    montaje = new Montaje();
  });

  afterEach(() => montaje.desmontar());

  it('muestra las cuentas activas y la indicación de informar el pago', async () => {
    await montaje.pintar(clubDe('JUGADOR'), '/c', '/c', <ComoPagar medios={[nequi]} />);

    expect(montaje.texto).toContain('Cómo pagar');
    expect(montaje.texto).toContain('3001234567 · Club Valfor');
    expect(montaje.texto).toContain('informe el pago con el comprobante');
  });

  it('sin medios dice que consulte al presidente', async () => {
    await montaje.pintar(clubDe('JUGADOR'), '/c', '/c', <ComoPagar medios={[]} />);

    expect(montaje.texto).toContain('Consulte al presidente del club');
  });

  it('aparece en la cuenta de la familia y no en la del equipo del club', async () => {
    const cuenta: CuentaJugadorDto = {
      usuarioRolId: 'ana',
      nombreCompleto: 'Ana Arango',
      estado: 'DEBE',
      saldo: 65000,
      cargos: [],
      pagos: [],
      mediosPago: [nequi],
      puedeRegistrar: false,
      puedeInformar: true,
    };
    simularGet({ '/api/clubes/club/jugadores/ana/cuenta': cuenta });
    await montaje.pintar(clubDe('JUGADOR', 'ana'), '/c/ana', '/c/:usuarioRolId', <CuentaDelJugador />);
    expect(montaje.texto).toContain('Cómo pagar');
    montaje.desmontar();

    montaje = new Montaje();
    simularGet({ '/api/clubes/club/jugadores/ana/cuenta': { ...cuenta, puedeInformar: false } });
    await montaje.pintar(clubDe('PRESIDENTE'), '/c/ana', '/c/:usuarioRolId', <CuentaDelJugador />);
    expect(montaje.texto).not.toContain('Cómo pagar');
  });
});
