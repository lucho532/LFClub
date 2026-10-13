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
import { FondoDeBalones } from './FondoDeBalones';

/**
 * Inicio de sesión con correo o documento. No ofrece registrarse: a la plataforma solo se entra
 * por invitación (historia 2, escenario 4).
 */
export function Entrar() {
  const { estado, sesion, iniciar } = useSesion();
  const navegar = useNavigate();
  const ubicacion = useLocation();
  const estadoDeLaUbicacion = ubicacion.state as { desde?: string; aviso?: string } | null;
  const destino = estadoDeLaUbicacion?.desde ?? '/';
  // Lo deja la pantalla de eliminar la cuenta al volver aquí (spec 014).
  const avisoDeLlegada = estadoDeLaUbicacion?.aviso ?? null;

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
    <DisposicionCuenta titulo="Iniciar sesión" fondo={<FondoDeBalones />}
    >
      <form className="columna" onSubmit={enviar}>
        {avisoDeLlegada && !error && <Aviso tono="exito">{avisoDeLlegada}</Aviso>}
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
        <div className="pie-entrar texto-pequeno">
          <Link to="/privacidad">Política de privacidad</Link>
          <Link to="/eliminar-cuenta">Eliminar mi cuenta</Link>
        </div>
      </form>
    </DisposicionCuenta>
  );
}
