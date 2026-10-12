import { useState } from 'react';
import { api } from '../../compartido/api/cliente';
import { ErrorApi, mensajeDe } from '../../compartido/api/errores';
import type { TipoDocumento } from '../../compartido/api/tipos';
import type {
  CambiarDocumentoIdentidadDto,
  CorregirIdentidadDto,
  FichaJugadorDto,
} from '../../compartido/api/tiposFicha';
import { Campo } from '../../compartido/componentes/Campo';
import { DialogoConfirmacion } from '../../compartido/componentes/DialogoConfirmacion';
import { dia, nombreDeTipoDocumento } from '../../compartido/formato';
import { useClub } from '../contextoClub';

const TIPOS: TipoDocumento[] = ['REGISTRO_CIVIL', 'TARJETA_IDENTIDAD', 'CEDULA_CIUDADANIA', 'CEDULA_EXTRANJERIA'];

/** Qué se guardó en una edición: el documento, la identidad o las dos cosas. */
export interface CambiosGuardados {
  documento: boolean;
  identidad: boolean;
}

interface Props {
  ficha: FichaJugadorDto;
  /** Dirección de la ficha en la API. */
  rutaDeLaFicha: string;
  /** Recibe la ficha tal como la devolvió la API y qué se guardó. */
  alGuardar: (ficha: FichaJugadorDto, cambios: CambiosGuardados) => void;
  /** Recibe la ficha cuando solo se guardó una parte y el diálogo sigue abierto por el error de la otra. */
  alActualizar: (ficha: FichaJugadorDto) => void;
  alCancelar: () => void;
}

/**
 * El único diálogo de edición de la identidad del jugador, que abre el lápiz de la tarjeta
 * "Identidad" (spec 012, RF-036 a RF-038). Reúne nombres, apellidos y fecha de nacimiento (para
 * quien la API dice que puede corregir la identidad: el presidente y la familia) y tipo y número
 * de documento (para quien puede cambiar la ficha); lo que la persona no puede editar se ve como
 * texto. Al guardar envía solo lo que cambió y primero el documento, que es lo que puede fallar por
 * duplicado o por ser de otra cuenta (§10): si falla, el error sale en su campo y no se envía lo
 * demás, de modo que un fallo no deja nada a medias. Corregir la fecha no cambia de categoría a
 * quien ya tiene una. La categoría y los equipos no se editan aquí (§11).
 */
