import type { ReactNode } from 'react';
import { Tarjeta } from '../../compartido/componentes/Tarjeta';
import { SIN_REGISTRAR } from './textos';

export interface Dato {
  etiqueta: string;
  /** Un dato vacío se muestra como "Sin registrar". */
  valor: ReactNode;
  /** Texto largo: ocupa todo el ancho y conserva sus saltos de línea. */
  largo?: boolean;
}

interface Props {
  titulo: string;
  datos: Dato[];
  /** Botones que acompañan al título. */
  acciones?: ReactNode;
  children?: ReactNode;
}

/**
 * Un grupo de datos de la ficha como texto de solo lectura: cada dato con su etiqueta (en el
 * teléfono, en la misma fila; un texto largo, debajo y a todo el ancho). Lo usan la
 * identidad, siempre, y los demás grupos cuando quien mira la ficha no puede cambiarla.
 */
export function GrupoDeDatos({ titulo, datos, acciones, children }: Props) {
  return (
    <Tarjeta titulo={titulo} acciones={acciones}>
      <dl className="ficha-datos">
        {datos.map((dato) => {
          const vacio = dato.valor === null || dato.valor === undefined || dato.valor === '';
          return (
            <div key={dato.etiqueta} className={dato.largo ? 'dato-ancho' : undefined}>
              <dt>{dato.etiqueta}</dt>
              <dd className={vacio ? 'texto-suave' : dato.largo ? 'dato-largo' : undefined}>
                {vacio ? SIN_REGISTRAR : dato.valor}
              </dd>
            </div>
          );
        })}
      </dl>
      {children}
    </Tarjeta>
  );
}
