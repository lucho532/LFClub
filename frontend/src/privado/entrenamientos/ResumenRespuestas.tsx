import type { RespuestaFamilia } from '../../compartido/api/tiposActividades';
import type { EnlaceFichaDto } from '../../compartido/api/tiposPersonas';
import { NombrePersona } from '../../compartido/componentes/NombrePersona';

/** Un jugador de la planilla con la respuesta de su familia. */
export interface ConRespuesta {
  usuarioRolId: string;
  nombres: string;
  apellidos: string;
  enlace?: EnlaceFichaDto | null;
  respuesta?: RespuestaFamilia | null;
}

interface Props {
  clubId: string;
  textoVolver: string;
  jugadores: ConRespuesta[];
}

const GRUPOS: { titulo: string; de: (jugador: ConRespuesta) => boolean }[] = [
  { titulo: 'Asistirán', de: (jugador) => jugador.respuesta === 'ASISTIRA' },
  { titulo: 'No asistirán', de: (jugador) => jugador.respuesta === 'NO_ASISTIRA' },
  { titulo: 'Sin responder', de: (jugador) => !jugador.respuesta },
];

/**
 * Lo que respondieron las familias, para quien ve la planilla (escenario 5.2): cuántos asistirán,
 * cuántos no y cuántos no han respondido, con los nombres de cada grupo. Sale de la planilla, que
 * nunca llega a la cuenta de un jugador.
 */
export function ResumenRespuestas({ clubId, textoVolver, jugadores }: Props) {
  return (
    <div className="resumen-respuestas">
      {GRUPOS.map((grupo) => {
        const delGrupo = jugadores.filter(grupo.de);
        return (
          <section key={grupo.titulo} className="columna">
            <h3>
              {grupo.titulo}: {delGrupo.length}
            </h3>
            {delGrupo.length > 0 && (
              <ul className="lista-de-inicio">
                {delGrupo.map((jugador) => (
                  <li key={jugador.usuarioRolId}>
                    <NombrePersona
                      nombre={`${jugador.nombres} ${jugador.apellidos}`}
                      enlace={jugador.enlace ?? null}
                      clubId={clubId}
                      textoVolver={textoVolver}
                    />
                  </li>
                ))}
              </ul>
            )}
          </section>
        );
      })}
    </div>
  );
}
