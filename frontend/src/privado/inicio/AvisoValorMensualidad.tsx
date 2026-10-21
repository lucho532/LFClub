import { Link } from 'react-router-dom';
import { Aviso } from '../../compartido/componentes/Aviso';
import { useMensualidad } from '../pagos/useMensualidad';
import { AVISO_VALOR_MENSUALIDAD } from '../pagos/textosPagos';

/**
 * Aviso del inicio del PRESIDENTE mientras su club no ha definido el valor de la mensualidad (spec
 * 015, RF-011): sin valor no se generan cargos. Lleva a la pantalla donde se define. No es un
 * recordatorio de cobro (§16.8): no habla de lo que deben los jugadores. Sin valor nulo, no pinta nada.
 */
export function AvisoValorMensualidad({ clubId }: { clubId: string }) {
  const mensualidad = useMensualidad(clubId);

  if (mensualidad.datos?.valorClub !== null) {
    return null;
  }

  return (
    <Aviso tono="aviso">
      <div className="aviso-con-accion">
        <span>{AVISO_VALOR_MENSUALIDAD}: mientras no exista, el club no genera cargos.</span>
        <Link to={`/club/${clubId}/pagos/mensualidad`}>Definir ahora</Link>
      </div>
    </Aviso>
  );
}
