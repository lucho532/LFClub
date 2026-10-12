import { useState } from 'react';
import { api } from '../../compartido/api/cliente';
import { mensajeDe } from '../../compartido/api/errores';
import type { CartaDto } from '../../compartido/api/tiposCarta';
import { Aviso } from '../../compartido/componentes/Aviso';
import { Boton } from '../../compartido/componentes/Boton';
import { textosCarta } from './textos';

interface Props {
  rutaDeLaCarta: string;
  carta: CartaDto;
}

/** Nombre de respaldo, por si el navegador no deja leer la cabecera: el mismo formato que la API. */
function nombreDeRespaldo(carta: CartaDto): string {
  const limpio = (texto: string) =>
    texto
      .normalize('NFD')
      .replace(/[̀-ͯ]/g, '')
      .trim()
      .toLowerCase()
      .replace(/\s+/g, '-')
      .replace(/[^a-z0-9-]/g, '');
  return `carta-${limpio(carta.apellidos)}-${limpio(carta.nombres)}.png`;
}

/**
 * "Descargar imagen": pide a la API el PNG de la carta con la sesión y lo guarda con el nombre que
 * ella da. Solo se pinta con `permisos.exporta` (la familia y el presidente), pero la barrera es la
 * API, que niega la imagen a cualquier otro aunque la pida directamente (RF-031).
 */
export function BotonDescargarCarta({ rutaDeLaCarta, carta }: Props) {
  const [preparando, setPreparando] = useState(false);
  const [fallo, setFallo] = useState<unknown>(null);

  async function descargar() {
    setFallo(null);
    setPreparando(true);
    try {
      const { contenido, nombre } = await api.getDescarga(`${rutaDeLaCarta}/imagen`);
      const enlace = document.createElement('a');
      enlace.href = URL.createObjectURL(contenido);
      enlace.download = nombre ?? nombreDeRespaldo(carta);
      enlace.click();
      setTimeout(() => URL.revokeObjectURL(enlace.href), 10_000);
    } catch (error) {
      setFallo(error);
    } finally {
      setPreparando(false);
    }
  }

  return (
    <div className="columna">
      <div className="fila">
        <Boton onClick={() => void descargar()} cargando={preparando} textoCargando={textosCarta.preparando}>
          {textosCarta.descargar}
        </Boton>
      </div>
      {fallo !== null && <Aviso tono="error">{mensajeDe(fallo)}</Aviso>}
    </div>
  );
}
