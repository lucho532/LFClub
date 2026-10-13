import type { CSSProperties } from 'react';
import { BalonLFClub } from '../arranque/BalonLFClub';
import './fondoDeBalones.css';

/** Cantidad de balones del fondo: suficientes para llenar una pantalla grande sin tapar el formulario. */
const CANTIDAD = 40;

/** Giros de tono (en grados) que dan a los paneles azules del balón otros colores; 0 es el azul original. */
const TONOS = [0, 150, 40, 300, 200, 90, 340];

/** Un balón del fondo, con valores fijos calculados por su posición para que no cambien al repintar. */
function balon(indice: number) {
  const semilla = (indice * 7919) % 997;
  return {
    izquierda: (indice * 37 + (semilla % 13)) % 100,
    tamano: 18 + (semilla % 27),
    giro: 2 + (semilla % 4),
    caida: 3.5 + (semilla % 50) / 10,
    retraso: -((semilla % 90) / 10),
    sentido: indice % 2 === 0 ? 1 : -1,
    tono: TONOS[indice % TONOS.length],
  };
}

/**
 * Fondo decorativo de la pantalla de entrar: una lluvia de balones pequeños, los mismos de la
 * aplicación, que caen girando detrás del formulario. No recibe clics, no se anuncia a los lectores
 * de pantalla y se queda quieto si la persona pidió menos movimiento.
 */
export function FondoDeBalones() {
  return (
    <div className="fondo-balones" aria-hidden="true">
      {Array.from({ length: CANTIDAD }, (_, indice) => {
        const b = balon(indice);
        return (
          <span
            key={indice}
            className="fondo-balon"
            style={
              {
                left: `${b.izquierda}%`,
                animationDuration: `${b.caida}s`,
                animationDelay: `${b.retraso}s`,
                '--tono': `${b.tono}deg`,
                '--giro': `${b.giro}s`,
                '--sentido': b.sentido === 1 ? 'normal' : 'reverse',
              } as CSSProperties
            }
          >
            <BalonLFClub tamano={b.tamano} className="fondo-balon-giro" />
          </span>
        );
      })}
    </div>
  );
}
