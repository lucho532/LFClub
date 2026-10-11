import { Boton } from '../../compartido/componentes/Boton';
import { Campo } from '../../compartido/componentes/Campo';
import { sumarDias } from '../../compartido/fechas';

interface Props {
  fechas: string[];
  alCambiar: (fechas: string[]) => void;
  error?: string;
}

/**
 * Los días de un entrenamiento nuevo: uno o varios, para no escribir el mismo entreno día tras
 * día. "Agregar otro día" propone el mismo día de la semana siguiente y se puede cambiar. Cada día
 * se programa como un entrenamiento independiente: después se cambia o se cancela por separado.
 * No comprueba nada de las fechas; lo hace la API.
 */
export function SelectorFechas({ fechas, alCambiar, error }: Props) {
  const cambiar = (indice: number, valor: string) =>
    alCambiar(fechas.map((fecha, otro) => (otro === indice ? valor : fecha)));
  const ultima = fechas[fechas.length - 1];

  return (
    <fieldset className="columna selector-fechas">
      <legend>{fechas.length > 1 ? `Días (${fechas.length} entrenamientos)` : 'Día'}</legend>
      {fechas.map((fecha, indice) => (
        <div key={indice} className="fila selector-fechas-dia">
          <Campo
            etiqueta={fechas.length > 1 ? `Día ${indice + 1}` : 'Fecha'}
            type="date"
            valor={fecha}
            alCambiar={(valor) => cambiar(indice, valor)}
            error={indice === 0 ? error : undefined}
            required
          />
          {fechas.length > 1 && (
            <Boton
              variante="secundario"
              aria-label={`Quitar el día ${indice + 1}`}
              onClick={() => alCambiar(fechas.filter((_, otro) => otro !== indice))}
            >
              Quitar
            </Boton>
          )}
        </div>
      ))}
      <div>
        <Boton variante="secundario" onClick={() => alCambiar([...fechas, ultima ? sumarDias(ultima, 7) : ''])}>
          + Agregar otro día
        </Boton>
      </div>
    </fieldset>
  );
}
