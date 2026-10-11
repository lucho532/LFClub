import { describe, expect, it } from 'vitest';
import type { PartidoResumenDto } from '../src/compartido/api/tiposActividades';
import { nombreRival, textoDeMarcador } from '../src/privado/partidos/textos';

const base: PartidoResumenDto = {
  partidoId: 'p1',
  fecha: '2026-10-17',
  hora: '09:00',
  lugar: 'Cancha de Minitas',
  condicion: 'LOCAL',
  tipo: 'AMISTOSO',
  estado: 'JUGADO',
  faltaResultado: false,
  golesClub: 3,
  golesRival: 1,
  categorias: [],
};

describe('nombreRival', () => {
  it('sin rival dice "Por definir"', () => {
    expect(nombreRival(base)).toBe('Por definir');
  });

  it('con rival lo devuelve tal cual', () => {
    expect(nombreRival({ ...base, rival: 'Equipo C' })).toBe('Equipo C');
  });
});

describe('textoDeMarcador', () => {
  it('de local pone primero al club', () => {
    expect(textoDeMarcador({ ...base, rival: 'Equipo C' }, 'La Pecosa')).toBe('La Pecosa 3 - 1 Equipo C');
  });

  it('de visitante y sin rival pone primero "Por definir"', () => {
    expect(textoDeMarcador({ ...base, condicion: 'VISITANTE' }, 'La Pecosa')).toBe('Por definir 1 - 3 La Pecosa');
  });
});
