import type { JugadorDeCategoriaDto } from '../../compartido/api/tiposCategorias';
import type { Carga } from '../../compartido/api/useCarga';
import { Aviso } from '../../compartido/componentes/Aviso';
import { Boton } from '../../compartido/componentes/Boton';
import { NombrePersona } from '../../compartido/componentes/NombrePersona';
import { Tabla, type Columna } from '../../compartido/componentes/Tabla';
import { Tarjeta } from '../../compartido/componentes/Tarjeta';
import { BotonRetirarJugador } from './BotonRetirarJugador';
import { DialogoCambiarCategoria } from './DialogoCambiarCategoria';
import { EstadoDeDocumentacion } from './EstadoDeDocumentacion';
import { nombreCompleto } from './textos';
import { useAccion } from './useAccion';
import { useUbicarJugador } from './useUbicarJugador';

interface Props {
  clubId: string;
  esPresidente: boolean;
  sinCategoria: Carga<JugadorDeCategoriaDto[]>;
  /** Se llama tras ubicar a un jugador, para recargar las listas del apartado. */
  alCambiar: () => void;
}

/**
 * Lista "Sin categoría": los jugadores aprobados del club que todavía no tienen categoría, porque
 * el club no tiene activa la de su año de nacimiento. Muestra nombre, apellidos y año de
 * nacimiento (RF-032); el nombre abre la ficha del jugador (RF-034). El presidente puede ubicar a
 * cada uno a mano.
 */
export function SeccionSinCategoria({ clubId, esPresidente, sinCategoria, alCambiar }: Props) {
  const { mensaje, fijarMensaje } = useAccion();
  const ubicar = useUbicarJugador(clubId, (texto) => {
    fijarMensaje({ tono: 'exito', texto });
    alCambiar();
  });

  const columnas: Columna<JugadorDeCategoriaDto>[] = [
    {
      titulo: 'Jugador',
      celda: (jugador) => (
        <strong>
          <NombrePersona
            nombre={nombreCompleto(jugador)}
            enlace={{ usuarioRolId: jugador.usuarioRolId, tipo: 'JUGADOR' }}
            clubId={clubId}
            textoVolver="Categorías"
          />
        </strong>
      ),
    },
    { titulo: 'Año de nacimiento', celda: (jugador) => jugador.anioNacimiento },
    {
      titulo: 'Documentación',
      celda: (jugador) => <EstadoDeDocumentacion pendientes={jugador.documentosPendientes} />,
    },
  ];

  if (esPresidente) {
    columnas.push({
      titulo: 'Acciones',
      celda: (jugador) => (
        <span className="fila">
          <Boton variante="secundario" onClick={() => ubicar.abrir(jugador)}>
            Ubicar
          </Boton>
          <BotonRetirarJugador
            clubId={clubId}
            jugador={jugador}
            alRetirar={(texto) => {
              fijarMensaje({ tono: 'exito', texto });
              alCambiar();
            }}
          />
        </span>
      ),
    });
  }

  return (
    <Tarjeta titulo="Sin categoría">
      <p className="texto-suave">
        Jugadores aprobados que todavía no tienen categoría. Entran solos cuando se crea o se reactiva la
        categoría de su año de nacimiento.
      </p>
      {mensaje && <Aviso tono={mensaje.tono}>{mensaje.texto}</Aviso>}
      {sinCategoria.error && <Aviso tono="error">{sinCategoria.error}</Aviso>}
      {!sinCategoria.datos && sinCategoria.cargando && <p className="texto-suave">Cargando…</p>}
      {sinCategoria.datos && (
        <Tabla
          descripcion="Jugadores sin categoría"
          columnas={columnas}
          filas={sinCategoria.datos}
          clave={(jugador) => jugador.usuarioRolId}
          vacio="Todos los jugadores del club tienen categoría."
        />
      )}

      {ubicar.jugador && (
        <DialogoCambiarCategoria
          key={ubicar.jugador.usuarioRolId}
          clubId={clubId}
          jugador={ubicar.jugador}
          cargando={ubicar.enviando}
          error={ubicar.error}
          alConfirmar={(destino) => void ubicar.confirmar(destino)}
          alCancelar={ubicar.cerrar}
        />
      )}
    </Tarjeta>
  );
}
