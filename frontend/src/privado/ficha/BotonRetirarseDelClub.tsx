import { useState } from 'react';
import { api } from '../../compartido/api/cliente';
import { mensajeDe } from '../../compartido/api/errores';
import type { FichaJugadorDto } from '../../compartido/api/tiposFicha';
import { Boton } from '../../compartido/componentes/Boton';
import { DialogoConfirmacion } from '../../compartido/componentes/DialogoConfirmacion';
import { useSesion } from '../../compartido/sesion/useSesion';
import { nombreCompleto } from '../categorias/textos';

interface Props {
  clubId: string;
  /** La ficha propia: la del jugador con el que continúa la familia. */
  ficha: FichaJugadorDto;
}

/**
 * Botón "Retirar del club" de la ficha propia, para la familia (RF-042 de la 006). Pide confirmación
 * y explica qué pasa: el jugador deja de entrar a este club, sus datos se conservan, solo el club
 * puede reincorporarlo y sus hermanos no cambian. Al terminar recarga la sesión, que ya lo trae
 * retirado, y la aplicación del club muestra el aviso de retiro, con "Cambiar de jugador" si hay
 * otros. No es el botón del presidente (`BotonRetirarJugador`), que retira a cualquier jugador.
 */
export function BotonRetirarseDelClub({ clubId, ficha }: Props) {
  const { sesion, recargar } = useSesion();
  const [abierto, setAbierto] = useState(false);
  const [enviando, setEnviando] = useState(false);
  const [error, setError] = useState<string | undefined>();
  const otros = (sesion?.clubes.find((club) => club.clubId === clubId)?.jugadores ?? []).filter(
    (jugador) => jugador.usuarioRolId !== ficha.usuarioRolId,
  );

  async function retirar() {
    setEnviando(true);
    setError(undefined);
    try {
      await api.post(`/api/clubes/${clubId}/jugadores/${ficha.usuarioRolId}/retiro`);
      setAbierto(false);
      await recargar();
    } catch (fallo) {
      setError(mensajeDe(fallo));
    } finally {
      setEnviando(false);
    }
  }

  return (
    <>
      <Boton
        variante="secundario"
        onClick={() => {
          setError(undefined);
          setAbierto(true);
        }}
      >
        Retirar del club
      </Boton>
      <DialogoConfirmacion
        abierto={abierto}
        titulo={`Retirar del club a ${nombreCompleto(ficha)}`}
        textoConfirmar="Retirar del club"
        peligro
        cargando={enviando}
        error={error}
        alConfirmar={() => void retirar()}
        alCancelar={() => setAbierto(false)}
      >
        <p>
          {nombreCompleto(ficha)} dejará de entrar a este club: saldrá de su categoría y de sus equipos.
        </p>
        <p className="texto-suave">
          Sus datos se conservan. Solo el club puede reincorporarlo; desde aquí no se puede deshacer.
          {otros.length > 0 &&
            ` ${otros.length === 1 ? 'Su hermano' : 'Sus hermanos'} (${otros.map(nombreCompleto).join(', ')}) no ${otros.length === 1 ? 'cambia' : 'cambian'}.`}
        </p>
      </DialogoConfirmacion>
    </>
  );
}
