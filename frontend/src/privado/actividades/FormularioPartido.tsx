import { useState, type FormEvent } from 'react';
import { api } from '../../compartido/api/cliente';
import { ErrorApi, mensajeDe } from '../../compartido/api/errores';
import type {
  CondicionPartido,
  GuardarPartidoDto,
  PartidoDto,
  TipoPartido,
} from '../../compartido/api/tiposActividades';
import { Aviso } from '../../compartido/componentes/Aviso';
import { Boton } from '../../compartido/componentes/Boton';
import { Campo } from '../../compartido/componentes/Campo';
import { hoyEnColombia } from '../../compartido/fechas';
import { EditorConvocatoria } from '../partidos/EditorConvocatoria';
import { useJugadoresDeCategorias } from '../partidos/useJugadoresDeCategorias';
import { SelectorCategorias } from './SelectorCategorias';
import type { CategoriaGestionable } from './useCategoriasGestionables';

interface Props {
  clubId: string;
  /** Las que se ofrecen al crear; al cambiar se muestran las del partido, fijas. */
  categorias: CategoriaGestionable[];
  /** Al cambiar, el partido tal como está; sin él, se crea uno nuevo y se convoca. */
  partido?: PartidoDto;
  alGuardar: (partido: PartidoDto) => void;
  alCancelar?: () => void;
}

/**
 * Los campos de un partido (categorías, rival, fecha, hora, lugar, local o visitante, amistoso o
 * torneo y nombre del torneo, que solo se pide en un torneo). Al crearlo muestra además la lista de
 * jugadores de las categorías elegidas para convocar: primero se crea el partido y después se
 * guarda la convocatoria. Muestra los errores de la API en cada campo. No decide quién puede crear.
 */
export function FormularioPartido({ clubId, categorias, partido, alGuardar, alCancelar }: Props) {
  const [elegidas, setElegidas] = useState<string[]>(partido?.categorias.map((categoria) => categoria.categoriaId) ?? []);
  const [rival, setRival] = useState(partido?.rival ?? '');
  const [fecha, setFecha] = useState(partido?.fecha ?? hoyEnColombia());
  const [hora, setHora] = useState(partido?.hora ?? '09:00');
  const [lugar, setLugar] = useState(partido?.lugar ?? '');
  const [condicion, setCondicion] = useState<CondicionPartido>(partido?.condicion ?? 'LOCAL');
  const [tipo, setTipo] = useState<TipoPartido>(partido?.tipo ?? 'AMISTOSO');
  const [torneo, setTorneo] = useState(partido?.nombreTorneo ?? '');
  const [convocados, setConvocados] = useState<string[]>([]);
  const [fallo, setFallo] = useState<unknown>(null);
  const [enviando, setEnviando] = useState(false);
  const jugadores = useJugadoresDeCategorias(clubId, partido ? [] : elegidas);
  const errorDe = (campo: string) => (fallo instanceof ErrorApi ? fallo.errorDe(campo) : undefined);
  const conErroresPorCampo = fallo instanceof ErrorApi && Object.keys(fallo.errores).length > 0;

  async function guardar(evento: FormEvent) {
    evento.preventDefault();
    setFallo(null);
    setEnviando(true);
    const datos: GuardarPartidoDto = {
      rival,
      fecha,
      hora,
      lugar,
      condicion,
      tipo,
      nombreTorneo: tipo === 'TORNEO' ? torneo : null,
      categoriaIds: elegidas,
    };

    try {
      if (partido) {
        alGuardar(await api.put<PartidoDto>(`/api/clubes/${clubId}/partidos/${partido.partidoId}`, datos));
        return;
      }

      const creado = await api.post<PartidoDto>(`/api/clubes/${clubId}/partidos`, datos);
      const validos = convocados.filter((id) => jugadores.some((jugador) => jugador.usuarioRolId === id));
      if (validos.length > 0) {
        await api.put(`/api/clubes/${clubId}/partidos/${creado.partidoId}/convocatoria`, { usuarioRolIds: validos });
      }

      alGuardar(creado);
    } catch (error) {
      setFallo(error);
    } finally {
      setEnviando(false);
    }
  }

  const ofrecidas = partido
    ? partido.categorias.map((categoria) => ({ categoriaId: categoria.categoriaId, anio: categoria.anio }))
    : categorias;

  return (
    <form className="columna" onSubmit={(evento) => void guardar(evento)} noValidate>
      <SelectorCategorias
        categorias={ofrecidas}
        elegidas={elegidas}
        alCambiar={setElegidas}
        error={errorDe('categoriaIds')}
        fijas={partido !== undefined}
      />
      <Campo etiqueta="Rival" valor={rival} alCambiar={setRival} error={errorDe('rival')} maxLength={120} required />
      <div className="rejilla">
        <Campo etiqueta="Fecha" type="date" valor={fecha} alCambiar={setFecha} error={errorDe('fecha')} required />
        <Campo etiqueta="Hora" type="time" valor={hora} alCambiar={setHora} error={errorDe('hora')} required />
      </div>
      <Campo etiqueta="Lugar" valor={lugar} alCambiar={setLugar} error={errorDe('lugar')} maxLength={120} required />
      <div className="rejilla">
        <Campo
          etiqueta="Condición"
          valor={condicion}
          alCambiar={(valor) => setCondicion(valor as CondicionPartido)}
          opciones={[
            { valor: 'LOCAL', texto: 'Local' },
            { valor: 'VISITANTE', texto: 'Visitante' },
          ]}
        />
        <Campo
          etiqueta="Tipo"
          valor={tipo}
          alCambiar={(valor) => setTipo(valor as TipoPartido)}
          opciones={[
            { valor: 'AMISTOSO', texto: 'Amistoso' },
            { valor: 'TORNEO', texto: 'Torneo' },
          ]}
        />
      </div>
      {tipo === 'TORNEO' && (
        <Campo
          etiqueta="Nombre del torneo"
          valor={torneo}
          alCambiar={setTorneo}
          error={errorDe('nombreTorneo')}
          maxLength={120}
          required
        />
      )}
      {!partido && elegidas.length > 0 && (
        <EditorConvocatoria clubId={clubId} jugadores={jugadores} elegidos={convocados} alCambiar={setConvocados} />
      )}
      {fallo !== null && !conErroresPorCampo && <Aviso tono="error">{mensajeDe(fallo)}</Aviso>}
      <div className="fila">
        <Boton type="submit" cargando={enviando} textoCargando="Guardando…">
          {partido ? 'Guardar cambios' : 'Crear partido'}
        </Boton>
        {alCancelar && (
          <Boton variante="secundario" onClick={alCancelar} disabled={enviando}>
            Cancelar
          </Boton>
        )}
      </div>
    </form>
  );
}
