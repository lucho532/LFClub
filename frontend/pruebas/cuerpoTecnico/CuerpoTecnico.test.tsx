import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { api } from '../../src/compartido/api/cliente';
import { ErrorApi } from '../../src/compartido/api/errores';
import type { MiembroCuerpoTecnicoDetalleDto, MiembroCuerpoTecnicoDto } from '../../src/compartido/api/tiposCuerpoTecnico';
import { CuerpoTecnico } from '../../src/privado/cuerpoTecnico/CuerpoTecnico';
import { DetalleCuerpoTecnico } from '../../src/privado/cuerpoTecnico/DetalleCuerpoTecnico';
import { Montaje, clubDe, simularGet } from '../pagos/ayudas';

const lista: MiembroCuerpoTecnicoDto[] = [
  { usuarioRolId: 'p', nombres: 'Paula', apellidos: 'Pérez', rol: 'PRESIDENTE', versionFoto: 0, categorias: [] },
  { usuarioRolId: 'd', nombres: 'Diego', apellidos: 'Díaz', rol: 'DIRECTIVO', versionFoto: 0, categorias: [] },
  {
    usuarioRolId: 'e',
    nombres: 'Elena',
    apellidos: 'Escobar',
    rol: 'ENTRENADOR',
    versionFoto: 0,
    categorias: [
      { anio: 2014, equipos: ['A', 'B'] },
      { anio: 2015, equipos: [] },
    ],
  },
  { usuarioRolId: 'n', nombres: 'Nico', apellidos: 'Nieto', rol: 'ENTRENADOR', versionFoto: 0, categorias: [] },
];

const entrenador: MiembroCuerpoTecnicoDetalleDto = {
  ...lista[2],
  tipoDocumento: 'CEDULA_CIUDADANIA',
  numeroDocumento: '1020304050',
  correo: 'elena@club.co',
  celular: '3001234567',
  proximosEntrenamientos: [
    {
      entrenamientoId: 'e1',
      fecha: '2026-11-03',
      horaInicio: '16:00',
      horaFin: '17:30',
      lugar: 'Cancha de Minitas',
      estado: 'PROGRAMADO',
      categorias: [{ categoriaId: 'c14', anio: 2014 }],
    },
  ],
};

describe('Cuerpo técnico', () => {
  let montaje: Montaje;

  beforeEach(() => {
    montaje = new Montaje();
  });

  afterEach(() => montaje.desmontar());

  it('la lista muestra el nombre completo, el rol y las categorías de cada persona, y cada una abre su ficha', async () => {
    simularGet({ '/api/clubes/club/cuerpo-tecnico': lista });
    await montaje.pintar(clubDe('PRESIDENTE'), '/ct', '/ct', <CuerpoTecnico />);

    expect(montaje.texto).toContain('Paula Pérez');
    expect(montaje.texto).toContain('Presidente · —');
    expect(montaje.texto).toContain('Elena Escobar');
    expect(montaje.texto).toContain('Entrenador · 2014 · A, B · 2015');
    expect(montaje.texto).toContain('Entrenador · Sin categorías asignadas');
    const enlace = [...montaje.contenedor.querySelectorAll('a')].find((a) => a.textContent?.includes('Elena Escobar'));
    expect(enlace?.getAttribute('href')).toBe('/club/club/cuerpo-tecnico/e');
  });

  it('un club sin personal lo dice', async () => {
    simularGet({ '/api/clubes/club/cuerpo-tecnico': [] });
    await montaje.pintar(clubDe('PRESIDENTE'), '/ct', '/ct', <CuerpoTecnico />);

    expect(montaje.texto).toContain('Todavía no hay personal en el club.');
  });

  it('los demás roles vuelven al inicio sin pedir ninguna lista', async () => {
    const pedir = vi.spyOn(api, 'get');

    for (const rol of ['DIRECTIVO', 'ENTRENADOR', 'JUGADOR'] as const) {
      await montaje.pintar(clubDe(rol), '/ct', '/ct', <CuerpoTecnico />);
      expect(montaje.texto).toBe('otra pantalla');
    }

    expect(pedir).not.toHaveBeenCalled();
  });

  it('la ficha de un entrenador muestra sus datos, sus categorías y una tarjeta por entrenamiento', async () => {
    simularGet({ '/api/clubes/club/cuerpo-tecnico/e': entrenador });
    await montaje.pintar(clubDe('PRESIDENTE'), '/ct/e', '/ct/:usuarioRolId', <DetalleCuerpoTecnico />);

    expect(montaje.texto).toContain('Elena Escobar');
    expect(montaje.texto).toContain('Cédula de ciudadanía');
    expect(montaje.texto).toContain('1020304050');
    expect(montaje.texto).toContain('elena@club.co');
    expect(montaje.texto).toContain('3001234567');
    expect(montaje.texto).toContain('Categoría 2014 · A, B');
    expect(montaje.texto).toContain('Categoría 2015');
    expect(montaje.texto).toContain('Próximos entrenamientos');
    expect(montaje.texto).toContain('Cancha de Minitas');
    const enlace = [...montaje.contenedor.querySelectorAll('a')].find((a) => a.textContent === 'Ver entrenamiento');
    expect(enlace?.getAttribute('href')).toBe('/club/club/entrenamientos/e1');
  });

  it('un entrenador sin entrenamientos próximos lo dice', async () => {
    simularGet({ '/api/clubes/club/cuerpo-tecnico/e': { ...entrenador, proximosEntrenamientos: [] } });
    await montaje.pintar(clubDe('PRESIDENTE'), '/ct/e', '/ct/:usuarioRolId', <DetalleCuerpoTecnico />);

    expect(montaje.texto).toContain('No tiene entrenamientos próximos.');
  });

  it('un directivo no tiene sección de entrenamientos y sin teléfono lo dice', async () => {
    simularGet({
      '/api/clubes/club/cuerpo-tecnico/d': {
        ...lista[1],
        tipoDocumento: 'CEDULA_CIUDADANIA',
        numeroDocumento: '99',
        correo: 'diego@club.co',
        celular: null,
        proximosEntrenamientos: [],
      },
    });
    await montaje.pintar(clubDe('PRESIDENTE'), '/ct/d', '/ct/:usuarioRolId', <DetalleCuerpoTecnico />);

    expect(montaje.texto).toContain('Directivo');
    expect(montaje.texto).toContain('Sin teléfono');
    expect(montaje.texto).toContain('No entrena ninguna categoría.');
    expect(montaje.texto).not.toContain('Próximos entrenamientos');
  });

  it('una persona que no existe dice que no la encuentra', async () => {
    vi.spyOn(api, 'get').mockRejectedValue(
      new ErrorApi({ title: 'No encontrado', status: 404, codigo: 'no_encontrado', errores: {} }),
    );
    await montaje.pintar(clubDe('PRESIDENTE'), '/ct/x', '/ct/:usuarioRolId', <DetalleCuerpoTecnico />);

    expect(montaje.texto).toContain('No encontramos a esta persona.');
  });

  it('en la ficha, los demás roles vuelven al inicio sin pedir ningún dato', async () => {
    const pedir = vi.spyOn(api, 'get');

    await montaje.pintar(clubDe('ENTRENADOR'), '/ct/e', '/ct/:usuarioRolId', <DetalleCuerpoTecnico />);

    expect(montaje.texto).toBe('otra pantalla');
    expect(pedir).not.toHaveBeenCalled();
  });
});
