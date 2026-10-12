# Contrato de pantalla: campana con avisos y ficha de identidad

**Funcionalidad**: `012-avisos-de-actividades` | **Fecha**: 2026-10-12

Completa [la 011](../../011-comunicados-y-campana/contracts/campana.md): solo se dice lo que
cambia. El lienzo de diseño (§24) no tiene estas pantallas; el propietario las revisa en la
validación. Cumple RF-012 a RF-034 y los RF-036 a RF-038 del ajuste de la ficha.

## Campana (barra superior)

- Sin cambios de lugar, tamaño ni actualización (al entrar, al volver a primer plano, al cambiar de
  pantalla y cada 60 segundos).
- **Nombre accesible** (RF-034): con 0, "Novedades, nada por leer"; con 3, "Novedades, 3 sin
  leer"; desde 100, "Novedades, más de 99 sin leer".
- El número cuenta comunicados y avisos; desde 100 muestra "99+".

## `/club/:clubId/comunicados` — JUGADOR ("Novedades")

- Título "Novedades". Botón "Marcar todos como leídos", deshabilitado sin elementos sin leer;
  marca comunicados y avisos.
- Lista mezclada del más reciente al más antiguo. Cada fila dice en texto **"Comunicado"** o
  **"Aviso"** y **"Sin leer"** o **"Leído"** (nunca solo por color ni solo por icono).
- **Fila de comunicado**: igual que en la 011 (título, autor con su rol, fecha).
- **Fila de aviso**: el texto del aviso (una o dos líneas, sin título), la fecha y el estado. Sin
  autor. Lo sin leer va en negrita.
- Tocar un aviso llama a su lectura y:
  - si `actividad.disponible`, navega a `/club/:clubId/entrenamientos/:id` o
    `/club/:clubId/partidos/:id`; el detalle ya muestra "Cancelado" cuando corresponde;
  - si no, queda en la lista, el aviso pasa a "Leído" y debajo de la fila aparece "Esa actividad ya
    no está disponible para ti." (región de estado educada, sin diálogo).
- "Ver más" al final cuando `hayMas`. Sin elementos: "Todavía no hay novedades del club."
- Texto siempre como texto plano; ningún enlace ni formato se interpreta.

## `/club/:clubId/comunicados` — PRESIDENTE, DIRECTIVO y ENTRENADOR

Sin cambios: "Comunicados publicados". No tienen campana ni avisos.

## Ficha del jugador: tarjeta "Identidad" (ajuste del propietario, bloque B del plan)

Hoy la tarjeta tiene dos botones de texto, "Cambiar documento" y "Corregir nombre y fecha". Pasa a
tener **un solo botón de icono: un lápiz** en la esquina superior derecha de la tarjeta.

- **Nombre accesible**: "Editar identidad". Zona táctil de 44 × 44 px. Mismo lenguaje visual que el
  lápiz de la foto (borde y fondo de superficie, foco visible).
- Se muestra a quien tiene alguno de los dos permisos de hoy: `permisos.puedeCambiar` o
  `permisos.puedeCorregirIdentidad`. Para los demás la tarjeta es de solo vista, sin lápiz.
- Abre **un solo diálogo**, "Editar identidad", con Nombres, Apellidos, Fecha de nacimiento, Tipo de
  documento y Número de documento. Cada grupo se habilita con su permiso: nombres, apellidos y
  fecha con `puedeCorregirIdentidad`; tipo y número con `puedeCambiar`. Lo que no puede editar se ve
  como texto.
- **El documento sigue editable**, con las reglas de siempre (§10): unicidad por club y cuenta,
  errores en el campo.
- Al guardar envía solo lo que cambió, **primero el documento** y después la identidad. Si el
  documento falla, el error se muestra en su campo y no se envía lo demás.
- **Categoría y equipos** siguen en la tarjeta como solo vista; los cambia el PRESIDENTE con sus
  flujos de siempre (§11), no con este lápiz.
- Se eliminan `DialogoCambiarDocumento` y el botón de texto de cada diálogo. A 360 px la tarjeta no
  desplaza horizontalmente y el lápiz no tapa el título "Identidad".

## Estados y accesibilidad

- Los dos temas y los colores de cualquier club (§24); sin desplazamiento horizontal a 360 px.
- Foco visible; el diálogo atrapa el foco y se cierra con Escape.
- El aviso nunca depende del color para decir si está leído.

## Lo que la pantalla NO hace

- No pide permiso de notificaciones ni muestra número sobre el icono de la aplicación (RF-027).
- No edita, retira ni responde un aviso.
