# Contrato de pantalla: campana y comunicados

**Funcionalidad**: `011-comunicados-y-campana` | **Fecha**: 2026-10-12

El lienzo de diseño (§24) no tiene la campana ni los comunicados. Este documento fija su
comportamiento con los componentes y tokens del proyecto; el propietario lo revisa en la validación.
Cumple RF-012 a RF-023 y RF-033 a RF-036.

## Campana (barra superior)

- Aparece solo en la zona del club y solo cuando el rol de la sesión es JUGADOR. Va a la izquierda
  del menú del perfil. Ningún otro rol la tiene (RF-019).
- Es un botón de icono, sin texto visible, con zona táctil de 44 × 44 px. Navega a
  `/club/:clubId/comunicados`.
- **Nombre accesible** (RF-034): "Comunicados, sin novedades" cuando el contador es 0; "Comunicados,
  3 sin leer" cuando es 3; "Comunicados, más de 99 sin leer" desde 100.
- **Insignia**: círculo con el número, en la esquina superior derecha del icono. Con 0 no hay
  insignia. Desde 100 muestra "99+". El número va escrito; el color de la insignia usa el color de
  acento del club, con contraste mínimo 4,5:1 contra su fondo en los dos temas.
- No parpadea ni se anima. El cambio del número lo anuncia una región de estado educada
  (`aria-live="polite"`), sin interrumpir.
- **Actualización** (research §10): al entrar al club, al volver la pestaña o la aplicación a
  primer plano, al cambiar de pantalla y cada 60 segundos mientras esté visible. Si la consulta
  falla, conserva el último número y no muestra error.
- En el teléfono ocupa el lugar de un solo botón de la barra; la marca y el perfil siguen
  visibles a 360 px.

## `/club/:clubId/comunicados` — JUGADOR

- Título "Comunicados". Botón "Marcar todos como leídos", deshabilitado sin comunicados sin leer.
- Lista del más reciente al más antiguo. Cada fila: título, nombre de quien lo publicó (abre su
  ficha básica, spec 008) con su rol escrito, fecha y estado en texto: **"Sin leer"** o **"Leído"**.
  El estado nunca se distingue solo por color (RF-015); además, el título de lo sin leer va en
  negrita.
- Tocar una fila abre el detalle en la misma pantalla (en escritorio, panel lateral; en teléfono,
  pantalla completa) y llama a la lectura. El contador baja con la respuesta.
- "Ver más" al final cuando `hayMas`. Sin comunicados: "Todavía no hay comunicados del club."
- El texto se pinta como texto, con saltos de línea; ningún enlace ni formato se interpreta
  (RF-036).

## `/club/:clubId/comunicados` — PRESIDENTE, DIRECTIVO y ENTRENADOR

- Título "Comunicados publicados" y botón "Nuevo comunicado".
- Lista del más reciente al más antiguo. Cada fila: título, a quién iba ("Todo el club",
  "Categoría 2015, 2016", "Equipos A y B de la 2015"), "N familias", quién lo publicó (solo para
  PRESIDENTE y DIRECTIVO) y estado en texto: **"Publicado"** o **"Retirado"** con quién y cuándo.
- "Retirar" solo donde `puedeRetirar`; pide confirmación con `DialogoConfirmacion`: "Dejará de verse
  en la campana de las familias. Esto no se puede deshacer."

## `/club/:clubId/comunicados/nuevo` — formulario

- **Título** (hasta 100) y **Texto** (hasta 1.000), con contador de caracteres restantes.
- **Para quién**: una de tres opciones, según el rol:
  - "Todo el club" — solo PRESIDENTE y DIRECTIVO.
  - "Categorías" — lista de categorías activas con casillas; para ENTRENADOR, solo las suyas.
  - "Equipos de una categoría" — primero una categoría, luego casillas con sus equipos activos.
- La `clave` se genera al abrir el formulario y se conserva en los reintentos (research §6). El
  botón "Publicar" se deshabilita mientras la petición va.
- Errores en el campo que corresponde: vacío, demasiado largo, "Elige al menos una categoría",
  "Esa categoría no tiene jugadores activos" (`sin_destinatarios`).
- Al publicar vuelve a la lista con un aviso "Comunicado publicado. Les llegará a N familias."

## Estados y accesibilidad

- Los dos temas y los colores de cualquier club (§24). Sin desplazamiento horizontal a 360 px.
- Foco visible; la lista se recorre con teclado; el diálogo de retiro atrapa el foco.
- Nada de lo anterior usa solo el color para distinguir leído de sin leer, ni publicado de
  retirado.

## Lo que la pantalla NO hace

- No pide permiso de notificaciones del sistema ni del navegador, ni muestra el número sobre el
  icono de la aplicación (RF-028).
- No edita un comunicado publicado (RF-011) ni lo vuelve a publicar tras retirarlo (RF-027).
- No muestra a la familia quién más lo recibió ni cuántos lo recibieron.
