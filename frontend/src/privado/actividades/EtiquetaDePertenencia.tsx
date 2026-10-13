/** Cómo pertenece un jugador a una actividad. */
export type Pertenencia = 'EQUIPO' | 'ANADIDO' | 'EQUIPO_DESACTIVADO';

interface Props {
  pertenencia: Pertenencia;
  /** El nombre del equipo, si es de un equipo. */
  equipo?: string;
}

const TEXTO: Record<Pertenencia, string> = {
  EQUIPO: 'Equipo',
  ANADIDO: 'Añadido',
  EQUIPO_DESACTIVADO: 'Equipo desactivado',
};

/**
 * La pertenencia de un jugador a una actividad ("Equipo A", "Añadido", "Equipo desactivado"),
 * siempre escrita: no se distingue solo por el color (§24). No decide quién pertenece.
 */
export function EtiquetaDePertenencia({ pertenencia, equipo }: Props) {
  const texto = pertenencia === 'EQUIPO' && equipo ? `Equipo ${equipo}` : TEXTO[pertenencia];
  return <span className={`etiqueta-pertenencia pertenencia-${pertenencia.toLowerCase()}`}>{texto}</span>;
}
