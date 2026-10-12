import { act } from 'react';
import { createRoot, type Root } from 'react-dom/client';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import type { PartidoDto } from '../src/compartido/api/tiposActividades';
import { SeccionResultadoFamilia } from '../src/privado/partidos/SeccionResultadoFamilia';

(globalThis as { IS_REACT_ACT_ENVIRONMENT?: boolean }).IS_REACT_ACT_ENVIRONMENT = true;

const base: PartidoDto = {
  partidoId: 'x',
  rival: 'Equipo C',
  fecha: '2026-10-10',
  hora: '09:00',
  lugar: 'Cancha de Minitas',
  condicion: 'LOCAL',
  tipo: 'AMISTOSO',
  estado: 'PROXIMO',
  faltaResultado: true,
  categorias: [{ categoriaId: 'c', anio: 2014 }],
  permisos: { gestiona: false, vePlanilla: false, responde: false, indicaResultado: true },
  miParticipacion: { jugo: false, goles: 0, puedeCambiar: true },
};

describe('SeccionResultadoFamilia', () => {
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
  });

  const pintar = (partido: PartidoDto) =>
    act(() =>
      raiz.render(
        <SeccionResultadoFamilia clubId="club" nombreClub="La Pecosa" partido={partido} alCambiar={() => {}} recargar={() => {}} />,
      ),
    );

  const etiquetas = () => Array.from(contenedor.querySelectorAll('label')).map((etiqueta) => etiqueta.textContent);

  it('sin marcador pide el marcador, si jugó y sus goles', () => {
    pintar(base);

    expect(etiquetas()).toEqual(['Goles del club', 'Goles del rival', 'Sí', 'No', 'Goles de tu jugador']);
    expect(contenedor.textContent).toContain('Publicar resultado');
  });

  it('con marcador lo muestra sin dejar cambiarlo y deja indicar lo de su jugador', () => {
    pintar({
      ...base,
      estado: 'JUGADO',
      faltaResultado: false,
      golesClub: 2,
      golesRival: 1,
      miParticipacion: { jugo: true, goles: 1, indicadoPor: 'FAMILIA', puedeCambiar: true },
    });

    expect(contenedor.textContent).toContain('La Pecosa 2 - 1 Equipo C');
    expect(etiquetas()).toEqual(['Sí', 'No', 'Goles de tu jugador']);
    expect((contenedor.querySelector('input[type="number"]') as HTMLInputElement).value).toBe('1');
    expect(contenedor.textContent).toContain('Guardar lo de mi jugador');
  });

  it('corregido por quien entrena lo dice y no muestra formulario', () => {
    pintar({
      ...base,
      estado: 'JUGADO',
      faltaResultado: false,
      golesClub: 3,
      golesRival: 1,
      permisos: { ...base.permisos, indicaResultado: false },
      miParticipacion: { jugo: true, goles: 2, indicadoPor: 'GESTION', puedeCambiar: false },
    });

    expect(contenedor.querySelector('form')).toBeNull();
    expect(contenedor.textContent).toContain('Quien entrena corrigió lo de tu jugador');
    expect(contenedor.textContent).toContain('Jugó y marcó 2 goles.');
  });

  it('no se pinta para quien no participa y nunca nombra a otro convocado', () => {
    pintar({ ...base, permisos: { ...base.permisos, indicaResultado: false }, miParticipacion: undefined });
    expect(contenedor.innerHTML).toBe('');

    pintar(base);
    expect(contenedor.textContent).not.toMatch(/convocad/i);
    expect(contenedor.querySelectorAll('input[type="checkbox"]')).toHaveLength(0);
  });
});
