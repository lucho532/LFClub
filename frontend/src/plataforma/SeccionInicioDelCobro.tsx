import { useState, type FormEvent } from 'react';
import { ErrorApi, mensajeDe } from '../compartido/api/errores';
import { Aviso } from '../compartido/componentes/Aviso';
import { Boton } from '../compartido/componentes/Boton';
import { Campo } from '../compartido/componentes/Campo';
import { Tarjeta } from '../compartido/componentes/Tarjeta';
import { apiPagoPlataforma } from './apiPagoPlataforma';
import type { PropsSeccion } from './DetalleClub';

/**
 * Desde qué mes se cobra el uso de la plataforma a un club (spec 017, RF-005a). Vacío significa que no
 * se le cobra. Un mes pasado no genera cobros atrasados: el primer cobro es el del mes en curso.
 */
export function SeccionInicioDelCobro({ club, alCambiar }: PropsSeccion & { alCambiar?: () => void }) {
  const [mes, setMes] = useState(club.mesDeInicioDelCobro ?? '');
  const [error, setError] = useState<string | undefined>();
  const [guardado, setGuardado] = useState(false);
  const [enviando, setEnviando] = useState(false);

  async function guardar(evento: FormEvent) {
    evento.preventDefault();
    setError(undefined);
    setGuardado(false);
    setEnviando(true);
    try {
      await apiPagoPlataforma.inicioDelCobro.fijar(club.clubId, { mesDeInicio: mes === '' ? null : mes });
      setGuardado(true);
      alCambiar?.();
    } catch (fallo) {
      setError(fallo instanceof ErrorApi ? (fallo.errorDe('mesDeInicio') ?? mensajeDe(fallo)) : mensajeDe(fallo));
    } finally {
      setEnviando(false);
    }
  }

  return (
    <Tarjeta titulo="Cobro de la plataforma">
      <form className="columna" onSubmit={guardar} noValidate>
        {guardado && <Aviso tono="exito">Mes de inicio guardado.</Aviso>}
        <Campo
          etiqueta="Se cobra desde el mes"
          type="month"
          valor={mes}
          alCambiar={(nuevo) => {
            setMes(nuevo);
            setGuardado(false);
          }}
          error={error}
          ayuda="Déjalo vacío para no cobrarle a este club. Los meses pasados no se cobran."
        />
        <div className="fila">
          <Boton type="submit" cargando={enviando} textoCargando="Guardando…">
            {mes === '' ? 'No cobrar' : 'Guardar mes de inicio'}
          </Boton>
        </div>
      </form>
    </Tarjeta>
  );
}
