import { useState, type FormEvent } from 'react';
import { api } from '../../compartido/api/cliente';
import { ErrorApi, mensajeDe } from '../../compartido/api/errores';
import type { EntrenamientoDto, GuardarEntrenamientoDto } from '../../compartido/api/tiposActividades';
import { Aviso } from '../../compartido/componentes/Aviso';
import { Boton } from '../../compartido/componentes/Boton';
import { Campo } from '../../compartido/componentes/Campo';
import { diaDeLaSemana, hoyEnColombia } from '../../compartido/fechas';
import { SelectorCategorias } from './SelectorCategorias';
import { SelectorEquipos } from './SelectorEquipos';
import { SelectorFechas } from './SelectorFechas';
import type { CategoriaGestionable } from './useCategoriasGestionables';

interface Props {
  clubId: string;
  /** Las que se ofrecen al crear; al cambiar se muestran las del entrenamiento, fijas. */
  categorias: CategoriaGestionable[];
  /** Al cambiar, el entrenamiento tal como está; sin él, se programa uno nuevo. */
  entrenamiento?: EntrenamientoDto;
  /** Con el entrenamiento guardado (el primero, si se programaron varios) y cuántos se guardaron. */
  alGuardar: (entrenamiento: EntrenamientoDto, cuantos: number) => void;
  alCancelar?: () => void;
}

/**
 * Los campos de un entrenamiento (categorías, fecha, hora de inicio y de fin, lugar y nota) para
 * programarlo o cambiarlo. Al programar se pueden elegir varios días: se crea un entrenamiento
 * independiente por cada uno, de uno en uno, y si alguno falla quedan en la lista solo los días que
 * faltan. Muestra en cada campo el error que devuelve la API y, en un aviso, los
 * demás (por ejemplo, que ya no lo gestiona o que está cancelado). No decide quién puede programar.
 */
