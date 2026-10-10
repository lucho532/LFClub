import type { ClubDto } from '../compartido/api/tipos';
import { useClub } from './contextoClub';
import { InicioEntrenador } from './inicio/InicioEntrenador';
import { ResumenDelClub } from './inicio/ResumenDelClub';
import { TarjetaMiJugador } from './inicio/TarjetaMiJugador';
import { TarjetaProximosEntrenamientos } from './inicio/TarjetaProximosEntrenamientos';
import { TarjetaTuClub } from './inicio/TarjetaTuClub';
import { TarjetaMiCategoria } from './TarjetaMiCategoria';

/** Lo propio del inicio de cada rol (spec 007, RF-011 a RF-013). */
function DelRol({ club }: { club: ClubDto }) {
  switch (club.miRol) {
    // El resumen se elige por el rol, no por las asignaciones: quien además entrena lo ve igual (RF-015).
    case 'PRESIDENTE':
    case 'DIRECTIVO':
      return <ResumenDelClub clubId={club.clubId} />;
    case 'ENTRENADOR':
      return <InicioEntrenador clubId={club.clubId} miUsuarioRolId={club.miUsuarioRolId} />;
    case 'JUGADOR':
      return (
        <>
          <TarjetaMiJugador clubId={club.clubId} miUsuarioRolId={club.miUsuarioRolId} />
          <TarjetaMiCategoria clubId={club.clubId} />
        </>
      );
  }
}

/**
 * Pantalla de inicio de la aplicación del club: su nombre, lo propio del rol de la persona en ese
 * club (el resumen, sus categorías o su jugador), el espacio reservado de los próximos
 * entrenamientos y la tarjeta del club. Cambia al cambiar de club, porque el rol es el del club
 * elegido.
 */
export function InicioClub() {
  const { club } = useClub();

  return (
    <>
      <div className="cabecera">
        <h1>{club.nombre}</h1>
      </div>
      <DelRol club={club} />
      <TarjetaProximosEntrenamientos />
      <TarjetaTuClub club={club} />
    </>
  );
}
