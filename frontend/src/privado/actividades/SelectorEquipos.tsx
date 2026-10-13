import type { EquipoDeCategoria } from './useCategoriasGestionables';

interface Props {
  /** Las categorías elegidas, con sus equipos activos. */
  categorias: { categoriaId: string; anio: number; equipos: EquipoDeCategoria[] }[];
  elegidos: string[];
  alCambiar: (elegidos: string[]) => void;
  /** Equipos que el entrenamiento ya tenía y se desactivaron: se muestran fijos y marcados. */
  desactivados?: { equipoId: string; nombre: string; anio: number }[];
  error?: string;
}

/**
 * Los equipos activos de las categorías elegidas como casillas, agrupados por año (spec 013,
 * RF-005). Solo aparece si alguna de las categorías tiene equipos activos; sin ninguno marcado dice
 * "Toda la categoría". El estado de cada uno se lee también como texto. No decide qué se ofrece.
 */
export function SelectorEquipos({ categorias, elegidos, alCambiar, desactivados = [], error }: Props) {
  const conEquipos = categorias.filter((categoria) => categoria.equipos.length > 0);
  if (conEquipos.length === 0 && desactivados.length === 0) {
    return null;
  }

  function conmutar(equipoId: string) {
    alCambiar(
      elegidos.includes(equipoId) ? elegidos.filter((elegido) => elegido !== equipoId) : [...elegidos, equipoId],
    );
  }

  return (
    <fieldset className="opciones selector-equipos">
      <legend>Equipos</legend>
      {conEquipos.map((categoria) => (
        <div key={categoria.categoriaId} className="grupo-de-equipos">
          <strong>{categoria.anio}</strong>
          <div className="fila">
            {categoria.equipos.map((equipo) => {
              const elegido = elegidos.includes(equipo.equipoId);
              return (
                <label key={equipo.equipoId} className="casilla-equipo">
                  <input type="checkbox" checked={elegido} onChange={() => conmutar(equipo.equipoId)} />
                  <span>{`Equipo ${equipo.nombre}`}</span>
                </label>
              );
            })}
          </div>
        </div>
      ))}
      {desactivados.map((equipo) => (
        <label key={equipo.equipoId} className="casilla-equipo">
          <input type="checkbox" checked disabled readOnly />
          <span>{`${equipo.anio} · Equipo ${equipo.nombre} (desactivado)`}</span>
        </label>
      ))}
      {elegidos.length === 0 && <span className="texto-suave">Toda la categoría</span>}
      {error && <span className="campo-error">{error}</span>}
    </fieldset>
  );
}
