import { useState } from 'react';
import { api } from '../../compartido/api/cliente';
import { mensajeDe } from '../../compartido/api/errores';
import type { EntrenamientoDto } from '../../compartido/api/tiposActividades';
import type { AudienciaDeEntrenamientoDto, JugadorSumableDto } from '../../compartido/api/tiposAudiencia';
import { DialogoConfirmacion } from '../../compartido/componentes/DialogoConfirmacion';
import { Boton } from '../../compartido/componentes/Boton';
import { BuscadorDeJugadores } from '../actividades/BuscadorDeJugadores';
import { EtiquetaDePertenencia } from '../actividades/EtiquetaDePertenencia';
import { SelectorEquipos } from '../actividades/SelectorEquipos';
import type { CategoriaGestionable } from '../actividades/useCategoriasGestionables';

interface Props {
  clubId: string;
  abierto: boolean;
  entrenamiento: EntrenamientoDto;
  /** Las categorías del entrenamiento con sus equipos activos. */
  categorias: CategoriaGestionable[];
  alGuardar: (entrenamiento: EntrenamientoDto) => void;
  alCancelar: () => void;
}

/**
 * Cambia a quién va dirigido un entrenamiento (spec 013, RF-020): los equipos activos de sus
 * categorías (los desactivados que ya tenía aparecen fijos y marcados) y los jugadores añadidos,
 * que se suman con el buscador y se quitan con "Quitar". Se guarda todo en un solo envío. Quitar a
 * un jugador pide confirmar y dice que no se le avisará. No decide quién puede cambiarlo.
 */
export function DialogoAudiencia({ clubId, abierto, entrenamiento, categorias, alGuardar, alCancelar }: Props) {
  const equiposActuales = (entrenamiento.equipos ?? []).filter((equipo) => equipo.activo).map((equipo) => equipo.equipoId);
  const [equipos, setEquipos] = useState<string[]>(equiposActuales);
  const [anadidos, setAnadidos] = useState<JugadorSumableDto[]>(entrenamiento.anadidos ?? []);
  const [aQuitar, setAQuitar] = useState<JugadorSumableDto | null>(null);
  const [guardando, setGuardando] = useState(false);
  const [error, setError] = useState<string | undefined>();
  const desactivados = (entrenamiento.equipos ?? []).filter((equipo) => !equipo.activo);

  async function guardar() {
    setGuardando(true);
    setError(undefined);
    const cuerpo: AudienciaDeEntrenamientoDto = {
      equipoIds: equipos,
      jugadorIds: anadidos.map((jugador) => jugador.usuarioRolId),
    };
    try {
      alGuardar(
        await api.put<EntrenamientoDto>(
          `/api/clubes/${clubId}/entrenamientos/${entrenamiento.entrenamientoId}/audiencia`,
          cuerpo,
        ),
      );
    } catch (fallo) {
      setError(mensajeDe(fallo));
    } finally {
      setGuardando(false);
    }
  }

  return (
    <>
      <DialogoConfirmacion
        abierto={abierto && aQuitar === null}
        titulo="Cambiar audiencia"
        textoConfirmar="Guardar audiencia"
        cargando={guardando}
        error={error}
        alConfirmar={() => void guardar()}
        alCancelar={alCancelar}
      >
        <div className="columna">
          <SelectorEquipos
            categorias={categorias}
            elegidos={equipos}
            alCambiar={setEquipos}
            desactivados={desactivados.map((equipo) => ({
              equipoId: equipo.equipoId,
              nombre: equipo.nombre,
              anio: equipo.anio,
            }))}
          />
          <fieldset className="opciones">
            <legend>Jugadores añadidos</legend>
            {anadidos.length === 0 && <p className="texto-suave">Todavía no hay jugadores añadidos.</p>}
            <ul className="lista-de-inicio">
              {anadidos.map((jugador) => (
                <li key={jugador.usuarioRolId}>
                  <span>{`${jugador.nombres} ${jugador.apellidos}`}</span>
                  <span className="texto-suave">{`Categoría ${jugador.categoria.anio}`}</span>
                  <EtiquetaDePertenencia pertenencia="ANADIDO" />
                  <Boton variante="secundario" onClick={() => setAQuitar(jugador)}>
                    Quitar
                  </Boton>
                </li>
              ))}
            </ul>
          </fieldset>
          <BuscadorDeJugadores
            clubId={clubId}
            excluidos={anadidos.map((jugador) => jugador.usuarioRolId)}
            alElegir={(jugador) => setAnadidos((previos) => [...previos, jugador])}
          />
        </div>
      </DialogoConfirmacion>
      <DialogoConfirmacion
        abierto={aQuitar !== null}
        titulo="Quitar jugador"
        textoConfirmar="Quitar"
        peligro
        alConfirmar={() => {
          setAnadidos((previos) => previos.filter((jugador) => jugador.usuarioRolId !== aQuitar?.usuarioRolId));
          setAQuitar(null);
        }}
        alCancelar={() => setAQuitar(null)}
      >
        <p>
          {aQuitar
            ? `${aQuitar.nombres} ${aQuitar.apellidos} dejará de ver este entrenamiento. No se le avisará.`
            : ''}
        </p>
      </DialogoConfirmacion>
    </>
  );
}
