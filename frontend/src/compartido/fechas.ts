// Fechas y horas de las actividades (spec 009). La API las entrega como fecha `AAAA-MM-DD` y hora
// `HH:mm` de pared en Colombia; aquí se escriben en español y se recorren por semanas sin
// depender de la zona horaria del dispositivo: todo el cálculo se hace en UTC.

const DIAS = ['domingo', 'lunes', 'martes', 'miércoles', 'jueves', 'viernes', 'sábado'];
const DIAS_CORTOS = ['dom', 'lun', 'mar', 'mié', 'jue', 'vie', 'sáb'];
const MESES = [
  'enero',
  'febrero',
  'marzo',
  'abril',
  'mayo',
  'junio',
  'julio',
  'agosto',
  'septiembre',
  'octubre',
  'noviembre',
  'diciembre',
];

/** Desfase de Colombia respecto de UTC, en horas: no tiene horario de verano. */
const DESFASE_COLOMBIA = -5;

function aFecha(texto: string): Date {
  return new Date(`${texto}T00:00:00Z`);
}

function aTexto(fecha: Date): string {
  return fecha.toISOString().slice(0, 10);
}

/** "martes 13 de octubre". */
export function diaDeLaSemana(fecha: string): string {
  const dia = aFecha(fecha);
  return `${DIAS[dia.getUTCDay()]} ${dia.getUTCDate()} de ${MESES[dia.getUTCMonth()]}`;
}

/** "mar" y "13", para la tira de la semana. */
export function diaCorto(fecha: string): { nombre: string; numero: number } {
  const dia = aFecha(fecha);
  return { nombre: DIAS_CORTOS[dia.getUTCDay()], numero: dia.getUTCDate() };
}

/** "13 de octubre de 2026". */
export function fechaLarga(fecha: string): string {
  const dia = aFecha(fecha);
  return `${dia.getUTCDate()} de ${MESES[dia.getUTCMonth()]} de ${dia.getUTCFullYear()}`;
}

/** "4:00 p. m." a partir de "16:00". */
export function horaCorta(hora: string): string {
  const [horas, minutos] = hora.split(':').map(Number);
  const sufijo = horas < 12 ? 'a. m.' : 'p. m.';
  const en12 = horas % 12 === 0 ? 12 : horas % 12;
  return `${en12}:${String(minutos).padStart(2, '0')} ${sufijo}`;
}

/** "4:00 p. m. a 5:30 p. m.". */
export function rangoDeHoras(inicio: string, fin: string): string {
  return `${horaCorta(inicio)} a ${horaCorta(fin)}`;
}

/** La fecha `n` días después (o antes, con `n` negativo). */
export function sumarDias(fecha: string, n: number): string {
  const dia = aFecha(fecha);
  dia.setUTCDate(dia.getUTCDate() + n);
  return aTexto(dia);
}

/** El lunes de la semana de esa fecha; el domingo pertenece a la semana que empezó el lunes anterior. */
export function inicioDeSemana(fecha: string): string {
  const diaSemana = aFecha(fecha).getUTCDay();
  return sumarDias(fecha, diaSemana === 0 ? -6 : 1 - diaSemana);
}

/** Los siete días de la semana que empieza ese lunes. */
export function diasDeLaSemana(lunes: string): string[] {
  return Array.from({ length: 7 }, (_, indice) => sumarDias(lunes, indice));
}

/** Hoy en Colombia, `AAAA-MM-DD`, sin depender de la zona del dispositivo. */
export function hoyEnColombia(ahora: Date = new Date()): string {
  return aTexto(new Date(ahora.getTime() + DESFASE_COLOMBIA * 60 * 60 * 1000));
}

/**
 * Si una actividad de esa fecha y hora de Colombia ya empezó. Solo sirve para ofrecer o no una
 * acción: la API lo vuelve a comprobar con su propio reloj.
 */
export function yaEmpezo(fecha: string, hora: string, ahora: Date = new Date()): boolean {
  const enColombia = new Date(ahora.getTime() + DESFASE_COLOMBIA * 60 * 60 * 1000).toISOString();
  return `${fecha}T${hora}` <= enColombia.slice(0, 16);
}
