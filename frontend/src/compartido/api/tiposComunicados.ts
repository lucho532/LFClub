// Tipos de los comunicados del club de
// specs/011-comunicados-y-campana/contracts/api.yaml, con los mismos nombres.

import type { Rol } from './tipos';

/** CLUB: todo el club. CATEGORIAS: una o más categorías. EQUIPOS: equipos de una sola categoría. */
export type AlcanceComunicado = 'CLUB' | 'CATEGORIAS' | 'EQUIPOS';

/** Lo que envía el equipo del club para publicar. */
export interface PublicarComunicadoDto {
  /** La genera la pantalla al abrir el formulario y se conserva en los reintentos. */
  clave: string;
  /** Hasta 100 caracteres. */
  titulo: string;
  /** Texto plano, hasta 1.000 caracteres. */
  texto: string;
  alcance: AlcanceComunicado;
  /** Solo con CATEGORIAS. */
  categoriaIds?: string[];
  /** Solo con EQUIPOS; de una sola categoría. */
  equipoIds?: string[];
}

/** Quién lo publicó: nombre y rol copiados al publicar. */
export interface AutorDeComunicadoDto {
  /** Para abrir su ficha básica. Nulo si ya no es del club. */
  usuarioRolId?: string | null;
  nombre: string;
  rol: Extract<Rol, 'PRESIDENTE' | 'DIRECTIVO' | 'ENTRENADOR'>;
}

/** Una categoría o un equipo al que iba dirigido. */
export interface DestinoDeComunicadoDto {
  categoriaId?: string | null;
  categoriaNombre: string;
  equipoId?: string | null;
  equipoNombre?: string | null;
}

/** Cuándo y quién lo retiró. */
export interface RetiroDeComunicadoDto {
  en: string;
  porNombre: string;
}

/** Lo que ve el equipo del club. */
export interface ComunicadoPublicadoDto {
  id: string;
  titulo: string;
  texto: string;
  publicadoEn: string;
  publicadoPor: AutorDeComunicadoDto;
  alcance: AlcanceComunicado;
  /** Vacío con CLUB. */
  destinos: DestinoDeComunicadoDto[];
  /** Cuentas que lo recibieron al publicar. */
  familiasAlcanzadas: number;
  /** Ausente mientras está publicado. */
  retirado?: RetiroDeComunicadoDto;
  /** Falso si ya está retirado o si no es suyo ni es PRESIDENTE. */
  puedeRetirar: boolean;
}

/** Lo que ve la cuenta de un jugador: nunca la audiencia ni cuántos más lo recibieron. */
export interface ComunicadoRecibidoDto {
  id: string;
  titulo: string;
  texto: string;
  publicadoEn: string;
  publicadoPor: AutorDeComunicadoDto;
  leido: boolean;
  /** Ausente mientras no lo haya leído. */
  leidoEn?: string | null;
}

export interface PaginaDePublicados {
  items: ComunicadoPublicadoDto[];
  hayMas: boolean;
}

export interface PaginaDeRecibidos {
  items: ComunicadoRecibidoDto[];
  hayMas: boolean;
  /** El contador, para no pedirlo aparte al abrir la lista. */
  sinLeer: number;
}

export interface ContadorDeComunicadosDto {
  sinLeer: number;
}
