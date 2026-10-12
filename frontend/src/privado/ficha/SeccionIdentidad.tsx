import { useState } from 'react';
import type { FichaJugadorDto } from '../../compartido/api/tiposFicha';
import { Aviso } from '../../compartido/componentes/Aviso';
import { EtiquetaEstado } from '../../compartido/componentes/EtiquetaEstado';
import { IconoLapiz } from '../../compartido/componentes/Iconos';
import { dia, nombreDeTipoDocumento } from '../../compartido/formato';
import { DialogoEditarIdentidad, type CambiosGuardados } from './DialogoEditarIdentidad';
import { GrupoDeDatos } from './GrupoDeDatos';

interface Props {
  ficha: FichaJugadorDto;
  /** Dirección de la ficha en la API. */
  rutaDeLaFicha: string;
  /** Recibe la ficha tal como la devolvió la API tras editar la identidad. */
  alCambiar: (ficha: FichaJugadorDto) => void;
}

/** Lo que se le dice a quien editó, según lo que se guardó. */
function mensajeDe(nueva: FichaJugadorDto, anterior: FichaJugadorDto, cambios: CambiosGuardados): string {
  if (cambios.documento && cambios.identidad) {
    return 'Cambios guardados. Desde ahora, para iniciar sesión con el documento se usa el número nuevo.';
  }

  if (cambios.documento) {
    return 'Documento guardado. Desde ahora, para iniciar sesión con el documento se usa el número nuevo.';
  }

  return nueva.categoriaAnio !== null && anterior.categoriaAnio === null
    ? `Identidad corregida. El jugador quedó en la categoría ${nueva.categoriaAnio}.`
    : 'Identidad corregida.';
}

/**
 * Identidad del jugador en la ficha: nombres, apellidos, documento, fecha de nacimiento, categoría
 * y equipos. Se muestra siempre como texto. Quien puede cambiar el documento o corregir la
 * identidad ve un solo lápiz en la esquina de la tarjeta, que abre un único diálogo con todos esos
 * campos (spec 012, RF-036 a RF-038); quien no puede ninguna de las dos cosas ve la tarjeta sin
 * control de edición. La categoría y los equipos son solo de vista: no los toca el lápiz (§11).
 * Que no tiene categoría o que está retirado se dice con texto, no solo con color.
 */
export function SeccionIdentidad({ ficha, rutaDeLaFicha, alCambiar }: Props) {
  const [abierto, setAbierto] = useState(false);
  const [hecho, setHecho] = useState<string | null>(null);

  const categoria = ficha.retirado ? (
    <EtiquetaEstado tono="neutro" texto="Retirado" />
  ) : ficha.categoriaAnio === null ? (
    <EtiquetaEstado tono="aviso" texto="Sin categoría" />
  ) : (
    `Categoría ${ficha.categoriaAnio}`
  );

  const acciones = (ficha.permisos.puedeCambiar || ficha.permisos.puedeCorregirIdentidad) && (
    <button
      type="button"
      className="lapiz-tarjeta"
      aria-label="Editar identidad"
      title="Editar identidad"
      onClick={() => setAbierto(true)}
    >
      <IconoLapiz />
    </button>
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
      {abierto && (
        <DialogoEditarIdentidad
          ficha={ficha}
          rutaDeLaFicha={rutaDeLaFicha}
          alCancelar={() => setAbierto(false)}
          alActualizar={alCambiar}
          alGuardar={(nueva, cambios) => {
            alCambiar(nueva);
            setAbierto(false);
            setHecho(mensajeDe(nueva, ficha, cambios));
          }}
        />
      )}
    </GrupoDeDatos>
  );
}
