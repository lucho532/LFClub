import { useState } from 'react';
import { Link, useParams, useSearchParams } from 'react-router-dom';
import { api } from '../../compartido/api/cliente';
import { ErrorApi } from '../../compartido/api/errores';
import type { PartidoDto } from '../../compartido/api/tiposActividades';
import { useCarga } from '../../compartido/api/useCarga';
import { Aviso } from '../../compartido/componentes/Aviso';
import { Tarjeta } from '../../compartido/componentes/Tarjeta';
import { diaDeLaSemana, horaCorta, yaEmpezo } from '../../compartido/fechas';
import { AccionesDeEstado } from '../actividades/AccionesDeEstado';
import { EtiquetaActividad } from '../actividades/EtiquetaActividad';
import { FormularioPartido } from '../actividades/FormularioPartido';
import { BotonesRespuesta } from '../calendario/BotonesRespuesta';
import { textoDeCategorias, textoDeCondicion } from '../calendario/TarjetaActividad';
import { useClub } from '../contextoClub';
import { SeccionConvocatoria } from './SeccionConvocatoria';
import { SeccionResultado } from './SeccionResultado';
import { nombreRival, textoDeTipo } from './textos';

/**
 * Detalle de un partido: rival, fecha, hora, lugar, condición, tipo y torneo, categorías y estado
 * escrito; si está jugado, el marcador y los goleadores. Quien gestiona un partido próximo lo
 * edita (con `?editar=1` el formulario ya viene abierto) o lo cancela, y uno cancelado que no ha empezado lo reactiva. Quien ve la planilla ve la
 * convocatoria. Si no existe o quien pregunta no lo ve, dice lo mismo que si no existiera (RF-011).
 */
export function DetallePartido() {
  const { club } = useClub();
  const { partidoId = '' } = useParams();
  const ruta = `/api/clubes/${club.clubId}/partidos/${partidoId}`;
  const carga = useCarga(ruta, () => api.get<PartidoDto>(ruta));
  const [parametros] = useSearchParams();
  const [cambiando, setCambiando] = useState(parametros.get('editar') === '1');
  const partido = carga.datos;
  const noExiste = carga.fallo instanceof ErrorApi && carga.fallo.status === 404;

  return (
    <>
      <Link to={`/club/${club.clubId}/partidos`}>← Partidos</Link>
      {noExiste ? (
        <Aviso tono="aviso">No encontramos este partido.</Aviso>
      ) : (
        carga.error && <Aviso tono="error">{carga.error}</Aviso>
      )}
      {!partido && carga.cargando && <p className="texto-suave">Cargando…</p>}
      {partido && (
        <>
          <div className="cabecera">
            <h1 className="dato-largo">Contra {nombreRival(partido)}</h1>
            <EtiquetaActividad tipo="PARTIDO" estado={partido.estado} faltaResultado={partido.faltaResultado} />
          </div>
          <SeccionResultado clubId={club.clubId} nombreClub={club.nombre} partido={partido} alCambiar={carga.fijar} />
          {cambiando && partido.permisos.gestiona && partido.estado === 'PROXIMO' ? (
            <Tarjeta titulo="Editar el partido">
              <FormularioPartido
                clubId={club.clubId}
                categorias={[]}
                partido={partido}
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
                  <dd>{diaDeLaSemana(partido.fecha)}</dd>
                </div>
                <div>
                  <dt>Hora</dt>
                  <dd>{horaCorta(partido.hora)}</dd>
                </div>
                <div>
                  <dt>Lugar</dt>
                  <dd className="dato-largo">{partido.lugar}</dd>
                </div>
                <div>
                  <dt>Condición</dt>
                  <dd>{textoDeCondicion(partido.condicion)}</dd>
                </div>
                <div>
                  <dt>Tipo</dt>
                  <dd className="dato-largo">{textoDeTipo(partido)}</dd>
                </div>
                <div>
                  <dt>Categorías</dt>
                  <dd>{textoDeCategorias(partido.categorias)}</dd>
                </div>
              </dl>
              {club.miRol === 'JUGADOR' && partido.estado !== 'JUGADO' && (
                <BotonesRespuesta
                  key={partido.estado}
                  ruta={`${ruta}/mi-respuesta`}
                  actual={partido.miRespuesta}
                  habilitado={partido.permisos.responde}
                />
              )}
              {partido.permisos.gestiona && partido.estado !== 'JUGADO' && (
                <AccionesDeEstado
                  nombre="partido"
                  cancelada={partido.estado === 'CANCELADO'}
                  empezo={yaEmpezo(partido.fecha, partido.hora)}
                  sePuedeCambiar={partido.estado === 'PROXIMO'}
                  alCambiar={() => setCambiando(true)}
                  cancelar={async () => carga.fijar(await api.post<PartidoDto>(`${ruta}/cancelacion`))}
                  reactivar={async () => carga.fijar(await api.delete<PartidoDto>(`${ruta}/cancelacion`))}
                />
              )}
            </Tarjeta>
          )}
          {partido.permisos.vePlanilla && <SeccionConvocatoria clubId={club.clubId} partido={partido} />}
        </>
      )}
    </>
  );
}
