import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { api } from '../../src/compartido/api/cliente';
import type { PagoDto } from '../../src/compartido/api/tiposPagos';
import { nombreDelResguardo } from '../../src/privado/pagos/compartirResguardo';
import { DetalleDePago } from '../../src/privado/pagos/DetalleDePago';
import { ListaDePagos } from '../../src/privado/pagos/ListaDePagos';
import { Montaje, clubDe, simularGet } from './ayudas';

const confirmado: PagoDto = {
  id: 'p1',
  usuarioRolId: 'ana',
  jugadorNombre: 'Ana Arango',
  estado: 'CONFIRMADO',
  valor: 65000,
  valorInformado: null,
  medio: 'EFECTIVO',
  entidad: null,
  referencia: null,
  fecha: '2026-10-13',
  concepto: 'Mensualidad octubre de 2026',
  numeroRecibo: 12,
  saldoTrasPago: 0,
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
};

describe('Recibo y resguardo', () => {
  let montaje: Montaje;

  beforeEach(() => {
    montaje = new Montaje();
    vi.stubGlobal('URL', Object.assign(URL, { createObjectURL: vi.fn(() => 'blob:resguardo'), revokeObjectURL: vi.fn() }));
  });

  afterEach(() => {
    montaje.desmontar();
    vi.unstubAllGlobals();
    Reflect.deleteProperty(navigator, 'share');
    Reflect.deleteProperty(navigator, 'canShare');
  });

  async function abrir(pago: PagoDto = confirmado) {
    simularGet({ '/api/clubes/club/pagos/p1': pago });
    const pedir = vi.spyOn(api, 'getArchivo').mockResolvedValue(new Blob(['png'], { type: 'image/png' }));
    await montaje.pintar(clubDe('JUGADOR', 'ana'), '/r/p1', '/r/:pagoId', <DetalleDePago />);
    return pedir;
  }

  it('muestra el recibo con su número y el resguardo pedido con la sesión', async () => {
    const pedir = await abrir();

    expect(montaje.texto).toContain('Recibo n.º 12');
    expect(pedir).toHaveBeenCalledWith('/api/clubes/club/pagos/p1/resguardo');
    expect(montaje.contenedor.querySelector('img[alt="Resguardo de pago n.º 12"]')?.getAttribute('src')).toBe(
      'blob:resguardo',
    );
  });

  it('ofrece Descargar y guarda el archivo con el número de recibo', async () => {
    await abrir();
    const clic = vi.spyOn(HTMLAnchorElement.prototype, 'click').mockImplementation(() => undefined);

    await montaje.clic(montaje.boton('Descargar')!);

    expect(clic).toHaveBeenCalled();
    expect(nombreDelResguardo(12)).toBe('resguardo-12.png');
  });

  it('no ofrece Compartir si el navegador no lo admite', async () => {
    await abrir();

    expect(montaje.boton('Compartir')).toBeUndefined();
  });

  it('ofrece Compartir cuando el navegador lo admite con archivos y lo usa con la imagen', async () => {
    const compartir = vi.fn().mockResolvedValue(undefined);
    Object.defineProperty(navigator, 'share', { value: compartir, configurable: true });
    Object.defineProperty(navigator, 'canShare', { value: () => true, configurable: true });
    await abrir();

    await montaje.clic(montaje.boton('Compartir')!);

    expect(compartir).toHaveBeenCalledTimes(1);
    const datos = compartir.mock.calls[0][0] as { files: File[] };
    expect(datos.files[0].name).toBe('resguardo-12.png');
    expect(datos.files[0].type).toBe('image/png');
  });

  it('un pago anulado conserva su recibo y su resguardo, marcado como anulado', async () => {
    await abrir({ ...confirmado, estado: 'ANULADO', anuladoPorNombre: 'Pat', motivoAnulacion: 'Error' });

    expect(montaje.texto).toContain('Anulado');
    expect(montaje.texto).toContain('Recibo n.º 12');
    expect(montaje.contenedor.querySelector('img')).not.toBeNull();
  });

  it('un pago sin resguardo no lo pide', async () => {
    const pedir = await abrir({ ...confirmado, estado: 'EN_REVISION', tieneResguardo: false, numeroRecibo: null });

    expect(pedir).not.toHaveBeenCalled();
    expect(montaje.contenedor.querySelector('img')).toBeNull();
  });
});

describe('ListaDePagos enlaza el recibo', () => {
  let montaje: Montaje;

  beforeEach(() => {
    montaje = new Montaje();
  });

  afterEach(() => montaje.desmontar());

  it('solo los pagos confirmados o anulados tienen enlace al recibo', async () => {
    await montaje.pintar(
      clubDe('JUGADOR'),
      '/l',
      '/l',
      <ListaDePagos
        clubId="club"
        pagos={[
          confirmado,
          { ...confirmado, id: 'p2', estado: 'EN_REVISION', tieneResguardo: false, numeroRecibo: null },
          { ...confirmado, id: 'p3', estado: 'ANULADO' },
        ]}
      />,
    );

    const enlaces = [...montaje.contenedor.querySelectorAll('a')].map((enlace) => enlace.getAttribute('href'));
    expect(enlaces).toEqual(['/club/club/recibos/p1', '/club/club/recibos/p3']);
  });
});
