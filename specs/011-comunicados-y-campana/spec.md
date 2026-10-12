# Especificación de funcionalidad: Comunicados del club y campana de notificaciones

**Rama de la funcionalidad**: `011-comunicados-y-campana`

**Creada**: 2026-10-12

**Estado**: Borrador

**Entrada**: Descripción del propietario del proyecto: "Comunicados que publican entrenadores,
presidente y directivos, y campana de notificaciones con contador en las pantallas del jugador.
Sin avisos al teléfono con la app cerrada."

**Plataforma**: La Pecosa

**Constitución aplicable**: versión 4.9.0, en especial §1, §7.1, §7.5, §8 (Presidente, Directivo,
Entrenador y Jugador), §11, §11.3, §12.1.1, §12.6, §13, §14, §14.4, §15, §20, §24, §25 y §28.

**Depende de**: spec 001 (base multiclub y colores del club), spec 003 (categorías y equipos),
spec 004 (invitación con rol), spec 007 (Inicio por rol) y spec 008 (nombres clicables y ficha
básica).

**Cambia**: la spec 009 dejó los "mensajes de convocatoria" de §1 para la funcionalidad de
comunicados y dijo que no se enviaban notificaciones por entrenamientos y partidos. Esta
especificación crea los comunicados; los avisos automáticos por entrenamientos y partidos siguen
fuera (ver "Fuera de alcance").

## Impacto en la constitución

Esta funcionalidad necesitaba una enmienda (4.8.1 → 4.9.0), porque la §8 no dejaba al DIRECTIVO
publicar comunicados:

- §8 (Directivo): nueva función de publicar comunicados. Hoy el DIRECTIVO solo consulta, y la §8
  (Entrenador) dice que los comunicados del DIRECTIVO asignado "se decidirán con esas
  funcionalidades": esta es esa decisión.
- §8 (Presidente y Entrenador): ya recogen enviar comunicados; se precisa a quién llegan (RF-004 a
  RF-007).
- §8 (Jugador): nueva función de consultar los comunicados que le llegan y de tener una campana
  con contador.
- §14: un comunicado no se elimina físicamente; se retira.
- §20: pruebas nuevas de a quién llega cada comunicado y de quién publica.
- §28: sin decisiones nuevas. La §1 ("mensajes de convocatoria") queda como está: esos mensajes no
  se construyen aquí.

**Aplicado**: la enmienda 4.8.1 → 4.9.0 (2026-10-12) recoge todo lo anterior en la §8, la nueva
§14.4, la §20 y la §24. No hace falta otra enmienda para construir lo que aquí se especifica.

## Aclaraciones

### Sesión 2026-10-12

- P: ¿Qué se construye ahora? → R: Solo la campana dentro de la aplicación, alimentada por
  comunicados que publica el equipo del club. Los avisos al teléfono con la aplicación cerrada
  (y el número sobre el icono) quedan para una especificación posterior.
- P: ¿Quién publica? → R: Los entrenadores, el presidente y los directivos.
- P: ¿Quién recibe y ve la campana? → R: La cuenta del jugador (la familia), en todas sus
  pantallas.

## Escenarios de usuario y pruebas *(obligatorio)*

En esta especificación, "la familia" es la cuenta de un jugador (rol JUGADOR), que usa su padre,
madre o responsable. "El equipo del club" son el PRESIDENTE, los DIRECTIVOS y los ENTRENADORES.
Un "comunicado" es un mensaje escrito (título y texto) que el equipo del club dirige a las
familias.

### Historia de usuario 1 - El equipo del club publica un comunicado (Prioridad: P1)

Un entrenador, el presidente o un directivo escribe un título y un texto, elige a quién va
dirigido (todo el club, una o varias categorías, o uno o varios equipos de una categoría) y lo
publica. Desde ese momento las familias que corresponden lo reciben dentro de la aplicación.

**Por qué esta prioridad**: sin comunicados no hay nada que mostrar en la campana. Es lo que le
da valor a toda la funcionalidad: hoy el club no tiene cómo hablarles a las familias desde la
plataforma.

**Prueba independiente**: se prueba entrando con cada rol, publicando un comunicado y comprobando,
con las cuentas de varias familias, quién lo recibe y quién no.

**Escenarios de aceptación**:

1. **Dado** un entrenador asignado a la categoría 2015, **cuando** publica un comunicado dirigido a
   esa categoría, **entonces** lo reciben las familias de los jugadores activos de la 2015 y
   ninguna otra.
