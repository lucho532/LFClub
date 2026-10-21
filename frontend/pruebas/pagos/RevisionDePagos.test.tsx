import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { api } from '../../src/compartido/api/cliente';
import type { PagoDto, PagosEnRevisionDto } from '../../src/compartido/api/tiposPagos';
import { TarjetaPagosEnRevision } from '../../src/privado/inicio/TarjetaPagosEnRevision';
import { PagosEnRevision } from '../../src/privado/pagos/PagosEnRevision';
import { Montaje, clubDe, simularGet } from './ayudas';

const pago: PagoDto = {
  id: 'p1',
  usuarioRolId: 'ana',
  jugadorNombre: 'Ana Arango',
  estado: 'EN_REVISION',
  valor: 65000,
  valorInformado: null,
  medio: 'TRANSFERENCIA',
  entidad: 'Nequi',
  referencia: 'R-1',
  fecha: '2026-10-13',
  concepto: null,
  numeroRecibo: null,
  saldoTrasPago: null,
  tieneSoporte: true,
  tieneResguardo: false,
  creadoEn: '2026-10-13T15:00:00Z',
  creadoPorNombre: 'Ana Arango',
  confirmadoEn: null,
  confirmadoPorNombre: null,
  rechazadoEn: null,
  rechazadoPorNombre: null,
  motivoRechazo: null,
  anuladoEn: null,
  anuladoPorNombre: null,
  motivoAnulacion: null,
};

const enRevision: PagosEnRevisionDto = {
  total: 2,
  pagos: [
    {
      pago,
      jugadorNombre: 'Ana Arango',
      cargosPendientes: [
        { id: 'sep', tipo: 'MENSUALIDAD', mes: '2026-09-01', valor: 65000, pagado: 0, pendiente: 65000 },
        { id: 'oct', tipo: 'MENSUALIDAD', mes: '2026-10-01', valor: 65000, pagado: 0, pendiente: 65000 },
      ],
    },
    {
      pago: { ...pago, id: 'p2', jugadorNombre: 'Beto Bermúdez', usuarioRolId: 'beto' },
      jugadorNombre: 'Beto Bermúdez',
      cargosPendientes: [],
    },
  ],
};

