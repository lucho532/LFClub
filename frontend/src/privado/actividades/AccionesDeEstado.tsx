import { useState } from 'react';
import { mensajeDe } from '../../compartido/api/errores';
import { Aviso } from '../../compartido/componentes/Aviso';
import { Boton } from '../../compartido/componentes/Boton';
import { DialogoConfirmacion } from '../../compartido/componentes/DialogoConfirmacion';

interface Props {
  /** "entrenamiento" o "partido", para los textos. */
  nombre: string;
  cancelada: boolean;
  /** Si ya empezó: entonces no se ofrece reactivarla. */
  empezo: boolean;
  /** Si se ofrece cambiarla (un partido jugado, por ejemplo, no se cambia). */
  sePuedeCambiar: boolean;
  alCambiar: () => void;
  cancelar: () => Promise<void>;
  reactivar: () => Promise<void>;
}

/**
 * Los botones de quien gestiona una actividad: "Cambiar", "Cancelar …" con confirmación y, si está
 * cancelada y no ha empezado, "Reactivar" (RF-038). Muestra con texto claro el motivo si la API lo
 * rechaza. Solo se pinta con `permisos.gestiona`; la API lo vuelve a comprobar.
 */
export function AccionesDeEstado({ nombre, cancelada, empezo, sePuedeCambiar, alCambiar, cancelar, reactivar }: Props) {
  const [confirmando, setConfirmando] = useState(false);
  const [enviando, setEnviando] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function hacer(accion: () => Promise<void>) {
    setEnviando(true);
    setError(null);
    try {
      await accion();
      setConfirmando(false);
    } catch (fallo) {
      setError(mensajeDe(fallo));
    } finally {
      setEnviando(false);
    }
  }

  return (
    <>
      <div className="fila">
        {!cancelada && sePuedeCambiar && (
          <>
            <Boton variante="secundario" onClick={alCambiar}>
              Cambiar
            </Boton>
            <Boton variante="peligro" onClick={() => setConfirmando(true)}>
              {`Cancelar ${nombre}`}
            </Boton>
          </>
        )}
        {cancelada && !empezo && (
          <Boton cargando={enviando} textoCargando="Reactivando…" onClick={() => void hacer(reactivar)}>
            Reactivar
          </Boton>
        )}
      </div>
      {error && !confirmando && <Aviso tono="error">{error}</Aviso>}
      <DialogoConfirmacion
        abierto={confirmando}
        titulo={`¿Cancelar este ${nombre}?`}
        textoConfirmar={`Cancelar ${nombre}`}
        peligro
        cargando={enviando}
        error={error ?? undefined}
        alConfirmar={() => void hacer(cancelar)}
        alCancelar={() => {
          setConfirmando(false);
          setError(null);
        }}
      >
        <p>
          Seguirá a la vista, marcado como cancelado. Podrás reactivarlo mientras no haya empezado.
        </p>
      </DialogoConfirmacion>
    </>
  );
}
