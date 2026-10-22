import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { ErrorApi } from '../../src/compartido/api/errores';
import type { CuentaJugadorDto, PagoDto } from '../../src/compartido/api/tiposPagos';
import { CuentaDelJugador } from '../../src/privado/pagos/CuentaDelJugador';
import { puedeVerPagos } from '../../src/privado/pagos/textosPagos';
import { Montaje, clubDe, simularGet } from './ayudas';
import { vi } from 'vitest';
import { api } from '../../src/compartido/api/cliente';

function pago(extra: Partial<PagoDto>): PagoDto {
  return {
    id: 'p1',
    usuarioRolId: 'ana',
    jugadorNombre: 'Ana Arango',
    estado: 'CONFIRMADO',
    valor: 90000,
    valorInformado: null,
    medio: 'EFECTIVO',
    entidad: null,
    referencia: null,
    fecha: '2026-10-13',
    concepto: null,
    numeroRecibo: 1,
    saldoTrasPago: 40000,
    tieneSoporte: false,
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
    ...extra,
  };
}

const cuenta: CuentaJugadorDto = {
  usuarioRolId: 'ana',
  nombreCompleto: 'Ana Arango',
  estado: 'PAGO_PARCIAL',
  saldo: 40000,
  cargos: [
    { id: 'c2', tipo: 'MENSUALIDAD', mes: '2026-10-01', valor: 65000, pagado: 25000, pendiente: 40000 },
    { id: 'c1', tipo: 'MENSUALIDAD', mes: '2026-09-01', valor: 65000, pagado: 65000, pendiente: 0 },
  ],
  pagos: [
    pago({}),
    pago({
      id: 'p2',
      estado: 'RECHAZADO',
      valor: 65000,
      medio: 'TRANSFERENCIA',
      entidad: 'Nequi',
      numeroRecibo: null,
      motivoRechazo: 'La imagen no se ve',
      tieneResguardo: false,
    }),
  ],
  mediosPago: [],
  puedeRegistrar: false,
  puedeInformar: true,
};

describe('CuentaDelJugador', () => {
  let montaje: Montaje;

  beforeEach(() => {
    montaje = new Montaje();
  });

  afterEach(() => montaje.desmontar());

  async function abrir(rol: 'PRESIDENTE' | 'DIRECTIVO' | 'JUGADOR', datos: CuentaJugadorDto = cuenta) {
    simularGet({ '/api/clubes/club/jugadores/ana/cuenta': datos });
    await montaje.pintar(clubDe(rol, 'ana'), '/cuenta/ana', '/cuenta/:usuarioRolId', <CuentaDelJugador />);
  }

  it('muestra el estado con texto, el saldo, los cargos y los pagos', async () => {
    await abrir('JUGADOR');

    expect(montaje.texto).toContain('Pago parcial');
    expect(montaje.texto).toContain('$ 40.000');
    expect(montaje.texto).toContain('Mensualidad octubre de 2026');
    expect(montaje.texto).toContain('Mensualidad septiembre de 2026');
    expect(montaje.texto).toContain('Pagado');
    expect(montaje.texto).toContain('Confirmado');
    expect(montaje.texto).toContain('Rechazado');
  });

  it('el rechazo se ve en la cuenta con su motivo', async () => {
    await abrir('JUGADOR');

    expect(montaje.texto).toContain('Motivo del rechazo: La imagen no se ve');
  });

  it('un pago anulado sigue a la vista, marcado', async () => {
    await abrir('PRESIDENTE', {
      ...cuenta,
      pagos: [pago({ estado: 'ANULADO', anuladoPorNombre: 'Pat Presidente', motivoAnulacion: 'Se equivocó' })],
    });

    expect(montaje.texto).toContain('Anulado');
    expect(montaje.texto).toContain('Se equivocó');
    expect(montaje.contenedor.querySelector('.pago-anulado')).not.toBeNull();
  });

  it('un jugador al día lo dice con texto', async () => {
    await abrir('DIRECTIVO', { ...cuenta, estado: 'AL_DIA', saldo: 0, cargos: [], pagos: [] });

    expect(montaje.texto).toContain('Al día');
    expect(montaje.texto).toContain('No debe nada');
    expect(montaje.texto).toContain('Todavía no hay cargos.');
  });

  it('una cuenta que no puede ver dice lo mismo que una que no existe', async () => {
    vi.spyOn(api, 'get').mockRejectedValue(
      new ErrorApi({ title: 'No se encontró lo que buscas.', status: 404, codigo: 'no_encontrado' }),
    );
    await montaje.pintar(clubDe('JUGADOR', 'ana'), '/cuenta/beto', '/cuenta/:usuarioRolId', <CuentaDelJugador />);

    expect(montaje.texto).toContain('Esta cuenta no existe o no puedes verla.');
  });

  it('vuelve al inicio la familia y a la lista de jugadores el equipo del club', async () => {
    await abrir('JUGADOR');
    expect(montaje.contenedor.querySelector('a')?.getAttribute('href')).toBe('/club/club');
    montaje.desmontar();

    montaje = new Montaje();
    await abrir('PRESIDENTE');
    expect(montaje.contenedor.querySelector('a')?.getAttribute('href')).toBe('/club/club/jugadores');
  });

  it('la entrada de menú de pagos no existe para el entrenador', () => {
    expect(puedeVerPagos('ENTRENADOR')).toBe(false);
    expect(puedeVerPagos('JUGADOR')).toBe(true);
  });
});
