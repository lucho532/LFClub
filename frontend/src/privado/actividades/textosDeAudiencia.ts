import type { EquipoDeActividadDto } from '../../compartido/api/tiposAudiencia';

/** Un equipo de la lista de una actividad: su nombre, el año de su categoría y si sigue activo. */
export function textoDeEquipo(equipo: EquipoDeActividadDto): string {
  return `${equipo.nombre}${equipo.activo ? '' : ' (desactivado)'}`;
}

/**
 * A quién va dirigida una actividad, para decirlo con palabras: "2014 · Equipo A, Equipo B" o
 * "2014 y 2015". Sin equipos es la categoría entera. El equipo desactivado se escribe como tal.
 */
export function textoDeAudiencia(
  categorias: { anio: number }[],
  equipos: EquipoDeActividadDto[] | undefined,
): string {
  if (!equipos || equipos.length === 0) {
    return categorias.map((categoria) => String(categoria.anio)).join(' y ');
  }

  const anios = [...new Set(equipos.map((equipo) => equipo.anio))].sort((a, b) => a - b);
  return anios
    .map((anio) => {
      const nombres = equipos.filter((equipo) => equipo.anio === anio).map((equipo) => `Equipo ${textoDeEquipo(equipo)}`);
      return `${anio} · ${nombres.join(', ')}`;
    })
    .join(' · ');
}
