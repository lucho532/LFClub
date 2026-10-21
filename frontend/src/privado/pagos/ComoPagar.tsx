import type { MedioPagoDto } from '../../compartido/api/tiposPagos';
import { Tarjeta } from '../../compartido/componentes/Tarjeta';

/**
 * "Cómo pagar" en la cuenta de la familia (spec 015, RF-027 y RF-028): las cuentas activas del club con
 * su entidad, titular, número y nota; la indicación de enviar el soporte después de transferir; y, si el
 * club no ha publicado ninguna, el aviso de consultar al presidente. No mueve dinero: el club lo recibe
 * fuera de la aplicación (§6.2).
 */
export function ComoPagar({ medios }: { medios: MedioPagoDto[] }) {
  return (
    <Tarjeta titulo="Cómo pagar">
      {medios.length === 0 ? (
        <p>El club todavía no ha publicado una cuenta para pagar. Consulte al presidente del club.</p>
      ) : (
        <>
          <ul className="lista-de-pagos">
            {medios.map((medio) => (
              <li key={medio.id}>
                <span className="columna">
                  <strong>{medio.entidad}</strong>
                  <span>
                    {medio.numero} · {medio.titular}
                  </span>
                  {medio.nota && <span className="texto-suave">{medio.nota}</span>}
                </span>
              </li>
            ))}
          </ul>
          <p className="texto-suave">
            Después de transferir, informe el pago con el comprobante para que el club lo confirme. También puede
            pagar en efectivo al presidente.
          </p>
        </>
      )}
    </Tarjeta>
  );
}
