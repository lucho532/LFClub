import { api } from '../../compartido/api/cliente';
import type { CategoriaDetalleDto } from '../../compartido/api/tiposCategorias';
import { useCarga } from '../../compartido/api/useCarga';
import type { JugadorConvocable } from './EditorConvocatoria';

/**
 * Los jugadores de las categorías elegidas al crear un partido, para convocarlos antes de que el
 * partido exista. Los pide al detalle de cada categoría, que solo se entrega a quien la puede ver.
 * Es solo comodidad: al guardar, la API vuelve a comprobar cada jugador (RF-009).
 */
export function useJugadoresDeCategorias(clubId: string, categoriaIds: string[]): JugadorConvocable[] {
  const clave = [...categoriaIds].sort().join(',');
  const carga = useCarga(`${clubId}/convocables/${clave}`, async () => {
    const detalles = await Promise.all(
      categoriaIds.map((categoriaId) =>
        api.get<CategoriaDetalleDto>(`/api/clubes/${clubId}/categorias/${categoriaId}`),
      ),
    );
    return detalles.flatMap((detalle) =>
      detalle.jugadores.map((jugador) => ({
        usuarioRolId: jugador.usuarioRolId,
        nombres: jugador.nombres,
        apellidos: jugador.apellidos,
        anio: detalle.anio,
      })),
    );
  });

  return carga.datos ?? [];
}
