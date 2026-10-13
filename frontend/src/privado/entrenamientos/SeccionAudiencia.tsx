import type { EntrenamientoDto } from '../../compartido/api/tiposActividades';
import { Boton } from '../../compartido/componentes/Boton';
import { EtiquetaDePertenencia } from '../actividades/EtiquetaDePertenencia';
import { textoDeAudiencia } from '../actividades/textosDeAudiencia';

interface Props {
  entrenamiento: EntrenamientoDto;
  /** Abre el diálogo de "Cambiar audiencia"; solo se ofrece a quien dirige. */
  alCambiar: () => void;
}

/**
 * A quién va dirigido un entrenamiento (spec 013, RF-021). Para todos: "Para: 2014 · Equipo A", con
 * "Equipo desactivado" escrito cuando corresponde. Para quien dirige, además, los jugadores
 * añadidos con su etiqueta y el botón "Cambiar audiencia" (solo con el entrenamiento programado).
 * No decide quién dirige: lo dice `permisos.dirige`, que la API vuelve a comprobar.
 */
export function SeccionAudiencia({ entrenamiento, alCambiar }: Props) {
  const equipos = entrenamiento.equipos ?? [];
  const dirige = entrenamiento.permisos.dirige === true;
  const anadidos = entrenamiento.anadidos ?? [];

  if (equipos.length === 0 && !dirige) {
    return null;
  }

  return (
    <div className="columna seccion-audiencia">
      <p>
        <strong>Para: </strong>
        {textoDeAudiencia(entrenamiento.categorias, equipos)}
        {equipos.some((equipo) => !equipo.activo) && (
          <>
            {' '}
            <EtiquetaDePertenencia pertenencia="EQUIPO_DESACTIVADO" />
          </>
        )}
      </p>
      {dirige && (
        <>
          {anadidos.length > 0 && (
            <div>
              <strong>Jugadores añadidos</strong>
              <ul className="lista-de-inicio">
                {anadidos.map((jugador) => (
                  <li key={jugador.usuarioRolId}>
                    <span>{`${jugador.nombres} ${jugador.apellidos}`}</span>
                    <span className="texto-suave">{`Categoría ${jugador.categoria.anio}`}</span>
                    <EtiquetaDePertenencia pertenencia="ANADIDO" />
                  </li>
                ))}
              </ul>
            </div>
          )}
          {entrenamiento.estado === 'PROGRAMADO' && (
            <div className="fila">
              <Boton variante="secundario" onClick={alCambiar}>
                Cambiar audiencia
              </Boton>
            </div>
          )}
        </>
      )}
    </div>
  );
}
