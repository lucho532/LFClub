import { DisposicionCuenta } from './DisposicionCuenta';

/** Grupo de Google al que pertenecen los probadores de la prueba cerrada. */
export const ENLACE_GRUPO_PROBADORES = 'https://groups.google.com/g/lfmova-probadores';

/** Página de Google Play donde la persona acepta ser probadora de la prueba cerrada. */
export const ENLACE_ACEPTAR_PRUEBA = 'https://play.google.com/apps/testing/com.lfclub.app';

/** Ficha de LFClub en Google Play, desde donde se instala. */
export const ENLACE_INSTALAR = 'https://play.google.com/store/apps/details?id=com.lfclub.app';

interface PasoProps {
  titulo: string;
  ayuda: string;
  boton: string;
  enlace: string;
}

function Paso({ titulo, ayuda, boton, enlace }: PasoProps) {
  return (
    <section className="tarjeta columna">
      <h2>{titulo}</h2>
      <p className="texto-suave">{ayuda}</p>
      <a className="boton boton-principal" href={enlace}>
        {boton}
      </a>
    </section>
  );
}

/**
 * Página pública (/probadores) a la que lleva el código QR de la prueba cerrada: reúne en una
 * sola pantalla los tres pasos para probar LFClub antes de su publicación (unirse al grupo,
 * aceptar la prueba e instalar), con un botón por paso y en orden. No exige sesión.
 */
export function Probadores() {
  return (
    <DisposicionCuenta
      titulo="Prueba LFClub"
      subtitulo="Ayúdanos a probar la app antes de su lanzamiento en Google Play. Son tres pasos y toman un minuto."
    >
      <div className="columna">
        <p>
          Hazlo desde tu teléfono Android, con la misma cuenta de Google que usas en Play Store, y en este
          orden.
        </p>
        <Paso
          titulo="Únete al grupo de probadores"
          ayuda="Se abre Grupos de Google: pulsa «Unirse al grupo» y confirma."
          boton="1 · Unirme al grupo"
          enlace={ENLACE_GRUPO_PROBADORES}
        />
        <Paso
          titulo="Acepta ser probador"
          ayuda="Se abre Google Play: pulsa «Convertirme en verificador»."
          boton="2 · Aceptar la prueba"
          enlace={ENLACE_ACEPTAR_PRUEBA}
        />
        <Paso
          titulo="Instala la app"
          ayuda="Si acabas de aceptar, puede tardar unos minutos en aparecer el botón «Instalar»."
          boton="3 · Instalar LFClub"
          enlace={ENLACE_INSTALAR}
        />
        <p className="texto-suave">
          Por favor, deja la app instalada y sigue en la prueba al menos 14 días: Google lo exige para poder
          publicarla. ¿Algo no funciona? Escríbenos a <a href="mailto:luisrak25@gmail.com">luisrak25@gmail.com</a>.
        </p>
      </div>
    </DisposicionCuenta>
  );
}
