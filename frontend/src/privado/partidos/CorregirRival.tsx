import { useState, type FormEvent } from 'react';
import { api } from '../../compartido/api/cliente';
import { ErrorApi, mensajeDe } from '../../compartido/api/errores';
import type { PartidoDto } from '../../compartido/api/tiposActividades';
import { Aviso } from '../../compartido/componentes/Aviso';
import { Boton } from '../../compartido/componentes/Boton';
import { Campo } from '../../compartido/componentes/Campo';

interface Props {
  ruta: string;
  partido: PartidoDto;
  /** Se llama con el partido que devuelve la API tras corregir el rival. */
  alCorregir: (partido: PartidoDto) => void;
}

/**
 * "Corregir rival" de un partido ya jugado, para quien lo gestiona: un partido jugado no cambia
 * sus demás datos, pero el nombre del rival se puede escribir mal o quedar "por definir". Dejarlo
 * vacío lo deja por definir. El marcador y los goles no se tocan.
 */
export function CorregirRival({ ruta, partido, alCorregir }: Props) {
  const [abierto, setAbierto] = useState(false);
  const [rival, setRival] = useState(partido.rival ?? '');
  const [enviando, setEnviando] = useState(false);
  const [fallo, setFallo] = useState<unknown>(null);

  async function guardar(evento: FormEvent) {
    evento.preventDefault();
    setFallo(null);
    setEnviando(true);
    try {
      alCorregir(await api.put<PartidoDto>(`${ruta}/rival`, { rival }));
      setAbierto(false);
    } catch (error) {
      setFallo(error);
    } finally {
      setEnviando(false);
    }
  }

  if (!abierto) {
    return (
      <div>
        <Boton
          variante="secundario"
          onClick={() => {
            setRival(partido.rival ?? '');
            setAbierto(true);
          }}
        >
          Corregir rival
        </Boton>
      </div>
    );
  }

  const errorRival = fallo instanceof ErrorApi ? fallo.errorDe('rival') : undefined;

  return (
    <form className="fila fila-con-campos" onSubmit={(evento) => void guardar(evento)} noValidate>
      <Campo
        etiqueta="Equipo rival"
        valor={rival}
        alCambiar={setRival}
        error={errorRival}
        ayuda="Si lo dejas vacío, queda por definir."
        maxLength={120}
        autoComplete="off"
      />
      <Boton type="submit" cargando={enviando} textoCargando="Guardando…">
        Guardar rival
      </Boton>
      <Boton variante="secundario" onClick={() => setAbierto(false)} disabled={enviando}>
        Cancelar
      </Boton>
      {fallo !== null && !errorRival && <Aviso tono="error">{mensajeDe(fallo)}</Aviso>}
    </form>
  );
}
