# Guía de validación: avisos de actividades y ficha de identidad

**Funcionalidad**: `012-avisos-de-actividades` | **Fecha**: 2026-10-12

Sirve para comprobar de extremo a extremo que la funcionalidad cumple la spec. No describe cómo
implementarla. El contrato está en [contracts/api.yaml](contracts/api.yaml), las pantallas en
[contracts/campana.md](contracts/campana.md) y el modelo en [data-model.md](data-model.md).

## Requisitos previos

- Docker Desktop en marcha. Si se duerme sin contenedores, dejar uno mínimo en marcha durante la
  validación y borrarlo al final.
- SDK de .NET 10 y Node 22 (solo para ejecutar las pruebas fuera de Docker).
- Puesta en marcha como la de la [001](../001-base-multiclub/quickstart.md), en un proyecto de
  Compose aparte (`docker compose -p la-pecosa-validacion …`) con `Brevo__Llave` vacía, para no
  enviar correos reales ni tocar los datos de desarrollo. Aplicación en <http://localhost:5173>, API
  en <http://localhost:8080>.
- La migración `AvisosDeActividad` se aplica sola al arrancar la API.

## Datos de partida

Los de las guías de la [009](../009-entrenamientos-y-partidos/quickstart.md) y la
[011](../011-comunicados-y-campana/quickstart.md): club A con P, E1 (2014), E2 (2014 y 2015), D1
(asignado a la 2014), D2 (sin asignar), J1 a J10 en la 2014 (J1 y J2 hermanos, J2 en la 2015), K1 en
la 2015, M1 en la 2016, J11 sin categoría, J12 en espera y J10 retirado; club B con PB.

## Bloque 1: entrenamientos (historia 1)

1. Como E1, programar un entrenamiento de la 2014 para el sábado a las 9:00. Como J3, la campana
   suma 1 y la lista dice "Aviso", "Sin leer" y "Nuevo entrenamiento el sábado a las 9:00".
2. Como K1 (2015) y como M1 (2016): sin aviso. Como J10 (retirado) y J12 (en espera): sin aviso.
3. Como E2, programar uno de la 2014 y la 2015: J2, hermano en las dos, recibe **un** aviso.
4. Cambiar solo la hora: el aviso de J3 se **reemplaza** y el contador no sube. Cambiar solo la
   nota: no hay aviso nuevo.
5. Cancelar y reactivar: avisos "Entrenamiento del sábado cancelado" y "…sigue en pie".
6. Registrar la asistencia de un entrenamiento pasado: aviso "Se registró la asistencia…" sin
   nombres. Volver a guardar lo mismo: ningún aviso nuevo.
7. Como J3, tocar un aviso: queda "Leído", el contador baja y abre el detalle del entrenamiento.

## Bloque 2: partidos (historia 2)

1. Como E1, crear un partido sin convocados: nadie recibe aviso.
2. Convocar a J1 y J3: reciben "Mateo fue convocado al partido contra …"; J4 no recibe nada y, con
   la API, `GET …/partidos/{id}` de J4 responde 404.
3. Convocar a J1 y J2 (hermanos) en un solo guardado: su cuenta recibe **un** aviso que nombra a los
   dos.
4. Cambiar el lugar: solo reciben aviso los convocados. Cancelar y reactivar: igual.
5. Sacar a J3 de la convocatoria: nadie recibe aviso por eso. Si J3 tenía sin leer el aviso de que
   lo convocaron, al tocarlo queda "Leído" y dice "Esa actividad ya no está disponible para ti." sin
   abrir nada.
6. Registrar el resultado como E1: aviso con el marcador a las familias de **toda** la categoría,
   también las no convocadas. Corregir solo los goles de un jugador: sin aviso.
7. Con otro partido, publicar el marcador como la familia de un convocado: las demás familias de la
   categoría lo reciben; quien lo publicó, no.

## Bloque 3: la campana (historia 3)

