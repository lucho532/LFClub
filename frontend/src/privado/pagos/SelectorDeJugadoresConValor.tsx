import { useState, type FormEvent } from 'react';
import { api } from '../../compartido/api/cliente';
import { ErrorApi, mensajeDe } from '../../compartido/api/errores';
import type { GrupoDeJugadoresDto } from '../../compartido/api/tiposJugadores';
import type { JugadorConValorPropioDto, MensualidadDto } from '../../compartido/api/tiposPagos';
import { useCarga } from '../../compartido/api/useCarga';
import { Aviso } from '../../compartido/componentes/Aviso';
import { Boton } from '../../compartido/componentes/Boton';
import { Campo } from '../../compartido/componentes/Campo';
import { nombreCompleto } from '../categorias/textos';
import { tituloDeGrupo } from '../jugadores/textos';
import { apiMensualidad } from './apiMensualidad';
import { formatoMoneda, leerPesos } from './formatoMoneda';

interface Props {
  clubId: string;
  propios: JugadorConValorPropioDto[];
  /** Recibe la mensualidad que devolvió la API tras cada cambio. */
  alCambiar: (mensualidad: MensualidadDto) => void;
}

/**
 * Selección de jugadores para fijarles, de una vez, un valor de mensualidad propio o devolverlos al
 * valor del club (spec 015, RF-012). Muestra los jugadores agrupados por categoría con una casilla y,
 * al lado, el valor propio si lo tienen. Solo cambia los cargos que se generen después.
 */
export function SelectorDeJugadoresConValor({ clubId, propios, alCambiar }: Props) {
  const grupos = useCarga(`${clubId}/jugadores`, () => api.get<GrupoDeJugadoresDto[]>(`/api/clubes/${clubId}/jugadores`));
  const [elegidos, setElegidos] = useState<string[]>([]);
  const [texto, setTexto] = useState('');
  const [errorDelValor, setErrorDelValor] = useState<string | undefined>();
  const [error, setError] = useState<string | null>(null);
  const [exito, setExito] = useState<string | null>(null);
  const [enviando, setEnviando] = useState(false);

  const valorPropio = new Map(propios.map((jugador) => [jugador.usuarioRolId, jugador.valor]));

  const alternar = (id: string) => {
    setExito(null);
    setElegidos((previos) => (previos.includes(id) ? previos.filter((otro) => otro !== id) : [...previos, id]));
  };

  async function aplicar(valor: number | null) {
    setError(null);
    setExito(null);
    setEnviando(true);
    try {
      alCambiar(await apiMensualidad.fijarValorPropio(clubId, { usuarioRolIds: elegidos, valor }));
      setExito(valor === null ? 'Los jugadores vuelven al valor del club.' : 'Valor propio guardado.');
      setElegidos([]);
      setTexto('');
    } catch (fallo) {
      setError(fallo instanceof ErrorApi && fallo.errorDe('valor') ? fallo.errorDe('valor')! : mensajeDe(fallo));
    } finally {
      setEnviando(false);
    }
  }

  function fijar(evento: FormEvent) {
    evento.preventDefault();
    const valor = leerPesos(texto);
    if (valor === null) {
      setErrorDelValor('Escribe un valor entero de pesos, mayor que cero.');
      return;
    }

    setErrorDelValor(undefined);
    void aplicar(valor);
  }

  return (
    <form className="columna" onSubmit={fijar} noValidate>
      {error && <Aviso tono="error">{error}</Aviso>}
      {exito && <Aviso tono="exito">{exito}</Aviso>}
      {!grupos.datos && grupos.cargando && <p className="texto-suave">Cargando…</p>}
      {grupos.error && <Aviso tono="error">{grupos.error}</Aviso>}
      {grupos.datos?.map((grupo) => (
        <fieldset key={grupo.categoria?.categoriaId ?? 'sin-categoria'} className="columna">
          <legend>{tituloDeGrupo(grupo)}</legend>
          {grupo.jugadores.map((jugador) => (
            <label key={jugador.usuarioRolId} className="fila">
              <input
                type="checkbox"
                checked={elegidos.includes(jugador.usuarioRolId)}
                onChange={() => alternar(jugador.usuarioRolId)}
              />
              <span>{nombreCompleto(jugador)}</span>
              {valorPropio.has(jugador.usuarioRolId) && (
                <span className="texto-suave">Valor propio: {formatoMoneda(valorPropio.get(jugador.usuarioRolId)!)}</span>
              )}
            </label>
          ))}
        </fieldset>
      ))}
      <Campo
        etiqueta="Valor propio de la mensualidad"
        inputMode="numeric"
        valor={texto}
        alCambiar={(nuevo) => {
          setTexto(nuevo);
          setErrorDelValor(undefined);
        }}
        error={errorDelValor}
        ayuda="Solo cambia los cargos que se generen desde ahora."
      />
      <div className="fila">
        <Boton type="submit" disabled={elegidos.length === 0} cargando={enviando} textoCargando="Guardando…">
          Fijar valor a los elegidos
        </Boton>
        <Boton
          variante="secundario"
          disabled={elegidos.length === 0 || enviando}
          onClick={() => void aplicar(null)}
        >
          Volver al valor del club
        </Boton>
      </div>
    </form>
  );
}
