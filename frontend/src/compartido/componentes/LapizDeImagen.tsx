import { useEffect, useRef, useState, type ChangeEvent, type ReactNode } from 'react';
import { IconoLapiz } from './Iconos';

const FORMATOS = 'image/png,image/jpeg,image/webp';

interface Props {
  /** La imagen (o las iniciales) que se edita, dentro de su círculo. */
  children: ReactNode;
  /** Qué se edita, en minúscula y con artículo: "la foto", "el escudo". */
  que: string;
  tieneImagen: boolean;
  ocupado: boolean;
  alElegir: (archivo: File) => void;
  /** Si se ofrece quitarla. Sin él, el lápiz abre directamente el selector de archivos. */
  alQuitar?: () => void;
}

/**
 * Un lápiz pequeño dentro del círculo de una foto o un escudo, en lugar del campo de archivo con su
 * texto. Sin imagen, o si no se puede quitar, abre el selector de archivos; con imagen que se puede
 * quitar, ofrece "Cambiar" y "Quitar". Los formatos admitidos (PNG, JPEG o WebP de hasta 1 MB) van
 * en la descripción del lápiz; si la API rechaza un archivo, quien lo usa muestra el motivo. No
 * envía nada: avisa con `alElegir` y `alQuitar`.
 */
export function LapizDeImagen({ children, que, tieneImagen, ocupado, alElegir, alQuitar }: Props) {
  const selector = useRef<HTMLInputElement>(null);
  const contenedor = useRef<HTMLDivElement>(null);
  const [menu, setMenu] = useState(false);
  const conMenu = tieneImagen && alQuitar !== undefined;
  const accion = `${tieneImagen ? 'Cambiar' : 'Cargar'} ${que}`;

  useEffect(() => {
    if (!menu) {
      return;
    }

    const cerrarFuera = (evento: MouseEvent) => {
      if (!contenedor.current?.contains(evento.target as Node)) {
        setMenu(false);
      }
    };
    const cerrarConEscape = (evento: KeyboardEvent) => evento.key === 'Escape' && setMenu(false);
    document.addEventListener('mousedown', cerrarFuera);
    document.addEventListener('keydown', cerrarConEscape);
    return () => {
      document.removeEventListener('mousedown', cerrarFuera);
      document.removeEventListener('keydown', cerrarConEscape);
    };
  }, [menu]);

  function elegido(evento: ChangeEvent<HTMLInputElement>) {
    const archivo = evento.target.files?.[0];
    evento.target.value = '';
    if (archivo) {
      alElegir(archivo);
    }
  }

  return (
    <div className="lapiz-imagen" ref={contenedor}>
      {children}
      <button
        type="button"
        className="lapiz-imagen-boton"
        aria-label={conMenu ? `Editar ${que}` : accion}
        title={`${conMenu ? `Editar ${que}` : accion}: PNG, JPEG o WebP de hasta 1 MB`}
        aria-haspopup={conMenu ? 'menu' : undefined}
        aria-expanded={conMenu ? menu : undefined}
        aria-busy={ocupado}
        disabled={ocupado}
        onClick={() => (conMenu ? setMenu(!menu) : selector.current?.click())}
      >
        {ocupado ? <span aria-hidden="true">…</span> : <IconoLapiz />}
      </button>
      {menu && (
        <div className="lapiz-imagen-menu" role="menu">
          <button
            type="button"
            role="menuitem"
            onClick={() => {
              setMenu(false);
              selector.current?.click();
            }}
          >
            {accion}
          </button>
          <button
            type="button"
            role="menuitem"
            onClick={() => {
              setMenu(false);
              alQuitar?.();
            }}
          >
            {`Quitar ${que}`}
          </button>
        </div>
      )}
      <input ref={selector} type="file" accept={FORMATOS} onChange={elegido} hidden />
    </div>
  );
}
