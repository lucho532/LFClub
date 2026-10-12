import { DESTELLOS, FORMA_DESTELLO, LIENZO, trazadoDestello } from './plano';

/**
 * Los destellos de luz de la carta (efecto premium, contracts/carta.md): estrellas blancas de cuatro
 * puntas con un resplandor suave, en las mismas posiciones que dibuja la API. Son decorativos: no
 * tapan ningún dato y no se anuncian a los lectores de pantalla.
 */
export function Destellos() {
  return (
    <svg className="carta-destellos" viewBox={`0 0 ${LIENZO.ancho} ${LIENZO.alto}`} aria-hidden="true" focusable="false">
      <defs>
        <radialGradient id="carta-resplandor">
          <stop offset="0" stopColor="#FFFFFF" stopOpacity={FORMA_DESTELLO.opacidadResplandor} />
          <stop offset="1" stopColor="#FFFFFF" stopOpacity={0} />
        </radialGradient>
      </defs>
      {DESTELLOS.map(({ x, y, tamano }) => (
        <g key={`${x}-${y}`}>
          <circle cx={x} cy={y} r={tamano * FORMA_DESTELLO.resplandor} fill="url(#carta-resplandor)" />
          <path d={trazadoDestello(x, y, tamano)} fill="#FFFFFF" />
        </g>
      ))}
    </svg>
  );
}
