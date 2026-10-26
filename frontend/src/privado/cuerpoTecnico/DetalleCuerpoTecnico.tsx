import { Link, Navigate, useParams } from 'react-router-dom';
import { api } from '../../compartido/api/cliente';
import { ErrorApi } from '../../compartido/api/errores';
import type { MiembroCuerpoTecnicoDetalleDto } from '../../compartido/api/tiposCuerpoTecnico';
import { useCarga } from '../../compartido/api/useCarga';
import { Aviso } from '../../compartido/componentes/Aviso';
import { Tarjeta } from '../../compartido/componentes/Tarjeta';
import { nombreDeTipoDocumento } from '../../compartido/formato';
import { useVolver } from '../../compartido/navegacion/useVolver';
import { EtiquetaActividad } from '../actividades/EtiquetaActividad';
import { diaDeLaSemana, rangoDeHoras } from '../../compartido/fechas';
import { useClub } from '../contextoClub';
import { FotoPersona } from '../personas/FotoPersona';
import { NOMBRE_DEL_ROL, textoDeCategoria } from './textosCuerpoTecnico';

/** Lo que dice la sección de entrenamientos de un entrenador que no tiene ninguno próximo. */
export const SIN_ENTRENAMIENTOS_DEL_ENTRENADOR = 'No tiene entrenamientos próximos.';

/**
 * La ficha completa de una persona del cuerpo técnico (spec 018), solo para el PRESIDENTE: su foto, su nombre
 * completo, su rol, su documento, su correo, su celular, las categorías y equipos asignados y, si es
 * entrenador, una tarjeta por cada uno de sus próximos entrenamientos. Son datos personales: la API los
 * entrega solo al presidente del club y esta pantalla devuelve al inicio a cualquier otro rol sin pedirlos.
 */
export function DetalleCuerpoTecnico() {
  const { club } = useClub();

  if (club.miRol !== 'PRESIDENTE') {
    return <Navigate to={`/club/${club.clubId}`} replace />;
  }

  return <FichaDelMiembro clubId={club.clubId} />;
}

function FichaDelMiembro({ clubId }: { clubId: string }) {
  const { usuarioRolId = '' } = useParams();
  const carga = useCarga(`${clubId}/cuerpo-tecnico/${usuarioRolId}`, () =>
    api.get<MiembroCuerpoTecnicoDetalleDto>(`/api/clubes/${clubId}/cuerpo-tecnico/${usuarioRolId}`),
  );
  const volver = useVolver({ ruta: `/club/${clubId}/cuerpo-tecnico`, texto: 'Cuerpo técnico' });
  const ficha = carga.datos;
  const noExiste = carga.fallo instanceof ErrorApi && carga.fallo.status === 404;

  return (
    <>
      <Link to={volver.ruta}>← {volver.texto}</Link>
      {noExiste ? (
        <Aviso tono="aviso">No encontramos a esta persona.</Aviso>
      ) : (
        carga.error && <Aviso tono="error">{carga.error}</Aviso>
      )}
      {!ficha && carga.cargando && <p className="texto-suave">Cargando…</p>}
      {ficha && (
        <>
          <div className="foto-de-ficha">
            <FotoPersona clubId={clubId} persona={ficha} tamano="grande" />
            <div className="columna ficha-basica-nombre">
              <h1 className="nombre-persona">
                {ficha.nombres} {ficha.apellidos}
              </h1>
              <p className="texto-suave">{NOMBRE_DEL_ROL[ficha.rol]}</p>
            </div>
          </div>

          <Tarjeta titulo="Datos personales">
            <dl className="ficha-datos">
              <div>
                <dt>Nombre completo</dt>
                <dd>
                  {ficha.nombres} {ficha.apellidos}
                </dd>
              </div>
              <div>
                <dt>{nombreDeTipoDocumento(ficha.tipoDocumento)}</dt>
                <dd>{ficha.numeroDocumento}</dd>
              </div>
              <div>
                <dt>Correo electrónico</dt>
                <dd>
                  <a href={`mailto:${ficha.correo}`}>{ficha.correo}</a>
                </dd>
              </div>
              <div>
                <dt>Teléfono</dt>
                <dd className={ficha.celular ? undefined : 'texto-suave'}>
                  {ficha.celular ? <a href={`tel:${ficha.celular}`}>{ficha.celular}</a> : 'Sin teléfono'}
                </dd>
              </div>
            </dl>
          </Tarjeta>

          <Tarjeta titulo="Categorías y equipos asignados">
            {ficha.categorias.length > 0 ? (
              <ul className="lista-de-inicio">
                {ficha.categorias.map((categoria) => (
                  <li key={categoria.anio}>Categoría {textoDeCategoria(categoria)}</li>
                ))}
              </ul>
            ) : (
              <p className="texto-suave">
                {ficha.rol === 'ENTRENADOR' ? 'Todavía no tiene categorías asignadas.' : 'No entrena ninguna categoría.'}
              </p>
            )}
          </Tarjeta>

          {ficha.rol === 'ENTRENADOR' && (
            <section className="columna" aria-labelledby="entrenamientos-del-entrenador">
              <h2 id="entrenamientos-del-entrenador">Próximos entrenamientos</h2>
              {ficha.proximosEntrenamientos.length === 0 && (
                <p className="texto-suave">{SIN_ENTRENAMIENTOS_DEL_ENTRENADOR}</p>
              )}
              {ficha.proximosEntrenamientos.map((entrenamiento) => (
                <Tarjeta
                  key={entrenamiento.entrenamientoId}
                  titulo={diaDeLaSemana(entrenamiento.fecha)}
                  acciones={<EtiquetaActividad tipo="ENTRENAMIENTO" estado={entrenamiento.estado} />}
                >
                  <p>
                    {rangoDeHoras(entrenamiento.horaInicio, entrenamiento.horaFin)} · {entrenamiento.lugar}
                  </p>
                  <p className="texto-suave">
                    {entrenamiento.categorias.map((categoria) => `Categoría ${categoria.anio}`).join(', ')}
                  </p>
                  <Link to={`/club/${clubId}/entrenamientos/${entrenamiento.entrenamientoId}`}>Ver entrenamiento</Link>
                </Tarjeta>
              ))}
            </section>
          )}
        </>
      )}
    </>
  );
}
