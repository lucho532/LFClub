import type { ReactNode } from 'react';
import { diaCorto, diaDeLaSemana } from '../../compartido/fechas';

interface Props {
  /** Los siete días de la semana, de lunes a domingo. */
  dias: string[];
  hoy: string;
  /** El día elegido: en el teléfono se ve junto con el siguiente. */
  elegido: string;
  alElegir: (dia: string) => void;
  /** Las actividades de cada día, ya ordenadas. */
  actividades: Record<string, ReactNode[]>;
}

/**
 * La semana del calendario. En el teléfono, como `M-Calendario` del lienzo: una tira con los siete
 * días (los que tienen actividad, de otro color y con el número escrito, para que la familia vea
 * de un vistazo qué días hay algo) y, debajo, dos días: el elegido y el siguiente (el domingo se ve
 * con el sábado). En escritorio, una columna por día. "Hoy" se dice con texto, no solo con color.
 */
export function TiraDeSemana({ dias, hoy, elegido, alElegir, actividades }: Props) {
  const primero = Math.min(Math.max(dias.indexOf(elegido), 0), dias.length - 2);
  const visibles = new Set(dias.slice(primero, primero + 2));
  return (
    <div className="semana">
      <div className="tira-semana">
        {dias.map((dia) => {
          const { nombre, numero } = diaCorto(dia);
          const cuantas = actividades[dia]?.length ?? 0;
          return (
            <button
              key={dia}
              type="button"
              className={`dia-de-tira${cuantas > 0 ? ' con-actividad' : ''}${
                dia === elegido ? ' encendido' : visibles.has(dia) ? ' acompanado' : ''
              }`}
              aria-pressed={dia === elegido}
              aria-label={`${diaDeLaSemana(dia)}${dia === hoy ? ', hoy' : ''}: ${cuantas} ${cuantas === 1 ? 'actividad' : 'actividades'}`}
              onClick={() => alElegir(dia)}
            >
              <span>{nombre}</span>
              <strong>{numero}</strong>
              <span className="texto-pequeno">{cuantas > 0 ? `${cuantas}` : '·'}</span>
            </button>
          );
        })}
      </div>
      <div className="semana-dias">
        {dias.map((dia) => (
          <section key={dia} className={`dia-columna${visibles.has(dia) ? ' dia-elegido' : ''}`}>
            <h3>
              {diaDeLaSemana(dia)}
              {dia === hoy && <span className="texto-suave"> · hoy</span>}
            </h3>
            {(actividades[dia]?.length ?? 0) > 0 ? (
              actividades[dia]
            ) : (
              <p className="texto-suave texto-pequeno">Sin actividades.</p>
            )}
          </section>
        ))}
      </div>
    </div>
  );
}
