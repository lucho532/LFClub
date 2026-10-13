import { Link } from 'react-router-dom';
import { api } from '../../compartido/api/cliente';
import type { ResumenClubDto } from '../../compartido/api/tiposJugadores';
import { useCarga } from '../../compartido/api/useCarga';
import { Aviso } from '../../compartido/componentes/Aviso';
import { Tarjeta } from '../../compartido/componentes/Tarjeta';

/** `destino` es la pantalla del club donde se ven esas personas o cosas; los equipos y los entrenadores viven dentro de cada categoría. */
const CIFRAS: { clave: keyof ResumenClubDto; nombre: string; destino: string }[] = [
  { clave: 'jugadores', nombre: 'Jugadores', destino: 'jugadores' },
  { clave: 'equipos', nombre: 'Equipos', destino: 'categorias' },
  { clave: 'entrenadores', nombre: 'Entrenadores', destino: 'categorias' },
  { clave: 'categorias', nombre: 'Categorías', destino: 'categorias' },
];

/**
 * Resumen del club en el inicio del presidente y de los directivos (spec 007, RF-011): cuántos
 * jugadores, equipos, entrenadores y categorías tiene hoy. Las cuentas las hace la API en cada
 * consulta; un club nuevo ve las cuatro en cero. Cada cifra es un enlace a la pantalla donde se ven.
 */
export function ResumenDelClub({ clubId }: { clubId: string }) {
  const resumen = useCarga(`${clubId}/resumen`, () => api.get<ResumenClubDto>(`/api/clubes/${clubId}/resumen`));

  return (
    <Tarjeta titulo="Resumen del club">
      {resumen.error && <Aviso tono="error">{resumen.error}</Aviso>}
      {!resumen.datos && resumen.cargando && <p className="texto-suave">Cargando…</p>}
      {resumen.datos && (
        <dl className="cifras">
          {CIFRAS.map(({ clave, nombre, destino }) => (
            <div key={clave} className="cifra">
              <dt>
                <Link to={`/club/${clubId}/${destino}`}>{nombre}</Link>
              </dt>
              <dd>{resumen.datos?.[clave]}</dd>
            </div>
          ))}
        </dl>
      )}
    </Tarjeta>
  );
}
