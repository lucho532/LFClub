import type { ClubDto } from '../compartido/api/tipos';
import { useClub } from './contextoClub';
import { AvisoDeMora } from './inicio/AvisoDeMora';
import { AvisoValorMensualidad } from './inicio/AvisoValorMensualidad';
import { InicioEntrenador } from './inicio/InicioEntrenador';
import { ResumenDelClub } from './inicio/ResumenDelClub';
import { TarjetaMiJugador } from './inicio/TarjetaMiJugador';
import { TarjetaPagosEnRevision } from './inicio/TarjetaPagosEnRevision';
import { TarjetaPartidos } from './inicio/TarjetaPartidos';
import { TarjetaProximosEntrenamientos } from './inicio/TarjetaProximosEntrenamientos';
import { TarjetaTuClub } from './inicio/TarjetaTuClub';
import { AvisoPagoPlataforma } from './pagoPlataforma/AvisoPagoPlataforma';
import { TarjetaMiCategoria } from './TarjetaMiCategoria';

/** Lo propio del inicio de cada rol (spec 007, RF-011 a RF-013). La familia ve arriba el aviso de mora (spec 016). */
function DelRol({ club }: { club: ClubDto }) {
  switch (club.miRol) {
    // El resumen se elige por el rol, no por las asignaciones: quien además entrena lo ve igual (RF-015).
    case 'PRESIDENTE':
      return (
        <>
          <AvisoPagoPlataforma clubId={club.clubId} />
          <AvisoValorMensualidad clubId={club.clubId} />
          <TarjetaPagosEnRevision clubId={club.clubId} />
          <ResumenDelClub clubId={club.clubId} />
        </>
      );
    case 'DIRECTIVO':
      return <ResumenDelClub clubId={club.clubId} />;
    case 'ENTRENADOR':
      return <InicioEntrenador clubId={club.clubId} miUsuarioRolId={club.miUsuarioRolId} />;
    case 'JUGADOR':
      return (
        <>
          <AvisoDeMora clubId={club.clubId} miUsuarioRolId={club.miUsuarioRolId} />
          <TarjetaMiJugador clubId={club.clubId} miUsuarioRolId={club.miUsuarioRolId} />
          <TarjetaMiCategoria clubId={club.clubId} />
        </>
      );
  }
}

/**
 * Pantalla de inicio de la aplicación del club: su nombre, lo propio del rol de la persona en ese
 * club (el resumen, sus categorías o su jugador), los próximos entrenamientos y los partidos
 * (próximos y últimos resultados) que puede ver (spec 009) y la tarjeta del club. Cambia al cambiar de club, porque el rol es el del club
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
      <TarjetaProximosEntrenamientos clubId={club.clubId} />
      <TarjetaPartidos clubId={club.clubId} nombreClub={club.nombre} />
      <TarjetaTuClub club={club} />
    </>
  );
}
