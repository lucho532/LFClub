import { act } from 'react';
import { createRoot, type Root } from 'react-dom/client';
import { MemoryRouter, Outlet, Route, Routes } from 'react-router-dom';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { api } from '../src/compartido/api/cliente';
import type { ClubDto } from '../src/compartido/api/tipos';
import type {
  AvisoRecibidoDto,
  ElementoDeCampanaDto,
  PaginaDeCampanaDto,
} from '../src/compartido/api/tiposCampana';
import type { ComunicadoRecibidoDto } from '../src/compartido/api/tiposComunicados';
import { CampanaComunicados } from '../src/compartido/componentes/CampanaComunicados';
import { ProveedorContadorComunicados, useContadorDelClub } from '../src/privado/comunicados/ContadorComunicados';
import { PantallaComunicados } from '../src/privado/comunicados/PantallaComunicados';

(globalThis as { IS_REACT_ACT_ENVIRONMENT?: boolean }).IS_REACT_ACT_ENVIRONMENT = true;

function comunicado(id: string, titulo: string, leido: boolean, texto = 'Texto'): ComunicadoRecibidoDto {
  return {
    id,
    titulo,
    texto,
    publicadoEn: '2026-10-12T15:00:00Z',
    publicadoPor: { usuarioRolId: 'p', nombre: 'Pía Presidenta', rol: 'PRESIDENTE' },
    leido,
    leidoEn: leido ? '2026-10-12T16:00:00Z' : undefined,
  };
}

function aviso(id: string, texto: string, leido: boolean): AvisoRecibidoDto {
  return {
    id,
    texto,
    generadoEn: '2026-10-12T15:30:00Z',
    actividad: { tipo: 'ENTRENAMIENTO', id: `act-${id}`, disponible: true },
    leido,
    leidoEn: leido ? '2026-10-12T16:00:00Z' : undefined,
  };
}

function CampanaDePrueba() {
  const { sinLeer } = useContadorDelClub();
  return <CampanaComunicados sinLeer={sinLeer} a="/club/club/comunicados" />;
}

