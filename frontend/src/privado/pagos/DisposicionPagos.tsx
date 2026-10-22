import { NavLink, Navigate, Outlet } from 'react-router-dom';
import { useClub } from '../contextoClub';
import { rutaDeLaCuenta, veFinanzasDelClub } from './textosPagos';

/**
 * Sección "Pagos" del club para el PRESIDENTE y el DIRECTIVO: una fila de pestañas (mensualidad,
 * medios de pago, pagos en revisión y finanzas, según lo que ya existe) y, debajo, la pantalla
 * elegida. La cuenta de un jugador no pasa por aquí: su entrada del menú lleva directo a su estado
 * de cuenta. El ENTRENADOR no ve esta sección (constitución §16.9).
 */
export function DisposicionPagos() {
  const contexto = useClub();
  const { club } = contexto;

  if (club.miRol === 'JUGADOR') {
    return <Navigate to={rutaDeLaCuenta(club.clubId, club.miUsuarioRolId)} replace />;
  }

  if (!veFinanzasDelClub(club.miRol)) {
    return <Navigate to={`/club/${club.clubId}`} replace />;
  }

  return (
    <>
      <nav className="pestanas-pagos" aria-label="Pagos">
        <NavLink to="finanzas">Finanzas</NavLink>
        <NavLink to="en-revision">En revisión</NavLink>
        <NavLink to="mensualidad">Mensualidad</NavLink>
        <NavLink to="medios-de-pago">Medios de pago</NavLink>
      </nav>
      <Outlet context={contexto} />
    </>
  );
}

/** Pantalla con la que abre la sección "Pagos" del PRESIDENTE y del DIRECTIVO. */
export function InicioDePagos() {
  return <Navigate to="finanzas" replace />;
}
