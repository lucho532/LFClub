import { useState, type FormEvent } from 'react';
import { api } from '../../compartido/api/cliente';
import { ErrorApi, mensajeDe } from '../../compartido/api/errores';
import type { PartidoDto, PlanillaPartidoDto, RegistrarResultadoDto } from '../../compartido/api/tiposActividades';
import { useCarga } from '../../compartido/api/useCarga';
import { Aviso } from '../../compartido/componentes/Aviso';
import { Boton } from '../../compartido/componentes/Boton';
import { Campo } from '../../compartido/componentes/Campo';
import { textoDeIndicacion } from './textos';

interface Props {
  clubId: string;
  partido: PartidoDto;
  alGuardar: (partido: PartidoDto) => void;
  alCancelar: () => void;
}

interface Fila {
  jugo: boolean;
  goles: string;
}

/**
 * El formulario del resultado: goles del club y del rival y la lista de convocados, todos marcados
 * como que jugaron al registrarlo por primera vez (escenario 3.3); al corregirlo, como estaba. Los
 * goles de un jugador solo se escriben si jugó, y avisa si la suma no cuadra con el marcador. Los
 * rechazos los decide la API (RF-017) y se muestran tal como los explica. Cada fila dice quién
 * indicó lo de ese jugador y cuándo, o "Sin indicar" (spec 010, RF-009).
 */
export function FormularioResultado({ clubId, partido, alGuardar, alCancelar }: Props) {
  const ruta = `/api/clubes/${clubId}/partidos/${partido.partidoId}`;
  const planilla = useCarga(`${ruta}/jugadores#resultado`, () => api.get<PlanillaPartidoDto>(`${ruta}/jugadores`));
  const [golesClub, setGolesClub] = useState(String(partido.golesClub ?? 0));
  const [golesRival, setGolesRival] = useState(String(partido.golesRival ?? 0));
  const [cambios, setCambios] = useState<Record<string, Fila>>({});
  const [fallo, setFallo] = useState<unknown>(null);
  const [enviando, setEnviando] = useState(false);
  const convocados = planilla.datos?.convocados ?? [];
  const primeraVez = partido.estado !== 'JUGADO';

  const filaDe = (usuarioRolId: string): Fila => {
    const convocado = convocados.find((fila) => fila.usuarioRolId === usuarioRolId);
    return (
      cambios[usuarioRolId] ?? {
        jugo: primeraVez || (convocado?.jugo ?? false),
        goles: String(primeraVez ? 0 : (convocado?.goles ?? 0)),
      }
    );
  };
  const cambiar = (usuarioRolId: string, fila: Partial<Fila>) =>
    setCambios((previos) => ({ ...previos, [usuarioRolId]: { ...filaDe(usuarioRolId), ...fila } }));

  const suma = convocados.reduce((total, fila) => total + (filaDe(fila.usuarioRolId).jugo ? Number(filaDe(fila.usuarioRolId).goles) || 0 : 0), 0);
  const delClub = Number(golesClub) || 0;

  async function guardar(evento: FormEvent) {
    evento.preventDefault();
    setFallo(null);
    setEnviando(true);
    const datos: RegistrarResultadoDto = {
      golesClub: Number(golesClub),
      golesRival: Number(golesRival),
      jugadores: convocados.map((convocado) => {
        const fila = filaDe(convocado.usuarioRolId);
        return { usuarioRolId: convocado.usuarioRolId, jugo: fila.jugo, goles: fila.jugo ? Number(fila.goles) || 0 : 0 };
      }),
    };

    try {
      alGuardar(await api.put<PartidoDto>(`${ruta}/resultado`, datos));
    } catch (error) {
      setFallo(error);
    } finally {
      setEnviando(false);
    }
  }

  const errorDe = (campo: string) => (fallo instanceof ErrorApi ? fallo.errorDe(campo) : undefined);

  return (
    <form className="columna" onSubmit={(evento) => void guardar(evento)} noValidate>
      <div className="rejilla">
        <Campo etiqueta="Goles del club" type="number" min={0} max={99} valor={golesClub} alCambiar={setGolesClub} error={errorDe('golesClub')} />
        <Campo etiqueta="Goles del rival" type="number" min={0} max={99} valor={golesRival} alCambiar={setGolesRival} error={errorDe('golesRival')} />
      </div>
      {planilla.error && <Aviso tono="error">{planilla.error}</Aviso>}
      {planilla.datos && convocados.length === 0 && <p className="texto-suave">Este partido no tiene convocados.</p>}
      {convocados.length > 0 && (
        <fieldset className="opciones">
          <legend>Quiénes jugaron y sus goles</legend>
          <p className="texto-suave texto-pequeno">
            El número a la derecha de cada nombre son los goles que anotó ese jugador en este partido. Solo se
            puede escribir en quien jugó.
          </p>
          <div className="filas-resultado">
            {convocados.map((convocado) => {
              const fila = filaDe(convocado.usuarioRolId);
              return (
                <div key={convocado.usuarioRolId} className="fila-resultado">
                  <label className="casilla">
                    <input
                      type="checkbox"
                      checked={fila.jugo}
                      onChange={(evento) => cambiar(convocado.usuarioRolId, { jugo: evento.target.checked })}
                    />
                    <span className="columna-compacta">
                      <span className="dato-largo">
                        {convocado.nombres} {convocado.apellidos} · {fila.jugo ? 'Jugó' : 'No jugó'}
                      </span>
                      <span className="texto-suave texto-pequeno">{textoDeIndicacion(convocado.indicacion)}</span>
                    </span>
                  </label>
                  <input
                    type="number"
                    min={0}
                    max={99}
                    aria-label={`Goles de ${convocado.nombres} ${convocado.apellidos}`}
                    title={`Goles que anotó ${convocado.nombres} en este partido`}
                    className="goles-de-jugador"
                    value={fila.jugo ? fila.goles : '0'}
                    disabled={!fila.jugo}
                    onChange={(evento) => cambiar(convocado.usuarioRolId, { goles: evento.target.value })}
                  />
                </div>
              );
            })}
          </div>
        </fieldset>
      )}
      {suma > delClub && <Aviso tono="aviso">Los goles de los jugadores ({suma}) suman más que los del club ({delClub}).</Aviso>}
      {suma < delClub && convocados.length > 0 && (
        <p className="texto-suave">{delClub - suma === 1 ? 'Queda 1 gol sin autor.' : `Quedan ${delClub - suma} goles sin autor.`}</p>
      )}
      {fallo !== null && !errorDe('golesClub') && !errorDe('golesRival') && <Aviso tono="error">{mensajeDe(fallo)}</Aviso>}
      <div className="fila">
        <Boton type="submit" cargando={enviando} textoCargando="Guardando…" disabled={!planilla.datos}>
          Guardar resultado
        </Boton>
        <Boton variante="secundario" onClick={alCancelar} disabled={enviando}>
          Cancelar
        </Boton>
      </div>
    </form>
  );
}