describe('Lectura de comunicados y avisos', () => {
  let contenedor: HTMLDivElement;
  let raiz: Root;

  beforeEach(() => {
    contenedor = document.createElement('div');
    document.body.appendChild(contenedor);
    raiz = createRoot(contenedor);
  });

  afterEach(() => {
    act(() => raiz.unmount());
    contenedor.remove();
    vi.restoreAllMocks();
  });

  /**
   * Lo que responde la API a cada ruta, con un contador que se pregunta cuántas veces se consultó.
   * La campana mezcla, en el orden en que se dan, comunicados y avisos.
   */
  function simular(
    comunicados: ComunicadoRecibidoDto[],
    avisos: AvisoRecibidoDto[],
    sinLeer: number,
    opciones: { hayMas?: boolean } = {},
  ) {
    const estado = { sinLeer, contador: 0, puts: [] as string[], gets: [] as string[] };
    const datos = new Map(comunicados.map((item) => [item.id, { ...item }]));
    const deAvisos = new Map(avisos.map((item) => [item.id, { ...item }]));
    vi.spyOn(api, 'get').mockImplementation(async (ruta: string) => {
      estado.gets.push(ruta);
      if (ruta.endsWith('/campana/contador')) {
        estado.contador++;
        return { sinLeer: estado.sinLeer };
      }

      const id = /comunicados\/recibidos\/([^/?]+)$/.exec(ruta)?.[1];
      if (id) {
        return { ...datos.get(id)! };
      }

      const elementos: ElementoDeCampanaDto[] = [
        ...[...datos.values()].map((item) => ({ tipo: 'COMUNICADO' as const, comunicado: item })),
        ...[...deAvisos.values()].map((item) => ({ tipo: 'AVISO' as const, aviso: item })),
      ];
      return {
        elementos,
        hayMas: opciones.hayMas === true && !ruta.includes('antesDe='),
        sinLeer: estado.sinLeer,
      } satisfies PaginaDeCampanaDto;
    });
    vi.spyOn(api, 'put').mockImplementation(async (ruta: string) => {
      estado.puts.push(ruta);
      const idAviso = /campana\/avisos\/([^/]+)\/lectura$/.exec(ruta)?.[1];
      if (idAviso) {
        deAvisos.get(idAviso)!.leido = true;
        deAvisos.get(idAviso)!.leidoEn = '2026-10-12T17:00:00Z';
        estado.sinLeer = Math.max(0, estado.sinLeer - 1);
        return { ...deAvisos.get(idAviso)! };
      }

      const id = /recibidos\/([^/]+)\/lectura$/.exec(ruta)![1];
      datos.get(id)!.leido = true;
      datos.get(id)!.leidoEn = '2026-10-12T17:00:00Z';
      estado.sinLeer = Math.max(0, estado.sinLeer - 1);
      return { ...datos.get(id)! };
    });
    vi.spyOn(api, 'post').mockImplementation(async (ruta: string) => {
      estado.puts.push(`POST ${ruta}`);
      datos.forEach((item) => (item.leido = true));
      deAvisos.forEach((item) => (item.leido = true));
      estado.sinLeer = 0;
      return { sinLeer: 0 };
    });
    return estado;
  }

  async function abrir(ruta: string) {
    const club = { clubId: 'club', miRol: 'JUGADOR', miUsuarioRolId: 'yo' } as ClubDto;
    await act(async () => {
      raiz.render(
        <MemoryRouter initialEntries={[ruta]}>
          <ProveedorContadorComunicados clubId="club" activo>
            <CampanaDePrueba />
            <Routes>
              <Route element={<Outlet context={{ club, fijarClub: () => undefined }} />}>
                <Route path="/club/club/comunicados" element={<PantallaComunicados />} />
                <Route path="/club/club/comunicados/:comunicadoId" element={<PantallaComunicados />} />
                <Route path="/club/club/entrenamientos/:id" element={<p>Detalle del entrenamiento</p>} />
              </Route>
            </Routes>
          </ProveedorContadorComunicados>
        </MemoryRouter>,
      );
    });
  }

  const insignia = () => contenedor.querySelector('.campana-insignia')?.textContent ?? null;
  const filas = () => Array.from(contenedor.querySelectorAll('.comunicado-fila'));
  const boton = (texto: string) =>
    Array.from(contenedor.querySelectorAll('button')).find((b) => b.textContent?.includes(texto)) as HTMLButtonElement;

  it('abrir un comunicado baja el contador y marca la fila como Leído, sin otra consulta del contador', async () => {
    const estado = simular([comunicado('a', 'Uno', false), comunicado('b', 'Dos', false)], [], 2);
    await abrir('/club/club/comunicados/a');

    expect(estado.puts).toEqual(['/api/clubes/club/comunicados/recibidos/a/lectura']);
    expect(insignia()).toBe('1');
    const consultasDelContador = estado.contador;
    expect(filas()[0].textContent).toContain('Leído');
    expect(filas()[1].textContent).toContain('Sin leer');
    // Con la respuesta de la lectura no hace falta preguntar el número otra vez.
    expect(estado.contador).toBe(consultasDelContador);
  });

  it('la lista mezcla comunicados y avisos, y cada fila dice su tipo y su estado en texto', async () => {
    simular([comunicado('a', 'Cambio de horario', false)], [aviso('x', 'Nuevo entrenamiento el sábado a las 9:00', true)], 1);
    await abrir('/club/club/comunicados');

    expect(contenedor.querySelector('h1')?.textContent).toBe('Novedades');
    const [deComunicado, deAviso] = filas();
    expect(deComunicado.textContent).toContain('Comunicado');
    expect(deComunicado.textContent).toContain('Sin leer');
    expect(deAviso.textContent).toContain('Aviso');
    expect(deAviso.textContent).toContain('Leído');
    expect(deAviso.textContent).toContain('Nuevo entrenamiento el sábado a las 9:00');
    expect(insignia()).toBe('1');
  });

  it('"Marcar todos como leídos" deja la insignia sin número y marca comunicados y avisos', async () => {
    const estado = simular([comunicado('a', 'Uno', false)], [aviso('x', 'Aviso uno', false)], 2);
    await abrir('/club/club/comunicados');
    expect(insignia()).toBe('2');
    expect(boton('Marcar todos').disabled).toBe(false);

    await act(async () => boton('Marcar todos').click());

    expect(estado.puts).toEqual(['POST /api/clubes/club/campana/lectura-total']);
    expect(insignia()).toBeNull();
    expect(boton('Marcar todos').disabled).toBe(true);
    expect(filas().every((fila) => fila.textContent?.includes('Leído'))).toBe(true);
  });

  it('con todo leído "Marcar todos como leídos" está deshabilitado desde el principio', async () => {
    simular([comunicado('a', 'Uno', true)], [aviso('x', 'Aviso uno', true)], 0);
    await abrir('/club/club/comunicados');

    expect(boton('Marcar todos').disabled).toBe(true);
    expect(insignia()).toBeNull();
  });

  it('un comunicado ya leído no se vuelve a marcar', async () => {
    const estado = simular([comunicado('a', 'Uno', true)], [], 0);
    await abrir('/club/club/comunicados/a');

    expect(estado.puts).toEqual([]);
  });

  it('tocar un aviso lo marca leído, baja el contador y abre la actividad', async () => {
    const estado = simular([], [aviso('x', 'Nuevo entrenamiento el sábado a las 9:00', false)], 1);
    await abrir('/club/club/comunicados');

    await act(async () => boton('Nuevo entrenamiento').click());

    expect(estado.puts).toEqual(['/api/clubes/club/campana/avisos/x/lectura']);
    expect(insignia()).toBeNull();
    expect(contenedor.textContent).toContain('Detalle del entrenamiento');
  });

  it('"Ver más" pide la siguiente página con el cursor del último elemento', async () => {
    const estado = simular([comunicado('a', 'Uno', false)], [aviso('x', 'Aviso uno', false)], 2, { hayMas: true });
    await abrir('/club/club/comunicados');

    await act(async () => boton('Ver más').click());

    expect(estado.gets.some((ruta) => ruta.includes('/campana?antesDe=x'))).toBe(true);
  });
});
