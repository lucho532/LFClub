import { Link } from 'react-router-dom';
import type { TarifaDeClubDto } from '../compartido/api/tiposPagoPlataforma';
import type { Carga } from '../compartido/api/useCarga';
import { Aviso } from '../compartido/componentes/Aviso';
import { Tabla, type Columna } from '../compartido/componentes/Tabla';
import { Tarjeta } from '../compartido/componentes/Tarjeta';
import { textoDeRango } from '../compartido/formato';
import { formatoMoneda } from '../privado/pagos/formatoMoneda';
import { textoDelPeriodo } from '../privado/pagoPlataforma/textosPagoPlataforma';

const COLUMNAS: Columna<TarifaDeClubDto>[] = [
  {
    titulo: 'Club',
    celda: (club) => <Link to={`/plataforma/clubes/${club.clubId}`}>{club.nombre}</Link>,
  },
  { titulo: 'Jugadores', celda: (club) => club.jugadores },
  {
    titulo: 'Rango',
    celda: (club) => (club.rango ? textoDeRango(club.rango.desde, club.rango.hasta) : 'Sin rango'),
  },
  {
    titulo: 'Valor mensual',
    celda: (club) => <strong>{club.valorMensual > 0 ? formatoMoneda(club.valorMensual) : '—'}</strong>,
  },
  {
    titulo: 'Cobro desde',
    celda: (club) => (club.mesDeInicioDelCobro ? textoDelPeriodo(club.mesDeInicioDelCobro) : 'No se cobra'),
  },
];

interface Props {
  carga: Carga<TarifaDeClubDto[]>;
}

/**
 * Lo que le corresponde pagar a cada club con la tarifa vigente: sus jugadores activos, el rango que le toca
 * y el valor mensual. Es una vista previa; el cobro real se genera al empezar el mes y conserva su valor.
 */
export function TarifasPorClub({ carga }: Props) {
  return (
    <Tarjeta titulo="Tarifa de cada club">
      {carga.error && <Aviso tono="error">{carga.error}</Aviso>}
      {!carga.datos && carga.cargando && <p className="texto-suave">Cargando…</p>}
      {carga.datos && (
        <Tabla
          descripcion="Tarifa que le corresponde a cada club"
          columnas={COLUMNAS}
          filas={carga.datos}
          clave={(club) => club.clubId}
          vacio="Todavía no hay ningún club."
        />
      )}
    </Tarjeta>
  );
}
