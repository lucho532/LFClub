import type { PosicionJugador } from '../../compartido/api/tiposCarta';

/** Las posiciones escritas, para elegir. */
export const NOMBRES_DE_POSICION: Record<PosicionJugador, string> = {
  PORTERO: 'Portero',
  DEFENSA: 'Defensa',
  MEDIOCAMPISTA: 'Mediocampista',
  DELANTERO: 'Delantero',
};

/** Las seis estadísticas en el orden del contrato, con su sigla y su nombre escrito. */
export const CAMPOS_DE_ESTADISTICAS = [
  { campo: 'ritmo', sigla: 'RIT', nombre: 'Ritmo' },
  { campo: 'tiro', sigla: 'TIR', nombre: 'Tiro' },
  { campo: 'pase', sigla: 'PAS', nombre: 'Pase' },
  { campo: 'regate', sigla: 'REG', nombre: 'Regate' },
  { campo: 'defensa', sigla: 'DEF', nombre: 'Defensa' },
  { campo: 'fisico', sigla: 'FÍS', nombre: 'Físico' },
] as const;

export const textosCarta = {
  titulo: 'Carta',
  verCarta: 'Ver carta',
  noEncontrada: 'No encontramos esta carta.',
  sinEstadisticas: 'Todavía no tiene estadísticas: quien entrena su categoría las pone.',
  rareza: 'Rareza por asistencia a los entrenamientos de este año',
  dorsalYPosicion: 'Dorsal y posición',
  sinPosicion: 'Sin posición',
  estadisticas: 'Estadísticas',
  quitarEstadisticas: 'Quitar estadísticas',
  ayudaEstadisticas: 'Las seis de 1 a 99, o ninguna.',
  descargar: 'Descargar imagen',
  preparando: 'Preparando la imagen…',
  guardado: 'Guardado.',
};
