import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { act } from 'react';
import { api } from '../../src/compartido/api/cliente';
import type { CuentaJugadorDto } from '../../src/compartido/api/tiposPagos';
import { CuentaDelJugador } from '../../src/privado/pagos/CuentaDelJugador';
import { Montaje, clubDe, simularGet } from './ayudas';

const cuenta: CuentaJugadorDto = {
  usuarioRolId: 'ana',
  nombreCompleto: 'Ana Arango',
  estado: 'DEBE',
  saldo: 65000,
  cargos: [{ id: 'oct', tipo: 'MENSUALIDAD', mes: '2026-10-01', valor: 65000, pagado: 0, pendiente: 65000 }],
  pagos: [],
  mediosPago: [{ id: 'm1', entidad: 'Nequi', titular: 'Club', numero: '300', nota: null, activo: true }],
  puedeRegistrar: false,
  puedeInformar: true,
};

describe('InformarTransferencia', () => {
  let montaje: Montaje;

  beforeEach(() => {
    montaje = new Montaje();
  });

  afterEach(() => montaje.desmontar());

  async function abrirFormulario(datos: CuentaJugadorDto = cuenta) {
    simularGet({ '/api/clubes/club/jugadores/ana/cuenta': datos });
    await montaje.pintar(clubDe('JUGADOR', 'ana'), '/c/ana', '/c/:usuarioRolId', <CuentaDelJugador />);
    const boton = montaje.boton('Informé una transferencia');
    if (boton) {
      await montaje.clic(boton);
    }
  }

  async function elegirImagen(tamano = 1000) {
    const entrada = montaje.contenedor.querySelector('input[type="file"]') as HTMLInputElement;
    const archivo = new File([new Uint8Array(tamano)], 'comprobante.png', { type: 'image/png' });
    Object.defineProperty(entrada, 'files', { value: [archivo], configurable: true });
    await act(async () => {
      entrada.dispatchEvent(new Event('change', { bubbles: true }));
    });
    return archivo;
  }

  it('el botón solo aparece a la cuenta del jugador y si debe', async () => {
    await abrirFormulario({ ...cuenta, puedeInformar: false });
    expect(montaje.boton('Informé una transferencia')).toBeUndefined();
    montaje.desmontar();

    montaje = new Montaje();
    await abrirFormulario({ ...cuenta, saldo: 0, estado: 'AL_DIA', cargos: [] });
    expect(montaje.boton('Informé una transferencia')).toBeUndefined();
  });

  it('muestra lo que se debe y las entidades del club', async () => {
    await abrirFormulario();

    expect(montaje.texto).toContain('Pendiente: $ 65.000');
    expect(montaje.campo('Entidad desde la que transfirió')).not.toBeNull();
    expect(montaje.texto).toContain('Nequi');
  });

  it('sin imagen no envía nada', async () => {
    await abrirFormulario();
    const enviar = vi.spyOn(api, 'post');

    await montaje.escribir(montaje.campo('Valor transferido')!, '65000');
    await montaje.enviar(montaje.contenedor.querySelectorAll('form')[0]);

    expect(montaje.texto).toContain('Adjunta la imagen del comprobante');
    expect(enviar).not.toHaveBeenCalled();
  });

  it('rechaza un valor mayor que lo pendiente', async () => {
    await abrirFormulario();
    const enviar = vi.spyOn(api, 'post');
    await elegirImagen();

    await montaje.escribir(montaje.campo('Valor transferido')!, '80000');
    await montaje.enviar(montaje.contenedor.querySelectorAll('form')[0]);

    expect(montaje.texto).toContain('El valor supera lo que se debe: pendiente $ 65.000');
    expect(enviar).not.toHaveBeenCalled();
  });

  it('rechaza una imagen de más de 5 MB', async () => {
    await abrirFormulario();
    const enviar = vi.spyOn(api, 'post');
    await elegirImagen(5 * 1024 * 1024 + 1);

    await montaje.escribir(montaje.campo('Valor transferido')!, '65000');
    await montaje.enviar(montaje.contenedor.querySelectorAll('form')[0]);

    expect(montaje.texto).toContain('La imagen supera los 5 MB');
    expect(enviar).not.toHaveBeenCalled();
  });

  it('envía el formulario con la imagen y avisa que queda en revisión', async () => {
    await abrirFormulario();
    const enviar = vi.spyOn(api, 'post').mockResolvedValue({ id: 'p1' } as never);
    const archivo = await elegirImagen();

    await montaje.escribir(montaje.campo('Valor transferido')!, '65000');
    await montaje.escribir(montaje.campo('Referencia (opcional)')!, 'R-9');
    await montaje.enviar(montaje.contenedor.querySelectorAll('form')[0]);

    const [ruta, cuerpo] = enviar.mock.calls[0];
    expect(ruta).toBe('/api/clubes/club/jugadores/ana/pagos/informados');
    const formulario = cuerpo as FormData;
    expect(formulario.get('valor')).toBe('65000');
    expect(formulario.get('entidad')).toBe('Nequi');
    expect(formulario.get('referencia')).toBe('R-9');
    expect((formulario.get('archivo') as File).name).toBe(archivo.name);
    expect(montaje.texto).toContain('Queda en revisión');
  });

  it('una entidad que no está en la lista se escribe a mano', async () => {
    await abrirFormulario();
    const enviar = vi.spyOn(api, 'post').mockResolvedValue({ id: 'p1' } as never);
    await elegirImagen();

    await montaje.escribir(montaje.campo('Entidad desde la que transfirió')!, 'otra');
    await montaje.escribir(montaje.campo('Nombre de la entidad')!, 'Banco Agrario');
    await montaje.escribir(montaje.campo('Valor transferido')!, '65000');
    await montaje.enviar(montaje.contenedor.querySelectorAll('form')[0]);

    expect((enviar.mock.calls[0][1] as FormData).get('entidad')).toBe('Banco Agrario');
  });
});
