import { api } from '../../compartido/api/cliente';
import type { ResumenClubDto } from '../../compartido/api/tiposJugadores';
import { useCarga } from '../../compartido/api/useCarga';
import { Aviso } from '../../compartido/componentes/Aviso';
import { Tarjeta } from '../../compartido/componentes/Tarjeta';

const CIFRAS: { clave: keyof ResumenClubDto; nombre: string }[] = [
  { clave: 'jugadores', nombre: 'Jugadores' },
  { clave: 'equipos', nombre: 'Equipos' },
  { clave: 'entrenadores', nombre: 'Entrenadores' },
  { clave: 'categorias', nombre: 'Categorías' },
];

/**
 * Resumen del club en el inicio del presidente y de los directivos (spec 007, RF-011): cuántos
 * jugadores, equipos, entrenadores y categorías tiene hoy. Las cuentas las hace la API en cada
 * consulta; un club nuevo ve las cuatro en cero.
 */
export function ResumenDelClub({ clubId }: { clubId: string }) {
  const resumen = useCarga(`${clubId}/resumen`, () => api.get<ResumenClubDto>(`/api/clubes/${clubId}/resumen`));

  return (
    <Tarjeta titulo="Resumen del club">
      {resumen.error && <Aviso tono="error">{resumen.error}</Aviso>}
      {!resumen.datos && resumen.cargando && <p className="texto-suave">Cargando…</p>}
      {resumen.datos && (
        <dl className="cifras">
          {CIFRAS.map(({ clave, nombre }) => (
            <div key={clave} className="cifra">
              <dt>{nombre}</dt>
              <dd>{resumen.datos?.[clave]}</dd>
            </div>
          ))}
        </dl>
      )}
    </Tarjeta>
  );
}
