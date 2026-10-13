import { useState, type FormEvent } from 'react';
import { useNavigate } from 'react-router-dom';
import { api } from '../compartido/api/cliente';
import { ErrorApi, mensajeDe } from '../compartido/api/errores';
import type {
  InvitacionVigenteDto,
  RegistrarConInvitacionDto,
  TipoDocumento,
  TokenSesionDto,
} from '../compartido/api/tipos';
import { Aviso } from '../compartido/componentes/Aviso';
import { Boton } from '../compartido/componentes/Boton';
import { Campo } from '../compartido/componentes/Campo';
import { esMenorDeEdad } from '../compartido/edad';
import { useSesion } from '../compartido/sesion/useSesion';

type Datos = Omit<RegistrarConInvitacionDto, 'token' | 'nombreResponsable'> & { nombreResponsable: string };

const TIPOS_DOCUMENTO: { valor: TipoDocumento; texto: string }[] = [
  { valor: 'CEDULA_CIUDADANIA', texto: 'Cédula de ciudadanía' },
  { valor: 'CEDULA_EXTRANJERIA', texto: 'Cédula de extranjería' },
  { valor: 'TARJETA_IDENTIDAD', texto: 'Tarjeta de identidad' },
  { valor: 'REGISTRO_CIVIL', texto: 'Registro civil' },
];

const VACIO: Datos = {
  nombres: '',
  apellidos: '',
  tipoDocumento: 'CEDULA_CIUDADANIA',
  numeroDocumento: '',
  fechaNacimiento: '',
  celular: '',
  nombreResponsable: '',
  contrasena: '',
};

/** Códigos de conflicto que se muestran junto al número de documento. */
const CONFLICTOS_DE_DOCUMENTO = ['documento_repetido_en_club', 'documento_en_otra_cuenta'];

interface Props {
  token: string;
  invitacion: InvitacionVigenteDto;
}

/**
 * Registro con invitación. El correo, el club y el rol vienen de la invitación y no se pueden
 * cambiar; la persona escribe sus datos y crea su propia contraseña, y al terminar entra
 * directamente al club. El nombre del responsable solo se pide, y solo se envía, cuando la
 * invitación es de jugador: es obligatorio si, por su fecha de nacimiento, quien ingresa es menor
 * de 18 años; lo comprueba la API.
 */
export function FormularioRegistro({ token, invitacion }: Props) {
  const { iniciar } = useSesion();
  const navegar = useNavigate();
  const [datos, setDatos] = useState<Datos>(VACIO);
  const [errores, setErrores] = useState<Record<string, string[]>>({});
  const [error, setError] = useState<string | null>(null);
  const [enviando, setEnviando] = useState(false);

  const cambiar = (campo: keyof Datos) => (valor: string) => setDatos((previos) => ({ ...previos, [campo]: valor }));
  const errorDe = (campo: keyof Datos) => errores[campo]?.[0];
  const esMenor = esMenorDeEdad(datos.fechaNacimiento);

  async function enviar(evento: FormEvent) {
    evento.preventDefault();
    setError(null);
    setErrores({});
    setEnviando(true);
    try {
      const cuerpo: RegistrarConInvitacionDto = {
        token,
        ...datos,
        nombreResponsable: invitacion.pideResponsable ? datos.nombreResponsable.trim() || null : null,
      };
      const sesion = await iniciar(await api.post<TokenSesionDto>('/api/invitaciones/registro', cuerpo));
      // Entra aprobado: la ruta del club muestra ya su aplicación, sin pantalla de espera.
      navegar(sesion.clubes.length > 0 ? `/club/${sesion.clubes[0].clubId}` : '/', { replace: true });
    } catch (fallo) {
      if (fallo instanceof ErrorApi && fallo.codigo === 'datos_invalidos') {
        setErrores(fallo.errores);
      } else if (fallo instanceof ErrorApi && CONFLICTOS_DE_DOCUMENTO.includes(fallo.codigo)) {
        setErrores({ numeroDocumento: [fallo.title] });
      } else {
        setError(mensajeDe(fallo));
      }

      setEnviando(false);
    }
  }

  // Cuando la invitación es de jugador, el formulario describe al futbolista y no a quien lo registra.
  const deJugador = invitacion.pideResponsable ? ' del jugador' : '';

  return (
    <form className="columna" onSubmit={enviar} noValidate>
      {error && <Aviso tono="error">{error}</Aviso>}
      {invitacion.pideResponsable && (
        <Aviso tono="info">
          Escribe los datos y el documento del jugador, no los de su familia; el correo es el de su acudiente.
        </Aviso>
      )}
      <Campo etiqueta={invitacion.pideResponsable ? 'Correo del acudiente' : 'Correo'}
        valor={invitacion.correo}
        readOnly
        ayuda="Es el correo de la invitación." />
      <div className="rejilla">
        <Campo
          etiqueta={`Nombres${deJugador}`}
          valor={datos.nombres}
          alCambiar={cambiar('nombres')}
          error={errorDe('nombres')}
          autoComplete="given-name"
          maxLength={80}
        />
        <Campo
          etiqueta={`Apellidos${deJugador}`}
          valor={datos.apellidos}
          alCambiar={cambiar('apellidos')}
          error={errorDe('apellidos')}
          autoComplete="family-name"
          maxLength={80}
        />
      </div>
      <div className="rejilla">
        <Campo
          etiqueta={`Tipo de documento${deJugador}`}
          valor={datos.tipoDocumento}
          alCambiar={cambiar('tipoDocumento')}
          error={errorDe('tipoDocumento')}
          opciones={TIPOS_DOCUMENTO}
        />
        <Campo
          etiqueta={`Número de documento${deJugador}`}
          valor={datos.numeroDocumento}
          alCambiar={cambiar('numeroDocumento')}
          error={errorDe('numeroDocumento')}
          ayuda="También podrás entrar con él."
          maxLength={30}
        />
      </div>
      <div className="rejilla">
        <Campo
          etiqueta={`Fecha de nacimiento${deJugador}`}
          type="date"
          valor={datos.fechaNacimiento}
          alCambiar={cambiar('fechaNacimiento')}
          error={errorDe('fechaNacimiento')}
          autoComplete="bday"
        />
        <Campo
          etiqueta={invitacion.pideResponsable ? 'Celular de contacto' : 'Celular'}
          type="tel"
          valor={datos.celular}
          alCambiar={cambiar('celular')}
          error={errorDe('celular')}
          autoComplete="tel"
          maxLength={20}
        />
      </div>
      {invitacion.pideResponsable && (
        <Campo
          etiqueta={`Nombre del padre, madre o responsable (${esMenor ? 'obligatorio' : 'opcional'})`}
          valor={datos.nombreResponsable}
          alCambiar={cambiar('nombreResponsable')}
          error={errorDe('nombreResponsable')}
          ayuda={
            esMenor
              ? 'Es obligatorio porque quien ingresa es menor de 18 años.'
              : 'Es obligatorio si quien ingresa es menor de 18 años.'
          }
          required={esMenor}
          autoComplete="off"
          maxLength={160}
        />
      )}
      <Campo
        etiqueta="Contraseña"
        type="password"
        valor={datos.contrasena}
        alCambiar={cambiar('contrasena')}
        error={errorDe('contrasena')}
        ayuda="Entre 8 y 128 caracteres."
        autoComplete="new-password"
      />
      <Boton type="submit" cargando={enviando} textoCargando="Creando tu cuenta…">
        Crear cuenta
      </Boton>
    </form>
  );
}
