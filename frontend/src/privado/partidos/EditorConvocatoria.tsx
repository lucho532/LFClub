import { NombrePersona } from '../../compartido/componentes/NombrePersona';
import { Aviso } from '../../compartido/componentes/Aviso';
import { Boton } from '../../compartido/componentes/Boton';

/** Un jugador que se puede marcar en la convocatoria. */
export interface JugadorConvocable {
  usuarioRolId: string;
  nombres: string;
  apellidos: string;
  /** Año de la categoría con la que aparece: la actual o, si ya está convocado, la copiada. */
  anio: number;
}

interface Props {
  clubId: string;
  jugadores: JugadorConvocable[];
  elegidos: string[];
  alCambiar: (elegidos: string[]) => void;
}

/**
 * La lista de jugadores de las categorías del partido, por categoría, con una casilla cada uno,
 * "Seleccionar todos", "Ninguno" y cuántos van seleccionados, como el lienzo (`Convocatoria`). Avisa
 * que solo los seleccionados verán el partido (§17.2). No guarda nada: quien la usa envía la lista.
 */
export function EditorConvocatoria({ clubId, jugadores, elegidos, alCambiar }: Props) {
  const anios = [...new Set(jugadores.map((jugador) => jugador.anio))].sort((a, b) => a - b);

  function conmutar(usuarioRolId: string, marcado: boolean) {
    alCambiar(marcado ? [...elegidos, usuarioRolId] : elegidos.filter((elegido) => elegido !== usuarioRolId));
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
      {anios.map((anio) => (
        <fieldset key={anio} className="opciones">
          <legend>Categoría {anio}</legend>
          <div className="casillas-convocatoria">
            {jugadores
              .filter((jugador) => jugador.anio === anio)
              .sort((a, b) => a.apellidos.localeCompare(b.apellidos, 'es'))
              .map((jugador) => (
                <label key={jugador.usuarioRolId} className="casilla">
                  <input
                    type="checkbox"
                    checked={elegidos.includes(jugador.usuarioRolId)}
                    onChange={(evento) => conmutar(jugador.usuarioRolId, evento.target.checked)}
                  />
                  <NombrePersona
                    nombre={`${jugador.nombres} ${jugador.apellidos}`}
                    enlace={null}
                    clubId={clubId}
                    textoVolver="Partido"
                  />
                </label>
              ))}
          </div>
        </fieldset>
      ))}
    </div>
  );
}
