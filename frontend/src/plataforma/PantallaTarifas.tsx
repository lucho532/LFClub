import { useState } from 'react';
import type { GuardarTarifasDto } from '../compartido/api/tiposPagoPlataforma';
import { useCarga } from '../compartido/api/useCarga';
import { Aviso } from '../compartido/componentes/Aviso';
import { EtiquetaEstado } from '../compartido/componentes/EtiquetaEstado';
import { IconoLapiz } from '../compartido/componentes/Iconos';
import { Tarjeta } from '../compartido/componentes/Tarjeta';
import { fechaYHora, textoDeRango } from '../compartido/formato';
import { formatoMoneda } from '../privado/pagos/formatoMoneda';
import { apiPagoPlataforma } from './apiPagoPlataforma';
import { DialogoEditarTarifas } from './DialogoEditarTarifas';
import { TarifasPorClub } from './TarifasPorClub';

/**
 * Tarifas del cobro de la plataforma (spec 017, historia 4) como una tarjeta de solo lectura: los rangos
 * de jugadores con su precio mensual fijo y los días de gracia. Nada se edita hasta pulsar el lápiz, que
 * abre el diálogo de edición. Debajo, lo que le corresponde pagar a cada club con esa tarifa. Indica si la
 * pasarela de pago está configurada, sin mostrar ninguna llave.
 */
export function PantallaTarifas() {
  const { datos, error, cargando, fijar } = useCarga('tarifas', () => apiPagoPlataforma.tarifas.obtener());
  const porClub = useCarga('tarifas-por-club', () => apiPagoPlataforma.tarifas.porClub());
  const [editando, setEditando] = useState(false);
  const [guardado, setGuardado] = useState(false);

  async function guardar(nuevas: GuardarTarifasDto) {
    fijar(await apiPagoPlataforma.tarifas.guardar(nuevas));
    porClub.recargar();
    setEditando(false);
    setGuardado(true);
  }

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
      {datos && (
        <>
          <Tarjeta
            titulo="Precio mensual por cantidad de jugadores"
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
            {datos.rangos.length === 0 ? (
              <Aviso tono="aviso">
                No hay rangos definidos: no se genera ningún cobro. Pulsa el lápiz para crearlos.
              </Aviso>
            ) : (
              <dl className="ficha-datos">
                {datos.rangos.map((rango) => (
                  <div key={rango.desde}>
                    <dt>{textoDeRango(rango.desde, rango.hasta)}</dt>
                    <dd>{formatoMoneda(rango.precio)} al mes</dd>
                  </div>
                ))}
              </dl>
            )}
            <dl className="ficha-datos">
              <div>
                <dt>Días de gracia</dt>
                <dd>{datos.diasDeGracia}</dd>
              </div>
            </dl>
            <p className="texto-suave">
              El precio es fijo para todo el club según sus jugadores activos; entrenadores, directivos y
              presidentes no suman. Aplica a los cobros que se generen desde ahora.
              {datos.actualizadoEn && ` Última actualización: ${fechaYHora(datos.actualizadoEn)}.`}
            </p>
          </Tarjeta>
          <TarifasPorClub carga={porClub} />
          {editando && (
            <DialogoEditarTarifas inicial={datos} alGuardar={guardar} alCancelar={() => setEditando(false)} />
          )}
        </>
      )}
    </>
  );
}
