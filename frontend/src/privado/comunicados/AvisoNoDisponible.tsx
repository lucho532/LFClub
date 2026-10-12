import { textosComunicados } from './textos';

/**
 * El mensaje de que la actividad de un aviso ya no está disponible para la cuenta (spec 012, RF-021).
 * Va debajo de la fila, en una región de estado educada, sin diálogo ni navegación: el aviso queda
 * leído y la familia no pierde su lugar en la lista. No explica el motivo.
 */
export function AvisoNoDisponible() {
  return (
    <p className="aviso-no-disponible" role="status" aria-live="polite">
      {textosComunicados.actividadNoDisponible}
    </p>
  );
}
