import { NavLink, Navigate, Outlet } from 'react-router-dom';
import { useClub } from '../contextoClub';

/**
 * Sección «Suscripción» del club (spec 017), solo para el PRESIDENTE: lo que el club paga a la plataforma, con
 * pestañas como las de Mensualidades: el pago (cobros pendientes, último pago y próximo cobro), cómo se cobra
 * (la tabla de la tarifa) y los pagos realizados (el historial). Cualquier otro rol vuelve al inicio sin pedir
 * nada: la API vuelve a comprobarlo. La dirección de retorno de la pasarela queda fuera de las pestañas.
 */
export function DisposicionSuscripcion() {
  const contexto = useClub();
  const { club } = contexto;

  if (club.miRol !== 'PRESIDENTE') {
    return <Navigate to={`/club/${club.clubId}`} replace />;
  }

  return (
    <>
      <div className="cabecera">
        <h1>Suscripción</h1>
      </div>
      <nav className="pestanas-pagos" aria-label="Suscripción">
        <NavLink to="." end>
          Pago
        </NavLink>
        <NavLink to="como-se-cobra">Cómo se cobra</NavLink>
        <NavLink to="pagos-realizados">Pagos realizados</NavLink>
      </nav>
      <Outlet context={contexto} />
    </>
  );
}
