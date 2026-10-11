import { useState } from 'react';
import { api } from '../../compartido/api/cliente';
import { mensajeDe } from '../../compartido/api/errores';
import type { RespuestaFamilia } from '../../compartido/api/tiposActividades';
import { Aviso } from '../../compartido/componentes/Aviso';

const TEXTOS: Record<RespuestaFamilia, string> = { ASISTIRA: 'Asistirá', NO_ASISTIRA: 'No asistirá' };

interface Props {
  /** Ruta de la API `…/mi-respuesta` de la actividad. */
  ruta: string;
  actual?: RespuestaFamilia;
  /** Falso cuando ya empezó o está cancelada: se ve la respuesta sin poder cambiarla. */
  habilitado: boolean;
}

/**
 * Los dos botones del lienzo (`M-Calendario`) con los que la familia dice si su jugador asistirá:
 * "Asistirá" y "No asistirá". La opción elegida se lee también como texto ("Respondiste: …") y con
 * `aria-pressed`. Si la API rechaza la respuesta, explica por qué. Solo se pinta a la cuenta de un
 * jugador; la API responde siempre por el jugador elegido.
 */
export function BotonesRespuesta({ ruta, actual, habilitado }: Props) {
  const [respuesta, setRespuesta] = useState<RespuestaFamilia | undefined>(actual);
  const [enviando, setEnviando] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function responder(nueva: RespuestaFamilia) {
    setEnviando(true);
    setError(null);
    try {
      const guardada = await api.put<{ miRespuesta?: RespuestaFamilia }>(ruta, { respuesta: nueva });
      setRespuesta(guardada.miRespuesta);
    } catch (fallo) {
      setError(mensajeDe(fallo));
    } finally {
      setEnviando(false);
    }
  }

  return (
    <div className="columna botones-respuesta">
      {(habilitado || respuesta) && (
        <div className="fila">
          {(['ASISTIRA', 'NO_ASISTIRA'] as const).map((opcion) => (
            <button
              key={opcion}
              type="button"
              className={`boton-conmutable${respuesta === opcion ? ' encendido' : ''}`}
              aria-pressed={respuesta === opcion}
              disabled={!habilitado || enviando}
              onClick={() => void responder(opcion)}
            >
              {TEXTOS[opcion]}
            </button>
          ))}
        </div>
      )}
      <span className="texto-suave texto-pequeno">
        {respuesta ? `Respondiste: ${TEXTOS[respuesta]}` : habilitado ? 'Todavía no respondes.' : 'Ya no se puede responder.'}
      </span>
      {error && <Aviso tono="error">{error}</Aviso>}
    </div>
  );
}
