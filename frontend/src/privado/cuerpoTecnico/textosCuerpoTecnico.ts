import type { MiembroCuerpoTecnicoDto } from '../../compartido/api/tiposCuerpoTecnico';
import type { CategoriaDeFichaBasicaDto } from '../../compartido/api/tiposPersonas';

export const NOMBRE_DEL_ROL: Record<MiembroCuerpoTecnicoDto['rol'], string> = {
  PRESIDENTE: 'Presidente',
  DIRECTIVO: 'Directivo',
  ENTRENADOR: 'Entrenador',
};

/** «2014 · A, B», o solo «2014» si entrena la categoría en general. */
export function textoDeCategoria(categoria: CategoriaDeFichaBasicaDto): string {
  return categoria.equipos.length > 0 ? `${categoria.anio} · ${categoria.equipos.join(', ')}` : `${categoria.anio}`;
}

/** Las categorías en una línea; sin ninguna, lo dice según el rol. */
export function textoDeCategorias(miembro: Pick<MiembroCuerpoTecnicoDto, 'rol' | 'categorias'>): string {
  if (miembro.categorias.length > 0) {
    return miembro.categorias.map(textoDeCategoria).join(' · ');
  }

  return miembro.rol === 'ENTRENADOR' ? 'Sin categorías asignadas' : '—';
}
