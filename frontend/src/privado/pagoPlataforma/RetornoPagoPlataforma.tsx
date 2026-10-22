import { useEffect, useState } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import type { CobroPlataformaDto } from '../../compartido/api/tiposPagoPlataforma';
import { Aviso } from '../../compartido/componentes/Aviso';
import { useClub } from '../contextoClub';
import { apiPagoPlataforma } from './apiPagoPlataforma';
import { TarjetaCobro } from './TarjetaCobro';

/** Cada cuánto se vuelve a consultar el cobro, y hasta cuándo. */
export const INTERVALO_DE_CONSULTA_MS = 3000;
export const LIMITE_DE_CONSULTA_MS = 30000;

/**
 * A donde vuelve la persona desde la pasarela (spec 017, RF-011). Solo consulta el cobro cada 3
 * segundos, hasta 30: mientras no figure pagado ni rechazado dice «En verificación»; si la pasarela ya
 * informó que no lo aprobó, lo dice y deja de esperar; cuando lo figura, «Pago
 * confirmado» con el comprobante; pasados los 30 segundos, que se puede volver más tarde. Los datos
 * de la dirección de retorno no prueban nada y no se envían a ningún sitio: el cobro solo queda
 * pagado cuando el servidor recibe la notificación verificada de la pasarela.
 */
export function RetornoPagoPlataforma() {
  const { club } = useClub();
  const [parametros] = useSearchParams();
  const cobroId = parametros.get('cobro');
  const [cobro, setCobro] = useState<CobroPlataformaDto | null>(null);
  const [terminoElTiempo, setTerminoElTiempo] = useState(false);

  useEffect(() => {
    if (!cobroId) {
      return;
    }

    let cancelado = false;
    let temporizador: ReturnType<typeof setTimeout>;
    const inicio = Date.now();

    async function consultar() {
      try {
        const actual = await apiPagoPlataforma.obtenerCobro(club.clubId, cobroId!);
        if (cancelado) {
          return;
        }

        setCobro(actual);
        // Pagado, anulado o rechazado por la pasarela: no hay nada más que esperar.
        if (actual.estado !== 'PENDIENTE' || actual.ultimoPagoNoAprobado) {
          return;
        }
      } catch {
        // Un fallo de red no cambia nada: se sigue intentando hasta que se acabe el tiempo.
      }

      if (Date.now() - inicio + INTERVALO_DE_CONSULTA_MS > LIMITE_DE_CONSULTA_MS) {
        setTerminoElTiempo(true);
        return;
      }

      temporizador = setTimeout(() => void consultar(), INTERVALO_DE_CONSULTA_MS);
    }

    void consultar();
    return () => {
      cancelado = true;
      clearTimeout(temporizador);
    };
  }, [club.clubId, cobroId]);

  const volver = (
    <Link to={`/club/${club.clubId}/pago-plataforma`}>
      {cobro?.ultimoPagoNoAprobado ? 'Intentar de nuevo' : 'Volver al pago de la plataforma'}
    </Link>
  );

  if (!cobroId) {
    return (
      <>
        <h1>Pago de la plataforma</h1>
        <Aviso tono="aviso">No encontramos el cobro que buscas.</Aviso>
        {volver}
      </>
    );
  }

  return (
    <>
      <h1>Pago de la plataforma</h1>
      {cobro?.estado === 'PAGADO' ? (
        <>
          <Aviso tono="exito">
            <span aria-hidden="true">✓</span> Pago confirmado. ¡Gracias!
          </Aviso>
          <TarjetaCobro cobro={cobro} />
        </>
      ) : cobro?.estado === 'ANULADO' ? (
        <Aviso tono="neutro">Este cobro fue anulado.</Aviso>
      ) : cobro?.ultimoPagoNoAprobado ? (
        <Aviso tono="aviso">
          <span aria-hidden="true">!</span> Tu pago no fue aprobado. No se debitó ningún dinero. Puedes intentarlo de
          nuevo con otro medio de pago.
        </Aviso>
      ) : terminoElTiempo ? (
        <Aviso tono="info">
          Todavía estamos verificando tu pago; puedes volver a esta pantalla más tarde.
        </Aviso>
      ) : (
        <Aviso tono="info">En verificación: estamos esperando la confirmación de la pasarela de pago.</Aviso>
      )}
      {volver}
    </>
  );
}