2. **Dado** un entrenador asignado a la categoría 2015, **cuando** intenta dirigir un comunicado a
   la 2016 o a todo el club, **entonces** el sistema no se lo permite: solo ofrece sus categorías.
3. **Dado** el presidente, **cuando** publica un comunicado dirigido a todo el club, **entonces**
   lo reciben las familias de todos los jugadores activos del club.
4. **Dado** un directivo, **cuando** publica un comunicado dirigido a dos categorías, **entonces**
   lo reciben las familias de los jugadores activos de esas dos categorías.
5. **Dado** una categoría dividida en equipos, **cuando** se publica un comunicado dirigido al
   equipo A, **entonces** lo reciben solo las familias de los jugadores que están en ese equipo.
6. **Dado** un comunicado con el título o el texto vacío, **cuando** se intenta publicar,
   **entonces** el sistema lo rechaza y explica qué falta.
7. **Dado** un comunicado dirigido a una categoría sin jugadores activos, **cuando** se intenta
   publicar, **entonces** el sistema lo rechaza porque no habría a quién llegarle.

---

### Historia de usuario 2 - La familia ve la campana con el contador (Prioridad: P1)

En todas las pantallas de la cuenta del jugador aparece una campana en la barra superior. Si hay
comunicados sin leer, muestra un contador con cuántos son. Al tocarla, la familia ve la lista de
comunicados que le llegaron, del más reciente al más antiguo, y puede abrir cada uno.

**Por qué esta prioridad**: es la mitad de lo pedido y la única manera en que la familia se entera
de lo que publica el club.

**Prueba independiente**: se prueba publicando un comunicado y entrando con la cuenta de un
jugador destinatario y con la de uno que no lo es.

**Escenarios de aceptación**:

1. **Dado** una familia con dos comunicados sin leer, **cuando** entra a cualquier pantalla,
   **entonces** la campana muestra un contador con 2.
2. **Dado** una familia sin comunicados sin leer, **cuando** entra a cualquier pantalla,
   **entonces** la campana se ve sin contador.
3. **Dado** una familia que toca la campana, **cuando** se abre la lista, **entonces** ve cada
   comunicado con su título, quién lo publicó, cuándo y si está leído o sin leer, del más reciente
   al más antiguo.
4. **Dado** un comunicado abierto, **cuando** la familia lo lee, **entonces** ve el título, el
   texto completo, el nombre de quien lo publicó (que abre su ficha básica, spec 008) y la fecha.
5. **Dado** una familia que no era destinataria de un comunicado, **cuando** abre la campana,
   **entonces** no lo ve, ni siquiera sabiendo su identificador.
6. **Dado** un teléfono con la aplicación cerrada, **cuando** se publica un comunicado,
   **entonces** no llega ningún aviso al teléfono ni cambia el número sobre el icono: la familia
   lo ve al abrir la aplicación.

---

### Historia de usuario 3 - Leído y no leído (Prioridad: P2)

La familia distingue lo que ya leyó de lo que no. Abrir un comunicado lo marca como leído y baja
el contador. También puede marcar todos como leídos de una vez.

**Por qué esta prioridad**: sin esto el contador no sirve: nunca bajaría y la familia dejaría de
mirarlo.

**Prueba independiente**: se prueba con una cuenta con varios comunicados sin leer: abrir uno y
revisar el contador, y luego marcar todos.

**Escenarios de aceptación**:

1. **Dado** una familia con tres comunicados sin leer, **cuando** abre uno, **entonces** ese queda
   leído y el contador baja a 2.
2. **Dado** una familia con comunicados sin leer, **cuando** usa "Marcar todos como leídos",
   **entonces** todos quedan leídos y el contador desaparece.
3. **Dado** dos familias destinatarias del mismo comunicado, **cuando** una lo lee, **entonces**
   para la otra sigue sin leer.
4. **Dado** un comunicado leído, **cuando** la familia entra otro día o desde otro aparato,
   **entonces** sigue figurando como leído.

---

### Historia de usuario 4 - Ver y retirar lo publicado (Prioridad: P3)

Quien publica puede ver la lista de lo que publicó y retirar un comunicado que ya no corresponde
(por ejemplo, un error). El PRESIDENTE y los DIRECTIVOS ven todos los comunicados del club; un
entrenador ve los suyos. El PRESIDENTE puede retirar cualquier comunicado; los demás, los suyos.

