const PESOS = new Intl.NumberFormat('es-CO', { maximumFractionDigits: 0 });

/** Un importe en pesos colombianos, sin decimales: `$ 65.000`. */
export function formatoMoneda(valor: number): string {
  return `$ ${PESOS.format(Math.round(valor))}`;
}

/**
 * El número que escribió la persona como importe: solo dígitos, con o sin puntos de miles. Devuelve
 * `null` si no es un entero de pesos mayor que cero.
 */
export function leerPesos(texto: string): number | null {
  const limpio = texto.replace(/[\s.$]/g, '');
  if (!/^\d+$/.test(limpio)) {
    return null;
  }

  const valor = Number(limpio);
  return valor > 0 ? valor : null;
}
