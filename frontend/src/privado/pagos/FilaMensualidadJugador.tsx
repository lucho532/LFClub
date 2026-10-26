import { useState, type FormEvent } from 'react';
import { ErrorApi, mensajeDe } from '../../compartido/api/errores';
import { Boton } from '../../compartido/componentes/Boton';
import { Campo } from '../../compartido/componentes/Campo';
import { EtiquetaEstado } from '../../compartido/componentes/EtiquetaEstado';
import { IconoLapiz } from '../../compartido/componentes/Iconos';
import { formatoMoneda, leerPesos } from './formatoMoneda';

interface Props {
  nombre: string;
  /** Valor propio vigente del jugador, o nulo si paga el del club. */
  valorPropio: number | null;
  valorClub: number | null;
  /** Falso para quien solo consulta (DIRECTIVO): sin lápiz. */
  editable: boolean;
  /** Fija el valor propio (un número) o lo quita (nulo); si falla, lanza el error. */
  alGuardar: (valor: number | null) => Promise<void>;
}

/**
 * La mensualidad de un jugador en una sola línea: su nombre, si paga el valor del club o uno propio y un
 * lápiz. Nada se edita hasta pulsar el lápiz: entonces la misma línea pasa a tener el campo del valor con
 * «Guardar», «Usar valor del club» (si tiene valor propio) y «Cancelar». Cada línea se guarda sola. Solo
 * cambia los cargos que se generen después.
 */
export function FilaMensualidadJugador({ nombre, valorPropio, valorClub, editable, alGuardar }: Props) {
  const inicial = valorPropio === null ? '' : String(valorPropio);
  const [editando, setEditando] = useState(false);
  const [texto, setTexto] = useState(inicial);
  const [error, setError] = useState<string>();
  const [enviando, setEnviando] = useState<'guardar' | 'quitar' | null>(null);
  const cambio = texto.trim() !== inicial;

  function cerrar() {
    setEditando(false);
    setTexto(inicial);
    setError(undefined);
  }

  async function aplicar(valor: number | null, accion: 'guardar' | 'quitar') {
    setError(undefined);
    setEnviando(accion);
    try {
      await alGuardar(valor);
      setEditando(false);
    } catch (fallo) {
      setError(fallo instanceof ErrorApi && fallo.errorDe('valor') ? fallo.errorDe('valor') : mensajeDe(fallo));
    } finally {
      setEnviando(null);
    }
  }

  function guardar(evento: FormEvent) {
    evento.preventDefault();
    const valor = leerPesos(texto);
    if (valor === null) {
      setError('Escribe un valor entero de pesos, mayor que cero.');
      return;
    }

    void aplicar(valor, 'guardar');
  }

  const etiqueta =
    valorPropio === null ? (
      <EtiquetaEstado
        tono="neutro"
        texto={valorClub === null ? 'Sin valor del club' : `Valor del club: ${formatoMoneda(valorClub)}`}
      />
    ) : (
      <EtiquetaEstado tono="info" texto={`Valor propio: ${formatoMoneda(valorPropio)}`} />
    );

  return (
    <li className="linea-mensualidad-jugador">
      {editando ? (
        <form className="fila-con-campos" onSubmit={guardar} noValidate>
          <Campo
            etiqueta={`Valor de ${nombre}`}
            inputMode="numeric"
            placeholder={valorClub === null ? 'Valor propio' : `Valor del club: ${formatoMoneda(valorClub)}`}
            valor={texto}
            alCambiar={(nuevo) => {
              setTexto(nuevo);
              setError(undefined);
            }}
            error={error}
          />
          <Boton
            type="submit"
            disabled={!cambio || enviando !== null}
            cargando={enviando === 'guardar'}
            textoCargando="Guardando…"
            aria-label={`Guardar el valor de ${nombre}`}
          >
            Guardar
          </Boton>
          {valorPropio !== null && (
            <Boton
              variante="secundario"
              disabled={enviando !== null}
              cargando={enviando === 'quitar'}
              textoCargando="Quitando…"
              aria-label={`Usar el valor del club para ${nombre}`}
              onClick={() => void aplicar(null, 'quitar')}
            >
              Usar valor del club
            </Boton>
          )}
          <Boton variante="texto" disabled={enviando !== null} onClick={cerrar}>
            Cancelar
          </Boton>
        </form>
      ) : (
        <>
          <strong className="nombre-jugador">{nombre}</strong>
          {etiqueta}
          {editable && (
            <button
              type="button"
              className="lapiz-tarjeta"
              aria-label={`Editar la mensualidad de ${nombre}`}
              title="Editar mensualidad"
              onClick={() => setEditando(true)}
            >
              <IconoLapiz />
            </button>
          )}
        </>
      )}
    </li>
  );
}
