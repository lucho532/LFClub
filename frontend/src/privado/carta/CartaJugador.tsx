import type { CSSProperties } from 'react';
import { api } from '../../compartido/api/cliente';
import type { IdentidadClubDto } from '../../compartido/api/tipos';
import type { CartaDto, ValoresCartaDto } from '../../compartido/api/tiposCarta';
import { useFotoJugador } from '../jugadores/fotosJugador';
import { Destellos } from './Destellos';
import { FiguraGenerica } from './FiguraGenerica';
import {
  ABREVIATURAS,
  ajustarNombre,
  BRILLO,
  CAJAS,
  type Caja,
  colorDelTexto,
  degradado,
  ESTADISTICAS,
  FUNDIDOS,
  LETRAS,
  LIENZO,
  type Medidor,
  medirAproximado,
  SOMBRA,
  TEXTO_DE_RAREZA,
  tonosDelMarco,
} from './plano';

interface Props {
  clubId: string;
  carta: CartaDto;
  identidad: IdentidadClubDto;
}

const ORDEN: (keyof ValoresCartaDto)[][] = [
  ['ritmo', 'tiro', 'pase'],
  ['regate', 'defensa', 'fisico'],
];

/** Escudo en óvalo de borde suave y foto fundida con el fondo, sin recuadro. */
const OVALO_ESCUDO = `radial-gradient(closest-side, #000 ${FUNDIDOS.escudoOpaco * 100}%, transparent 100%)`;
const FUNDIDO_FOTO = [
  `linear-gradient(to bottom, #000 ${FUNDIDOS.fotoOpacaHasta * 100}%, transparent 100%)`,
  `linear-gradient(to right, transparent, #000 ${FUNDIDOS.fotoLados * 100}%, #000 ${100 - FUNDIDOS.fotoLados * 100}%, transparent)`,
].join(', ');

/** Una máscara CSS (varias capas se cruzan: solo queda lo opaco en todas). */
const mascara = (capas: string): CSSProperties => ({
  maskImage: capas,
  WebkitMaskImage: capas,
  maskComposite: 'intersect',
  WebkitMaskComposite: 'source-in',
});

/** Un color #RRGGBB con opacidad, como #RRGGBBAA. */
const conOpacidad = (color: string, opacidad: number) => `${color}${Math.round(opacidad * 255).toString(16).padStart(2, '0')}`;

/** Una medida del lienzo como porcentaje del ancho o del alto de la carta. */
const ancho = (valor: number) => `${(valor / LIENZO.ancho) * 100}%`;
const alto = (valor: number) => `${(valor / LIENZO.alto) * 100}%`;
/** Un tamaño del lienzo como unidad del contenedor: la carta escala con su ancho. */
const letra = (valor: number) => `${(valor / LIENZO.ancho) * 100}cqw`;

const posicionDe = (caja: Caja): CSSProperties => ({
  left: ancho(caja.x),
  top: alto(caja.y),
  width: ancho(caja.ancho),
  height: alto(caja.alto),
});

// Mide con la fuente de verdad si el navegador puede; si no, con la aproximación del plano.
let lienzo: CanvasRenderingContext2D | null | undefined;
const medir: Medidor = (texto, tamano) => {
  if (lienzo === undefined) {
    lienzo = typeof navigator !== 'undefined' && !navigator.userAgent.includes('jsdom')
      ? document.createElement('canvas').getContext('2d')
      : null;
  }

  if (!lienzo) {
    return medirAproximado(texto, tamano);
  }

  lienzo.font = `700 ${tamano}px 'Barlow Condensed'`;
  return lienzo.measureText(texto).width;
};

/**
 * La carta del jugador dibujada con HTML y CSS siguiendo el plano común (contracts/carta.md), el
 * mismo que usa la imagen que exporta la API: degradado con los colores del club, reflejo con destellos
 * de luz, marco metálico del color de la rareza, valoración, posición, escudo, foto o figura genérica, dorsal, nombre, estadísticas y
 * la rareza escrita. Sin estadísticas, las siglas llevan "–". No lleva el porcentaje de asistencia ni
 * ningún otro dato (supuesto 4, RF-030). No cambia con el tema: tiene su propio fondo.
 */
