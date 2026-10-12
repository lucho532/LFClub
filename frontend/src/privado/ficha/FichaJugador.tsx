import { useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { api } from '../../compartido/api/cliente';
import { ErrorApi } from '../../compartido/api/errores';
import type { ActualizarFichaDto, FichaJugadorDto } from '../../compartido/api/tiposFicha';
import type { HermanoAgregadoDto } from '../../compartido/api/tiposSesion';
import { useCarga } from '../../compartido/api/useCarga';
import { Aviso } from '../../compartido/componentes/Aviso';
import { Boton } from '../../compartido/componentes/Boton';
import { useVolver } from '../../compartido/navegacion/useVolver';
import { nombreCompleto, puedeVerCategorias } from '../categorias/textos';
import { useClub } from '../contextoClub';
import { SIN_CATEGORIA, textoDeEquipos } from '../jugadores/textos';
import { BotonRetirarseDelClub } from './BotonRetirarseDelClub';
import { DialogoAgregarHermano } from './DialogoAgregarHermano';
import { FichaDeSoloLectura } from './FichaDeSoloLectura';
import { FormularioFicha } from './FormularioFicha';
import { SeccionDocumentos } from './SeccionDocumentos';
import { SeccionEstadisticas } from './SeccionEstadisticas';
import { SeccionFoto } from './SeccionFoto';
import { SeccionIdentidad } from './SeccionIdentidad';
import { UltimoCambio } from './UltimoCambio';
import { textosCarta } from '../carta/textos';

/**
 * Pantalla de la ficha de un jugador, la misma para todos los roles. Carga la ficha y la reparte en
 * secciones; arriba, la foto propia del jugador (spec 007) con su nombre, su categoría, sus equipos
 * y el enlace a su carta (spec 010) al lado. Muestra y permite solo lo que trae la respuesta: los grupos que la API no entrega no
 * se pintan, y los botones dependen de `permisos`, no del rol. Quien no puede ver la ficha recibe
 * de la API el mismo "no encontrado" que si no existiera, y aquí se le dice lo mismo.
 * "Agregar un hermano" y "Retirar del club" solo aparecen en la ficha propia de la cuenta de un
 * jugador (RF-032 y RF-043 de la 006): un presidente, un directivo, un entrenador y la ficha de un
 * hermano no elegido no los muestran, y la API se lo negaría. El enlace de volver lleva a la
 * pantalla desde la que se pulsó el nombre del jugador (`useVolver`), con su nombre.
 */
export function FichaJugador() {
  const { club } = useClub();
  const { usuarioRolId = '' } = useParams();
  const ruta = `/api/clubes/${club.clubId}/jugadores/${usuarioRolId}/ficha`;
  const carga = useCarga(`${club.clubId}/ficha/${usuarioRolId}`, () => api.get<FichaJugadorDto>(ruta));
  const ficha = carga.datos;
  const noLaVe = carga.fallo instanceof ErrorApi && carga.fallo.status === 404;
  const [agregando, setAgregando] = useState(false);
  const [agregado, setAgregado] = useState<HermanoAgregadoDto | null>(null);
  const esLaPropia = club.miRol === 'JUGADOR' && ficha?.usuarioRolId === club.miUsuarioRolId;

  // Se vuelve a la pantalla desde la que se pulsó el nombre. Si no se llegó desde un enlace, quien
  // ve las listas del club vuelve a la categoría del jugador, y la familia, al inicio.
  const volver = useVolver(
    !puedeVerCategorias(club.miRol)
      ? { ruta: `/club/${club.clubId}`, texto: 'Inicio' }
      : ficha?.categoriaId
        ? { ruta: `/club/${club.clubId}/categorias/${ficha.categoriaId}`, texto: `Categoría ${ficha.categoriaAnio}` }
        : { ruta: `/club/${club.clubId}/categorias`, texto: 'Categorías' },
  );

  async function guardar(datos: ActualizarFichaDto): Promise<FichaJugadorDto> {
    const guardada = await api.put<FichaJugadorDto>(ruta, datos);
    carga.fijar(guardada);
    return guardada;
  }

  return (
    <>
      <Link to={volver.ruta}>← {volver.texto}</Link>
      {noLaVe ? (
        <Aviso tono="aviso">Esta ficha no existe o no puedes verla.</Aviso>
      ) : (
        carga.error && <Aviso tono="error">{carga.error}</Aviso>
      )}
      {!ficha && carga.cargando && <p className="texto-suave">Cargando…</p>}
      {ficha && (
        <>
          <SeccionFoto clubId={club.clubId} ficha={ficha} rutaDeLaFicha={ruta} alCambiar={carga.fijar}>
            <span className="texto-suave texto-pequeno">
              {ficha.usuarioRolId === club.miUsuarioRolId ? 'Mi ficha' : 'Ficha del jugador'}
            </span>
            <h1 className="nombre-persona">{nombreCompleto(ficha)}</h1>
            <span>{ficha.categoriaAnio ? `Categoría ${ficha.categoriaAnio}` : SIN_CATEGORIA}</span>
            <span className="texto-suave">{textoDeEquipos(ficha.equipos)}</span>
            <Link to={`/club/${club.clubId}/jugadores/${ficha.usuarioRolId}/carta`}>{textosCarta.verCarta}</Link>
          </SeccionFoto>
          <div className="columna">
            <UltimoCambio cambio={ficha.ultimoCambio} clubId={club.clubId} textoVolver={nombreCompleto(ficha)} />
            {esLaPropia && (
              <div className="fila">
                <Boton variante="secundario" onClick={() => setAgregando(true)}>
                  Agregar un hermano
                </Boton>
                <BotonRetirarseDelClub clubId={club.clubId} ficha={ficha} />
              </div>
            )}
            {agregado && (
              <Aviso tono="exito">
                El ingreso de {agregado.nombres} está pendiente de aprobación del club.
                {agregado.retiraDeOtroClub &&
                  ' Si el club lo aprueba, dejará de estar en el otro club donde está inscrito.'}
              </Aviso>
            )}
          </div>
          {agregando && (
            <DialogoAgregarHermano
              clubId={club.clubId}
              ficha={ficha}
              alAgregar={(hermano) => {
                setAgregado(hermano);
                setAgregando(false);
              }}
              alCancelar={() => setAgregando(false)}
            />
          )}
          <SeccionIdentidad ficha={ficha} rutaDeLaFicha={ruta} alCambiar={carga.fijar} />
          {ficha.permisos.puedeCambiar && ficha.datosClinicos ? (
            <FormularioFicha key={ficha.usuarioRolId} ficha={ficha} alGuardar={guardar} />
          ) : (
            <FichaDeSoloLectura ficha={ficha} />
          )}
          <SeccionEstadisticas rutaDeLaFicha={ruta} />
          {ficha.documentos && (
            <SeccionDocumentos
              rutaDeLaFicha={ruta}
              documentos={ficha.documentos}
              puedeCambiar={ficha.permisos.puedeCambiar}
              alCambiar={carga.fijar}
            />
          )}
        </>
      )}
    </>
  );
}
