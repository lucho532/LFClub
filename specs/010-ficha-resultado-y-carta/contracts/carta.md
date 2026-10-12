# Plano de la carta del jugador

**Funcionalidad**: `010-ficha-resultado-y-carta` | **Fecha**: 2026-10-11

Este plano lo siguen las dos versiones de la carta: la de la pantalla (HTML y CSS, `CartaJugador.tsx`)
y la imagen que dibuja la API (`GeneradorImagenCarta`). Que las dos lo sigan es lo que hace que la
imagen exportada sea "la carta tal como se ve en pantalla" (RF-030). Un cambio de diseño se hace
aquí primero y después en los dos lados. Las medidas están en unidades del lienzo de la imagen
(750 × 1050); la pantalla las usa como porcentajes de ese lienzo.

## Lienzo

- 750 × 1050 px, proporción 5:7 (2,5 × 3,5 pulgadas a 300 ppp). En pantalla, ancho máximo 320 px
  en teléfono y 360 px en escritorio, sin desplazamiento horizontal (CE-008).
- Esquinas redondeadas de radio 48. Fuera de la carta, en la imagen, transparente.

## Capas, de atrás hacia delante

| Capa | Posición y tamaño | Contenido |
| --- | --- | --- |
| Fondo | Todo el lienzo | Degradado lineal de arriba a la izquierda (color principal del club) a abajo a la derecha (color de acento). Sin acento: del principal a su versión 35 % más oscura. Sin colores (identidad neutra): los neutros del tema claro |
| Brillo | Todo el lienzo | Reflejo blanco: dos bandas diagonales a 115° (del 20 % al 55 % con máximo de 35 % de opacidad en el 38 %; y del 60 % al 76 % con máximo de 20 % en el 68 %) y un halo arriba a la izquierda (centro x 225, y 0; radio 630; de 35 % a 0) |
| Marco | Borde de 18 hacia dentro | Degradado metálico de arriba abajo con el color de la rareza (Bronce `#B0793F`, Plata `#BFC5CC`, Oro `#D9A62E`): arriba mezclado al 55 % con blanco, en medio el color, abajo mezclado al 40 % con negro |
| Valoración | x 60–220, y 70–230 | Número de la valoración, Barlow Condensed 800, 120 px. Sin estadísticas: vacío |
| Posición | Debajo de la valoración, y 235–285 | Abreviatura: POR, DEF, MED, DEL. Sin posición: vacía |
| Escudo | x 70–200, y 300–430 | Escudo del club ajustado sin deformar, recortado en óvalo con borde suave (opaco hasta el 86 % del radio menor de la caja, transparente en el 100 %): no se ve el recuadro de la imagen. Sin escudo, nada |
| Foto | x 260–690, y 60–520 | Foto del jugador recortada al centro para llenar la caja y fundida con el fondo, sin recuadro: opaca hasta el 62 % del alto y transparente abajo, y con los lados (8 % del ancho) también difuminados; sin foto, figura genérica (silueta) del color del texto con 30 % de opacidad |
| Dorsal | Esquina inferior derecha de la foto, x 590–690, y 440–520 | Número, Barlow Condensed 800, 72 px. Sin dorsal: vacío |
| Nombre | x 60–690, y 540–640, centrado | "Nombres Apellidos" en mayúsculas, Barlow Condensed 700. Empieza en 64 px y se reduce hasta 36 px para caber en una línea; si aún no cabe, dos líneas, y lo que sobre termina en "…" |
| Línea | y 660, x 120–630 | Línea de 3 px del color del texto con 50 % de opacidad |
| Estadísticas | Dos columnas de tres, x 110–360 y x 400–650, filas en y 700, 790 y 880 | Valor (Barlow Condensed 800, 60 px) y sigla (700, 44 px): RIT, TIR, PAS a la izquierda; REG, DEF, FÍS a la derecha. Sin estadísticas: las siglas con "–" en lugar de números |
| Rareza | y 960–1010, centrada | "BRONCE", "PLATA" u "ORO" escrito, Barlow Condensed 700, 36 px (el estado por texto de RF-034) |
| Destellos | Encima de todo, en (120, 44), (375, 10), (712, 92), (40, 512), (726, 700), (86, 1004) y (664, 1040), con tamaños 22, 16, 30, 18, 20, 26 y 16 | Efecto premium: estrella blanca de cuatro puntas (medio largo = tamaño; cintura al 16 %) sobre un resplandor blanco de radio 1,1 × tamaño, de 55 % a 0 de opacidad. No tapan ningún dato |

## Color del texto

Blanco o casi negro (`#141414`), el que tenga más contraste con el color medio del degradado,
con la misma fórmula de luminancia de `contraste.ts` (WCAG). Si el contraste del elegido con
cualquiera de los dos extremos del degradado baja de 4,5:1, el texto lleva un halo del color
contrario con 85 % de opacidad (contorno de 2 px y desenfoque de 6 px). Así la carta se lee con colores del club poco contrastados
entre sí (caso límite 11). La carta no cambia con el tema claro u oscuro: tiene su propio fondo;
solo cambia el marco de la pantalla que la rodea.

## Lo que la carta nunca lleva

Ni documento, ni fecha de nacimiento, ni contacto, ni datos de salud, ni categoría, ni porcentaje de
asistencia, ni fecha de exportación (RF-030). Solo foto, nombre, dorsal, posición, escudo, colores,
estadísticas, valoración y rareza.

## Fuentes

Barlow Condensed (licencia SIL Open Font License), pesos 700 y 800. La pantalla la carga como
el resto de la aplicación; la API la lleva como recurso incrustado para no depender de las fuentes
del contenedor.
