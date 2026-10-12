import { useState, type FormEvent } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { api } from '../../compartido/api/cliente';
import { ErrorApi, mensajeDe } from '../../compartido/api/errores';
import type { ClubDto } from '../../compartido/api/tipos';
import type { CategoriaDto } from '../../compartido/api/tiposCategorias';
import type { AlcanceComunicado, ComunicadoPublicadoDto, PublicarComunicadoDto } from '../../compartido/api/tiposComunicados';
import { useCarga } from '../../compartido/api/useCarga';
import { Aviso } from '../../compartido/componentes/Aviso';
import { Boton } from '../../compartido/componentes/Boton';
import { Campo } from '../../compartido/componentes/Campo';
import { Tarjeta } from '../../compartido/componentes/Tarjeta';
import { useClub } from '../contextoClub';
import { SelectorAudiencia } from './SelectorAudiencia';
import {
  LIMITE_DEL_TEXTO,
  LIMITE_DEL_TITULO,
  puedeDirigirseATodoElClub,
  puedePublicarComunicados,
  textoDeFamilias,
  textosComunicados,
} from './textos';

/** Una clave única para la publicación; no depende de que el contexto sea seguro (`randomUUID`). */
export function nuevaClave(): string {
  if (typeof crypto.randomUUID === 'function') {
    return crypto.randomUUID();
  }

  return '10000000-1000-4000-8000-100000000000'.replace(/[018]/g, (digito) =>
    (Number(digito) ^ (crypto.getRandomValues(new Uint8Array(1))[0] & (15 >> (Number(digito) / 4)))).toString(16),
  );
}

/** Las categorías activas a las que la persona puede dirigir comunicados; la API vuelve a comprobarlo. */
function categoriasPermitidas(club: ClubDto, todas: CategoriaDto[]): CategoriaDto[] {
  return todas
    .filter((categoria) => categoria.activa)
    .filter(
      (categoria) =>
        club.miRol === 'PRESIDENTE' ||
        club.miRol === 'DIRECTIVO' ||
        categoria.entrenadores.some((entrenador) => entrenador.usuarioRolId === club.miUsuarioRolId),
    )
    .sort((a, b) => a.anio - b.anio);
}

/** Lo que falta elegir en la audiencia, o `undefined` si ya está completa. */
export function errorDeAudiencia(
  alcance: AlcanceComunicado,
  categoriaIds: string[],
  equipoIds: string[],
): string | undefined {
  if (alcance === 'CATEGORIAS' && categoriaIds.length === 0) {
    return textosComunicados.elegirCategoria;
  }

  return alcance === 'EQUIPOS' && equipoIds.length === 0 ? textosComunicados.elegirEquipo : undefined;
}

/**
 * "Nuevo comunicado" (spec 011, historia 1): título de hasta 100 caracteres, texto de hasta 1.000 con
 * el contador de lo que queda y "Para quién" según el rol. La clave de la publicación se genera al
 * abrir el formulario y se conserva en los reintentos, así un doble toque o un reintento por red no
 * publica dos veces (RF-010); además el botón queda deshabilitado mientras la petición va. Al publicar
 * vuelve a la lista con "Comunicado publicado. Les llegará a N familias." Quien decide si se puede
 * publicar a esa audiencia es la API.
 */
