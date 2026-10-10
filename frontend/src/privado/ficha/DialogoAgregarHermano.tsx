import { useState } from 'react';
import { api } from '../../compartido/api/cliente';
import { ErrorApi, mensajeDe } from '../../compartido/api/errores';
import type { TipoDocumento } from '../../compartido/api/tipos';
import type { FichaJugadorDto } from '../../compartido/api/tiposFicha';
import type { AgregarHermanoDto, HermanoAgregadoDto } from '../../compartido/api/tiposSesion';
import { Campo } from '../../compartido/componentes/Campo';
import { DialogoConfirmacion } from '../../compartido/componentes/DialogoConfirmacion';
import { esMenorDeEdad } from '../../compartido/edad';
import { nombreDeTipoDocumento } from '../../compartido/formato';
import { guardarJugadorElegido } from '../../compartido/sesion/jugadorElegido';
import { useSesion } from '../../compartido/sesion/useSesion';

const TIPOS: TipoDocumento[] = ['REGISTRO_CIVIL', 'TARJETA_IDENTIDAD', 'CEDULA_CIUDADANIA', 'CEDULA_EXTRANJERIA'];

interface Props {
  clubId: string;
  /** La ficha desde la que se agrega: la del jugador con el que continúa la familia. */
  ficha: FichaJugadorDto;
  /** Recibe al hermano tal como lo devolvió la API, ya en espera y con el aviso de otro club. */
  alAgregar: (hermano: HermanoAgregadoDto) => void;
  alCancelar: () => void;
}

/**
 * Diálogo "Agregar un hermano". Pide solo la identidad del nuevo jugador: el correo, el celular y
 * la contraseña son los de la cuenta y no se vuelven a escribir (RF-002). El nombre del responsable
 * solo se pide si la cuenta no lo tiene y la fecha escrita es de un menor de 18 años; quien decide
 * es la API. Los errores de un campo se pintan junto a él; un documento que ya está en uso, como
 * aviso. Al terminar deja elegido en este club al jugador de la ficha, porque la cuenta pasa a
 * tener varios y cada petición debe decir cuál es, y recarga la sesión para que traiga al hermano.
 */
export function DialogoAgregarHermano({ clubId, ficha, alAgregar, alCancelar }: Props) {
  const { recargar } = useSesion();
  const [datos, setDatos] = useState({
    nombres: '',
    apellidos: '',
    tipoDocumento: 'TARJETA_IDENTIDAD' as string,
    numeroDocumento: '',
    fechaNacimiento: '',
    nombreResponsable: '',
  });
  const [errores, setErrores] = useState<Record<string, string[]>>({});
  const [error, setError] = useState<string>();
  const [enviando, setEnviando] = useState(false);

  const pideResponsable = !ficha.contacto.nombreResponsable?.trim() && esMenorDeEdad(datos.fechaNacimiento);
  const cambiar = (campo: keyof typeof datos) => (valor: string) => setDatos((previos) => ({ ...previos, [campo]: valor }));

  async function agregar() {
    setError(undefined);
    setErrores({});
    setEnviando(true);
    try {
      const cuerpo: AgregarHermanoDto = {
        nombres: datos.nombres,
        apellidos: datos.apellidos,
        tipoDocumento: datos.tipoDocumento as TipoDocumento,
        numeroDocumento: datos.numeroDocumento,
        fechaNacimiento: datos.fechaNacimiento || null,
        nombreResponsable: pideResponsable ? datos.nombreResponsable : null,
      };
      const hermano = await api.post<HermanoAgregadoDto>(
        `/api/clubes/${clubId}/jugadores/${ficha.usuarioRolId}/hermanos`,
        cuerpo,
      );

      // Desde ahora la cuenta tiene varios jugadores aquí: sin esto, la siguiente petición no diría cuál.
      guardarJugadorElegido(clubId, ficha.usuarioRolId);
      await recargar();
      alAgregar(hermano);
    } catch (fallo) {
      if (fallo instanceof ErrorApi && fallo.codigo === 'datos_invalidos') {
        setErrores(fallo.errores);
      } else {
        setError(`${mensajeDe(fallo)} No se agregó a nadie.`);
      }
    } finally {
      setEnviando(false);
    }
  }

  return (
    <DialogoConfirmacion
      abierto
      titulo="Agregar un hermano"
      textoConfirmar="Agregar"
      cargando={enviando}
      error={error}
      alConfirmar={() => void agregar()}
      alCancelar={alCancelar}
    >
      <p className="texto-suave">
        Escribe los datos del nuevo jugador. Usará el mismo correo, el mismo celular y la misma contraseña de esta
        cuenta, y quedará pendiente hasta que el club apruebe su ingreso.
      </p>
      <div className="rejilla">
        <Campo
          etiqueta="Nombres"
          valor={datos.nombres}
          alCambiar={cambiar('nombres')}
          error={errores.nombres?.[0]}
          maxLength={80}
          autoComplete="off"
        />
        <Campo
          etiqueta="Apellidos"
          valor={datos.apellidos}
          alCambiar={cambiar('apellidos')}
          error={errores.apellidos?.[0]}
          maxLength={80}
          autoComplete="off"
        />
      </div>
      <div className="rejilla">
        <Campo
          etiqueta="Tipo de documento"
          valor={datos.tipoDocumento}
          alCambiar={cambiar('tipoDocumento')}
          error={errores.tipoDocumento?.[0]}
          opciones={TIPOS.map((valor) => ({ valor, texto: nombreDeTipoDocumento(valor) }))}
        />
        <Campo
          etiqueta="Número de documento"
          valor={datos.numeroDocumento}
          alCambiar={cambiar('numeroDocumento')}
          error={errores.numeroDocumento?.[0]}
          ayuda="También se podrá entrar con él."
          maxLength={30}
          autoComplete="off"
        />
      </div>
      <Campo
        etiqueta="Fecha de nacimiento"
        type="date"
        valor={datos.fechaNacimiento}
        alCambiar={cambiar('fechaNacimiento')}
        error={errores.fechaNacimiento?.[0]}
        autoComplete="off"
      />
      {pideResponsable && (
        <Campo
          etiqueta="Nombre del padre, madre o responsable"
          valor={datos.nombreResponsable}
          alCambiar={cambiar('nombreResponsable')}
          error={errores.nombreResponsable?.[0]}
          ayuda="Es obligatorio porque el nuevo jugador es menor de 18 años y la cuenta no tiene responsable."
          maxLength={160}
          autoComplete="off"
          required
        />
      )}
    </DialogoConfirmacion>
  );
}
