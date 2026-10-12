import { useEffect, useRef, useState } from 'react';

interface Props {
  src: string;
  alt: string;
  className: string;
  onError?: () => void;
}

/**
 * Una foto que, al tocarla, se abre en tamaño completo sobre un velo. Se cierra tocándola de nuevo,
 * tocando fuera, con la equis, con Escape o con "Atrás": al abrirla se agrega un paso al historial,
 * para que "Atrás" (el del navegador o el del teléfono en el APK) cierre la foto y no salga de la
 * pantalla ni de la app. Solo para fotos que se ven en grande (fichas, Inicio y "Mi perfil").
 */
export function FotoAmpliable({ src, alt, className, onError }: Props) {
  const visor = useRef<HTMLDialogElement>(null);
  const [abierta, setAbierta] = useState(false);

  useEffect(() => {
    const dialogo = visor.current;
    if (!abierta || !dialogo) {
      return;
    }

    dialogo.showModal();
    // Se conserva el estado del enrutador para que, al volver, siga en la misma pantalla.
    window.history.pushState({ ...window.history.state, visorFoto: true }, '');
    const alVolver = () => setAbierta(false);
    window.addEventListener('popstate', alVolver);
    return () => {
      window.removeEventListener('popstate', alVolver);
      dialogo.close();
    };
  }, [abierta]);

  // Cerrar quita el paso que se agregó al abrir; el `popstate` que eso provoca la cierra.
  const cerrar = () => window.history.back();

  return (
    <>
      <button
        type="button"
        className="foto-ampliable"
        aria-label={`Ver en grande: ${alt}`}
        onClick={() => setAbierta(true)}
      >
        <img className={className} src={src} alt={alt} onError={onError} />
      </button>
      <dialog
        ref={visor}
        className="visor-foto"
        aria-label={alt}
        onClick={cerrar}
        onCancel={(evento) => {
          evento.preventDefault();
          cerrar();
        }}
      >
        <img src={src} alt={alt} />
        <button type="button" className="visor-foto-cerrar" aria-label="Cerrar">
          ×
        </button>
      </dialog>
    </>
  );
}
