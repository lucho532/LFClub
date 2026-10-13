import { Link } from 'react-router-dom';
import { DisposicionCuenta } from './DisposicionCuenta';

/** Correo público al que se piden consultas, correcciones y eliminación de datos. */
export const CORREO_PRIVACIDAD = 'privacidad@lfclub.app';

/** Fecha de la última revisión del texto; cámbiala cada vez que cambie la política. */
export const ACTUALIZADA_PRIVACIDAD = '13 de octubre de 2026';

/**
 * Política de privacidad pública (/privacidad). Es la dirección que Google Play pide en la ficha de
 * la app; no exige sesión.
 */
export function Privacidad() {
  return (
    <DisposicionCuenta titulo="Política de privacidad" subtitulo={`Última actualización: ${ACTUALIZADA_PRIVACIDAD}`}>
      <div className="columna">
        <p>
          LFClub es una plataforma para escuelas y clubes de fútbol. Cada club es responsable de los datos
          que registra en su espacio; LFClub los almacena y los mantiene aislados de los de los demás
          clubes.
        </p>

        <h2>Datos que tratamos</h2>
        <ul>
          <li>De las cuentas: nombre, correo electrónico, contraseña (guardada de forma cifrada) y foto de perfil.</li>
          <li>
            De los jugadores: nombre, fecha de nacimiento, documento de identidad, dirección, teléfono y
            correo de contacto, nombre del responsable, alergias indicadas en la ficha y fotos.
          </li>
          <li>
            De la actividad deportiva: equipos, convocatorias, asistencia, marcadores, goles y comunicados
            del club.
          </li>
        </ul>

        <h2>Para qué los usamos</h2>
        <p>
          Para gestionar el club: mostrar la agenda, las fichas y las cartas de los jugadores a quienes el
          club autorice, enviar avisos dentro de la aplicación y enviar correos de invitación y de
          recuperación de contraseña. No usamos los datos para publicidad ni para elaborar perfiles.
        </p>

        <h2>Con quién se comparten</h2>
        <p>
          No vendemos ni cedemos datos. Solo usamos un proveedor de envío de correo (Brevo) para entregar
          los mensajes de invitación y recuperación. Dentro de un club, cada persona ve únicamente lo que
          su rol permite.
        </p>

        <h2>Menores de edad</h2>
        <p>
          Los datos de los jugadores menores de edad los registran y mantienen sus responsables o el club.
          Los menores no crean cuenta propia en la aplicación.
        </p>

        <h2>Seguridad y conservación</h2>
        <p>
          Las comunicaciones con la aplicación usan HTTPS. Conservamos los datos mientras el club o la
          cuenta estén activos y los eliminamos cuando se pide su eliminación.
        </p>

        <h2>Tus derechos</h2>
        <p>
          Puedes consultar y corregir tus datos desde tu perfil y la ficha de tu jugador. Para pedir la
          eliminación de tu cuenta o de los datos de un jugador, escribe a{' '}
          <a href={`mailto:${CORREO_PRIVACIDAD}`}>{CORREO_PRIVACIDAD}</a>. Responderemos a esa solicitud y
          borraremos los datos que corresponda.
        </p>

        <h2>Cambios</h2>
        <p>
          Si esta política cambia, publicaremos aquí la versión nueva con su fecha de actualización.
        </p>

        <Link to="/entrar">Volver a iniciar sesión</Link>
      </div>
    </DisposicionCuenta>
  );
}
