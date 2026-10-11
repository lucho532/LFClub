import { useState } from 'react';
import { api } from '../../compartido/api/cliente';
import { mensajeDe } from '../../compartido/api/errores';
import type { RespuestaFamilia } from '../../compartido/api/tiposActividades';
import { Aviso } from '../../compartido/componentes/Aviso';

const TEXTOS: Record<RespuestaFamilia, string> = { ASISTIRA: 'Asistirá', NO_ASISTIRA: 'No asistirá' };
const SIMBOLOS: Record<RespuestaFamilia, string> = { ASISTIRA: '✓', NO_ASISTIRA: '✕' };

interface Props {
  /** Ruta de la API `…/mi-respuesta` de la actividad. */
  ruta: string;
  actual?: RespuestaFamilia;
  /** Falso cuando ya empezó o está cancelada: se ve la respuesta sin poder cambiarla. */
  habilitado: boolean;
}

/**
 * Con qué responde la familia si su jugador asistirá: arriba "Confirma asistencia" y, en una línea,
 * una casilla verde con un chulo ("Asistirá") y una roja con una X ("No asistirá"). La opción
 * elegida se rellena de su color y además se escribe al lado ("Asistirá"), y cada casilla dice su
 * opción a los lectores de pantalla (RF-036). Si la API rechaza la respuesta, explica por qué. Solo
 * se pinta a la cuenta de un jugador; la API responde siempre por el jugador elegido.
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

  if (!habilitado && !respuesta) {
    return <span className="texto-suave texto-pequeno">Ya no se puede responder.</span>;
  }

  return (
    <div className="botones-respuesta">
      <span className="texto-suave texto-pequeno">{habilitado ? 'Confirma asistencia' : 'Tu respuesta'}</span>
      <div className="botones-respuesta-linea" role="group" aria-label="Confirma asistencia">
        {(['ASISTIRA', 'NO_ASISTIRA'] as const).map((opcion) => (
          <button
            key={opcion}
            type="button"
            className={`respuesta-casilla respuesta-${opcion === 'ASISTIRA' ? 'si' : 'no'}${respuesta === opcion ? ' encendido' : ''}`}
            aria-label={TEXTOS[opcion]}
            title={TEXTOS[opcion]}
            aria-pressed={respuesta === opcion}
            disabled={!habilitado || enviando}
            onClick={() => void responder(opcion)}
          >
            <span aria-hidden="true">{SIMBOLOS[opcion]}</span>
          </button>
        ))}
        <span className="texto-pequeno" aria-live="polite">
          {respuesta ? TEXTOS[respuesta] : 'Sin responder'}
        </span>
      </div>
      {error && <Aviso tono="error">{error}</Aviso>}
    </div>
  );
}
