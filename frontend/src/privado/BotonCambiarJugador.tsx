import { useNavigate } from 'react-router-dom';
import type { ClubDeSesionDto } from '../compartido/api/tiposSesion';
import { Boton } from '../compartido/componentes/Boton';
import { olvidarJugadorElegido } from '../compartido/sesion/jugadorElegido';

interface Props {
  club: ClubDeSesionDto;
  /** `menu` lo pinta como una opción del menú de perfil de la barra superior. */
  variante?: 'secundario' | 'menu';
}

/**
 * "Cambiar de jugador": olvida con cuál continúa la familia en ese club y la devuelve a la lista
 * de sus jugadores, sin cerrar sesión (RF-025). Solo se pinta cuando hay entre quién elegir: no
 * aparece con un solo jugador ni en una sesión iniciada con el documento de uno (RF-026).
 */
export function BotonCambiarJugador({ club, variante = 'secundario' }: Props) {
  const navegar = useNavigate();

  if (club.jugadores.length === 0) {
    return null;
  }

  function cambiar() {
    olvidarJugadorElegido(club.clubId);
    navegar(`/club/${club.clubId}`, { replace: true });
  }

  if (variante === 'menu') {
    return (
      <button type="button" role="menuitem" onClick={cambiar}>
        Cambiar de jugador
      </button>
    );
  }

  return (
    <Boton variante={variante} onClick={cambiar}>
      Cambiar de jugador
    </Boton>
  );
}
