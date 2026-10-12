import type { ClubDeSesionDto } from '../api/tipos';

const CLAVE = 'lfclub.ultimoClub';

/** Recuerda en el dispositivo el último club elegido. */
export function guardarUltimoClub(clubId: string): void {
  try {
    localStorage.setItem(CLAVE, clubId);
  } catch {
    // Sin almacenamiento disponible simplemente no se recuerda.
  }
}

function leerUltimoClub(): string | null {
  try {
    return localStorage.getItem(CLAVE);
  } catch {
    return null;
  }
}

/**
 * Club al que entra una persona: el último que eligió, si sigue siendo suyo, aunque en él esté en
 * espera o retirada. Si no, el primero de los suyos en el que su ingreso está aprobado y no está
 * retirada; después, el primero en el que no está retirada; y, solo si no hay ninguno, el primero.
 * Devuelve `null` si no pertenece a ninguno. Nunca devuelve un club ajeno.
 */
export function clubDeEntrada(clubes: ClubDeSesionDto[]): string | null {
  if (clubes.length === 0) {
    return null;
  }

  const ultimo = leerUltimoClub();
  if (clubes.some((club) => club.clubId === ultimo)) {
    return ultimo;
  }

  const dondeEntra = clubes.find((club) => club.estadoIngreso === 'APROBADO' && !club.retirado);
  const dondeNoEstaRetirada = clubes.find((club) => !club.retirado);
  return (dondeEntra ?? dondeNoEstaRetirada ?? clubes[0]).clubId;
}
