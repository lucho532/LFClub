import { useState, type FormEvent } from 'react';
import type { CargoDto, MedioPago, PagoDto } from '../../compartido/api/tiposPagos';
import { Aviso } from '../../compartido/componentes/Aviso';
import { Boton } from '../../compartido/componentes/Boton';
import { Campo } from '../../compartido/componentes/Campo';
import { hoyEnColombia } from '../../compartido/fechas';
import { formatoMoneda, leerPesos } from './formatoMoneda';
import { SelectorDeCargos } from './SelectorDeCargos';
import { useRegistrarPago } from './useRegistrarPago';

interface Props {
  clubId: string;
  usuarioRolId: string;
  /** Cargos con algo pendiente. */
  pendientes: CargoDto[];
  /** Recibe el pago confirmado; la cuenta se refresca con él. */
  alRegistrar: (pago: PagoDto) => void;
  alCancelar: () => void;
}

const MEDIOS = [
  { valor: 'EFECTIVO', texto: 'Efectivo' },
  { valor: 'TRANSFERENCIA', texto: 'Transferencia' },
];

/**
 * Formulario con el que el PRESIDENTE registra un pago que recibió: efectivo o una transferencia que ya
 * verificó (spec 015, RF-020). Muestra lo que se debe y no deja registrar más; la API vuelve a comprobarlo.
 * La entidad solo se pide en una transferencia. Los cargos que cubre son opcionales. Al guardar, el pago
 * queda confirmado, con su recibo, y el estado de cuenta cambia en el acto.
 */
export function RegistrarPago({ clubId, usuarioRolId, pendientes, alRegistrar, alCancelar }: Props) {
  const { registrar, enviando, resultado } = useRegistrarPago(clubId, usuarioRolId);
  const debe = pendientes.reduce((suma, cargo) => suma + cargo.pendiente, 0);
  const [valor, setValor] = useState('');
  const [medio, setMedio] = useState<MedioPago>('EFECTIVO');
  const [entidad, setEntidad] = useState('');
  const [referencia, setReferencia] = useState('');
  const [fecha, setFecha] = useState(hoyEnColombia());
  const [cargoIds, setCargoIds] = useState<string[]>([]);
  const [errorDelValor, setErrorDelValor] = useState<string | undefined>();

  async function enviar(evento: FormEvent) {
    evento.preventDefault();
    const pesos = leerPesos(valor);
    if (pesos === null) {
      setErrorDelValor('Escribe un valor entero de pesos, mayor que cero.');
      return;
    }

    const aCubrir =
      cargoIds.length > 0
        ? pendientes.filter((cargo) => cargoIds.includes(cargo.id)).reduce((suma, cargo) => suma + cargo.pendiente, 0)
        : debe;
    if (pesos > aCubrir) {
      setErrorDelValor(`El pago supera lo que se debe: pendiente ${formatoMoneda(aCubrir)}.`);
      return;
    }

    setErrorDelValor(undefined);
    const pago = await registrar({
      valor: pesos,
      medio,
      entidad: medio === 'TRANSFERENCIA' ? entidad.trim() || null : null,
      referencia: referencia.trim() || null,
      fecha,
      cargoIds: cargoIds.length > 0 ? cargoIds : null,
    });
    if (pago) {
      alRegistrar(pago);
    }
  }

  return (
    <form className="columna" onSubmit={enviar} noValidate>
      <p>
        Pendiente: <strong>{formatoMoneda(debe)}</strong>
      </p>
      {resultado.error && <Aviso tono="error">{resultado.error}</Aviso>}
      <Campo
        etiqueta="Valor recibido"
        inputMode="numeric"
        valor={valor}
        alCambiar={(nuevo) => {
          setValor(nuevo);
          setErrorDelValor(undefined);
        }}
        error={errorDelValor ?? resultado.errores.valor?.[0]}
      />
      <div className="rejilla">
        <Campo
          etiqueta="Medio de pago"
          valor={medio}
          alCambiar={(nuevo) => setMedio(nuevo as MedioPago)}
          opciones={MEDIOS}
        />
        <Campo
          etiqueta="Fecha en que se pagó"
          type="date"
          valor={fecha}
          max={hoyEnColombia()}
          alCambiar={setFecha}
          error={resultado.errores.fecha?.[0]}
        />
      </div>
      {medio === 'TRANSFERENCIA' && (
        <Campo
          etiqueta="Entidad"
          valor={entidad}
          alCambiar={setEntidad}
          maxLength={40}
          error={resultado.errores.entidad?.[0]}
          ayuda="Nequi, Bancolombia, Daviplata u otra."
        />
      )}
      <Campo
        etiqueta="Referencia (opcional)"
        valor={referencia}
        alCambiar={setReferencia}
        maxLength={60}
        error={resultado.errores.referencia?.[0]}
      />
      <SelectorDeCargos cargos={pendientes} elegidos={cargoIds} alCambiar={setCargoIds} />
      <div className="fila">
        <Boton type="submit" cargando={enviando} textoCargando="Registrando…">
          Registrar pago
        </Boton>
        <Boton variante="secundario" onClick={alCancelar} disabled={enviando}>
          Cancelar
        </Boton>
      </div>
    </form>
  );
}