**Por qué esta prioridad**: es el cuidado de lo publicado. El club funciona sin esto, pero un
error de publicación no tendría arreglo.

**Prueba independiente**: se prueba publicando un comunicado, retirándolo y comprobando que las
familias dejan de verlo y el contador baja.

**Escenarios de aceptación**:

1. **Dado** un entrenador con comunicados publicados, **cuando** abre "Comunicados publicados",
   **entonces** ve los suyos y no los de otros.
2. **Dado** el presidente, **cuando** abre "Comunicados publicados", **entonces** ve los de todo
   el club, con quién los publicó y a quién iban dirigidos.
3. **Dado** un comunicado retirado, **cuando** una familia abre la campana, **entonces** ya no lo
   ve y, si estaba sin leer, el contador baja.
4. **Dado** un entrenador, **cuando** intenta retirar un comunicado de otra persona, **entonces**
   el sistema se lo niega.
5. **Dado** un comunicado retirado, **cuando** el presidente consulta lo publicado, **entonces**
   sigue apareciendo marcado como retirado, con quién y cuándo lo retiró.

---

### Casos límite

- Una familia con dos hermanos en categorías distintas recibe un comunicado dirigido a las dos:
  lo ve una sola vez y el contador cuenta uno.
- Una familia con un hermano en espera de aprobación (§12.1.1) no recibe nada por ese hermano:
  solo por sus jugadores activos.
- Un jugador que cambia de categoría después de publicado un comunicado conserva lo que ya le
  había llegado y no recibe lo anterior de su categoría nueva.
- Un jugador que entra al club (o a una categoría) después de publicado un comunicado no lo
  recibe: solo recibe lo publicado desde que entró.
- Un jugador retirado deja de acceder al club y por tanto de ver comunicados; al reincorporarlo
  vuelve a ver lo que ya le había llegado.
- Un entrenador que pierde la asignación a una categoría conserva lo que publicó y puede retirarlo,
  pero ya no publica dirigido a esa categoría.
- Un comunicado se muestra como texto: lo escrito con símbolos de marcado, enlaces o instrucciones
  se ve tal cual y nunca se ejecuta.
- Un título o un texto muy largo debe caber en la pantalla del teléfono sin romper la campana ni
  la lista.
- Con más de 99 comunicados sin leer, el contador muestra "99+".
- Quien publica toca dos veces el botón de publicar: se publica un solo comunicado.
- Una persona que pertenece a dos clubes ve la campana y el contador del club elegido, y al
  cambiar de club se recalculan.
- Con el club suspendido (§7.4) solo entra el PRESIDENTE; las familias verán los comunicados
  cuando el club vuelva a la normalidad.
- Comunicados en otro club: nadie los obtiene, ni por el identificador ni por el contador.

## Requisitos *(obligatorio)*

### Requisitos funcionales

**Publicar**

- **RF-001**: El PRESIDENTE, los DIRECTIVOS y los ENTRENADORES DEBEN poder publicar un
  comunicado. Ningún otro rol DEBE poder hacerlo, tampoco el DESARROLLADOR ni las cuentas de
  jugador.
- **RF-002**: Un comunicado DEBE tener un título y un texto, ambos obligatorios, de texto plano. El
  título admite hasta 100 caracteres y el texto hasta 1.000.
- **RF-003**: Un comunicado DEBE dirigirse a una de estas audiencias: todo el club, una o varias
  categorías, o uno o varios equipos de una categoría.
- **RF-004**: El PRESIDENTE y los DIRECTIVOS DEBEN poder dirigir un comunicado a cualquier
  audiencia del club.
- **RF-005**: Un ENTRENADOR DEBE poder dirigir un comunicado únicamente a categorías (o equipos
  de ellas) que tiene asignadas. El sistema NO DEBE ofrecerle otras ni aceptarlas si las pide
  directamente (§7.5, §15).
- **RF-006**: Un PRESIDENTE o DIRECTIVO asignado como entrenador DEBE publicar con el alcance de
  su rol (RF-004), no con el de entrenador.
- **RF-007**: Un comunicado DEBE llegar únicamente a las cuentas con al menos un jugador activo
  dentro de la audiencia. No DEBE llegar a jugadores en espera, retirados ni sin categoría
  cuando la audiencia es una categoría o un equipo; para "todo el club", DEBE llegar también a
  los jugadores activos sin categoría.
