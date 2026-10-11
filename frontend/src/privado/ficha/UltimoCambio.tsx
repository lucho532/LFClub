import type { UltimoCambioDto } from '../../compartido/api/tiposFicha';
import { NombrePersona } from '../../compartido/componentes/NombrePersona';
import { fechaYHora } from '../../compartido/formato';

interface Props {
  cambio: UltimoCambioDto | undefined;
  clubId: string;
  /** Nombre de la ficha que se está viendo, para volver a ella desde la ficha básica del autor. */
  textoVolver: string;
}

/**
 * Línea con el último cambio de la ficha: cuándo fue y quién lo hizo. Cuando lo hizo la familia
 * dice "la cuenta del jugador", en lugar de repetir el nombre del jugador. El nombre del autor es el
 * que tenía entonces (§13) y lleva a su ficha básica de hoy si sigue en el club (spec 008); si no,
 * es texto normal. No se pinta si nadie ha cambiado la ficha. No es un historial: solo existe el
 * último cambio.
 */
export function UltimoCambio({ cambio, clubId, textoVolver }: Props) {
  if (!cambio) {
    return null;
  }

  return (
    <p className="texto-suave">
      Último cambio: {fechaYHora(cambio.fecha)}, por{' '}
      {cambio.porLaCuentaDelJugador ? (
        'la cuenta del jugador'
      ) : (
        <NombrePersona nombre={cambio.autor} enlace={cambio.autorFicha} clubId={clubId} textoVolver={textoVolver} />
      )}
      .
    </p>
  );
}
