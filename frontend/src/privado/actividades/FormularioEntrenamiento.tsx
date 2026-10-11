import { useState, type FormEvent } from 'react';
import { api } from '../../compartido/api/cliente';
import { ErrorApi, mensajeDe } from '../../compartido/api/errores';
import type { EntrenamientoDto, GuardarEntrenamientoDto } from '../../compartido/api/tiposActividades';
import { Aviso } from '../../compartido/componentes/Aviso';
import { Boton } from '../../compartido/componentes/Boton';
import { Campo } from '../../compartido/componentes/Campo';
import { hoyEnColombia } from '../../compartido/fechas';
import { SelectorCategorias } from './SelectorCategorias';
import type { CategoriaGestionable } from './useCategoriasGestionables';

interface Props {
  clubId: string;
  /** Las que se ofrecen al crear; al cambiar se muestran las del entrenamiento, fijas. */
  categorias: CategoriaGestionable[];
  /** Al cambiar, el entrenamiento tal como está; sin él, se programa uno nuevo. */
  entrenamiento?: EntrenamientoDto;
  alGuardar: (entrenamiento: EntrenamientoDto) => void;
  alCancelar?: () => void;
}

/**
 * Los campos de un entrenamiento (categorías, fecha, hora de inicio y de fin, lugar y nota) para
 * programarlo o cambiarlo. Muestra en cada campo el error que devuelve la API y, en un aviso, los
 * demás (por ejemplo, que ya no lo gestiona o que está cancelado). No decide quién puede programar.
 */
export function FormularioEntrenamiento({ clubId, categorias, entrenamiento, alGuardar, alCancelar }: Props) {
  const [elegidas, setElegidas] = useState<string[]>(
    entrenamiento?.categorias.map((categoria) => categoria.categoriaId) ?? [],
  );
  const [fecha, setFecha] = useState(entrenamiento?.fecha ?? hoyEnColombia());
  const [horaInicio, setHoraInicio] = useState(entrenamiento?.horaInicio ?? '16:00');
  const [horaFin, setHoraFin] = useState(entrenamiento?.horaFin ?? '17:30');
  const [lugar, setLugar] = useState(entrenamiento?.lugar ?? '');
  const [nota, setNota] = useState(entrenamiento?.nota ?? '');
  const [fallo, setFallo] = useState<unknown>(null);
  const [enviando, setEnviando] = useState(false);
  const errorDe = (campo: string) => (fallo instanceof ErrorApi ? fallo.errorDe(campo) : undefined);
  const conErroresPorCampo = fallo instanceof ErrorApi && Object.keys(fallo.errores).length > 0;

  async function guardar(evento: FormEvent) {
    evento.preventDefault();
    setFallo(null);
    setEnviando(true);
    const datos: GuardarEntrenamientoDto = {
      fecha,
      horaInicio,
      horaFin,
      lugar,
      nota: nota.trim() === '' ? null : nota,
      categoriaIds: elegidas,
    };

    try {
      alGuardar(
        entrenamiento
          ? await api.put<EntrenamientoDto>(
              `/api/clubes/${clubId}/entrenamientos/${entrenamiento.entrenamientoId}`,
              datos,
            )
          : await api.post<EntrenamientoDto>(`/api/clubes/${clubId}/entrenamientos`, datos),
      );
    } catch (error) {
      setFallo(error);
    } finally {
      setEnviando(false);
    }
  }

  const ofrecidas = entrenamiento
    ? entrenamiento.categorias.map((categoria) => ({ categoriaId: categoria.categoriaId, anio: categoria.anio }))
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
      <div className="rejilla">
        <Campo etiqueta="Fecha" type="date" valor={fecha} alCambiar={setFecha} error={errorDe('fecha')} required />
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
      {fallo !== null && !conErroresPorCampo && <Aviso tono="error">{mensajeDe(fallo)}</Aviso>}
      <div className="fila">
        <Boton type="submit" cargando={enviando} textoCargando="Guardando…">
          {entrenamiento ? 'Guardar cambios' : 'Programar entrenamiento'}
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
