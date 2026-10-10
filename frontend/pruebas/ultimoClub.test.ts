import { beforeEach, describe, expect, it, vi } from 'vitest';
import type { ClubDeSesionDto, EstadoIngreso } from '../src/compartido/api/tipos';
import { clubDeEntrada, guardarUltimoClub } from '../src/compartido/sesion/ultimoClub';

function club(clubId: string, estadoIngreso: EstadoIngreso = 'APROBADO', retirado = false): ClubDeSesionDto {
  return {
    clubId,
    nombre: `Club ${clubId}`,
    rol: estadoIngreso === 'APROBADO' && !retirado ? 'PRESIDENTE' : 'JUGADOR',
    estado: 'ACTIVO',
    estadoIngreso,
    retirado,
    identidad: { colorPrincipal: null, colorAcento: null, urlEscudo: null },
    nombres: 'Ana',
    apellidos: 'Pérez',
    usuarioRolId: `integrante-${clubId}`,
    jugadores: [],
  };
}

describe('club de entrada', () => {
  beforeEach(() => {
    localStorage.clear();
    vi.restoreAllMocks();
  });

  it('sin clubes no hay club de entrada', () => {
    expect(clubDeEntrada([])).toBeNull();
  });

  it('quien tiene un solo club entra directamente a él', () => {
    expect(clubDeEntrada([club('a')])).toBe('a');
  });

  it('sin elección previa entra al primero de sus clubes', () => {
    expect(clubDeEntrada([club('a'), club('b')])).toBe('a');
  });

  it('recuerda el último club elegido', () => {
    guardarUltimoClub('b');

    expect(clubDeEntrada([club('a'), club('b')])).toBe('b');
  });

  it('si el último club ya no es suyo, vuelve a uno propio', () => {
    guardarUltimoClub('ajeno');

    expect(clubDeEntrada([club('a'), club('b')])).toBe('a');
  });

  it('sin elección previa prefiere un club con el ingreso aprobado', () => {
    expect(clubDeEntrada([club('a', 'EN_ESPERA'), club('b'), club('c')])).toBe('b');
  });

  it('respeta el último club elegido aunque en él esté en espera', () => {
    guardarUltimoClub('a');

    expect(clubDeEntrada([club('a', 'EN_ESPERA'), club('b')])).toBe('a');
  });

  it('si en todos está en espera entra al primero', () => {
    expect(clubDeEntrada([club('a', 'EN_ESPERA'), club('b', 'EN_ESPERA')])).toBe('a');
  });

  it('si el último club ya no es suyo, vuelve a uno aprobado', () => {
    guardarUltimoClub('ajeno');

    expect(clubDeEntrada([club('a', 'EN_ESPERA'), club('b')])).toBe('b');
  });

  it('sin elección previa prefiere un club en el que no está retirada', () => {
    expect(clubDeEntrada([club('a', 'APROBADO', true), club('b'), club('c')])).toBe('b');
  });

  it('entre uno retirado y otro en espera prefiere el que está en espera', () => {
    expect(clubDeEntrada([club('a', 'APROBADO', true), club('b', 'EN_ESPERA')])).toBe('b');
  });

  it('respeta el último club elegido aunque esté retirada en él', () => {
    guardarUltimoClub('a');

    expect(clubDeEntrada([club('a', 'APROBADO', true), club('b')])).toBe('a');
  });

  it('si está retirada en todos entra al primero', () => {
    expect(clubDeEntrada([club('a', 'APROBADO', true), club('b', 'APROBADO', true)])).toBe('a');
  });

  it('funciona aunque el almacenamiento local no esté disponible', () => {
    vi.spyOn(Storage.prototype, 'getItem').mockImplementation(() => {
      throw new Error('bloqueado');
    });
    vi.spyOn(Storage.prototype, 'setItem').mockImplementation(() => {
      throw new Error('bloqueado');
    });

    expect(() => guardarUltimoClub('b')).not.toThrow();
    expect(clubDeEntrada([club('a'), club('b')])).toBe('a');
  });
});
