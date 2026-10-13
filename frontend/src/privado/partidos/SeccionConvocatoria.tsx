import { useState } from 'react';
import { api } from '../../compartido/api/cliente';
import { mensajeDe } from '../../compartido/api/errores';
import type { PartidoDto, PlanillaPartidoDto } from '../../compartido/api/tiposActividades';
import { useCarga } from '../../compartido/api/useCarga';
import { Aviso } from '../../compartido/componentes/Aviso';
import { Boton } from '../../compartido/componentes/Boton';
import { NombrePersona } from '../../compartido/componentes/NombrePersona';
import { Tarjeta } from '../../compartido/componentes/Tarjeta';
import { ResumenRespuestas } from '../entrenamientos/ResumenRespuestas';
import type { JugadorSumableDto } from '../../compartido/api/tiposAudiencia';
import { EtiquetaDePertenencia } from '../actividades/EtiquetaDePertenencia';
import { EditorConvocatoria, type JugadorConvocable } from './EditorConvocatoria';

interface Props {
  clubId: string;
  partido: PartidoDto;
}

/**
 * La convocatoria de un partido para quien ve la planilla (el presidente y quien entrena alguna de
 * sus categorías): cada convocado con la categoría con la que se le convocó. Quien gestiona un
 * partido próximo la cambia con el editor. Nunca se pinta a la cuenta de un jugador, y la API
 * tampoco se la daría (RF-012).
 */
export function SeccionConvocatoria({ clubId, partido }: Props) {
  const ruta = `/api/clubes/${clubId}/partidos/${partido.partidoId}`;
  const carga = useCarga(`${ruta}/jugadores#${partido.estado}`, () => api.get<PlanillaPartidoDto>(`${ruta}/jugadores`));
  const [editando, setEditando] = useState(false);
  const [elegidos, setElegidos] = useState<string[]>([]);
  const [sumados, setSumados] = useState<JugadorSumableDto[]>([]);
  const [guardando, setGuardando] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const planilla = carga.datos;
  const puedeCambiar = partido.permisos.gestiona && partido.estado === 'PROXIMO';

  const jugadores: JugadorConvocable[] = planilla
    ? [
        ...planilla.convocados.map((fila) => ({
          usuarioRolId: fila.usuarioRolId,
          nombres: fila.nombres,
          apellidos: fila.apellidos,
          anio: fila.categoria.anio,
          equipoIds: fila.equipoIds,
          anadido: fila.anadido,
        })),
        ...planilla.convocables.map((fila) => ({
          usuarioRolId: fila.usuarioRolId,
          nombres: fila.nombres,
          apellidos: fila.apellidos,
          anio: fila.categoria.anio,
          equipoIds: fila.equipoIds,
        })),
        // Los que se sumaron con el buscador en esta edición: de otra categoría, marcados "Añadido".
        ...sumados.map((jugador) => ({
          usuarioRolId: jugador.usuarioRolId,
          nombres: jugador.nombres,
          apellidos: jugador.apellidos,
          anio: jugador.categoria.anio,
          equipoIds: [],
          anadido: true,
        })),
      ]
    : [];

  async function guardar() {
    setGuardando(true);
    setError(null);
    try {
      carga.fijar(await api.put<PlanillaPartidoDto>(`${ruta}/convocatoria`, { usuarioRolIds: elegidos }));
      setEditando(false);
    } catch (fallo) {
      setError(mensajeDe(fallo));
    } finally {
      setGuardando(false);
    }
  }

  return (
    <Tarjeta
      titulo="Convocatoria"
      acciones={
        puedeCambiar &&
        planilla &&
        !editando && (
          <Boton
            variante="secundario"
            onClick={() => {
              setElegidos(planilla.convocados.map((fila) => fila.usuarioRolId));
              setSumados([]);
              setEditando(true);
            }}
          >
            Cambiar convocatoria
          </Boton>
        )
      }
    >
      {carga.error && <Aviso tono="error">{carga.error}</Aviso>}
      {!planilla && carga.cargando && <p className="texto-suave">Cargando…</p>}
      {planilla && editando && (
        <>
          <EditorConvocatoria
            clubId={clubId}
            jugadores={jugadores}
            equipos={planilla.equipos}
            elegidos={elegidos}
            alCambiar={setElegidos}
            alSumar={(jugador) => {
              setSumados((previos) => [...previos, jugador]);
              setElegidos((previos) => [...previos, jugador.usuarioRolId]);
            }}
          />
          {error && <Aviso tono="error">{error}</Aviso>}
          <div className="fila">
            <Boton cargando={guardando} textoCargando="Guardando…" onClick={() => void guardar()}>
              Guardar convocatoria
            </Boton>
            <Boton variante="secundario" disabled={guardando} onClick={() => setEditando(false)}>
              Cancelar
            </Boton>
          </div>
        </>
      )}
      {planilla && !editando && planilla.convocados.length === 0 && (
        <p className="texto-suave">Todavía no hay jugadores convocados.</p>
      )}
      {planilla && !editando && planilla.convocados.length > 0 && (
        <>
          <ul className="lista-de-inicio">
            {planilla.convocados.map((fila) => (
              <li key={fila.usuarioRolId}>
                <NombrePersona
                  nombre={`${fila.nombres} ${fila.apellidos}`}
                  enlace={fila.enlace ?? null}
                  clubId={clubId}
                  textoVolver="Partido"
                />
                <span className="texto-suave">Categoría {fila.categoria.anio}</span>
                {fila.anadido && <EtiquetaDePertenencia pertenencia="ANADIDO" />}
              </li>
            ))}
          </ul>
          {partido.estado !== 'JUGADO' && (
            <ResumenRespuestas clubId={clubId} textoVolver="Partido" jugadores={planilla.convocados} />
          )}
        </>
      )}
    </Tarjeta>
  );
}
