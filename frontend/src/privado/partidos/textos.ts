import type { PartidoDto, PartidoResumenDto } from '../../compartido/api/tiposActividades';
import type { IndicacionDto } from '../../compartido/api/tiposResultadoFamilia';
import { fechaYHora } from '../../compartido/formato';

/** El rival, o "Por definir" si todavía no se sabe. */
export function nombreRival(partido: PartidoResumenDto): string {
  return partido.rival ?? 'Por definir';
}

/**
 * El marcador escrito según la condición: "La Pecosa 3 - 1 Equipo C" de local y
 * "Equipo C 1 - 3 La Pecosa" de visitante. Vacío si el partido no tiene resultado.
 */
export function textoDeMarcador(partido: PartidoResumenDto, nombreClub: string): string {
  if (partido.golesClub === null || partido.golesClub === undefined) {
    return '';
  }

  const rival = partido.golesRival ?? 0;
  return partido.condicion === 'LOCAL'
    ? `${nombreClub} ${partido.golesClub} - ${rival} ${nombreRival(partido)}`
    : `${nombreRival(partido)} ${rival} - ${partido.golesClub} ${nombreClub}`;
}

/** "Amistoso" o "Torneo: Copa Ciudad". */
export function textoDeTipo(partido: PartidoResumenDto): string {
  return partido.tipo === 'TORNEO' ? `Torneo: ${partido.nombreTorneo ?? ''}` : 'Amistoso';
}

/**
 * El estado del resultado escrito, para quien ve la planilla (spec 010, RF-034): "Falta el
 * resultado", "Publicado por una familia", "Corregido por quien entrena" (lo publicó una familia y
 * después lo cambió quien entrena) o "Registrado por quien entrena". Vacío si no aplica.
 */
export function estadoDelResultado(partido: PartidoDto): string {
  if (partido.faltaResultado) {
    return 'Falta el resultado';
  }

  if (partido.estado !== 'JUGADO' || !partido.origenResultado) {
    return '';
  }

  if (partido.origenResultado === 'FAMILIA') {
    return 'Publicado por una familia';
  }

  return partido.publicacion?.porFamilia ? 'Corregido por quien entrena' : 'Registrado por quien entrena';
}

/** Lo que la API responde a la familia, dicho para la pantalla (los demás errores, tal como vienen). */
export const textosResultadoFamilia = {
  corregido: 'Quien entrena corrigió lo de tu jugador. Si algo no está bien, habla con quien entrena.',
  otroMarcador: 'Otra familia publicó el marcador primero. Ya puedes indicar lo de tu jugador.',
  guardado: 'Guardado. Gracias por contarlo.',
  sinIndicar: 'Sin indicar',
};

/** "Indicado por Ana Pérez, 12 oct, 10:05" o "Sin indicar" (spec 010, RF-009). */
export function textoDeIndicacion(indicacion: IndicacionDto | undefined): string {
  return indicacion ? `Indicado por ${indicacion.autor}, ${fechaYHora(indicacion.fecha)}` : textosResultadoFamilia.sinIndicar;
}
