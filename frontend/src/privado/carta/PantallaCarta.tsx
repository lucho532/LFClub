import { Link, useParams } from 'react-router-dom';
import { api } from '../../compartido/api/cliente';
import { ErrorApi } from '../../compartido/api/errores';
import type { CartaDto } from '../../compartido/api/tiposCarta';
import { useCarga } from '../../compartido/api/useCarga';
import { Aviso } from '../../compartido/componentes/Aviso';
import { useClub } from '../contextoClub';
import { BotonDescargarCarta } from './BotonDescargarCarta';
import { CartaJugador } from './CartaJugador';
import { FormularioDorsalYPosicion } from './FormularioDorsalYPosicion';
import { FormularioEstadisticas } from './FormularioEstadisticas';
import { TEXTO_DE_RAREZA } from './plano';
import { textosCarta } from './textos';

/**
 * La carta de un jugador (spec 010, historia 3): la carta dibujada con el escudo y los colores de la
 * identidad del club, la rareza escrita y, debajo, según los permisos que da la API, el dorsal y la
 * posición, las estadísticas con su último cambio y la descarga de la imagen. Si no existe o quien
 * pregunta no ve la ficha, dice lo mismo que si no existiera (RF-020).
 */
export function PantallaCarta() {
  const { club } = useClub();
  const { usuarioRolId = '' } = useParams();
  const ruta = `/api/clubes/${club.clubId}/jugadores/${usuarioRolId}/carta`;
  const carga = useCarga(ruta, () => api.get<CartaDto>(ruta));
  const carta = carga.datos;
  const noExiste = carga.fallo instanceof ErrorApi && carga.fallo.status === 404;

  return (
    <>
      <Link to={`/club/${club.clubId}/jugadores/${usuarioRolId}/ficha`}>← Ficha</Link>
      {noExiste ? (
        <Aviso tono="aviso">{textosCarta.noEncontrada}</Aviso>
      ) : (
        carga.error && <Aviso tono="error">{carga.error}</Aviso>
      )}
      {!carta && carga.cargando && <p className="texto-suave">Cargando…</p>}
      {carta && (
        <div className="carta-pantalla">
          <h1 className="nombre-persona">
            {textosCarta.titulo} de {carta.nombres} {carta.apellidos}
          </h1>
          <CartaJugador clubId={club.clubId} carta={carta} identidad={club.identidad} />
          <p className="carta-rareza-texto">
            <strong>{TEXTO_DE_RAREZA[carta.rareza]}</strong>
            <span className="texto-suave texto-pequeno"> · {textosCarta.rareza}</span>
          </p>
          {!carta.estadisticas && <p className="texto-suave carta-rareza-texto">{textosCarta.sinEstadisticas}</p>}
          {carta.permisos.exporta && <BotonDescargarCarta rutaDeLaCarta={ruta} carta={carta} />}
          {carta.permisos.cambiaDorsalYPosicion && (
            <FormularioDorsalYPosicion rutaDeLaCarta={ruta} carta={carta} alGuardar={carga.fijar} />
          )}
          {carta.permisos.cambiaEstadisticas && (
            <FormularioEstadisticas rutaDeLaCarta={ruta} carta={carta} alGuardar={carga.fijar} />
          )}
        </div>
      )}
    </>
  );
}
