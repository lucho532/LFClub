import { useSyncExternalStore } from 'react';
import type { ClubDeSesionDto, JugadorDeSesionDto } from '../api/tiposSesion';

const PREFIJO = 'lapecosa.jugadorElegido.';

/** Cabecera con la que cada petición del club dice con cuál jugador continúa la familia. */
export const CABECERA_JUGADOR_ELEGIDO = 'X-Jugador-Elegido';

const oyentes = new Set<() => void>();

function avisar(): void {
  oyentes.forEach((oyente) => oyente());
}

/**
 * El jugador con el que la familia continúa en ese club, o `null` si no ha elegido. Vive en la
 * pestaña: sobrevive a recargar la página y se pierde al cerrarla (RF-027).
 */
export function leerJugadorElegido(clubId: string): string | null {
  try {
    return sessionStorage.getItem(PREFIJO + clubId);
  } catch {
    return null;
  }
}

/** Recuerda, solo en esta pestaña, con cuál jugador continúa la familia en ese club. */
export function guardarJugadorElegido(clubId: string, usuarioRolId: string): void {
  try {
    sessionStorage.setItem(PREFIJO + clubId, usuarioRolId);
  } catch {
    // Sin almacenamiento disponible la familia tendrá que elegir de nuevo.
  }

  avisar();
}

/** Olvida la elección de ese club: la familia vuelve a la lista de sus jugadores. */
export function olvidarJugadorElegido(clubId: string): void {
  try {
    sessionStorage.removeItem(PREFIJO + clubId);
  } catch {
    // Nada que olvidar.
  }

  avisar();
}

/** Olvida las elecciones de todos los clubes: al iniciar y al cerrar sesión. */
export function olvidarJugadoresElegidos(): void {
  try {
    const claves: string[] = [];
    for (let indice = 0; indice < sessionStorage.length; indice += 1) {
      const clave = sessionStorage.key(indice);
      if (clave?.startsWith(PREFIJO)) {
        claves.push(clave);
      }
    }

    claves.forEach((clave) => sessionStorage.removeItem(clave));
  } catch {
    // Nada que olvidar.
  }

  avisar();
}

/**
 * El jugador elegido en ese club, si sigue entre los de la cuenta. `null` si no se ha elegido, si
 * el elegido ya no está en la lista (lo rechazaron) o si no hay nada que elegir.
 */
export function jugadorDe(club: ClubDeSesionDto): JugadorDeSesionDto | null {
  const elegido = leerJugadorElegido(club.clubId);
  return club.jugadores.find((jugador) => jugador.usuarioRolId === elegido) ?? null;
}

function suscribir(oyente: () => void): () => void {
  oyentes.add(oyente);
  return () => oyentes.delete(oyente);
}

/** La elección de ese club, al día: quien la usa se vuelve a pintar cuando se elige o se olvida. */
export function useJugadorElegido(clubId: string): string | null {
  return useSyncExternalStore(suscribir, () => leerJugadorElegido(clubId));
}
