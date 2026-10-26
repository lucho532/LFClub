import { useState } from 'react';
import { ErrorApi, mensajeDe } from '../compartido/api/errores';
import type { GuardarTarifasDto, RangoTarifaDto, TarifasDto } from '../compartido/api/tiposPagoPlataforma';
import { Aviso } from '../compartido/componentes/Aviso';
import { Boton } from '../compartido/componentes/Boton';
import { Campo } from '../compartido/componentes/Campo';
import { DialogoConfirmacion } from '../compartido/componentes/DialogoConfirmacion';

/** Cuántos rangos admite la API. */
const MAXIMO_DE_RANGOS = 10;

/** Los rangos con los que se puede arrancar; la persona los ajusta antes de guardar. */
const RANGOS_SUGERIDOS: RangoTarifaDto[] = [
  { desde: 1, hasta: 20, precio: 25000 },
  { desde: 21, hasta: 40, precio: 45000 },
  { desde: 41, hasta: 80, precio: 75000 },
  { desde: 81, hasta: null, precio: 110000 },
];

/** Una fila del formulario: hasta cuántos jugadores llega el rango y su precio, como texto. */
interface Fila {
  hasta: string;
  precio: string;
}

const aFilas = (rangos: RangoTarifaDto[]): Fila[] =>
  rangos.map((rango) => ({ hasta: rango.hasta === null ? '' : String(rango.hasta), precio: String(rango.precio) }));

/** Un entero escrito por la persona, con o sin puntos y signo de pesos; vacío o inválido es `null`. */
function leerEntero(texto: string): number | null {
  const limpio = texto.replace(/[\s.$]/g, '');
  return /^\d+$/.test(limpio) ? Number(limpio) : null;
}

interface Props {
  /** Las tarifas vigentes. */
  inicial: TarifasDto;
  /** Envía las tarifas a la API; si falla, debe lanzar el error. */
  alGuardar: (datos: GuardarTarifasDto) => Promise<void>;
  alCancelar: () => void;
}

/**
 * Diálogo que abre el lápiz de la tarjeta de tarifas (spec 017): los rangos de jugadores con su precio
 * mensual fijo y los días de gracia. Cada rango empieza donde termina el anterior, así que solo se escribe
 * hasta dónde llega y cuánto cuesta; el último no tiene límite. Valida en el cliente que los números sean
 * enteros y que cada rango termine después de empezar; lo demás lo valida la API y su mensaje sale en
 * pantalla. Quien lo abre lo cierra al guardar con éxito.
 */
export function DialogoEditarTarifas({ inicial, alGuardar, alCancelar }: Props) {
  const [filas, setFilas] = useState<Fila[]>(aFilas(inicial.rangos));
  const [dias, setDias] = useState(String(inicial.diasDeGracia));
  const [errores, setErrores] = useState<Record<string, string>>({});
  const [error, setError] = useState<string>();
  const [enviando, setEnviando] = useState(false);

  const cambiarFila = (indice: number, cambios: Partial<Fila>) =>
    setFilas((previas) => previas.map((fila, i) => (i === indice ? { ...fila, ...cambios } : fila)));

  /** Dónde empieza el rango: 1 el primero, y uno más que el final del anterior. */
  const desdeDe = (indice: number): number | null => {
    if (indice === 0) {
      return 1;
    }

    const anterior = leerEntero(filas[indice - 1].hasta);
    return anterior === null ? null : anterior + 1;
  };

  async function guardar() {
    setError(undefined);
    const nuevosErrores: Record<string, string> = {};
    const rangos: RangoTarifaDto[] = [];

    filas.forEach((fila, indice) => {
      const esElUltimo = indice === filas.length - 1;
      const desde = desdeDe(indice);
      const hasta = esElUltimo ? null : leerEntero(fila.hasta);
      const precio = leerEntero(fila.precio);

      if (!esElUltimo && (hasta === null || (desde !== null && hasta < desde))) {
        nuevosErrores[`hasta${indice}`] = 'Escribe hasta cuántos jugadores llega, igual o mayor que donde empieza.';
      }

      if (precio === null) {
        nuevosErrores[`precio${indice}`] = 'Escribe un valor entero de pesos.';
      }

      if (desde !== null && precio !== null && (esElUltimo || hasta !== null)) {
        rangos.push({ desde, hasta, precio });
      }
    });

    const diasLeidos = /^\d+$/.test(dias.trim()) ? Number(dias) : Number.NaN;
    if (Number.isNaN(diasLeidos) || diasLeidos > 30) {
      nuevosErrores.diasDeGracia = 'Los días de gracia van de 0 a 30.';
    }

    setErrores(nuevosErrores);
    if (Object.keys(nuevosErrores).length > 0) {
      return;
    }

    setEnviando(true);
    try {
      await alGuardar({ rangos, diasDeGracia: diasLeidos });
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
      {errores.rangos && <Aviso tono="error">{errores.rangos}</Aviso>}
      {filas.map((fila, indice) => {
        const esElUltimo = indice === filas.length - 1;
        const desde = desdeDe(indice);
        return (
          <fieldset key={indice} className="rango-tarifa">
            <legend>
              Rango {indice + 1}: {desde === null ? 'desde…' : `desde ${desde} jugadores`}
            </legend>
            {esElUltimo ? (
              <p className="texto-suave">Sin límite: aplica a todos los clubes con más jugadores.</p>
            ) : (
              <Campo
                etiqueta={`Hasta (rango ${indice + 1})`}
                inputMode="numeric"
                valor={fila.hasta}
                alCambiar={(nuevo) => cambiarFila(indice, { hasta: nuevo })}
                error={errores[`hasta${indice}`]}
                ayuda="Último número de jugadores que cubre este rango."
              />
            )}
            <Campo
              etiqueta={`Precio mensual (rango ${indice + 1})`}
              inputMode="numeric"
              valor={fila.precio}
              alCambiar={(nuevo) => cambiarFila(indice, { precio: nuevo })}
              error={errores[`precio${indice}`]}
              ayuda="Pesos al mes, fijos para todo el club."
            />
            <Boton variante="texto" onClick={() => setFilas((previas) => previas.filter((_, i) => i !== indice))}>
              Quitar rango {indice + 1}
            </Boton>
          </fieldset>
        );
      })}
      <div className="fila">
        {filas.length < MAXIMO_DE_RANGOS && (
          <Boton variante="secundario" onClick={() => setFilas((previas) => [...previas, { hasta: '', precio: '' }])}>
            Agregar rango
          </Boton>
        )}
        <Boton variante="secundario" onClick={() => setFilas(aFilas(RANGOS_SUGERIDOS))}>
          Usar rangos sugeridos
        </Boton>
      </div>
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
