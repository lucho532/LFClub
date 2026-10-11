import type { CategoriaGestionable } from './useCategoriasGestionables';

interface Props {
  categorias: CategoriaGestionable[];
  elegidas: string[];
  alCambiar: (elegidas: string[]) => void;
  /** Mensaje de error del campo, tal como lo devuelve la API. */
  error?: string;
  /** Al cambiar una actividad sus categorías no se cambian: se muestran sin poder tocarlas. */
  fijas?: boolean;
}

/**
 * Las categorías de una actividad como botones que se encienden y se apagan, como el lienzo. El
 * estado de cada uno se lee también como texto ("elegida") y con `aria-pressed`, no solo por el
 * color (RF-036). No decide qué categorías se ofrecen.
 */
export function SelectorCategorias({ categorias, elegidas, alCambiar, error, fijas = false }: Props) {
  function conmutar(categoriaId: string) {
    alCambiar(
      elegidas.includes(categoriaId)
        ? elegidas.filter((elegida) => elegida !== categoriaId)
        : [...elegidas, categoriaId],
    );
  }

  return (
    <fieldset className="opciones selector-categorias">
      <legend>Categorías</legend>
      {categorias.length === 0 ? (
        <p className="texto-suave">No tienes categorías activas para las que crear actividades.</p>
      ) : (
        <div className="fila">
          {categorias.map((categoria) => {
            const elegida = elegidas.includes(categoria.categoriaId);
            return (
              <button
                key={categoria.categoriaId}
                type="button"
                className={`boton-conmutable${elegida ? ' encendido' : ''}`}
                aria-pressed={elegida}
                disabled={fijas}
                onClick={() => conmutar(categoria.categoriaId)}
              >
                {categoria.anio}
                {elegida && <span className="solo-lectores"> (elegida)</span>}
                {elegida && <span aria-hidden="true"> ✓</span>}
              </button>
            );
          })}
        </div>
      )}
      {fijas && <span className="texto-suave">Las categorías de una actividad no se cambian.</span>}
      {error && <span className="campo-error">{error}</span>}
    </fieldset>
  );
}
