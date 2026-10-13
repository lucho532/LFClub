import { useEffect, useState, type KeyboardEvent } from 'react';
import { api } from '../../compartido/api/cliente';
import { mensajeDe } from '../../compartido/api/errores';
import type { JugadorSumableDto } from '../../compartido/api/tiposAudiencia';
import { Aviso } from '../../compartido/componentes/Aviso';
import { Campo } from '../../compartido/componentes/Campo';

/** Letras mínimas para buscar; la API vuelve a exigirlas. */
const MINIMO = 2;
/** Espera tras la última tecla antes de preguntar a la API. */
const ESPERA_MS = 250;

interface Props {
  clubId: string;
  /** Jugadores que ya están sumados o convocados: no se ofrecen otra vez. */
  excluidos?: string[];
  alElegir: (jugador: JugadorSumableDto) => void;
}

/**
 * Busca jugadores del club por nombre o apellido para sumarlos a una actividad (spec 013, RF-013).
 * Pide al menos dos letras, muestra hasta veinte resultados con nombre y categoría, se maneja con
 * teclado (Enter elige el primero; cada resultado es un botón) y dice qué hacer cuando no hay
 * texto o no hay coincidencias. Los resultados no llevan enlace a la ficha: aparecer aquí no da
 * acceso a ella. No decide quién puede sumar a quién: la API lo comprueba.
 */
export function BuscadorDeJugadores({ clubId, excluidos = [], alElegir }: Props) {
  const [texto, setTexto] = useState('');
  const [resultados, setResultados] = useState<JugadorSumableDto[] | null>(null);
  const [error, setError] = useState<string | null>(null);
  const busqueda = texto.trim();
  const buscable = busqueda.length >= MINIMO;

  useEffect(() => {
    if (!buscable) {
      return;
    }

    let cancelado = false;
    const temporizador = setTimeout(() => {
      api
        .get<JugadorSumableDto[]>(
          `/api/clubes/${clubId}/actividades/jugadores-sumables?busqueda=${encodeURIComponent(busqueda)}`,
        )
        .then((encontrados) => {
          if (!cancelado) {
            setResultados(encontrados);
            setError(null);
          }
        })
        .catch((fallo: unknown) => {
          if (!cancelado) {
            setError(mensajeDe(fallo));
          }
        });
    }, ESPERA_MS);

    return () => {
      cancelado = true;
      clearTimeout(temporizador);
    };
  }, [clubId, busqueda, buscable]);

  const visibles = buscable ? (resultados ?? []).filter((jugador) => !excluidos.includes(jugador.usuarioRolId)) : [];

  function elegir(jugador: JugadorSumableDto) {
    alElegir(jugador);
    setTexto('');
    setResultados(null);
  }

  function alPulsar(evento: KeyboardEvent<HTMLDivElement>) {
    if (evento.key === 'Enter' && evento.target instanceof HTMLInputElement) {
      // Enter nunca envía el formulario que contenga al buscador: elige el primer resultado, si hay.
      evento.preventDefault();
      if (visibles.length > 0) {
        elegir(visibles[0]);
      }
    }
  }

  return (
    <div className="columna buscador-de-jugadores" onKeyDown={alPulsar}>
      <Campo
        etiqueta="Buscar un jugador del club"
        valor={texto}
        alCambiar={setTexto}
        maxLength={60}
        autoComplete="off"
        placeholder="Nombre o apellido"
      />
      {!buscable && <p className="texto-suave">Escribe al menos 2 letras del nombre o apellido.</p>}
      {error && <Aviso tono="error">{error}</Aviso>}
      {buscable && resultados !== null && visibles.length === 0 && !error && (
        <p className="texto-suave">No encontramos jugadores con ese nombre.</p>
      )}
      {visibles.length > 0 && (
        <ul className="resultados-de-busqueda">
          {visibles.map((jugador) => (
            <li key={jugador.usuarioRolId}>
              <button type="button" className="boton-conmutable" onClick={() => elegir(jugador)}>
                {`${jugador.nombres} ${jugador.apellidos}`}
                <span className="texto-suave">{` · Categoría ${jugador.categoria.anio}`}</span>
              </button>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
