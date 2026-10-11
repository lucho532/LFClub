import { useState } from 'react';
import { Link, Navigate, useLocation } from 'react-router-dom';
import { api } from '../../compartido/api/cliente';
import type { Rol } from '../../compartido/api/tipos';
import type { CategoriaDto } from '../../compartido/api/tiposCategorias';
import { useCarga } from '../../compartido/api/useCarga';
import { Aviso } from '../../compartido/componentes/Aviso';
import { EtiquetaEstado } from '../../compartido/componentes/EtiquetaEstado';
import { NombrePersona } from '../../compartido/componentes/NombrePersona';
import { Tabla, type Columna } from '../../compartido/componentes/Tabla';
import { Tarjeta } from '../../compartido/componentes/Tarjeta';
import { useClub } from '../contextoClub';
import { AccionesDeCategoria } from './AccionesDeCategoria';
import { FormularioCrearCategoria } from './FormularioCrearCategoria';
import { ListasDelClub } from './ListasDelClub';
import { nombreCompleto, puedeVerCategorias } from './textos';
import type { Mensaje } from './useAccion';

/**
 * Apartado "Categorías" del club. El presidente las gestiona, el directivo las consulta todas y
 * el entrenador ve solo las que tiene asignadas. La API rechaza a cualquier otro rol; aquí
 * simplemente no se le muestra la pantalla. El nombre de cada entrenador abre su ficha básica
 * (spec 008).
 */
export function Categorias() {
  const { club } = useClub();

  if (!puedeVerCategorias(club.miRol)) {
    return <Navigate to={`/club/${club.clubId}`} replace />;
  }

  return <ApartadoCategorias clubId={club.clubId} miRol={club.miRol} />;
}

const VACIO: Record<string, string> = {
  PRESIDENTE: 'Tu club todavía no tiene categorías. Crea la primera con el año de nacimiento de sus jugadores.',
  DIRECTIVO: 'El club todavía no tiene categorías.',
  ENTRENADOR: 'Todavía no tienes categorías asignadas.',
};

/** Carga las categorías y compone el apartado; cada acción recarga lo que le afecta. */
function ApartadoCategorias({ clubId, miRol }: { clubId: string; miRol: Rol }) {
  const esPresidente = miRol === 'PRESIDENTE';
  const veListas = miRol !== 'ENTRENADOR';
  const avisoDeEntrada = (useLocation().state as { aviso?: string } | null)?.aviso;
  const [mensaje, setMensaje] = useState<Mensaje | null>(avisoDeEntrada ? { tono: 'aviso', texto: avisoDeEntrada } : null);
  const [version, setVersion] = useState(0);
  const categorias = useCarga(`${clubId}/categorias`, () => api.get<CategoriaDto[]>(`/api/clubes/${clubId}/categorias`));

  function recargar() {
    categorias.recargar();
    setVersion((numero) => numero + 1);
  }

  const columnas: Columna<CategoriaDto>[] = [
    {
      titulo: 'Categoría',
      celda: (categoria) => (
        <Link to={`/club/${clubId}/categorias/${categoria.categoriaId}`}>
          <strong>Categoría {categoria.anio}</strong>
        </Link>
      ),
    },
    {
      titulo: 'Estado',
      celda: (categoria) =>
        categoria.activa ? (
          <EtiquetaEstado tono="exito" texto="Activa" />
        ) : (
          <EtiquetaEstado tono="neutro" texto="Inactiva" />
        ),
    },
    { titulo: 'Jugadores', celda: (categoria) => categoria.numeroJugadores },
    {
      titulo: 'Equipos',
      celda: (categoria) => categoria.equipos.map((equipo) => equipo.nombre).join(', ') || 'Sin equipos',
    },
    {
      titulo: 'Entrenadores',
      celda: (categoria) =>
        categoria.entrenadores.length === 0
          ? 'Sin entrenador'
          : categoria.entrenadores.map((entrenador, indice) => (
              <span key={entrenador.usuarioRolId}>
                {indice > 0 && ', '}
                <NombrePersona
                  nombre={nombreCompleto(entrenador)}
                  enlace={{ usuarioRolId: entrenador.usuarioRolId, tipo: 'BASICA' }}
                  clubId={clubId}
                  textoVolver="Categorías"
                />
              </span>
            )),
    },
  ];

  if (esPresidente) {
    columnas.push({
      titulo: 'Acciones',
      celda: (categoria) => (
        <AccionesDeCategoria
          clubId={clubId}
          categoria={categoria}
          alTerminar={(resultado) => {
            setMensaje(resultado);
            recargar();
          }}
        />
      ),
    });
  }

  return (
    <>
      <div className="cabecera">
        <h1>Categorías</h1>
      </div>

      {esPresidente && <FormularioCrearCategoria clubId={clubId} alCrear={recargar} />}

      <Tarjeta titulo={veListas ? 'Categorías del club' : 'Tus categorías'}>
        {mensaje && <Aviso tono={mensaje.tono}>{mensaje.texto}</Aviso>}
        {categorias.error && <Aviso tono="error">{categorias.error}</Aviso>}
        {!categorias.datos && categorias.cargando && <p className="texto-suave">Cargando…</p>}
        {categorias.datos && (
          <Tabla
            descripcion="Categorías, por año de nacimiento"
            columnas={columnas}
            filas={categorias.datos}
            clave={(categoria) => categoria.categoriaId}
            vacio={VACIO[miRol]}
          />
        )}
      </Tarjeta>

      {veListas && (
        <ListasDelClub clubId={clubId} esPresidente={esPresidente} version={version} alCambiar={recargar} />
      )}
    </>
  );
}
