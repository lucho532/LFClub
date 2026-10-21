import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { api } from '../../src/compartido/api/cliente';
import { ErrorApi } from '../../src/compartido/api/errores';
import type { AvisoDeMoraDto } from '../../src/compartido/api/tiposPagos';
import { AvisoDeMora } from '../../src/privado/inicio/AvisoDeMora';
import { InicioClub } from '../../src/privado/InicioClub';
import { Montaje, clubDe, simularGet } from './ayudas';

const RUTA = '/api/clubes/club/jugadores/ana/cuenta/aviso';

const alDia: AvisoDeMoraDto = {
  estado: 'AL_DIA',
  valorVencido: 0,
  mesesVencidos: 0,
  valorEnPlazo: 0,
  venceEl: null,
  pagosEnRevision: 0,
};

describe('AvisoDeMora', () => {
  let montaje: Montaje;

  beforeEach(() => {
    montaje = new Montaje();
  });

  afterEach(() => montaje.desmontar());

  async function abrir(aviso: AvisoDeMoraDto) {
    simularGet({ [RUTA]: aviso });
    await montaje.pintar(clubDe('JUGADOR', 'ana'), '/inicio', '/inicio', <AvisoDeMora clubId="club" miUsuarioRolId="ana" />);
  }

  it('en mora es rojo, dice cuánto debe y de cuántas mensualidades, con ícono y texto', async () => {
    await abrir({ ...alDia, estado: 'EN_MORA', valorVencido: 130000, mesesVencidos: 2 });

    expect(montaje.texto).toContain('Estás en mora: debes $ 130.000 de 2 mensualidades vencidas.');
    expect(montaje.contenedor.querySelector('.tono-error')).not.toBeNull();
    expect(montaje.contenedor.querySelector('[aria-hidden="true"]')?.textContent).toBe('⚠');
  });

  it('con una sola mensualidad vencida lo dice en singular', async () => {
    await abrir({ ...alDia, estado: 'EN_MORA', valorVencido: 65000, mesesVencidos: 1 });

    expect(montaje.texto).toContain('de 1 mensualidad vencida.');
  });

  it('no se puede cerrar, ocultar ni posponer: no tiene ningún botón', async () => {
    await abrir({ ...alDia, estado: 'EN_MORA', valorVencido: 65000, mesesVencidos: 1 });

    expect(montaje.contenedor.querySelectorAll('button')).toHaveLength(0);
  });

  it('lleva a la cuenta del jugador', async () => {
    await abrir({ ...alDia, estado: 'EN_MORA', valorVencido: 65000, mesesVencidos: 1 });

    const enlace = montaje.contenedor.querySelector('a');
    expect(enlace?.textContent).toBe('Ver mi cuenta');
    expect(enlace?.getAttribute('href')).toBe('/club/club/jugadores/ana/cuenta');
  });

  it('dentro del plazo es ámbar y dice que vence el día 5', async () => {
    await abrir({ ...alDia, estado: 'PENDIENTE_EN_PLAZO', valorEnPlazo: 65000, venceEl: '2026-11-05' });

    expect(montaje.texto).toContain('Tienes la mensualidad de este mes pendiente ($ 65.000). Vence el día 5.');
    expect(montaje.contenedor.querySelector('.tono-aviso')).not.toBeNull();
    expect(montaje.contenedor.querySelector('.tono-error')).toBeNull();
  });

  it('al día es verde y lo dice con texto', async () => {
    await abrir(alDia);

    expect(montaje.texto).toContain('Estás al día.');
    expect(montaje.contenedor.querySelector('.tono-exito')).not.toBeNull();
  });

  it('avisa de los pagos en revisión sin cambiar el color', async () => {
    await abrir({ ...alDia, estado: 'EN_MORA', valorVencido: 65000, mesesVencidos: 1, pagosEnRevision: 1 });
    expect(montaje.texto).toContain('Tienes 1 pago en revisión.');
    expect(montaje.contenedor.querySelector('.tono-error')).not.toBeNull();
  });

  it('con varios pagos en revisión usa el plural', async () => {
    await abrir({ ...alDia, pagosEnRevision: 2 });

    expect(montaje.texto).toContain('Tienes 2 pagos en revisión.');
  });

  it('no muestra nada mientras carga: ni verde ni rojo', async () => {
    vi.spyOn(api, 'get').mockReturnValue(new Promise(() => undefined));
    await montaje.pintar(clubDe('JUGADOR', 'ana'), '/inicio', '/inicio', <AvisoDeMora clubId="club" miUsuarioRolId="ana" />);

    expect(montaje.contenedor.querySelector('.aviso')).toBeNull();
  });

  it('si no se pudo consultar muestra un aviso neutro con Reintentar y nunca el verde', async () => {
    vi.spyOn(api, 'get').mockRejectedValue(new ErrorApi({ title: 'Algo salió mal.', status: 500, codigo: 'error' }));
    await montaje.pintar(clubDe('JUGADOR', 'ana'), '/inicio', '/inicio', <AvisoDeMora clubId="club" miUsuarioRolId="ana" />);

    expect(montaje.texto).toContain('No pudimos consultar el estado de tu cuenta.');
    expect(montaje.boton('Reintentar')).toBeDefined();
    expect(montaje.contenedor.querySelector('.tono-neutro')).not.toBeNull();
    expect(montaje.contenedor.querySelector('.tono-exito')).toBeNull();
    expect(montaje.texto).not.toContain('Estás al día');
  });
});

describe('InicioClub y el aviso de mora', () => {
  let montaje: Montaje;

  beforeEach(() => {
    montaje = new Montaje();
  });

  afterEach(() => montaje.desmontar());

  function simularInicio() {
    return vi.spyOn(api, 'get').mockImplementation(async (ruta: string) => {
      if (ruta.endsWith('/cuenta/aviso')) {
        return { ...alDia, estado: 'EN_MORA', valorVencido: 65000, mesesVencidos: 1 } as never;
      }

      if (ruta.endsWith('/ficha')) {
        return { nombres: 'Ana', apellidos: 'Arango', equipos: [], categoriaAnio: 2014, permisos: { puedeCambiar: false }, fotoActualizadaEn: null, retirado: false } as never;
      }

      return [] as never;
    });
  }

  it('la familia lo ve arriba, con el jugador con el que entró', async () => {
    const llamadas = simularInicio();
    await montaje.pintar(clubDe('JUGADOR', 'ana'), '/', '/', <InicioClub />);

    expect(llamadas.mock.calls.map(([ruta]) => ruta)).toContain(RUTA);
    expect(montaje.texto).toContain('Estás en mora');
  });

  it.each(['PRESIDENTE', 'DIRECTIVO', 'ENTRENADOR'] as const)('el inicio del %s no lo pide ni lo muestra', async (rol) => {
    const llamadas = simularInicio();
    await montaje.pintar(clubDe(rol, 'yo'), '/', '/', <InicioClub />);

    expect(llamadas.mock.calls.some(([ruta]) => String(ruta).includes('/cuenta/aviso'))).toBe(false);
    expect(montaje.texto).not.toContain('Estás en mora');
    expect(montaje.texto).not.toContain('Estás al día');
  });
});
