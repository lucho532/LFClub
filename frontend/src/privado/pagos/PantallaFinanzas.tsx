import { useState } from 'react';
import { Link } from 'react-router-dom';
import type { JugadorQueDebeDto } from '../../compartido/api/tiposPagos';
import { Aviso } from '../../compartido/componentes/Aviso';
import { Campo } from '../../compartido/componentes/Campo';
import { Tabla, type Columna } from '../../compartido/componentes/Tabla';
import { Tarjeta } from '../../compartido/componentes/Tarjeta';
import { useClub } from '../contextoClub';
import { formatoMoneda } from './formatoMoneda';
import { InsigniaDeEstadoDeCuenta } from './InsigniaDeEstado';
import { TablaPorCategoria } from './TablaPorCategoria';
import { nombreDelMes, rutaDeLaCuenta } from './textosPagos';
import { useFinanzas } from './useFinanzas';

/**
 * Pagos → Finanzas (spec 015, historia 9): el recaudo del mes, la cartera pendiente, los totales por
 * categoría y quiénes deben, para el PRESIDENTE y el DIRECTIVO. Las cifras salen solo de los pagos
 * confirmados no anulados. Es información para llevar las cuentas: no avisa a nadie ni cobra (§16.8). El
 * ENTRENADOR no ve esta pantalla.
 */
export function PantallaFinanzas() {
  const { club } = useClub();
  const [mes, setMes] = useState<string | undefined>();
  const finanzas = useFinanzas(club.clubId, mes);
  const datos = finanzas.datos;

  const columnas: Columna<JugadorQueDebeDto>[] = [
    {
      titulo: 'Jugador',
      celda: (fila) => (
        <span>
          <Link to={rutaDeLaCuenta(club.clubId, fila.usuarioRolId)}>{fila.nombreCompleto}</Link>
          {fila.retirado && <span className="texto-suave"> · Retirado</span>}
        </span>
      ),
    },
    { titulo: 'Estado', celda: (fila) => <InsigniaDeEstadoDeCuenta estado={fila.estado} /> },
    { titulo: 'Debe', celda: (fila) => formatoMoneda(fila.saldo) },
  ];

  return (
    <>
      <div className="cabecera">
        <h1>Finanzas</h1>
      </div>
      <Tarjeta>
        <Campo
          etiqueta="Mes"
          type="month"
          valor={(mes ?? datos?.mes ?? '').slice(0, 7)}
          alCambiar={(valor) => setMes(valor ? `${valor}-01` : undefined)}
        />
      </Tarjeta>
      {finanzas.error && <Aviso tono="error">{finanzas.error}</Aviso>}
      {!datos && finanzas.cargando && <p className="texto-suave">Cargando…</p>}
      {datos && (
        <>
          <Tarjeta titulo={nombreDelMes(datos.mes)}>
            <dl className="cifras-de-finanzas">
              <div className="cifra">
                <dt>Recaudo del mes</dt>
                <dd>{formatoMoneda(datos.recaudoMes)}</dd>
              </div>
              <div className="cifra">
                <dt>Cartera pendiente</dt>
                <dd>{formatoMoneda(datos.carteraPendiente)}</dd>
              </div>
              <div className="cifra">
                <dt>
                  <Link to={`/club/${club.clubId}/pagos/en-revision`}>Pagos en revisión</Link>
                </dt>
                <dd>{datos.pagosEnRevision}</dd>
              </div>
            </dl>
          </Tarjeta>
          <Tarjeta titulo="Por categoría">
            <TablaPorCategoria filas={datos.porCategoria} />
          </Tarjeta>
          <Tarjeta titulo="Quiénes deben">
            <Tabla
              descripcion="Jugadores con saldo pendiente"
              columnas={columnas}
              filas={datos.jugadoresQueDeben}
              clave={(fila) => fila.usuarioRolId}
              vacio="Nadie debe nada."
            />
          </Tarjeta>
        </>
      )}
    </>
  );
}
