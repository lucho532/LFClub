import type { AlcanceComunicado } from '../../compartido/api/tiposComunicados';
import type { CategoriaDto } from '../../compartido/api/tiposCategorias';

interface Props {
  /** Si el rol puede dirigirse a todo el club (PRESIDENTE y DIRECTIVO). */
  conTodoElClub: boolean;
  /** Categorías activas que la persona puede usar; para un ENTRENADOR, las suyas. */
  categorias: CategoriaDto[];
  alcance: AlcanceComunicado;
  categoriaIds: string[];
  /** Categoría de la que se eligen los equipos (solo con alcance EQUIPOS). */
  categoriaDeEquipos: string;
  equipoIds: string[];
  alCambiarAlcance: (alcance: AlcanceComunicado) => void;
  alCambiarCategorias: (categoriaIds: string[]) => void;
  alCambiarCategoriaDeEquipos: (categoriaId: string) => void;
  alCambiarEquipos: (equipoIds: string[]) => void;
  /** Mensaje de error de la audiencia, tal como lo dice la API o la pantalla. */
  error?: string;
}

function conmutar(elegidos: string[], id: string): string[] {
  return elegidos.includes(id) ? elegidos.filter((elegido) => elegido !== id) : [...elegidos, id];
}

/**
 * "Para quién": una de tres opciones según el rol (spec 011, RF-004 a RF-007): "Todo el club" (solo
 * PRESIDENTE y DIRECTIVO), "Categorías" con casillas y "Equipos de una categoría", con la categoría
 * primero y luego las casillas de sus equipos. El estado de cada opción se lee también en texto y con
 * `aria-pressed`. No decide qué se puede ofrecer: recibe ya las categorías permitidas.
 */
export function SelectorAudiencia({
  conTodoElClub,
  categorias,
  alcance,
  categoriaIds,
  categoriaDeEquipos,
  equipoIds,
  alCambiarAlcance,
  alCambiarCategorias,
  alCambiarCategoriaDeEquipos,
  alCambiarEquipos,
  error,
}: Props) {
  const opciones: { valor: AlcanceComunicado; texto: string }[] = [
    ...(conTodoElClub ? [{ valor: 'CLUB' as const, texto: 'Todo el club' }] : []),
    { valor: 'CATEGORIAS', texto: 'Categorías' },
    { valor: 'EQUIPOS', texto: 'Equipos de una categoría' },
  ];
  const deEquipos = categorias.find((categoria) => categoria.categoriaId === categoriaDeEquipos);

  return (
    <fieldset className="opciones selector-audiencia">
      <legend>Para quién</legend>
      <div className="fila" role="group" aria-label="Para quién">
        {opciones.map((opcion) => (
          <button
            key={opcion.valor}
            type="button"
            className={`boton-conmutable${alcance === opcion.valor ? ' encendido' : ''}`}
            aria-pressed={alcance === opcion.valor}
            onClick={() => alCambiarAlcance(opcion.valor)}
          >
            {opcion.texto}
          </button>
        ))}
      </div>

      {alcance === 'CLUB' && <p className="texto-suave">Les llegará a todas las familias con un jugador activo.</p>}

      {alcance === 'CATEGORIAS' &&
        (categorias.length === 0 ? (
          <p className="texto-suave">No tienes categorías activas a las que dirigir comunicados.</p>
        ) : (
          <div className="fila">
            {categorias.map((categoria) => {
              const elegida = categoriaIds.includes(categoria.categoriaId);
              return (
                <button
                  key={categoria.categoriaId}
                  type="button"
                  className={`boton-conmutable${elegida ? ' encendido' : ''}`}
                  aria-pressed={elegida}
                  onClick={() => alCambiarCategorias(conmutar(categoriaIds, categoria.categoriaId))}
                >
                  {categoria.anio}
                  {elegida && <span className="solo-lectores"> (elegida)</span>}
                  {elegida && <span aria-hidden="true"> ✓</span>}
                </button>
              );
            })}
          </div>
        ))}

      {alcance === 'EQUIPOS' && (
        <>
          <div className="fila" role="group" aria-label="Categoría de los equipos">
            {categorias.map((categoria) => (
              <button
                key={categoria.categoriaId}
                type="button"
                className={`boton-conmutable${categoriaDeEquipos === categoria.categoriaId ? ' encendido' : ''}`}
                aria-pressed={categoriaDeEquipos === categoria.categoriaId}
                onClick={() => alCambiarCategoriaDeEquipos(categoria.categoriaId)}
              >
                {categoria.anio}
              </button>
            ))}
          </div>
          {deEquipos &&
            (deEquipos.equipos.length === 0 ? (
              <p className="texto-suave">Esa categoría no tiene equipos activos.</p>
            ) : (
              <div className="fila">
                {deEquipos.equipos.map((equipo) => {
                  const elegido = equipoIds.includes(equipo.equipoId);
                  return (
                    <button
                      key={equipo.equipoId}
                      type="button"
                      className={`boton-conmutable${elegido ? ' encendido' : ''}`}
                      aria-pressed={elegido}
                      onClick={() => alCambiarEquipos(conmutar(equipoIds, equipo.equipoId))}
                    >
                      {equipo.nombre}
                      {elegido && <span className="solo-lectores"> (elegido)</span>}
                      {elegido && <span aria-hidden="true"> ✓</span>}
                    </button>
                  );
                })}
              </div>
            ))}
          {!deEquipos && categorias.length > 0 && <p className="texto-suave">Elige primero una categoría.</p>}
        </>
      )}

      {error && (
        <span className="campo-error" role="alert">
          {error}
        </span>
      )}
    </fieldset>
  );
}
