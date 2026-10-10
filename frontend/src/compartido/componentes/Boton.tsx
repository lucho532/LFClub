import type { ButtonHTMLAttributes } from 'react';

type Variante = 'principal' | 'secundario' | 'peligro' | 'texto';

interface Props extends ButtonHTMLAttributes<HTMLButtonElement> {
  variante?: Variante;
  /** Mientras es verdadero el botón queda deshabilitado y muestra `textoCargando`. */
  cargando?: boolean;
  textoCargando?: string;
}

/** Botón de la aplicación. El principal usa el color del club, o el neutro de la plataforma. */
export function Boton({
  variante = 'principal',
  cargando = false,
  textoCargando = 'Un momento…',
  type = 'button',
  disabled,
  children,
  className,
  ...resto
}: Props) {
  return (
    <button
      type={type}
      className={`boton boton-${variante} ${className ?? ''}`.trim()}
      disabled={disabled || cargando}
      {...resto}
    >
      {cargando ? textoCargando : children}
    </button>
  );
}
