import { Link } from 'react-router-dom';
import { DisposicionCuenta } from './DisposicionCuenta';
import { CORREO_PRIVACIDAD } from './Privacidad';
import { QueSeBorraYQueSeConserva } from './QueSeBorraYQueSeConserva';

/** Días en los que se atiende una solicitud de eliminación enviada por correo. */
export const PLAZO_ELIMINACION_DIAS = 15;

/**
 * Página pública (/eliminar-cuenta, spec 014) que explica cómo pedir la eliminación de la cuenta sin
 * tener la aplicación a mano: es la dirección que Google Play pide para ello. No exige sesión ni
 * elimina nada por sí misma.
 */
export function EliminarCuentaPublica() {
  return (
    <DisposicionCuenta titulo="Eliminar mi cuenta" subtitulo="Cómo pedir que se elimine tu cuenta de LFClub">
      <div className="columna">
        <h2>Desde la aplicación</h2>
        <ol>
          <li>Inicia sesión en LFClub.</li>
          <li>Abre «Mi perfil» y entra en «Eliminar mi cuenta».</li>
          <li>Revisa qué se borra, escribe tu contraseña y confirma.</li>
        </ol>
        <p>
          Es inmediato y no se puede deshacer. Si eres el único presidente de un club, antes debes
          nombrar a otro.
        </p>

        <h2>Si no recuerdas tu contraseña</h2>
        <p>
          Usa <Link to="/recuperar">«Olvidé mi contraseña»</Link>: te llega un enlace al correo de la
          cuenta. Con la contraseña nueva, sigue los pasos de arriba.
        </p>

        <h2>Si no puedes hacerlo desde la aplicación</h2>
        <p>
          Escribe a <a href={`mailto:${CORREO_PRIVACIDAD}`}>{CORREO_PRIVACIDAD}</a> desde el correo de la
          cuenta. Atendemos la solicitud en un plazo de {PLAZO_ELIMINACION_DIAS} días.
        </p>

        <QueSeBorraYQueSeConserva />

        <Link to="/privacidad">Política de privacidad</Link>
        <Link to="/entrar">Volver a iniciar sesión</Link>
      </div>
    </DisposicionCuenta>
  );
}
