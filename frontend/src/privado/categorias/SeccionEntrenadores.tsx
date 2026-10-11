import { useState } from 'react';
import { api } from '../../compartido/api/cliente';
import { mensajeDe } from '../../compartido/api/errores';
import type {
  CandidatoEntrenadorDto,
  CategoriaDetalleDto,
  EntrenadorDeCategoriaDto,
  EquiposDeEntrenadorDto,
} from '../../compartido/api/tiposCategorias';
import { Aviso } from '../../compartido/componentes/Aviso';
import { Boton } from '../../compartido/componentes/Boton';
import { DialogoConfirmacion } from '../../compartido/componentes/DialogoConfirmacion';
import { NombrePersona } from '../../compartido/componentes/NombrePersona';
import { Tabla, type Columna } from '../../compartido/componentes/Tabla';
import { Tarjeta } from '../../compartido/componentes/Tarjeta';
import { nombreDeRol } from '../../compartido/formato';
import { DialogoAsignarEntrenador } from './DialogoAsignarEntrenador';
import { nombreCompleto } from './textos';
import { useAccion } from './useAccion';

interface Props {
  clubId: string;
  categoria: CategoriaDetalleDto;
  esPresidente: boolean;
  /** Recibe la categoría tal como la devolvió la API tras el cambio. */
  alCambiar: (categoria: CategoriaDetalleDto) => void;
}

/**
 * Entrenadores de una categoría: quiénes son, su rol en el club y qué equipos dirigen. El
 * presidente asigna, retira e indica los equipos de cada uno; los demás solo lo ven. Quien no
 * dirige ningún equipo es entrenador de la categoría en general. Su nombre abre su ficha básica,
 * y desde ella se vuelve a esta categoría (spec 008).
 */
export function SeccionEntrenadores({ clubId, categoria, esPresidente, alCambiar }: Props) {
  const base = `/api/clubes/${clubId}/categorias/${categoria.categoriaId}/entrenadores`;
  const { ocupado, mensaje, ejecutar } = useAccion();
  const [asignando, setAsignando] = useState(false);
  const [enviando, setEnviando] = useState(false);
  const [errorDialogo, setErrorDialogo] = useState<string | undefined>();
  const [porRetirar, setPorRetirar] = useState<EntrenadorDeCategoriaDto | null>(null);

  async function asignar(candidato: CandidatoEntrenadorDto) {
    setErrorDialogo(undefined);
    setEnviando(true);
    try {
      alCambiar(await api.put<CategoriaDetalleDto>(`${base}/${candidato.usuarioRolId}`, {}));
      setAsignando(false);
    } catch (fallo) {
      setErrorDialogo(mensajeDe(fallo));
    } finally {
      setEnviando(false);
    }
  }

  async function retirar() {
    if (!porRetirar) {
      return;
    }

    const entrenador = porRetirar;
    setPorRetirar(null);
    await ejecutar(async () => {
      alCambiar(await api.delete<CategoriaDetalleDto>(`${base}/${entrenador.usuarioRolId}`));
      return `${nombreCompleto(entrenador)} ya no entrena la categoría ${categoria.anio}.`;
    });
  }

  /** Marca o desmarca un equipo y guarda la lista completa de los que dirige (RF-028). */
  const cambiarEquipo = (entrenador: EntrenadorDeCategoriaDto, equipoId: string, dirige: boolean) =>
    ejecutar(async () => {
      const actuales = entrenador.equipos.map((equipo) => equipo.equipoId);
      const datos: EquiposDeEntrenadorDto = {
        equipoIds: dirige ? [...actuales, equipoId] : actuales.filter((id) => id !== equipoId),
      };
      alCambiar(await api.put<CategoriaDetalleDto>(`${base}/${entrenador.usuarioRolId}/equipos`, datos));
      return null;
    });

  const columnas: Columna<EntrenadorDeCategoriaDto>[] = [
    {
      titulo: 'Entrenador',
      celda: (entrenador) => (
        <strong>
          <NombrePersona
            nombre={nombreCompleto(entrenador)}
            enlace={{ usuarioRolId: entrenador.usuarioRolId, tipo: 'BASICA' }}
            clubId={clubId}
            textoVolver={`Categoría ${categoria.anio}`}
          />
        </strong>
      ),
    },
    { titulo: 'Rol en el club', celda: (entrenador) => nombreDeRol(entrenador.rol) },
    {
      titulo: 'Equipos que dirige',
      celda: (entrenador) =>
        esPresidente && categoria.equipos.length > 0 ? (
          <span className="casillas">
            {categoria.equipos.map((equipo) => (
              <label key={equipo.equipoId} className="casilla">
                <input
                  type="checkbox"
                  checked={entrenador.equipos.some((suyo) => suyo.equipoId === equipo.equipoId)}
                  disabled={ocupado}
                  onChange={(evento) => void cambiarEquipo(entrenador, equipo.equipoId, evento.target.checked)}
                />
                {equipo.nombre}
              </label>
            ))}
          </span>
        ) : (
          entrenador.equipos.map((equipo) => equipo.nombre).join(', ') || 'De la categoría en general'
        ),
    },
  ];

  if (esPresidente) {
    columnas.push({
      titulo: 'Acciones',
      celda: (entrenador) => (
        <Boton variante="secundario" onClick={() => setPorRetirar(entrenador)} disabled={ocupado}>
          Retirar
        </Boton>
      ),
    });
  }

  return (
    <Tarjeta
      titulo="Entrenadores"
      acciones={
        esPresidente &&
        categoria.activa && (
          <Boton
            onClick={() => {
              setErrorDialogo(undefined);
              setAsignando(true);
            }}
          >
            Asignar entrenador
          </Boton>
        )
      }
    >
      {mensaje && <Aviso tono={mensaje.tono}>{mensaje.texto}</Aviso>}
      <Tabla
        descripcion={`Entrenadores de la categoría ${categoria.anio}`}
        columnas={columnas}
        filas={categoria.entrenadores}
        clave={(entrenador) => entrenador.usuarioRolId}
        vacio="Esta categoría todavía no tiene entrenador."
      />

      {asignando && (
        <DialogoAsignarEntrenador
          clubId={clubId}
          categoriaId={categoria.categoriaId}
          anio={categoria.anio}
          cargando={enviando}
          error={errorDialogo}
          alConfirmar={(candidato) => void asignar(candidato)}
          alCancelar={() => setAsignando(false)}
        />
      )}

      <DialogoConfirmacion
        abierto={porRetirar !== null}
        titulo={`Retirar a ${porRetirar ? nombreCompleto(porRetirar) : ''} de la categoría ${categoria.anio}`}
        textoConfirmar="Retirar de la categoría"
        peligro
        alConfirmar={() => void retirar()}
        alCancelar={() => setPorRetirar(null)}
      >
        <p>
          Dejará de entrenar esta categoría y de dirigir sus equipos. Conserva su rol en el club y las demás
          categorías que tenga.
        </p>
      </DialogoConfirmacion>
    </Tarjeta>
  );
}
