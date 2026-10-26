// Tipos de specs/018-cuerpo-tecnico/contracts/api.yaml, con los mismos nombres.
import type { Rol, TipoDocumento } from './tipos';
import type { EntrenamientoResumenDto } from './tiposActividades';
import type { CategoriaDeFichaBasicaDto } from './tiposPersonas';

/** Una persona del cuerpo técnico en la lista del PRESIDENTE. */
export interface MiembroCuerpoTecnicoDto {
  usuarioRolId: string;
  nombres: string;
  apellidos: string;
  rol: Exclude<Rol, 'DESARROLLADOR' | 'JUGADOR'>;
  /** 0 si no tiene foto de perfil. */
  versionFoto: number;
  /** Ordenadas por año; vacía si no entrena. */
  categorias: CategoriaDeFichaBasicaDto[];
}

/** Todos los datos de una persona del cuerpo técnico: es un dato personal, solo para su PRESIDENTE. */
export interface MiembroCuerpoTecnicoDetalleDto extends MiembroCuerpoTecnicoDto {
  tipoDocumento: TipoDocumento;
  numeroDocumento: string;
  correo: string;
  celular: string | null;
  /** Los que no han terminado y ve por sus categorías; vacía si no es ENTRENADOR. */
  proximosEntrenamientos: EntrenamientoResumenDto[];
}