export function FormularioComunicado() {
  const { club } = useClub();
  const navegar = useNavigate();
  // Se genera una sola vez al abrir el formulario y vale para todos los reintentos.
  const [clave] = useState(nuevaClave);
  const carga = useCarga(`${club.clubId}/comunicados/categorias`, () =>
    api.get<CategoriaDto[]>(`/api/clubes/${club.clubId}/categorias`),
  );
  const categorias = categoriasPermitidas(club, carga.datos ?? []);
  const conTodoElClub = puedeDirigirseATodoElClub(club.miRol);

  const [titulo, setTitulo] = useState('');
  const [texto, setTexto] = useState('');
  const [alcance, setAlcance] = useState<AlcanceComunicado>(conTodoElClub ? 'CLUB' : 'CATEGORIAS');
  const [categoriaIds, setCategoriaIds] = useState<string[]>([]);
  const [categoriaDeEquipos, setCategoriaDeEquipos] = useState('');
  const [equipoIds, setEquipoIds] = useState<string[]>([]);
  const [enviando, setEnviando] = useState(false);
  const [errores, setErrores] = useState<Record<string, string>>({});
  const [errorGeneral, setErrorGeneral] = useState<string>();

  if (!puedePublicarComunicados(club.miRol)) {
    return <Aviso tono="aviso">Tu rol no publica comunicados.</Aviso>;
  }

  function cambiarAlcance(nuevo: AlcanceComunicado) {
    setAlcance(nuevo);
    setErrores((previos) => ({ ...previos, audiencia: '' }));
  }

  async function publicar(evento: FormEvent) {
    evento.preventDefault();
    if (enviando) {
      return;
    }

    const locales: Record<string, string> = {};
    if (titulo.trim() === '') {
      locales.titulo = 'El título es obligatorio.';
    }

    if (texto.trim() === '') {
      locales.texto = 'El texto es obligatorio.';
    }

    const falta = errorDeAudiencia(alcance, categoriaIds, equipoIds);
    if (falta) {
      locales.audiencia = falta;
    }

    setErrores(locales);
    setErrorGeneral(undefined);
    if (Object.keys(locales).length > 0) {
      return;
    }

    const cuerpo: PublicarComunicadoDto = {
      clave,
      titulo: titulo.trim(),
      texto: texto.trim(),
      alcance,
      ...(alcance === 'CATEGORIAS' ? { categoriaIds } : {}),
      ...(alcance === 'EQUIPOS' ? { equipoIds } : {}),
    };

    setEnviando(true);
    try {
      const publicado = await api.post<ComunicadoPublicadoDto>(`/api/clubes/${club.clubId}/comunicados`, cuerpo);
      navegar(`/club/${club.clubId}/comunicados`, {
        state: {
          aviso: `Comunicado publicado. Les llegará a ${textoDeFamilias(publicado.familiasAlcanzadas)}.`,
        },
      });
    } catch (fallo) {
      if (fallo instanceof ErrorApi && fallo.codigo === 'sin_destinatarios') {
        setErrores({ audiencia: textosComunicados.sinJugadores });
      } else if (fallo instanceof ErrorApi && Object.keys(fallo.errores).length > 0) {
        setErrores({
          titulo: fallo.errorDe('titulo') ?? '',
          texto: fallo.errorDe('texto') ?? '',
          audiencia: fallo.errorDe('alcance') ?? '',
        });
      } else if (fallo instanceof ErrorApi && fallo.codigo === 'audiencia_invalida') {
        setErrores({ audiencia: fallo.title });
      } else {
        setErrorGeneral(mensajeDe(fallo));
      }
    } finally {
      setEnviando(false);
    }
  }

  return (
    <>
      <Link to={`/club/${club.clubId}/comunicados`}>← {textosComunicados.tituloEquipo}</Link>
      <div className="cabecera">
        <h1>{textosComunicados.nuevo}</h1>
      </div>
      <Tarjeta>
        {carga.error && <Aviso tono="error">{carga.error}</Aviso>}
        <form className="formulario" onSubmit={(evento) => void publicar(evento)} noValidate>
          <Campo
            etiqueta="Título"
            valor={titulo}
            alCambiar={setTitulo}
            maxLength={LIMITE_DEL_TITULO}
            error={errores.titulo || undefined}
            ayuda={`${LIMITE_DEL_TITULO - titulo.length} caracteres restantes`}
          />
          <Campo
            etiqueta="Texto"
            valor={texto}
            alCambiar={setTexto}
            lineas={6}
            maxLength={LIMITE_DEL_TEXTO}
            error={errores.texto || undefined}
            ayuda={`${LIMITE_DEL_TEXTO - texto.length} caracteres restantes`}
          />
          <SelectorAudiencia
            conTodoElClub={conTodoElClub}
            categorias={categorias}
            alcance={alcance}
            categoriaIds={categoriaIds}
            categoriaDeEquipos={categoriaDeEquipos}
            equipoIds={equipoIds}
            alCambiarAlcance={cambiarAlcance}
            alCambiarCategorias={setCategoriaIds}
            alCambiarCategoriaDeEquipos={(categoriaId) => {
              setCategoriaDeEquipos(categoriaId);
              setEquipoIds([]);
            }}
            alCambiarEquipos={setEquipoIds}
            error={errores.audiencia || undefined}
          />
          {errorGeneral && <Aviso tono="error">{errorGeneral}</Aviso>}
          <div className="fila">
            <Boton type="submit" cargando={enviando} textoCargando="Publicando…">
              Publicar
            </Boton>
          </div>
        </form>
      </Tarjeta>
    </>
  );
}
