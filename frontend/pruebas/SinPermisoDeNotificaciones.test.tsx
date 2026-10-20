import { readdirSync, readFileSync, statSync } from 'node:fs';
import { join } from 'node:path';
import { act } from 'react';
import { createRoot, type Root } from 'react-dom/client';
import { MemoryRouter } from 'react-router-dom';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { api } from '../src/compartido/api/cliente';
import type { AvisoRecibidoDto } from '../src/compartido/api/tiposCampana';
import { FilaDeAviso } from '../src/privado/comunicados/FilaDeAviso';

(globalThis as { IS_REACT_ACT_ENVIRONMENT?: boolean }).IS_REACT_ACT_ENVIRONMENT = true;

function archivosDe(carpeta: string): string[] {
  return readdirSync(carpeta).flatMap((nombre) => {
    const ruta = join(carpeta, nombre);
    return statSync(ruta).isDirectory() ? archivosDe(ruta) : [ruta];
  });
}

/**
 * Ningún aviso sale de la aplicación (spec 012, RF-027): la aplicación no pide permiso de
 * notificaciones ni registra notificaciones del sistema, ni de la web ni de Capacitor.
 */
describe('Sin permiso de notificaciones', () => {
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
    vi.unstubAllGlobals();
  });

  it('el código de la aplicación no usa la API de notificaciones ni el registro de push', () => {
    const fuentes = archivosDe(join(__dirname, '..', 'src')).filter((ruta) => /\.(ts|tsx)$/.test(ruta));
    const prohibidos = [
      /Notification\.requestPermission/,
      /new\s+Notification\s*\(/,
      /serviceWorker\.register/,
      /PushManager/,
      /PushNotifications/,
      /LocalNotifications/,
      /showNotification/,
    ];

    const hallazgos = fuentes.flatMap((ruta) => {
      const texto = readFileSync(ruta, 'utf-8');
      // Único registro permitido: el service worker mínimo que hace instalable la web (sin avisos).
      const permitidos = ruta.endsWith('instalacionEscritorio.ts') ? [/serviceWorker\.register/] : [];
      return prohibidos
        .filter((patron) => patron.test(texto) && !permitidos.some((permitido) => permitido.source === patron.source))
        .map((patron) => `${ruta}: ${patron}`);
    });

    expect(hallazgos).toEqual([]);
  });

  it('el service worker de la instalación de escritorio no recibe push ni muestra notificaciones', () => {
    const texto = readFileSync(join(__dirname, '..', 'public', 'sw.js'), 'utf-8');
    expect(texto).not.toMatch(/addEventListener\(\s*['"](push|notificationclick|sync)['"]|showNotification|PushManager/);
  });

  it('leer un aviso no pide permiso ni crea notificaciones', async () => {
    const requestPermission = vi.fn();
    const Notificacion = vi.fn();
    vi.stubGlobal('Notification', Object.assign(Notificacion, { requestPermission, permission: 'default' }));
    const aviso: AvisoRecibidoDto = {
      id: 'av1',
      texto: 'Nuevo entrenamiento el sábado a las 9:00',
      generadoEn: '2026-10-12T15:30:00Z',
      actividad: { tipo: 'ENTRENAMIENTO', id: 'ent1', disponible: false },
      leido: false,
    };
    vi.spyOn(api, 'put').mockResolvedValue({ ...aviso, leido: true });

    await act(async () => {
      raiz.render(
        <MemoryRouter>
          <ul>
            <FilaDeAviso clubId="c" aviso={aviso} alLeer={vi.fn()} />
          </ul>
        </MemoryRouter>,
      );
    });
    await act(async () => (contenedor.querySelector('button') as HTMLButtonElement).click());

    expect(requestPermission).not.toHaveBeenCalled();
    expect(Notificacion).not.toHaveBeenCalled();
  });
});
