import { useState, type FormEvent } from 'react';
import { ErrorApi, mensajeDe } from '../../compartido/api/errores';
import type { GuardarMedioPagoDto, MedioPagoDto } from '../../compartido/api/tiposPagos';
import { Aviso } from '../../compartido/componentes/Aviso';
import { Boton } from '../../compartido/componentes/Boton';
import { Campo } from '../../compartido/componentes/Campo';

interface Props {
  /** El medio que se edita; nulo para uno nuevo. */
  inicial: MedioPagoDto | null;
  /** Envía los datos a la API; si falla, debe lanzar el error. */
  alGuardar: (datos: GuardarMedioPagoDto) => Promise<void>;
  alCancelar: () => void;
}

/**
 * Formulario de un medio de pago del club (spec 015, RF-027): la entidad, el titular, el número y una
 * nota opcional, y si las familias lo ven. Solo lo usa el PRESIDENTE.
 */
export function FormularioMedioDePago({ inicial, alGuardar, alCancelar }: Props) {
  const [entidad, setEntidad] = useState(inicial?.entidad ?? '');
  const [titular, setTitular] = useState(inicial?.titular ?? '');
  const [numero, setNumero] = useState(inicial?.numero ?? '');
  const [nota, setNota] = useState(inicial?.nota ?? '');
  const [activo, setActivo] = useState(inicial?.activo ?? true);
  const [errores, setErrores] = useState<Record<string, string[]>>({});
  const [error, setError] = useState<string | null>(null);
  const [enviando, setEnviando] = useState(false);

  async function enviar(evento: FormEvent) {
    evento.preventDefault();
    setError(null);
    setErrores({});
    setEnviando(true);
    try {
      await alGuardar({ entidad, titular, numero, nota: nota.trim() || null, activo });
    } catch (fallo) {
      if (fallo instanceof ErrorApi && fallo.codigo === 'datos_invalidos') {
        setErrores(fallo.errores);
      } else {
        setError(mensajeDe(fallo));
      }
    } finally {
      setEnviando(false);
    }
  }

  return (
    <form className="columna" onSubmit={enviar} noValidate>
      {error && <Aviso tono="error">{error}</Aviso>}
      <div className="rejilla">
        <Campo etiqueta="Entidad" valor={entidad} alCambiar={setEntidad} maxLength={40} error={errores.entidad?.[0]} ayuda="Nequi, Bancolombia, Daviplata u otra." />
        <Campo etiqueta="Titular" valor={titular} alCambiar={setTitular} maxLength={100} error={errores.titular?.[0]} />
      </div>
      <Campo etiqueta="Número de cuenta, celular o llave" valor={numero} alCambiar={setNumero} maxLength={60} error={errores.numero?.[0]} />
      <Campo etiqueta="Nota (opcional)" valor={nota} alCambiar={setNota} maxLength={200} error={errores.nota?.[0]} ayuda="Por ejemplo: Ahorros, o poner el nombre del jugador." />
      <label className="fila">
        <input type="checkbox" checked={activo} onChange={(evento) => setActivo(evento.target.checked)} />
        <span>Mostrar a las familias</span>
      </label>
      <div className="fila">
        <Boton type="submit" cargando={enviando} textoCargando="Guardando…">
          Guardar
        </Boton>
        <Boton variante="secundario" onClick={alCancelar} disabled={enviando}>
          Cancelar
        </Boton>
      </div>
    </form>
  );
}
