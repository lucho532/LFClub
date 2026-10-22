import { useEffect, useState } from 'react';
import { mensajeDe } from '../../compartido/api/errores';
import { Aviso } from '../../compartido/componentes/Aviso';
import { Boton } from '../../compartido/componentes/Boton';
import { apiRevision } from './apiRevision';

interface Props {
  clubId: string;
  pagoId: string;
}

/**
 * La imagen del soporte de un pago, a un toque (spec 015, RF-030). Se pide a la API con la sesión solo
 * cuando se pulsa "Ver soporte", se muestra desde memoria y se suelta al cerrarla o al salir de la
 * pantalla: es un comprobante de pago, no tiene dirección pública ni se guarda en el navegador.
 */
export function SoporteDelPago({ clubId, pagoId }: Props) {
  const [direccion, setDireccion] = useState<string | null>(null);
  const [cargando, setCargando] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(
    () => () => {
      if (direccion) {
        URL.revokeObjectURL(direccion);
      }
    },
    [direccion],
  );

  async function abrir() {
    setError(null);
    setCargando(true);
    try {
      setDireccion(URL.createObjectURL(await apiRevision.soporte(clubId, pagoId)));
    } catch (fallo) {
      setError(mensajeDe(fallo));
    } finally {
      setCargando(false);
    }
  }

  if (direccion) {
    return (
      <div className="columna">
        <img className="imagen-de-soporte" src={direccion} alt="Comprobante de la transferencia" />
        <div className="fila">
          <Boton variante="secundario" onClick={() => setDireccion(null)}>
            Cerrar soporte
          </Boton>
        </div>
      </div>
    );
  }

  return (
    <div className="columna">
      {error && <Aviso tono="error">{error}</Aviso>}
      <div className="fila">
        <Boton variante="secundario" cargando={cargando} textoCargando="Abriendo…" onClick={() => void abrir()}>
          Ver soporte
        </Boton>
      </div>
    </div>
  );
}
