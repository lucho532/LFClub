import { useState } from 'react';
import { api } from '../../compartido/api/cliente';
import { ErrorApi, mensajeDe } from '../../compartido/api/errores';
import type { IngresoAprobadoDto, IngresoEnEsperaDto } from '../../compartido/api/tiposIngresos';
import type { Carga } from '../../compartido/api/useCarga';
import { Aviso } from '../../compartido/componentes/Aviso';
import { Boton } from '../../compartido/componentes/Boton';
import { DialogoConfirmacion } from '../../compartido/componentes/DialogoConfirmacion';
import { NombrePersona } from '../../compartido/componentes/NombrePersona';
import { Tarjeta } from '../../compartido/componentes/Tarjeta';
import { dia, fechaYHora, nombreDeRol, nombreDeTipoDocumento } from '../../compartido/formato';

interface Props {
  clubId: string;
  enEspera: Carga<IngresoEnEsperaDto[]>;
  /** Se llama tras aprobar o rechazar (o intentarlo), para recargar las otras listas del apartado. */
  alCambiar: () => void;
}

/** Lo que se le dice al presidente cuando aprobar retira al jugador de otra cuenta en otro club. */
const AVISO_OTRO_CLUB = 'Este documento está activo en otro club. Si apruebas su ingreso, quedará retirado de allí.';

/** La API responde 409 o 404 cuando otra persona se adelantó: el ingreso ya no está pendiente. */
const yaNoEstaPendiente = (fallo: unknown): fallo is ErrorApi =>
  fallo instanceof ErrorApi && (fallo.status === 409 || fallo.status === 404);

/**
 * Sala de espera del club, que solo ve su presidente: las personas pendientes de aprobación, de
 * la más antigua a la más reciente, cada una como una ficha con sus datos. Quien se registra con
 * una invitación no pasa por aquí: llega el jugador que una familia agregó desde la ficha de un
 * hermano, y de cada uno se dice de quién es hermano y si aprobarlo lo retirará de otro club donde
 * está activo con otra cuenta, sin decir cuál. Desde aquí se aprueba o se rechaza un ingreso. Al
 * aprobar no se elige rol: la persona entra siempre como jugador. El nombre del hermano abre su
 * ficha si es un jugador aprobado; el de quien espera es texto, porque su tarjeta ya muestra sus
 * datos y todavía no tiene ficha (spec 008, research §8).
 */
