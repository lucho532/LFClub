import { createContext, useContext, type ReactNode } from 'react';
import { useContadorComunicados } from './useContadorComunicados';

interface Valor {
  sinLeer: number;
  fijar: (sinLeer: number) => void;
  restarUno: () => void;
}

const SinContador: Valor = { sinLeer: 0, fijar: () => undefined, restarUno: () => undefined };
const Contexto = createContext<Valor>(SinContador);

interface Props {
  clubId: string;
  /** Solo la cuenta de un jugador tiene contador; los demás roles no consultan nada. */
  activo: boolean;
  children: ReactNode;
}

/**
 * Comparte el contador de comunicados sin leer entre la campana de la barra superior y las
 * pantallas que lo cambian al leer, para que la insignia baje con la respuesta de la lectura sin
 * otra consulta. No pinta nada propio.
 */
export function ProveedorContadorComunicados({ clubId, activo, children }: Props) {
  const contador = useContadorComunicados(clubId, activo);
  return <Contexto.Provider value={contador}>{children}</Contexto.Provider>;
}

/** El contador del club elegido; sin proveedor, siempre en cero y sin efecto al fijarlo. */
export function useContadorDelClub(): Valor {
  return useContext(Contexto);
}
