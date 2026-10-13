import { useState, type FormEvent } from 'react';
import { Link, Navigate, useLocation, useNavigate } from 'react-router-dom';
import { api } from '../compartido/api/cliente';
import { mensajeDe } from '../compartido/api/errores';
import type { IniciarSesionDto, TokenSesionDto } from '../compartido/api/tipos';
import { Aviso } from '../compartido/componentes/Aviso';
import { Boton } from '../compartido/componentes/Boton';
import { Campo } from '../compartido/componentes/Campo';
import { useSesion } from '../compartido/sesion/useSesion';
import { DisposicionCuenta } from './DisposicionCuenta';

/**
 * Inicio de sesión con correo o documento. No ofrece registrarse: a la plataforma solo se entra
 * por invitación (historia 2, escenario 4).
 */
export function Entrar() {
  const { estado, sesion, iniciar } = useSesion();
  const navegar = useNavigate();
  const ubicacion = useLocation();
  const destino = (ubicacion.state as { desde?: string } | null)?.desde ?? '/';

  const [identificador, setIdentificador] = useState('');
  const [contrasena, setContrasena] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [enviando, setEnviando] = useState(false);

  if (estado === 'con_sesion' && sesion && !enviando) {
    return <Navigate to={destino} replace />;
  }

  async function enviar(evento: FormEvent) {
    evento.preventDefault();
    setError(null);
    setEnviando(true);
    try {
      const datos: IniciarSesionDto = { identificador, contrasena };
      await iniciar(await api.post<TokenSesionDto>('/api/sesion', datos));
      navegar(destino, { replace: true });
    } catch (fallo) {
      setError(mensajeDe(fallo));
      setEnviando(false);
    }
  }

  return (
    <DisposicionCuenta titulo="Iniciar sesión">
      <form className="columna" onSubmit={enviar}>
        {error && (
          <Aviso tono="error">
            {error} <Link to="/recuperar">Recuperar mi contraseña</Link>
          </Aviso>
        )}
        <Campo
          etiqueta="Correo o documento"
          valor={identificador}
          alCambiar={setIdentificador}
          autoComplete="username"
          autoCapitalize="none"
          required
        />
        <Campo
          etiqueta="Contraseña"
          type="password"
          valor={contrasena}
          alCambiar={setContrasena}
          autoComplete="current-password"
          required
        />
        <Boton type="submit" cargando={enviando} textoCargando="Entrando…">
          Entrar
        </Boton>
        <Link to="/recuperar">Olvidé mi contraseña</Link>
        <Link to="/privacidad">Política de privacidad</Link>
      </form>
    </DisposicionCuenta>
  );
}
