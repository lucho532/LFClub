import type { CargoDto } from '../../compartido/api/tiposPagos';
import { formatoMoneda } from './formatoMoneda';
import { descripcionDelCargo } from './textosPagos';

/**
 * Los cargos de un jugador, del más reciente al más antiguo: qué cobra cada uno, su valor, lo que ya se
 * pagó y lo que falta (spec 015, RF-001). Un cargo pagado entero lo dice con texto. Una mensualidad sin
 * pagar no cambia de aspecto con los días: no hay mora (§16.8).
 */
export function ListaDeCargos({ cargos }: { cargos: CargoDto[] }) {
  if (cargos.length === 0) {
    return <p className="texto-suave">Todavía no hay cargos.</p>;
  }

  return (
    <ul className="lista-de-cargos">
      {cargos.map((cargo) => (
        <li key={cargo.id}>
          <span>
            <strong>{descripcionDelCargo(cargo.tipo, cargo.mes)}</strong>
            <span className="texto-suave"> · {formatoMoneda(cargo.valor)}</span>
          </span>
          <span>
            {cargo.pendiente === 0 ? (
              <span className="texto-suave">Pagado</span>
            ) : (
              <>
                Falta <strong>{formatoMoneda(cargo.pendiente)}</strong>
                {cargo.pagado > 0 && <span className="texto-suave"> (pagado {formatoMoneda(cargo.pagado)})</span>}
              </>
            )}
          </span>
        </li>
      ))}
    </ul>
  );
}
