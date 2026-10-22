import { useEffect, useState } from 'react';
import { api } from '../../compartido/api/cliente';
import { mensajeDe } from '../../compartido/api/errores';
import { Aviso } from '../../compartido/componentes/Aviso';
import { Boton } from '../../compartido/componentes/Boton';
import { compartirResguardo, descargarResguardo, nombreDelResguardo, puedeCompartirResguardo } from './compartirResguardo';

interface Props {
  clubId: string;
  pagoId: string;
  numeroRecibo: number | null;
  /** Texto con el que se comparte, por ejemplo "Resguardo de pago n.º 12". */
  titulo: string;
}

/**
 * El resguardo de un pago como imagen (spec 015, RF-025): se pide a la API con la sesión, se muestra desde
 * memoria y se puede descargar o, donde el dispositivo lo admite, compartir. El resguardo de un pago anulado
 * lleva el sello ANULADO. No tiene dirección pública: la imagen vive solo en esta pantalla.
 */
export function ResguardoDePago({ clubId, pagoId, numeroRecibo, titulo }: Props) {
  const [imagen, setImagen] = useState<Blob | null>(null);
  const [direccion, setDireccion] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const nombre = nombreDelResguardo(numeroRecibo);

  useEffect(() => {
    let cancelado = false;
    api
      .getArchivo(`/api/clubes/${clubId}/pagos/${pagoId}/resguardo`)
      .then((blob) => {
        if (!cancelado) {
          setImagen(blob);
          setDireccion(URL.createObjectURL(blob));
        }
      })
      .catch((fallo: unknown) => {
        if (!cancelado) {
          setError(mensajeDe(fallo));
        }
      });

    return () => {
      cancelado = true;
    };
  }, [clubId, pagoId]);

  useEffect(
    () => () => {
      if (direccion) {
        URL.revokeObjectURL(direccion);
      }
    },
    [direccion],
  );

  async function compartir() {
    if (!imagen) {
      return;
    }

    try {
      await compartirResguardo(imagen, nombre, titulo);
    } catch (fallo) {
      setError(mensajeDe(fallo));
    }
  }

  return (
    <div className="columna">
      {error && <Aviso tono="error">{error}</Aviso>}
      {!direccion && !error && <p className="texto-suave">Preparando el resguardo…</p>}
      {direccion && <img className="imagen-de-resguardo" src={direccion} alt={titulo} />}
      {imagen && (
        <div className="fila">
          <Boton onClick={() => descargarResguardo(imagen, nombre)}>Descargar</Boton>
          {puedeCompartirResguardo(imagen, nombre) && (
            <Boton variante="secundario" onClick={() => void compartir()}>
              Compartir
            </Boton>
          )}
        </div>
      )}
    </div>
  );
}
