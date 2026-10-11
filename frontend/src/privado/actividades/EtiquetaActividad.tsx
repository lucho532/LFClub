import type { EstadoEntrenamiento, EstadoPartido } from '../../compartido/api/tiposActividades';
import type { Tono } from '../../compartido/componentes/Aviso';
import { EtiquetaEstado } from '../../compartido/componentes/EtiquetaEstado';

type Props =
  | { tipo: 'ENTRENAMIENTO'; estado: EstadoEntrenamiento }
  | { tipo: 'PARTIDO'; estado: EstadoPartido; faltaResultado?: boolean };

const ESTADOS: Record<EstadoEntrenamiento | EstadoPartido, { tono: Tono; texto: string }> = {
  PROGRAMADO: { tono: 'exito', texto: 'Programado' },
  CANCELADO: { tono: 'error', texto: 'Cancelado' },
  PROXIMO: { tono: 'info', texto: 'Próximo' },
  JUGADO: { tono: 'neutro', texto: 'Jugado' },
};

/** El estado escrito de una actividad; un partido próximo que ya empezó dice "Falta el resultado". */
export function textoDeEstado(props: Props): { tono: Tono; texto: string } {
  if (props.tipo === 'PARTIDO' && props.estado === 'PROXIMO' && props.faltaResultado) {
    return { tono: 'aviso', texto: 'Falta el resultado' };
  }

  return ESTADOS[props.estado];
}

/**
 * Etiqueta de una actividad: su tipo ("Entreno" o "Partido", como el lienzo) y su estado escrito.
 * El color acompaña, pero el estado siempre se lee como texto (RF-036). No decide el estado: lo
 * muestra tal como lo entrega la API.
 */
export function EtiquetaActividad(props: Props) {
  const estado = textoDeEstado(props);
  return (
    <span className="etiquetas-actividad">
      <span className={`etiqueta-tipo tipo-${props.tipo === 'PARTIDO' ? 'partido' : 'entreno'}`}>
        {props.tipo === 'PARTIDO' ? 'Partido' : 'Entreno'}
      </span>
      <EtiquetaEstado tono={estado.tono} texto={estado.texto} />
    </span>
  );
}
