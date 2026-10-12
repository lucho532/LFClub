import { Link } from 'react-router-dom';
import { IconoCampana } from './Iconos';

interface Props {
  /** Comunicados y avisos sin leer de la cuenta. */
  sinLeer: number;
  /** Ruta de la lista de comunicados. */
  a: string;
}

/** Desde cuántos sin leer la insignia deja de decir el número exacto. */
const LIMITE_DE_LA_INSIGNIA = 100;

/** Lo que dice el número de la insignia: el exacto, o "99+" desde 100. */
export function textoDeInsignia(sinLeer: number): string {
  return sinLeer >= LIMITE_DE_LA_INSIGNIA ? '99+' : String(sinLeer);
}

/** Lo que dicen los lectores de pantalla de la campana (spec 011, RF-034; spec 012, RF-018). */
export function nombreAccesibleDeCampana(sinLeer: number): string {
  if (sinLeer <= 0) {
    return 'Novedades, nada por leer';
  }

  return sinLeer >= LIMITE_DE_LA_INSIGNIA
    ? 'Novedades, más de 99 sin leer'
    : `Novedades, ${sinLeer} sin leer`;
}

/**
 * Campana de la barra superior de la familia, con la insignia de lo que hay sin leer: comunicados y avisos. Es un
 * botón de icono de 44 × 44 px, sin texto visible, que lleva a la lista; el número va escrito y
 * nunca depende del color. Con 0 no hay insignia. No parpadea ni se anima: el cambio del número lo
 * anuncia una región de estado educada. No conoce la sesión ni consulta nada: recibe el contador y
 * el destino.
 */
export function CampanaComunicados({ sinLeer, a }: Props) {
  const nombre = nombreAccesibleDeCampana(sinLeer);

  return (
    <>
      <Link to={a} className="campana" aria-label={nombre} title={nombre}>
        <IconoCampana />
        {sinLeer > 0 && (
          <span className="campana-insignia" aria-hidden="true">
            {textoDeInsignia(sinLeer)}
          </span>
        )}
      </Link>
      <span className="solo-lectores" role="status" aria-live="polite">
        {sinLeer > 0 ? nombre : ''}
      </span>
    </>
  );
}
