import type { ReactNode } from 'react';
import { NavLink } from 'react-router-dom';

interface Props {
  /** Ruta a la que lleva. */
  a: string;
  /** Icono decorativo (de `Iconos.tsx`); el texto es el que dice adónde lleva. */
  icono: ReactNode;
  /** Solo se marca activo en esa ruta exacta, no en las que cuelgan de ella. */
  exacto?: boolean;
  children: ReactNode;
}

/**
 * Enlace del menú: en escritorio, una fila del menú lateral; en el teléfono, una pestaña de la
 * barra inferior con el icono sobre el texto. No decide quién lo ve: eso lo hace el menú que lo usa.
 */
export function EnlaceMenu({ a, icono, exacto = false, children }: Props) {
  return (
    <NavLink to={a} end={exacto}>
      <span className="lateral-icono">{icono}</span>
      <span className="lateral-texto">{children}</span>
    </NavLink>
  );
}
