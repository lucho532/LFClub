import { useEffect, useState } from 'react';
import { mensajeDe } from '../../compartido/api/errores';
import { Aviso } from '../../compartido/componentes/Aviso';
import { Boton } from '../../compartido/componentes/Boton';
import { useClub } from '../contextoClub';
import { formatoMoneda } from '../pagos/formatoMoneda';
import { apiPagoPlataforma } from './apiPagoPlataforma';
import { TarjetaCobro } from './TarjetaCobro';
import { AVISO_SIN_PAGO_EN_LINEA } from './textosPagoPlataforma';
import { usePagoPlataforma } from './usePagoPlataforma';

/**
 * Pago de la plataforma (spec 017, historia 1): solo para el PRESIDENTE. Muestra los cobros pendientes
 * con su detalle y un botón que lleva a la pasarela de pago, y el historial. El botón solo abre la
 * dirección que firmó el servidor; que el cobro quede pagado lo decide la notificación de la pasarela,
 * nunca esta pantalla. Si la pasarela no está configurada no hay botón y se dice por qué.
 */
export function PagoPlataforma() {
  const { club } = useClub();
  const carga = usePagoPlataforma(club.clubId);
  const [pagandoCobro, setPagandoCobro] = useState<string | null>(null);
  const [errorDePago, setErrorDePago] = useState<string | null>(null);
  const resumen = carga.datos;

  const { recargar } = carga;

  // La pasarela puede abrirse en otra aplicación (el navegador del teléfono) y el pago se confirma por la
  // notificación mientras esta pantalla sigue abierta: al volver a verla se pide de nuevo el cobro.
  useEffect(() => {
    function alVolver() {
      if (document.visibilityState === 'visible') {
        setPagandoCobro(null);
        recargar();
      }
    }

    document.addEventListener('visibilitychange', alVolver);
    window.addEventListener('focus', alVolver);
    return () => {
      document.removeEventListener('visibilitychange', alVolver);
      window.removeEventListener('focus', alVolver);
    };
  }, [recargar]);

  async function pagar(cobroId: string) {
    setErrorDePago(null);
    setPagandoCobro(cobroId);
    try {
      const intento = await apiPagoPlataforma.iniciarPago(club.clubId, cobroId);
      window.location.assign(intento.urlDePago);
    } catch (fallo) {
      setErrorDePago(mensajeDe(fallo));
      setPagandoCobro(null);
    }
  }

  return (
    <>
      <div className="cabecera">
        <h1>Pago de la plataforma</h1>
      </div>
      {carga.error && <Aviso tono="error">{carga.error}</Aviso>}
      {errorDePago && <Aviso tono="error">{errorDePago}</Aviso>}
      {!resumen && carga.cargando && <p className="texto-suave">Cargando…</p>}
      {resumen && (
        <>
          {resumen.suspendidoPorImpago && (
            <Aviso tono="error">
              <span aria-hidden="true">⚠</span> Tu club está suspendido por falta de pago. Paga para que todos
              puedan volver a entrar.
            </Aviso>
          )}
          {resumen.pendientes.length === 0 ? (
            <Aviso tono="exito">
              <span aria-hidden="true">✓</span> Estás al día con la plataforma.
            </Aviso>
          ) : (
            <>
              <p>
                Total por pagar: <strong>{formatoMoneda(resumen.totalPendiente)}</strong>
              </p>
              {!resumen.pagoEnLineaDisponible && <Aviso tono="aviso">{AVISO_SIN_PAGO_EN_LINEA}</Aviso>}
            </>
          )}
          {resumen.pendientes.map((cobro) => (
            <TarjetaCobro
              key={cobro.cobroId}
              cobro={cobro}
              accion={
                resumen.pagoEnLineaDisponible && (
                  <div className="fila">
                    <Boton
                      onClick={() => void pagar(cobro.cobroId)}
                      cargando={pagandoCobro === cobro.cobroId}
                      textoCargando="Abriendo la pasarela…"
                    >
                      Pagar
                    </Boton>
                  </div>
                )
              }
            />
          ))}
          {resumen.historial.length > 0 && <h2>Historial</h2>}
          {resumen.historial.map((cobro) => (
            <TarjetaCobro key={cobro.cobroId} cobro={cobro} />
          ))}
        </>
      )}
    </>
  );
}
