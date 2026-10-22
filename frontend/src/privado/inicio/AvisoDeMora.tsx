import { Link } from 'react-router-dom';
import type { EstadoDeAvisoDeMora } from '../../compartido/api/tiposPagos';
import { Aviso, type Tono } from '../../compartido/componentes/Aviso';
import { useAvisoDeMora } from '../pagos/useAvisoDeMora';
import { rutaDeLaCuenta, textoDePagosEnRevision, textoDelAvisoDeMora } from '../pagos/textosPagos';

interface Props {
  clubId: string;
  /** El jugador con el que continúa la familia: el de la petición. */
  miUsuarioRolId: string;
}

const TONO: Record<EstadoDeAvisoDeMora, Tono> = {
  AL_DIA: 'exito',
  PENDIENTE_EN_PLAZO: 'aviso',
  EN_MORA: 'error',
};

const ICONO: Record<EstadoDeAvisoDeMora, string> = {
  AL_DIA: '✓',
  PENDIENTE_EN_PLAZO: '!',
  EN_MORA: '⚠',
};

/**
 * Aviso fijo de mora del inicio de la familia (spec 016): rojo si el jugador está en mora, ámbar si
 * tiene la mensualidad pendiente dentro del plazo, verde si no debe nada. No se puede cerrar ni
 * posponer: no tiene botón para ello y vuelve a pedirse cada vez que se abre el inicio. El servidor
 * decide el estado con la fecha de Colombia; aquí solo se pinta. Cada estado va con ícono y texto,
 * nunca solo con color. Mientras carga no muestra nada y, si no se pudo consultar, un aviso neutro con
 * "Reintentar": jamás el verde por defecto.
 * No restringe nada ni calcula fechas.
 */
export function AvisoDeMora({ clubId, miUsuarioRolId }: Props) {
  const carga = useAvisoDeMora(clubId, miUsuarioRolId);
  const aviso = carga.datos;

  if (carga.error) {
    return (
      <Aviso tono="neutro">
        No pudimos consultar el estado de tu cuenta.{' '}
        <button type="button" className="boton boton-secundario" onClick={carga.recargar}>
          Reintentar
        </button>
      </Aviso>
    );
  }

  if (!aviso) {
    return null;
  }

  return (
    <Aviso tono={TONO[aviso.estado]}>
      <p>
        <span aria-hidden="true">{ICONO[aviso.estado]}</span> {textoDelAvisoDeMora(aviso)}
      </p>
      {aviso.pagosEnRevision > 0 && <p>{textoDePagosEnRevision(aviso.pagosEnRevision)}</p>}
      <Link to={rutaDeLaCuenta(clubId, miUsuarioRolId)}>Ver mi cuenta</Link>
    </Aviso>
  );
}