- **RF-008**: El sistema NO DEBE publicar un comunicado que no le llegue a nadie, y DEBE explicar
  el motivo.
- **RF-009**: Al publicar, el sistema DEBE fijar quién lo recibe en ese momento. Quien entra a
  una audiencia después NO DEBE recibir lo publicado antes, y quien sale de ella conserva lo que
  ya recibió.
- **RF-010**: Publicar dos veces seguidas por un doble toque NO DEBE crear dos comunicados.
- **RF-011**: Un comunicado publicado NO DEBE poder editarse. Si hay un error, se retira y se
  publica de nuevo.

**Campana y lista**

- **RF-012**: Las pantallas de la cuenta de un jugador DEBEN mostrar una campana en la barra
  superior, con un contador de comunicados sin leer cuando hay alguno, y sin contador cuando no
  hay ninguno.
- **RF-013**: El contador DEBE contar cada comunicado una sola vez por cuenta aunque le llegue por
  más de un jugador, y DEBE mostrar "99+" desde 100.
- **RF-014**: Al tocar la campana, la cuenta DEBE ver la lista de sus comunicados, del más
  reciente al más antiguo, con título, quién lo publicó, fecha y estado leído o sin leer.
- **RF-015**: El estado leído o sin leer DEBE distinguirse por texto además de por color (§24).
- **RF-016**: Al abrir un comunicado se DEBE mostrar título, texto completo, fecha y el nombre de
  quien lo publicó, que abre su ficha básica (spec 008).
- **RF-017**: El contador DEBE ponerse al día cuando la familia abre la aplicación, vuelve a ella
  o cambia de pantalla, y periódicamente mientras la aplicación está abierta, sin que tenga que
  recargar la página.
- **RF-018**: Los comunicados DEBEN mantenerse en la lista mientras no se retiren; no caducan por
  tiempo.
- **RF-019**: Los ENTRENADORES, DIRECTIVOS y PRESIDENTES NO DEBEN tener campana en esta versión;
  la campana es de la cuenta de jugador (ver RF-024 para lo que ve el equipo del club).

**Leído y no leído**

- **RF-020**: Abrir un comunicado DEBE marcarlo como leído para esa cuenta, y el contador DEBE
  bajar.
- **RF-021**: La cuenta DEBE poder marcar todos sus comunicados como leídos de una vez.
- **RF-022**: Lo leído DEBE ser por cuenta, no compartido: que una familia lo lea NO DEBE cambiar
  el estado para otra.
- **RF-023**: Lo leído DEBE conservarse entre sesiones y entre aparatos.

**Lo publicado y su retiro**

- **RF-024**: Quien publica DEBE poder ver la lista de lo que publicó, con la audiencia de cada
  comunicado. El PRESIDENTE y los DIRECTIVOS DEBEN ver además los de todo el club, con quién
  los publicó. Un ENTRENADOR NO DEBE ver los de otras personas.
- **RF-025**: Quien publicó un comunicado y el PRESIDENTE DEBEN poder retirarlo. Nadie más. Un
  DIRECTIVO retira los suyos, no los de otros.
- **RF-026**: Un comunicado retirado DEBE desaparecer de la campana y del contador de todas las
  familias, y NO DEBE eliminarse físicamente: DEBE seguir en la lista de lo publicado, marcado
  como retirado, con quién lo retiró y cuándo (§14).
- **RF-027**: Un comunicado retirado NO DEBE poder volver a publicarse; se escribe de nuevo.

**Alcance acotado**

- **RF-028**: En esta versión NO DEBE enviarse ningún aviso fuera de la aplicación (ni al teléfono
  con la aplicación cerrada, ni número sobre el icono, ni correo) por comunicados.
- **RF-029**: Esta versión NO DEBE crear comunicados automáticos a partir de entrenamientos,
  partidos, pagos u otros eventos: todo comunicado lo escribe una persona del equipo del club.

**Aislamiento e interfaz**

- **RF-030**: Todo lo de esta funcionalidad DEBE quedar limitado al club elegido; nadie de otro
  club ni el DESARROLLADOR DEBE ver comunicados ni sus contadores (§7.1).
- **RF-031**: El backend DEBE aplicar todas las reglas de quién publica, a quién llega y quién
  retira; ocultar opciones en la interfaz NO DEBE ser la única barrera (§15).
