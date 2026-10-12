import { useState, type FormEvent } from 'react';
import { api } from '../../compartido/api/cliente';
import { ErrorApi, mensajeDe } from '../../compartido/api/errores';
import type { PartidoDto } from '../../compartido/api/tiposActividades';
import type { ResultadoDeFamiliaDto } from '../../compartido/api/tiposResultadoFamilia';
import { Aviso } from '../../compartido/componentes/Aviso';
import { Boton } from '../../compartido/componentes/Boton';
import { Campo } from '../../compartido/componentes/Campo';
import { Tarjeta } from '../../compartido/componentes/Tarjeta';
import { textoDeMarcador, textosResultadoFamilia } from './textos';

interface Props {
  clubId: string;
  nombreClub: string;
  partido: PartidoDto;
  alCambiar: (partido: PartidoDto) => void;
  /** Vuelve a pedir el partido, para mostrar el marcador que otra familia publicó primero. */
  recargar: () => void;
}

/**
 * Lo que la familia de un convocado hace con el resultado de un partido que ya empezó (spec 010,
 * historia 1). Sin marcador: los goles del club y del rival, "¿Jugó?" y los goles de su jugador.
 * Con marcador: el publicado, sin poder cambiarlo, y el formulario de su jugador con lo que ya
 * indicó. Si quien entrena ya lo corrigió, solo lo dice, sin formulario. Nunca pinta a otro
 * convocado: solo recibe lo de su jugador (RF-005). Las reglas las decide la API.
 */
export function SeccionResultadoFamilia({ clubId, nombreClub, partido, alCambiar, recargar }: Props) {
  const mia = partido.miParticipacion;
  const [golesClub, setGolesClub] = useState('');
  const [golesRival, setGolesRival] = useState('');
  const [jugo, setJugo] = useState(mia?.jugo ?? false);
  const [goles, setGoles] = useState(String(mia?.goles ?? 0));
  const [fallo, setFallo] = useState<unknown>(null);
  const [aviso, setAviso] = useState<string | null>(null);
  const [enviando, setEnviando] = useState(false);

  if (!partido.permisos.indicaResultado && !mia) {
    return null;
  }

  const conMarcador = partido.golesClub !== undefined && partido.golesClub !== null;
  const corregido = mia ? !mia.puedeCambiar : false;

  async function guardar(evento: FormEvent) {
    evento.preventDefault();
    setFallo(null);
    setAviso(null);
    setEnviando(true);
    const datos: ResultadoDeFamiliaDto = {
      golesClub: conMarcador ? null : golesClub === '' ? null : Number(golesClub),
      golesRival: conMarcador ? null : golesRival === '' ? null : Number(golesRival),
      jugo,
      goles: jugo ? Number(goles) || 0 : 0,
    };

    try {
      alCambiar(await api.put<PartidoDto>(`/api/clubes/${clubId}/partidos/${partido.partidoId}/resultado/mi-jugador`, datos));
      setAviso(textosResultadoFamilia.guardado);
    } catch (error) {
      if (error instanceof ErrorApi && error.codigo === 'marcador_ya_publicado') {
        setAviso(textosResultadoFamilia.otroMarcador);
        recargar();
      } else {
        setFallo(error);
      }
    } finally {
      setEnviando(false);
    }
  }

  return (
    <Tarjeta titulo="Tu jugador en este partido">
      {conMarcador && !corregido && (
        <p className="texto-suave">
          El marcador ya está publicado (<span className="dato-largo">{textoDeMarcador(partido, nombreClub)}</span>): solo indicas
          lo de tu jugador.
        </p>
      )}
      {corregido ? (
        <>
          <p>
            {mia?.jugo ? `Jugó${mia.goles > 0 ? ` y marcó ${mia.goles === 1 ? '1 gol' : `${mia.goles} goles`}` : ''}.` : 'No jugó.'}
          </p>
          <Aviso tono="neutro">{textosResultadoFamilia.corregido}</Aviso>
        </>
      ) : (
        <form className="columna" onSubmit={(evento) => void guardar(evento)} noValidate>
          {!conMarcador && (
            <div className="rejilla">
              <Campo etiqueta="Goles del club" type="number" min={0} max={99} valor={golesClub} alCambiar={setGolesClub} />
              <Campo etiqueta="Goles del rival" type="number" min={0} max={99} valor={golesRival} alCambiar={setGolesRival} />
            </div>
          )}
          <fieldset className="opciones">
            <legend>¿Jugó?</legend>
            <label className="opcion">
              <input type="radio" name="jugo" checked={jugo} onChange={() => setJugo(true)} />
              <span>Sí</span>
            </label>
            <label className="opcion">
              <input type="radio" name="jugo" checked={!jugo} onChange={() => setJugo(false)} />
              <span>No</span>
            </label>
          </fieldset>
          <Campo
            etiqueta="Goles de tu jugador"
            type="number"
            min={0}
            max={99}
            valor={jugo ? goles : '0'}
            alCambiar={setGoles}
            disabled={!jugo}
          />
          {aviso && <Aviso tono="exito">{aviso}</Aviso>}
          {fallo !== null && <Aviso tono="error">{mensajeDe(fallo)}</Aviso>}
          <div className="fila">
            <Boton type="submit" cargando={enviando} textoCargando="Guardando…">
              {conMarcador ? 'Guardar lo de mi jugador' : 'Publicar resultado'}
            </Boton>
          </div>
        </form>
      )}
    </Tarjeta>
  );
}
