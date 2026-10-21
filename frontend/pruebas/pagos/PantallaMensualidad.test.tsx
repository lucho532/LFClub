import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { api } from '../../src/compartido/api/cliente';
import type { GrupoDeJugadoresDto } from '../../src/compartido/api/tiposJugadores';
import type { MensualidadDto } from '../../src/compartido/api/tiposPagos';
import { AvisoValorMensualidad } from '../../src/privado/inicio/AvisoValorMensualidad';
import { PantallaMensualidad } from '../../src/privado/pagos/PantallaMensualidad';
import { Montaje, clubDe, simularGet } from './ayudas';

const jugadores: GrupoDeJugadoresDto[] = [
  {
    categoria: { categoriaId: 'c14', anio: 2014 },
    jugadores: [
      { usuarioRolId: 'ana', nombres: 'Ana', apellidos: 'Arango', equipos: [], fotoActualizadaEn: null },
      { usuarioRolId: 'beto', nombres: 'Beto', apellidos: 'Bermúdez', equipos: [], fotoActualizadaEn: null },
    ],
  },
];

const conValor: MensualidadDto = {
  valorClub: 65000,
  jugadoresConValorPropio: [{ usuarioRolId: 'beto', nombreCompleto: 'Beto Bermúdez', valor: 50000 }],
};

describe('PantallaMensualidad', () => {
  let montaje: Montaje;

  beforeEach(() => {
    montaje = new Montaje();
  });

  afterEach(() => montaje.desmontar());

  async function abrir(rol: 'PRESIDENTE' | 'DIRECTIVO', mensualidad: MensualidadDto) {
    simularGet({ '/api/clubes/club/mensualidad': mensualidad, '/api/clubes/club/jugadores': jugadores });
    await montaje.pintar(clubDe(rol), '/m', '/m', <PantallaMensualidad />);
  }

  it('muestra el valor vigente y los jugadores con valor propio', async () => {
    await abrir('PRESIDENTE', conValor);

    expect(montaje.texto).toContain('$ 65.000');
    expect(montaje.texto).toContain('Beto Bermúdez');
    expect(montaje.texto).toContain('$ 50.000');
  });

  it('rechaza un valor inválido con mensaje y no llama a la API', async () => {
    await abrir('PRESIDENTE', conValor);
    const guardar = vi.spyOn(api, 'put');

    await montaje.escribir(montaje.campo('Nuevo valor mensual')!, '65,5');
    await montaje.enviar(montaje.contenedor.querySelector('form')!);

    expect(montaje.texto).toContain('Escribe un valor entero de pesos');
    expect(guardar).not.toHaveBeenCalled();
  });

  it('guarda el valor nuevo y lo muestra', async () => {
    await abrir('PRESIDENTE', conValor);
    const guardar = vi
      .spyOn(api, 'put')
      .mockResolvedValue({ ...conValor, valorClub: 70000 } satisfies MensualidadDto as never);

    await montaje.escribir(montaje.campo('Nuevo valor mensual')!, '70000');
    await montaje.enviar(montaje.contenedor.querySelector('form')!);

    expect(guardar).toHaveBeenCalledWith('/api/clubes/club/mensualidad', { valor: 70000 });
    expect(montaje.texto).toContain('$ 70.000');
  });

  it('permite elegir jugadores y fijarles o quitarles el valor propio', async () => {
    await abrir('PRESIDENTE', conValor);
    const guardar = vi.spyOn(api, 'put').mockResolvedValue({ ...conValor, jugadoresConValorPropio: [] } as never);
    const casillas = [...montaje.contenedor.querySelectorAll('input[type="checkbox"]')] as HTMLInputElement[];
    expect(casillas).toHaveLength(2);

    await montaje.clic(casillas[0]);
    await montaje.escribir(montaje.campo('Valor propio de la mensualidad')!, '40000');
    await montaje.enviar(montaje.contenedor.querySelectorAll('form')[1] ?? montaje.contenedor.querySelector('form')!);

    expect(guardar).toHaveBeenCalledWith('/api/clubes/club/mensualidad/jugadores', {
      usuarioRolIds: ['ana'],
      valor: 40000,
    });

    await montaje.clic(casillas[1]);
    await montaje.clic(montaje.boton('Volver al valor del club')!);

    expect(guardar).toHaveBeenLastCalledWith('/api/clubes/club/mensualidad/jugadores', {
      usuarioRolIds: ['beto'],
      valor: null,
    });
  });

  it('al directivo solo le deja consultar', async () => {
    await abrir('DIRECTIVO', conValor);

    expect(montaje.texto).toContain('$ 65.000');
    expect(montaje.boton('Guardar valor')).toBeUndefined();
    expect(montaje.contenedor.querySelector('input[type="checkbox"]')).toBeNull();
  });

  it('avisa que se defina el valor cuando el club no lo tiene', async () => {
    await abrir('PRESIDENTE', { valorClub: null, jugadoresConValorPropio: [] });

    expect(montaje.texto).toContain('Defina el valor de la mensualidad');
  });
});

describe('AvisoValorMensualidad', () => {
  let montaje: Montaje;

  beforeEach(() => {
    montaje = new Montaje();
  });

  afterEach(() => montaje.desmontar());

  it('aparece en el inicio del presidente solo si el valor del club es nulo', async () => {
    simularGet({ '/api/clubes/club/mensualidad': { valorClub: null, jugadoresConValorPropio: [] } });
    await montaje.pintar(clubDe('PRESIDENTE'), '/i', '/i', <AvisoValorMensualidad clubId="club" />);

    expect(montaje.texto).toContain('Defina el valor de la mensualidad');
    expect(montaje.contenedor.querySelector('a')?.getAttribute('href')).toBe('/club/club/pagos/mensualidad');
  });

  it('no aparece si el club ya definió su valor', async () => {
    simularGet({ '/api/clubes/club/mensualidad': conValor });
    await montaje.pintar(clubDe('PRESIDENTE'), '/i', '/i', <AvisoValorMensualidad clubId="club" />);

    expect(montaje.texto).not.toContain('Defina');
  });
});
