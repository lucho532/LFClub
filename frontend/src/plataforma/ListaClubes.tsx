import { useState } from 'react';
import { Link } from 'react-router-dom';
import { api } from '../compartido/api/cliente';
import type { ClubResumenDto } from '../compartido/api/tipos';
import { useCarga } from '../compartido/api/useCarga';
import { Aviso } from '../compartido/componentes/Aviso';
import { Boton } from '../compartido/componentes/Boton';
import { EtiquetaEstado, EtiquetaEstadoClub, EtiquetaEstadoDePagoClub } from '../compartido/componentes/EtiquetaEstado';
import { Tabla, type Columna } from '../compartido/componentes/Tabla';
import { Tarjeta } from '../compartido/componentes/Tarjeta';
import { Escudo, IdentidadClub } from '../compartido/tema/IdentidadClub';
import { formatoMoneda } from '../privado/pagos/formatoMoneda';
import { FormularioCrearClub } from './FormularioCrearClub';

const COLUMNAS: Columna<ClubResumenDto>[] = [
  {
    titulo: 'Club',
    celda: (club) => (
      <IdentidadClub identidad={club.identidad} className="fila">
        <Escudo identidad={club.identidad} nombre={club.nombre} />
        <Link to={`/plataforma/clubes/${club.clubId}`}>{club.nombre}</Link>
      </IdentidadClub>
    ),
  },
  { titulo: 'Estado', celda: (club) => <EtiquetaEstadoClub estado={club.estado} /> },
  {
    titulo: 'Presidente',
    celda: (club) =>
      club.presidenteRegistrado ? (
        <EtiquetaEstado tono="exito" texto="Registrado" />
      ) : (
        <EtiquetaEstado tono="aviso" texto="Presidente sin registrar" />
      ),
  },
  {
    titulo: 'Pago',
    celda: (club) => (
      <span className="fila">
        <EtiquetaEstadoDePagoClub estado={club.estadoDePago} />
        {club.valorAdeudado > 0 && <strong>{formatoMoneda(club.valorAdeudado)}</strong>}
      </span>
    ),
  },
  { titulo: 'Jugadores', celda: (club) => club.integrantes.jugadores },
  { titulo: 'Entrenadores', celda: (club) => club.integrantes.entrenadores },
  { titulo: 'Directivos', celda: (club) => club.integrantes.directivos },
  { titulo: 'Presidentes', celda: (club) => club.integrantes.presidentes },
  {
    titulo: 'Total',
    celda: (club) => {
      const { jugadores, entrenadores, directivos, presidentes } = club.integrantes;
      return <strong>{jugadores + entrenadores + directivos + presidentes}</strong>;
    },
  },
];

/** Lista de todos los clubes de la plataforma y acceso a crear uno. */
export function ListaClubes() {
  const { datos, error, cargando } = useCarga('clubes', () => api.get<ClubResumenDto[]>('/api/plataforma/clubes'));
  const [creando, setCreando] = useState(false);

  return (
    <>
      <div className="cabecera">
        <h1>Clubes</h1>
        {!creando && <Boton onClick={() => setCreando(true)}>Crear club</Boton>}
      </div>

      {creando && <FormularioCrearClub alCancelar={() => setCreando(false)} />}

      <Tarjeta>
        {error && <Aviso tono="error">{error}</Aviso>}
        {cargando && <p className="texto-suave">Cargando…</p>}
        {datos && (
          <Tabla
            descripcion="Clubes de la plataforma"
            columnas={COLUMNAS}
            filas={datos}
            clave={(club) => club.clubId}
            vacio="Todavía no hay ningún club. Crea el primero con «Crear club»."
          />
        )}
      </Tarjeta>
    </>
  );
}
