import { Link, Navigate } from 'react-router-dom';
import { api } from '../../compartido/api/cliente';
import type { MiembroCuerpoTecnicoDto } from '../../compartido/api/tiposCuerpoTecnico';
import { useCarga } from '../../compartido/api/useCarga';
import { Aviso } from '../../compartido/componentes/Aviso';
import { Tarjeta } from '../../compartido/componentes/Tarjeta';
import { estadoVolver } from '../../compartido/navegacion/useVolver';
import { useClub } from '../contextoClub';
import { FotoPersona } from '../personas/FotoPersona';
import { NOMBRE_DEL_ROL, textoDeCategorias } from './textosCuerpoTecnico';

/**
 * «Cuerpo técnico» (spec 018): las personas del club que no son jugadores, con su nombre completo, su rol y
 * las categorías que entrenan. Cada una abre su ficha con todos sus datos. Es exclusivo del PRESIDENTE: la API
 * rechaza a cualquier otro rol, y aquí se le devuelve al inicio sin pedir ninguna lista.
 */
export function CuerpoTecnico() {
  const { club } = useClub();

  if (club.miRol !== 'PRESIDENTE') {
    return <Navigate to={`/club/${club.clubId}`} replace />;
  }

  return <ListaDelCuerpoTecnico clubId={club.clubId} />;
}

function ListaDelCuerpoTecnico({ clubId }: { clubId: string }) {
  const carga = useCarga(`${clubId}/cuerpo-tecnico`, () =>
    api.get<MiembroCuerpoTecnicoDto[]>(`/api/clubes/${clubId}/cuerpo-tecnico`),
  );
  const miembros = carga.datos;

  return (
    <>
      <div className="cabecera">
        <h1>Cuerpo técnico</h1>
      </div>
      {carga.error && <Aviso tono="error">{carga.error}</Aviso>}
      {!miembros && carga.cargando && <p className="texto-suave">Cargando…</p>}
      {miembros?.length === 0 && <p className="texto-suave">Todavía no hay personal en el club.</p>}
      {miembros && miembros.length > 0 && (
        <Tarjeta>
          <ul className="lista-cuerpo-tecnico">
            {miembros.map((miembro) => (
              <li key={miembro.usuarioRolId}>
                <Link
                  className="miembro-cuerpo-tecnico"
                  to={`/club/${clubId}/cuerpo-tecnico/${miembro.usuarioRolId}`}
                  state={estadoVolver(`/club/${clubId}/cuerpo-tecnico`, 'Cuerpo técnico')}
                >
                  <FotoPersona clubId={clubId} persona={miembro} tamano="miniatura" />
                  <span className="columna">
                    <strong>
                      {miembro.nombres} {miembro.apellidos}
                    </strong>
                    <span className="texto-suave">
                      {NOMBRE_DEL_ROL[miembro.rol]} · {textoDeCategorias(miembro)}
                    </span>
                  </span>
                </Link>
              </li>
            ))}
          </ul>
        </Tarjeta>
      )}
    </>
  );
}
