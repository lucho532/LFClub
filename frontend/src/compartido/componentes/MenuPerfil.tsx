import { useEffect, useRef, useState, type ReactNode } from 'react';
import { Link } from 'react-router-dom';
import { Avatar } from './Avatar';

interface Props {
  /** Nombre que se muestra junto a la foto: el de la persona o el del jugador elegido. */
  nombre: string;
  /** Segunda línea, por ejemplo el rol en el club. */
  detalle?: string;
  iniciales: string;
  alCerrarSesion: () => void;
  /** Opciones propias de la pantalla, que van entre "Mi perfil" y "Cerrar sesión". */
  opciones?: ReactNode;
  /**
   * En el teléfono el botón muestra solo la foto y el nombre pasa al panel. Con verdadero el nombre
   * se queda también en el botón: por ejemplo, cuando dice cuál de varios jugadores está elegido.
   */
  nombreSiempreVisible?: boolean;
}

/**
 * Perfil de la persona en la barra superior: su foto o sus iniciales, su nombre y su rol. Al
 * pulsarlo despliega su nombre, "Mi perfil", las opciones que pase la pantalla y "Cerrar sesión".
 * Se cierra al elegir una opción, al pulsar fuera o con Escape. No decide nada: cada opción la resuelve
 * quien la pasa.
 */
export function MenuPerfil({
  nombre,
  detalle,
  iniciales,
  alCerrarSesion,
  opciones,
  nombreSiempreVisible = false,
}: Props) {
  const [abierto, setAbierto] = useState(false);
  const contenedor = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!abierto) {
      return;
    }

    const alPulsarFuera = (evento: PointerEvent) => {
      if (!contenedor.current?.contains(evento.target as Node)) {
        setAbierto(false);
      }
    };
    const alTeclear = (evento: KeyboardEvent) => {
      if (evento.key === 'Escape') {
        setAbierto(false);
      }
    };
    document.addEventListener('pointerdown', alPulsarFuera);
    document.addEventListener('keydown', alTeclear);
    return () => {
      document.removeEventListener('pointerdown', alPulsarFuera);
      document.removeEventListener('keydown', alTeclear);
    };
  }, [abierto]);

  return (
    <div className="menu-perfil" ref={contenedor}>
      <button
        type="button"
        className={nombreSiempreVisible ? 'menu-perfil-boton menu-perfil-con-nombre' : 'menu-perfil-boton'}
        aria-label={`${nombre}${detalle ? `, ${detalle}` : ''}: abrir el menú de perfil`}
        onClick={() => setAbierto((valor) => !valor)}
        aria-haspopup="menu"
        aria-expanded={abierto}
      >
        <Avatar iniciales={iniciales} />
        <span className="menu-perfil-texto">
          <span>{nombre}</span>
          {detalle && <span className="texto-suave">{detalle}</span>}
        </span>
      </button>
      {abierto && (
        // Elegir una opción cierra el menú; tocar otra cosa del panel, como el desplegable de
        // clubes, no.
        <div
          className="menu-perfil-panel"
          role="menu"
          onClick={(evento) => {
            if ((evento.target as HTMLElement).closest('[role="menuitem"]')) {
              setAbierto(false);
            }
          }}
        >
          <div className="menu-perfil-encabezado">
            <strong>{nombre}</strong>
            {detalle && <span className="texto-suave">{detalle}</span>}
          </div>
          <Link to="/perfil" role="menuitem">
            Mi perfil
          </Link>
          {opciones}
          <button type="button" role="menuitem" onClick={alCerrarSesion}>
            Cerrar sesión
          </button>
        </div>
      )}
    </div>
  );
}
