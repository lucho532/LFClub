import { useFotoPerfil } from '../sesion/useFotoPerfil';

interface Props {
  /** Lo que se muestra cuando la cuenta no tiene foto. */
  iniciales: string;
}

/** Iniciales de una persona a partir de sus nombres y apellidos. */
export function inicialesDe(nombres: string, apellidos: string): string {
  return `${nombres.trim().charAt(0)}${apellidos.trim().charAt(0)}`.toUpperCase();
}

/**
 * Foto de perfil de la cuenta con sesión o, si no tiene, sus iniciales. Solo dibuja: el enlace a
 * "Mi perfil", donde se carga, se cambia y se quita, está en el menú de perfil de la barra superior.
 */
export function Avatar({ iniciales }: Props) {
  const foto = useFotoPerfil();

  return foto ? (
    <img className="distintivo" src={foto} alt="" />
  ) : (
    <span className="distintivo" aria-hidden="true">
      {iniciales}
    </span>
  );
}
