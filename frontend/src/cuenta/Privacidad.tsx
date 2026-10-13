import { Link } from 'react-router-dom';
import { DisposicionCuenta } from './DisposicionCuenta';

/** Correo público al que se piden consultas, correcciones y eliminación de datos. */
export const CORREO_PRIVACIDAD = 'luisrak25@gmail.com';

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
          cuenta estén activos. Cuando eliminas tu cuenta se borran de inmediato tu cuenta, tus jugadores,
          sus fichas, documentos, fotos, cartas y estadísticas. Se conservan los comunicados y resultados
          que publicaste, sin tu nombre; el marcador de los partidos, sin los jugadores; y los pagos
          registrados, con el nombre y el documento que tenían, porque el club los necesita para su
          contabilidad.
        </p>

        <h2>Tus derechos</h2>
        <p>
          Puedes consultar y corregir tus datos desde tu perfil y la ficha de tu jugador. Puedes eliminar
          tu cuenta tú mismo desde la aplicación, en «Mi perfil» → «Eliminar mi cuenta», con tu
          contraseña; eliminarla borra también a los jugadores de la cuenta, en todos los clubes. Si
          prefieres retirar a un jugador de un club sin eliminar la cuenta, hazlo desde su ficha. Si no
          puedes hacerlo desde la aplicación, <Link to="/eliminar-cuenta">te explicamos cómo pedirlo</Link>{' '}
          o escribe a <a href={`mailto:${CORREO_PRIVACIDAD}`}>{CORREO_PRIVACIDAD}</a> desde el correo de la
          cuenta y la atenderemos en un plazo de 15 días.
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
