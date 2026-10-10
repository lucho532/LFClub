import type { ReactNode } from 'react';

/**
 * Iconos de línea de los menús, dibujados con el color del texto que los rodea. Son decorativos:
 * el texto del enlace es el que dice adónde lleva, así que se ocultan a los lectores de pantalla.
 */
function Icono({ children }: { children: ReactNode }) {
  return (
    <svg
      viewBox="0 0 24 24"
      width="22"
      height="22"
      fill="none"
      stroke="currentColor"
      strokeWidth="2"
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden="true"
    >
      {children}
    </svg>
  );
}

export function IconoInicio() {
  return (
    <Icono>
      <path d="M3 10.5 12 3l9 7.5" />
      <path d="M5 9.5V21h14V9.5" />
      <path d="M9.5 21v-6h5v6" />
    </Icono>
  );
}

export function IconoFicha() {
  return (
    <Icono>
      <rect x="3" y="5" width="18" height="14" rx="2" />
      <circle cx="9" cy="11" r="2.5" />
      <path d="M5.5 16.5c.7-1.6 2-2.5 3.5-2.5s2.8.9 3.5 2.5M15 10h3M15 14h3" />
    </Icono>
  );
}

export function IconoIngresos() {
  return (
    <Icono>
      <circle cx="10" cy="8" r="4" />
      <path d="M3 21c.9-3.5 3.8-6 7-6 1.4 0 2.7.4 3.8 1.2M18 14v7M14.5 17.5h7" />
    </Icono>
  );
}

export function IconoCategorias() {
  return (
    <Icono>
      <circle cx="9" cy="8" r="3.5" />
      <path d="M2.5 20c.8-3.3 3.4-5.5 6.5-5.5s5.7 2.2 6.5 5.5" />
      <path d="M16 4.6a3.5 3.5 0 0 1 0 6.8M18 14.8c1.9.8 3.1 2.6 3.5 5.2" />
    </Icono>
  );
}

export function IconoJugadores() {
  return (
    <Icono>
      <path d="M8 3 3.5 5.5 5 10l2-.8V21h10V9.2l2 .8 1.5-4.5L16 3" />
      <path d="M8 3c.6 1.7 2.2 2.8 4 2.8S15.4 4.7 16 3" />
    </Icono>
  );
}

export function IconoDatosClub() {
  return (
    <Icono>
      <path d="M12 3 4 6v5.5c0 4.7 3.3 8.3 8 9.5 4.7-1.2 8-4.8 8-9.5V6z" />
      <path d="m9 12 2 2 4-4" />
    </Icono>
  );
}

export function IconoClubes() {
  return (
    <Icono>
      <rect x="3" y="4" width="18" height="16" rx="2" />
      <path d="M12 4v16M3 12h18" />
      <circle cx="12" cy="12" r="3" />
    </Icono>
  );
}
