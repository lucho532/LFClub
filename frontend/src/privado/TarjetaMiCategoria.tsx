import { api } from '../compartido/api/cliente';
import type { MiCategoriaDto } from '../compartido/api/tiposCategorias';
import { useCarga } from '../compartido/api/useCarga';
import { Aviso } from '../compartido/componentes/Aviso';
import { NombrePersona } from '../compartido/componentes/NombrePersona';
import { Tarjeta } from '../compartido/componentes/Tarjeta';
import { FotoPersona } from './personas/FotoPersona';

/**
 * Tarjeta "Mi categoría" del inicio del club, solo para la cuenta de un jugador: su categoría, los
 * equipos en los que está y quiénes lo entrenan, con el equipo que dirige cada uno (RF-035). Cada
 * entrenador sale con su foto de perfil en miniatura o sus iniciales, y su nombre abre su ficha
 * básica (spec 008, historia 3). Del entrenador no se muestra nada más: la API no lo entrega.
 */
export function TarjetaMiCategoria({ clubId }: { clubId: string }) {
  const mia = useCarga(`${clubId}/mi-categoria`, () => api.get<MiCategoriaDto>(`/api/clubes/${clubId}/mi-categoria`));
  const categoria = mia.datos?.categoria;

  return (
    <Tarjeta titulo="Mi categoría">
      {mia.error && <Aviso tono="error">{mia.error}</Aviso>}
      {!mia.datos && mia.cargando && <p className="texto-suave">Cargando…</p>}
      {mia.datos && !categoria && (
        <p>
          Todavía no tienes categoría asignada. El club te ubicará en una; no tienes que hacer nada.
        </p>
      )}
      {categoria && (
        <>
          <p>
            <strong>Categoría {categoria.anio}</strong>
          </p>
          <p>
            {categoria.equipos.length === 0
              ? 'Sin equipo por ahora.'
              : `${categoria.equipos.length === 1 ? 'Equipo' : 'Equipos'}: ${categoria.equipos.join(', ')}`}
          </p>
          {categoria.entrenadores.length === 0 ? (
            <p className="texto-suave">La categoría todavía no tiene entrenador.</p>
          ) : (
            <>
              <p className="texto-suave">{categoria.entrenadores.length === 1 ? 'Entrenador' : 'Entrenadores'}</p>
              <ul className="lista-de-entrenadores">
                {categoria.entrenadores.map((entrenador) => (
                  <li key={entrenador.usuarioRolId}>
                    <FotoPersona clubId={clubId} persona={entrenador} tamano="miniatura" />
                    <span>
                      <NombrePersona
                        nombre={`${entrenador.nombres} ${entrenador.apellidos}`}
                        enlace={{ usuarioRolId: entrenador.usuarioRolId, tipo: 'BASICA' }}
                        clubId={clubId}
                        textoVolver="Inicio"
                      />
                      {entrenador.equipos.length > 0 && (
                        <span className="texto-suave"> · dirige {entrenador.equipos.join(', ')}</span>
                      )}
                    </span>
                  </li>
                ))}
              </ul>
            </>
          )}
        </>
      )}
    </Tarjeta>
  );
}
