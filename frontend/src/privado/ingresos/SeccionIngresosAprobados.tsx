import type { IngresoAprobadoDto } from '../../compartido/api/tiposIngresos';
import type { Carga } from '../../compartido/api/useCarga';
import { Aviso } from '../../compartido/componentes/Aviso';
import { NombrePersona } from '../../compartido/componentes/NombrePersona';
import { Tabla, type Columna } from '../../compartido/componentes/Tabla';
import { Tarjeta } from '../../compartido/componentes/Tarjeta';
import { fechaYHora, nombreDeRol } from '../../compartido/formato';

function columnas(clubId: string): Columna<IngresoAprobadoDto>[] {
  return [
    {
      titulo: 'Persona',
      celda: (ingreso) => (
        <strong>
          <NombrePersona
            nombre={`${ingreso.nombres} ${ingreso.apellidos}`}
            enlace={ingreso.ficha}
            clubId={clubId}
            textoVolver="Ingresos"
          />
        </strong>
      ),
    },
    { titulo: 'Entró como', celda: (ingreso) => nombreDeRol(ingreso.rolDeIngreso) },
    {
      titulo: 'Aprobó',
      celda: (ingreso) => (
        <NombrePersona nombre={ingreso.aprobadoPor} enlace={ingreso.aprobadoPorFicha} clubId={clubId} textoVolver="Ingresos" />
      ),
    },
    { titulo: 'Cuándo', celda: (ingreso) => fechaYHora(ingreso.aprobadoEn) },
  ];
}

/**
 * Ingresos aprobados del club, del más reciente al más antiguo: quién entró, con qué rol, quién lo
 * aprobó y cuándo. Es de solo lectura: no ofrece ninguna acción (RF-025a). La persona abre su ficha
 * de hoy y quien aprobó, su ficha básica si sigue en el club; si no, su nombre es texto (spec 008).
 */
export function SeccionIngresosAprobados({ clubId, aprobados }: { clubId: string; aprobados: Carga<IngresoAprobadoDto[]> }) {
  return (
    <Tarjeta titulo="Ingresos aprobados">
      {aprobados.error && <Aviso tono="error">{aprobados.error}</Aviso>}
      {!aprobados.datos && aprobados.cargando && <p className="texto-suave">Cargando…</p>}
      {aprobados.datos && (
        <Tabla
          descripcion="Ingresos aprobados del club"
          columnas={columnas(clubId)}
          filas={aprobados.datos}
          clave={(ingreso) => ingreso.usuarioRolId}
          vacio="Todavía no se ha aprobado ningún ingreso."
        />
      )}
    </Tarjeta>
  );
}
