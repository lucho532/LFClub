import { Link } from 'react-router-dom';
import type { ClubDto } from '../../compartido/api/tipos';
import { Tarjeta } from '../../compartido/componentes/Tarjeta';
import { nombreDeRol } from '../../compartido/formato';

/**
 * Tarjeta "Tu club" del inicio, igual para todos los roles: el rol de la persona y lo que el club
 * ha publicado de sí (sede, dirección y contacto). El presidente tiene además el enlace para
 * editar esos datos.
 */
export function TarjetaTuClub({ club }: { club: ClubDto }) {
  const contacto = [club.correoContacto, club.telefonoContacto].filter(Boolean).join(' · ');

  return (
    <Tarjeta titulo="Tu club">
      <p>
        Tu rol en este club: <strong>{nombreDeRol(club.miRol)}</strong>
      </p>
      {club.sede && <p className="texto-suave">Sede: {club.sede}</p>}
      {club.direccion && <p className="texto-suave">Dirección: {club.direccion}</p>}
      {contacto && <p className="texto-suave">Contacto: {contacto}</p>}
      {club.miRol === 'PRESIDENTE' && <Link to={`/club/${club.clubId}/configuracion`}>Editar los datos del club</Link>}
    </Tarjeta>
  );
}
