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

  const lapizDe = (nombre: string) =>
    montaje.contenedor.querySelector(`button[aria-label="Editar la mensualidad de ${nombre}"]`) as HTMLButtonElement | null;

  const botonDe = (nombre: string) =>
    montaje.contenedor.querySelector(`button[aria-label="${nombre}"]`) as HTMLButtonElement | null;

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

  it('cada jugador es una línea de solo lectura con su valor y un lápiz, sin campos hasta pulsarlo', async () => {
    await abrir('PRESIDENTE', conValor);

    expect(montaje.contenedor.querySelectorAll('.linea-mensualidad-jugador')).toHaveLength(2);
    expect(montaje.texto).toContain('Valor propio: $ 50.000');
    expect(montaje.texto).toContain('Valor del club: $ 65.000');
    expect(montaje.campo('Valor de Ana Arango')).toBeNull();
    expect(lapizDe('Ana Arango')).not.toBeNull();
  });

  it('el lápiz abre el campo en la misma línea con el valor vigente y Cancelar lo cierra sin guardar', async () => {
    await abrir('PRESIDENTE', conValor);
    const guardar = vi.spyOn(api, 'put');

    await montaje.clic(lapizDe('Beto Bermúdez')!);
    expect(montaje.campo('Valor de Beto Bermúdez')?.value).toBe('50000');
    await montaje.escribir(montaje.campo('Valor de Beto Bermúdez')!, '1');
    await montaje.clic(montaje.boton('Cancelar')!);

    expect(montaje.campo('Valor de Beto Bermúdez')).toBeNull();
    expect(montaje.texto).toContain('Valor propio: $ 50.000');
    expect(guardar).not.toHaveBeenCalled();
  });

  it('guarda el valor propio de un jugador desde su línea y la vuelve a cerrar', async () => {
    await abrir('PRESIDENTE', conValor);
    const guardar = vi.spyOn(api, 'put').mockResolvedValue({
      ...conValor,
      jugadoresConValorPropio: [...conValor.jugadoresConValorPropio, { usuarioRolId: 'ana', nombreCompleto: 'Ana Arango', valor: 40000 }],
    } as never);

    await montaje.clic(lapizDe('Ana Arango')!);
    expect((montaje.campo('Valor de Ana Arango') as HTMLInputElement).placeholder).toContain('$ 65.000');
    expect(botonDe('Guardar el valor de Ana Arango')?.disabled).toBe(true);
    await montaje.escribir(montaje.campo('Valor de Ana Arango')!, '40.000');
    await montaje.clic(botonDe('Guardar el valor de Ana Arango')!);

    expect(guardar).toHaveBeenCalledWith('/api/clubes/club/mensualidad/jugadores', { usuarioRolIds: ['ana'], valor: 40000 });
    expect(montaje.campo('Valor de Ana Arango')).toBeNull();
    expect(montaje.texto).toContain('Valor propio: $ 40.000');
  });

  it('rechaza un valor inválido en la tarjeta sin llamar a la API', async () => {
    await abrir('PRESIDENTE', conValor);
    const guardar = vi.spyOn(api, 'put');

    await montaje.clic(lapizDe('Ana Arango')!);
    await montaje.escribir(montaje.campo('Valor de Ana Arango')!, '40,5');
    await montaje.clic(botonDe('Guardar el valor de Ana Arango')!);

    expect(guardar).not.toHaveBeenCalled();
    expect(montaje.texto).toContain('Escribe un valor entero de pesos');
  });

  it('«Usar valor del club» quita el valor propio de ese jugador', async () => {
    await abrir('PRESIDENTE', conValor);
    const guardar = vi.spyOn(api, 'put').mockResolvedValue({ ...conValor, jugadoresConValorPropio: [] } as never);

    await montaje.clic(lapizDe('Beto Bermúdez')!);
    await montaje.clic(botonDe('Usar el valor del club para Beto Bermúdez')!);

    expect(guardar).toHaveBeenCalledWith('/api/clubes/club/mensualidad/jugadores', { usuarioRolIds: ['beto'], valor: null });
    expect(montaje.campo('Valor de Beto Bermúdez')).toBeNull();
    expect(montaje.texto).not.toContain('Valor propio');
    expect(montaje.texto).toContain('Valor del club: $ 65.000');
  });

  it('si no se pudo guardar lo dice y deja el valor como estaba', async () => {
    await abrir('PRESIDENTE', conValor);
    vi.spyOn(api, 'put').mockRejectedValue(new Error('falló'));

    await montaje.clic(lapizDe('Beto Bermúdez')!);
    await montaje.clic(botonDe('Usar el valor del club para Beto Bermúdez')!);

    expect(montaje.campo('Valor de Beto Bermúdez')?.getAttribute('aria-invalid')).toBe('true');
    expect(montaje.campo('Valor de Beto Bermúdez')?.value).toBe('50000');
  });

  it('busca por nombre sin fijarse en tildes ni mayúsculas y puede mostrar solo a quienes tienen valor propio', async () => {
    await abrir('PRESIDENTE', conValor);

    await montaje.escribir(montaje.campo('Buscar jugador')!, 'bermudez');
    expect(montaje.contenedor.querySelectorAll('.linea-mensualidad-jugador')).toHaveLength(1);
    expect(montaje.texto).toContain('Beto Bermúdez');

    await montaje.escribir(montaje.campo('Buscar jugador')!, 'zzz');
    expect(montaje.texto).toContain('Ningún jugador coincide');

    await montaje.escribir(montaje.campo('Buscar jugador')!, '');
    const filtro = montaje.contenedor.querySelector('input[type="checkbox"]') as HTMLInputElement;
    await montaje.clic(filtro);
    expect(montaje.contenedor.querySelectorAll('.linea-mensualidad-jugador')).toHaveLength(1);
  });

  it('al directivo solo le deja consultar: ve los valores pero no los campos ni los botones', async () => {
    await abrir('DIRECTIVO', conValor);

    expect(montaje.texto).toContain('$ 65.000');
    expect(montaje.texto).toContain('Valor propio: $ 50.000');
    expect(montaje.boton('Guardar valor')).toBeUndefined();
    expect(montaje.campo('Valor de Beto Bermúdez')).toBeNull();
    expect(lapizDe('Beto Bermúdez')).toBeNull();
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
