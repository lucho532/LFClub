import { useState } from 'react';
import { Link } from 'react-router-dom';
import type { PagoEnRevisionDto } from '../../compartido/api/tiposPagos';
import { Boton } from '../../compartido/componentes/Boton';
import { Tarjeta } from '../../compartido/componentes/Tarjeta';
import { fechaYHora } from '../../compartido/formato';
import { ConfirmarPago } from './ConfirmarPago';
import { formatoMoneda } from './formatoMoneda';
import { InsigniaDeEstadoDePago } from './InsigniaDeEstado';
import { SoporteDelPago } from './SoporteDelPago';
import { fechaDelPago, rutaDeLaCuenta } from './textosPagos';

interface Props {
  clubId: string;
  revision: PagoEnRevisionDto;
  /** Solo el PRESIDENTE confirma y rechaza; el DIRECTIVO consulta. */
  puedeRevisar: boolean;
  alTerminar: () => void;
}

/**
 * Una transferencia informada, con el jugador, el valor, la entidad, la fecha y lo que el jugador debe, el
 * soporte a un toque y, para el PRESIDENTE, las acciones de confirmar y rechazar (spec 015, RF-022).
 */
export function TarjetaPagoEnRevision({ clubId, revision, puedeRevisar, alTerminar }: Props) {
  const { pago } = revision;
  const [revisando, setRevisando] = useState(false);
  const debe = revision.cargosPendientes.reduce((suma, cargo) => suma + cargo.pendiente, 0);

  return (
    <Tarjeta titulo={revision.jugadorNombre} acciones={<InsigniaDeEstadoDePago estado={pago.estado} />}>
      <p>
        <strong>{formatoMoneda(pago.valor)}</strong>
        <span className="texto-suave">
          {' '}
          · {pago.entidad} · {fechaDelPago(pago.fecha)}
          {pago.referencia ? ` · Ref. ${pago.referencia}` : ''}
        </span>
      </p>
      <p className="texto-suave">
        Informado el {fechaYHora(pago.creadoEn)}. El jugador debe {formatoMoneda(debe)}.
        {pago.usuarioRolId && (
          <>
            {' '}
            <Link to={rutaDeLaCuenta(clubId, pago.usuarioRolId)}>Ver su cuenta</Link>
          </>
        )}
      </p>
      {pago.tieneSoporte && <SoporteDelPago clubId={clubId} pagoId={pago.id} />}
      {puedeRevisar &&
        (revisando ? (
          <ConfirmarPago
            clubId={clubId}
            revision={revision}
            alTerminar={alTerminar}
            alCancelar={() => setRevisando(false)}
          />
        ) : (
          <div className="fila">
            <Boton onClick={() => setRevisando(true)}>Confirmar o rechazar</Boton>
          </div>
        ))}
    </Tarjeta>
  );
}
