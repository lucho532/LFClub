import { useCallback, useState } from 'react';
import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom';
import { ArranqueAnimado } from './arranque/ArranqueAnimado';
import { ProveedorSesion } from './compartido/sesion/ProveedorSesion';
import { RutaProtegida } from './compartido/sesion/RutaProtegida';
import { ProveedorTema } from './compartido/tema/ProveedorTema';
import { Entrar } from './cuenta/Entrar';
import { Invitacion } from './cuenta/Invitacion';
import { MiPerfil } from './cuenta/MiPerfil';
import { Recuperar } from './cuenta/Recuperar';
import { Restablecer } from './cuenta/Restablecer';
import { Inicio } from './Inicio';
import { DetalleClub } from './plataforma/DetalleClub';
import { DisposicionPlataforma } from './plataforma/DisposicionPlataforma';
import { ListaClubes } from './plataforma/ListaClubes';
import { FormularioActividad } from './privado/actividades/FormularioActividad';
import { Calendario } from './privado/calendario/Calendario';
import { Categorias } from './privado/categorias/Categorias';
import { DetalleCategoria } from './privado/categorias/DetalleCategoria';
import { ConfiguracionClub } from './privado/ConfiguracionClub';
import { DisposicionClub } from './privado/DisposicionClub';
import { DetalleEntrenamiento } from './privado/entrenamientos/DetalleEntrenamiento';
import { FichaJugador } from './privado/ficha/FichaJugador';
import { Ingresos } from './privado/ingresos/Ingresos';
import { InicioClub } from './privado/InicioClub';
import { ListaJugadores } from './privado/jugadores/ListaJugadores';
import { DetallePartido } from './privado/partidos/DetallePartido';
import { Partidos } from './privado/partidos/Partidos';
import { FichaBasica } from './privado/personas/FichaBasica';

interface Props {
  /** Si se muestra la animación de arranque encima mientras la aplicación carga (solo en Android). */
  conArranque?: boolean;
}

/** Rutas de la aplicación: las de la tabla "Pantallas" del plan, con la animación de arranque encima. */
export function App({ conArranque = false }: Props) {
  const [arranque, setArranque] = useState(conArranque);
  const terminarArranque = useCallback(() => setArranque(false), []);

  return (
    <ProveedorTema>
      {arranque && <ArranqueAnimado alTerminar={terminarArranque} />}
      <ProveedorSesion>
        <BrowserRouter>
          <Routes>
            <Route path="/" element={<Inicio />} />
            <Route path="/entrar" element={<Entrar />} />
            <Route path="/recuperar" element={<Recuperar />} />
            <Route path="/restablecer" element={<Restablecer />} />
            <Route path="/invitacion" element={<Invitacion />} />

            <Route element={<RutaProtegida quien="cualquiera" />}>
              <Route path="/perfil" element={<MiPerfil />} />
            </Route>

            <Route element={<RutaProtegida quien="desarrollador" />}>
              <Route path="/plataforma" element={<DisposicionPlataforma />}>
                <Route index element={<ListaClubes />} />
                <Route path="clubes/:clubId" element={<DetalleClub />} />
              </Route>
            </Route>

            <Route element={<RutaProtegida quien="integrante" />}>
              <Route path="/club/:clubId" element={<DisposicionClub />}>
                <Route index element={<InicioClub />} />
                <Route path="configuracion" element={<ConfiguracionClub />} />
                <Route path="ingresos" element={<Ingresos />} />
                <Route path="categorias" element={<Categorias />} />
                <Route path="categorias/:categoriaId" element={<DetalleCategoria />} />
                <Route path="jugadores" element={<ListaJugadores />} />
                <Route path="jugadores/:usuarioRolId/ficha" element={<FichaJugador />} />
                <Route path="personas/:usuarioRolId" element={<FichaBasica />} />
                <Route path="calendario" element={<Calendario />} />
                <Route path="actividades/nueva" element={<FormularioActividad />} />
                <Route path="entrenamientos/:entrenamientoId" element={<DetalleEntrenamiento />} />
                <Route path="partidos" element={<Partidos />} />
                <Route path="partidos/:partidoId" element={<DetallePartido />} />
              </Route>
            </Route>

            <Route path="*" element={<Navigate to="/" replace />} />
          </Routes>
        </BrowserRouter>
      </ProveedorSesion>
    </ProveedorTema>
  );
}
