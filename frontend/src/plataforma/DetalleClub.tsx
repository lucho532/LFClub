import { Link, useParams } from 'react-router-dom';
import { api } from '../compartido/api/cliente';
import type { ClubDetalleDto } from '../compartido/api/tipos';
import { useCarga } from '../compartido/api/useCarga';
import { Aviso } from '../compartido/componentes/Aviso';
import { EtiquetaEstadoClub } from '../compartido/componentes/EtiquetaEstado';
import { SeccionDatos } from './SeccionDatos';
import { SeccionEstado } from './SeccionEstado';
import { SeccionInvitaciones } from './SeccionInvitaciones';
import { SeccionPresidentes } from './SeccionPresidentes';

/** Lo que recibe cada sección del detalle de un club. */
export interface PropsSeccion {
  club: ClubDetalleDto;
  /** La sección entrega el club tal como lo devolvió la API tras un cambio. */
  alActualizar: (club: ClubDetalleDto) => void;
  /** Vuelve a pedir el club a la API. */
  recargar: () => void;
}

/** Detalle de un club en el panel de administración. Solo compone sus secciones. */
export function DetalleClub() {
  const { clubId = '' } = useParams();
  const { datos: club, error, cargando, recargar, fijar } = useCarga(clubId, () =>
    api.get<ClubDetalleDto>(`/api/plataforma/clubes/${clubId}`),
  );

  if (error) {
    return (
      <>
        <Aviso tono="error">{error}</Aviso>
        <Link to="/plataforma">Volver a los clubes</Link>
      </>
    );
  }

  if (!club) {
    return <p className="texto-suave">{cargando ? 'Cargando…' : ''}</p>;
  }

  const seccion: PropsSeccion = { club, alActualizar: fijar, recargar };

  return (
    <>
      <Link to="/plataforma">← Clubes</Link>
      <div className="cabecera">
        <h1>{club.nombre}</h1>
        <EtiquetaEstadoClub estado={club.estado} />
      </div>
      {club.presidentes.length === 0 && (
        <Aviso tono="aviso">El presidente de este club todavía no se ha registrado.</Aviso>
      )}
      <SeccionDatos {...seccion} />
      <SeccionPresidentes {...seccion} />
      <SeccionInvitaciones {...seccion} />
      <SeccionEstado {...seccion} />
    </>
  );
}
