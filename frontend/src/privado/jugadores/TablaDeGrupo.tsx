import { Link } from 'react-router-dom';
import type { JugadorDeListaDto } from '../../compartido/api/tiposJugadores';
import { Tabla, type Columna } from '../../compartido/componentes/Tabla';
import { estadoVolver } from '../../compartido/navegacion/useVolver';
import { nombreCompleto, rutaDeFicha } from '../categorias/textos';
import { FotoJugador } from './FotoJugador';
import { TEXTO_AL_DIA, TITULO_AL_DIA, textoDeEquipos } from './textos';

interface Props {
  clubId: string;
  /** Título del grupo, para describir la tabla a los lectores de pantalla. */
  titulo: string;
  jugadores: JugadorDeListaDto[];
}

/**
 * Los jugadores de un grupo de la lista "Jugadores": su foto en miniatura, que se pide solo cuando
 * la fila entra en pantalla, o sus iniciales; su nombre completo, que abre su ficha con "← Jugadores"
 * para volver a la lista, sus equipos o "Sin equipo", y la columna "Al
 * día", que es un espacio reservado con el mismo texto suave para todos (RF-006). Un nombre largo
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
    {
      titulo: 'Al día',
      celda: () => (
        <span className="texto-suave" title={TITULO_AL_DIA}>
          {TEXTO_AL_DIA}
          <span className="solo-lectores">: {TITULO_AL_DIA}</span>
        </span>
      ),
    },
  ];

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
