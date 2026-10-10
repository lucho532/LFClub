import { Link } from 'react-router-dom';
import { api } from '../../compartido/api/cliente';
import type { CategoriaDto } from '../../compartido/api/tiposCategorias';
import { useCarga } from '../../compartido/api/useCarga';
import { Aviso } from '../../compartido/componentes/Aviso';
import { Tarjeta } from '../../compartido/componentes/Tarjeta';
import { cuantosJugadores } from '../categorias/textos';
import { SIN_CATEGORIAS_ASIGNADAS } from '../jugadores/textos';

interface Props {
  clubId: string;
  /** El integrante del entrenador en este club, para encontrar su propia asignación. */
  miUsuarioRolId: string;
}

/** "Dirige: A, B" con los equipos de su propia asignación, o "Toda la categoría". */
function queDirige(categoria: CategoriaDto, miUsuarioRolId: string): string {
  const equipos = categoria.entrenadores.find((entrenador) => entrenador.usuarioRolId === miUsuarioRolId)?.equipos ?? [];
  return equipos.length === 0 ? 'Toda la categoría' : `Dirige: ${equipos.map((equipo) => equipo.nombre).join(', ')}`;
}

/**
 * Inicio del entrenador (spec 007, RF-012): sus categorías activas asignadas, cada una con cuántos
 * jugadores tiene y qué equipos dirige él, y el enlace a su detalle, que lista a sus jugadores
 * (supuesto 3 del plan). No pide nada nuevo a la API: la lista de categorías ya le llega limitada a
 * las suyas.
 */
export function InicioEntrenador({ clubId, miUsuarioRolId }: Props) {
  const categorias = useCarga(`${clubId}/categorias`, () => api.get<CategoriaDto[]>(`/api/clubes/${clubId}/categorias`));

  return (
    <Tarjeta titulo="Mis categorías">
      {categorias.error && <Aviso tono="error">{categorias.error}</Aviso>}
      {!categorias.datos && categorias.cargando && <p className="texto-suave">Cargando…</p>}
      {categorias.datos?.length === 0 && <p className="texto-suave">{SIN_CATEGORIAS_ASIGNADAS}</p>}
      {categorias.datos && categorias.datos.length > 0 && (
        <ul className="lista-de-inicio">
          {categorias.datos.map((categoria) => (
            <li key={categoria.categoriaId}>
              <Link to={`/club/${clubId}/categorias/${categoria.categoriaId}`}>
                <strong>Categoría {categoria.anio}</strong>
              </Link>
              <span className="texto-suave">
                {cuantosJugadores(categoria.numeroJugadores)} · {queDirige(categoria, miUsuarioRolId)}
              </span>
            </li>
          ))}
        </ul>
      )}
    </Tarjeta>
  );
}
