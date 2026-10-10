import { Outlet, useNavigate } from 'react-router-dom';
import { BarraSuperior } from '../compartido/componentes/BarraSuperior';
import { EnlaceMenu } from '../compartido/componentes/EnlaceMenu';
import { IconoClubes } from '../compartido/componentes/Iconos';
import { MenuPerfil } from '../compartido/componentes/MenuPerfil';
import { useSesion } from '../compartido/sesion/useSesion';

/**
 * Armazón del panel de administración de la plataforma: menú lateral, barra superior con el tema y
 * el perfil (desde donde se cierra sesión) e identidad neutra de la plataforma (nunca la de un club).
 */
export function DisposicionPlataforma() {
  const { sesion, cerrar } = useSesion();
  const navegar = useNavigate();
  const correo = sesion?.correo ?? '';

  function cerrarSesion() {
    cerrar();
    navegar('/entrar', { replace: true });
  }

  return (
    <div className="disposicion">
      <aside className="lateral">
        <div className="lateral-marca">
          <span>
            La Pecosa
            <br />
            <small>Administración</small>
          </span>
        </div>
        <nav className="lateral-menu" aria-label="Panel de administración">
          <EnlaceMenu a="/plataforma" icono={<IconoClubes />} exacto>
            Clubes
          </EnlaceMenu>
        </nav>
      </aside>
      <div className="contenido">
        <BarraSuperior marca={<span>La Pecosa</span>}>
          <MenuPerfil
            nombre={correo}
            detalle="Desarrollador"
            iniciales={correo.charAt(0).toUpperCase()}
            alCerrarSesion={cerrarSesion}
          />
        </BarraSuperior>
        <main className="principal">
          <Outlet />
        </main>
      </div>
    </div>
  );
}
