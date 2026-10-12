import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { Tarjeta } from '../../compartido/componentes/Tarjeta';
import { useClub } from '../contextoClub';
import { FormularioEntrenamiento } from './FormularioEntrenamiento';
import { FormularioPartido } from './FormularioPartido';
import { useCategoriasGestionables } from './useCategoriasGestionables';

type Tipo = 'PARTIDO' | 'ENTRENAMIENTO';

/**
 * "Nueva actividad", según el lienzo (`Convocatoria`): el selector "Partido / Entrenamiento", las
 * categorías que la persona gestiona como botones y los campos de cada tipo; cada uno se guarda en
 * su propio endpoint y al terminar lleva al detalle (o al calendario, si se programaron varios
 * entrenamientos). Sin costo de arbitraje, mensaje para los
 * convocados ni borradores, que están fuera de alcance (plan, nota 1). Quien decide si se puede
 * crear es la API.
 */
export function FormularioActividad() {
  const { club } = useClub();
  const navegar = useNavigate();
  const { categorias, cargando } = useCategoriasGestionables(club);
  const [tipo, setTipo] = useState<Tipo>('PARTIDO');
  const base = `/club/${club.clubId}`;

  return (
    <>
      <Link to={`${base}/calendario`}>← Calendario</Link>
      <div className="cabecera">
        <h1>Nueva actividad</h1>
      </div>
      <div className="fila" role="group" aria-label="Tipo de actividad">
        {(['PARTIDO', 'ENTRENAMIENTO'] as const).map((opcion) => (
          <button
            key={opcion}
            type="button"
            className={`boton-conmutable${tipo === opcion ? ' encendido' : ''}`}
            aria-pressed={tipo === opcion}
            onClick={() => setTipo(opcion)}
          >
            {opcion === 'PARTIDO' ? 'Partido' : 'Entrenamiento'}
          </button>
        ))}
      </div>
      <Tarjeta titulo={tipo === 'PARTIDO' ? 'Partido' : 'Entrenamiento'}>
        {cargando ? (
          <p className="texto-suave">Cargando…</p>
        ) : tipo === 'PARTIDO' ? (
          <FormularioPartido
            key="partido"
            clubId={club.clubId}
            categorias={categorias}
            alGuardar={(partido) => navegar(`${base}/partidos/${partido.partidoId}`)}
          />
        ) : (
          <FormularioEntrenamiento
            key="entrenamiento"
            clubId={club.clubId}
            categorias={categorias}
            alGuardar={(entrenamiento, cuantos) =>
              navegar(cuantos > 1 ? `${base}/calendario` : `${base}/entrenamientos/${entrenamiento.entrenamientoId}`)
            }
          />
        )}
      </Tarjeta>
    </>
  );
}
