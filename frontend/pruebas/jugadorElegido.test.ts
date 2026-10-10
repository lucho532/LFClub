import { beforeEach, describe, expect, it, vi } from 'vitest';
import type { ClubDeSesionDto, JugadorDeSesionDto } from '../src/compartido/api/tiposSesion';
import {
  guardarJugadorElegido,
  jugadorDe,
  leerJugadorElegido,
  olvidarJugadorElegido,
  olvidarJugadoresElegidos,
} from '../src/compartido/sesion/jugadorElegido';

function jugador(usuarioRolId: string): JugadorDeSesionDto {
  return { usuarioRolId, nombres: `Jugador ${usuarioRolId}`, apellidos: 'Gómez', estadoIngreso: 'APROBADO', retirado: false };
}

function club(clubId: string, jugadores: JugadorDeSesionDto[]): ClubDeSesionDto {
  return {
    clubId,
    nombre: `Club ${clubId}`,
    rol: 'JUGADOR',
    estado: 'ACTIVO',
    estadoIngreso: 'APROBADO',
    retirado: false,
    identidad: { colorPrincipal: null, colorAcento: null, urlEscudo: null },
    nombres: 'Ana',
    apellidos: 'Gómez',
    usuarioRolId: jugadores[0]?.usuarioRolId ?? 'unico',
    jugadores,
  };
}

describe('jugador elegido', () => {
  beforeEach(() => {
    sessionStorage.clear();
    localStorage.clear();
    vi.restoreAllMocks();
  });

  it('sin haber elegido no hay jugador', () => {
    expect(leerJugadorElegido('a')).toBeNull();
    expect(jugadorDe(club('a', [jugador('ana'), jugador('luis')]))).toBeNull();
  });

  it('guarda y lee la elección de un club', () => {
    guardarJugadorElegido('a', 'luis');

    expect(leerJugadorElegido('a')).toBe('luis');
    expect(jugadorDe(club('a', [jugador('ana'), jugador('luis')]))?.usuarioRolId).toBe('luis');
  });

  it('la elección de un club no vale en otro', () => {
    guardarJugadorElegido('a', 'luis');

    expect(leerJugadorElegido('b')).toBeNull();
    expect(jugadorDe(club('b', [jugador('ana'), jugador('luis')]))).toBeNull();
  });

  it('cada club recuerda la suya', () => {
    guardarJugadorElegido('a', 'luis');
    guardarJugadorElegido('b', 'ana');

    expect(leerJugadorElegido('a')).toBe('luis');
    expect(leerJugadorElegido('b')).toBe('ana');
  });

  it('si el elegido ya no está en la lista no hay jugador', () => {
    guardarJugadorElegido('a', 'nico');

    expect(jugadorDe(club('a', [jugador('ana'), jugador('luis')]))).toBeNull();
  });

  it('si la lista está vacía no hay jugador, aunque quede una elección guardada', () => {
    guardarJugadorElegido('a', 'ana');

    expect(jugadorDe(club('a', []))).toBeNull();
  });

  it('olvidar la de un club no toca la de otro', () => {
    guardarJugadorElegido('a', 'luis');
    guardarJugadorElegido('b', 'ana');

    olvidarJugadorElegido('a');

    expect(leerJugadorElegido('a')).toBeNull();
    expect(leerJugadorElegido('b')).toBe('ana');
  });

  it('al iniciar o cerrar sesión se olvidan todas y nada más', () => {
    guardarJugadorElegido('a', 'luis');
    guardarJugadorElegido('b', 'ana');
    sessionStorage.setItem('otra.clave', 'se queda');

    olvidarJugadoresElegidos();

    expect(leerJugadorElegido('a')).toBeNull();
    expect(leerJugadorElegido('b')).toBeNull();
    expect(sessionStorage.getItem('otra.clave')).toBe('se queda');
  });

  it('no se guarda en el almacenamiento que sobrevive a cerrar la pestaña', () => {
    guardarJugadorElegido('a', 'luis');

    expect(localStorage.length).toBe(0);
  });

  it('funciona aunque no haya almacenamiento', () => {
    vi.spyOn(Storage.prototype, 'getItem').mockImplementation(() => {
      throw new Error('bloqueado');
    });
    vi.spyOn(Storage.prototype, 'setItem').mockImplementation(() => {
      throw new Error('bloqueado');
    });
    vi.spyOn(Storage.prototype, 'removeItem').mockImplementation(() => {
      throw new Error('bloqueado');
    });

    expect(() => guardarJugadorElegido('a', 'luis')).not.toThrow();
    expect(() => olvidarJugadorElegido('a')).not.toThrow();
    expect(() => olvidarJugadoresElegidos()).not.toThrow();
    expect(leerJugadorElegido('a')).toBeNull();
    expect(jugadorDe(club('a', [jugador('ana'), jugador('luis')]))).toBeNull();
  });
});
