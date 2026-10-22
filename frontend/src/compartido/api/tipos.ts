// Tipos de los esquemas de specs/001-base-multiclub/contracts/api.yaml y de
// specs/002-ingreso-club/contracts/api.yaml, con los cambios de
// specs/004-invitacion-con-rol/contracts/api.yaml y con los mismos nombres. Los de
// specs/003-categorias-club/contracts/api.yaml están en tiposCategorias.ts, y los de la sala de
// espera y los ingresos aprobados, en tiposIngresos.ts.

import type { EstadoDePagoClub, MotivoDeSuspension } from './tiposPagoPlataforma';
import type { ClubDeSesionDto } from './tiposSesion';

export type Rol = 'DESARROLLADOR' | 'PRESIDENTE' | 'DIRECTIVO' | 'ENTRENADOR' | 'JUGADOR';
export type EstadoClub = 'ACTIVO' | 'SUSPENDIDO' | 'DADO_DE_BAJA';
export type EstadoEnvio = 'PENDIENTE' | 'ENVIADO' | 'FALLIDO';
export type EstadoIngreso = 'EN_ESPERA' | 'APROBADO';
export type EstadoInvitacion = 'PENDIENTE' | 'USADA' | 'VENCIDA' | 'CANCELADA';
export type RolDeIngreso = 'JUGADOR' | 'ENTRENADOR' | 'DIRECTIVO';
export type TipoDocumento =
  | 'REGISTRO_CIVIL'
  | 'TARJETA_IDENTIDAD'
  | 'CEDULA_CIUDADANIA'
  | 'CEDULA_EXTRANJERIA';

export interface Problema {
  title: string;
  status: number;
  codigo: string;
  errores?: Record<string, string[]>;
  /** Campos propios de un código, como la lista `clubes` de `presidente_unico`. */
  [campo: string]: unknown;
}

export interface TokenDto {
  token: string;
}

export interface IniciarSesionDto {
  identificador: string;
  contrasena: string;
}

export interface TokenSesionDto {
  token: string;
  venceEn: string;
}

export interface IdentidadClubDto {
  colorPrincipal: string | null;
  colorAcento: string | null;
  urlEscudo: string | null;
}

// Los tipos de la sesión con varios jugadores (006) están en tiposSesion.ts.
export type { ClubDeSesionDto };

export interface SesionDto {
  usuarioId: string;
  correo: string;
  esDesarrollador: boolean;
  versionFoto: number;
  clubes: ClubDeSesionDto[];
}

export interface PedirRecuperacionDto {
  correo: string;
}

export interface RestablecerContrasenaDto {
  token: string;
  contrasenaNueva: string;
}

export interface InvitacionVigenteDto {
  nombreClub: string;
  rol: Rol;
  correo: string;
  tieneCuenta: boolean;
  /** Verdadero solo en las invitaciones de jugador: el registro pide el nombre del responsable. */
  pideResponsable: boolean;
  /**
   * Verdadero solo si el correo ya tiene cuenta, la invitación es de jugador, la persona es menor
   * de 18 años y su cuenta no tiene responsable: la aceptación pide entonces ese nombre.
   */
  faltaResponsable: boolean;
  identidad: IdentidadClubDto;
}

export interface AceptarInvitacionDto {
  token: string;
  /** Solo se envía cuando la invitación trae `faltaResponsable`; en otro caso la API lo ignora. */
  nombreResponsable?: string | null;
}

export interface RegistrarConInvitacionDto {
  token: string;
  nombres: string;
  apellidos: string;
  tipoDocumento: TipoDocumento;
  numeroDocumento: string;
  fechaNacimiento: string;
  celular: string;
  /**
   * Solo con una invitación de jugador: obligatorio si la persona es menor de 18 años el día del
   * registro. Con cualquier otro rol no se envía.
   */
  nombreResponsable?: string | null;
  contrasena: string;
}

export interface ClubDto {
  clubId: string;
  nombre: string;
  sede: string | null;
  direccion: string | null;
  correoContacto: string | null;
  telefonoContacto: string | null;
  estado: EstadoClub;
  identidad: IdentidadClubDto;
  miRol: Rol;
  /** Integrante de quien pregunta en este club; con el rol JUGADOR abre "Mi ficha". */
  miUsuarioRolId: string;
}

export interface ActualizarConfiguracionClubDto {
  nombre: string;
  sede: string | null;
  direccion: string | null;
  correoContacto: string | null;
  telefonoContacto: string | null;
}

/** Integrantes activos de un club por rol, para la facturación. */
export interface ConteoIntegrantesDto {
  jugadores: number;
  entrenadores: number;
  directivos: number;
  presidentes: number;
}

export interface ClubResumenDto {
  clubId: string;
  nombre: string;
  estado: EstadoClub;
  presidenteRegistrado: boolean;
  identidad: IdentidadClubDto;
  integrantes: ConteoIntegrantesDto;
  /** Situación de pago frente a la plataforma (spec 017). */
  estadoDePago: EstadoDePagoClub;
  valorAdeudado: number;
  mesDeInicioDelCobro: string | null;
  motivoDeSuspension: MotivoDeSuspension | null;
}

export interface PresidenteDto {
  usuarioRolId: string;
  nombres: string;
  apellidos: string;
  correo: string;
}

export interface InvitacionDto {
  invitacionId: string;
  correo: string;
  rol: Rol;
  estadoEnvio: EstadoEnvio;
  creadaEn: string;
  venceEn: string;
  vencida: boolean;
}

export interface ClubDetalleDto {
  clubId: string;
  nombre: string;
  sede: string | null;
  direccion: string | null;
  correoContacto: string | null;
  telefonoContacto: string | null;
  estado: EstadoClub;
  estadoCambiadoEn: string | null;
  identidad: IdentidadClubDto;
  presidentes: PresidenteDto[];
  invitaciones: InvitacionDto[];
  /** Situación de pago frente a la plataforma (spec 017). */
  estadoDePago: EstadoDePagoClub;
  valorAdeudado: number;
  mesDeInicioDelCobro: string | null;
  motivoDeSuspension: MotivoDeSuspension | null;
}

export interface CrearClubDto {
  nombre: string;
  correoPresidente: string;
}

export interface ActualizarColoresDto {
  colorPrincipal: string;
  colorAcento: string;
}

export interface CambiarEstadoClubDto {
  estado: EstadoClub;
}

export interface EliminarClubDto {
  nombreDeConfirmacion: string;
}

export interface ReenviarInvitacionDto {
  correo?: string;
}

export interface RetirarPresidenteDto {
  accion: 'ASIGNAR_ROL' | 'ELIMINAR_DEL_CLUB';
  rolNuevo?: 'DIRECTIVO' | 'ENTRENADOR';
}

export interface InvitarAlClubDto {
  correo: string;
  /** Rol con el que entra la persona. Lo elige el presidente al invitar y no cambia después. */
  rol: RolDeIngreso;
}

export interface InvitacionClubDto {
  invitacionId: string;
  correo: string;
  rol: RolDeIngreso;
  estado: EstadoInvitacion;
  estadoEnvio: EstadoEnvio;
  /** Nombre de quien la envió; nulo si ya no está en el club. */
  enviadaPor: string | null;
  creadaEn: string;
  venceEn: string;
}

/** Cuerpo opcional de la aprobación: al aprobar no se elige rol y la API solo admite JUGADOR. */
export interface AprobarIngresoDto {
  rol?: 'JUGADOR' | null;
}
