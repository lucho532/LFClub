import { useState, type FormEvent } from 'react';
import { ErrorApi, mensajeDe } from '../../compartido/api/errores';
import { Aviso } from '../../compartido/componentes/Aviso';
import { Boton } from '../../compartido/componentes/Boton';
import { Campo } from '../../compartido/componentes/Campo';
import { Tarjeta } from '../../compartido/componentes/Tarjeta';
import { useClub } from '../contextoClub';
import { apiMensualidad } from './apiMensualidad';
import { formatoMoneda, leerPesos } from './formatoMoneda';
import { MensualidadPorJugador } from './MensualidadPorJugador';
import { AVISO_VALOR_MENSUALIDAD } from './textosPagos';
import { useMensualidad } from './useMensualidad';

/**
 * Pagos → Mensualidad (spec 015, historia 2): el valor de la mensualidad del club y una tarjeta editable
 * por jugador para los que pagan otro. Solo el PRESIDENTE los cambia; el DIRECTIVO los consulta. Cambiar el valor solo
 * afecta los cargos que se generen después: los ya generados conservan el suyo (RF-013).
 */
export function PantallaMensualidad() {
  const { club } = useClub();
  const mensualidad = useMensualidad(club.clubId);
  const puedeCambiar = club.miRol === 'PRESIDENTE';
  const [texto, setTexto] = useState('');
  const [errorDelValor, setErrorDelValor] = useState<string | undefined>();
  const [error, setError] = useState<string | null>(null);
  const [guardado, setGuardado] = useState(false);
  const [enviando, setEnviando] = useState(false);

  async function guardar(evento: FormEvent) {
    evento.preventDefault();
    setError(null);
    setGuardado(false);
    const valor = leerPesos(texto);
    if (valor === null) {
      setErrorDelValor('Escribe un valor entero de pesos, mayor que cero.');
      return;
    }

    setErrorDelValor(undefined);
    setEnviando(true);
    try {
      mensualidad.fijar(await apiMensualidad.fijarValorDelClub(club.clubId, valor));
      setTexto('');
      setGuardado(true);
    } catch (fallo) {
      if (fallo instanceof ErrorApi && fallo.errorDe('valor')) {
        setErrorDelValor(fallo.errorDe('valor'));
      } else {
        setError(mensajeDe(fallo));
      }
    } finally {
      setEnviando(false);
    }
  }

  const datos = mensualidad.datos;

  return (
    <>
      <div className="cabecera">
        <h1>Mensualidad</h1>
      </div>
      {mensualidad.error && <Aviso tono="error">{mensualidad.error}</Aviso>}
      {!datos && mensualidad.cargando && <p className="texto-suave">Cargando…</p>}
      {datos && (
        <>
          <Tarjeta titulo="Valor de la mensualidad del club">
            {datos.valorClub === null ? (
              <Aviso tono="aviso">
                {AVISO_VALOR_MENSUALIDAD}: mientras no exista, no se generan cargos.
              </Aviso>
            ) : (
              <p>
                Cada jugador paga <strong>{formatoMoneda(datos.valorClub)}</strong> al mes.
              </p>
            )}
            {puedeCambiar && (
              <form className="columna" onSubmit={guardar} noValidate>
                {error && <Aviso tono="error">{error}</Aviso>}
                {guardado && <Aviso tono="exito">Valor guardado.</Aviso>}
                <Campo
                  etiqueta="Nuevo valor mensual"
                  inputMode="numeric"
                  valor={texto}
                  alCambiar={(nuevo) => {
                    setTexto(nuevo);
                    setGuardado(false);
                    setErrorDelValor(undefined);
                  }}
                  error={errorDelValor}
                  ayuda="Solo cambia los cargos que se generen desde ahora: los ya generados conservan su valor."
                />
                <div className="fila">
                  <Boton type="submit" cargando={enviando} textoCargando="Guardando…">
                    Guardar valor
                  </Boton>
                </div>
              </form>
            )}
          </Tarjeta>
          <Tarjeta titulo="Mensualidad de cada jugador">
            <MensualidadPorJugador
              clubId={club.clubId}
              mensualidad={datos}
              editable={puedeCambiar}
              alCambiar={mensualidad.fijar}
            />
          </Tarjeta>
        </>
      )}
    </>
  );
}
