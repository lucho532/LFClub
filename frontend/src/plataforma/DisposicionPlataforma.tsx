import type { CSSProperties } from 'react';
import { Outlet, useNavigate } from 'react-router-dom';
import { BarraSuperior } from '../compartido/componentes/BarraSuperior';
import { EnlaceMenu } from '../compartido/componentes/EnlaceMenu';
import { IconoCampana, IconoClubes, IconoPagos } from '../compartido/componentes/Iconos';
import { MenuPerfil } from '../compartido/componentes/MenuPerfil';
import { useSesion } from '../compartido/sesion/useSesion';

/**
 * Armazón del panel de administración de la plataforma: menú lateral, barra superior con el tema y
 * el perfil (desde donde se cierra sesión) y la marca de LFDevelopments, con su logo de marca de agua
 * (nunca la identidad de un club).
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
    <div
      className="disposicion marca-plataforma con-marca-agua"
      style={{ '--escudo-marca': 'url("/logo-lfdevelopments-fondo.png")' } as CSSProperties}
    >
      <aside className="lateral">
        <div className="lateral-marca">
          <img className="logo-plataforma" src="/logo-lfdevelopments.png" alt="" />
          <span>
            LFClub
            <br />
            <small>Administración</small>
          </span>
        </div>
        <nav className="lateral-menu" aria-label="Panel de administración">
          <EnlaceMenu a="/plataforma" icono={<IconoClubes />} exacto>
            Clubes
          </EnlaceMenu>
          <EnlaceMenu a="/plataforma/tarifas" icono={<IconoPagos />}>
            Tarifas
          </EnlaceMenu>
          <EnlaceMenu a="/plataforma/notificaciones" icono={<IconoCampana />}>
            Notificaciones
          </EnlaceMenu>
        </nav>
      </aside>
      <div className="contenido">
        <BarraSuperior
          marca={
            <>
              <img className="logo-plataforma" src="/logo-lfdevelopments.png" alt="" />
              <span>LFClub</span>
            </>
          }
        >
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
