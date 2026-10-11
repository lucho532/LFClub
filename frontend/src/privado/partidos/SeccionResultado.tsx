import { useState } from 'react';
import type { PartidoDto } from '../../compartido/api/tiposActividades';
import { Boton } from '../../compartido/componentes/Boton';
import { NombrePersona } from '../../compartido/componentes/NombrePersona';
import { Tarjeta } from '../../compartido/componentes/Tarjeta';
import { fechaYHora } from '../../compartido/formato';
import { FormularioResultado } from './FormularioResultado';
import { textoDeMarcador } from './textos';

interface Props {
  clubId: string;
  nombreClub: string;
  partido: PartidoDto;
  alCambiar: (partido: PartidoDto) => void;
}

/**
 * El resultado de un partido: en uno jugado, el marcador según la condición, los goleadores y, para
 * quien ve la planilla, quién lo cambió por última vez y cuándo; y, para quien lo gestiona y una vez
 * empezado, "Registrar resultado" o "Corregir resultado". No se pinta en un partido próximo que no
 * ha empezado ni en uno cancelado.
 */
export function SeccionResultado({ clubId, nombreClub, partido, alCambiar }: Props) {
  const [editando, setEditando] = useState(false);
  const jugado = partido.estado === 'JUGADO';
  const puedeRegistrar = partido.permisos.gestiona && (jugado || partido.faltaResultado);

  if (!jugado && !puedeRegistrar) {
    return null;
  }

  const cambio = partido.ultimoCambioResultado;

  return (
    <Tarjeta
      titulo="Resultado"
      acciones={
        puedeRegistrar &&
        !editando && (
          <Boton variante={jugado ? 'secundario' : 'principal'} onClick={() => setEditando(true)}>
            {jugado ? 'Corregir resultado' : 'Registrar resultado'}
          </Boton>
        )
      }
    >
      {editando ? (
        <FormularioResultado
          clubId={clubId}
          partido={partido}
          alGuardar={(guardado) => {
            alCambiar(guardado);
            setEditando(false);
          }}
          alCancelar={() => setEditando(false)}
        />
      ) : jugado ? (
        <>
          <p className="marcador dato-largo">{textoDeMarcador(partido, nombreClub)}</p>
          {(partido.goleadores?.length ?? 0) > 0 ? (
            <ul className="lista-de-inicio">
              {partido.goleadores?.map((goleador) => (
                <li key={`${goleador.nombres} ${goleador.apellidos}`}>
                  <NombrePersona
                    nombre={`${goleador.nombres} ${goleador.apellidos}`}
                    enlace={goleador.enlace ?? null}
                    clubId={clubId}
                    textoVolver="Partido"
                  />
                  <span>{goleador.goles === 1 ? '1 gol' : `${goleador.goles} goles`}</span>
                </li>
              ))}
            </ul>
          ) : (
            <p className="texto-suave">Sin goleadores registrados.</p>
          )}
          {cambio && (
            <p className="texto-suave texto-pequeno">
              Último cambio por{' '}
              <NombrePersona nombre={cambio.autor} enlace={cambio.autorFicha} clubId={clubId} textoVolver="Partido" />,{' '}
              {fechaYHora(cambio.fecha)}
            </p>
          )}
        </>
      ) : (
        <p className="texto-suave">Falta el resultado de este partido.</p>
      )}
    </Tarjeta>
  );
}
