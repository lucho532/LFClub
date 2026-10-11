import { useState, type ReactNode } from 'react';
import { api } from '../../compartido/api/cliente';
import { mensajeDe } from '../../compartido/api/errores';
import type { FichaJugadorDto } from '../../compartido/api/tiposFicha';
import { Aviso } from '../../compartido/componentes/Aviso';
import { DialogoConfirmacion } from '../../compartido/componentes/DialogoConfirmacion';
import { LapizDeImagen } from '../../compartido/componentes/LapizDeImagen';
import { FotoJugador } from '../jugadores/FotoJugador';

interface Props {
  clubId: string;
  ficha: FichaJugadorDto;
  rutaDeLaFicha: string;
  /** Se llama con la ficha que devuelve la API tras cargar o quitar la foto. */
  alCambiar: (ficha: FichaJugadorDto) => void;
  /** Lo que va al lado de la foto: el nombre del jugador, su categoría y sus equipos. */
  children?: ReactNode;
}

/**
 * Cabecera de la ficha: la foto propia del jugador, o sus iniciales (spec 007), y a su lado lo que
 * reciba como hijos (el nombre, la categoría y los equipos), para que todo se vea de un vistazo
 * también en el teléfono. Un lápiz pequeño dentro del círculo de la foto la carga, la cambia o la
 * quita (`LapizDeImagen`). Quien
 * puede cambiar la ficha (la familia y el presidente) la carga, la cambia y la quita, salvo si el
 * jugador está retirado: un retirado no tiene foto (§14.2). Si la API rechaza una imagen, se dice
 * por qué y la anterior sigue a la vista. No es la foto de perfil de la cuenta.
 */
export function SeccionFoto({ clubId, ficha, rutaDeLaFicha, alCambiar, children }: Props) {
  const [ocupado, setOcupado] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [quitando, setQuitando] = useState(false);
  const ruta = rutaDeLaFicha.replace(/\/ficha$/, '/foto');
  const puedeCambiarla = ficha.permisos.puedeCambiar && !ficha.retirado;
  const tieneFoto = ficha.fotoActualizadaEn !== null;

  async function ejecutar(accion: () => Promise<FichaJugadorDto>) {
    setError(null);
    setOcupado(true);
    try {
      alCambiar(await accion());
      setQuitando(false);
    } catch (fallo) {
      setError(mensajeDe(fallo));
    } finally {
      setOcupado(false);
    }
  }

  return (
    <>
      <div className="cabecera-ficha">
        <div className="cabecera-ficha-foto">
          {puedeCambiarla ? (
            <LapizDeImagen
              que="la foto"
              tieneImagen={tieneFoto}
              ocupado={ocupado}
              alElegir={(archivo) => void ejecutar(() => api.putArchivo<FichaJugadorDto>(ruta, archivo))}
              alQuitar={() => setQuitando(true)}
            >
              <FotoJugador clubId={clubId} jugador={ficha} tamano="grande" />
            </LapizDeImagen>
          ) : (
            <FotoJugador clubId={clubId} jugador={ficha} tamano="grande" />
          )}
        </div>
        <div className="cabecera-ficha-datos">{children}</div>
      </div>
      {error && !quitando && <Aviso tono="error">{error}</Aviso>}
      <DialogoConfirmacion
        abierto={quitando}
        titulo="Quitar la foto"
        textoConfirmar="Quitar foto"
        peligro
        cargando={ocupado}
        error={quitando ? (error ?? undefined) : undefined}
        alConfirmar={() => void ejecutar(() => api.delete<FichaJugadorDto>(ruta))}
        alCancelar={() => {
          setQuitando(false);
          setError(null);
        }}
      >
        <p>La foto se borra y en su lugar se muestran las iniciales del jugador.</p>
      </DialogoConfirmacion>
    </>
  );
}
