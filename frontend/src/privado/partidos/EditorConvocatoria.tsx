import { Aviso } from '../../compartido/componentes/Aviso';
import { Boton } from '../../compartido/componentes/Boton';
import type { EquipoDeActividadDto } from '../../compartido/api/tiposAudiencia';
import type { JugadorSumableDto } from '../../compartido/api/tiposAudiencia';
import { BuscadorDeJugadores } from '../actividades/BuscadorDeJugadores';
import { GrupoDeEquipoConvocatoria } from './GrupoDeEquipoConvocatoria';

/** Un jugador que se puede marcar en la convocatoria. */
export interface JugadorConvocable {
  usuarioRolId: string;
  nombres: string;
  apellidos: string;
  /** Año de la categoría con la que aparece: la actual o, si ya está convocado, la copiada. */
  anio: number;
  /** Los equipos activos en los que juega hoy. */
  equipoIds?: string[];
  /** Es un convocado de otra categoría que sumó quien convoca. */
  anadido?: boolean;
}

interface Props {
  clubId: string;
  jugadores: JugadorConvocable[];
  /** Los equipos activos de las categorías del partido; sin ellos, la lista va por categoría. */
  equipos?: EquipoDeActividadDto[];
  elegidos: string[];
  alCambiar: (elegidos: string[]) => void;
  /** Si se indica, se ofrece el buscador para sumar a un jugador que no está en la lista. */
  alSumar?: (jugador: JugadorSumableDto) => void;
}

interface Grupo {
  clave: string;
  titulo: string;
  jugadores: JugadorConvocable[];
}

/** Los grupos de la lista: por equipo (más "Sin equipo") o, si no hay equipos, por categoría; y los añadidos. */
function gruposDe(jugadores: JugadorConvocable[], equipos: EquipoDeActividadDto[]): Grupo[] {
  const propios = jugadores.filter((jugador) => !jugador.anadido);
  const anadidos = jugadores.filter((jugador) => jugador.anadido);
  const grupos: Grupo[] = [];

  if (equipos.length > 0) {
    const ordenados = [...equipos].sort((a, b) => a.anio - b.anio || a.nombre.localeCompare(b.nombre, 'es'));
    for (const equipo of ordenados) {
      grupos.push({
        clave: equipo.equipoId,
        titulo: `Equipo ${equipo.nombre} · ${equipo.anio}`,
        jugadores: propios.filter((jugador) => jugador.equipoIds?.includes(equipo.equipoId)),
      });
    }

    const idsDeEquipos = new Set(equipos.map((equipo) => equipo.equipoId));
    const sinEquipo = propios.filter((jugador) => !(jugador.equipoIds ?? []).some((id) => idsDeEquipos.has(id)));
    if (sinEquipo.length > 0) {
      grupos.push({ clave: 'sin-equipo', titulo: 'Sin equipo', jugadores: sinEquipo });
    }
  } else {
    const anios = [...new Set(propios.map((jugador) => jugador.anio))].sort((a, b) => a - b);
    for (const anio of anios) {
      grupos.push({
        clave: `categoria-${anio}`,
        titulo: `Categoría ${anio}`,
        jugadores: propios.filter((jugador) => jugador.anio === anio),
      });
    }
  }

  if (anadidos.length > 0) {
    grupos.push({ clave: 'anadidos', titulo: 'Añadidos', jugadores: anadidos });
  }

  return grupos;
}

/**
 * La lista de jugadores a convocar, agrupada por equipo cuando las categorías del partido tienen
 * equipos (y por categoría si no), con "Marcar a todos" por grupo, "Seleccionar todos", "Ninguno" y
 * cuántos van seleccionados, como el lienzo (`Convocatoria`). Un jugador de varios equipos aparece
 * en cada uno con la misma marca y se convoca una sola vez. Avisa que solo los seleccionados verán
 * el partido (§17.2). No guarda nada: quien la usa envía la lista.
 */
export function EditorConvocatoria({ clubId, jugadores, equipos = [], elegidos, alCambiar, alSumar }: Props) {
  const grupos = gruposDe(jugadores, equipos);

  function conmutar(usuarioRolId: string, marcado: boolean) {
    alCambiar(
      marcado
        ? [...new Set([...elegidos, usuarioRolId])]
        : elegidos.filter((elegido) => elegido !== usuarioRolId),
    );
  }

  function marcarGrupo(ids: string[], marcado: boolean) {
    alCambiar(marcado ? [...new Set([...elegidos, ...ids])] : elegidos.filter((elegido) => !ids.includes(elegido)));
  }

  return (
    <div className="columna editor-convocatoria">
      <Aviso tono="info">Solo los jugadores que selecciones verán este partido.</Aviso>
      <div className="cabecera">
        <strong>
          {elegidos.length} de {jugadores.length} seleccionados
        </strong>
        <div className="fila">
          <Boton variante="secundario" onClick={() => alCambiar(jugadores.map((jugador) => jugador.usuarioRolId))}>
            Seleccionar todos
          </Boton>
          <Boton variante="secundario" onClick={() => alCambiar([])}>
            Ninguno
          </Boton>
        </div>
      </div>
      {jugadores.length === 0 && <p className="texto-suave">Las categorías elegidas no tienen jugadores activos.</p>}
      {alSumar && (
        <BuscadorDeJugadores
          clubId={clubId}
          excluidos={jugadores.map((jugador) => jugador.usuarioRolId)}
          alElegir={alSumar}
        />
      )}
      {grupos.map((grupo) => (
        <GrupoDeEquipoConvocatoria
          key={grupo.clave}
          clubId={clubId}
          titulo={grupo.titulo}
          jugadores={grupo.jugadores}
          elegidos={elegidos}
          alConmutar={conmutar}
          alMarcarGrupo={marcarGrupo}
        />
      ))}
    </div>
  );
}