1. Con un comunicado y dos avisos sin leer, la campana muestra 3 y su nombre accesible dice
   "Novedades, 3 sin leer".
2. La lista mezcla comunicados y avisos por fecha; cada fila dice en texto "Comunicado" o "Aviso".
3. "Marcar todos como leídos" deja el contador en cero y marca los dos tipos.
4. Con más de 99 sin leer, la campana muestra "99+".
5. Dos familias con el mismo aviso: leerlo en una no cambia la otra.
6. Como E1, D1 y P, `GET …/campana` responde 403; como el DESARROLLADOR, 404.
7. Sesión iniciada con el documento de J1 (hermano de J2): solo ve los avisos de J1.

## Bloque 4: sin ruido y sin avisos externos (historia 4)

1. Cambiar tres veces la hora sin que J3 lea: J3 tiene un solo aviso sin leer de esa actividad.
2. Leer ese aviso y cambiar otra vez: llega uno nuevo y el leído se queda.
3. Con `Brevo__Llave` vacía y el servicio de correo observado, ninguna de las acciones anteriores
   invoca el envío de correo. La aplicación no pide permiso de notificaciones.
4. Cerrar la aplicación, hacer un cambio como E1 y volver a abrir como J3: el aviso está al abrir;
   no llegó nada antes.

## Bloque 5: ficha de identidad (bloque B del plan)

1. Como la cuenta de J3 (o como P), abrir la ficha: la tarjeta "Identidad" tiene **un lápiz** y
   ningún botón "Cambiar documento" ni "Corregir nombre y fecha".
2. Pulsar el lápiz: el diálogo muestra nombres, apellidos, fecha de nacimiento, tipo y número de
   documento, todos editables para quien tiene ambos permisos.
3. Cambiar solo el nombre: se envía solo la corrección de identidad y el documento no se toca.
4. Cambiar el número de documento por uno que ya existe en el club: el error sale en el campo y no
   se guarda nada, tampoco el nombre si también se cambió.
5. Cambiar nombre y documento a valores válidos: se guardan los dos y la tarjeta se actualiza.
6. Como quien solo tiene uno de los dos permisos, el otro grupo de campos se ve como texto.
7. Como quien no tiene ninguno, la tarjeta se ve sin lápiz.
8. A 360 px la tarjeta no desplaza horizontalmente y el lápiz no tapa el título.

## Cierre

- Contrato contra Swagger: 109 operaciones y 85 de club, con 404 a otro club y al DESARROLLADOR.
- Batería completa del backend y del frontend en verde, incluidas la 001 a la 011.
- Revisión a 360 px y a escritorio, en tema claro y oscuro, con los colores de al menos dos clubes.

## Notas de la validación (2026-10-12)

Lo que se comprobó al implementar y lo que queda para el propietario:

- **Bloques 1 a 4 y 5 (lógica)**: cubiertos por las pruebas de integración sobre PostgreSQL 17
  (`backend/pruebas/Integracion/Avisos/`) y de Vitest (`FilaDeAviso`, `DialogoEditarIdentidad`,
  `SeccionIdentidad`, `SinPermisoDeNotificaciones`). El texto de los avisos sale con "mañana", "hoy" o
  el día de la semana según la fecha; en una prueba con el sábado de la guía el aviso dice
  "el sábado".
- **Orden de los identificadores en la mezcla (bloque 3)**: en .NET 10 `Guid.CompareTo`
  coincide con el orden de bytes de red (no hay parejas que difieran), así que la prueba compara
  `OrdenDeIdentificadores` con el texto hexadecimal, no con `CompareTo` (research §3 queda como
  contrato, no como trampa).
- **No ejecutado en Compose aparte**: el stack de desarrollo ocupa los puertos 8080 y 5173; levantar
  un segundo proyecto exigía mover puertos y reconstruir las imágenes. Queda para el propietario el
  recorrido por pantalla de los bloques 1 a 5 y la revisión visual a 360 px, en los dos temas y con
  los colores de dos clubes (T029 y T030 de `tasks.md`).
