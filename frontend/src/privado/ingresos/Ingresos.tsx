import { Navigate } from 'react-router-dom';
import { api } from '../../compartido/api/cliente';
import type { IngresoAprobadoDto, IngresoEnEsperaDto, InvitacionClubDto } from '../../compartido/api/tipos';
import { useCarga } from '../../compartido/api/useCarga';
import { useClub } from '../contextoClub';
import { SeccionIngresosAprobados } from './SeccionIngresosAprobados';
import { SeccionInvitacionesClub } from './SeccionInvitacionesClub';
import { SeccionSalaDeEspera } from './SeccionSalaDeEspera';

/**
 * Apartado "Ingresos" del club, exclusivo de su presidente: las invitaciones enviadas, la sala de
 * espera y los ingresos aprobados, en ese orden. La API rechaza a cualquier otro rol, también al
 * directivo; aquí se le devuelve al inicio del club sin pedir ninguna lista.
 */
export function Ingresos() {
  const { club } = useClub();

  if (club.miRol !== 'PRESIDENTE') {
    return <Navigate to={`/club/${club.clubId}`} replace />;
  }

  return <SeccionesDeIngresos clubId={club.clubId} />;
}

/** Carga los datos del apartado y compone sus secciones; cada una recarga las que le afectan. */
function SeccionesDeIngresos({ clubId }: { clubId: string }) {
  const base = `/api/clubes/${clubId}`;
  const enEspera = useCarga(`${clubId}/en-espera`, () =>
    api.get<IngresoEnEsperaDto[]>(`${base}/ingresos/en-espera`),
  );
  const invitaciones = useCarga(`${clubId}/invitaciones`, () =>
    api.get<InvitacionClubDto[]>(`${base}/invitaciones`),
  );
  const aprobados = useCarga(`${clubId}/aprobados`, () =>
    api.get<IngresoAprobadoDto[]>(`${base}/ingresos/aprobados`),
  );

  return (
    <>
      <div className="cabecera">
        <h1>Ingresos</h1>
      </div>
      <SeccionInvitacionesClub clubId={clubId} invitaciones={invitaciones} />
      <SeccionSalaDeEspera
        clubId={clubId}
        enEspera={enEspera}
        alCambiar={() => {
          aprobados.recargar();
          invitaciones.recargar();
        }}
      />
      <SeccionIngresosAprobados aprobados={aprobados} />
    </>
  );
}
