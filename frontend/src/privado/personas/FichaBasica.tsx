import { Link, useParams } from 'react-router-dom';
import { api } from '../../compartido/api/cliente';
import { ErrorApi } from '../../compartido/api/errores';
import type { CategoriaDeFichaBasicaDto, FichaBasicaDto } from '../../compartido/api/tiposPersonas';
import { useCarga } from '../../compartido/api/useCarga';
import { Aviso } from '../../compartido/componentes/Aviso';
import { Tarjeta } from '../../compartido/componentes/Tarjeta';
import { useVolver } from '../../compartido/navegacion/useVolver';
import { useClub } from '../contextoClub';
import { FotoPersona } from './FotoPersona';

const NOMBRE_DEL_ROL: Record<FichaBasicaDto['rol'], string> = {
  PRESIDENTE: 'Presidente',
  DIRECTIVO: 'Directivo',
  ENTRENADOR: 'Entrenador',
};

/** "Categoría 2014 · A, B", o solo "Categoría 2014" si entrena la categoría en general. */
export function textoDeCategoria(categoria: CategoriaDeFichaBasicaDto): string {
  return categoria.equipos.length > 0
    ? `Categoría ${categoria.anio} · ${categoria.equipos.join(', ')}`
    : `Categoría ${categoria.anio}`;
}

/**
 * Ficha básica de un presidente, directivo o entrenador del club (spec 008, historia 1): su foto de
 * perfil o sus iniciales en grande, su nombre, su rol y las categorías y equipos que entrena en este
 * club. La abre cualquier rol del club, también la familia. No muestra documento, fecha de
 * nacimiento ni contacto: la API no los entrega. Sin categorías, un entrenador dice que todavía no
 * tiene, y a un presidente o directivo no se le pinta la sección (RF-006). Si la ficha no existe o
 * es de otro club, dice lo mismo que si no existiera. Volver lleva a la pantalla desde la que se
 * pulsó el nombre o, si se abrió la dirección directamente, al inicio.
 */
export function FichaBasica() {
  const { club } = useClub();
  const { usuarioRolId = '' } = useParams();
  const carga = useCarga(`${club.clubId}/personas/${usuarioRolId}`, () =>
    api.get<FichaBasicaDto>(`/api/clubes/${club.clubId}/personas/${usuarioRolId}`),
  );
  const ficha = carga.datos;
  const noExiste = carga.fallo instanceof ErrorApi && carga.fallo.status === 404;
  const volver = useVolver({ ruta: `/club/${club.clubId}`, texto: 'Inicio' });

  return (
    <>
      <Link to={volver.ruta}>← {volver.texto}</Link>
      {noExiste ? (
        <Aviso tono="aviso">No encontramos esta ficha.</Aviso>
      ) : (
        carga.error && <Aviso tono="error">{carga.error}</Aviso>
      )}
      {!ficha && carga.cargando && <p className="texto-suave">Cargando…</p>}
      {ficha && (
        <>
          <div className="foto-de-ficha">
            <FotoPersona clubId={club.clubId} persona={ficha} tamano="grande" />
            <div className="columna ficha-basica-nombre">
              <h1 className="nombre-persona">
                {ficha.nombres} {ficha.apellidos}
              </h1>
              <p className="texto-suave">{NOMBRE_DEL_ROL[ficha.rol]}</p>
            </div>
          </div>
          {ficha.categorias.length > 0 ? (
            <Tarjeta titulo="Categorías que entrena">
              <ul className="lista-de-inicio">
                {ficha.categorias.map((categoria) => (
                  <li key={categoria.anio}>{textoDeCategoria(categoria)}</li>
                ))}
              </ul>
            </Tarjeta>
          ) : (
            ficha.rol === 'ENTRENADOR' && (
              <Tarjeta titulo="Categorías que entrena">
                <p className="texto-suave">Todavía no tiene categorías asignadas.</p>
              </Tarjeta>
            )
          )}
        </>
      )}
    </>
  );
}
