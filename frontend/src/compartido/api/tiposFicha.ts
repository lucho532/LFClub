// Tipos del contrato de la ficha del jugador (specs/005-ficha-jugador/contracts/api.yaml).
// Van aparte de tipos.ts para que ningún archivo pase de 250 líneas.

import type { TipoDocumento } from './tipos';

/** Documentos que pide la ficha: una lista fija, igual para todos los clubes. */
export type DocumentoPedido = 'COPIA_DOCUMENTO_IDENTIDAD' | 'CERTIFICADO_SALUD';

export type GrupoSanguineo =
  | 'A_POSITIVO'
  | 'A_NEGATIVO'
  | 'B_POSITIVO'
  | 'B_NEGATIVO'
  | 'AB_POSITIVO'
  | 'AB_NEGATIVO'
  | 'O_POSITIVO'
  | 'O_NEGATIVO';

/** Datos de la cuenta: los mismos en todos sus jugadores y clubes. */
export interface ContactoDeFichaDto {
  /** No se cambia desde la ficha. */
  correo: string;
  celular: string | null;
  nombreResponsable: string | null;
}

export interface ContactoEmergenciaDto {
  nombre: string | null;
  parentesco: string | null;
  celular: string | null;
}

export interface SeguridadSocialDto {
  entidadSalud: string | null;
  lugarAtencion: string | null;
}

/** Los datos más restringidos de la ficha. Un directivo nunca los recibe. */
export interface DatosClinicosDto {
  grupoSanguineo: GrupoSanguineo | null;
  alergias: string | null;
  enfermedades: string | null;
  medicamentos: string | null;
  observaciones: string | null;
}

export interface DocumentoDeFichaDto {
  documento: DocumentoPedido;
  entregado: boolean;
  /** Fecha del archivo vigente, en UTC; `null` si está pendiente. */
  subidoEn: string | null;
  tipoContenido: string | null;
  tamanoBytes: number | null;
}

/** Solo el último cambio; no hay historial. */
export interface UltimoCambioDto {
  fecha: string;
  /** Nombre de quien cambió, tal como era en ese momento. */
  autor: string;
  /** Verdadero si lo hizo la cuenta del jugador (su familia) y no el presidente. */
  porLaCuentaDelJugador: boolean;
}

/** Lo que quien pregunta puede hacer en esta ficha; la API lo comprueba igualmente. */
export interface PermisosDeFichaDto {
  /** Contacto, salud, documento de identidad y archivos. */
  puedeCambiar: boolean;
  /** Nombres, apellidos y fecha de nacimiento. */
  puedeCorregirIdentidad: boolean;
}

/**
 * La ficha de un jugador en un club. `datosClinicos`, `documentos` y `ultimoCambio` no vienen
 * cuando quien pregunta no puede verlos o cuando nadie ha cambiado la ficha: la propiedad falta.
 */
export interface FichaJugadorDto {
  usuarioRolId: string;
  nombres: string;
  apellidos: string;
  tipoDocumento: TipoDocumento;
  numeroDocumento: string;
  /** `AAAA-MM-DD`. */
  fechaNacimiento: string;
  /** Verdadero si hoy tiene menos de 18 años; entonces el responsable es obligatorio. */
  esMenorDeEdad: boolean;
  retirado: boolean;
  categoriaId: string | null;
  categoriaAnio: number | null;
  /** Nombres de los equipos de su categoría en los que juega. */
  equipos: string[];
  contacto: ContactoDeFichaDto;
  contactoEmergencia: ContactoEmergenciaDto;
  seguridadSocial: SeguridadSocialDto;
  datosClinicos?: DatosClinicosDto;
  /** Siempre los dos documentos pedidos, entregados o pendientes. */
  documentos?: DocumentoDeFichaDto[];
  ultimoCambio?: UltimoCambioDto;
  permisos: PermisosDeFichaDto;
  /** Fecha UTC de la última carga de la foto del jugador; `null` si no tiene (spec 007). */
  fotoActualizadaEn: string | null;
}

/** Todo es opcional salvo el celular y, si el jugador es menor de 18 años, el responsable. */
export interface ActualizarFichaDto {
  celular: string;
  nombreResponsable: string | null;
  emergenciaNombre: string | null;
  emergenciaParentesco: string | null;
  emergenciaCelular: string | null;
  entidadSalud: string | null;
  lugarAtencion: string | null;
  grupoSanguineo: GrupoSanguineo | null;
  alergias: string | null;
  enfermedades: string | null;
  medicamentos: string | null;
  observaciones: string | null;
}

export interface CambiarDocumentoIdentidadDto {
  tipoDocumento: TipoDocumento;
  numeroDocumento: string;
}

export interface CorregirIdentidadDto {
  nombres: string;
  apellidos: string;
  /** `AAAA-MM-DD`; no puede ser futura. */
  fechaNacimiento: string;
}
