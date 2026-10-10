import { api } from '../compartido/api/cliente';
import type { ClubDetalleDto } from '../compartido/api/tipos';
import { EditorIdentidad } from '../compartido/componentes/EditorIdentidad';
import { Tarjeta } from '../compartido/componentes/Tarjeta';
import type { PropsSeccion } from './DetalleClub';

/**
 * Identidad visual de un club en el panel: escudo y colores, con el mismo editor que usa el
 * presidente en "Datos del club" (spec 007). El DESARROLLADOR la cambia con las rutas del panel; si
 * el presidente la cambió antes, vale la última.
 */
export function SeccionIdentidad({ club, alActualizar }: PropsSeccion) {
  const base = `/api/plataforma/clubes/${club.clubId}`;

  return (
    <Tarjeta titulo="Identidad">
      <EditorIdentidad
        identidad={club.identidad}
        nombre={club.nombre}
        guardarColores={async (datos) => alActualizar(await api.put<ClubDetalleDto>(`${base}/colores`, datos))}
        guardarEscudo={async (archivo) => alActualizar(await api.putArchivo<ClubDetalleDto>(`${base}/escudo`, archivo))}
      />
    </Tarjeta>
  );
}
