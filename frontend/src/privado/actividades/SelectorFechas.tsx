import { useState } from 'react';
import { Boton } from '../../compartido/componentes/Boton';
import {
  diaCorto,
  diaDeLaSemana,
  diasDeLaSemana,
  hoyEnColombia,
  inicioDeSemana,
  sumarDias,
} from '../../compartido/fechas';

interface Props {
  fechas: string[];
  alCambiar: (fechas: string[]) => void;
  error?: string;
}

/**
 * Los días de un entrenamiento nuevo: los siete días de una semana con una casilla cada uno, y los
 * marcados son los que se publican. Con "Semana anterior" y "Semana siguiente" se pasa a otra
 * semana sin perder lo ya marcado, y debajo se listan todos los días elegidos. Cada día se
 * programa como un entrenamiento independiente: después se cambia o se cancela por separado. Los
 * días pasados no se pueden marcar; el resto de las fechas lo comprueba la API.
 */
export function SelectorFechas({ fechas, alCambiar, error }: Props) {
  const hoy = hoyEnColombia();
  const [lunes, setLunes] = useState(() => inicioDeSemana(fechas[0] ?? hoy));
  const marcadas = new Set(fechas);
  const semana = diasDeLaSemana(lunes);
  const esLaActual = lunes === inicioDeSemana(hoy);

  function conmutar(fecha: string, marcada: boolean) {
    const siguiente = marcada ? [...marcadas, fecha] : [...marcadas].filter((otra) => otra !== fecha);
    alCambiar(siguiente.sort());
  }

  return (
    <fieldset className="columna selector-fechas">
      <legend>Días del entrenamiento</legend>
      <div className="fila">
        <Boton
          variante="secundario"
          disabled={esLaActual}
          onClick={() => setLunes(sumarDias(lunes, -7))}
        >
          ← Semana anterior
        </Boton>
        <Boton variante="secundario" onClick={() => setLunes(sumarDias(lunes, 7))}>
          Semana siguiente →
        </Boton>
      </div>
      <div className="casillas-semana">
        {semana.map((fecha) => {
          const { nombre, numero } = diaCorto(fecha);
          const pasado = fecha < hoy;
          const marcada = marcadas.has(fecha);
          return (
            <label key={fecha} className={`casilla-dia${marcada ? ' marcada' : ''}`}>
              <input
                type="checkbox"
                checked={marcada}
                disabled={pasado && !marcada}
                aria-label={diaDeLaSemana(fecha)}
                onChange={(evento) => conmutar(fecha, evento.target.checked)}
              />
              <span className="casilla-dia-nombre">{nombre}</span>
              <span className="casilla-dia-numero">{numero}</span>
            </label>
          );
        })}
      </div>
      {fechas.length === 0 ? (
        <p className="texto-suave">Marca al menos un día.</p>
      ) : (
        <p className="texto-suave">
          {fechas.length === 1
            ? `Se publica el ${diaDeLaSemana(fechas[0])}.`
            : `Se publican ${fechas.length} entrenamientos: ${fechas.map(diaDeLaSemana).join(', ')}.`}
        </p>
      )}
      {error && (
        <p className="campo-error" role="alert">
          {error}
        </p>
      )}
    </fieldset>
  );
}
