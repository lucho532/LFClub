import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { api } from '../../compartido/api/cliente';
import { mensajeDe } from '../../compartido/api/errores';
import type { ActividadDeAvisoDto, AvisoRecibidoDto } from '../../compartido/api/tiposCampana';
import { Aviso } from '../../compartido/componentes/Aviso';
import { fechaYHora } from '../../compartido/formato';
import { AvisoNoDisponible } from './AvisoNoDisponible';
import { textosComunicados } from './textos';

interface Props {
  clubId: string;
  aviso: AvisoRecibidoDto;
  /** Recibe el aviso ya leído y si estaba sin leer antes de tocarlo, para bajar el contador. */
  alLeer: (aviso: AvisoRecibidoDto, estabaSinLeer: boolean) => void;
}

/** A dónde lleva un aviso: el detalle de la actividad, con las rutas de siempre. */
function rutaDeActividad(clubId: string, actividad: ActividadDeAvisoDto): string {
  const carpeta = actividad.tipo === 'ENTRENAMIENTO' ? 'entrenamientos' : 'partidos';
  return `/club/${clubId}/${carpeta}/${actividad.id}`;
}

/**
 * Una fila de aviso en la campana de la familia (spec 012, RF-012 a RF-017 y RF-021). Es una línea
 * corta escrita por el sistema: sin título ni autor, con la fecha, la palabra "Aviso" y "Sin leer" o
 * "Leído" en texto, y en negrita lo que está sin leer. Al tocarla la marca leída para toda la cuenta
 * y, si la actividad sigue disponible, abre su detalle; si no, la fila queda "Leído" y debajo
 * aparece el mensaje de que ya no está disponible, sin navegar. El texto se pinta como texto, nunca
 * como HTML. No decide la disponibilidad: la trae la respuesta de la lectura.
 */
export function FilaDeAviso({ clubId, aviso, alLeer }: Props) {
  const navegar = useNavigate();
  const [abriendo, setAbriendo] = useState(false);
  const [noDisponible, setNoDisponible] = useState(false);
  const [error, setError] = useState<string>();

  async function abrir() {
    if (abriendo) {
      return;
    }

    setAbriendo(true);
    setError(undefined);
    try {
      const leido = await api.put<AvisoRecibidoDto>(`/api/clubes/${clubId}/campana/avisos/${aviso.id}/lectura`, {});
      alLeer(leido, !aviso.leido);
      if (leido.actividad.disponible) {
        navegar(rutaDeActividad(clubId, leido.actividad));
      } else {
        setNoDisponible(true);
      }
    } catch (fallo) {
      setError(mensajeDe(fallo));
    } finally {
      setAbriendo(false);
    }
  }

  return (
    <li className={`comunicado-fila aviso-fila${aviso.leido ? '' : ' sin-leer'}`}>
      <button type="button" className="aviso-texto" disabled={abriendo} onClick={() => void abrir()}>
        {aviso.texto}
      </button>
      <div className="comunicado-datos">
        <span className="etiqueta-leido tipo-aviso">{textosComunicados.etiquetaAviso}</span>
        <span className={`etiqueta-leido ${aviso.leido ? 'leido' : 'no-leido'}`}>
          {aviso.leido ? textosComunicados.leido : textosComunicados.sinLeer}
        </span>
        <span className="texto-suave">{fechaYHora(aviso.generadoEn)}</span>
      </div>
      {noDisponible && <AvisoNoDisponible />}
      {error && <Aviso tono="error">{error}</Aviso>}
    </li>
  );
}
