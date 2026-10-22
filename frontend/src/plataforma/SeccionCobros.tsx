import { useState } from 'react';
import { mensajeDe } from '../compartido/api/errores';
import type { CobroPlataformaDto } from '../compartido/api/tiposPagoPlataforma';
import { useCarga } from '../compartido/api/useCarga';
import { Aviso } from '../compartido/componentes/Aviso';
import { Boton } from '../compartido/componentes/Boton';
import { Campo } from '../compartido/componentes/Campo';
import { DialogoConfirmacion } from '../compartido/componentes/DialogoConfirmacion';
import { formatoMoneda } from '../privado/pagos/formatoMoneda';
import { TarjetaCobro } from '../privado/pagoPlataforma/TarjetaCobro';
import { textoDelPeriodo } from '../privado/pagoPlataforma/textosPagoPlataforma';
import { apiPagoPlataforma } from './apiPagoPlataforma';
import type { PropsSeccion } from './DetalleClub';

type Accion = { tipo: 'pagar' | 'anular'; cobro: CobroPlataformaDto };

/**
 * Cobros de la plataforma de un club para el DESARROLLADOR (spec 017, historia 5): el historial con el
 * detalle de cada cobro, y en los pendientes dos acciones, cada una con confirmación: «Marcar pagado»
 * (un pago hecho por fuera de la pasarela) y «Anular» (con un motivo obligatorio). No muestra datos
 * personales de nadie del club.
 */
export function SeccionCobros({ club, recargar }: PropsSeccion) {
  const cobros = useCarga(`${club.clubId}/cobros`, () => apiPagoPlataforma.cobros.listar(club.clubId));
  const [accion, setAccion] = useState<Accion | null>(null);
  const [motivo, setMotivo] = useState('');
  const [error, setError] = useState<string | undefined>();
  const [enviando, setEnviando] = useState(false);

  function abrir(tipo: Accion['tipo'], cobro: CobroPlataformaDto) {
    setMotivo('');
    setError(undefined);
    setAccion({ tipo, cobro });
  }

  async function confirmar() {
    if (!accion) {
      return;
    }

    setError(undefined);
    setEnviando(true);
    try {
      if (accion.tipo === 'pagar') {
        await apiPagoPlataforma.cobros.marcarPagado(club.clubId, accion.cobro.cobroId);
      } else {
        await apiPagoPlataforma.cobros.anular(club.clubId, accion.cobro.cobroId, { motivo: motivo.trim() });
      }

      setAccion(null);
      cobros.recargar();
      recargar();
    } catch (fallo) {
      setError(mensajeDe(fallo));
    } finally {
      setEnviando(false);
    }
  }

  return (
    <>
      <h2>Cobros de la plataforma</h2>
      {cobros.error && <Aviso tono="error">{cobros.error}</Aviso>}
      {cobros.datos?.length === 0 && <p className="texto-suave">Este club todavía no tiene cobros.</p>}
      {cobros.datos?.map((cobro) => (
        <TarjetaCobro
          key={cobro.cobroId}
          cobro={cobro}
          accion={
            cobro.estado === 'PENDIENTE' && (
              <div className="fila">
                <Boton variante="secundario" onClick={() => abrir('pagar', cobro)}>
                  Marcar pagado
                </Boton>
                <Boton variante="peligro" onClick={() => abrir('anular', cobro)}>
                  Anular
                </Boton>
              </div>
            )
          }
        />
      ))}

      <DialogoConfirmacion
        abierto={accion !== null}
        titulo={
          accion?.tipo === 'pagar'
            ? `Marcar como pagado: ${accion ? textoDelPeriodo(accion.cobro.periodo) : ''}`
            : `Anular el cobro de ${accion ? textoDelPeriodo(accion.cobro.periodo) : ''}`
        }
        textoConfirmar={accion?.tipo === 'pagar' ? 'Marcar pagado' : 'Anular cobro'}
        peligro={accion?.tipo === 'anular'}
        deshabilitado={accion?.tipo === 'anular' && motivo.trim().length < 3}
        cargando={enviando}
        error={error}
        alConfirmar={() => void confirmar()}
        alCancelar={() => setAccion(null)}
      >
        {accion?.tipo === 'pagar' ? (
          <p>
            Se registrará un pago de {formatoMoneda(accion.cobro.total)} hecho por fuera de la pasarela, con tu
            nombre y la fecha de hoy. Si el club estaba suspendido por falta de pago y ya no debe nada vencido, vuelve
            a estar activo.
          </p>
        ) : (
          <>
            <p>El cobro no se borra: queda anulado, con el motivo, y ya no se puede pagar.</p>
            <Campo etiqueta="Motivo de la anulación" valor={motivo} alCambiar={setMotivo} lineas={3} maxLength={300} required />
          </>
        )}
      </DialogoConfirmacion>
    </>
  );
}
