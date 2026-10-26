import type { PagoPlataformaDto } from '../../compartido/api/tiposPagoPlataforma';
import { fechaYHora, textoDeRango } from '../../compartido/formato';
import { formatoMoneda } from '../pagos/formatoMoneda';
import { TEXTO_ORIGEN_DEL_PAGO, textoDelPeriodo } from './textosPagoPlataforma';

interface Props {
  resumen: PagoPlataformaDto;
}

/**
 * El último pago registrado y el próximo cobro del club con la plataforma (spec 017). Los cobros pendientes
 * no se repiten aquí: la pantalla de pago ya los lista completos. El próximo cobro es solo una estimación,
 * hecha en el servidor con los jugadores de hoy y la tarifa vigente, y se rotula como tal: el cobro real se
 * genera al empezar el mes y puede variar.
 */
export function ResumenPagosPlataforma({ resumen }: Props) {
  const { ultimoPago, proximoCobro } = resumen;

  return (
    <div className="resumen-pagos-plataforma">
      <section aria-labelledby="ultimo-pago-plataforma">
        <h3 id="ultimo-pago-plataforma">Último pago registrado</h3>
        {ultimoPago && ultimoPago.pagadoEn ? (
          <p>
            <strong>{textoDelPeriodo(ultimoPago.periodo)}</strong> · {formatoMoneda(ultimoPago.total)}
            <br />
            <span className="texto-suave">
              Pagado el {fechaYHora(ultimoPago.pagadoEn)}
              {ultimoPago.origenDelPago && ` · ${TEXTO_ORIGEN_DEL_PAGO[ultimoPago.origenDelPago]}`}
              {ultimoPago.medioDePago && ` · ${ultimoPago.medioDePago}`}
            </span>
          </p>
        ) : (
          <p className="texto-suave">Todavía no hay pagos registrados.</p>
        )}
      </section>

      <section aria-labelledby="proximos-pagos-plataforma">
        <h3 id="proximos-pagos-plataforma">Próximo cobro</h3>
        {proximoCobro ? (
          <p>
            <strong>{textoDelPeriodo(proximoCobro.periodo)}</strong> · estimado {formatoMoneda(proximoCobro.valorEstimado)}
            <br />
            <span className="texto-suave">
              Con tus {proximoCobro.jugadores} jugadores de hoy ({textoDeRango(proximoCobro.rango.desde, proximoCobro.rango.hasta)}
              ). El cobro se genera al empezar el mes y puede cambiar si cambian tus jugadores.
            </span>
          </p>
        ) : (
          <p className="texto-suave">No hay pagos próximos.</p>
        )}
      </section>
    </div>
  );
}
