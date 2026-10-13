// Tipos de los esquemas de specs/014-eliminar-cuenta/contracts/api.yaml, con los mismos nombres:
// lo que se eliminaría con la cuenta y los clubes que lo impiden.

/** Un jugador que se iría con la cuenta: solo nombre y club, nunca documento ni datos de contacto. */
export interface JugadorQueSeElimina {
  nombreCompleto: string;
  clubNombre: string;
  /** Verdadero si está retirado del club; se dice en texto. */
  retirado: boolean;
  /** Verdadero si su ingreso aún espera aprobación; se dice en texto. */
  enEspera: boolean;
}

/** Un club donde la persona es el único presidente y por eso no puede eliminar su cuenta. */
export interface ClubQueImpide {
  clubId: string;
  clubNombre: string;
}

/** Respuesta de `GET /api/cuenta/eliminacion`. */
export interface ResumenEliminacionCuenta {
  puedeEliminar: boolean;
  jugadores: JugadorQueSeElimina[];
  clubesQueLoImpiden: ClubQueImpide[];
}

/** Cuerpo de `POST /api/cuenta/eliminacion`. */
export interface EliminarCuentaDto {
  contrasena: string;
}
