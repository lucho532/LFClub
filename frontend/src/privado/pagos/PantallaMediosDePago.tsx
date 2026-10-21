import { useState } from 'react';
import { mensajeDe } from '../../compartido/api/errores';
import type { GuardarMedioPagoDto, MedioPagoDto } from '../../compartido/api/tiposPagos';
import { Aviso } from '../../compartido/componentes/Aviso';
import { Boton } from '../../compartido/componentes/Boton';
import { EtiquetaEstado } from '../../compartido/componentes/EtiquetaEstado';
import { Tarjeta } from '../../compartido/componentes/Tarjeta';
import { useClub } from '../contextoClub';
import { apiMediosPago } from './apiMediosPago';
import { FormularioMedioDePago } from './FormularioMedioDePago';
import { useMediosPago } from './useMediosPago';

/**
 * Pagos → Medios de pago (spec 015, historia 5): las cuentas por las que el club recibe pagos. El
 * PRESIDENTE las añade, edita, desactiva (las familias dejan de verla) y quita; el DIRECTIVO las
 * consulta. Quitar un medio no toca ningún pago hecho con esa entidad.
 */
export function PantallaMediosDePago() {
  const { club } = useClub();
  const medios = useMediosPago(club.clubId);
  const puedeCambiar = club.miRol === 'PRESIDENTE';
  const [editando, setEditando] = useState<MedioPagoDto | 'nuevo' | null>(null);
  const [error, setError] = useState<string | null>(null);

  async function guardar(datos: GuardarMedioPagoDto) {
    if (editando === 'nuevo') {
      await apiMediosPago.crear(club.clubId, datos);
    } else if (editando) {
      await apiMediosPago.editar(club.clubId, editando.id, datos);
    }

    setEditando(null);
    medios.recargar();
  }

  async function quitar(medio: MedioPagoDto) {
    setError(null);
    try {
      await apiMediosPago.quitar(club.clubId, medio.id);
      medios.recargar();
    } catch (fallo) {
      setError(mensajeDe(fallo));
    }
  }

  return (
    <>
      <div className="cabecera">
        <h1>Medios de pago</h1>
      </div>
      {(medios.error || error) && <Aviso tono="error">{medios.error ?? error}</Aviso>}
      {!medios.datos && medios.cargando && <p className="texto-suave">Cargando…</p>}
      {editando && (
        <Tarjeta titulo={editando === 'nuevo' ? 'Nuevo medio de pago' : 'Editar medio de pago'}>
          <FormularioMedioDePago
            key={editando === 'nuevo' ? 'nuevo' : editando.id}
            inicial={editando === 'nuevo' ? null : editando}
            alGuardar={guardar}
            alCancelar={() => setEditando(null)}
          />
        </Tarjeta>
      )}
      {medios.datos && (
        <Tarjeta
          titulo="Por dónde se le paga al club"
          acciones={
            puedeCambiar && !editando ? <Boton onClick={() => setEditando('nuevo')}>Añadir medio</Boton> : undefined
          }
        >
          {medios.datos.length === 0 ? (
            <p className="texto-suave">Todavía no hay medios de pago publicados.</p>
          ) : (
            <ul className="lista-de-pagos">
              {medios.datos.map((medio) => (
                <li key={medio.id}>
                  <span className="columna">
                    <strong>{medio.entidad}</strong>
                    <span>
                      {medio.numero} · {medio.titular}
                    </span>
                    {medio.nota && <span className="texto-suave">{medio.nota}</span>}
                  </span>
                  <span className="fila">
                    <EtiquetaEstado tono={medio.activo ? 'exito' : 'neutro'} texto={medio.activo ? 'Visible' : 'Oculto'} />
                    {puedeCambiar && (
                      <>
                        <Boton variante="secundario" onClick={() => setEditando(medio)}>
                          Editar
                        </Boton>
                        <Boton variante="texto" onClick={() => void quitar(medio)}>
                          Quitar
                        </Boton>
                      </>
                    )}
                  </span>
                </li>
              ))}
            </ul>
          )}
        </Tarjeta>
      )}
    </>
  );
}
