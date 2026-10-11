import { useState, type ReactNode } from 'react';
import { Link } from 'react-router-dom';
import { api } from '../../compartido/api/cliente';
import type { CalendarioDto } from '../../compartido/api/tiposActividades';
import { useCarga } from '../../compartido/api/useCarga';
import { Aviso } from '../../compartido/componentes/Aviso';
import { Boton } from '../../compartido/componentes/Boton';
import { diaDeLaSemana, diasDeLaSemana, hoyEnColombia, inicioDeSemana, sumarDias, yaEmpezo } from '../../compartido/fechas';
import { useCategoriasGestionables } from '../actividades/useCategoriasGestionables';
import { useClub } from '../contextoClub';
import { BotonesRespuesta } from './BotonesRespuesta';
import { TarjetaActividad } from './TarjetaActividad';
import { TiraDeSemana } from './TiraDeSemana';

/**
 * El calendario del club: una semana (la actual al entrar) con los entrenamientos y los partidos
 * que la persona puede ver, tal como los entrega la API, y botones para ir a la semana anterior o
 * a la siguiente. "Nuevo" solo para quien puede crear actividades. No decide qué se ve: un partido
 * al que la persona no está convocada no llega de la API.
 */
export function Calendario() {
  const { club } = useClub();
  const hoy = hoyEnColombia();
  const [lunes, setLunes] = useState(() => inicioDeSemana(hoy));
  const [elegido, setElegido] = useState(hoy);
  const dias = diasDeLaSemana(lunes);
  const ruta = `/api/clubes/${club.clubId}/calendario?desde=${dias[0]}&hasta=${dias[6]}`;
  const carga = useCarga(ruta, () => api.get<CalendarioDto>(ruta));
  const { categorias } = useCategoriasGestionables(club);
  const puedeCrear = club.miRol === 'PRESIDENTE' || categorias.length > 0;
  const esJugador = club.miRol === 'JUGADOR';
  const base = `/api/clubes/${club.clubId}`;

  function irA(semana: number) {
    const nuevoLunes = sumarDias(lunes, semana * 7);
    setLunes(nuevoLunes);
    setElegido(diasDeLaSemana(nuevoLunes).includes(hoy) ? hoy : nuevoLunes);
  }

  const actividades: Record<string, { orden: string; nodo: ReactNode }[]> = {};
  const agregar = (fecha: string, orden: string, nodo: ReactNode) => (actividades[fecha] ??= []).push({ orden, nodo });
  for (const entrenamiento of carga.datos?.entrenamientos ?? []) {
    agregar(
      entrenamiento.fecha,
      entrenamiento.horaInicio,
      <TarjetaActividad
        key={entrenamiento.entrenamientoId}
        clubId={club.clubId}
        tipo="ENTRENAMIENTO"
        entrenamiento={entrenamiento}
        pie={
          esJugador && (
            <BotonesRespuesta
              ruta={`${base}/entrenamientos/${entrenamiento.entrenamientoId}/mi-respuesta`}
              actual={entrenamiento.miRespuesta}
              habilitado={entrenamiento.estado === 'PROGRAMADO' && !yaEmpezo(entrenamiento.fecha, entrenamiento.horaInicio)}
            />
          )
        }
      />,
    );
  }
  for (const partido of carga.datos?.partidos ?? []) {
    agregar(
      partido.fecha,
      partido.hora,
      <TarjetaActividad
        key={partido.partidoId}
        clubId={club.clubId}
        tipo="PARTIDO"
        partido={partido}
        pie={
          esJugador &&
          partido.estado !== 'JUGADO' && (
            <BotonesRespuesta
              ruta={`${base}/partidos/${partido.partidoId}/mi-respuesta`}
              actual={partido.miRespuesta}
              habilitado={partido.estado === 'PROXIMO' && !yaEmpezo(partido.fecha, partido.hora)}
            />
          )
        }
      />,
    );
  }

  const porDia = Object.fromEntries(
    Object.entries(actividades).map(([fecha, lista]) => [
      fecha,
      lista.sort((a, b) => a.orden.localeCompare(b.orden)).map((actividad) => actividad.nodo),
    ]),
  );
  const vacia = carga.datos !== null && Object.keys(porDia).length === 0;

  return (
    <>
      <div className="cabecera">
        <h1>Calendario</h1>
        {puedeCrear && (
          <Link className="boton boton-principal" to={`/club/${club.clubId}/actividades/nueva`}>
            Nuevo
          </Link>
        )}
      </div>
      <div className="navegacion-semana">
        <strong>Semana del {diaDeLaSemana(dias[0])}</strong>
        <div className="navegacion-semana-botones">
          <Boton variante="secundario" aria-label="Semana anterior" onClick={() => irA(-1)}>
            ← Anterior
          </Boton>
          <Boton variante="secundario" aria-label="Semana siguiente" onClick={() => irA(1)}>
            Siguiente →
          </Boton>
        </div>
      </div>
      {carga.error && <Aviso tono="error">{carga.error}</Aviso>}
      {carga.cargando && !carga.datos && <p className="texto-suave">Cargando…</p>}
      {vacia && <p className="texto-suave">No hay actividades esta semana.</p>}
      <TiraDeSemana dias={dias} hoy={hoy} elegido={elegido} alElegir={setElegido} actividades={porDia} />
    </>
  );
}
