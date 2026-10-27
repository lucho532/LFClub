import type { RangoTarifaDto, TarifaVigenteDto } from '../../compartido/api/tiposPagoPlataforma';
import { EtiquetaEstado } from '../../compartido/componentes/EtiquetaEstado';
import { Tabla, type Columna } from '../../compartido/componentes/Tabla';
import { Tarjeta } from '../../compartido/componentes/Tarjeta';
import { textoDeRango } from '../../compartido/formato';
import { formatoMoneda } from '../pagos/formatoMoneda';

interface Props {
  tarifa: TarifaVigenteDto;
}

/**
 * «Cómo se cobra» (spec 017): una tabla informativa con los rangos de jugadores y su precio mensual fijo, con la
 * fila de su club marcada, y unas líneas que explican cuándo se genera el cobro, qué jugadores cuentan y cuánto
 * tiempo hay para pagar. Es de solo lectura: el valor de cada cobro queda fijo al generarse, así que un cambio de
 * tarifa solo afecta a los cobros siguientes. Todo viene del servidor: no calcula nada. Si todavía no hay tarifa
 * definida, lo dice.
 */
export function TablaDeTarifa({ tarifa }: Props) {
  const esDelClub = (rango: RangoTarifaDto) => tarifa.rangoDelClub?.desde === rango.desde;

  const columnas: Columna<RangoTarifaDto>[] = [
    {
      titulo: 'Jugadores activos',
      celda: (rango) => (
        <>
          {textoDeRango(rango.desde, rango.hasta)}{' '}
          {esDelClub(rango) && <EtiquetaEstado tono="info" texto={`Tu club hoy: ${tarifa.jugadores}`} />}
        </>
      ),
    },
    { titulo: 'Precio mensual', celda: (rango) => <strong>{formatoMoneda(rango.precio)}</strong> },
  ];

  return (
    <Tarjeta titulo="Cómo se cobra">
      {tarifa.rangos.length === 0 ? (
        <p className="texto-suave">La tarifa todavía no está definida: por ahora no se genera ningún cobro.</p>
      ) : (
        <Tabla
          descripcion="Precio mensual según los jugadores activos del club"
          columnas={columnas}
          filas={tarifa.rangos}
          clave={(rango) => String(rango.desde)}
          vacio=""
        />
      )}
      <ul className="lista-de-inicio">
        <li>Pagas un valor fijo cada mes, según cuántos jugadores activos tiene tu club ese mes.</li>
        <li>Solo cuentan los jugadores: los entrenadores, directivos y presidentes no suman.</li>
        <li>El cobro se genera el primer día de cada mes con los jugadores de ese momento y su valor ya no cambia.</li>
        <li>
          Tienes {tarifa.diasDeGracia} {tarifa.diasDeGracia === 1 ? 'día' : 'días'} desde que se genera para pagar; pasado ese
          plazo el club se suspende hasta que se pague.
        </li>
        <li>Si la tarifa cambia, solo aplica a los cobros que se generen después.</li>
      </ul>
    </Tarjeta>
  );
}
