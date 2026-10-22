import type { CargoDto } from '../../compartido/api/tiposPagos';
import { formatoMoneda } from './formatoMoneda';
import { descripcionDelCargo } from './textosPagos';

interface Props {
  /** Cargos con algo pendiente, del más antiguo al más reciente. */
  cargos: CargoDto[];
  elegidos: string[];
  alCambiar: (elegidos: string[]) => void;
}

/**
 * Casillas para elegir qué cargos cubre un pago (spec 015, RF-024). Es opcional: sin ninguno marcado,
 * el pago cubre los más antiguos primero. Cada casilla muestra lo que falta de ese cargo.
 */
export function SelectorDeCargos({ cargos, elegidos, alCambiar }: Props) {
  if (cargos.length === 0) {
    return null;
  }

  const alternar = (id: string) =>
    alCambiar(elegidos.includes(id) ? elegidos.filter((otro) => otro !== id) : [...elegidos, id]);

  return (
    <fieldset className="casillas-de-cargos">
      <legend>Cargos que cubre (opcional)</legend>
      {cargos.map((cargo) => (
        <label key={cargo.id} className="fila">
          <input type="checkbox" checked={elegidos.includes(cargo.id)} onChange={() => alternar(cargo.id)} />
          <span>
            {descripcionDelCargo(cargo.tipo, cargo.mes)} · falta {formatoMoneda(cargo.pendiente)}
          </span>
        </label>
      ))}
      <span className="texto-suave">Sin elegir ninguno, cubre los más antiguos primero.</span>
    </fieldset>
  );
}
