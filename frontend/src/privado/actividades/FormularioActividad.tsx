import { Link, useNavigate } from 'react-router-dom';
import { Tarjeta } from '../../compartido/componentes/Tarjeta';
import { useClub } from '../contextoClub';
import { FormularioEntrenamiento } from './FormularioEntrenamiento';
import { useCategoriasGestionables } from './useCategoriasGestionables';

/**
 * "Nueva actividad", según el lienzo (`Convocatoria`): las categorías que la persona gestiona como
 * botones y los campos del entrenamiento. Al guardar lleva al detalle. Sin costo de arbitraje,
 * mensaje para los convocados ni borradores, que están fuera de alcance (plan, nota 1). Quien
 * decide si se puede programar es la API.
 */
export function FormularioActividad() {
  const { club } = useClub();
  const navegar = useNavigate();
  const { categorias, cargando } = useCategoriasGestionables(club);
  const base = `/club/${club.clubId}`;

  return (
    <>
      <Link to={`${base}/calendario`}>← Calendario</Link>
      <div className="cabecera">
        <h1>Nueva actividad</h1>
      </div>
      <Tarjeta titulo="Entrenamiento">
        {cargando ? (
          <p className="texto-suave">Cargando…</p>
        ) : (
          <FormularioEntrenamiento
            clubId={club.clubId}
            categorias={categorias}
            alGuardar={(entrenamiento) => navegar(`${base}/entrenamientos/${entrenamiento.entrenamientoId}`)}
          />
        )}
      </Tarjeta>
    </>
  );
}
