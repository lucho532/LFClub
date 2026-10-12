/**
 * Silueta de un jugador para la carta de quien no tiene foto (contracts/carta.md): del color del
 * texto de la carta con 30 % de opacidad. Es decorativa: el nombre ya está escrito en la carta.
 */
export function FiguraGenerica({ color }: { color: string }) {
  return (
    <svg className="carta-figura" viewBox="0 0 430 460" aria-hidden="true" focusable="false">
      <g fill={color} fillOpacity={0.3}>
        <circle cx="215" cy="150" r="92" />
        <path d="M40 460c0-110 78-190 175-190s175 80 175 190z" />
      </g>
    </svg>
  );
}