export function CartaJugador({ clubId, carta, identidad }: Props) {
  const extremos = degradado(identidad.colorPrincipal, identidad.colorAcento);
  const { texto, sombra } = colorDelTexto(extremos);
  const foto = useFotoJugador(clubId, carta.usuarioRolId, carta.fotoActualizadaEn ?? null);
  const nombre = ajustarNombre(carta.nombres, carta.apellidos, medir);
  const halo = sombra && conOpacidad(sombra, SOMBRA.opacidad);
  const estilo: CSSProperties = {
    background: `linear-gradient(to bottom right, ${extremos[0]}, ${extremos[1]})`,
    borderRadius: letra(LIENZO.radio),
    color: texto,
    textShadow: halo
      ? `0 0 ${letra(SOMBRA.contorno)} ${halo}, 0 0 ${letra(SOMBRA.contorno)} ${halo}, 0 0 ${letra(SOMBRA.desenfoque)} ${halo}`
      : undefined,
  };
  const blanco = (opacidad: number) => conOpacidad('#FFFFFF', opacidad);
  const brillo: CSSProperties = {
    background: [
      ...BRILLO.bandas.map(
        ({ inicio, maximo, fin, opacidad }) =>
          `linear-gradient(${BRILLO.angulo}deg, transparent ${inicio * 100}%, ${blanco(opacidad)} ${maximo * 100}%, transparent ${fin * 100}%)`,
      ),
      `radial-gradient(circle ${letra(BRILLO.halo.radio)} at ${ancho(BRILLO.halo.x)} ${alto(BRILLO.halo.y)}, ${blanco(BRILLO.halo.opacidad)}, transparent)`,
    ].join(', '),
  };
  const [claro, medio, oscuro] = tonosDelMarco(carta.rareza);

  return (
    <div className="carta">
      <figure className="carta-lienzo" style={estilo} aria-label={`Carta de ${carta.nombres} ${carta.apellidos}`}>
        <span className="carta-brillo" style={brillo} />
        <span className="carta-caja carta-valoracion" style={{ ...posicionDe(CAJAS.valoracion), fontSize: letra(LETRAS.valoracion) }}>
          {carta.valoracion ?? ''}
        </span>
        <span className="carta-caja carta-posicion" style={{ ...posicionDe(CAJAS.posicion), fontSize: letra(LETRAS.posicion) }}>
          {carta.posicion ? ABREVIATURAS[carta.posicion] : ''}
        </span>
        {identidad.urlEscudo && (
          <img className="carta-caja carta-escudo" style={{ ...posicionDe(CAJAS.escudo), ...mascara(OVALO_ESCUDO) }} src={api.url(identidad.urlEscudo)} alt="" />
        )}
        <span className="carta-caja carta-foto" style={{ ...posicionDe(CAJAS.foto), ...mascara(FUNDIDO_FOTO) }}>
          {foto ? <img src={foto} alt="" /> : <FiguraGenerica color={texto} />}
        </span>
        <span className="carta-caja carta-dorsal" style={{ ...posicionDe(CAJAS.dorsal), fontSize: letra(LETRAS.dorsal) }}>
          {carta.dorsal ?? ''}
        </span>
        <span className="carta-caja carta-nombre" style={{ ...posicionDe(CAJAS.nombre), fontSize: letra(nombre.tamano) }}>
          {nombre.lineas.map((linea) => (
            <span key={linea}>{linea}</span>
          ))}
        </span>
        <span className="carta-caja carta-linea" style={{ ...posicionDe(CAJAS.linea), background: texto }} />
        {ESTADISTICAS.columnas.map((columna, indiceColumna) =>
          ESTADISTICAS.filas.map((fila, indiceFila) => (
            <span
              key={ESTADISTICAS.siglas[indiceColumna][indiceFila]}
              className="carta-caja carta-estadistica"
              style={posicionDe({ x: columna.x, y: fila, ancho: columna.ancho, alto: ESTADISTICAS.altoFila })}
            >
              <span style={{ fontSize: letra(LETRAS.valorEstadistica) }}>
                {carta.estadisticas ? carta.estadisticas[ORDEN[indiceColumna][indiceFila]] : '–'}
              </span>
              <span style={{ fontSize: letra(LETRAS.siglaEstadistica) }}>{ESTADISTICAS.siglas[indiceColumna][indiceFila]}</span>
            </span>
          )),
        )}
        <span className="carta-caja carta-rareza" style={{ ...posicionDe(CAJAS.rareza), fontSize: letra(LETRAS.rareza) }}>
          {TEXTO_DE_RAREZA[carta.rareza]}
        </span>
        <span
          className="carta-marco"
          style={{
            padding: letra(LIENZO.marco),
            borderRadius: letra(LIENZO.radio),
            background: `linear-gradient(to bottom, ${claro}, ${medio}, ${oscuro})`,
          }}
        />
        <Destellos />
      </figure>
    </div>
  );
}
