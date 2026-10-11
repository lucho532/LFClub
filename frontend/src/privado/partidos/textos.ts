import type { PartidoResumenDto } from '../../compartido/api/tiposActividades';

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
