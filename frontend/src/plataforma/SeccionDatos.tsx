import { api } from '../compartido/api/cliente';
import type { ClubDetalleDto } from '../compartido/api/tipos';
import { PanelDatosClub } from '../compartido/componentes/PanelDatosClub';
import type { PropsSeccion } from './DetalleClub';

/**
 * Datos e identidad de un club en el panel, como tarjetas de visualización con su lápiz: el escudo
 * en grande, los datos (nombre, sede, dirección y contacto) y los colores. Los edita el
 * DESARROLLADOR con las rutas del panel; si el presidente los cambió antes, vale lo último.
 */
export function SeccionDatos({ club, alActualizar }: PropsSeccion) {
  const base = `/api/plataforma/clubes/${club.clubId}`;

  return (
    <PanelDatosClub
      club={club}
      guardarDatos={async (datos) => alActualizar(await api.put<ClubDetalleDto>(`${base}/configuracion`, datos))}
      guardarColores={async (datos) => alActualizar(await api.put<ClubDetalleDto>(`${base}/colores`, datos))}
      guardarEscudo={async (archivo) => alActualizar(await api.putArchivo<ClubDetalleDto>(`${base}/escudo`, archivo))}
    />
  );
}
