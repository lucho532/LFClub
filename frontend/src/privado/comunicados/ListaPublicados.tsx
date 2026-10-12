import { useState } from 'react';
import { Link } from 'react-router-dom';
import { api } from '../../compartido/api/cliente';
import { mensajeDe } from '../../compartido/api/errores';
import type { Rol } from '../../compartido/api/tipos';
import type { ComunicadoPublicadoDto } from '../../compartido/api/tiposComunicados';
import { Boton } from '../../compartido/componentes/Boton';
import { DialogoConfirmacion } from '../../compartido/componentes/DialogoConfirmacion';
import { NombrePersona } from '../../compartido/componentes/NombrePersona';
import { fechaYHora, nombreDeRol } from '../../compartido/formato';
import { estadoDeComunicado, textoDeAudiencia, textoDeFamilias, textosComunicados } from './textos';

interface Props {
  clubId: string;
  rol: Rol;
  comunicados: ComunicadoPublicadoDto[];
  /** Comunicado abierto en el detalle, para marcarlo en la lista. */
  abierto: string | undefined;
  hayMas: boolean;
  cargandoMas: boolean;
  alVerMas: () => void;
  /** Recibe el comunicado ya retirado, tal como lo devolvió la API. */
  alRetirar: (comunicado: ComunicadoPublicadoDto) => void;
}

/**
 * La lista de lo publicado por el equipo del club, del más reciente al más antiguo (spec 011,
 * RF-024 a RF-026): título, a quién iba, a cuántas familias llegó, quién lo publicó (solo el
 * PRESIDENTE y el DIRECTIVO) y el estado escrito, "Publicado" o "Retirado" con quién y cuándo.
 * "Retirar" aparece solo donde `puedeRetirar` y pide confirmación antes de llamar a la API.
 */
export function ListaPublicados({ clubId, rol, comunicados, abierto, hayMas, cargandoMas, alVerMas, alRetirar }: Props) {
  const [porRetirar, setPorRetirar] = useState<ComunicadoPublicadoDto | null>(null);
  const [retirando, setRetirando] = useState(false);
  const [error, setError] = useState<string>();
  const veAutor = rol === 'PRESIDENTE' || rol === 'DIRECTIVO';

  async function confirmar() {
    if (!porRetirar) {
      return;
    }

    setRetirando(true);
    setError(undefined);
    try {
      const retirado = await api.post<ComunicadoPublicadoDto>(
        `/api/clubes/${clubId}/comunicados/${porRetirar.id}/retiro`,
      );
      alRetirar(retirado);
      setPorRetirar(null);
    } catch (fallo) {
      setError(mensajeDe(fallo));
    } finally {
      setRetirando(false);
    }
  }

  if (comunicados.length === 0) {
    return <p className="texto-suave">{textosComunicados.vacioEquipo}</p>;
  }

  return (
    <div className="lista-comunicados">
      <ul className="comunicados" aria-label="Comunicados publicados">
        {comunicados.map((comunicado) => (
          <li
            key={comunicado.id}
            className={`comunicado-fila${comunicado.retirado ? ' retirado' : ''}${abierto === comunicado.id ? ' abierto' : ''}`}
          >
            <Link to={`/club/${clubId}/comunicados/${comunicado.id}`} className="comunicado-titulo">
              {comunicado.titulo}
            </Link>
            <div className="comunicado-datos">
              <span className={`etiqueta-leido ${comunicado.retirado ? 'retirado' : 'publicado'}`}>
                {estadoDeComunicado(comunicado)}
              </span>
              <span>{textoDeAudiencia(comunicado.alcance, comunicado.destinos)}</span>
              <span>{textoDeFamilias(comunicado.familiasAlcanzadas)}</span>
              <span className="texto-suave">{fechaYHora(comunicado.publicadoEn)}</span>
            </div>
            {veAutor && (
              <div className="comunicado-datos">
                <span>
                  Publicado por{' '}
                  <NombrePersona
                    nombre={comunicado.publicadoPor.nombre}
                    enlace={
                      comunicado.publicadoPor.usuarioRolId
                        ? { usuarioRolId: comunicado.publicadoPor.usuarioRolId, tipo: 'BASICA' }
                        : null
                    }
                    clubId={clubId}
                    textoVolver="Comunicados publicados"
                  />{' '}
                  · {nombreDeRol(comunicado.publicadoPor.rol)}
                </span>
              </div>
            )}
            {comunicado.retirado && (
              <p className="texto-suave comunicado-retiro">
                Retirado por {comunicado.retirado.porNombre} el {fechaYHora(comunicado.retirado.en)}
              </p>
            )}
            {comunicado.puedeRetirar && (
              <div className="comunicado-datos">
                <Boton variante="peligro" onClick={() => setPorRetirar(comunicado)}>
                  {textosComunicados.retirar}
                </Boton>
              </div>
            )}
          </li>
        ))}
      </ul>
      {hayMas && (
        <Boton variante="secundario" cargando={cargandoMas} onClick={alVerMas}>
          {textosComunicados.verMas}
        </Boton>
      )}
      <DialogoConfirmacion
        abierto={porRetirar !== null}
        titulo={`Retirar «${porRetirar?.titulo ?? ''}»`}
        textoConfirmar="Retirar comunicado"
        peligro
        cargando={retirando}
        error={error}
        alConfirmar={() => void confirmar()}
        alCancelar={() => {
          setPorRetirar(null);
          setError(undefined);
        }}
      >
        <p>{textosComunicados.confirmarRetiro}</p>
      </DialogoConfirmacion>
    </div>
  );
}
