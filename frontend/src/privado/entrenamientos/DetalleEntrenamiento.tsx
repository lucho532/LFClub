import { useState } from 'react';
import { Link, useParams, useSearchParams } from 'react-router-dom';
import { api } from '../../compartido/api/cliente';
import { ErrorApi } from '../../compartido/api/errores';
import type { EntrenamientoDto, PlanillaEntrenamientoDto } from '../../compartido/api/tiposActividades';
import { useCarga } from '../../compartido/api/useCarga';
import { Aviso } from '../../compartido/componentes/Aviso';
import { Tarjeta } from '../../compartido/componentes/Tarjeta';
import { diaDeLaSemana, rangoDeHoras, yaEmpezo } from '../../compartido/fechas';
import { AccionesDeEstado } from '../actividades/AccionesDeEstado';
import { EtiquetaActividad } from '../actividades/EtiquetaActividad';
import { FormularioEntrenamiento } from '../actividades/FormularioEntrenamiento';
import { BotonesRespuesta } from '../calendario/BotonesRespuesta';
import { textoDeCategorias } from '../calendario/TarjetaActividad';
import { useClub } from '../contextoClub';
import { PlanillaAsistencia } from './PlanillaAsistencia';
import { ResumenRespuestas } from './ResumenRespuestas';

/**
 * Detalle de un entrenamiento: fecha, horas, lugar, nota, categorías y estado escrito. Quien lo
 * gestiona lo edita (con las categorías fijas; con `?editar=1` el formulario ya viene abierto), lo cancela o, si está cancelado y no ha empezado,
 * lo reactiva; los demás no ven esos botones. Si no existe o quien pregunta no lo ve, dice lo mismo
 * que si no existiera, sin más datos (RF-011).
 */
export function DetalleEntrenamiento() {
  const { club } = useClub();
  const { entrenamientoId = '' } = useParams();
  const ruta = `/api/clubes/${club.clubId}/entrenamientos/${entrenamientoId}`;
  const carga = useCarga(ruta, () => api.get<EntrenamientoDto>(ruta));
  const [parametros] = useSearchParams();
  const [cambiando, setCambiando] = useState(parametros.get('editar') === '1');
  const entrenamiento = carga.datos;
  const vePlanilla = entrenamiento?.permisos.vePlanilla ?? false;
  const planilla = useCarga<PlanillaEntrenamientoDto | null>(`${ruta}/jugadores#${vePlanilla}#${entrenamiento?.estado}`, () =>
    vePlanilla ? api.get<PlanillaEntrenamientoDto>(`${ruta}/jugadores`) : Promise.resolve(null),
  );
  const noExiste = carga.fallo instanceof ErrorApi && carga.fallo.status === 404;

  return (
    <>
      <Link to={`/club/${club.clubId}/calendario`}>← Calendario</Link>
      {noExiste ? (
        <Aviso tono="aviso">No encontramos este entrenamiento.</Aviso>
      ) : (
        carga.error && <Aviso tono="error">{carga.error}</Aviso>
      )}
      {!entrenamiento && carga.cargando && <p className="texto-suave">Cargando…</p>}
      {entrenamiento && (
        <>
          <div className="cabecera">
            <h1>Entrenamiento</h1>
            <EtiquetaActividad tipo="ENTRENAMIENTO" estado={entrenamiento.estado} />
          </div>
          {cambiando && entrenamiento.permisos.gestiona && entrenamiento.estado === 'PROGRAMADO' ? (
            <Tarjeta titulo="Editar el entrenamiento">
              <FormularioEntrenamiento
                clubId={club.clubId}
                categorias={[]}
                entrenamiento={entrenamiento}
                alGuardar={(cambiado) => {
                  carga.fijar(cambiado);
                  setCambiando(false);
                }}
                alCancelar={() => setCambiando(false)}
              />
            </Tarjeta>
          ) : (
            <Tarjeta>
              <dl className="ficha-datos">
                <div>
                  <dt>Fecha</dt>
                  <dd>{diaDeLaSemana(entrenamiento.fecha)}</dd>
                </div>
                <div>
                  <dt>Hora</dt>
                  <dd>{rangoDeHoras(entrenamiento.horaInicio, entrenamiento.horaFin)}</dd>
                </div>
                <div>
                  <dt>Lugar</dt>
                  <dd className="dato-largo">{entrenamiento.lugar}</dd>
                </div>
                <div>
                  <dt>Categorías</dt>
                  <dd>{textoDeCategorias(entrenamiento.categorias)}</dd>
                </div>
                {entrenamiento.nota && (
                  <div className="dato-ancho">
                    <dt>Nota</dt>
                    <dd className="dato-largo">{entrenamiento.nota}</dd>
                  </div>
                )}
              </dl>
              {club.miRol === 'JUGADOR' && (
                <BotonesRespuesta
                  key={`${entrenamiento.estado}`}
                  ruta={`${ruta}/mi-respuesta`}
                  actual={entrenamiento.miRespuesta}
                  habilitado={entrenamiento.permisos.responde}
                />
              )}
              {entrenamiento.miAsistencia !== undefined && (
                <p>
                  <strong>{entrenamiento.miAsistencia ? 'Asististe' : 'No asististe'}</strong>
                </p>
              )}
              {entrenamiento.permisos.gestiona && (
                <AccionesDeEstado
                  nombre="entrenamiento"
                  cancelada={entrenamiento.estado === 'CANCELADO'}
                  empezo={yaEmpezo(entrenamiento.fecha, entrenamiento.horaInicio)}
                  sePuedeCambiar
                  alCambiar={() => setCambiando(true)}
                  cancelar={async () => carga.fijar(await api.post<EntrenamientoDto>(`${ruta}/cancelacion`))}
                  reactivar={async () => carga.fijar(await api.delete<EntrenamientoDto>(`${ruta}/cancelacion`))}
                />
              )}
            </Tarjeta>
          )}
          {vePlanilla && planilla.datos && (
            <Tarjeta titulo="Respuestas de las familias">
              <ResumenRespuestas clubId={club.clubId} textoVolver="Entrenamiento" jugadores={planilla.datos.jugadores} />
            </Tarjeta>
          )}
          {vePlanilla && <PlanillaAsistencia clubId={club.clubId} entrenamiento={entrenamiento} planilla={planilla} />}
        </>
      )}
    </>
  );
}
