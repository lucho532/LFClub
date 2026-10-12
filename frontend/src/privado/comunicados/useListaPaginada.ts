import { useCallback, useEffect, useRef, useState } from 'react';
import { api } from '../../compartido/api/cliente';
import { mensajeDe } from '../../compartido/api/errores';

interface Pagina {
  hayMas: boolean;
}

interface Lista<T> {
  items: T[];
  hayMas: boolean;
  cargando: boolean;
  cargandoMas: boolean;
  error: string | null;
  /** Pide la página siguiente, a partir del último elemento ya cargado. */
  verMas: () => void;
  /** Sustituye un elemento por el que devolvió la API tras una acción, sin volver a pedir la lista. */
  reemplazar: (elemento: T) => void;
}

/**
 * Una lista paginada por cursor (research §9): carga la primera página al montar o al cambiar la
 * ruta, y cada "Ver más" añade la siguiente con `antesDe` igual al último identificador.
 * `alPrimeraPagina` recibe la primera respuesta, por si trae algo más que la lista (el contador).
 * `elementosDe` saca los elementos de una página; por defecto, su propiedad `items`; la campana, que
 * mezcla comunicados y avisos, los saca de `elementos` (spec 012).
 * No decide quién ve qué: la API ya devolvió solo lo suyo.
 */
export function useListaPaginada<T extends { id: string }, P extends Pagina>(
  ruta: string,
  alPrimeraPagina?: (pagina: P) => void,
  elementosDe: (pagina: P) => T[] = (pagina) => (pagina as unknown as { items: T[] }).items,
): Lista<T> {
  const [items, setItems] = useState<T[]>([]);
  const [hayMas, setHayMas] = useState(false);
  const [cargando, setCargando] = useState(true);
  const [cargandoMas, setCargandoMas] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const vigente = useRef(ruta);
  const aviso = useRef(alPrimeraPagina);
  const sacar = useRef(elementosDe);

  useEffect(() => {
    aviso.current = alPrimeraPagina;
    sacar.current = elementosDe;
  });

  useEffect(() => {
    vigente.current = ruta;
    let cancelado = false;
    // eslint-disable-next-line react-hooks/set-state-in-effect
    setCargando(true);
    setError(null);
    api
      .get<P>(ruta)
      .then((pagina) => {
        if (!cancelado) {
          setItems(sacar.current(pagina));
          setHayMas(pagina.hayMas);
          aviso.current?.(pagina);
        }
      })
      .catch((fallo: unknown) => {
        if (!cancelado) {
          setError(mensajeDe(fallo));
        }
      })
      .finally(() => {
        if (!cancelado) {
          setCargando(false);
        }
      });

    return () => {
      cancelado = true;
    };
  }, [ruta]);

  const verMas = useCallback(() => {
    const ultimo = items[items.length - 1];
    if (!ultimo || cargandoMas) {
      return;
    }

    const separador = ruta.includes('?') ? '&' : '?';
    const deEstaRuta = vigente.current;
    setCargandoMas(true);
    api
      .get<P>(`${ruta}${separador}antesDe=${ultimo.id}`)
      .then((pagina) => {
        if (vigente.current === deEstaRuta) {
          setItems((previos) => [...previos, ...sacar.current(pagina)]);
          setHayMas(pagina.hayMas);
        }
      })
      .catch((fallo: unknown) => setError(mensajeDe(fallo)))
      .finally(() => setCargandoMas(false));
  }, [items, ruta, cargandoMas]);

  const reemplazar = useCallback((elemento: T) => {
    setItems((previos) => previos.map((previo) => (previo.id === elemento.id ? elemento : previo)));
  }, []);

  return { items, hayMas, cargando, cargandoMas, error, verMas, reemplazar };
}
