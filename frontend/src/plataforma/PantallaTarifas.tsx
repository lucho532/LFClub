import { useState } from 'react';
import type { GuardarTarifasDto } from '../compartido/api/tiposPagoPlataforma';
import { useCarga } from '../compartido/api/useCarga';
import { Aviso } from '../compartido/componentes/Aviso';
import { EtiquetaEstado } from '../compartido/componentes/EtiquetaEstado';
import { IconoLapiz } from '../compartido/componentes/Iconos';
import { Tarjeta } from '../compartido/componentes/Tarjeta';
import { fechaYHora } from '../compartido/formato';
import { formatoMoneda } from '../privado/pagos/formatoMoneda';
import { apiPagoPlataforma } from './apiPagoPlataforma';
import { DialogoEditarTarifas } from './DialogoEditarTarifas';

const SIN_DEFINIR = 'Sin definir';

const precio = (valor: number | null) => (valor === null ? null : formatoMoneda(valor));

/**
 * Tarifas del cobro de la plataforma (spec 017, historia 4) como una tarjeta de solo lectura: los cuatro
 * precios por rol y los días de gracia. Nada se edita hasta pulsar el lápiz, que abre el diálogo de
 * edición. Indica si la pasarela de pago está configurada, sin mostrar ninguna llave.
 */
export function PantallaTarifas() {
  const { datos, error, cargando, fijar } = useCarga('tarifas', () => apiPagoPlataforma.tarifas.obtener());
  const [editando, setEditando] = useState(false);
  const [guardado, setGuardado] = useState(false);

  async function guardar(nuevas: GuardarTarifasDto) {
    fijar(await apiPagoPlataforma.tarifas.guardar(nuevas));
    setEditando(false);
    setGuardado(true);
  }

  const filas = datos && [
    { etiqueta: 'Precio por jugador', valor: precio(datos.precioJugador) },
    { etiqueta: 'Precio por entrenador', valor: precio(datos.precioEntrenador) },
    { etiqueta: 'Precio por directivo', valor: precio(datos.precioDirectivo) },
    { etiqueta: 'Precio por presidente', valor: precio(datos.precioPresidente) },
  ];

  return (
    <>
      <div className="cabecera">
        <h1>Tarifas</h1>
        {datos &&
          (datos.pasarelaConfigurada ? (
            <EtiquetaEstado tono="exito" texto="Pasarela de pago configurada" />
          ) : (
            <EtiquetaEstado tono="aviso" texto="Pasarela de pago sin configurar" />
          ))}
      </div>
      {error && <Aviso tono="error">{error}</Aviso>}
      {guardado && <Aviso tono="exito">Tarifas guardadas.</Aviso>}
      {!datos && cargando && <p className="texto-suave">Cargando…</p>}
      {datos && filas && (
        <>
          <Tarjeta
            titulo="Precios por integrante"
            acciones={
              <button
                type="button"
                className="lapiz-tarjeta"
                aria-label="Editar tarifas"
                title="Editar tarifas"
                onClick={() => {
                  setGuardado(false);
                  setEditando(true);
                }}
              >
                <IconoLapiz />
              </button>
            }
          >
            <dl className="ficha-datos">
              {filas.map(({ etiqueta, valor }) => (
                <div key={etiqueta}>
                  <dt>{etiqueta}</dt>
                  <dd className={valor ? undefined : 'texto-suave'}>{valor ?? SIN_DEFINIR}</dd>
                </div>
              ))}
              <div>
                <dt>Días de gracia</dt>
                <dd>{datos.diasDeGracia}</dd>
              </div>
            </dl>
            <p className="texto-suave">
              Aplican a los cobros que se generen desde ahora.
              {datos.actualizadoEn && ` Última actualización: ${fechaYHora(datos.actualizadoEn)}.`}
            </p>
          </Tarjeta>
          {editando && (
            <DialogoEditarTarifas inicial={datos} alGuardar={guardar} alCancelar={() => setEditando(false)} />
          )}
        </>
      )}
    </>
  );
}
