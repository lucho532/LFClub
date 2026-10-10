import { Link } from 'react-router-dom';
import type { JugadorDeListaDto } from '../../compartido/api/tiposJugadores';
import { inicialesDe } from '../../compartido/componentes/Avatar';
import { Tabla, type Columna } from '../../compartido/componentes/Tabla';
import { nombreCompleto, rutaDeFicha } from '../categorias/textos';
import { TEXTO_AL_DIA, TITULO_AL_DIA, textoDeEquipos } from './textos';

interface Props {
  clubId: string;
  /** Título del grupo, para describir la tabla a los lectores de pantalla. */
  titulo: string;
  jugadores: JugadorDeListaDto[];
}

/**
 * Los jugadores de un grupo de la lista "Jugadores": sus iniciales, su nombre completo, que abre
 * su ficha recordando que se llegó desde la lista, sus equipos o "Sin equipo", y la columna "Al
 * día", que es un espacio reservado con el mismo texto suave para todos (RF-006). Un nombre largo
 * se corta con puntos suspensivos sin desbordar la pantalla.
 */
export function TablaDeGrupo({ clubId, titulo, jugadores }: Props) {
  const columnas: Columna<JugadorDeListaDto>[] = [
    {
      titulo: 'Jugador',
      celda: (jugador) => (
        <span className="jugador-de-lista">
          <span className="distintivo foto-jugador-miniatura" aria-hidden="true">
            {inicialesDe(jugador.nombres, jugador.apellidos)}
          </span>
          <Link
            className="nombre-cortado"
            to={rutaDeFicha(clubId, jugador.usuarioRolId)}
            state={{ desde: 'jugadores' }}
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
