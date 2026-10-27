import { Aviso } from '../../compartido/componentes/Aviso';
import { useClub } from '../contextoClub';
import { TablaDeTarifa } from './TablaDeTarifa';
import { usePagoPlataforma } from './usePagoPlataforma';

/** Pestaña «Cómo se cobra» de la suscripción: la tabla de la tarifa y las reglas del cobro. */
export function PantallaComoSeCobra() {
  const { club } = useClub();
  const carga = usePagoPlataforma(club.clubId);

  return (
    <>
      {carga.error && <Aviso tono="error">{carga.error}</Aviso>}
      {!carga.datos && carga.cargando && <p className="texto-suave">Cargando…</p>}
      {carga.datos && <TablaDeTarifa tarifa={carga.datos.tarifa} />}
    </>
  );
}
