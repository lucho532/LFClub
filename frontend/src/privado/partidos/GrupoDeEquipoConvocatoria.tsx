import { Boton } from '../../compartido/componentes/Boton';
import { NombrePersona } from '../../compartido/componentes/NombrePersona';
import { EtiquetaDePertenencia } from '../actividades/EtiquetaDePertenencia';

/** Un jugador de un grupo de la convocatoria. */
export interface JugadorDeGrupo {
  usuarioRolId: string;
  nombres: string;
  apellidos: string;
  /** Lo sumó quien convoca y no es de las categorías del partido: se lee "Añadido" en texto. */
  anadido?: boolean;
}

interface Props {
  clubId: string;
  /** El nombre del grupo: "Equipo A · 2014", "Sin equipo" o "Añadidos". */
  titulo: string;
  jugadores: JugadorDeGrupo[];
  elegidos: string[];
  /** Marca o desmarca a un jugador; es la misma marca en todos los grupos en los que aparece. */
  alConmutar: (usuarioRolId: string, marcado: boolean) => void;
  /** Marca o desmarca a todo el grupo. */
  alMarcarGrupo: (usuarioRolIds: string[], marcado: boolean) => void;
}

/**
 * Un grupo de la convocatoria por equipo (spec 013, RF-009): su nombre, "Marcar a todos" y "Quitar a
 * todos" y una casilla por jugador. Un jugador de varios equipos aparece en cada grupo con la misma
 * marca. No guarda nada: quien lo usa envía la lista completa.
 */
export function GrupoDeEquipoConvocatoria({ clubId, titulo, jugadores, elegidos, alConmutar, alMarcarGrupo }: Props) {
  const ids = jugadores.map((jugador) => jugador.usuarioRolId);
  const marcados = ids.filter((id) => elegidos.includes(id)).length;

  return (
    <fieldset className="opciones grupo-convocatoria">
      <legend>{titulo}</legend>
      <div className="cabecera">
        <span className="texto-suave">{`${marcados} de ${jugadores.length}`}</span>
        <div className="fila">
          <Boton variante="secundario" onClick={() => alMarcarGrupo(ids, true)} disabled={marcados === ids.length}>
            Marcar a todos
          </Boton>
          <Boton variante="secundario" onClick={() => alMarcarGrupo(ids, false)} disabled={marcados === 0}>
            Quitar a todos
          </Boton>
        </div>
      </div>
      <div className="casillas-convocatoria">
        {[...jugadores]
          .sort((a, b) => a.apellidos.localeCompare(b.apellidos, 'es'))
          .map((jugador) => (
            <label key={jugador.usuarioRolId} className="casilla">
              <input
                type="checkbox"
                checked={elegidos.includes(jugador.usuarioRolId)}
                onChange={(evento) => alConmutar(jugador.usuarioRolId, evento.target.checked)}
              />
              <NombrePersona
                nombre={`${jugador.nombres} ${jugador.apellidos}`}
                enlace={null}
                clubId={clubId}
                textoVolver="Partido"
              />
              {jugador.anadido && <EtiquetaDePertenencia pertenencia="ANADIDO" />}
            </label>
          ))}
      </div>
    </fieldset>
  );
}
