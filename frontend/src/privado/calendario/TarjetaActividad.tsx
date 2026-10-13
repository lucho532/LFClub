import type { ReactNode } from 'react';
import { Link } from 'react-router-dom';
import type {
  CategoriaDeActividadDto,
  EntrenamientoResumenDto,
  PartidoResumenDto,
} from '../../compartido/api/tiposActividades';
import { horaCorta, rangoDeHoras } from '../../compartido/fechas';
import { EtiquetaActividad } from '../actividades/EtiquetaActividad';
import { textoDeAudiencia } from '../actividades/textosDeAudiencia';
import { nombreRival } from '../partidos/textos';

/** "Categoría 2014", "Categorías 2014 y 2015" o "Categorías 2013, 2014 y 2015". */
export function textoDeCategorias(categorias: CategoriaDeActividadDto[]): string {
  const anios = categorias.map((categoria) => String(categoria.anio));
  if (anios.length <= 1) {
    return `Categoría ${anios[0] ?? ''}`.trim();
  }

  return `Categorías ${anios.slice(0, -1).join(', ')} y ${anios[anios.length - 1]}`;
}

/** "Local" o "Visitante". */
export function textoDeCondicion(condicion: PartidoResumenDto['condicion']): string {
  return condicion === 'LOCAL' ? 'Local' : 'Visitante';
}

type Props = {
  clubId: string;
  pie?: ReactNode;
  /** Si quien mira gestiona la actividad: entonces se ofrece "Editar". Solo comodidad; decide la API. */
  editable?: boolean;
} & (
  | { tipo: 'ENTRENAMIENTO'; entrenamiento: EntrenamientoResumenDto }
  | { tipo: 'PARTIDO'; partido: PartidoResumenDto }
);

/**
 * Una actividad en el calendario o en una lista, como las tarjetas del lienzo (`M-Calendario`): su
 * etiqueta con el tipo y el estado escritos, la hora, el lugar, las categorías y, en un partido, el
 * rival y si es local o visitante. Lleva a su detalle y, para quien la gestiona, mientras no esté
 * cancelada ni jugada, a editarla. No decide qué actividades se muestran.
 */
export function TarjetaActividad(props: Props) {
  const ruta =
    props.tipo === 'ENTRENAMIENTO'
      ? `/club/${props.clubId}/entrenamientos/${props.entrenamiento.entrenamientoId}`
      : `/club/${props.clubId}/partidos/${props.partido.partidoId}`;
  const sePuedeEditar =
    props.editable === true &&
    (props.tipo === 'ENTRENAMIENTO' ? props.entrenamiento.estado === 'PROGRAMADO' : props.partido.estado === 'PROXIMO');

  return (
    <article className="tarjeta-actividad">
      {props.tipo === 'ENTRENAMIENTO' ? (
        <>
          <EtiquetaActividad tipo="ENTRENAMIENTO" estado={props.entrenamiento.estado} />
          <Link to={ruta} className="tarjeta-actividad-titulo">
            {rangoDeHoras(props.entrenamiento.horaInicio, props.entrenamiento.horaFin)}
          </Link>
          <span className="dato-largo">{props.entrenamiento.lugar}</span>
          <span className="texto-suave">
            {(props.entrenamiento.equipos?.length ?? 0) > 0
              ? `Para: ${textoDeAudiencia(props.entrenamiento.categorias, props.entrenamiento.equipos)}`
              : textoDeCategorias(props.entrenamiento.categorias)}
          </span>
        </>
      ) : (
        <>
          <EtiquetaActividad
            tipo="PARTIDO"
            estado={props.partido.estado}
            faltaResultado={props.partido.faltaResultado}
          />
          <Link to={ruta} className="tarjeta-actividad-titulo dato-largo">
            {`Contra ${nombreRival(props.partido)}`}
          </Link>
          <span>
            {horaCorta(props.partido.hora)} · {textoDeCondicion(props.partido.condicion)}
          </span>
          <span className="dato-largo">{props.partido.lugar}</span>
          <span className="texto-suave">{textoDeCategorias(props.partido.categorias)}</span>
        </>
      )}
      {sePuedeEditar && (
        <Link to={`${ruta}?editar=1`} className="tarjeta-actividad-editar">
          Editar
        </Link>
      )}
      {props.pie}
    </article>
  );
}
