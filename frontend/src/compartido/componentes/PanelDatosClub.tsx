import { useState } from 'react';
import { mensajeDe } from '../api/errores';
import type { ActualizarColoresDto, ActualizarConfiguracionClubDto, IdentidadClubDto } from '../api/tipos';
import { Escudo } from '../tema/IdentidadClub';
import { Aviso } from './Aviso';
import { COLOR_ACENTO_NEUTRO, COLOR_PRINCIPAL_NEUTRO, DialogoEditarColoresClub } from './DialogoEditarColoresClub';
import { DialogoEditarDatosClub } from './DialogoEditarDatosClub';
import { IconoLapiz } from './Iconos';
import { LapizDeImagen } from './LapizDeImagen';
import { Tarjeta } from './Tarjeta';

const SIN_REGISTRAR = 'Sin registrar';

interface Props {
  club: ActualizarConfiguracionClubDto & { identidad: IdentidadClubDto };
  /** Guarda los datos con la ruta de quien edita; si falla, debe lanzar el error. */
  guardarDatos: (datos: ActualizarConfiguracionClubDto) => Promise<void>;
  /** Guarda los colores con la ruta de quien edita; si falla, debe lanzar el error. */
  guardarColores: (datos: ActualizarColoresDto) => Promise<unknown>;
  /** Guarda el escudo con la ruta de quien edita; si falla, debe lanzar el error. */
  guardarEscudo: (archivo: File) => Promise<unknown>;
}

function LapizDeTarjeta({ etiqueta, alPulsar }: { etiqueta: string; alPulsar: () => void }) {
  return (
    <button type="button" className="lapiz-tarjeta" aria-label={etiqueta} title={etiqueta} onClick={alPulsar}>
      <IconoLapiz />
    </button>
  );
}

/**
 * Los datos de un club como tarjetas de solo lectura: el escudo en grande, los datos (nombre, sede,
 * dirección y contacto) y los colores. Cada tarjeta lleva su lápiz, que abre la edición; nada se
 * edita hasta pulsarlo. Lo usan el presidente, en "Datos del club", y el desarrollador, en el
 * detalle del club, cada uno con sus rutas (spec 007): las reglas y el aviso de contraste son los
 * mismos. Quién puede guardar lo decide la API.
 */
export function PanelDatosClub({ club, guardarDatos, guardarColores, guardarEscudo }: Props) {
  const [editando, setEditando] = useState<'datos' | 'colores' | null>(null);
  const [ocupado, setOcupado] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [hecho, setHecho] = useState<string | null>(null);
  const { identidad } = club;

  async function subirEscudo(archivo: File) {
    setError(null);
    setHecho(null);
    setOcupado(true);
    try {
      await guardarEscudo(archivo);
      setHecho('Escudo guardado.');
    } catch (fallo) {
      setError(mensajeDe(fallo));
    } finally {
      setOcupado(false);
    }
  }

  const datos = [
    { etiqueta: 'Nombre del club', valor: club.nombre },
    { etiqueta: 'Sede', valor: club.sede },
    { etiqueta: 'Dirección', valor: club.direccion },
    { etiqueta: 'Correo de contacto', valor: club.correoContacto },
    { etiqueta: 'Teléfono de contacto', valor: club.telefonoContacto },
  ];
  const colores = [
    { etiqueta: 'Color principal', valor: identidad.colorPrincipal ?? COLOR_PRINCIPAL_NEUTRO },
    { etiqueta: 'Color de acento', valor: identidad.colorAcento ?? COLOR_ACENTO_NEUTRO },
  ];

  return (
    <>
      {error && <Aviso tono="error">{error}</Aviso>}
      {hecho && <Aviso tono="exito">{hecho}</Aviso>}

      <Tarjeta titulo="Escudo">
        <div className="escudo-grande">
          <LapizDeImagen
            que="el escudo"
            tieneImagen={Boolean(identidad.urlEscudo)}
            ocupado={ocupado}
            alElegir={(archivo) => void subirEscudo(archivo)}
          >
            <Escudo identidad={identidad} nombre={club.nombre} />
          </LapizDeImagen>
        </div>
      </Tarjeta>

      <Tarjeta titulo="Datos del club" acciones={<LapizDeTarjeta etiqueta="Editar datos del club" alPulsar={() => setEditando('datos')} />}>
        <dl className="ficha-datos">
          {datos.map(({ etiqueta, valor }) => (
            <div key={etiqueta}>
              <dt>{etiqueta}</dt>
              <dd className={valor ? undefined : 'texto-suave'}>{valor || SIN_REGISTRAR}</dd>
            </div>
          ))}
        </dl>
      </Tarjeta>

      <Tarjeta titulo="Colores" acciones={<LapizDeTarjeta etiqueta="Editar colores" alPulsar={() => setEditando('colores')} />}>
        <dl className="ficha-datos">
          {colores.map(({ etiqueta, valor }) => (
            <div key={etiqueta}>
              <dt>{etiqueta}</dt>
              <dd className="fila">
                <span className="muestra-color" style={{ background: valor }} aria-hidden="true" />
                {valor.toUpperCase()}
              </dd>
            </div>
          ))}
        </dl>
      </Tarjeta>

      {editando === 'datos' && (
        <DialogoEditarDatosClub
          inicial={club}
          alCancelar={() => setEditando(null)}
          alGuardar={async (nuevos) => {
            await guardarDatos(nuevos);
            setEditando(null);
            setError(null);
            setHecho('Datos guardados.');
          }}
        />
      )}
      {editando === 'colores' && (
        <DialogoEditarColoresClub
          identidad={identidad}
          nombre={club.nombre}
          alCancelar={() => setEditando(null)}
          alGuardar={async (nuevos) => {
            await guardarColores(nuevos);
            setEditando(null);
            setError(null);
            setHecho('Colores guardados.');
          }}
        />
      )}
    </>
  );
}
