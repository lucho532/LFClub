import type { Rol, TipoDocumento } from './api/tipos';

const FECHA = new Intl.DateTimeFormat('es-CO', { day: 'numeric', month: 'long', year: 'numeric' });
const FECHA_Y_HORA = new Intl.DateTimeFormat('es-CO', {
  day: 'numeric',
  month: 'long',
  year: 'numeric',
  hour: 'numeric',
  minute: '2-digit',
});

/** Fecha en español a partir de la fecha en formato ISO que entrega la API. */
export function fecha(iso: string): string {
  return FECHA.format(new Date(iso));
}

const DIA = new Intl.DateTimeFormat('es-CO', { day: 'numeric', month: 'long', year: 'numeric', timeZone: 'UTC' });

/**
 * Día en español a partir de una fecha sin hora (`AAAA-MM-DD`), como la de nacimiento. No depende
 * de la zona horaria del dispositivo: el día que se muestra es el que se escribió.
 */
export function dia(fechaSinHora: string): string {
  return DIA.format(new Date(`${fechaSinHora}T00:00:00Z`));
}

/** Fecha y hora en español a partir de la fecha en formato ISO que entrega la API. */
export function fechaYHora(iso: string): string {
  return FECHA_Y_HORA.format(new Date(iso));
}

const ROLES: Record<Rol, string> = {
  DESARROLLADOR: 'Desarrollador',
  PRESIDENTE: 'Presidente',
  DIRECTIVO: 'Directivo',
  ENTRENADOR: 'Entrenador',
  JUGADOR: 'Jugador',
};

/** Nombre de un rol para mostrar. */
export function nombreDeRol(rol: Rol): string {
  return ROLES[rol];
}

const TIPOS_DOCUMENTO: Record<TipoDocumento, string> = {
  REGISTRO_CIVIL: 'Registro civil',
  TARJETA_IDENTIDAD: 'Tarjeta de identidad',
  CEDULA_CIUDADANIA: 'Cédula de ciudadanía',
  CEDULA_EXTRANJERIA: 'Cédula de extranjería',
};

/** Nombre de un tipo de documento para mostrar. */
export function nombreDeTipoDocumento(tipo: TipoDocumento): string {
  return TIPOS_DOCUMENTO[tipo];
}

/** Un rango de jugadores para mostrar: «1 a 20 jugadores» o «81 jugadores o más». */
export function textoDeRango(desde: number, hasta: number | null): string {
  return hasta === null ? `${desde} jugadores o más` : `${desde} a ${hasta} jugadores`;
}
