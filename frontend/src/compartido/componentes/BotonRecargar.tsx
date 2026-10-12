/**
 * Botón visible para recargar la pantalla, al lado del de cambiar el tema. Sirve sobre todo en la
 * aplicación del teléfono, donde no hay botón de recargar del navegador: vuelve a pedir todo a la
 * API sin cerrar la sesión. Su nombre accesible y su texto emergente lo dicen con palabras.
 */
export function BotonRecargar() {
  const texto = 'Recargar la pantalla';

  return (
    <button type="button" className="boton-icono" onClick={() => window.location.reload()} aria-label={texto} title={texto}>
      <svg
        viewBox="0 0 24 24"
        width="22"
        height="22"
        fill="none"
        stroke="currentColor"
        strokeWidth="2"
        strokeLinecap="round"
        strokeLinejoin="round"
        aria-hidden="true"
      >
        <path d="M20 12a8 8 0 1 1-2.34-5.66" />
        <path d="M20 4v5h-5" />
      </svg>
    </button>
  );
}
