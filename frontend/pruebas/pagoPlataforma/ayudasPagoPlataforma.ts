import type { CobroPlataformaDto, PagoPlataformaDto } from '../../src/compartido/api/tiposPagoPlataforma';

/** Un cobro pendiente de noviembre de 2026 con el detalle por rol ya calculado por el servidor. */
export function cobroDe(cambios: Partial<CobroPlataformaDto> = {}): CobroPlataformaDto {
  return {
    cobroId: 'c1',
    periodo: '2026-11',
    estado: 'PENDIENTE',
    detalle: [
      { rol: 'JUGADOR', integrantes: 20, precio: 1500, subtotal: 30000 },
      { rol: 'ENTRENADOR', integrantes: 3, precio: 5000, subtotal: 15000 },
      { rol: 'DIRECTIVO', integrantes: 0, precio: 3000, subtotal: 0 },
      { rol: 'PRESIDENTE', integrantes: 1, precio: 0, subtotal: 0 },
    ],
    total: 45000,
    generadoEn: '2026-11-03T15:00:00Z',
    suspendeDesde: '2026-11-08',
    pagadoEn: null,
    origenDelPago: null,
    referencia: null,
    medioDePago: null,
    enVerificacion: false,
    ultimoPagoNoAprobado: false,
    motivoAnulacion: null,
    ...cambios,
  };
}

/** El resumen del PRESIDENTE; por defecto con un cobro pendiente y el pago en línea disponible. */
export function resumenDe(cambios: Partial<PagoPlataformaDto> = {}): PagoPlataformaDto {
  const pendientes = cambios.pendientes ?? [cobroDe()];
  return {
    pagoEnLineaDisponible: true,
    suspendidoPorImpago: false,
    pendientes,
    totalPendiente: pendientes.reduce((suma, cobro) => suma + cobro.total, 0),
    historial: [],
    ...cambios,
  };
}
