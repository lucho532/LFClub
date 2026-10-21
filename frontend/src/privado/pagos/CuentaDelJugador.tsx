import { useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { ErrorApi } from '../../compartido/api/errores';
import type { PagoDto } from '../../compartido/api/tiposPagos';
import { Aviso } from '../../compartido/componentes/Aviso';
import { Boton } from '../../compartido/componentes/Boton';
import { Tarjeta } from '../../compartido/componentes/Tarjeta';
import { useVolver } from '../../compartido/navegacion/useVolver';
import { useClub } from '../contextoClub';
import { formatoMoneda } from './formatoMoneda';
import { ComoPagar } from './ComoPagar';
import { InsigniaDeEstadoDeCuenta } from './InsigniaDeEstado';
import { ListaDeCargos } from './ListaDeCargos';
import { InformarTransferencia } from './InformarTransferencia';
import { ListaDePagos } from './ListaDePagos';
import { RegistrarPago } from './RegistrarPago';
import { useCuentaJugador } from './useCuentaJugador';

/**
 * Estado de cuenta de un jugador (spec 015, historia 3): su estado en texto, su saldo, sus cargos y sus
 * pagos. La familia llega desde su inicio o desde "Pagos"; el PRESIDENTE y el DIRECTIVO, desde la fila
 * del jugador en la lista. Lo que la persona no puede ver (la cuenta de otro jugador) dice lo mismo que
 * una cuenta que no existe. Deber no restringe nada: esta pantalla solo informa (§16.8). El PRESIDENTE
 * registra desde aquí un pago recibido directamente (historia 4); al guardarlo, la cuenta se refresca.
 */
export function CuentaDelJugador() {
  const { club } = useClub();
  const { usuarioRolId = '' } = useParams();
  const cuenta = useCuentaJugador(club.clubId, usuarioRolId);
  const datos = cuenta.datos;
  const [registrando, setRegistrando] = useState(false);
  const [registrado, setRegistrado] = useState<PagoDto | null>(null);
  const [informando, setInformando] = useState(false);
  const [informado, setInformado] = useState(false);
  const noLaVe = cuenta.fallo instanceof ErrorApi && cuenta.fallo.status === 404;
  const volver = useVolver(
    club.miRol === 'JUGADOR'
      ? { ruta: `/club/${club.clubId}`, texto: 'Inicio' }
      : { ruta: `/club/${club.clubId}/jugadores`, texto: 'Jugadores' },
  );

  function alRegistrar(pago: PagoDto) {
    setRegistrando(false);
    setRegistrado(pago);
    cuenta.recargar();
  }

  function alInformar() {
    setInformando(false);
    setInformado(true);
    cuenta.recargar();
  }

  const pendientes = datos?.cargos.filter((cargo) => cargo.pendiente > 0).reverse() ?? [];

  return (
    <>
      <Link to={volver.ruta}>← {volver.texto}</Link>
      {noLaVe ? (
        <Aviso tono="aviso">Esta cuenta no existe o no puedes verla.</Aviso>
      ) : (
        cuenta.error && <Aviso tono="error">{cuenta.error}</Aviso>
      )}
      {!datos && cuenta.cargando && <p className="texto-suave">Cargando…</p>}
      {datos && (
        <>
          <div className="cabecera">
            <h1>{datos.nombreCompleto}</h1>
          </div>
          <Tarjeta titulo="Estado de cuenta">
            <div className="saldo-de-cuenta">
              <InsigniaDeEstadoDeCuenta estado={datos.estado} />
              <strong>{formatoMoneda(datos.saldo)}</strong>
              <span className="texto-suave">{datos.saldo === 0 ? 'No debe nada' : 'Saldo pendiente'}</span>
            </div>
            {registrado && (
              <Aviso tono="exito">
                Pago registrado de {formatoMoneda(registrado.valor)}. Recibo n.º {registrado.numeroRecibo}.
              </Aviso>
            )}
            {informado && (
              <Aviso tono="exito">
                Recibimos su transferencia. Queda en revisión: el estado de cuenta cambia cuando el presidente la confirme.
              </Aviso>
            )}
            {datos.puedeInformar && datos.saldo > 0 && !informando && (
              <div className="fila">
                <Boton onClick={() => setInformando(true)}>Informé una transferencia</Boton>
              </div>
            )}
            {datos.puedeRegistrar && pendientes.length > 0 && !registrando && (
              <div className="fila">
                <Boton onClick={() => setRegistrando(true)}>Registrar pago</Boton>
              </div>
            )}
          </Tarjeta>
          {informando && (
            <Tarjeta titulo="Informar una transferencia">
              <InformarTransferencia
                clubId={club.clubId}
                usuarioRolId={datos.usuarioRolId}
                pendiente={datos.saldo}
                medios={datos.mediosPago}
                alInformar={alInformar}
                alCancelar={() => setInformando(false)}
              />
            </Tarjeta>
          )}
          {registrando && (
            <Tarjeta titulo="Registrar un pago">
              <RegistrarPago
                clubId={club.clubId}
                usuarioRolId={datos.usuarioRolId}
                pendientes={pendientes}
                alRegistrar={alRegistrar}
                alCancelar={() => setRegistrando(false)}
              />
            </Tarjeta>
          )}
          {datos.puedeInformar && <ComoPagar medios={datos.mediosPago} />}
          <Tarjeta titulo="Cargos">
            <ListaDeCargos cargos={datos.cargos} />
          </Tarjeta>
          <Tarjeta titulo="Pagos">
            <ListaDePagos clubId={club.clubId} pagos={datos.pagos} />
          </Tarjeta>
        </>
      )}
    </>
  );
}
