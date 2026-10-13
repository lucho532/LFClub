import { useState, type FormEvent } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { api } from '../compartido/api/cliente';
import { ErrorApi, mensajeDe } from '../compartido/api/errores';
import type { ClubQueImpide, EliminarCuentaDto, ResumenEliminacionCuenta } from '../compartido/api/tiposCuenta';
import { useCarga } from '../compartido/api/useCarga';
import { Aviso } from '../compartido/componentes/Aviso';
import { Boton } from '../compartido/componentes/Boton';
import { BotonRecargar } from '../compartido/componentes/BotonRecargar';
import { BotonTema } from '../compartido/componentes/BotonTema';
import { Campo } from '../compartido/componentes/Campo';
import { useSesion } from '../compartido/sesion/useSesion';
import { JugadoresQueSeEliminan } from './JugadoresQueSeEliminan';
import { QueSeBorraYQueSeConserva } from './QueSeBorraYQueSeConserva';

/** Aviso que `/entrar` muestra al volver de eliminar la cuenta. */
export const AVISO_CUENTA_ELIMINADA = 'Tu cuenta se eliminó';

/** Clubes de un 409 `presidente_unico`, o nulos si el error es otro. */
function clubesDelConflicto(fallo: unknown): ClubQueImpide[] | null {
  if (fallo instanceof ErrorApi && fallo.codigo === 'presidente_unico') {
    return (fallo.extras.clubes as ClubQueImpide[] | undefined) ?? [];
  }

  return null;
}

/**
 * "Eliminar mi cuenta" (/cuenta/eliminar, spec 014): lo que se borra y lo que se conserva, los
 * jugadores que se irían con la cuenta y la contraseña para confirmar. Si la persona es el único
 * presidente de algún club solo ve qué club lo impide. Al terminar limpia la sesión local y vuelve
 * a `/entrar`. La acción destructiva se distingue por su texto, no solo por el color (§24).
 */
export function EliminarCuenta() {
  const { cerrar } = useSesion();
  const navegar = useNavigate();
  const resumen = useCarga<ResumenEliminacionCuenta>('eliminacion', () =>
    api.get<ResumenEliminacionCuenta>('/api/cuenta/eliminacion'),
  );
  const [contrasena, setContrasena] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [bloqueo, setBloqueo] = useState<ClubQueImpide[] | null>(null);
  const [enviando, setEnviando] = useState(false);

  async function eliminar(evento: FormEvent) {
    evento.preventDefault();
    setError(null);
    setEnviando(true);
    try {
      const datos: EliminarCuentaDto = { contrasena };
      await api.post('/api/cuenta/eliminacion', datos);
      // Primero se navega: con la sesión ya vacía la ruta privada mandaría a /entrar sin el aviso.
      navegar('/entrar', { replace: true, state: { aviso: AVISO_CUENTA_ELIMINADA } });
      cerrar();
    } catch (fallo) {
      const clubes = clubesDelConflicto(fallo);
      if (clubes) {
        setBloqueo(clubes);
      } else {
        setError(mensajeDe(fallo));
      }

      setEnviando(false);
    }
  }

  const datos = resumen.datos;
  const impiden = bloqueo ?? (datos && !datos.puedeEliminar ? datos.clubesQueLoImpiden : null);

  return (
    <div className="centrado">
      <header className="columna">
        <div className="cabecera">
          <Link to="/perfil">← Volver</Link>
          <span className="barra-superior-acciones">
            <BotonRecargar />
            <BotonTema />
          </span>
        </div>
        <h1>Eliminar mi cuenta</h1>
      </header>

      <main className="columna">
        {resumen.error && <Aviso tono="error">{resumen.error}</Aviso>}
        {resumen.cargando && !datos && <p className="texto-suave">Cargando…</p>}

        {impiden && impiden.length > 0 && (
          <Aviso tono="aviso">
            <p>
              <strong>No puedes eliminar tu cuenta todavía.</strong> Eres el único presidente de:
            </p>
            <ul>
              {impiden.map((club) => (
                <li key={club.clubId}>
                  {club.clubNombre} — <Link to={`/club/${club.clubId}/ingresos`}>Nombra antes a otro presidente</Link>
                </li>
              ))}
            </ul>
          </Aviso>
        )}

        {datos && (
          <>
            <JugadoresQueSeEliminan jugadores={datos.jugadores} />
            <QueSeBorraYQueSeConserva />
          </>
        )}

        {datos && !impiden && (
          <form className="tarjeta columna" onSubmit={eliminar}>
            <Aviso tono="aviso">
              <strong>No se puede deshacer.</strong> Se eliminan tu cuenta y todo lo que se indica arriba.
            </Aviso>
            {error && <Aviso tono="error">{error}</Aviso>}
            <Campo
              etiqueta="Tu contraseña"
              type="password"
              valor={contrasena}
              alCambiar={setContrasena}
              autoComplete="current-password"
              autoFocus
              required
            />
            <Boton type="submit" variante="peligro" cargando={enviando} textoCargando="Eliminando…">
              Eliminar mi cuenta para siempre
            </Boton>
            <Link to="/perfil">Mejor no, volver a mi perfil</Link>
          </form>
        )}
      </main>
    </div>
  );
}
