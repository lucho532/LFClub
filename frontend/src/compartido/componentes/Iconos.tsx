import type { ReactNode } from 'react';

/**
 * Iconos de línea de los menús, dibujados con el color del texto que los rodea. Son decorativos:
 * el texto del enlace es el que dice adónde lleva, así que se ocultan a los lectores de pantalla.
 */
function Icono({ children, tamano = 22 }: { children: ReactNode; tamano?: number }) {
  return (
    <svg
      viewBox="0 0 24 24"
      width={tamano}
      height={tamano}
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

export function IconoCalendario() {
  return (
    <Icono>
      <rect x="3" y="5" width="18" height="16" rx="2" />
      <path d="M3 10h18M8 3v4M16 3v4" />
      <path d="M8 14h2M14 14h2M8 17.5h2" />
    </Icono>
  );
}

export function IconoPartido() {
  return (
    <Icono>
      <circle cx="12" cy="12" r="9" />
      <path d="m12 7.5 4 2.9-1.5 4.7h-5L8 10.4z" />
      <path d="M12 3v4.5M16 10.4l4.5-1.4M14.5 15.1l2.8 3.8M9.5 15.1l-2.8 3.8M8 10.4 3.5 9" />
    </Icono>
  );
}

export function IconoGoleadores() {
  return (
    <Icono>
      <path d="M8 4h8v5a4 4 0 0 1-8 0z" />
      <path d="M8 6H5a3 3 0 0 0 3 4M16 6h3a3 3 0 0 1-3 4" />
      <path d="M12 13v4M8.5 21h7M10 17h4l.5 4h-5z" />
    </Icono>
  );
}

export function IconoOjo() {
  return (
    <Icono>
      <path d="M2 12s3.6-7 10-7 10 7 10 7-3.6 7-10 7S2 12 2 12z" />
      <circle cx="12" cy="12" r="3" />
    </Icono>
  );
}

export function IconoOjoTachado() {
  return (
    <Icono>
      <path d="M10.6 5.1A10.4 10.4 0 0 1 12 5c6.4 0 10 7 10 7a17.6 17.6 0 0 1-2.6 3.6M6.6 6.6C3.7 8.4 2 12 2 12s3.6 7 10 7a9.7 9.7 0 0 0 5.4-1.6" />
      <path d="M9.9 9.9a3 3 0 0 0 4.2 4.2M3 3l18 18" />
    </Icono>
  );
}

/** El lápiz de editar una foto o un escudo, pequeño para ir dentro del círculo. */
export function IconoLapiz() {
  return (
    <Icono tamano={14}>
      <path d="M4 20h4L19 9a2.8 2.8 0 0 0-4-4L4 16v4Z" />
      <path d="m13.5 6.5 4 4" />
    </Icono>
  );
}
