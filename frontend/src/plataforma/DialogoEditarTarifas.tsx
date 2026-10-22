import { useState } from 'react';
import { ErrorApi, mensajeDe } from '../compartido/api/errores';
import type { GuardarTarifasDto, TarifasDto } from '../compartido/api/tiposPagoPlataforma';
import { Aviso } from '../compartido/componentes/Aviso';
import { Campo } from '../compartido/componentes/Campo';
import { DialogoConfirmacion } from '../compartido/componentes/DialogoConfirmacion';

type Precio = 'precioJugador' | 'precioEntrenador' | 'precioDirectivo' | 'precioPresidente';

const PRECIOS: { campo: Precio; etiqueta: string }[] = [
  { campo: 'precioJugador', etiqueta: 'Precio por jugador' },
  { campo: 'precioEntrenador', etiqueta: 'Precio por entrenador' },
  { campo: 'precioDirectivo', etiqueta: 'Precio por directivo' },
  { campo: 'precioPresidente', etiqueta: 'Precio por presidente' },
];

/** Un precio escrito por la persona: vacío es «sin definir» y 0 es válido; otra cosa no es un entero de pesos. */
function leerPrecio(texto: string): number | null | 'invalido' {
  const limpio = texto.replace(/[\s.$]/g, '');
  if (limpio === '') {
    return null;
  }

  return /^\d+$/.test(limpio) ? Number(limpio) : 'invalido';
}

const comoTexto = (valor: number | null) => (valor === null ? '' : String(valor));

interface Props {
  /** Las tarifas vigentes. */
  inicial: TarifasDto;
  /** Envía las tarifas a la API; si falla, debe lanzar el error. */
  alGuardar: (datos: GuardarTarifasDto) => Promise<void>;
  alCancelar: () => void;
}

/**
 * Diálogo que abre el lápiz de la tarjeta de tarifas (spec 017): los cuatro precios por rol y los días de
 * gracia. Valida en el cliente que cada precio sea un entero de pesos (o vacío) y los días de 0 a 30; los
 * errores que devuelve la API salen en su campo. Quien lo abre lo cierra al guardar con éxito.
 */
export function DialogoEditarTarifas({ inicial, alGuardar, alCancelar }: Props) {
  const [textos, setTextos] = useState<Record<Precio, string>>({
    precioJugador: comoTexto(inicial.precioJugador),
    precioEntrenador: comoTexto(inicial.precioEntrenador),
    precioDirectivo: comoTexto(inicial.precioDirectivo),
    precioPresidente: comoTexto(inicial.precioPresidente),
  });
  const [dias, setDias] = useState(String(inicial.diasDeGracia));
  const [errores, setErrores] = useState<Record<string, string>>({});
  const [error, setError] = useState<string>();
  const [enviando, setEnviando] = useState(false);

  async function guardar() {
    setError(undefined);
    const nuevosErrores: Record<string, string> = {};
    const datos: GuardarTarifasDto = {
      precioJugador: null,
      precioEntrenador: null,
      precioDirectivo: null,
      precioPresidente: null,
      diasDeGracia: 0,
    };

    for (const { campo } of PRECIOS) {
      const precio = leerPrecio(textos[campo]);
      if (precio === 'invalido') {
        nuevosErrores[campo] = 'Escribe un valor entero de pesos, o déjalo vacío si no está definido.';
      } else {
        datos[campo] = precio;
      }
    }

    const diasLeidos = /^\d+$/.test(dias.trim()) ? Number(dias) : Number.NaN;
    if (Number.isNaN(diasLeidos) || diasLeidos > 30) {
      nuevosErrores.diasDeGracia = 'Los días de gracia van de 0 a 30.';
    }
    datos.diasDeGracia = diasLeidos;

    setErrores(nuevosErrores);
    if (Object.keys(nuevosErrores).length > 0) {
      return;
    }

    setEnviando(true);
    try {
      await alGuardar(datos);
    } catch (fallo) {
      if (fallo instanceof ErrorApi && Object.keys(fallo.errores).length > 0) {
        setErrores(Object.fromEntries(Object.entries(fallo.errores).map(([campo, mensajes]) => [campo, mensajes[0]])));
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
      titulo="Editar tarifas"
      textoConfirmar="Guardar tarifas"
      cargando={enviando}
      error={error}
      alConfirmar={() => void guardar()}
      alCancelar={alCancelar}
    >
      <Aviso tono="info">
        Los cambios aplican a los cobros que se generen desde ahora: los ya generados conservan su valor.
      </Aviso>
      {PRECIOS.map(({ campo, etiqueta }) => (
        <Campo
          key={campo}
          etiqueta={etiqueta}
          inputMode="numeric"
          valor={textos[campo]}
          alCambiar={(nuevo) => setTextos((previos) => ({ ...previos, [campo]: nuevo }))}
          error={errores[campo]}
          ayuda="Pesos por integrante al mes. Vacío = sin definir."
        />
      ))}
      <Campo
        etiqueta="Días de gracia"
        inputMode="numeric"
        valor={dias}
        alCambiar={setDias}
        error={errores.diasDeGracia}
        ayuda="Días que el club puede tardar en pagar antes de suspenderse (0 a 30)."
      />
    </DialogoConfirmacion>
  );
}
