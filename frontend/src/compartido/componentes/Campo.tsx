import { useId, useState, type InputHTMLAttributes } from 'react';
import { IconoOjo, IconoOjoTachado } from './Iconos';

interface Opcion {
  valor: string;
  texto: string;
}

interface Props extends Omit<InputHTMLAttributes<HTMLInputElement>, 'onChange' | 'value'> {
  etiqueta: string;
  valor: string;
  alCambiar?: (valor: string) => void;
  /** Mensaje de error del campo, tal como lo devuelve la API. */
  error?: string;
  ayuda?: string;
  /** Si se indican, el campo es una lista desplegable. */
  opciones?: Opcion[];
  /** Si se indica, el campo es un área de texto con ese número de líneas a la vista. */
  lineas?: number;
}

/**
 * Etiqueta, entrada y error de un campo de formulario. Una contraseña lleva al lado un botón con un
 * ojo para verla mientras se escribe y volver a ocultarla.
 */
export function Campo({ etiqueta, valor, alCambiar, error, ayuda, opciones, lineas, ...resto }: Props) {
  const id = useId();
  const [visible, setVisible] = useState(false);
  const esContrasena = resto.type === 'password';
  const idMensaje = `${id}-mensaje`;
  const comunes = {
    id,
    value: valor,
    'aria-invalid': error ? true : undefined,
    'aria-describedby': error || ayuda ? idMensaje : undefined,
  };

  return (
    <div className="campo">
      <label htmlFor={id}>{etiqueta}</label>
      {opciones ? (
        <select
          {...comunes}
          required={resto.required}
          disabled={resto.disabled}
          onChange={(evento) => alCambiar?.(evento.target.value)}
        >
          {opciones.map((opcion) => (
            <option key={opcion.valor} value={opcion.valor}>
              {opcion.texto}
            </option>
          ))}
        </select>
      ) : lineas ? (
        <textarea
          {...comunes}
          rows={lineas}
          maxLength={resto.maxLength}
          required={resto.required}
          disabled={resto.disabled}
          onChange={(evento) => alCambiar?.(evento.target.value)}
        />
      ) : esContrasena ? (
        <div className="campo-contrasena">
          <input
            {...resto}
            {...comunes}
            type={visible ? 'text' : 'password'}
            onChange={(evento) => alCambiar?.(evento.target.value)}
          />
          <button
            type="button"
            className="boton-ver-contrasena"
            aria-label={visible ? 'Ocultar contraseña' : 'Mostrar contraseña'}
            aria-pressed={visible}
            aria-controls={id}
            onClick={() => setVisible((antes) => !antes)}
          >
            {visible ? <IconoOjoTachado /> : <IconoOjo />}
          </button>
        </div>
      ) : (
        <input {...resto} {...comunes} onChange={(evento) => alCambiar?.(evento.target.value)} />
      )}
      {error ? (
        <span id={idMensaje} className="campo-error">
          {error}
        </span>
      ) : (
        ayuda && (
          <span id={idMensaje} className="texto-suave">
            {ayuda}
          </span>
        )
      )}
    </div>
  );
}
