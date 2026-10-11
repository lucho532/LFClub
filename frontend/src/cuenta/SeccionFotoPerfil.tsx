import { useId, useState, type ChangeEvent } from 'react';
import { api } from '../compartido/api/cliente';
import { mensajeDe } from '../compartido/api/errores';
import type { SesionDto } from '../compartido/api/tipos';
import { Aviso } from '../compartido/componentes/Aviso';
import { Boton } from '../compartido/componentes/Boton';
import { useFotoPerfil } from '../compartido/sesion/useFotoPerfil';
import { useSesion } from '../compartido/sesion/useSesion';

/**
 * Foto de perfil en "Mi perfil": la persona la carga, la cambia y la quita. Es opcional y la misma
 * en todos sus clubes. Solo ella la cambia; si es presidente, directivo o entrenador, la ven además
 * los integrantes de sus clubes en su ficha básica y junto a su nombre (spec 008, §14.2).
 */
export function SeccionFotoPerfil({ sesion, quienLaVe }: { sesion: SesionDto; quienLaVe: string }) {
  const { actualizar, recargar } = useSesion();
  const foto = useFotoPerfil();
  const idArchivo = useId();
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

  function cargar(evento: ChangeEvent<HTMLInputElement>) {
    const archivo = evento.target.files?.[0];
    evento.target.value = '';
    if (archivo) {
      void ejecutar(async () => actualizar(await api.putArchivo<SesionDto>('/api/cuenta/foto', archivo)), 'Foto guardada.');
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
        {foto ? (
          <img className="distintivo" style={{ width: 96, height: 96 }} src={foto} alt="Tu foto de perfil" />
        ) : (
          <span className="distintivo" style={{ width: 96, height: 96, fontSize: 32 }} aria-hidden="true">
            {sesion.correo.charAt(0).toUpperCase()}
          </span>
        )}
        <p className="texto-suave">
          {sesion.versionFoto === 0 ? 'Todavía no tienes foto. Es opcional.' : `Es la misma en todos tus clubes. ${quienLaVe}`}
        </p>
      </div>

      <div className="campo">
        <label htmlFor={idArchivo}>{sesion.versionFoto === 0 ? 'Cargar una foto' : 'Cambiar la foto'}</label>
        <input id={idArchivo} type="file" accept="image/png,image/jpeg,image/webp" onChange={cargar} disabled={ocupado} />
        <span className="texto-suave">PNG, JPEG o WebP de hasta 1 MB.</span>
      </div>

      {sesion.versionFoto > 0 && (
        <div className="fila">
          <Boton variante="secundario" onClick={quitar} disabled={ocupado}>
            Quitar la foto
          </Boton>
        </div>
      )}
    </section>
  );
}