- **RF-032**: Una cuenta de jugador DEBE ver solo los comunicados dirigidos a sus jugadores. Que
  conozca el identificador de otro comunicado NO DEBE darle acceso (§7.5).
- **RF-033**: Toda pantalla de esta funcionalidad, incluida la campana, DEBE funcionar en teléfono
  y en escritorio, en tema claro y oscuro, con la identidad del club (§24), y NO DEBE saturar la
  barra superior en el teléfono.
- **RF-034**: La campana DEBE tener un nombre accesible que diga cuántos comunicados hay sin leer.
- **RF-035**: Los nombres de personas en estas pantallas DEBEN seguir la regla de la spec 008.
- **RF-036**: El texto de un comunicado DEBE mostrarse siempre como texto, sin interpretar
  formato, enlaces ni instrucciones.

### Entidades clave

- **Comunicado**: título, texto, quién lo publicó, cuándo, audiencia, y si está retirado (con
  quién y cuándo). No se edita ni se elimina físicamente.
- **Audiencia**: a quién va dirigido un comunicado: todo el club, categorías o equipos de una
  categoría. Se resuelve al publicar.
- **Recepción**: lo que une un comunicado con una cuenta de jugador que lo recibe, y si esa
  cuenta lo leyó y cuándo. Una cuenta recibe un comunicado una sola vez.
- **Campana**: vista de los comunicados que le llegan a una cuenta, con el contador de los
  sin leer.

## Criterios de éxito *(obligatorio)*

### Resultados medibles

- **CE-001**: En el 100 % de los casos probados, un comunicado llega exactamente a las cuentas
  con un jugador activo en su audiencia, y a ninguna otra.
- **CE-002**: En el 100 % de los intentos probados, un entrenador no publica hacia categorías
  que no tiene asignadas.
- **CE-003**: En el 100 % de los casos probados, el contador de la campana coincide con la
  cantidad de comunicados sin leer de la cuenta, con cada comunicado contado una sola vez.
- **CE-004**: En el 100 % de los casos probados, un comunicado retirado desaparece de la campana
  y del contador de todas las cuentas y sigue en la lista de lo publicado.
- **CE-005**: En el 100 % de los intentos probados, una cuenta no obtiene comunicados de otro
  club ni dirigidos a otros jugadores.
- **CE-006**: En el 100 % de los casos probados, publicar un comunicado no genera ningún aviso
  fuera de la aplicación.
- **CE-007**: Las pantallas de esta funcionalidad se leen sin desplazamiento horizontal en
  teléfono y en escritorio, en tema claro y oscuro, con los colores de cualquier club probado.

## Supuestos

- La campana es de las cuentas de jugador. El equipo del club no recibe comunicados de otros: ve
  lo publicado según RF-024.
- Un comunicado no lleva adjuntos, imágenes ni formato. Solo título y texto.
- Los límites de 100 y 1.000 caracteres son un punto de partida razonable; pueden ajustarse en
  el plan si hay motivo.
- Cuando una cuenta tiene varios jugadores en un mismo club, la campana muestra los comunicados de
  todos ellos juntos, aunque la ficha solo muestre un jugador a la vez: un comunicado del club no
  es información de la ficha.
- El estado leído es por cuenta y por comunicado, no por jugador.
- El retiro de un comunicado no avisa a nadie: simplemente deja de aparecer.
- El PRESIDENTE y los DIRECTIVOS ven todos los comunicados del club porque pueden dirigirse a
  cualquier audiencia; un ENTRENADOR ve solo los suyos.
- No se envían correos por comunicados; la integración de correo de la plataforma solo se usa
  para invitaciones y cuenta (§6.3).
- La aplicación está en construcción: no hay datos reales que migrar.

## Fuera de alcance

- Avisos al teléfono con la aplicación cerrada y el número sobre el icono de la aplicación: los
  recogerá una especificación posterior.
- Avisos por correo, mensaje de texto o mensajería.
- Comunicados o notificaciones automáticas por entrenamientos, partidos, convocatorias, pagos u
  otros eventos.
- Los "mensajes de convocatoria" de §1, dirigidos a los convocados de un partido.
- Responder a un comunicado, comentarios o reacciones de las familias.
- Ver quién leyó cada comunicado.
- Editar un comunicado ya publicado.
- Adjuntos, imágenes, enlaces con formato y programación de publicaciones futuras.
- Que el equipo del club tenga campana propia.
- Comunicados dirigidos a una familia concreta o a una lista armada a mano.
