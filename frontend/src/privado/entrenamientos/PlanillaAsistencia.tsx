import { useState } from 'react';
import { api } from '../../compartido/api/cliente';
import { mensajeDe } from '../../compartido/api/errores';
import type { EntrenamientoDto, PlanillaEntrenamientoDto } from '../../compartido/api/tiposActividades';
import type { Carga } from '../../compartido/api/useCarga';
import { Aviso } from '../../compartido/componentes/Aviso';
import { Boton } from '../../compartido/componentes/Boton';
import { NombrePersona } from '../../compartido/componentes/NombrePersona';
import { Tarjeta } from '../../compartido/componentes/Tarjeta';
import { EtiquetaDePertenencia } from '../actividades/EtiquetaDePertenencia';

interface Props {
  clubId: string;
  entrenamiento: EntrenamientoDto;
  /** La planilla ya cargada: la comparte con el resumen de respuestas. */
  planilla: Carga<PlanillaEntrenamientoDto | null>;
}

/**
 * La asistencia de un entrenamiento para quien ve la planilla: por categoría, cada jugador con
 * "Presente" y "Ausente" escritos en botones que se encienden, o "Sin registrar" si nadie la marcó.
 * Guardar envía solo los que cambiaron. Antes del día del entrenamiento, o si está cancelado, se ve
 * sin poder marcar, con el motivo escrito. La API vuelve a comprobar a quién puede marcar.
 */
export function PlanillaAsistencia({ clubId, entrenamiento, planilla }: Props) {
  const [cambios, setCambios] = useState<Record<string, boolean>>({});
  const [guardando, setGuardando] = useState(false);
  const [mensaje, setMensaje] = useState<{ tono: 'exito' | 'error'; texto: string } | null>(null);
  const datos = planilla.datos;
  const marcable = datos?.jugadores.some((jugador) => jugador.marcable) ?? false;
  const anios = [...new Set((datos?.jugadores ?? []).map((jugador) => jugador.categoria.anio))];

  async function guardar() {
    setGuardando(true);
    setMensaje(null);
    try {
      const jugadores = Object.entries(cambios).map(([usuarioRolId, presente]) => ({ usuarioRolId, presente }));
      planilla.fijar(
        await api.put<PlanillaEntrenamientoDto>(
          `/api/clubes/${clubId}/entrenamientos/${entrenamiento.entrenamientoId}/asistencia`,
          { jugadores },
        ),
      );
      setCambios({});
      setMensaje({ tono: 'exito', texto: 'Asistencia guardada.' });
    } catch (fallo) {
      setMensaje({ tono: 'error', texto: mensajeDe(fallo) });
    } finally {
      setGuardando(false);
    }
  }

  return (
    <Tarjeta titulo="Asistencia">
      {planilla.error && <Aviso tono="error">{planilla.error}</Aviso>}
      {!datos && planilla.cargando && <p className="texto-suave">Cargando…</p>}
      {datos && !marcable && (
        <p className="texto-suave">
          {entrenamiento.estado === 'CANCELADO'
            ? 'Este entrenamiento está cancelado: no admite asistencia.'
            : 'La asistencia se registra desde el día del entrenamiento.'}
        </p>
      )}
      {datos?.jugadores.length === 0 && <p className="texto-suave">No hay jugadores en tus categorías.</p>}
      {anios.map((anio) => (
        <fieldset key={anio} className="opciones">
          <legend>Categoría {anio}</legend>
          <ul className="lista-asistencia">
            {datos?.jugadores
              .filter((jugador) => jugador.categoria.anio === anio)
              .map((jugador) => {
                const presente = cambios[jugador.usuarioRolId] ?? jugador.presente ?? null;
                return (
                  <li key={jugador.usuarioRolId}>
                    <span className="columna">
                      <NombrePersona
                        nombre={`${jugador.nombres} ${jugador.apellidos}`}
                        enlace={jugador.enlace ?? null}
                        clubId={clubId}
                        textoVolver="Entrenamiento"
                      />
                      {jugador.anadido && <EtiquetaDePertenencia pertenencia="ANADIDO" />}
                      {jugador.marcable && presente === null && <span className="texto-suave texto-pequeno">Sin registrar</span>}
                    </span>
                    {jugador.marcable ? (
                      <span className="fila">
                        {([true, false] as const).map((valor) => (
                          <button
                            key={String(valor)}
                            type="button"
                            className={`boton-conmutable${presente === valor ? ' encendido' : ''}`}
                            aria-pressed={presente === valor}
                            onClick={() => setCambios((previos) => ({ ...previos, [jugador.usuarioRolId]: valor }))}
                          >
                            {valor ? 'Presente' : 'Ausente'}
                          </button>
                        ))}
                      </span>
                    ) : (
                      <span className="texto-suave">
                        {presente === null ? 'Sin registrar' : presente ? 'Presente' : 'Ausente'}
                      </span>
                    )}
                  </li>
                );
              })}
          </ul>
        </fieldset>
      ))}
      {mensaje && <Aviso tono={mensaje.tono}>{mensaje.texto}</Aviso>}
      {marcable && (
        <div className="fila">
          <Boton
            cargando={guardando}
            textoCargando="Guardando…"
            disabled={Object.keys(cambios).length === 0}
            onClick={() => void guardar()}
          >
            Guardar asistencia
          </Boton>
        </div>
      )}
    </Tarjeta>
  );
}
