import { useState } from 'react';
import { api } from '../../compartido/api/cliente';
import type { GrupoDeJugadoresDto } from '../../compartido/api/tiposJugadores';
import type { MensualidadDto } from '../../compartido/api/tiposPagos';
import { useCarga } from '../../compartido/api/useCarga';
import { Aviso } from '../../compartido/componentes/Aviso';
import { Campo } from '../../compartido/componentes/Campo';
import { nombreCompleto } from '../categorias/textos';
import { tituloDeGrupo } from '../jugadores/textos';
import { apiMensualidad } from './apiMensualidad';
import { FilaMensualidadJugador } from './FilaMensualidadJugador';

interface Props {
  clubId: string;
  mensualidad: MensualidadDto;
  /** Falso para quien solo consulta (DIRECTIVO). */
  editable: boolean;
  /** Recibe la mensualidad que devolvió la API tras cada cambio. */
  alCambiar: (mensualidad: MensualidadDto) => void;
}

/** Texto sin mayúsculas ni tildes, para buscar sin fijarse en cómo se escribió el nombre. */
const simple = (texto: string) => texto.normalize('NFD').replace(/\p{Diacritic}/gu, '').toLowerCase();

/**
 * La mensualidad de cada jugador (spec 015, RF-012): una tarjeta por jugador, agrupada por categoría, donde
 * se escribe su valor propio o se vuelve al del club. Con cientos de jugadores se busca por nombre y se
 * puede ver solo a quienes tienen valor propio; no hay que seleccionar nada ni ir al final de la lista.
 * Cada cambio solo afecta a los cargos que se generen después.
 */
export function MensualidadPorJugador({ clubId, mensualidad, editable, alCambiar }: Props) {
  const grupos = useCarga(`${clubId}/jugadores`, () => api.get<GrupoDeJugadoresDto[]>(`/api/clubes/${clubId}/jugadores`));
  const [busqueda, setBusqueda] = useState('');
  const [soloPropios, setSoloPropios] = useState(false);

  const valorPropio = new Map(mensualidad.jugadoresConValorPropio.map((jugador) => [jugador.usuarioRolId, jugador.valor]));
  const buscado = simple(busqueda.trim());

  const visibles = (grupos.datos ?? [])
    .map((grupo) => ({
      grupo,
      jugadores: grupo.jugadores.filter(
        (jugador) =>
          simple(nombreCompleto(jugador)).includes(buscado) && (!soloPropios || valorPropio.has(jugador.usuarioRolId)),
      ),
    }))
    .filter(({ jugadores }) => jugadores.length > 0);

  return (
    <div className="columna">
      <div className="fila-con-campos">
        <Campo etiqueta="Buscar jugador" valor={busqueda} alCambiar={setBusqueda} placeholder="Nombre o apellido" />
      </div>
      <label className="fila">
        <input type="checkbox" checked={soloPropios} onChange={(evento) => setSoloPropios(evento.target.checked)} />
        <span>Mostrar solo a quienes tienen valor propio ({mensualidad.jugadoresConValorPropio.length})</span>
      </label>
      {!grupos.datos && grupos.cargando && <p className="texto-suave">Cargando…</p>}
      {grupos.error && <Aviso tono="error">{grupos.error}</Aviso>}
      {grupos.datos && visibles.length === 0 && (
        <p className="texto-suave">
          {grupos.datos.length === 0 ? 'Todavía no hay jugadores.' : 'Ningún jugador coincide con lo que buscas.'}
        </p>
      )}
      {visibles.map(({ grupo, jugadores }) => (
        <section key={grupo.categoria?.categoriaId ?? 'sin-categoria'} className="columna">
          <h3>{tituloDeGrupo(grupo)}</h3>
          <ul className="lista-mensualidad-jugadores">
            {jugadores.map((jugador) => {
              const propio = valorPropio.get(jugador.usuarioRolId) ?? null;
              return (
                <FilaMensualidadJugador
                  key={`${jugador.usuarioRolId}-${propio ?? 'club'}`}
                  nombre={nombreCompleto(jugador)}
                  valorPropio={propio}
                  valorClub={mensualidad.valorClub}
                  editable={editable}
                  alGuardar={async (valor) =>
                    alCambiar(await apiMensualidad.fijarValorPropio(clubId, { usuarioRolIds: [jugador.usuarioRolId], valor }))
                  }
                />
              );
            })}
          </ul>
        </section>
      ))}
    </div>
  );
}
