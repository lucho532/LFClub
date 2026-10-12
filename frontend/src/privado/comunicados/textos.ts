import type { Rol } from '../../compartido/api/tipos';
import type {
  AlcanceComunicado,
  ComunicadoPublicadoDto,
  DestinoDeComunicadoDto,
} from '../../compartido/api/tiposComunicados';

/** Límites de lo que se escribe; la API los vuelve a comprobar (spec 011, RF-002). */
export const LIMITE_DEL_TITULO = 100;
export const LIMITE_DEL_TEXTO = 1000;

/** Los roles que publican comunicados (§14.4). */
export function puedePublicarComunicados(rol: Rol | undefined): boolean {
  return rol === 'PRESIDENTE' || rol === 'DIRECTIVO' || rol === 'ENTRENADOR';
}

/** Solo el PRESIDENTE y el DIRECTIVO se dirigen a todo el club. */
export function puedeDirigirseATodoElClub(rol: Rol | undefined): boolean {
  return rol === 'PRESIDENTE' || rol === 'DIRECTIVO';
}

/** "A", "A y B" o "A, B y C". */
export function unir(elementos: string[]): string {
  if (elementos.length <= 1) {
    return elementos.join('');
  }

  return `${elementos.slice(0, -1).join(', ')} y ${elementos[elementos.length - 1]}`;
}

/** "Todo el club", "Categoría 2015", "Categorías 2014 y 2016" o "Equipos A y B de la 2015". */
export function textoDeAudiencia(alcance: AlcanceComunicado, destinos: DestinoDeComunicadoDto[]): string {
  if (alcance === 'CLUB') {
    return 'Todo el club';
  }

  if (alcance === 'CATEGORIAS') {
    const anios = destinos.map((destino) => destino.categoriaNombre);
    return `${anios.length === 1 ? 'Categoría' : 'Categorías'} ${unir(anios)}`;
  }

  const nombres = destinos.map((destino) => destino.equipoNombre ?? '').filter((nombre) => nombre !== '');
  const categoria = destinos[0]?.categoriaNombre ?? '';
  return `${nombres.length === 1 ? 'Equipo' : 'Equipos'} ${unir(nombres)} de la ${categoria}`;
}

/** "1 familia" o "12 familias". */
export function textoDeFamilias(cuantas: number): string {
  return cuantas === 1 ? '1 familia' : `${cuantas} familias`;
}

/** El estado escrito de lo publicado; quién y cuándo lo retiró se pinta aparte. */
export function estadoDeComunicado(comunicado: ComunicadoPublicadoDto): 'Publicado' | 'Retirado' {
  return comunicado.retirado ? 'Retirado' : 'Publicado';
}

/** Textos de las pantallas de comunicados, juntos para no repetirlos. */
export const textosComunicados = {
  tituloFamilia: 'Novedades',
  tituloEquipo: 'Comunicados publicados',
  vacioFamilia: 'Todavía no hay novedades del club.',
  vacioEquipo: 'Todavía no se ha publicado ningún comunicado.',
  sinLeer: 'Sin leer',
  etiquetaComunicado: 'Comunicado',
  etiquetaAviso: 'Aviso',
  actividadNoDisponible: 'Esa actividad ya no está disponible para ti.',
  leido: 'Leído',
  marcarTodos: 'Marcar todos como leídos',
  nuevo: 'Nuevo comunicado',
  retirar: 'Retirar',
  confirmarRetiro: 'Dejará de verse en la campana de las familias. Esto no se puede deshacer.',
  verMas: 'Ver más',
  elegirCategoria: 'Elige al menos una categoría',
  elegirEquipo: 'Elige al menos un equipo',
  sinJugadores: 'Esa categoría no tiene jugadores activos',
};