export function FormularioEntrenamiento({ clubId, categorias, entrenamiento, alGuardar, alCancelar }: Props) {
  const [elegidas, setElegidas] = useState<string[]>(
    entrenamiento?.categorias.map((categoria) => categoria.categoriaId) ?? [],
  );
  const [equiposElegidos, setEquiposElegidos] = useState<string[]>([]);
  const [fechas, setFechas] = useState<string[]>([entrenamiento?.fecha ?? hoyEnColombia()]);
  const [horaInicio, setHoraInicio] = useState(entrenamiento?.horaInicio ?? '16:00');
  const [horaFin, setHoraFin] = useState(entrenamiento?.horaFin ?? '17:30');
  const [lugar, setLugar] = useState(entrenamiento?.lugar ?? '');
  const [nota, setNota] = useState(entrenamiento?.nota ?? '');
  const [fallo, setFallo] = useState<unknown>(null);
  const [enviando, setEnviando] = useState(false);
  const [programados, setProgramados] = useState(0);
  const errorDe = (campo: string) => (fallo instanceof ErrorApi ? fallo.errorDe(campo) : undefined);
  const categoriasElegidas = categorias.filter((categoria) => elegidas.includes(categoria.categoriaId));
  // Un equipo solo cuenta si su categoría sigue elegida: al apagar una categoría se sueltan los suyos.
  const equiposDeLasElegidas = equiposElegidos.filter((equipoId) =>
    categoriasElegidas.some((categoria) => categoria.equipos.some((equipo) => equipo.equipoId === equipoId)),
  );
  const conErroresPorCampo = fallo instanceof ErrorApi && Object.keys(fallo.errores).length > 0;

  async function guardar(evento: FormEvent) {
    evento.preventDefault();
    setFallo(null);
    setProgramados(0);
    setEnviando(true);
    const datosDe = (fecha: string): GuardarEntrenamientoDto => ({
      fecha,
      horaInicio,
      horaFin,
      lugar,
      nota: nota.trim() === '' ? null : nota,
      categoriaIds: elegidas,
      // Al cambiar, la audiencia no se toca aquí: se cambia desde el detalle (spec 013).
      ...(entrenamiento ? {} : { equipoIds: equiposDeLasElegidas }),
    });

    if (entrenamiento) {
      try {
        const ruta = `/api/clubes/${clubId}/entrenamientos/${entrenamiento.entrenamientoId}`;
        alGuardar(await api.put<EntrenamientoDto>(ruta, datosDe(fechas[0])), 1);
      } catch (error) {
        setFallo(error);
      } finally {
        setEnviando(false);
      }
      return;
    }

    const dias = [...new Set(fechas)];
    const creados: EntrenamientoDto[] = [];
    for (const fecha of dias) {
      try {
        creados.push(await api.post<EntrenamientoDto>(`/api/clubes/${clubId}/entrenamientos`, datosDe(fecha)));
      } catch (error) {
        setFallo(error);
        setProgramados(creados.length);
        setFechas(dias.slice(creados.length));
        setEnviando(false);
        return;
      }
    }

    setEnviando(false);
    alGuardar(creados[0], creados.length);
  }

  const ofrecidas = entrenamiento
    ? entrenamiento.categorias.map((categoria) => ({
        categoriaId: categoria.categoriaId,
        anio: categoria.anio,
        equipos: [],
      }))
    : categorias;

  return (
    <form className="columna" onSubmit={(evento) => void guardar(evento)} noValidate>
      <SelectorCategorias
        categorias={ofrecidas}
        elegidas={elegidas}
        alCambiar={setElegidas}
        error={errorDe('categoriaIds')}
        fijas={entrenamiento !== undefined}
      />
      {!entrenamiento && (
        <SelectorEquipos
          categorias={categoriasElegidas}
          elegidos={equiposDeLasElegidas}
          alCambiar={setEquiposElegidos}
          error={errorDe('equipoIds')}
        />
      )}
      {!entrenamiento && <SelectorFechas fechas={fechas} alCambiar={setFechas} error={errorDe('fecha')} />}
      <div className="rejilla">
        {entrenamiento && (
          <Campo
            etiqueta="Fecha"
            type="date"
            valor={fechas[0]}
            alCambiar={(valor) => setFechas([valor])}
            error={errorDe('fecha')}
            required
          />
        )}
        <Campo
          etiqueta="Hora de inicio"
          type="time"
          valor={horaInicio}
          alCambiar={setHoraInicio}
          error={errorDe('horaInicio')}
          required
        />
        <Campo
          etiqueta="Hora de fin"
          type="time"
          valor={horaFin}
          alCambiar={setHoraFin}
          error={errorDe('horaFin')}
          required
        />
      </div>
      <Campo
        etiqueta="Lugar"
        valor={lugar}
        alCambiar={setLugar}
        error={errorDe('lugar')}
        maxLength={120}
        placeholder="Cancha de Minitas"
        required
      />
      <Campo
        etiqueta="Nota para las familias (opcional)"
        valor={nota}
        alCambiar={setNota}
        error={errorDe('nota')}
        maxLength={500}
        lineas={3}
      />
      {programados > 0 && (
        <Aviso tono="aviso">
          {`Se ${programados === 1 ? 'programó 1 entrenamiento' : `programaron ${programados} entrenamientos`}. `}
          {`Faltan ${fechas.map(diaDeLaSemana).join(', ')}: revisa y vuelve a intentarlo.`}
        </Aviso>
      )}
      {fallo !== null && !conErroresPorCampo && <Aviso tono="error">{mensajeDe(fallo)}</Aviso>}
      <div className="fila">
        <Boton
          type="submit"
          cargando={enviando}
          textoCargando="Guardando…"
          disabled={!entrenamiento && fechas.length === 0}
        >
          {entrenamiento
            ? 'Guardar cambios'
            : fechas.length > 1
              ? `Programar ${new Set(fechas).size} entrenamientos`
              : 'Programar entrenamiento'}
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
