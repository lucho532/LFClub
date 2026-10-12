import { useState } from 'react';
import { api } from '../../compartido/api/cliente';
import type { GoleadoresDto } from '../../compartido/api/tiposEstadisticas';
import { useCarga } from '../../compartido/api/useCarga';
import { Aviso } from '../../compartido/componentes/Aviso';
import { Campo } from '../../compartido/componentes/Campo';
import { NombrePersona } from '../../compartido/componentes/NombrePersona';
import { Tarjeta } from '../../compartido/componentes/Tarjeta';
import { useClub } from '../contextoClub';

/**
 * Los goleadores del club por categoría (spec 009, RF-029): un selector con las categorías que
 * tienen goles (la primera elegida al entrar) y la tabla de posición, nombre y goles. Los goles
 * cuentan para la categoría con la que se marcaron, también los de jugadores retirados. Solo
 * muestra nombre y goles: la API no entrega más.
 */
export function Goleadores() {
  const { club } = useClub();
  const base = `/api/clubes/${club.clubId}/goleadores`;
  const categorias = useCarga(base, () => api.get<GoleadoresDto>(base));
  const [elegida, setElegida] = useState<string | null>(null);
  const categoriaId = elegida ?? categorias.datos?.categoriasConGoles[0]?.categoriaId ?? null;
  const goleadores = useCarga(`${base}/${categoriaId}`, () =>
    categoriaId ? api.get<GoleadoresDto>(`${base}?categoriaId=${categoriaId}`) : Promise.resolve(null),
  );
  const lista = goleadores.datos?.goleadores ?? [];

  return (
    <>
      <div className="cabecera">
        <h1>Goleadores</h1>
      </div>
      {categorias.error && <Aviso tono="error">{categorias.error}</Aviso>}
      {categorias.cargando && !categorias.datos && <p className="texto-suave">Cargando…</p>}
      {categorias.datos?.categoriasConGoles.length === 0 && (
        <p className="texto-suave">Todavía no hay goles registrados.</p>
      )}
      {categoriaId && categorias.datos && (
        <Tarjeta>
          <Campo
            etiqueta="Categoría"
            valor={categoriaId}
            alCambiar={setElegida}
            opciones={categorias.datos.categoriasConGoles.map((categoria) => ({
              valor: categoria.categoriaId,
              texto: `Categoría ${categoria.anio}`,
            }))}
          />
          {goleadores.error && <Aviso tono="error">{goleadores.error}</Aviso>}
          {lista.length === 0 && !goleadores.cargando && (
            <p className="texto-suave">Todavía no hay goles registrados.</p>
          )}
          {lista.length > 0 && (
            <ol className="lista-goleadores">
              {lista.map((goleador, indice) => (
                <li key={`${goleador.nombres} ${goleador.apellidos} ${indice}`}>
                  <span className="texto-suave">{indice + 1}.</span>
                  <NombrePersona
                    nombre={`${goleador.nombres} ${goleador.apellidos}`}
                    enlace={goleador.enlace ?? null}
                    clubId={club.clubId}
                    textoVolver="Goleadores"
                  />
                  <strong>{goleador.goles === 1 ? '1 gol' : `${goleador.goles} goles`}</strong>
                </li>
              ))}
            </ol>
          )}
        </Tarjeta>
      )}
    </>
  );
}
