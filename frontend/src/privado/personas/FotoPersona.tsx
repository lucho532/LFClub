import { useState } from 'react';
import { FotoAmpliable } from '../../compartido/componentes/FotoAmpliable';
import { inicialesDe } from '../../compartido/componentes/Avatar';
import { useFotoEnMemoria } from '../../compartido/fotos/fotosEnMemoria';

interface Props {
  clubId: string;
  persona: { usuarioRolId: string; nombres: string; apellidos: string; versionFoto: number };
  tamano: 'miniatura' | 'grande';
}

/**
 * Foto de perfil de un presidente, directivo o entrenador ante su club (spec 008) o, si no tiene,
 * si todavía no llegó o si falla, sus iniciales. Se pide con la sesión a la ruta del club y se guarda
 * en memoria por su `versionFoto`: con versión 0 no hace ninguna petición. La grande se abre en
 * tamaño completo al tocarla. Solo dibuja: la foto se carga, se cambia y se quita en "Mi perfil" de
 * su dueño.
 */
export function FotoPersona({ clubId, persona, tamano }: Props) {
  const foto = useFotoEnMemoria(
    `/api/clubes/${clubId}/personas/${persona.usuarioRolId}/foto`,
    persona.versionFoto,
  );
  const [fallida, setFallida] = useState<string | null>(null);
  const clase = `distintivo foto-persona-${tamano}`;

  if (foto && foto !== fallida && tamano === 'grande') {
    return (
      <FotoAmpliable
        className={clase}
        src={foto}
        alt={`Foto de ${persona.nombres} ${persona.apellidos}`}
        onError={() => setFallida(foto)}
      />
    );
  }

  if (foto && foto !== fallida) {
    return (
      <img
        className={clase}
        src={foto}
        alt=""
        onError={() => setFallida(foto)}
      />
    );
  }

  return (
    <span className={clase} aria-hidden="true">
      {inicialesDe(persona.nombres, persona.apellidos)}
    </span>
  );
}
