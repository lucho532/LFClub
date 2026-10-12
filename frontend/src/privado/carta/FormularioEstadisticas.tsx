import { useState, type FormEvent } from 'react';
import { api } from '../../compartido/api/cliente';
import { ErrorApi, mensajeDe } from '../../compartido/api/errores';
import type { CartaDto, EstadisticasCartaDto } from '../../compartido/api/tiposCarta';
import { Aviso } from '../../compartido/componentes/Aviso';
import { Boton } from '../../compartido/componentes/Boton';
import { Campo } from '../../compartido/componentes/Campo';
import { Tarjeta } from '../../compartido/componentes/Tarjeta';
import { fechaYHora } from '../../compartido/formato';
import { CAMPOS_DE_ESTADISTICAS, textosCarta } from './textos';

interface Props {
  rutaDeLaCarta: string;
  carta: CartaDto;
  alGuardar: (carta: CartaDto) => void;
}

type Campos = Record<keyof EstadisticasCartaDto, string>;

const vacias = (): Campos => ({ ritmo: '', tiro: '', pase: '', regate: '', defensa: '', fisico: '' });

/**
 * Las seis estadísticas de la carta con su sigla (RIT, TIR, PAS, REG, DEF, FÍS), "Quitar
 * estadísticas" y su último cambio. Solo lo pintan quien entrena la categoría del jugador y el
 * presidente, según `permisos`. Las seis de 1 a 99 o ninguna: lo decide la API (RF-022).
 */
export function FormularioEstadisticas({ rutaDeLaCarta, carta, alGuardar }: Props) {
  const [campos, setCampos] = useState<Campos>(() => {
    const actuales = carta.estadisticas;
    return actuales
      ? (Object.fromEntries(Object.entries(actuales).map(([clave, valor]) => [clave, String(valor)])) as Campos)
      : vacias();
  });
  const [fallo, setFallo] = useState<unknown>(null);
  const [hecho, setHecho] = useState(false);
  const [enviando, setEnviando] = useState(false);

  async function enviar(valores: Campos) {
    setFallo(null);
    setHecho(false);
    setEnviando(true);
    const numero = (texto: string) => (texto.trim() === '' ? null : Number(texto));
    const datos: EstadisticasCartaDto = {
      ritmo: numero(valores.ritmo),
      tiro: numero(valores.tiro),
      pase: numero(valores.pase),
      regate: numero(valores.regate),
      defensa: numero(valores.defensa),
      fisico: numero(valores.fisico),
    };

    try {
      alGuardar(await api.put<CartaDto>(`${rutaDeLaCarta}/estadisticas`, datos));
      setHecho(true);
    } catch (error) {
      setFallo(error);
    } finally {
      setEnviando(false);
    }
  }

  function guardar(evento: FormEvent) {
    evento.preventDefault();
    void enviar(campos);
  }

  function quitar() {
    setCampos(vacias());
    void enviar(vacias());
  }

  const errorDe = (campo: string) => (fallo instanceof ErrorApi ? fallo.errorDe(campo) : undefined);
  const conErrorDeCampo = CAMPOS_DE_ESTADISTICAS.some(({ campo }) => errorDe(campo));
  const cambio = carta.ultimoCambioEstadisticas;

  return (
    <Tarjeta titulo={textosCarta.estadisticas}>
      <form className="columna" onSubmit={guardar} noValidate>
        <p className="texto-suave">{textosCarta.ayudaEstadisticas}</p>
        <div className="rejilla">
          {CAMPOS_DE_ESTADISTICAS.map(({ campo, sigla, nombre }) => (
            <Campo
              key={campo}
              etiqueta={`${sigla} · ${nombre}`}
              type="number"
              min={1}
              max={99}
              valor={campos[campo]}
              alCambiar={(valor) => setCampos((previos) => ({ ...previos, [campo]: valor }))}
              error={errorDe(campo)}
            />
          ))}
        </div>
        {cambio && (
          <p className="texto-suave texto-pequeno">
            Último cambio por {cambio.autor}, {fechaYHora(cambio.fecha)}
          </p>
        )}
        {hecho && <Aviso tono="exito">{textosCarta.guardado}</Aviso>}
        {fallo !== null && !conErrorDeCampo && <Aviso tono="error">{mensajeDe(fallo)}</Aviso>}
        <div className="fila">
          <Boton type="submit" cargando={enviando} textoCargando="Guardando…">
            Guardar estadísticas
          </Boton>
          {carta.estadisticas && (
            <Boton variante="secundario" onClick={quitar} disabled={enviando}>
              {textosCarta.quitarEstadisticas}
            </Boton>
          )}
        </div>
      </form>
    </Tarjeta>
  );
}
