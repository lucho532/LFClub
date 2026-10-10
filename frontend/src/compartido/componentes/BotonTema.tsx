import { temaContrario } from '../tema/tema';
import { useTema } from '../tema/useTema';

const NOMBRE = { claro: 'claro', oscuro: 'oscuro' } as const;

/**
 * Botón visible para cambiar entre tema claro y oscuro (RF-032). Muestra un sol en el tema oscuro
 * y una luna en el claro: el tema al que se cambia. Como el dibujo no basta para todos, su nombre
 * accesible y su texto emergente dicen con palabras a qué tema cambia.
 */
export function BotonTema() {
  const { tema, alternar } = useTema();
  const texto = `Cambiar a tema ${NOMBRE[temaContrario(tema)]}`;

  return (
    <button type="button" className="boton-icono" onClick={alternar} aria-label={texto} title={texto}>
      {tema === 'oscuro' ? <Sol /> : <Luna />}
    </button>
  );
}

function Sol() {
  return (
    <svg viewBox="0 0 24 24" width="22" height="22" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" aria-hidden="true">
      <circle cx="12" cy="12" r="4.5" />
      <path d="M12 2v2M12 20v2M4.93 4.93l1.41 1.41M17.66 17.66l1.41 1.41M2 12h2M20 12h2M4.93 19.07l1.41-1.41M17.66 6.34l1.41-1.41" />
    </svg>
  );
}

function Luna() {
  return (
    <svg viewBox="0 0 24 24" width="22" height="22" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
      <path d="M21 12.79A9 9 0 1 1 11.21 3 7 7 0 0 0 21 12.79z" />
    </svg>
  );
}
