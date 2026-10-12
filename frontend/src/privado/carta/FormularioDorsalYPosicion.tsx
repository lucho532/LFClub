import { useState, type FormEvent } from 'react';
import { api } from '../../compartido/api/cliente';
import { ErrorApi, mensajeDe } from '../../compartido/api/errores';
import type { CartaDto, DorsalYPosicionDto, PosicionJugador } from '../../compartido/api/tiposCarta';
import { Aviso } from '../../compartido/componentes/Aviso';
import { Boton } from '../../compartido/componentes/Boton';
import { Campo } from '../../compartido/componentes/Campo';
import { Tarjeta } from '../../compartido/componentes/Tarjeta';
import { NOMBRES_DE_POSICION, textosCarta } from './textos';

interface Props {
  rutaDeLaCarta: string;
  carta: CartaDto;
  alGuardar: (carta: CartaDto) => void;
}

/**
 * Dorsal (opcional, de 1 a 99) y posición (una de las cuatro o "Sin posición") de la carta. Lo
 * pintan quien entrena su categoría, el presidente y la familia, según `permisos`; los rechazos los
 * decide la API y se muestran tal como los explica.
 */
export function FormularioDorsalYPosicion({ rutaDeLaCarta, carta, alGuardar }: Props) {
  const [dorsal, setDorsal] = useState(carta.dorsal === undefined ? '' : String(carta.dorsal));
  const [posicion, setPosicion] = useState<string>(carta.posicion ?? '');
  const [fallo, setFallo] = useState<unknown>(null);
  const [hecho, setHecho] = useState(false);
  const [enviando, setEnviando] = useState(false);

  async function guardar(evento: FormEvent) {
    evento.preventDefault();
    setFallo(null);
    setHecho(false);
    setEnviando(true);
    const datos: DorsalYPosicionDto = {
      dorsal: dorsal.trim() === '' ? null : Number(dorsal),
      posicion: posicion === '' ? null : (posicion as PosicionJugador),
    };

    try {
      alGuardar(await api.put<CartaDto>(`${rutaDeLaCarta}/dorsal-y-posicion`, datos));
      setHecho(true);
    } catch (error) {
      setFallo(error);
    } finally {
      setEnviando(false);
    }
  }

  const errorDelDorsal = fallo instanceof ErrorApi ? fallo.errorDe('dorsal') : undefined;
  const opciones = [
    { valor: '', texto: textosCarta.sinPosicion },
    ...Object.entries(NOMBRES_DE_POSICION).map(([valor, texto]) => ({ valor, texto })),
  ];

  return (
    <Tarjeta titulo={textosCarta.dorsalYPosicion}>
      <form className="columna" onSubmit={(evento) => void guardar(evento)} noValidate>
        <div className="rejilla">
          <Campo etiqueta="Dorsal" type="number" min={1} max={99} valor={dorsal} alCambiar={setDorsal} error={errorDelDorsal} />
          <Campo etiqueta="Posición" valor={posicion} alCambiar={setPosicion} opciones={opciones} />
        </div>
        {hecho && <Aviso tono="exito">{textosCarta.guardado}</Aviso>}
        {fallo !== null && !errorDelDorsal && <Aviso tono="error">{mensajeDe(fallo)}</Aviso>}
        <div className="fila">
          <Boton type="submit" cargando={enviando} textoCargando="Guardando…">
            Guardar dorsal y posición
          </Boton>
        </div>
      </form>
    </Tarjeta>
  );
}