export function SeccionSalaDeEspera({ clubId, enEspera, alCambiar }: Props) {
  const base = `/api/clubes/${clubId}/ingresos`;
  const [porAprobar, setPorAprobar] = useState<IngresoEnEsperaDto | null>(null);
  const [porRechazar, setPorRechazar] = useState<IngresoEnEsperaDto | null>(null);
  const [enviando, setEnviando] = useState(false);
  const [errorDialogo, setErrorDialogo] = useState<string | undefined>();
  const [resultado, setResultado] = useState<{ tono: 'exito' | 'aviso'; texto: string } | null>(null);

  function abrir(fijar: (persona: IngresoEnEsperaDto) => void, persona: IngresoEnEsperaDto) {
    setErrorDialogo(undefined);
    setResultado(null);
    fijar(persona);
  }

  /** Ejecuta la acción del diálogo abierto, lo cierra si terminó y recarga lo que cambió. */
  async function ejecutar(accion: () => Promise<string>) {
    setEnviando(true);
    setErrorDialogo(undefined);
    try {
      setResultado({ tono: 'exito', texto: await accion() });
      setPorAprobar(null);
      setPorRechazar(null);
    } catch (fallo) {
      if (yaNoEstaPendiente(fallo)) {
        setResultado({ tono: 'aviso', texto: fallo.title });
        setPorAprobar(null);
        setPorRechazar(null);
      } else {
        setErrorDialogo(mensajeDe(fallo));
      }
    } finally {
      setEnviando(false);
      enEspera.recargar();
      alCambiar();
    }
  }

  const aprobar = (persona: IngresoEnEsperaDto) =>
    ejecutar(async () => {
      // Sin cuerpo: la API deja siempre a la persona como jugador.
      const aprobado = await api.post<IngresoAprobadoDto>(`${base}/${persona.usuarioRolId}/aprobacion`);
      return `${aprobado.nombres} ${aprobado.apellidos} ya entra al club como ${nombreDeRol(aprobado.rolDeIngreso).toLowerCase()}.`;
    });

  const rechazar = (persona: IngresoEnEsperaDto) =>
    ejecutar(async () => {
      await api.post(`${base}/${persona.usuarioRolId}/rechazo`);
      return `Se rechazó el ingreso de ${persona.nombres} ${persona.apellidos} y se borró su registro.`;
    });

  return (
    <Tarjeta titulo="Sala de espera">
      {resultado && <Aviso tono={resultado.tono}>{resultado.texto}</Aviso>}
      {enEspera.error && <Aviso tono="error">{enEspera.error}</Aviso>}
      {!enEspera.datos && enEspera.cargando && <p className="texto-suave">Cargando…</p>}
      {enEspera.datos?.length === 0 && <p className="texto-suave">No hay nadie esperando aprobación.</p>}

      {enEspera.datos?.map((persona) => (
        <article key={persona.usuarioRolId} className="ficha">
          <h3>
            {persona.nombres} {persona.apellidos}
          </h3>
          {persona.hermanoDe && (
            <p className="texto-suave">
              Hermano de{' '}
              <NombrePersona nombre={persona.hermanoDe} enlace={persona.hermanoDeFicha} clubId={clubId} textoVolver="Ingresos" />
            </p>
          )}
          {persona.retiraDeOtroClub && <Aviso tono="aviso">{AVISO_OTRO_CLUB}</Aviso>}
          <dl className="ficha-datos">
            <Dato nombre={nombreDeTipoDocumento(persona.tipoDocumento)} valor={persona.numeroDocumento} />
            <Dato nombre="Fecha de nacimiento" valor={dia(persona.fechaNacimiento)} />
            <Dato nombre="Correo" valor={persona.correo} />
            <Dato nombre="Celular" valor={persona.celular} />
            <Dato nombre="Padre, madre o responsable" valor={persona.nombreResponsable ?? 'No indicado'} />
            <Dato nombre="Se registró" valor={fechaYHora(persona.registradoEn)} />
          </dl>
          <div className="fila">
            <Boton onClick={() => abrir(setPorAprobar, persona)}>Aprobar</Boton>
            <Boton variante="secundario" onClick={() => abrir(setPorRechazar, persona)}>
              Rechazar
            </Boton>
          </div>
        </article>
      ))}

      <DialogoConfirmacion
        abierto={porAprobar !== null}
        titulo={`Aprobar el ingreso de ${porAprobar?.nombres ?? ''} ${porAprobar?.apellidos ?? ''}`}
        textoConfirmar="Aprobar ingreso"
        cargando={enviando}
        error={errorDialogo}
        alConfirmar={() => {
          if (porAprobar) {
            void aprobar(porAprobar);
          }
        }}
        alCancelar={() => setPorAprobar(null)}
      >
        <p>
          <strong>
            {porAprobar?.nombres} {porAprobar?.apellidos}
          </strong>{' '}
          entrará al club como <strong>jugador</strong> y quedará en la categoría de su año de nacimiento, si el
          club la tiene.
        </p>
        {porAprobar?.retiraDeOtroClub && <Aviso tono="aviso">{AVISO_OTRO_CLUB}</Aviso>}
      </DialogoConfirmacion>

      <DialogoConfirmacion
        abierto={porRechazar !== null}
        titulo={`Rechazar el ingreso de ${porRechazar?.nombres ?? ''} ${porRechazar?.apellidos ?? ''}`}
        textoConfirmar="Rechazar y borrar el registro"
        peligro
        cargando={enviando}
        error={errorDialogo}
        alConfirmar={() => {
          if (porRechazar) {
            void rechazar(porRechazar);
          }
        }}
        alCancelar={() => setPorRechazar(null)}
      >
        <p>
          Su registro se borrará del club y no quedará ningún dato suyo en él.{' '}
          {porRechazar?.hermanoDe
            ? `La cuenta de la familia y ${porRechazar.hermanoDe} siguen como están; la familia puede volver a agregarlo desde la ficha.`
            : 'Esta persona solo podrá volver si le envías una invitación nueva y se registra otra vez.'}
        </p>
        <p className="texto-suave">No se le envía ningún aviso.</p>
      </DialogoConfirmacion>
    </Tarjeta>
  );
}

function Dato({ nombre, valor }: { nombre: string; valor: string }) {
  return (
    <div>
      <dt>{nombre}</dt>
      <dd>{valor}</dd>
    </div>
  );
}
