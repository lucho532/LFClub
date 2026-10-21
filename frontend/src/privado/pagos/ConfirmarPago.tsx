import { useState, type FormEvent } from 'react';
import { ErrorApi, mensajeDe } from '../../compartido/api/errores';
import type { PagoEnRevisionDto } from '../../compartido/api/tiposPagos';
import { Aviso } from '../../compartido/componentes/Aviso';
import { Boton } from '../../compartido/componentes/Boton';
import { Campo } from '../../compartido/componentes/Campo';
import { apiRevision } from './apiRevision';
import { formatoMoneda, leerPesos } from './formatoMoneda';
import { SelectorDeCargos } from './SelectorDeCargos';

interface Props {
  clubId: string;
  revision: PagoEnRevisionDto;
  alTerminar: () => void;
  alCancelar: () => void;
}

/**
 * Confirma o rechaza una transferencia informada (spec 015, RF-022). Confirmar deja corregir el valor y
 * elegir los cargos que cubre (sin elegir, los más antiguos primero); al hacerlo el pago se aplica, tiene
 * recibo y el jugador cambia de estado. Rechazar exige un motivo, que la familia ve. Solo el PRESIDENTE.
 */
export function ConfirmarPago({ clubId, revision, alTerminar, alCancelar }: Props) {
  const { pago, cargosPendientes } = revision;
  const [valor, setValor] = useState(String(pago.valor));
  const [cargoIds, setCargoIds] = useState<string[]>([]);
  const [rechazando, setRechazando] = useState(false);
  const [motivo, setMotivo] = useState('');
  const [errorDelCampo, setErrorDelCampo] = useState<string | undefined>();
  const [error, setError] = useState<string | null>(null);
  const [enviando, setEnviando] = useState(false);

  async function ejecutar(accion: () => Promise<unknown>) {
    setError(null);
    setEnviando(true);
    try {
      await accion();
      alTerminar();
    } catch (fallo) {
      if (fallo instanceof ErrorApi && fallo.errorDe('motivo')) {
        setErrorDelCampo(fallo.errorDe('motivo'));
      } else {
        setError(mensajeDe(fallo));
      }
    } finally {
      setEnviando(false);
    }
  }

  function confirmar(evento: FormEvent) {
    evento.preventDefault();
    const pesos = leerPesos(valor);
    if (pesos === null) {
      setErrorDelCampo('Escribe un valor entero de pesos, mayor que cero.');
      return;
    }

    setErrorDelCampo(undefined);
    void ejecutar(() =>
      apiRevision.confirmar(clubId, pago.id, {
        valor: pesos === pago.valor ? null : pesos,
        cargoIds: cargoIds.length > 0 ? cargoIds : null,
      }),
    );
  }

  function rechazar(evento: FormEvent) {
    evento.preventDefault();
    if (!motivo.trim()) {
      setErrorDelCampo('El motivo es obligatorio: la familia lo ve.');
      return;
    }

    setErrorDelCampo(undefined);
    void ejecutar(() => apiRevision.rechazar(clubId, pago.id, motivo.trim()));
  }

  if (rechazando) {
    return (
      <form className="columna" onSubmit={rechazar} noValidate>
        {error && <Aviso tono="error">{error}</Aviso>}
        <Campo
          etiqueta="Motivo del rechazo"
          valor={motivo}
          alCambiar={setMotivo}
          lineas={3}
          maxLength={300}
          error={errorDelCampo}
          ayuda="La familia ve este motivo y puede informar una transferencia nueva."
        />
        <div className="fila">
          <Boton type="submit" variante="peligro" cargando={enviando} textoCargando="Rechazando…">
            Rechazar pago
          </Boton>
          <Boton variante="secundario" onClick={() => setRechazando(false)} disabled={enviando}>
            Volver
          </Boton>
        </div>
      </form>
    );
  }

  return (
    <form className="columna" onSubmit={confirmar} noValidate>
      {error && <Aviso tono="error">{error}</Aviso>}
      <Campo
        etiqueta="Valor a confirmar"
        inputMode="numeric"
        valor={valor}
        alCambiar={(nuevo) => {
          setValor(nuevo);
          setErrorDelCampo(undefined);
        }}
        error={errorDelCampo}
        ayuda={`La familia informó ${formatoMoneda(pago.valor)}. Puede corregirlo si lo recibido es distinto.`}
      />
      <SelectorDeCargos cargos={cargosPendientes} elegidos={cargoIds} alCambiar={setCargoIds} />
      <div className="fila">
        <Boton type="submit" cargando={enviando} textoCargando="Confirmando…">
          Confirmar pago
        </Boton>
        <Boton variante="secundario" onClick={() => setRechazando(true)} disabled={enviando}>
          Rechazar
        </Boton>
        <Boton variante="texto" onClick={alCancelar} disabled={enviando}>
          Cerrar
        </Boton>
      </div>
    </form>
  );
}
