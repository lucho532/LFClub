import { useState } from 'react';
import type { NotificacionPasarelaDto, ResultadoNotificacion } from '../compartido/api/tiposPagoPlataforma';
import { useCarga } from '../compartido/api/useCarga';
import { Aviso } from '../compartido/componentes/Aviso';
import { Campo } from '../compartido/componentes/Campo';
import { EtiquetaEstado } from '../compartido/componentes/EtiquetaEstado';
import { Tabla, type Columna } from '../compartido/componentes/Tabla';
import { fechaYHora } from '../compartido/formato';
import { formatoMoneda } from '../privado/pagos/formatoMoneda';
import { apiPagoPlataforma } from './apiPagoPlataforma';

const TEXTO_RESULTADO: Record<ResultadoNotificacion, string> = {
  APLICADA: 'Aplicada',
  REPETIDA: 'Repetida',
  NO_APROBADA: 'No aprobada',
  PAGO_DUPLICADO: 'Pago duplicado',
  PAGO_A_COBRO_ANULADO: 'Pago a cobro anulado',
  VALOR_NO_COINCIDE: 'Valor no coincide',
  REFERENCIA_DESCONOCIDA: 'Referencia desconocida',
  FIRMA_INVALIDA: 'Firma inválida',
};

/** Los que el DESARROLLADOR debe revisar: un pago duplicado o a un cobro anulado hay que devolverlo. */
const A_REVISAR: ResultadoNotificacion[] = [
  'PAGO_DUPLICADO',
  'PAGO_A_COBRO_ANULADO',
  'VALOR_NO_COINCIDE',
  'FIRMA_INVALIDA',
];

const COLUMNAS: Columna<NotificacionPasarelaDto>[] = [
  { titulo: 'Recibida', celda: (n) => fechaYHora(n.recibidaEn) },
  {
    titulo: 'Resultado',
    celda: (n) => (
      <EtiquetaEstado
        tono={n.resultado === 'APLICADA' ? 'exito' : A_REVISAR.includes(n.resultado) ? 'aviso' : 'neutro'}
        texto={`${A_REVISAR.includes(n.resultado) ? '⚠ ' : ''}${TEXTO_RESULTADO[n.resultado]}`}
      />
    ),
  },
  { titulo: 'Transacción', celda: (n) => n.transaccionId ?? '—' },
  { titulo: 'Estado en la pasarela', celda: (n) => n.estadoDeLaPasarela ?? '—' },
  { titulo: 'Referencia', celda: (n) => n.referencia ?? '—' },
  { titulo: 'Valor', celda: (n) => (n.valor === null ? '—' : formatoMoneda(n.valor)) },
  { titulo: 'Periodo', celda: (n) => n.periodo ?? '—' },
];

/**
 * Notificaciones recibidas de la pasarela (spec 017, RF-015): las últimas 100, filtrables por resultado,
 * con los casos que hay que revisar (duplicados, pagos a cobros anulados, valores que no coinciden,
 * firmas inválidas) resaltados. No muestra el cuerpo ni la firma.
 */
export function PantallaNotificaciones() {
  const [resultado, setResultado] = useState<ResultadoNotificacion | ''>('');
  const carga = useCarga(`notificaciones/${resultado}`, () =>
    apiPagoPlataforma.notificaciones.listar(resultado === '' ? undefined : resultado),
  );

  return (
    <>
      <div className="cabecera">
        <h1>Notificaciones de la pasarela</h1>
      </div>
      <Campo
        etiqueta="Filtrar por resultado"
        valor={resultado}
        alCambiar={(nuevo) => setResultado(nuevo as ResultadoNotificacion | '')}
        opciones={[
          { valor: '', texto: 'Todos' },
          ...(Object.keys(TEXTO_RESULTADO) as ResultadoNotificacion[]).map((valor) => ({
            valor,
            texto: TEXTO_RESULTADO[valor],
          })),
        ]}
      />
      {carga.error && <Aviso tono="error">{carga.error}</Aviso>}
      {!carga.datos && carga.cargando && <p className="texto-suave">Cargando…</p>}
      {carga.datos && (
        <Tabla
          descripcion="Notificaciones recibidas de la pasarela de pago"
          columnas={COLUMNAS}
          filas={carga.datos}
          clave={(n) => n.notificacionId}
          vacio="No hay notificaciones con ese resultado."
        />
      )}
    </>
  );
}
