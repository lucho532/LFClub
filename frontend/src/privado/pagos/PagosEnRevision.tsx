import { Aviso } from '../../compartido/componentes/Aviso';
import { Tarjeta } from '../../compartido/componentes/Tarjeta';
import { useClub } from '../contextoClub';
import { TarjetaPagoEnRevision } from './TarjetaPagoEnRevision';
import { usePagosEnRevision } from './usePagosEnRevision';

/**
 * Pagos → En revisión (spec 015, historia 6): las transferencias que informó la familia, de la más
 * antigua a la más reciente. El PRESIDENTE las confirma o rechaza; el DIRECTIVO las consulta. Ninguna
 * suma al estado de cuenta ni al recaudo hasta que se confirme.
 */
export function PagosEnRevision() {
  const { club } = useClub();
  const revision = usePagosEnRevision(club.clubId);

  return (
    <>
      <div className="cabecera">
        <h1>Pagos en revisión</h1>
      </div>
      {revision.error && <Aviso tono="error">{revision.error}</Aviso>}
      {!revision.datos && revision.cargando && <p className="texto-suave">Cargando…</p>}
      {revision.datos?.pagos.length === 0 && (
        <Tarjeta>
          <p className="texto-suave">No hay transferencias esperando revisión.</p>
        </Tarjeta>
      )}
      {revision.datos?.pagos.map((fila) => (
        <TarjetaPagoEnRevision
          key={fila.pago.id}
          clubId={club.clubId}
          revision={fila}
          puedeRevisar={club.miRol === 'PRESIDENTE'}
          alTerminar={revision.recargar}
        />
      ))}
    </>
  );
}
