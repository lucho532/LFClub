import { Link } from 'react-router-dom';
import type { JugadorDeListaDto } from '../../compartido/api/tiposJugadores';
import { Tabla, type Columna } from '../../compartido/componentes/Tabla';
import { estadoVolver } from '../../compartido/navegacion/useVolver';
import { nombreCompleto, rutaDeFicha } from '../categorias/textos';
import { FotoJugador } from './FotoJugador';
import { InsigniaDeEstadoDeCuenta } from '../pagos/InsigniaDeEstado';
import { rutaDeLaCuenta } from '../pagos/textosPagos';
import { textoDeEquipos } from './textos';

interface Props {
  clubId: string;
  /** Título del grupo, para describir la tabla a los lectores de pantalla. */
  titulo: string;
  jugadores: JugadorDeListaDto[];
}

/**
 * Los jugadores de un grupo de la lista "Jugadores": su foto en miniatura, que se pide solo cuando
 * la fila entra en pantalla, o sus iniciales; su nombre completo, que abre su ficha con "← Jugadores"
 * para volver a la lista, sus equipos o "Sin equipo", y la columna "Al día" con el estado de cuenta
 * real, en texto además del color, que abre su cuenta (spec 015, RF-034). La columna solo existe para
 * quien recibe el estado, el presidente y el directivo: al entrenador no se le pinta. Un nombre largo
 * se corta con puntos suspensivos sin desbordar la pantalla.
 */
export function TablaDeGrupo({ clubId, titulo, jugadores }: Props) {
  const columnas: Columna<JugadorDeListaDto>[] = [
    {
      titulo: 'Jugador',
      celda: (jugador) => (
        <span className="jugador-de-lista">
          <FotoJugador clubId={clubId} jugador={jugador} tamano="miniatura" perezosa />
          <Link
            className="nombre-cortado"
            to={rutaDeFicha(clubId, jugador.usuarioRolId)}
            state={estadoVolver(`/club/${clubId}/jugadores`, 'Jugadores')}
            title={nombreCompleto(jugador)}
          >
            <strong>{nombreCompleto(jugador)}</strong>
          </Link>
        </span>
      ),
    },
    { titulo: 'Equipos', celda: (jugador) => textoDeEquipos(jugador.equipos) },
  ];

  // El estado solo llega al presidente y al directivo; para el entrenador la columna no se pinta.
  if (jugadores.some((jugador) => jugador.estadoCuenta)) {
    columnas.push({
      titulo: 'Al día',
      celda: (jugador) =>
        jugador.estadoCuenta ? (
          <Link
            to={rutaDeLaCuenta(clubId, jugador.usuarioRolId)}
            state={estadoVolver(`/club/${clubId}/jugadores`, 'Jugadores')}
            title={`Abrir la cuenta de ${nombreCompleto(jugador)}`}
          >
            <InsigniaDeEstadoDeCuenta estado={jugador.estadoCuenta} />
          </Link>
        ) : null,
    });
  }

  return (
    <Tabla
      descripcion={`Jugadores: ${titulo}`}
      columnas={columnas}
      filas={jugadores}
      clave={(jugador) => jugador.usuarioRolId}
      vacio="Este grupo no tiene jugadores."
    />
  );
}
