import { useState, type FormEvent } from 'react';
import { api } from '../compartido/api/cliente';
import { ErrorApi, mensajeDe } from '../compartido/api/errores';
import type { CambiarDatosPropiosDto, DatosPropiosDto } from '../compartido/api/tiposPersonas';
import { useCarga } from '../compartido/api/useCarga';
import { Aviso } from '../compartido/componentes/Aviso';
import { Boton } from '../compartido/componentes/Boton';
import { Campo } from '../compartido/componentes/Campo';
import { nombreDeTipoDocumento } from '../compartido/formato';
import { useSesion } from '../compartido/sesion/useSesion';

/**
 * Datos personales de un presidente, directivo o entrenador en "Mi perfil" (spec 008, historia 4).
 * Cambia nombres, apellidos, fecha de nacimiento y celular, con las reglas del registro y en todos
 * sus clubes a la vez; el tipo y el número de documento son de solo lectura (RF-019), y el correo
 * se muestra arriba, también de solo lectura. Si la API rechaza algo, cada campo dice por qué y lo
 * escrito se conserva; nada se guarda. Al guardar, recarga la sesión para que la barra superior
 * muestre el nombre nuevo.
 */
export function SeccionDatosPropios() {
  const carga = useCarga('datos-propios', () => api.get<DatosPropiosDto>('/api/cuenta/datos-propios'));

  return (
    <section className="tarjeta">
      <h2>Mis datos</h2>
      {carga.error && <Aviso tono="error">{carga.error}</Aviso>}
      {!carga.datos && carga.cargando && <p className="texto-suave">Cargando…</p>}
      {carga.datos && <FormularioDatosPropios datos={carga.datos} alGuardar={carga.fijar} />}
    </section>
  );
}

function FormularioDatosPropios({ datos, alGuardar }: { datos: DatosPropiosDto; alGuardar: (datos: DatosPropiosDto) => void }) {
  const { recargar } = useSesion();
  const [valores, setValores] = useState<CambiarDatosPropiosDto>({
    nombres: datos.nombres,
    apellidos: datos.apellidos,
    fechaNacimiento: datos.fechaNacimiento,
    celular: datos.celular,
  });
  const [errores, setErrores] = useState<ErrorApi | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [hecho, setHecho] = useState(false);
  const [ocupado, setOcupado] = useState(false);

  const cambiar = (campo: keyof CambiarDatosPropiosDto) => (valor: string) =>
    setValores((actuales) => ({ ...actuales, [campo]: valor }));

  async function guardar(evento: FormEvent) {
    evento.preventDefault();
    setErrores(null);
    setError(null);
    setHecho(false);
    setOcupado(true);
    try {
      alGuardar(await api.put<DatosPropiosDto>('/api/cuenta/datos-propios', valores));
      setHecho(true);
      await recargar();
    } catch (fallo) {
      if (fallo instanceof ErrorApi && fallo.codigo === 'datos_invalidos') {
        setErrores(fallo);
      }

      setError(mensajeDe(fallo));
    } finally {
      setOcupado(false);
    }
  }

  return (
    <form className="columna" onSubmit={(evento) => void guardar(evento)} noValidate>
      {error && <Aviso tono="error">{error}</Aviso>}
      {hecho && <Aviso tono="exito">Datos guardados.</Aviso>}
      <Campo etiqueta="Nombres" valor={valores.nombres} alCambiar={cambiar('nombres')} error={errores?.errorDe('nombres')} maxLength={80} required />
      <Campo etiqueta="Apellidos" valor={valores.apellidos} alCambiar={cambiar('apellidos')} error={errores?.errorDe('apellidos')} maxLength={80} required />
      <Campo
        etiqueta="Fecha de nacimiento"
        type="date"
        valor={valores.fechaNacimiento}
        alCambiar={cambiar('fechaNacimiento')}
        error={errores?.errorDe('fechaNacimiento')}
        required
      />
      <Campo etiqueta="Celular" type="tel" valor={valores.celular} alCambiar={cambiar('celular')} error={errores?.errorDe('celular')} maxLength={20} required />
      <Campo
        etiqueta={nombreDeTipoDocumento(datos.tipoDocumento)}
        valor={datos.numeroDocumento}
        readOnly
        ayuda="El documento no se cambia desde aquí."
      />
      <div className="fila">
        <Boton type="submit" disabled={ocupado}>
          Guardar
        </Boton>
      </div>
    </form>
  );
}
