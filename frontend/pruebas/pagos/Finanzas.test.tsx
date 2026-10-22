import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import type { FinanzasDto } from '../../src/compartido/api/tiposPagos';
import { DisposicionPagos } from '../../src/privado/pagos/DisposicionPagos';
import { PantallaFinanzas } from '../../src/privado/pagos/PantallaFinanzas';
import { puedeVerPagos, veFinanzasDelClub } from '../../src/privado/pagos/textosPagos';
import { Montaje, clubDe, simularGet } from './ayudas';

const finanzas: FinanzasDto = {
  mes: '2026-10-01',
  recaudoMes: 85000,
  carteraPendiente: 110000,
  pagosEnRevision: 2,
  porCategoria: [
    { categoriaId: 'c14', nombre: 'Categoría 2014', recaudoMes: 65000, cartera: 65000 },
    { categoriaId: 'c15', nombre: 'Categoría 2015', recaudoMes: 20000, cartera: 45000 },
    { categoriaId: null, nombre: 'Sin categoría', recaudoMes: 0, cartera: 0 },
  ],
  jugadoresQueDeben: [
    { usuarioRolId: 'beto', nombreCompleto: 'Beto Bermúdez', saldo: 65000, estado: 'DEBE', retirado: false },
    { usuarioRolId: 'caro', nombreCompleto: 'Caro Cárdenas', saldo: 45000, estado: 'PAGO_PARCIAL', retirado: true },
  ],
};

describe('PantallaFinanzas', () => {
  let montaje: Montaje;

  beforeEach(() => {
    montaje = new Montaje();
  });

  afterEach(() => montaje.desmontar());

  async function abrir(datos: FinanzasDto = finanzas) {
    simularGet({ '/api/clubes/club/finanzas': datos });
    await montaje.pintar(clubDe('PRESIDENTE'), '/f', '/f', <PantallaFinanzas />);
  }

  it('muestra el recaudo del mes, la cartera y los pagos en revisión', async () => {
    await abrir();

    expect(montaje.texto).toContain('octubre de 2026');
    expect(montaje.texto).toContain('Recaudo del mes$ 85.000');
    expect(montaje.texto).toContain('Cartera pendiente$ 110.000');
    expect(montaje.texto).toContain('Pagos en revisión2');
  });

  it('muestra los totales por categoría, con "Sin categoría" al final', async () => {
    await abrir();

    const filas = [...montaje.contenedor.querySelectorAll('tbody tr')].map((fila) => fila.textContent);
    expect(filas[0]).toContain('Categoría 2014');
    expect(filas[0]).toContain('$ 65.000');
    expect(filas[1]).toContain('Categoría 2015');
    expect(filas[1]).toContain('$ 45.000');
    expect(filas[2]).toContain('Sin categoría');
  });

  it('lista quiénes deben con su estado en texto y marca a los retirados', async () => {
    await abrir();

    expect(montaje.texto).toContain('Beto Bermúdez');
    expect(montaje.texto).toContain('Debe');
    expect(montaje.texto).toContain('Caro Cárdenas · Retirado');
    expect(montaje.texto).toContain('Pago parcial');
    const enlace = [...montaje.contenedor.querySelectorAll('a')].find((a) => a.textContent === 'Beto Bermúdez');
    expect(enlace?.getAttribute('href')).toBe('/club/club/jugadores/beto/cuenta');
  });

  it('sin deudas lo dice', async () => {
    await abrir({ ...finanzas, carteraPendiente: 0, jugadoresQueDeben: [], porCategoria: [] });

    expect(montaje.texto).toContain('Nadie debe nada.');
    expect(montaje.texto).toContain('Todavía no hay cargos ni pagos para mostrar.');
  });

  it('no es un recordatorio de cobro: no pide cobrar ni avisa a nadie', async () => {
    await abrir();

    for (const palabra of ['recordar', 'recordatorio', 'enviar aviso', 'cobrar a']) {
      expect(montaje.texto.toLowerCase()).not.toContain(palabra);
    }
  });
});

describe('Pagos para el entrenador', () => {
  let montaje: Montaje;

  beforeEach(() => {
    montaje = new Montaje();
  });

  afterEach(() => montaje.desmontar());

  it('el entrenador no tiene entrada de pagos ni ve las finanzas', () => {
    expect(puedeVerPagos('ENTRENADOR')).toBe(false);
    expect(veFinanzasDelClub('ENTRENADOR')).toBe(false);
    expect(veFinanzasDelClub('JUGADOR')).toBe(false);
  });

  it('si el entrenador abre la sección a mano lo devuelve al inicio sin pedir nada', async () => {
    const pedir = simularGet({});
    await montaje.pintar(clubDe('ENTRENADOR'), '/p', '/p', <DisposicionPagos />);

    expect(montaje.texto).toContain('otra pantalla');
    expect(pedir).not.toHaveBeenCalled();
  });

  it('el directivo ve las pestañas de la sección', async () => {
    await montaje.pintar(clubDe('DIRECTIVO'), '/p', '/p', <DisposicionPagos />);

    expect(montaje.texto).toContain('Finanzas');
    expect(montaje.texto).toContain('En revisión');
    expect(montaje.texto).toContain('Mensualidad');
    expect(montaje.texto).toContain('Medios de pago');
  });
});
