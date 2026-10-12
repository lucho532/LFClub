import { useState } from 'react';
import { api } from '../compartido/api/cliente';
import { ErrorApi, mensajeDe } from '../compartido/api/errores';
import type { InvitacionDto, ReenviarInvitacionDto } from '../compartido/api/tipos';
import { Aviso } from '../compartido/componentes/Aviso';
import { Boton } from '../compartido/componentes/Boton';
import { Campo } from '../compartido/componentes/Campo';
import { EtiquetaEstado, EtiquetaEstadoEnvio } from '../compartido/componentes/EtiquetaEstado';
import { Tarjeta } from '../compartido/componentes/Tarjeta';
import { fecha } from '../compartido/formato';
import type { PropsSeccion } from './DetalleClub';

/**
 * Invitación del presidente de un club mientras no se haya usado: reenviarla o corregir su
 * correo, lo que anula el enlace anterior. No se invita a otro presidente a un club que ya existe:
 * el desarrollador solo invita al crear el club.
 */
export function SeccionInvitaciones({ club, recargar }: PropsSeccion) {
  const base = `/api/plataforma/clubes/${club.clubId}/invitaciones`;
  const [corrigiendo, setCorrigiendo] = useState<string | null>(null);
  const [correoCorregido, setCorreoCorregido] = useState('');
  const [ocupado, setOcupado] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [hecho, setHecho] = useState<string | null>(null);

  async function ejecutar(accion: () => Promise<InvitacionDto>) {
    setError(null);
    setHecho(null);
    setOcupado(true);
    try {
      const invitacion = await accion();
      setHecho(
        invitacion.estadoEnvio === 'FALLIDO'
          ? `No se pudo enviar el correo a ${invitacion.correo}. Puedes reenviarlo.`
          : `Invitación enviada a ${invitacion.correo}.`,
      );
      setCorrigiendo(null);
      recargar();
    } catch (fallo) {
      setError(fallo instanceof ErrorApi ? (fallo.errorDe('correo') ?? fallo.title) : mensajeDe(fallo));
    } finally {
      setOcupado(false);
    }
  }

  function reenviar(invitacion: InvitacionDto, correo?: string) {
    const datos: ReenviarInvitacionDto = correo ? { correo } : {};
    return ejecutar(() => api.post<InvitacionDto>(`${base}/${invitacion.invitacionId}/reenvio`, datos));
  }

  return (
    <Tarjeta titulo="Invitaciones">
      {error && <Aviso tono="error">{error}</Aviso>}
      {hecho && <Aviso tono="info">{hecho}</Aviso>}

      {club.invitaciones.length === 0 && <p className="texto-suave">No hay invitaciones pendientes.</p>}

      {club.invitaciones.map((invitacion) => (
        <div key={invitacion.invitacionId} className="columna">
          <div className="fila">
            <strong>{invitacion.correo}</strong>
            <EtiquetaEstadoEnvio estado={invitacion.estadoEnvio} />
            {invitacion.vencida ? (
              <EtiquetaEstado tono="error" texto="Vencida" />
            ) : (
              <span className="texto-suave">Vence el {fecha(invitacion.venceEn)}</span>
            )}
          </div>
          {corrigiendo === invitacion.invitacionId ? (
            <form
              className="fila fila-con-campos"
              onSubmit={(evento) => {
                evento.preventDefault();
                void reenviar(invitacion, correoCorregido);
              }}
            >
              <Campo
                etiqueta="Correo corregido"
                type="email"
                valor={correoCorregido}
                alCambiar={setCorreoCorregido}
                required
              />
              <Boton type="submit" cargando={ocupado}>
                Enviar al correo corregido
              </Boton>
              <Boton variante="secundario" onClick={() => setCorrigiendo(null)} disabled={ocupado}>
                Cancelar
              </Boton>
            </form>
          ) : (
            <div className="fila">
              <Boton variante="secundario" onClick={() => void reenviar(invitacion)} disabled={ocupado}>
                Reenviar
              </Boton>
              <Boton
                variante="secundario"
                disabled={ocupado}
                onClick={() => {
                  setCorreoCorregido(invitacion.correo);
                  setCorrigiendo(invitacion.invitacionId);
                }}
              >
                Corregir correo
              </Boton>
            </div>
          )}
        </div>
      ))}
    </Tarjeta>
  );
}
