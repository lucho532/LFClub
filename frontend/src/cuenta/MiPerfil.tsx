import { Link } from 'react-router-dom';
import { BotonRecargar } from '../compartido/componentes/BotonRecargar';
import { BotonTema } from '../compartido/componentes/BotonTema';
import { useSesion } from '../compartido/sesion/useSesion';
import { AccesosMiFicha } from './AccesosMiFicha';
import { SeccionDatosPropios } from './SeccionDatosPropios';
import { SeccionFotoPerfil } from './SeccionFotoPerfil';

/**
 * "Mi perfil": el correo de la cuenta arriba, de solo lectura (RF-018 de la 008), y la foto de
 * perfil. Si la cuenta es presidente, directivo o entrenador en algún club, también sus datos
 * personales, que cambia ella misma (spec 008, historia 4); si es la de un jugador, el acceso a
 * "Mi ficha", donde están los suyos. Con roles de los dos tipos se ven los dos bloques.
 */
export function MiPerfil() {
  const { sesion } = useSesion();

  if (!sesion) {
    return null;
  }

  const tieneDatosPropios = sesion.clubes.some((club) => club.rol !== 'JUGADOR');

  return (
    <div className="centrado">
      <header className="columna">
        <div className="cabecera">
          <Link to="/">← Volver</Link>
          <span className="barra-superior-acciones">
            <BotonRecargar />
            <BotonTema />
          </span>
        </div>
        <h1>Mi perfil</h1>
        <p className="texto-suave">{sesion.correo}</p>
      </header>

      <main className="columna">
        <SeccionFotoPerfil
          sesion={sesion}
          quienLaVe={
            tieneDatosPropios
              ? 'La ven los integrantes de tus clubes en tu ficha y junto a tu nombre.'
              : 'Solo la ves tú.'
          }
        />
        {tieneDatosPropios && <SeccionDatosPropios />}
        <AccesosMiFicha clubes={sesion.clubes} />
      </main>
    </div>
  );
}
