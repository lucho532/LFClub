import { useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { ErrorApi } from '../../compartido/api/errores';
import type { PagoDto } from '../../compartido/api/tiposPagos';
import { useCarga } from '../../compartido/api/useCarga';
import { Aviso } from '../../compartido/componentes/Aviso';
import { Tarjeta } from '../../compartido/componentes/Tarjeta';
import { fechaYHora } from '../../compartido/formato';
import { useClub } from '../contextoClub';
import { AnularPago } from './AnularPago';
import { apiPagos } from './apiPagos';
import { formatoMoneda } from './formatoMoneda';
import { InsigniaDeEstadoDePago } from './InsigniaDeEstado';
import { ResguardoDePago } from './ResguardoDePago';
import { fechaDelPago, rutaDeLaCuenta } from './textosPagos';

/**
 * Recibo de un pago (spec 015, RF-024 y RF-025): su número, el club, el jugador, el valor, el medio y la
 * entidad, la fecha, lo que cubre, quién lo confirmó y lo que le quedó debiendo al jugador. Un pago
 * anulado sigue a la vista con su sello, quién lo anuló, cuándo y por qué. El PRESIDENTE puede anular uno
 * confirmado; nadie lo edita ni lo borra (§16.3). Debajo va su resguardo en imagen, que se descarga o se
 * comparte (historia 8). Lo ven el PRESIDENTE, el DIRECTIVO y la cuenta del jugador del pago.
 */
export function DetalleDePago() {
  const { club } = useClub();
  const { pagoId = '' } = useParams();
  const carga = useCarga(`${club.clubId}/pago/${pagoId}`, () => apiPagos.obtener(club.clubId, pagoId));
  const [anulado, setAnulado] = useState<PagoDto | null>(null);
  const pago = anulado ?? carga.datos;
  const noLoVe = carga.fallo instanceof ErrorApi && carga.fallo.status === 404;

  return (
    <>
      <Link to={pago?.usuarioRolId ? rutaDeLaCuenta(club.clubId, pago.usuarioRolId) : `/club/${club.clubId}/pagos`}>
        ← Cuenta
      </Link>
      {noLoVe ? (
        <Aviso tono="aviso">Este pago no existe o no puedes verlo.</Aviso>
      ) : (
        carga.error && <Aviso tono="error">{carga.error}</Aviso>
      )}
      {!pago && carga.cargando && <p className="texto-suave">Cargando…</p>}
      {pago && (
        <>
          <div className="cabecera">
            <h1>Recibo {pago.numeroRecibo ? `n.º ${pago.numeroRecibo}` : 'de pago'}</h1>
          </div>
          <Tarjeta titulo={club.nombre} acciones={<InsigniaDeEstadoDePago estado={pago.estado} />}>
            <dl className="datos-de-pago">
              <Dato titulo="Jugador" valor={pago.jugadorNombre} />
              <Dato titulo="Valor" valor={formatoMoneda(pago.valor)} />
              <Dato
                titulo="Medio"
                valor={pago.medio === 'EFECTIVO' ? 'Efectivo' : `Transferencia${pago.entidad ? ` (${pago.entidad})` : ''}`}
              />
              <Dato titulo="Fecha del pago" valor={fechaDelPago(pago.fecha)} />
              {pago.referencia && <Dato titulo="Referencia" valor={pago.referencia} />}
              {pago.concepto && <Dato titulo="Cubre" valor={pago.concepto} />}
              {pago.confirmadoPorNombre && pago.confirmadoEn && (
                <Dato titulo="Confirmado por" valor={`${pago.confirmadoPorNombre}, ${fechaYHora(pago.confirmadoEn)}`} />
              )}
              {pago.saldoTrasPago !== null && (
                <Dato
                  titulo="Después de este pago"
                  valor={pago.saldoTrasPago === 0 ? 'Al día' : `Debe ${formatoMoneda(pago.saldoTrasPago)}`}
                />
              )}
            </dl>
            {pago.estado === 'ANULADO' && (
              <Aviso tono="error">
                Pago anulado por {pago.anuladoPorNombre}
                {pago.anuladoEn ? ` el ${fechaYHora(pago.anuladoEn)}` : ''}: {pago.motivoAnulacion}
              </Aviso>
            )}
            {pago.estado === 'CONFIRMADO' && club.miRol === 'PRESIDENTE' && (
              <AnularPago clubId={club.clubId} pago={pago} alAnular={setAnulado} />
            )}
          </Tarjeta>
          {pago.tieneResguardo && pago.numeroRecibo !== null && (
            <Tarjeta titulo="Resguardo de pago">
              <ResguardoDePago
                key={`${pago.id}-${pago.estado}`}
                clubId={club.clubId}
                pagoId={pago.id}
                numeroRecibo={pago.numeroRecibo}
                titulo={`Resguardo de pago n.º ${pago.numeroRecibo}`}
              />
            </Tarjeta>
          )}
        </>
      )}
    </>
  );
}

function Dato({ titulo, valor }: { titulo: string; valor: string }) {
  return (
    <div className="fila">
      <dt className="texto-suave">{titulo}</dt>
      <dd>{valor}</dd>
    </div>
  );
}
