import { act, type ReactElement } from 'react';
import { createRoot, type Root } from 'react-dom/client';
import { MemoryRouter, Outlet, Route, Routes } from 'react-router-dom';
import { vi } from 'vitest';
import { api } from '../../src/compartido/api/cliente';
import type { ClubDto, Rol } from '../../src/compartido/api/tipos';

(globalThis as { IS_REACT_ACT_ENVIRONMENT?: boolean }).IS_REACT_ACT_ENVIRONMENT = true;

/** El club de la sesión, con solo lo que las pantallas de pagos leen. */
export function clubDe(rol: Rol, miUsuarioRolId = 'yo'): ClubDto {
  return { clubId: 'club', nombre: 'Club de prueba', miRol: rol, miUsuarioRolId } as ClubDto;
}

/** Monta una pantalla dentro de la disposición del club, con su contexto, como lo hace la aplicación. */
export class Montaje {
  readonly contenedor = document.createElement('div');
  private readonly raiz: Root;

  constructor() {
    document.body.appendChild(this.contenedor);
    this.raiz = createRoot(this.contenedor);
  }

  async pintar(club: ClubDto, ruta: string, patron: string, pantalla: ReactElement) {
    await act(async () => {
      this.raiz.render(
        <MemoryRouter initialEntries={[ruta]}>
          <Routes>
            <Route element={<Outlet context={{ club, fijarClub: () => undefined }} />}>
              <Route path={patron} element={pantalla} />
            </Route>
            <Route path="*" element={<output>otra pantalla</output>} />
          </Routes>
        </MemoryRouter>,
      );
    });
  }

  desmontar() {
    act(() => this.raiz.unmount());
    this.contenedor.remove();
    vi.restoreAllMocks();
  }

  get texto() {
    return this.contenedor.textContent ?? '';
  }

  boton(texto: string) {
    return [...this.contenedor.querySelectorAll('button')].find((candidato) =>
      candidato.textContent?.includes(texto),
    ) as HTMLButtonElement | undefined;
  }

  campo(etiqueta: string) {
    const rotulo = [...this.contenedor.querySelectorAll('label')].find((candidato) =>
      candidato.textContent?.includes(etiqueta),
    );
    return (rotulo ? document.getElementById(rotulo.htmlFor) : null) as
      | HTMLInputElement
      | HTMLTextAreaElement
      | HTMLSelectElement
      | null;
  }

  async escribir(elemento: HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement, valor: string) {
    const prototipo =
      elemento instanceof HTMLTextAreaElement
        ? HTMLTextAreaElement
        : elemento instanceof HTMLSelectElement
          ? HTMLSelectElement
          : HTMLInputElement;
    await act(async () => {
      Object.getOwnPropertyDescriptor(prototipo.prototype, 'value')!.set!.call(elemento, valor);
      elemento.dispatchEvent(new Event(elemento instanceof HTMLSelectElement ? 'change' : 'input', { bubbles: true }));
    });
  }

  async clic(elemento: HTMLElement) {
    await act(async () => {
      elemento.click();
    });
  }

  async enviar(formulario: HTMLFormElement) {
    await act(async () => {
      formulario.dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }));
    });
  }
}

/** Responde cada GET de la pantalla con lo que dice el mapa de rutas; una ruta que falta falla la prueba. */
export function simularGet(respuestas: Record<string, unknown>) {
  return vi.spyOn(api, 'get').mockImplementation(async (ruta: string) => {
    const clave = Object.keys(respuestas).find((candidata) => ruta === candidata || ruta.startsWith(`${candidata}?`));
    if (clave === undefined) {
      throw new Error(`GET inesperado: ${ruta}`);
    }

    return respuestas[clave] as never;
  });
}
