import { useState, type FormEvent } from 'react';
import { api } from '../../compartido/api/cliente';
import { ErrorApi, mensajeDe } from '../../compartido/api/errores';
import type { CategoriaConUbicadosDto, CrearCategoriaDto } from '../../compartido/api/tiposCategorias';
import { Aviso } from '../../compartido/componentes/Aviso';
import { Boton } from '../../compartido/componentes/Boton';
import { Campo } from '../../compartido/componentes/Campo';
import { Tarjeta } from '../../compartido/componentes/Tarjeta';
import { resumenDeUbicados } from './textos';
import type { Mensaje } from './useAccion';

interface Props {
  clubId: string;
  /** Se llama tras crear una categoría, para recargar las listas del apartado. */
  alCrear: () => void;
}

/**
 * Alta de una categoría: un solo campo, el año de nacimiento, y un botón, a la vista sin abrir
 * nada. Tras crearla dice cuántos jugadores sin categoría entraron solos en ella. Solo se
 * pinta al presidente; quien decide si se crea es la API.
 */
export function FormularioCrearCategoria({ clubId, alCrear }: Props) {
  const [anio, setAnio] = useState('');
  const [errorAnio, setErrorAnio] = useState<string | undefined>();
  const [mensaje, setMensaje] = useState<Mensaje | null>(null);
  const [enviando, setEnviando] = useState(false);

  async function crear(evento: FormEvent) {
    evento.preventDefault();
    setMensaje(null);
    setErrorAnio(undefined);

    const escrito = anio.trim();
    if (!/^\d{4}$/.test(escrito)) {
      setErrorAnio(
        escrito === ''
          ? 'Escribe el año de nacimiento de la categoría.'
          : 'El año debe tener cuatro cifras, por ejemplo 2014.',
      );
      return;
    }

    setEnviando(true);
    try {
      const datos: CrearCategoriaDto = { anio: Number(escrito) };
      const creada = await api.post<CategoriaConUbicadosDto>(`/api/clubes/${clubId}/categorias`, datos);
      setMensaje({ tono: 'exito', texto: resumenDeUbicados(creada.categoria, creada.jugadoresUbicados, 'creada') });
      setAnio('');
      alCrear();
    } catch (fallo) {
      if (fallo instanceof ErrorApi && fallo.errorDe('anio')) {
        setErrorAnio(fallo.errorDe('anio'));
      } else {
        setMensaje({ tono: 'error', texto: mensajeDe(fallo) });
      }
    } finally {
      setEnviando(false);
    }
  }

  return (
    <Tarjeta titulo="Crear una categoría">
      <p className="texto-suave">
        Una categoría agrupa a los jugadores nacidos en un mismo año. Al crearla entran solos los jugadores sin
        categoría nacidos ese año.
      </p>
      <form className="fila fila-con-campos" onSubmit={(evento) => void crear(evento)} noValidate>
        <Campo
          etiqueta="Año de nacimiento"
          type="text"
          inputMode="numeric"
          valor={anio}
          alCambiar={setAnio}
          error={errorAnio}
          placeholder="2014"
          autoComplete="off"
          maxLength={4}
        />
        <Boton type="submit" cargando={enviando} textoCargando="Creando…">
          Crear categoría
        </Boton>
      </form>
      {mensaje && <Aviso tono={mensaje.tono}>{mensaje.texto}</Aviso>}
    </Tarjeta>
  );
}
