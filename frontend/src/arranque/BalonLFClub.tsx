import { useId } from 'react';
import { BRILLO, CONTORNO, PANELES, SOMBRA } from './geometriaBalon';

interface Props {
  /** Lado del balón en píxeles. */
  tamano: number;
  className?: string;
}

/**
 * El balón de LFClub: blanco con seis paneles azules en estrella, sombra y brillo. Es el mismo
 * dibujo del icono y de la pantalla de arranque de la aplicación Android. Solo dibuja: los paneles
 * van en su propio grupo (`balon-paneles`) para que la animación de arranque los haga girar sin
 * mover la luz. Es decorativo y no se anuncia a los lectores de pantalla.
 */
export function BalonLFClub({ tamano, className }: Props) {
  const recorte = useId();

  return (
    <svg className={className} viewBox="24 24 60 60" width={tamano} height={tamano} aria-hidden="true">
      <defs>
        <clipPath id={recorte}>
          <path d={CONTORNO} />
        </clipPath>
      </defs>
      <path d={CONTORNO} fill="#FFFFFF" />
      <g clipPath={`url(#${recorte})`}>
        <g className="balon-paneles">
          <path d={PANELES} fill="#24448C" stroke="#FFFFFF" strokeWidth={0.9} strokeLinejoin="round" />
        </g>
        <path d={SOMBRA} fill="#0F1F45" fillOpacity={0.22} fillRule="evenodd" />
        <path d={BRILLO} fill="#FFFFFF" fillOpacity={0.45} />
      </g>
      <path d={CONTORNO} fill="none" stroke="#0F1F45" strokeWidth={1.2} />
    </svg>
  );
}