describe('PagosEnRevision', () => {
  let montaje: Montaje;

  beforeEach(() => {
    montaje = new Montaje();
  });

  afterEach(() => montaje.desmontar());

  async function abrir(rol: 'PRESIDENTE' | 'DIRECTIVO', datos: PagosEnRevisionDto = enRevision) {
    simularGet({ '/api/clubes/club/pagos/en-revision': datos });
    await montaje.pintar(clubDe(rol), '/r', '/r', <PagosEnRevision />);
  }

  it('lista cada transferencia con su jugador, valor, entidad y lo que el jugador debe', async () => {
    await abrir('PRESIDENTE');

    expect(montaje.texto).toContain('Ana Arango');
    expect(montaje.texto).toContain('Beto Bermúdez');
    expect(montaje.texto).toContain('$ 65.000');
    expect(montaje.texto).toContain('Nequi');
    expect(montaje.texto).toContain('El jugador debe $ 130.000');
    expect(montaje.texto).toContain('En revisión');
  });

  it('sin transferencias lo dice', async () => {
    await abrir('PRESIDENTE', { total: 0, pagos: [] });

    expect(montaje.texto).toContain('No hay transferencias esperando revisión');
  });

  it('el soporte se abre a un toque y se pide con la sesión', async () => {
    await abrir('PRESIDENTE');
    const pedir = vi.spyOn(api, 'getArchivo').mockResolvedValue(new Blob(['x'], { type: 'image/png' }));
    const crear = vi.fn(() => 'blob:soporte');
    vi.stubGlobal('URL', Object.assign(URL, { createObjectURL: crear, revokeObjectURL: vi.fn() }));

    await montaje.clic(montaje.boton('Ver soporte')!);

    expect(pedir).toHaveBeenCalledWith('/api/clubes/club/pagos/p1/soporte');
    expect(montaje.contenedor.querySelector('img')?.getAttribute('src')).toBe('blob:soporte');
  });

  it('el presidente confirma con el valor corregible y los cargos elegidos', async () => {
    await abrir('PRESIDENTE');
    const confirmar = vi.spyOn(api, 'post').mockResolvedValue({ ...pago, estado: 'CONFIRMADO' } as never);

    await montaje.clic(montaje.boton('Confirmar o rechazar')!);
    await montaje.escribir(montaje.campo('Valor a confirmar')!, '40000');
    await montaje.clic(montaje.contenedor.querySelectorAll('input[type="checkbox"]')[1] as HTMLInputElement);
    await montaje.enviar(montaje.contenedor.querySelector('form')!);

    expect(confirmar).toHaveBeenCalledWith('/api/clubes/club/pagos/p1/confirmacion', {
      valor: 40000,
      cargoIds: ['oct'],
    });
  });

  it('confirmar sin cambiar nada manda el valor nulo para no corregirlo', async () => {
    await abrir('PRESIDENTE');
    const confirmar = vi.spyOn(api, 'post').mockResolvedValue({ ...pago, estado: 'CONFIRMADO' } as never);

    await montaje.clic(montaje.boton('Confirmar o rechazar')!);
    await montaje.enviar(montaje.contenedor.querySelector('form')!);

    expect(confirmar).toHaveBeenCalledWith('/api/clubes/club/pagos/p1/confirmacion', { valor: null, cargoIds: null });
  });

  it('rechazar exige un motivo', async () => {
    await abrir('PRESIDENTE');
    const rechazar = vi.spyOn(api, 'post').mockResolvedValue({ ...pago, estado: 'RECHAZADO' } as never);

    await montaje.clic(montaje.boton('Confirmar o rechazar')!);
    await montaje.clic(montaje.boton('Rechazar')!);
    await montaje.enviar(montaje.contenedor.querySelector('form')!);

    expect(montaje.texto).toContain('El motivo es obligatorio');
    expect(rechazar).not.toHaveBeenCalled();

    await montaje.escribir(montaje.campo('Motivo del rechazo')!, 'La imagen no se ve');
    await montaje.enviar(montaje.contenedor.querySelector('form')!);

    expect(rechazar).toHaveBeenCalledWith('/api/clubes/club/pagos/p1/rechazo', { motivo: 'La imagen no se ve' });
  });

  it('el directivo solo consulta', async () => {
    await abrir('DIRECTIVO');

    expect(montaje.texto).toContain('Ana Arango');
    expect(montaje.boton('Confirmar o rechazar')).toBeUndefined();
  });
});

describe('TarjetaPagosEnRevision', () => {
  let montaje: Montaje;

  beforeEach(() => {
    montaje = new Montaje();
  });

  afterEach(() => montaje.desmontar());

  it('muestra el contador de transferencias por revisar y lleva a la lista', async () => {
    simularGet({ '/api/clubes/club/pagos/en-revision': enRevision });
    await montaje.pintar(clubDe('PRESIDENTE'), '/i', '/i', <TarjetaPagosEnRevision clubId="club" />);

    expect(montaje.texto).toContain('Hay 2 transferencias esperando su revisión.');
    expect(montaje.contenedor.querySelector('a')?.getAttribute('href')).toBe('/club/club/pagos/en-revision');
  });

  it('no pinta nada si no hay ninguna', async () => {
    simularGet({ '/api/clubes/club/pagos/en-revision': { total: 0, pagos: [] } });
    await montaje.pintar(clubDe('PRESIDENTE'), '/i', '/i', <TarjetaPagosEnRevision clubId="club" />);

    expect(montaje.texto).toBe('');
  });

  it('no es un recordatorio de cobro: no habla de deudas', async () => {
    simularGet({ '/api/clubes/club/pagos/en-revision': enRevision });
    await montaje.pintar(clubDe('PRESIDENTE'), '/i', '/i', <TarjetaPagosEnRevision clubId="club" />);

    for (const palabra of ['debe', 'deuda', 'mora', 'cobrar']) {
      expect(montaje.texto.toLowerCase()).not.toContain(palabra);
    }
  });
});
