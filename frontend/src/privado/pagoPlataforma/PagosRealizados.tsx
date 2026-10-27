import { Aviso } from '../../compartido/componentes/Aviso';
import { useClub } from '../contextoClub';
import { TarjetaCobro } from './TarjetaCobro';
import { usePagoPlataforma } from './usePagoPlataforma';

/**
 * Pestaña «Pagos realizados» de la suscripción: el historial de cobros ya cerrados, el más reciente primero,
 * cada uno con su comprobante (fecha, origen, referencia y medio de pago) si se pagó, o el motivo si se anuló.
 * Todo sale del servidor tal como se guardó.
 */
export function PagosRealizados() {
  const { club } = useClub();
  const carga = usePagoPlataforma(club.clubId);
  const historial = carga.datos?.historial;

  return (
    <>
      {carga.error && <Aviso tono="error">{carga.error}</Aviso>}
      {!historial && carga.cargando && <p className="texto-suave">Cargando…</p>}
      {historial?.length === 0 && <p className="texto-suave">Todavía no hay pagos registrados.</p>}
      {historial?.map((cobro) => <TarjetaCobro key={cobro.cobroId} cobro={cobro} />)}
    </>
  );
}
