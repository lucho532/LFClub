import { useState } from 'react';
import { mensajeDe } from '../api/errores';
import type { ActualizarColoresDto, IdentidadClubDto } from '../api/tipos';
import { temasDondeSePierde, type Tema } from '../tema/contraste';
import { IdentidadClub } from '../tema/IdentidadClub';
import { Aviso } from './Aviso';
import { Campo } from './Campo';
import { DialogoConfirmacion } from './DialogoConfirmacion';

const NOMBRE_DEL_TEMA: Record<Tema, string> = { claro: 'claro', oscuro: 'oscuro' };

/** Texto del aviso de contraste de un color, o `null` si se distingue en los dos temas. */
function avisoDeContraste(nombre: string, color: string): string | null {
  const temas = temasDondeSePierde(color);
  if (temas.length === 0) {
    return null;
  }

  return `El color ${nombre} casi no se distingue del fondo en el tema ${temas.map((tema) => NOMBRE_DEL_TEMA[tema]).join(' ni en el ')}.`;
}

export const COLOR_PRINCIPAL_NEUTRO = '#231F1E';
export const COLOR_ACENTO_NEUTRO = '#5E5650';

interface Props {
  identidad: IdentidadClubDto;
  /** Nombre del club, para la vista previa. */
  nombre: string;
  /** Guarda los colores con la ruta de quien edita; si falla, debe lanzar el error. */
  alGuardar: (datos: ActualizarColoresDto) => Promise<unknown>;
  alCancelar: () => void;
}

/**
 * Diálogo que abre el lápiz de la tarjeta "Colores": color principal y de acento, con la vista
 * previa en los dos temas. Antes de guardar avisa si un color se perdería en algún tema; es un
 * aviso y no impide guardar. Quién puede guardar lo decide la API.
 */
export function DialogoEditarColoresClub({ identidad, nombre, alGuardar, alCancelar }: Props) {
  const [principal, setPrincipal] = useState(identidad.colorPrincipal ?? COLOR_PRINCIPAL_NEUTRO);
  const [acento, setAcento] = useState(identidad.colorAcento ?? COLOR_ACENTO_NEUTRO);
  const [error, setError] = useState<string>();
  const [enviando, setEnviando] = useState(false);

  const avisos = [avisoDeContraste('principal', principal), avisoDeContraste('de acento', acento)].filter(
    (aviso): aviso is string => aviso !== null,
  );
  const vistaPrevia = { colorPrincipal: principal, colorAcento: acento, urlEscudo: identidad.urlEscudo };

  async function guardar() {
    setError(undefined);
    setEnviando(true);
    try {
      await alGuardar({ colorPrincipal: principal, colorAcento: acento });
    } catch (fallo) {
      setError(mensajeDe(fallo));
    } finally {
      setEnviando(false);
    }
  }

  return (
    <DialogoConfirmacion
      abierto
      titulo="Editar colores"
      textoConfirmar="Guardar colores"
      cargando={enviando}
      error={error}
      alConfirmar={() => void guardar()}
      alCancelar={alCancelar}
    >
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
    </DialogoConfirmacion>
  );
}
