import { api } from '../../compartido/api/cliente';
import { ErrorApi } from '../../compartido/api/errores';
import type { GrupoDeJugadoresDto } from '../../compartido/api/tiposJugadores';
import { useCarga } from '../../compartido/api/useCarga';
import { Aviso } from '../../compartido/componentes/Aviso';
import { Tarjeta } from '../../compartido/componentes/Tarjeta';
import { useClub } from '../contextoClub';
import { TablaDeGrupo } from './TablaDeGrupo';
import { SIN_CATEGORIAS_ASIGNADAS, tituloDeGrupo } from './textos';

/**
 * Apartado "Jugadores" del club (spec 007, historia 1): los jugadores activos agrupados por
 * categoría, una tarjeta por grupo, con "Sin categoría" al final. Qué grupos llegan lo decide la
 * API según el rol: el entrenador recibe solo sus categorías. Quien abre la dirección a mano sin
 * poder verla (la cuenta de un jugador) recibe un aviso y ninguna lista.
 */
export function ListaJugadores() {
  const { club } = useClub();
  const grupos = useCarga(`${club.clubId}/jugadores`, () =>
    api.get<GrupoDeJugadoresDto[]>(`/api/clubes/${club.clubId}/jugadores`),
  );
  const sinPermiso = grupos.fallo instanceof ErrorApi && grupos.fallo.codigo === 'rol_no_autorizado';
  const vacio = club.miRol === 'ENTRENADOR' ? SIN_CATEGORIAS_ASIGNADAS : 'El club todavía no tiene jugadores.';

  return (
    <>
      <div className="cabecera">
        <h1>Jugadores</h1>
      </div>
      {sinPermiso ? (
        <Aviso tono="aviso">Tu rol en este club no permite ver la lista de jugadores.</Aviso>
      ) : (
        grupos.error && <Aviso tono="error">{grupos.error}</Aviso>
      )}
      {!grupos.datos && grupos.cargando && <p className="texto-suave">Cargando…</p>}
      {grupos.datos?.length === 0 && (
        <Tarjeta>
          <p className="texto-suave">{vacio}</p>
        </Tarjeta>
      )}
      {grupos.datos?.map((grupo) => (
        <Tarjeta key={grupo.categoria?.categoriaId ?? 'sin-categoria'} titulo={tituloDeGrupo(grupo)}>
          <TablaDeGrupo clubId={club.clubId} titulo={tituloDeGrupo(grupo)} jugadores={grupo.jugadores} />
        </Tarjeta>
      ))}
    </>
  );
}
