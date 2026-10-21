import { useState, type ChangeEvent, type FormEvent } from 'react';
import type { MedioPagoDto, PagoDto } from '../../compartido/api/tiposPagos';
import { Aviso } from '../../compartido/componentes/Aviso';
import { Boton } from '../../compartido/componentes/Boton';
import { Campo } from '../../compartido/componentes/Campo';
import { hoyEnColombia } from '../../compartido/fechas';
import { formatoMoneda, leerPesos } from './formatoMoneda';
import { useInformarPago } from './useInformarPago';

interface Props {
  clubId: string;
  usuarioRolId: string;
  /** Lo que debe el jugador hoy. */
  pendiente: number;
  /** Medios activos del club, para elegir la entidad desde la que se transfirió. */
  medios: MedioPagoDto[];
  alInformar: (pago: PagoDto) => void;
  alCancelar: () => void;
}

const OTRA = 'otra';
const TAMANO_MAXIMO = 5 * 1024 * 1024;

/**
 * Formulario con el que la familia informa una transferencia (spec 015, RF-021): la entidad (de la lista
 * del club u otra), el valor, la fecha, la referencia y la imagen del comprobante, tomada con la cámara o
 * elegida de la galería. Muestra lo que se debe. El pago queda en revisión y no cambia ningún saldo hasta
 * que el presidente lo confirme.
 */
export function InformarTransferencia({ clubId, usuarioRolId, pendiente, medios, alInformar, alCancelar }: Props) {
  const { informar, enviando, resultado } = useInformarPago(clubId, usuarioRolId);
  const [seleccion, setSeleccion] = useState(medios[0]?.entidad ?? OTRA);
  const [otraEntidad, setOtraEntidad] = useState('');
  const [valor, setValor] = useState('');
  const [fecha, setFecha] = useState(hoyEnColombia());
  const [referencia, setReferencia] = useState('');
  const [archivo, setArchivo] = useState<File | null>(null);
  const [errorDelValor, setErrorDelValor] = useState<string | undefined>();
  const [errorDeLaImagen, setErrorDeLaImagen] = useState<string | undefined>();

  const entidad = seleccion === OTRA ? otraEntidad.trim() : seleccion;
  const opciones = [...medios.map((medio) => ({ valor: medio.entidad, texto: medio.entidad })), { valor: OTRA, texto: 'Otra entidad' }];

  function elegirArchivo(evento: ChangeEvent<HTMLInputElement>) {
    setErrorDeLaImagen(undefined);
    setArchivo(evento.target.files?.[0] ?? null);
  }

  async function enviar(evento: FormEvent) {
    evento.preventDefault();
    const pesos = leerPesos(valor);
    if (pesos === null) {
      setErrorDelValor('Escribe un valor entero de pesos, mayor que cero.');
      return;
    }

    if (pesos > pendiente) {
      setErrorDelValor(`El valor supera lo que se debe: pendiente ${formatoMoneda(pendiente)}.`);
      return;
    }

    if (!archivo) {
      setErrorDeLaImagen('Adjunta la imagen del comprobante.');
      return;
    }

    if (archivo.size > TAMANO_MAXIMO) {
      setErrorDeLaImagen('La imagen supera los 5 MB.');
      return;
    }

    setErrorDelValor(undefined);
    const pago = await informar({ valor: pesos, entidad, referencia: referencia.trim() || null, fecha, archivo });
    if (pago) {
      alInformar(pago);
    }
  }

  return (
    <form className="columna" onSubmit={enviar} noValidate>
      <p>
        Pendiente: <strong>{formatoMoneda(pendiente)}</strong>
      </p>
      {resultado.error && <Aviso tono="error">{resultado.error}</Aviso>}
      <Campo etiqueta="Entidad desde la que transfirió" valor={seleccion} alCambiar={setSeleccion} opciones={opciones} />
      {seleccion === OTRA && (
        <Campo
          etiqueta="Nombre de la entidad"
          valor={otraEntidad}
          alCambiar={setOtraEntidad}
          maxLength={40}
          error={resultado.errores.entidad?.[0]}
        />
      )}
      <Campo
        etiqueta="Valor transferido"
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
          etiqueta="Fecha de la transferencia"
          type="date"
          valor={fecha}
          max={hoyEnColombia()}
          alCambiar={setFecha}
          error={resultado.errores.fecha?.[0]}
        />
        <Campo
          etiqueta="Referencia (opcional)"
          valor={referencia}
          alCambiar={setReferencia}
          maxLength={60}
          error={resultado.errores.referencia?.[0]}
        />
      </div>
      <div className="campo">
        <label htmlFor="comprobante">Imagen del comprobante</label>
        <input id="comprobante" type="file" accept="image/png,image/jpeg,image/webp" onChange={elegirArchivo} />
        {errorDeLaImagen ? (
          <span className="campo-error">{errorDeLaImagen}</span>
        ) : (
          <span className="texto-suave">Tome una foto o elija una imagen de la galería (PNG, JPEG o WebP, hasta 5 MB).</span>
        )}
      </div>
      <div className="fila">
        <Boton type="submit" cargando={enviando} textoCargando="Enviando…">
          Informar transferencia
        </Boton>
        <Boton variante="secundario" onClick={alCancelar} disabled={enviando}>
          Cancelar
        </Boton>
      </div>
    </form>
  );
}