export function DialogoEditarIdentidad({ ficha, rutaDeLaFicha, alGuardar, alActualizar, alCancelar }: Props) {
  const [actual, setActual] = useState(ficha);
  const [datos, setDatos] = useState<CorregirIdentidadDto>({
    nombres: ficha.nombres,
    apellidos: ficha.apellidos,
    fechaNacimiento: ficha.fechaNacimiento,
  });
  const [tipo, setTipo] = useState<string>(ficha.tipoDocumento);
  const [numero, setNumero] = useState(ficha.numeroDocumento);
  const [errores, setErrores] = useState<Record<string, string[]>>({});
  const [error, setError] = useState<string>();
  const [enviando, setEnviando] = useState(false);
  // La familia corrige sin que cambie la categoría (spec 010, RF-015).
  const porLaFamilia = useClub().club.miRol === 'JUGADOR';
  // El día de hoy en UTC, que es con el que compara la API.
  const hoy = new Date().toISOString().slice(0, 10);
  const puedeIdentidad = ficha.permisos.puedeCorregirIdentidad;
  const puedeDocumento = ficha.permisos.puedeCambiar;

  const cambioDocumento =
    puedeDocumento && (tipo !== actual.tipoDocumento || numero.trim() !== actual.numeroDocumento);
  const cambioIdentidad =
    puedeIdentidad &&
    (datos.nombres !== actual.nombres ||
      datos.apellidos !== actual.apellidos ||
      datos.fechaNacimiento !== actual.fechaNacimiento);
  const incompleto =
    (puedeIdentidad && (datos.nombres.trim() === '' || datos.apellidos.trim() === '' || datos.fechaNacimiento === '')) ||
    (puedeDocumento && numero.trim() === '');

  const campo = (nombre: keyof CorregirIdentidadDto) => ({
    valor: datos[nombre],
    error: errores[nombre]?.[0],
    alCambiar: (valor: string) => setDatos((previos) => ({ ...previos, [nombre]: valor })),
  });

  async function guardar() {
    setError(undefined);
    setErrores({});
    setEnviando(true);
    let vigente = actual;
    let guardoDocumento = false;
    try {
      if (cambioDocumento) {
        const cuerpo: CambiarDocumentoIdentidadDto = { tipoDocumento: tipo as TipoDocumento, numeroDocumento: numero };
        vigente = await api.put<FichaJugadorDto>(`${rutaDeLaFicha}/documento-identidad`, cuerpo);
        guardoDocumento = true;
        setActual(vigente);
        alActualizar(vigente);
      }

      if (cambioIdentidad) {
        vigente = await api.put<FichaJugadorDto>(`${rutaDeLaFicha}/identidad`, datos);
      }

      alGuardar(vigente, { documento: guardoDocumento, identidad: cambioIdentidad });
    } catch (fallo) {
      if (fallo instanceof ErrorApi && fallo.codigo === 'datos_invalidos') {
        setErrores(fallo.errores);
      } else {
        setError(`${mensajeDe(fallo)} ${guardoDocumento ? 'El documento sí se guardó.' : 'No se cambió nada.'}`);
      }
    } finally {
      setEnviando(false);
    }
  }

  return (
    <DialogoConfirmacion
      abierto
      titulo="Editar identidad"
      textoConfirmar="Guardar cambios"
      deshabilitado={incompleto || (!cambioDocumento && !cambioIdentidad)}
      cargando={enviando}
      error={error}
      alConfirmar={() => void guardar()}
      alCancelar={alCancelar}
    >
      {puedeIdentidad && (
        <p className="texto-suave">
          {porLaFamilia
            ? 'Corregir la fecha de nacimiento no cambia su categoría.'
            : actual.categoriaAnio !== null
              ? `Si cambias el año de nacimiento, sigue en la categoría ${actual.categoriaAnio}.`
              : actual.retirado
                ? 'Está retirado: corregir la fecha no lo ubica en ninguna categoría.'
                : 'No tiene categoría: si existe la de su año de nacimiento, entra en ella al guardar.'}
        </p>
      )}
      {puedeDocumento && (
        <p className="texto-suave">
          Sigue siendo el mismo jugador: conserva su categoría, sus equipos y su ficha. Si cambias el documento,
          para iniciar sesión con él se usa el número nuevo.
        </p>
      )}
      {puedeIdentidad ? (
        <>
          <Campo etiqueta="Nombres" maxLength={80} required {...campo('nombres')} />
          <Campo etiqueta="Apellidos" maxLength={80} required {...campo('apellidos')} />
          <Campo etiqueta="Fecha de nacimiento" type="date" max={hoy} required {...campo('fechaNacimiento')} />
        </>
      ) : (
        <dl className="ficha-datos">
          <DatoDeSoloLectura etiqueta="Nombres" valor={actual.nombres} />
          <DatoDeSoloLectura etiqueta="Apellidos" valor={actual.apellidos} />
          <DatoDeSoloLectura etiqueta="Fecha de nacimiento" valor={dia(actual.fechaNacimiento)} />
        </dl>
      )}
      {puedeDocumento ? (
        <>
          <Campo
            etiqueta="Tipo de documento"
            valor={tipo}
            alCambiar={setTipo}
            error={errores.tipoDocumento?.[0]}
            opciones={TIPOS.map((valor) => ({ valor, texto: nombreDeTipoDocumento(valor) }))}
          />
          <Campo
            etiqueta="Número de documento"
            valor={numero}
            alCambiar={setNumero}
            error={errores.numeroDocumento?.[0]}
            maxLength={30}
            inputMode="numeric"
            required
          />
        </>
      ) : (
        <dl className="ficha-datos">
          <DatoDeSoloLectura etiqueta="Tipo de documento" valor={nombreDeTipoDocumento(actual.tipoDocumento)} />
          <DatoDeSoloLectura etiqueta="Número de documento" valor={actual.numeroDocumento} />
        </dl>
      )}
    </DialogoConfirmacion>
  );
}

function DatoDeSoloLectura({ etiqueta, valor }: { etiqueta: string; valor: string }) {
  return (
    <div>
      <dt>{etiqueta}</dt>
      <dd>{valor}</dd>
    </div>
  );
}
