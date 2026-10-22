import { useState } from 'react';
import { api } from '../compartido/api/cliente';
import { mensajeDe } from '../compartido/api/errores';
import type { CambiarEstadoClubDto, ClubDetalleDto, EstadoClub } from '../compartido/api/tipos';
import { Boton } from '../compartido/componentes/Boton';
import { DialogoConfirmacion } from '../compartido/componentes/DialogoConfirmacion';
import { EtiquetaEstadoClub } from '../compartido/componentes/EtiquetaEstado';
import { Tarjeta } from '../compartido/componentes/Tarjeta';
import { fechaYHora } from '../compartido/formato';
import type { PropsSeccion } from './DetalleClub';
import { DialogoEliminarClub } from './DialogoEliminarClub';

interface Accion {
  hacia: EstadoClub;
  texto: string;
  /** Qué pasa al confirmar, explicado para quien lo decide. */
  efecto: string;
  peligro?: boolean;
}

/** Acciones posibles desde cada estado; las mismas transiciones que permite la API. */
const ACCIONES: Record<EstadoClub, Accion[]> = {
  ACTIVO: [
    {
      hacia: 'SUSPENDIDO',
      texto: 'Suspender',
      efecto:
        'Solo entrará su presidente. El resto de los integrantes verá un aviso de incidencia temporal. No se modifica ningún dato del club.',
    },
    {
      hacia: 'DADO_DE_BAJA',
      texto: 'Dar de baja',
      efecto: 'No entrará nadie del club, tampoco su presidente. Se puede revertir.',
      peligro: true,
    },
  ],
  SUSPENDIDO: [
    {
      hacia: 'ACTIVO',
      texto: 'Levantar la suspensión',
      efecto: 'Todos los integrantes vuelven a entrar y el club queda exactamente como estaba.',
    },
    {
      hacia: 'DADO_DE_BAJA',
      texto: 'Dar de baja',
      efecto: 'No entrará nadie del club, tampoco su presidente. Se puede revertir.',
      peligro: true,
    },
  ],
  DADO_DE_BAJA: [
    {
      hacia: 'ACTIVO',
      texto: 'Revertir la baja',
      efecto: 'El club vuelve a estar activo y todos sus integrantes vuelven a entrar.',
    },
  ],
};

/**
 * Estado de un club: el actual, cuándo cambió y solo las acciones posibles desde él, cada una con
 * confirmación. Eliminar aparece únicamente cuando el club está dado de baja.
 */
export function SeccionEstado({ club, alActualizar }: PropsSeccion) {
  const [accion, setAccion] = useState<Accion | null>(null);
  const [eliminando, setEliminando] = useState(false);
  const [error, setError] = useState<string | undefined>();
  const [enviando, setEnviando] = useState(false);

  async function confirmar() {
    if (!accion) {
      return;
    }

    setError(undefined);
    setEnviando(true);
    try {
      const datos: CambiarEstadoClubDto = { estado: accion.hacia };
      alActualizar(await api.put<ClubDetalleDto>(`/api/plataforma/clubes/${club.clubId}/estado`, datos));
      setAccion(null);
    } catch (fallo) {
      setError(mensajeDe(fallo));
    } finally {
      setEnviando(false);
    }
  }

  return (
    <Tarjeta titulo="Estado">
      <div className="fila">
        <EtiquetaEstadoClub estado={club.estado} />
        {club.estadoCambiadoEn && (
          <span className="texto-suave">Cambió el {fechaYHora(club.estadoCambiadoEn)}</span>
        )}
      </div>

      <div className="fila">
        {ACCIONES[club.estado].map((posible) => (
          <Boton
            key={posible.hacia}
            variante="secundario"
            onClick={() => {
              setError(undefined);
              setAccion(posible);
            }}
          >
            {posible.texto}
          </Boton>
        ))}
        {club.estado === 'DADO_DE_BAJA' && (
          <Boton variante="peligro" onClick={() => setEliminando(true)}>
            Eliminar
          </Boton>
        )}
      </div>

      {club.estado === 'SUSPENDIDO' && club.motivoDeSuspension === 'IMPAGO' && (
        <p className="texto-suave">
          Está suspendido por falta de pago. Si levantas la suspensión, el club queda activo hasta el próximo
          cobro; los cobros vencidos siguen pendientes y se pueden pagar.
        </p>
      )}

      <DialogoConfirmacion
        abierto={accion !== null}
        titulo={`${accion?.texto ?? ''}: ${club.nombre}`}
        textoConfirmar={accion?.texto ?? ''}
        peligro={accion?.peligro}
        cargando={enviando}
        error={error}
        alConfirmar={() => void confirmar()}
        alCancelar={() => setAccion(null)}
      >
        <p>{accion?.efecto}</p>
      </DialogoConfirmacion>

      <DialogoEliminarClub club={club} abierto={eliminando} alCerrar={() => setEliminando(false)} />
    </Tarjeta>
  );
}
