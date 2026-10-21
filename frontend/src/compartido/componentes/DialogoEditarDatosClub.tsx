import { useState } from 'react';
import { ErrorApi, mensajeDe } from '../api/errores';
import type { ActualizarConfiguracionClubDto } from '../api/tipos';
import { Campo } from './Campo';
import { DialogoConfirmacion } from './DialogoConfirmacion';

type Campos = Record<keyof ActualizarConfiguracionClubDto, string>;

interface Props {
  /** Datos actuales del club. */
  inicial: ActualizarConfiguracionClubDto;
  /** Envía los datos a la API; si falla, debe lanzar el error. */
  alGuardar: (datos: ActualizarConfiguracionClubDto) => Promise<void>;
  alCancelar: () => void;
}

/**
 * Diálogo que abre el lápiz de la tarjeta "Datos del club": nombre, sede, dirección y contacto. Lo
 * usan el presidente, sobre su club, y el desarrollador, desde su panel. Al guardar con éxito, quien
 * lo abre lo cierra; si la API rechaza un campo, el error sale en ese campo.
 */
export function DialogoEditarDatosClub({ inicial, alGuardar, alCancelar }: Props) {
  const [datos, setDatos] = useState<Campos>({
    nombre: inicial.nombre,
    sede: inicial.sede ?? '',
    direccion: inicial.direccion ?? '',
    correoContacto: inicial.correoContacto ?? '',
    telefonoContacto: inicial.telefonoContacto ?? '',
  });
  const [errores, setErrores] = useState<Record<string, string[]>>({});
  const [error, setError] = useState<string>();
  const [enviando, setEnviando] = useState(false);

  const campo = (nombre: keyof Campos) => ({
    valor: datos[nombre],
    error: errores[nombre]?.[0],
    alCambiar: (valor: string) => setDatos((previos) => ({ ...previos, [nombre]: valor })),
  });

  async function guardar() {
    setError(undefined);
    setErrores({});
    setEnviando(true);
    try {
      await alGuardar({
        nombre: datos.nombre,
        sede: datos.sede || null,
        direccion: datos.direccion || null,
        correoContacto: datos.correoContacto || null,
        telefonoContacto: datos.telefonoContacto || null,
      });
    } catch (fallo) {
      if (fallo instanceof ErrorApi && fallo.codigo === 'datos_invalidos') {
        setErrores(fallo.errores);
      } else if (fallo instanceof ErrorApi && fallo.codigo === 'nombre_de_club_repetido') {
        setErrores({ nombre: [fallo.title] });
      } else {
        setError(mensajeDe(fallo));
      }
    } finally {
      setEnviando(false);
    }
  }

  return (
    <DialogoConfirmacion
      abierto
      titulo="Editar datos del club"
      textoConfirmar="Guardar datos"
      deshabilitado={datos.nombre.trim() === ''}
      cargando={enviando}
      error={error}
      alConfirmar={() => void guardar()}
      alCancelar={alCancelar}
    >
      <Campo etiqueta="Nombre del club" maxLength={120} required {...campo('nombre')} />
      <Campo etiqueta="Sede" maxLength={120} {...campo('sede')} />
      <Campo etiqueta="Dirección" maxLength={200} {...campo('direccion')} />
      <Campo etiqueta="Correo de contacto" type="email" {...campo('correoContacto')} />
      <Campo etiqueta="Teléfono de contacto" type="tel" maxLength={20} {...campo('telefonoContacto')} />
    </DialogoConfirmacion>
  );
}
