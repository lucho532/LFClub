import { Tarjeta } from '../../compartido/componentes/Tarjeta';

/** Lo que dice el espacio reservado de los entrenamientos, para todos los roles (RF-014). */
export const SIN_ENTRENAMIENTOS = 'Los entrenamientos todavía no están disponibles en la aplicación.';

/**
 * Espacio reservado "Próximos entrenamientos" del inicio (spec 007, RF-014). Los entrenamientos
 * todavía no existen: un texto fijo, sin datos de ejemplo ni petición a la API, para que nada se
 * pueda tomar por real (CE-007).
 */
export function TarjetaProximosEntrenamientos() {
  return (
    <Tarjeta titulo="Próximos entrenamientos">
      <p className="texto-suave">{SIN_ENTRENAMIENTOS}</p>
    </Tarjeta>
  );
}
