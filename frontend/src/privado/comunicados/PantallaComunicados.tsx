import { useState } from 'react';
import { Link, useLocation, useParams } from 'react-router-dom';
import { api } from '../../compartido/api/cliente';
import { mensajeDe } from '../../compartido/api/errores';
import {
  conIdentificador,
  type AvisoRecibidoDto,
  type ContadorDeCampanaDto,
  type ElementoDeLista,
  type PaginaDeCampanaDto,
} from '../../compartido/api/tiposCampana';
import type {
  ComunicadoPublicadoDto,
  ComunicadoRecibidoDto,
  PaginaDePublicados,
} from '../../compartido/api/tiposComunicados';
import type { Rol } from '../../compartido/api/tipos';
import { Aviso } from '../../compartido/componentes/Aviso';
import { useClub } from '../contextoClub';
import { useContadorDelClub } from './ContadorComunicados';
import { DetallePublicado, DetalleRecibido } from './DetalleComunicado';
import { ListaPublicados } from './ListaPublicados';
import { ListaRecibidos } from './ListaRecibidos';
import { puedePublicarComunicados, textosComunicados } from './textos';
import { useListaPaginada } from './useListaPaginada';

/**
 * La pantalla de comunicados, con dos caras según el rol (spec 011, research §12): la cuenta de un
 * jugador ve las novedades, comunicados y avisos mezclados, con leído y sin leer (spec 012); el
 * PRESIDENTE, el DIRECTIVO y el ENTRENADOR ven
 * lo publicado, con "Nuevo comunicado" y "Retirar". La ruta con un identificador abre el detalle: en
 * escritorio al lado de la lista y en el teléfono en lugar de ella. Mantiene la lista al día con la
 * respuesta de cada lectura o retiro, sin volver a pedirla.
 */
export function PantallaComunicados() {
  const { club } = useClub();
  const { comunicadoId } = useParams();
  const { state } = useLocation();
  const aviso = (state as { aviso?: string } | null)?.aviso;

  return (
    <>
      {aviso && <Aviso tono="exito">{aviso}</Aviso>}
      {club.miRol === 'JUGADOR' ? (
        <CaraDeLaFamilia clubId={club.clubId} comunicadoId={comunicadoId} />
      ) : (
        <CaraDelEquipo clubId={club.clubId} rol={club.miRol} comunicadoId={comunicadoId} />
      )}
    </>
  );
}

function CaraDeLaFamilia({ clubId, comunicadoId }: { clubId: string; comunicadoId: string | undefined }) {
  const contador = useContadorDelClub();
  const [marcando, setMarcando] = useState(false);
  const [errorAlMarcar, setErrorAlMarcar] = useState<string>();
  const lista = useListaPaginada<ElementoDeLista, PaginaDeCampanaDto>(
    `/api/clubes/${clubId}/campana`,
    (pagina) => contador.fijar(pagina.sinLeer),
    (pagina) => pagina.elementos.map(conIdentificador),
  );

  function alLeer(leido: ComunicadoRecibidoDto, estabaSinLeer: boolean) {
    lista.reemplazar(conIdentificador({ tipo: 'COMUNICADO', comunicado: leido }));
    if (estabaSinLeer) {
      contador.restarUno();
    }
  }

  function alLeerAviso(leido: AvisoRecibidoDto, estabaSinLeer: boolean) {
    lista.reemplazar(conIdentificador({ tipo: 'AVISO', aviso: leido }));
    if (estabaSinLeer) {
      contador.restarUno();
    }
  }

  async function marcarTodos() {
    setMarcando(true);
    setErrorAlMarcar(undefined);
    try {
      const respuesta = await api.post<ContadorDeCampanaDto>(`/api/clubes/${clubId}/campana/lectura-total`);
      contador.fijar(respuesta.sinLeer);
      lista.items.forEach((item) => {
        if (item.comunicado && !item.comunicado.leido) {
          lista.reemplazar({ ...item, comunicado: { ...item.comunicado, leido: true } });
        }

        if (item.aviso && !item.aviso.leido) {
          lista.reemplazar({ ...item, aviso: { ...item.aviso, leido: true } });
        }
      });
    } catch (fallo) {
      setErrorAlMarcar(mensajeDe(fallo));
    } finally {
      setMarcando(false);
    }
  }

  return (
    <div className={`comunicados-disposicion${comunicadoId ? ' con-detalle' : ''}`}>
      <section className="comunicados-lista" aria-label="Novedades">
        <div className="cabecera">
          <h1>{textosComunicados.tituloFamilia}</h1>
        </div>
        {(lista.error ?? errorAlMarcar) && <Aviso tono="error">{lista.error ?? errorAlMarcar}</Aviso>}
        {lista.cargando && <p className="texto-suave">Cargando…</p>}
        {!lista.cargando && (
          <ListaRecibidos
            clubId={clubId}
            elementos={lista.items}
            abierto={comunicadoId}
            sinLeer={contador.sinLeer}
            hayMas={lista.hayMas}
            cargandoMas={lista.cargandoMas}
            marcandoTodos={marcando}
            alVerMas={lista.verMas}
            alMarcarTodos={() => void marcarTodos()}
            alLeerAviso={alLeerAviso}
          />
        )}
      </section>
      {comunicadoId && (
        <section className="comunicados-detalle" aria-label="Comunicado">
          <DetalleRecibido clubId={clubId} comunicadoId={comunicadoId} alLeer={alLeer} />
        </section>
      )}
    </div>
  );
}

function CaraDelEquipo({
  clubId,
  rol,
  comunicadoId,
}: {
  clubId: string;
  rol: Rol;
  comunicadoId: string | undefined;
}) {
  const lista = useListaPaginada<ComunicadoPublicadoDto, PaginaDePublicados>(`/api/clubes/${clubId}/comunicados`);
  const abierto = lista.items.find((item) => item.id === comunicadoId);

  return (
    <div className={`comunicados-disposicion${comunicadoId ? ' con-detalle' : ''}`}>
      <section className="comunicados-lista" aria-label="Lista de comunicados">
        <div className="cabecera">
          <h1>{textosComunicados.tituloEquipo}</h1>
          {puedePublicarComunicados(rol) && (
            <Link className="boton boton-principal" to={`/club/${clubId}/comunicados/nuevo`}>
              {textosComunicados.nuevo}
            </Link>
          )}
        </div>
        {lista.error && <Aviso tono="error">{lista.error}</Aviso>}
        {lista.cargando && <p className="texto-suave">Cargando…</p>}
        {!lista.cargando && (
          <ListaPublicados
            clubId={clubId}
            rol={rol}
            comunicados={lista.items}
            abierto={comunicadoId}
            hayMas={lista.hayMas}
            cargandoMas={lista.cargandoMas}
            alVerMas={lista.verMas}
            alRetirar={lista.reemplazar}
          />
        )}
      </section>
      {comunicadoId && (
        <section className="comunicados-detalle" aria-label="Comunicado">
          <DetallePublicado clubId={clubId} comunicado={abierto} cargando={lista.cargando} />
        </section>
      )}
    </div>
  );
}
