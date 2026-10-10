import { useId, useState, type ChangeEvent, type FormEvent } from 'react';
import { mensajeDe } from '../api/errores';
import type { ActualizarColoresDto, IdentidadClubDto } from '../api/tipos';
import { temasDondeSePierde, type Tema } from '../tema/contraste';
import { Escudo, IdentidadClub } from '../tema/IdentidadClub';
import { Aviso } from './Aviso';
import { Boton } from './Boton';
import { Campo } from './Campo';

const NOMBRE_DEL_TEMA: Record<Tema, string> = { claro: 'claro', oscuro: 'oscuro' };

/** Texto del aviso de contraste de un color, o `null` si se distingue en los dos temas. */
function avisoDeContraste(nombre: string, color: string): string | null {
  const temas = temasDondeSePierde(color);
  if (temas.length === 0) {
    return null;
  }

  return `El color ${nombre} casi no se distingue del fondo en el tema ${temas.map((tema) => NOMBRE_DEL_TEMA[tema]).join(' ni en el ')}.`;
}

interface Props {
  identidad: IdentidadClubDto;
  /** Nombre del club, para la vista previa y el texto alternativo del escudo. */
  nombre: string;
  /** Guarda los colores con la ruta de quien edita; la pantalla actualiza el club con la respuesta. */
  guardarColores: (datos: ActualizarColoresDto) => Promise<unknown>;
  /** Guarda el escudo con la ruta de quien edita; la pantalla actualiza el club con la respuesta. */
  guardarEscudo: (archivo: File) => Promise<unknown>;
}

/**
 * Formulario de la identidad visual de un club: escudo y colores, con la vista previa en los dos
 * temas. Lo usan el panel del desarrollador y "Datos del club" del presidente (spec 007, RF-019),
 * cada uno con sus rutas, así que las reglas y el aviso son los mismos. Antes de guardar avisa si un
 * color se perdería en algún tema; es un aviso y no impide guardar. Quién puede guardar lo decide
 * la API.
 */
export function EditorIdentidad({ identidad, nombre, guardarColores, guardarEscudo }: Props) {
  const [principal, setPrincipal] = useState(identidad.colorPrincipal ?? '#231F1E');
  const [acento, setAcento] = useState(identidad.colorAcento ?? '#5E5650');
  const idEscudo = useId();
  const [ocupado, setOcupado] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [hecho, setHecho] = useState<string | null>(null);

  const avisos = [avisoDeContraste('principal', principal), avisoDeContraste('de acento', acento)].filter(
    (aviso): aviso is string => aviso !== null,
  );
  const vistaPrevia = { colorPrincipal: principal, colorAcento: acento, urlEscudo: identidad.urlEscudo };

  async function ejecutar(accion: () => Promise<unknown>, mensaje: string) {
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

  function enviarColores(evento: FormEvent) {
    evento.preventDefault();
    void ejecutar(() => guardarColores({ colorPrincipal: principal, colorAcento: acento }), 'Colores guardados.');
  }

  function cargarEscudo(evento: ChangeEvent<HTMLInputElement>) {
    const archivo = evento.target.files?.[0];
    evento.target.value = '';
    if (archivo) {
      void ejecutar(() => guardarEscudo(archivo), 'Escudo guardado.');
    }
  }

  return (
    <>
      {error && <Aviso tono="error">{error}</Aviso>}
      {hecho && <Aviso tono="exito">{hecho}</Aviso>}

      <div className="fila">
        <Escudo identidad={identidad} nombre={nombre} />
        <div className="campo">
          <label htmlFor={idEscudo}>{identidad.urlEscudo ? 'Cambiar el escudo' : 'Cargar el escudo'}</label>
          <input
            id={idEscudo}
            type="file"
            accept="image/png,image/jpeg,image/webp"
            onChange={cargarEscudo}
            disabled={ocupado}
          />
          <span className="texto-suave">PNG, JPEG o WebP de hasta 1 MB.</span>
        </div>
      </div>

      <form className="columna" onSubmit={enviarColores}>
        <div className="rejilla">
          <Campo etiqueta="Color principal" type="color" valor={principal} alCambiar={setPrincipal} />
          <Campo etiqueta="Color de acento" type="color" valor={acento} alCambiar={setAcento} />
        </div>

        <div className="rejilla" aria-label="Vista previa en los dos temas">
          {(['claro', 'oscuro'] as Tema[]).map((tema) => (
            <div key={tema} data-tema={tema} className="tarjeta" style={{ background: 'var(--fondo)', color: 'var(--texto)' }}>
              <span className="texto-suave">Tema {NOMBRE_DEL_TEMA[tema]}</span>
              <IdentidadClub identidad={vistaPrevia} className="columna">
                {/* Muestra del menú lateral: su fondo y el ítem activo, como se verán en el club. */}
                <span className="vista-menu">
                  <span className="vista-menu-item vista-menu-activo">Inicio</span>
                  <span className="vista-menu-item">Jugadores</span>
                </span>
                <span className="fila">
                  <span className="boton boton-principal">{nombre}</span>
                  <span
                    className="etiqueta"
                    style={{ background: 'var(--color-club-acento)', color: 'var(--color-club-acento-texto)' }}
                  >
                    Acento
                  </span>
                </span>
              </IdentidadClub>
            </div>
          ))}
        </div>

        {avisos.map((aviso) => (
          <Aviso key={aviso} tono="aviso">
            {aviso} Puedes guardarlo igualmente.
          </Aviso>
        ))}

        <div className="fila">
          <Boton type="submit" cargando={ocupado} textoCargando="Guardando…">
            Guardar colores
          </Boton>
        </div>
      </form>
    </>
  );
}
