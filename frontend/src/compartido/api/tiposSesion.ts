// Tipos de los esquemas de specs/006-agregar-hermano/contracts/api.yaml, con los mismos nombres:
// la sesión con los jugadores de la cuenta en cada club.

import type { EstadoClub, EstadoIngreso, IdentidadClubDto, Rol, TipoDocumento } from './tipos';

/** Uno de los jugadores de la cuenta en un club, para la lista en la que la familia elige. */
export interface JugadorDeSesionDto {
  /** Es el identificador que se envía como jugador elegido. */
  usuarioRolId: string;
  nombres: string;
  apellidos: string;
  estadoIngreso: EstadoIngreso;
  /** Verdadero si está retirado del club: por el club, por su familia o por otro club. */
  retirado: boolean;
}

/**
 * El hermano que la familia acaba de agregar: lo mismo que `JugadorDeSesionDto` más el aviso de
 * que, si el club lo aprueba, dejará de estar en otro club donde está inscrito. No dice cuál.
 */
export interface HermanoAgregadoDto extends JugadorDeSesionDto {
  retiraDeOtroClub: boolean;
}

/**
 * Un club de la sesión: una entrada por club, aunque la cuenta tenga varios jugadores en él. El
 * rol, el estado de ingreso, el retiro y el nombre describen al integrante `usuarioRolId`: el único
 * de la cuenta en el club; si la sesión se inició con un documento, el de ese documento; y si hay
 * que elegir, el más antiguo, hasta que la aplicación los sustituya por los del jugador elegido.
 */
export interface ClubDeSesionDto {
  clubId: string;
  nombre: string;
  rol: Rol;
  estado: EstadoClub;
  estadoIngreso: EstadoIngreso;
  /** Verdadero si este jugador está retirado del club; no entra a él hasta que lo reincorporen. */
  retirado: boolean;
  identidad: IdentidadClubDto;
  nombres: string;
  apellidos: string;
  usuarioRolId: string;
  /**
   * Los jugadores de la cuenta en este club entre los que hay que elegir, del más antiguo al más
   * reciente. Vacía si tiene uno solo o si la sesión está limitada a uno: entonces no se ofrece
   * elegir ni cambiar.
   */
  jugadores: JugadorDeSesionDto[];
}

/**
 * Lo que la familia escribe para agregar un hermano. No lleva correo, celular ni contraseña: son
 * los de la cuenta.
 */
export interface AgregarHermanoDto {
  nombres: string;
  apellidos: string;
  tipoDocumento: TipoDocumento;
  numeroDocumento: string;
  /** `AAAA-MM-DD`; `null` si todavía no se escribió, para que la API diga que es obligatoria. */
  fechaNacimiento: string | null;
  /** Solo si el hermano es menor de 18 años y la cuenta no tiene responsable; si lo tiene, se ignora. */
  nombreResponsable: string | null;
}
