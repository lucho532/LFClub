import { describe, expect, it } from 'vitest';
import {
  diaDeLaSemana,
  diasDeLaSemana,
  fechaLarga,
  horaCorta,
  hoyEnColombia,
  inicioDeSemana,
  rangoDeHoras,
  sumarDias,
} from '../src/compartido/fechas';

describe('fechas y horas en español', () => {
  it('escribe el día de la semana con el día y el mes', () => {
    expect(diaDeLaSemana('2026-10-13')).toBe('martes 13 de octubre');
    expect(diaDeLaSemana('2026-11-01')).toBe('domingo 1 de noviembre');
  });

  it('escribe la fecha larga con el año', () => {
    expect(fechaLarga('2026-12-31')).toBe('31 de diciembre de 2026');
  });

  it('escribe la hora en formato de doce horas', () => {
    expect(horaCorta('16:00')).toBe('4:00 p. m.');
    expect(horaCorta('09:05')).toBe('9:05 a. m.');
    expect(horaCorta('00:30')).toBe('12:30 a. m.');
    expect(horaCorta('12:00')).toBe('12:00 p. m.');
  });

  it('escribe un rango de horas', () => {
    expect(rangoDeHoras('16:00', '17:30')).toBe('4:00 p. m. a 5:30 p. m.');
  });
});

describe('semanas', () => {
  it('el lunes de la semana de un miércoles es dos días antes', () => {
    expect(inicioDeSemana('2026-10-14')).toBe('2026-10-12');
  });

  it('un lunes es el inicio de su propia semana', () => {
    expect(inicioDeSemana('2026-10-12')).toBe('2026-10-12');
  });

  it('un domingo pertenece a la semana del lunes anterior', () => {
    expect(inicioDeSemana('2026-10-18')).toBe('2026-10-12');
  });

  it('una semana que cruza de mes', () => {
    expect(diasDeLaSemana('2026-10-26')).toEqual([
      '2026-10-26',
      '2026-10-27',
      '2026-10-28',
      '2026-10-29',
      '2026-10-30',
      '2026-10-31',
      '2026-11-01',
    ]);
  });

  it('una semana que cruza de año', () => {
    const dias = diasDeLaSemana(inicioDeSemana('2027-01-01'));
    expect(dias[0]).toBe('2026-12-28');
    expect(dias[6]).toBe('2027-01-03');
  });

  it('suma y resta días', () => {
    expect(sumarDias('2026-12-31', 1)).toBe('2027-01-01');
    expect(sumarDias('2026-03-01', -1)).toBe('2026-02-28');
  });
});

describe('hoy en Colombia', () => {
  it('a las 23:30 de Colombia sigue siendo el mismo día aunque en UTC ya sea el siguiente', () => {
    expect(hoyEnColombia(new Date('2026-10-14T04:30:00Z'))).toBe('2026-10-13');
  });

  it('desde las 5:00 UTC ya es el día siguiente en Colombia', () => {
    expect(hoyEnColombia(new Date('2026-10-14T05:00:00Z'))).toBe('2026-10-14');
  });
});
