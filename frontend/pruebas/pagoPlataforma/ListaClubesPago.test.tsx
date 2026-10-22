import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import type { ClubResumenDto } from '../../src/compartido/api/tipos';
import { ListaClubes } from '../../src/plataforma/ListaClubes';
import { Montaje, clubDe, simularGet } from '../pagos/ayudas';

const base = {
  presidenteRegistrado: true,
  identidad: { colorPrincipal: null, colorAcento: null, urlEscudo: null },
  integrantes: { jugadores: 10, entrenadores: 2, directivos: 1, presidentes: 1 },
  motivoDeSuspension: null,
  mesDeInicioDelCobro: '2026-11',
} as const;

const clubes = [
  { ...base, clubId: 'a', nombre: 'Club al día', estado: 'ACTIVO', estadoDePago: 'AL_DIA', valorAdeudado: 0 },
  { ...base, clubId: 'b', nombre: 'Club pendiente', estado: 'ACTIVO', estadoDePago: 'PENDIENTE', valorAdeudado: 45000 },
  { ...base, clubId: 'c', nombre: 'Club vencido', estado: 'ACTIVO', estadoDePago: 'VENCIDO', valorAdeudado: 30000 },
  { ...base, clubId: 'd', nombre: 'Club suspendido', estado: 'SUSPENDIDO', estadoDePago: 'SUSPENDIDO_POR_IMPAGO', valorAdeudado: 30000, motivoDeSuspension: 'IMPAGO' },
  { ...base, clubId: 'e', nombre: 'Club sin cobro', estado: 'ACTIVO', estadoDePago: 'SIN_COBRO', valorAdeudado: 0, mesDeInicioDelCobro: null },
] as unknown as ClubResumenDto[];

describe('ListaClubes: estado de pago', () => {
  let montaje: Montaje;

  beforeEach(async () => {
    montaje = new Montaje();
    simularGet({ '/api/plataforma/clubes': clubes });
    await montaje.pintar(clubDe('DESARROLLADOR'), '/p', '/p', <ListaClubes />);
  });

  afterEach(() => montaje.desmontar());

  it('muestra una columna Pago con el estado de cada club en texto', () => {
    const encabezados = [...montaje.contenedor.querySelectorAll('th')].map((th) => th.textContent);
    expect(encabezados).toContain('Pago');
    expect(montaje.texto).toContain('✓ Al día');
    expect(montaje.texto).toContain('! Pendiente');
    expect(montaje.texto).toContain('⚠ Vencido');
    expect(montaje.texto).toContain('⚠ Suspendido por impago');
    expect(montaje.texto).toContain('Sin cobro');
  });

  it('muestra el valor adeudado solo cuando hay algo por pagar', () => {
    expect(montaje.texto).toContain('$ 45.000');
    expect(montaje.texto).toContain('$ 30.000');
    expect(montaje.texto).not.toContain('$ 0');
  });
});
