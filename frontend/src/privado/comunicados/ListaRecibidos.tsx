import { Link } from 'react-router-dom';
import type { AvisoRecibidoDto, ElementoDeLista } from '../../compartido/api/tiposCampana';
import { Boton } from '../../compartido/componentes/Boton';
import { NombrePersona } from '../../compartido/componentes/NombrePersona';
import { fechaYHora, nombreDeRol } from '../../compartido/formato';
import { FilaDeAviso } from './FilaDeAviso';
import { textosComunicados } from './textos';

interface Props {
  clubId: string;
  /** Comunicados y avisos mezclados, del más reciente al más antiguo. */
  elementos: ElementoDeLista[];
  /** Comunicado abierto en el detalle, para marcarlo en la lista. */
  abierto: string | undefined;
  /** Comunicados y avisos sin leer de la cuenta; con 0 "Marcar todos" queda deshabilitado. */
  sinLeer: number;
  hayMas: boolean;
  cargandoMas: boolean;
  marcandoTodos: boolean;
  alVerMas: () => void;
  alMarcarTodos: () => void;
  /** Un aviso se acaba de leer: la pantalla actualiza la lista y el contador. */
  alLeerAviso: (aviso: AvisoRecibidoDto, estabaSinLeer: boolean) => void;
}

/**
 * La campana de la familia: comunicados y avisos mezclados, del más reciente al más antiguo (spec 011,
 * RF-014 a RF-016; spec 012, RF-018). Cada fila dice en texto "Comunicado" o "Aviso" y "Sin leer" o
 * "Leído", además de poner en negrita lo sin leer. El comunicado lleva el nombre de quien lo publicó
 * con su rol, que abre su ficha básica; el aviso, su línea de texto. Solo pinta lo que recibe: no
 * sabe a quién más le llegó.
 */
export function ListaRecibidos({
  clubId,
  elementos,
  abierto,
  sinLeer,
  hayMas,
  cargandoMas,
  marcandoTodos,
  alVerMas,
  alMarcarTodos,
  alLeerAviso,
}: Props) {
  if (elementos.length === 0) {
    return <p className="texto-suave">{textosComunicados.vacioFamilia}</p>;
  }

  return (
    <div className="lista-comunicados">
      <div className="comunicados-acciones">
        <Boton
          variante="secundario"
          disabled={sinLeer === 0}
          cargando={marcandoTodos}
          onClick={alMarcarTodos}
        >
          {textosComunicados.marcarTodos}
        </Boton>
      </div>
      <ul className="comunicados" aria-label="Novedades del club">
        {elementos.map((elemento) => {
          if (elemento.aviso) {
            return <FilaDeAviso key={elemento.id} clubId={clubId} aviso={elemento.aviso} alLeer={alLeerAviso} />;
          }

          const comunicado = elemento.comunicado;
          if (!comunicado) {
            return null;
          }

          return (
            <li
              key={elemento.id}
              className={`comunicado-fila${comunicado.leido ? '' : ' sin-leer'}${abierto === comunicado.id ? ' abierto' : ''}`}
            >
              <Link
                to={`/club/${clubId}/comunicados/${comunicado.id}`}
                className="comunicado-titulo"
                aria-current={abierto === comunicado.id ? 'page' : undefined}
              >
                {comunicado.titulo}
              </Link>
              <div className="comunicado-datos">
                <span className="etiqueta-leido tipo-comunicado">{textosComunicados.etiquetaComunicado}</span>
                <span className={`etiqueta-leido ${comunicado.leido ? 'leido' : 'no-leido'}`}>
                  {comunicado.leido ? textosComunicados.leido : textosComunicados.sinLeer}
                </span>
                <span>
                  <NombrePersona
                    nombre={comunicado.publicadoPor.nombre}
                    enlace={
                      comunicado.publicadoPor.usuarioRolId
                        ? { usuarioRolId: comunicado.publicadoPor.usuarioRolId, tipo: 'BASICA' }
                        : null
                    }
                    clubId={clubId}
                    textoVolver="Novedades"
                  />{' '}
                  · {nombreDeRol(comunicado.publicadoPor.rol)}
                </span>
                <span className="texto-suave">{fechaYHora(comunicado.publicadoEn)}</span>
              </div>
            </li>
          );
        })}
      </ul>
      {hayMas && (
        <Boton variante="secundario" cargando={cargandoMas} onClick={alVerMas}>
          {textosComunicados.verMas}
        </Boton>
      )}
    </div>
  );
}
