import { api } from '../../compartido/api/cliente';
import type { CategoriaDetalleDto, JugadorDeCategoriaDto } from '../../compartido/api/tiposCategorias';
import { Aviso } from '../../compartido/componentes/Aviso';
import { Boton } from '../../compartido/componentes/Boton';
import { EtiquetaEstado } from '../../compartido/componentes/EtiquetaEstado';
import { NombrePersona } from '../../compartido/componentes/NombrePersona';
import { Tabla, type Columna } from '../../compartido/componentes/Tabla';
import { Tarjeta } from '../../compartido/componentes/Tarjeta';
import { BotonRetirarJugador } from './BotonRetirarJugador';
import { DialogoCambiarCategoria } from './DialogoCambiarCategoria';
import { EstadoDeDocumentacion } from './EstadoDeDocumentacion';
import { nombreCompleto } from './textos';
import { useAccion } from './useAccion';
import { useUbicarJugador } from './useUbicarJugador';

interface Props {
  clubId: string;
  categoria: CategoriaDetalleDto;
  esPresidente: boolean;
  /** Recibe la categoría tal como la devolvió la API tras poner o sacar a un jugador de un equipo. */
  alCambiar: (categoria: CategoriaDetalleDto) => void;
  /** Se llama cuando un jugador salió de la categoría, para volver a pedirla. */
  alRecargar: () => void;
}

/**
 * Jugadores de una categoría: nombre, apellidos, año de nacimiento y equipos (RF-032). El nombre abre
 * la ficha del jugador (RF-034), que es donde están sus demás datos y donde la API decide qué ve cada quien.
 * A quien está en una categoría que no es la de su año se le señala con texto, no solo con color
 * (RF-016). El presidente ve una casilla por equipo en cada fila, que guarda al instante,
 * y puede pasar a cada jugador a otra categoría. El nombre abre la ficha del jugador, y desde ella
 * se vuelve a esta categoría (spec 008).
 */
export function SeccionJugadores({ clubId, categoria, esPresidente, alCambiar, alRecargar }: Props) {
  const { ocupado, mensaje, ejecutar, fijarMensaje } = useAccion();
  const mover = useUbicarJugador(clubId, (texto) => {
    fijarMensaje({ tono: 'exito', texto });
    alRecargar();
  });
  const conCasillas = esPresidente && categoria.equipos.length > 0;

  /** Marca o desmarca un equipo del jugador; la API devuelve la categoría ya actualizada. */
  const cambiarEquipo = (jugador: JugadorDeCategoriaDto, equipoId: string, juega: boolean) =>
    ejecutar(async () => {
      const ruta = `/api/clubes/${clubId}/categorias/${categoria.categoriaId}/equipos/${equipoId}/jugadores/${jugador.usuarioRolId}`;
      try {
        alCambiar(juega ? await api.put<CategoriaDetalleDto>(ruta, {}) : await api.delete<CategoriaDetalleDto>(ruta));
      } catch (fallo) {
        // Otra persona pudo moverlo mientras tanto: se vuelve a pedir la categoría.
        alRecargar();
        throw fallo;
      }

      return null;
    });

  const columnas: Columna<JugadorDeCategoriaDto>[] = [
    {
      titulo: 'Jugador',
      celda: (jugador) => (
        <strong>
          <NombrePersona
            nombre={nombreCompleto(jugador)}
            enlace={{ usuarioRolId: jugador.usuarioRolId, tipo: 'JUGADOR' }}
            clubId={clubId}
            textoVolver={`Categoría ${categoria.anio}`}
          />
        </strong>
      ),
    },
    {
      titulo: 'Año de nacimiento',
      celda: (jugador) => (
        <span className="fila">
          {jugador.anioNacimiento}
          {jugador.fueraDeSuAnio && <EtiquetaEstado tono="aviso" texto="Fuera de su año" />}
        </span>
      ),
    },
    {
      titulo: 'Equipos',
      celda: (jugador) =>
        conCasillas ? (
          <span className="casillas">
            {categoria.equipos.map((equipo) => (
              <label key={equipo.equipoId} className="casilla">
                <input
                  type="checkbox"
                  checked={jugador.equipos.some((suyo) => suyo.equipoId === equipo.equipoId)}
                  disabled={ocupado}
                  onChange={(evento) => void cambiarEquipo(jugador, equipo.equipoId, evento.target.checked)}
                />
                {equipo.nombre}
              </label>
            ))}
          </span>
        ) : (
          jugador.equipos.map((equipo) => equipo.nombre).join(', ') || 'Sin equipo'
        ),
    },
  ];

  // La API solo entrega el estado de la documentación al presidente y a los directivos (RF-032).
  if (categoria.jugadores.some((jugador) => jugador.documentosPendientes !== undefined)) {
    columnas.push({
      titulo: 'Documentación',
      celda: (jugador) => <EstadoDeDocumentacion pendientes={jugador.documentosPendientes} />,
    });
  }

  if (esPresidente) {
    columnas.push({
      titulo: 'Acciones',
      celda: (jugador) => (
        <span className="fila">
          <Boton variante="secundario" onClick={() => mover.abrir(jugador)} disabled={ocupado}>
            Cambiar de categoría
          </Boton>
          <BotonRetirarJugador
            clubId={clubId}
            jugador={jugador}
            deshabilitado={ocupado}
            alRetirar={(texto) => {
              fijarMensaje({ tono: 'exito', texto });
              alRecargar();
            }}
          />
        </span>
      ),
    });
  }

  return (
    <Tarjeta titulo="Jugadores">
      {conCasillas && (
        <p className="texto-suave">Marca en qué equipos juega cada jugador. Puede estar en varios o en ninguno.</p>
      )}
      {mensaje && <Aviso tono={mensaje.tono}>{mensaje.texto}</Aviso>}
      <Tabla
        descripcion={`Jugadores de la categoría ${categoria.anio}`}
        columnas={columnas}
        filas={categoria.jugadores}
        clave={(jugador) => jugador.usuarioRolId}
        vacio="Esta categoría todavía no tiene jugadores."
      />

      {mover.jugador && (
        <DialogoCambiarCategoria
          key={mover.jugador.usuarioRolId}
          clubId={clubId}
          jugador={mover.jugador}
          categoriaActualId={categoria.categoriaId}
          cargando={mover.enviando}
          error={mover.error}
          alConfirmar={(destino) => void mover.confirmar(destino)}
          alCancelar={mover.cerrar}
        />
      )}
    </Tarjeta>
  );
}
