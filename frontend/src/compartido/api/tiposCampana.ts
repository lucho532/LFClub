// Tipos de la campana unificada de
// specs/012-avisos-de-actividades/contracts/api.yaml, con los mismos nombres.

import type { ComunicadoRecibidoDto } from './tiposComunicados';

export type TipoDeActividad = 'ENTRENAMIENTO' | 'PARTIDO';

/** La actividad a la que apunta un aviso; nunca lleva más datos que estos. */
export interface ActividadDeAvisoDto {
  tipo: TipoDeActividad;
  id: string;
  /** Falso si la cuenta ya no puede verla: la pantalla muestra el mensaje y no navega. */
  disponible: boolean;
}

/** Una línea corta escrita por el sistema; sin autor, título ni audiencia. */
export interface AvisoRecibidoDto {
  id: string;
  /** Texto plano de hasta 300 caracteres; nunca se interpreta. */
  texto: string;
  generadoEn: string;
  actividad: ActividadDeAvisoDto;
  leido: boolean;
  /** Ausente mientras no lo haya leído. */
  leidoEn?: string | null;
}

export type TipoDeElementoDeCampana = 'COMUNICADO' | 'AVISO';

/** Exactamente uno de `comunicado` o `aviso`, el que indica `tipo`. */
export interface ElementoDeCampanaDto {
  tipo: TipoDeElementoDeCampana;
  comunicado?: ComunicadoRecibidoDto;
  aviso?: AvisoRecibidoDto;
}

export interface PaginaDeCampanaDto {
  elementos: ElementoDeCampanaDto[];
  hayMas: boolean;
  /** Comunicados más avisos sin leer. */
  sinLeer: number;
}

export interface ContadorDeCampanaDto {
  sinLeer: number;
}

/** Un elemento de la campana con el identificador de su contenido, que es el cursor de la lista. */
export type ElementoDeLista = ElementoDeCampanaDto & { id: string };

/** Añade el identificador del contenido (comunicado o aviso) al elemento que devuelve la API. */
export function conIdentificador(elemento: ElementoDeCampanaDto): ElementoDeLista {
  return { ...elemento, id: (elemento.comunicado?.id ?? elemento.aviso?.id) as string };
}
