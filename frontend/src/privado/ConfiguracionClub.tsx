import { Navigate } from 'react-router-dom';
import { api } from '../compartido/api/cliente';
import type { ActualizarColoresDto, ActualizarConfiguracionClubDto, ClubDto } from '../compartido/api/tipos';
import { PanelDatosClub } from '../compartido/componentes/PanelDatosClub';
import { useSesion } from '../compartido/sesion/useSesion';
import { useClub } from './contextoClub';

/**
 * Datos del club, para su presidente: tarjetas de visualización con el escudo en grande, los datos
 * (nombre, sede, dirección y contacto) y los colores; cada una se edita con su lápiz, con la misma
 * vista previa y aviso de contraste que el panel de la plataforma (spec 007). Tras guardar, la pantalla toma el club que devuelve la API y recarga la sesión, para
 * que el menú, la barra superior y el desplegable de clubes muestren el cambio. La API rechaza a
 * cualquier otro rol; aquí simplemente no se le muestra la pantalla.
 */
export function ConfiguracionClub() {
  const { club, fijarClub } = useClub();
  const { recargar } = useSesion();

  if (club.miRol !== 'PRESIDENTE') {
    return <Navigate to={`/club/${club.clubId}`} replace />;
  }

  const base = `/api/clubes/${club.clubId}`;

  // El nombre y la identidad del club también están en la sesión (desplegable y cabecera).
  async function aplicar(actualizado: ClubDto) {
    fijarClub(actualizado);
    await recargar();
  }

  async function guardar(datos: ActualizarConfiguracionClubDto) {
    await aplicar(await api.put<ClubDto>(`${base}/configuracion`, datos));
  }

  return (
    <>
      <div className="cabecera">
        <h1>Datos del club</h1>
      </div>
      <PanelDatosClub
        key={club.clubId}
        club={club}
        guardarDatos={guardar}
        guardarColores={async (datos: ActualizarColoresDto) => aplicar(await api.put<ClubDto>(`${base}/colores`, datos))}
        guardarEscudo={async (archivo) => aplicar(await api.putArchivo<ClubDto>(`${base}/escudo`, archivo))}
      />
    </>
  );
}
