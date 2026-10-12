import { useState } from 'react';
import { api } from '../compartido/api/cliente';
import { mensajeDe } from '../compartido/api/errores';
import type { SesionDto } from '../compartido/api/tipos';
import { Aviso } from '../compartido/componentes/Aviso';
import { FotoAmpliable } from '../compartido/componentes/FotoAmpliable';
import { LapizDeImagen } from '../compartido/componentes/LapizDeImagen';
import { useFotoPerfil } from '../compartido/sesion/useFotoPerfil';
import { useSesion } from '../compartido/sesion/useSesion';

/**
 * Foto de perfil en "Mi perfil": la persona la carga, la cambia y la quita con el lápiz del
 * círculo (`LapizDeImagen`) y, al tocarla, la ve en tamaño completo (`FotoAmpliable`). Es opcional
 * y la misma en todos sus clubes. Solo ella la cambia; si es presidente, directivo o entrenador, la ven además
 * los integrantes de sus clubes en su ficha básica y junto a su nombre (spec 008, §14.2).
 */
export function SeccionFotoPerfil({ sesion, quienLaVe }: { sesion: SesionDto; quienLaVe: string }) {
  const { actualizar, recargar } = useSesion();
  const foto = useFotoPerfil();
  const [ocupado, setOcupado] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [hecho, setHecho] = useState<string | null>(null);

  async function ejecutar(accion: () => Promise<void>, mensaje: string) {
    setError(null);
    setHecho(null);
    setOcupado(true);
    try {
      await accion();
      setHecho(mensaje);
    } catch (fallo) {
      setError(mensajeDe(fallo));
    } finally {
      setOcupado(false);
    }
  }

  function quitar() {
    void ejecutar(async () => {
      await api.delete('/api/cuenta/foto');
      await recargar();
    }, 'Foto quitada.');
  }

  return (
    <section className="tarjeta">
      <h2>Foto de perfil</h2>
      {error && <Aviso tono="error">{error}</Aviso>}
      {hecho && <Aviso tono="exito">{hecho}</Aviso>}

      <div className="fila">
        <LapizDeImagen
          que="la foto"
          tieneImagen={sesion.versionFoto > 0}
          ocupado={ocupado}
          alElegir={(archivo) =>
            void ejecutar(async () => actualizar(await api.putArchivo<SesionDto>('/api/cuenta/foto', archivo)), 'Foto guardada.')
          }
          alQuitar={quitar}
        >
          {foto ? (
            <FotoAmpliable className="distintivo foto-perfil-grande" src={foto} alt="Tu foto de perfil" />
          ) : (
            <span className="distintivo foto-perfil-grande" aria-hidden="true">
              {sesion.correo.charAt(0).toUpperCase()}
            </span>
          )}
        </LapizDeImagen>
        <p className="texto-suave">
          {sesion.versionFoto === 0 ? 'Es opcional.' : `Es la misma en todos tus clubes. ${quienLaVe}`}
        </p>
      </div>
    </section>
  );
}
