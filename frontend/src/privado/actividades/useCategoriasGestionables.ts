import { api } from '../../compartido/api/cliente';
import type { ClubDto } from '../../compartido/api/tipos';
import type { CategoriaDto } from '../../compartido/api/tiposCategorias';
import { useCarga } from '../../compartido/api/useCarga';
import { puedeVerCategorias } from '../categorias/textos';

/** Una categoría que se puede elegir en el formulario de una actividad. */
export interface CategoriaGestionable {
  categoriaId: string;
  anio: number;
}

/**
 * Las categorías activas para las que la persona puede crear actividades: el presidente, todas;
 * los demás, aquellas en cuya lista de entrenadores figura (research §13). Es solo comodidad para
 * no ofrecer lo que la API rechazaría: la API lo vuelve a comprobar. La cuenta de un jugador no
 * pide nada y recibe una lista vacía.
 */
export function useCategoriasGestionables(club: ClubDto): { categorias: CategoriaGestionable[]; cargando: boolean } {
  const pide = puedeVerCategorias(club.miRol);
  const carga = useCarga(`${club.clubId}/categorias-gestionables/${pide}`, () =>
    pide ? api.get<CategoriaDto[]>(`/api/clubes/${club.clubId}/categorias`) : Promise.resolve([]),
  );

  const categorias = (carga.datos ?? [])
    .filter((categoria) => categoria.activa)
    .filter(
      (categoria) =>
        club.miRol === 'PRESIDENTE' ||
        categoria.entrenadores.some((entrenador) => entrenador.usuarioRolId === club.miUsuarioRolId),
    )
    .map((categoria) => ({ categoriaId: categoria.categoriaId, anio: categoria.anio }))
    .sort((a, b) => a.anio - b.anio);

  return { categorias, cargando: carga.cargando };
}
