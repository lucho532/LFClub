import type { TotalPorCategoriaDto } from '../../compartido/api/tiposPagos';
import { Tabla, type Columna } from '../../compartido/componentes/Tabla';
import { formatoMoneda } from './formatoMoneda';

/**
 * Recaudo del mes y cartera de cada categoría (spec 015, RF-032): la categoría es la que tenía el jugador
 * cuando se generó cada cargo, así que un cambio de categoría no reordena lo anterior. Sin movimientos, lo dice.
 */
export function TablaPorCategoria({ filas }: { filas: TotalPorCategoriaDto[] }) {
  const columnas: Columna<TotalPorCategoriaDto>[] = [
    { titulo: 'Categoría', celda: (fila) => <strong>{fila.nombre}</strong> },
    { titulo: 'Recaudo del mes', celda: (fila) => formatoMoneda(fila.recaudoMes) },
    { titulo: 'Cartera pendiente', celda: (fila) => formatoMoneda(fila.cartera) },
  ];

  return (
    <Tabla
      descripcion="Recaudo y cartera por categoría"
      columnas={columnas}
      filas={filas}
      clave={(fila) => fila.categoriaId ?? 'sin-categoria'}
      vacio="Todavía no hay cargos ni pagos para mostrar."
    />
  );
}
