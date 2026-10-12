import { useState } from 'react';
import type { FichaJugadorDto } from '../../compartido/api/tiposFicha';
import { Aviso } from '../../compartido/componentes/Aviso';
import { Boton } from '../../compartido/componentes/Boton';
import { EtiquetaEstado } from '../../compartido/componentes/EtiquetaEstado';
import { dia, nombreDeTipoDocumento } from '../../compartido/formato';
import { DialogoCambiarDocumento } from './DialogoCambiarDocumento';
import { DialogoCorregirIdentidad } from './DialogoCorregirIdentidad';
import { GrupoDeDatos } from './GrupoDeDatos';

interface Props {
  ficha: FichaJugadorDto;
  /** Dirección de la ficha en la API. */
  rutaDeLaFicha: string;
  /** Recibe la ficha tal como la devolvió la API tras cambiar el documento o corregir la identidad. */
  alCambiar: (ficha: FichaJugadorDto) => void;
}

/**
 * Identidad del jugador en la ficha: nombres, apellidos, documento, fecha de nacimiento, categoría
 * y equipos. Se muestra siempre como texto; lo que se puede cambiar de ella se cambia en un diálogo:
 * "Cambiar documento", para quien puede cambiar la ficha, y "Corregir nombre y fecha", solo para
 * quien la API dice que puede: el presidente y, desde la spec 010, la familia. Que no tiene categoría o que está retirado se dice
 * con texto, no solo con color.
 */
export function SeccionIdentidad({ ficha, rutaDeLaFicha, alCambiar }: Props) {
  const [dialogo, setDialogo] = useState<'documento' | 'identidad' | null>(null);
  const [hecho, setHecho] = useState<string | null>(null);

  const categoria = ficha.retirado ? (
    <EtiquetaEstado tono="neutro" texto="Retirado" />
  ) : ficha.categoriaAnio === null ? (
    <EtiquetaEstado tono="aviso" texto="Sin categoría" />
  ) : (
    `Categoría ${ficha.categoriaAnio}`
  );

  function guardado(nueva: FichaJugadorDto, mensaje: string) {
    alCambiar(nueva);
    setDialogo(null);
    setHecho(mensaje);
  }

  const acciones = (ficha.permisos.puedeCambiar || ficha.permisos.puedeCorregirIdentidad) && (
    <span className="fila">
      {ficha.permisos.puedeCambiar && (
        <Boton variante="secundario" onClick={() => setDialogo('documento')}>
          Cambiar documento
        </Boton>
      )}
      {ficha.permisos.puedeCorregirIdentidad && (
        <Boton variante="secundario" onClick={() => setDialogo('identidad')}>
          Corregir nombre y fecha
        </Boton>
      )}
    </span>
  );

  return (
    <GrupoDeDatos
      titulo="Identidad"
      acciones={acciones}
      datos={[
        { etiqueta: 'Nombres', valor: ficha.nombres },
        { etiqueta: 'Apellidos', valor: ficha.apellidos },
        { etiqueta: 'Tipo de documento', valor: nombreDeTipoDocumento(ficha.tipoDocumento) },
        { etiqueta: 'Número de documento', valor: ficha.numeroDocumento },
        { etiqueta: 'Fecha de nacimiento', valor: dia(ficha.fechaNacimiento) },
        { etiqueta: 'Categoría', valor: categoria },
        { etiqueta: 'Equipos', valor: ficha.equipos.length > 0 ? ficha.equipos.join(', ') : 'Sin equipo' },
      ]}
    >
      {hecho && <Aviso tono="exito">{hecho}</Aviso>}
      {dialogo === 'documento' && (
        <DialogoCambiarDocumento
          ficha={ficha}
          rutaDeLaFicha={rutaDeLaFicha}
          alCancelar={() => setDialogo(null)}
          alGuardar={(nueva) =>
            guardado(nueva, 'Documento guardado. Desde ahora, para iniciar sesión con el documento se usa el número nuevo.')
          }
        />
      )}
      {dialogo === 'identidad' && (
        <DialogoCorregirIdentidad
          ficha={ficha}
          rutaDeLaFicha={rutaDeLaFicha}
          alCancelar={() => setDialogo(null)}
          alGuardar={(nueva) =>
            guardado(
              nueva,
              nueva.categoriaAnio !== null && ficha.categoriaAnio === null
                ? `Identidad corregida. El jugador quedó en la categoría ${nueva.categoriaAnio}.`
                : 'Identidad corregida.',
            )
          }
        />
      )}
    </GrupoDeDatos>
  );
}
