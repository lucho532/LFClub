# Especificación de funcionalidad: Avisos de entrenamientos y partidos en la campana

**Rama de la funcionalidad**: `012-avisos-de-actividades`

**Creada**: 2026-10-12

**Estado**: Borrador

**Entrada**: Descripción del propietario del proyecto: "Las notificaciones también deben darse
cuando el entrenador, el presidente o el directivo publica un partido, un entrenamiento, etc., y no
solo mensajes personalizados." Eventos: también los cambios de fecha, hora y lugar, la convocatoria,
el resultado y la asistencia. A las familias de la categoría; en partidos, solo a las familias de los
convocados. Una línea corta que, al tocarla, abre el detalle, mezclada con los comunicados. El aviso
con la aplicación cerrada sigue fuera de alcance.

**Plataforma**: La Pecosa

**Constitución aplicable**: versión 4.9.0, en especial §1, §7.1, §7.5, §8 (Presidente, Directivo,
Entrenador y Jugador), §11.1, §14, §14.4, §15, §17.2, §20, §24, §25 y §28.

**Depende de**: spec 009 (entrenamientos, partidos, convocatorias, resultados y asistencia), spec 010
(resultado publicado por la familia y su corrección) y spec 011 (comunicados y campana).

**Cambia**: la spec 011 dejó la campana solo para comunicados escritos por una persona (RF-029) y la
§14.4 de la constitución dice que "no se crean comunicados automáticos por otros eventos". Esta
especificación añade a la campana los avisos automáticos de entrenamientos y partidos. No cambia
ningún comunicado ya publicado ni cómo se publican, se leen o se retiran.

## Impacto en la constitución

Esta funcionalidad necesita una enmienda (4.9.0 → 4.10.0) antes del plan, porque hoy la constitución
dice lo contrario:

- §14.4: la campana deja de ser "solo de comunicados". Pasa a reunir comunicados y avisos de
  actividades, con un solo contador. Se elimina la frase "ni se crean comunicados automáticos por
  otros eventos" y se sustituye por: un entrenamiento o un partido genera avisos automáticos a las
  familias que ya podían verlo, dentro de la aplicación, y nada fuera de ella.
- §8 (Jugador): consultar también los avisos de actividades que le llegan.
- §14: un aviso de actividad no se elimina físicamente por una acción de usuario.
- §17.2: los avisos de partidos respetan la visibilidad de la spec 009: un partido próximo solo lo
  ve, y solo lo avisa a, quien está convocado.
- §20: pruebas nuevas de a quién llega cada aviso, de que no revela lo que la familia no puede ver y
  de que no sale nada fuera de la aplicación.
- §24: la campana distingue en texto un comunicado de un aviso de actividad.
- §28: sin decisiones nuevas. El aviso con la aplicación cerrada sigue pendiente.

**Pendiente**: aplicar la enmienda 4.9.0 → 4.10.0 antes de `/speckit-plan`.

## Aclaraciones

### Sesión 2026-10-12

- P: ¿Qué hechos avisan? → R: Entrenamiento o partido nuevo, cambios de fecha, hora y lugar,
  cancelación y reactivación, convocatoria, resultado y asistencia.
- P: ¿A quién llega? → R: A las familias de la categoría del entrenamiento o del partido. En
  partidos, solo a las familias de los convocados.
- P: ¿Cómo se ve? → R: Una línea corta, tipo "Nuevo entrenamiento el sábado a las 9:00", que al
  tocarla abre el detalle.
- P: ¿Dónde? → R: Mezclada con los comunicados, en la misma lista y con el mismo contador.
- P: ¿Y con la aplicación cerrada? → R: Sigue fuera de alcance.

## Escenarios de usuario y pruebas *(obligatorio)*

En esta especificación, "la familia" es la cuenta de un jugador (rol JUGADOR). "El equipo del club"
son el PRESIDENTE, los DIRECTIVOS y los ENTRENADORES. Un "aviso" es una línea que el sistema
escribe solo, sin que nadie la redacte, cuando el equipo del club hace algo con un entrenamiento o
un partido. Un "comunicado" sigue siendo lo que una persona escribe (spec 011).

### Historia de usuario 1 - La familia se entera de lo que pasa con los entrenamientos (Prioridad: P1)

Cuando el equipo del club programa un entrenamiento, cambia su fecha, hora o lugar, lo cancela, lo
reactiva o registra su asistencia, las familias de las categorías del entrenamiento reciben un aviso
en la campana, sin que nadie tenga que escribirlo.

**Por qué esta prioridad**: es lo que más cambia en la vida del club y hoy la familia solo lo sabe
si abre el calendario. Es el núcleo de lo pedido.

**Prueba independiente**: se prueba programando, cambiando, cancelando, reactivando y registrando
asistencia de un entrenamiento, y mirando la campana de las cuentas de la categoría y de otra que no
lo es.

**Escenarios de aceptación**:

1. **Dado** un entrenador de la categoría 2015, **cuando** programa un entrenamiento para el sábado
   a las 9:00, **entonces** cada familia con un jugador activo de la 2015 recibe el aviso "Nuevo
   entrenamiento el sábado a las 9:00" y ninguna otra.
2. **Dado** un entrenamiento programado, **cuando** el equipo del club cambia la hora, la fecha o el
   lugar, **entonces** las familias de sus categorías reciben un aviso que dice qué cambió y cómo
   queda.
3. **Dado** un entrenamiento programado, **cuando** se cancela, **entonces** las familias reciben
   "Entrenamiento del sábado cancelado"; **cuando** se reactiva, reciben un aviso de que sigue en
   pie.
4. **Dado** un entrenamiento pasado, **cuando** quien entrena registra la asistencia, **entonces**
   las familias de sus categorías reciben un aviso de que se registró, sin decir quién asistió ni
   quién faltó.
5. **Dado** un entrenamiento de dos categorías, **cuando** se programa, **entonces** una familia con
   hermanos en las dos recibe un solo aviso.
6. **Dado** una familia que toca el aviso, **cuando** se abre, **entonces** llega al detalle de ese
   entrenamiento.

---

### Historia de usuario 2 - La familia se entera de lo que pasa con los partidos (Prioridad: P1)

Cuando el equipo del club convoca a un jugador, cambia la fecha, la hora o el lugar de un partido,
lo cancela, lo reactiva o registra su resultado, las familias que corresponden reciben un aviso. Un
partido próximo solo se avisa a las familias de los convocados, igual que solo ellas pueden verlo
(spec 009). Sacar a un jugador de la convocatoria no genera ningún aviso.

**Por qué esta prioridad**: tiene el mismo peso que la historia 1 y las dos son lo que el
propietario pidió. La convocatoria es el aviso más valioso: es lo que hoy una familia no sabe.

**Prueba independiente**: se prueba convocando a algunos jugadores de una categoría, cambiando y
cancelando el partido y publicando el resultado, y comprobando quién recibe cada aviso.

**Escenarios de aceptación**:

1. **Dado** un partido próximo, **cuando** el equipo del club convoca a un jugador, **entonces** la
   familia de ese jugador recibe un aviso que dice cuál jugador fue convocado y a qué partido, y
   ninguna otra familia lo recibe.
2. **Dado** un jugador convocado, **cuando** lo sacan de la convocatoria, **entonces** nadie
   recibe ningún aviso por eso: ni su familia ni las demás.
3. **Dado** un partido próximo con varios convocados, **cuando** cambia la hora o el lugar, o se
   cancela o se reactiva, **entonces** reciben el aviso solo las familias de los convocados.
4. **Dado** un partido próximo, **cuando** se crea sin convocados, **entonces** no se avisa a nadie:
   las familias se enteran al convocarlas.
5. **Dado** un partido que ya se jugó, **cuando** el equipo del club registra o corrige el
   resultado, **entonces** las familias de la categoría reciben el aviso con el marcador.
6. **Dado** un partido próximo, **cuando** una familia no convocada abre la campana, **entonces**
   no ve ningún aviso de ese partido.

---

### Historia de usuario 3 - La campana reúne comunicados y avisos (Prioridad: P1)

En la campana de la familia, los avisos de actividades aparecen en la misma lista y suman al mismo
contador que los comunicados, del más reciente al más antiguo. Cada fila dice en texto si es un
comunicado o un aviso de actividad, y si está leída o sin leer. Tocar un aviso lo marca como leído y
abre el detalle de la actividad.

**Por qué esta prioridad**: sin esto los avisos no tienen dónde verse. Reutiliza lo que la familia
ya conoce de la spec 011.

**Prueba independiente**: se prueba con una cuenta con comunicados y avisos sin leer: contador, lista
mezclada, abrir un aviso y "Marcar todos como leídos".

**Escenarios de aceptación**:

1. **Dado** una familia con un comunicado y dos avisos sin leer, **cuando** entra a cualquier
   pantalla, **entonces** la campana muestra 3.
2. **Dado** la lista, **cuando** se abre, **entonces** comunicados y avisos van juntos por fecha y
   cada fila dice en texto "Comunicado" o "Aviso" y "Sin leer" o "Leído".
3. **Dado** un aviso sin leer, **cuando** la familia lo toca, **entonces** queda leído, el contador
   baja y se abre el detalle del entrenamiento o del partido.
4. **Dado** una familia con comunicados y avisos sin leer, **cuando** usa "Marcar todos como
   leídos", **entonces** todos quedan leídos y el contador desaparece.
5. **Dado** dos familias destinatarias del mismo aviso, **cuando** una lo lee, **entonces** para la
   otra sigue sin leer.
6. **Dado** un aviso cuya actividad la familia ya no puede ver, **cuando** lo toca, **entonces**
   queda leído y ve un mensaje claro de que esa actividad ya no está disponible para ella.

---

### Historia de usuario 4 - Sin ruido ni avisos de más (Prioridad: P2)

El equipo del club hace varios cambios seguidos y la familia no debe recibir una avalancha: lo que
sigue sin leer sobre una misma actividad se reemplaza por lo más reciente. Y los avisos nunca dicen
más de lo que la familia puede ver.

**Por qué esta prioridad**: cuida que la campana siga siendo útil, pero el club funciona sin esto.

**Prueba independiente**: se prueba cambiando tres veces la hora de un entrenamiento sin que la
familia lo lea, y comprobando qué ve; y revisando el texto de los avisos.

**Escenarios de aceptación**:

1. **Dado** un entrenamiento con un aviso sin leer sobre su hora, **cuando** se cambia otra vez la
   hora, **entonces** esa familia sigue con un solo aviso sin leer sobre ese entrenamiento, con lo
   más reciente, y el contador no sube.
2. **Dado** un aviso ya leído, **cuando** hay un cambio nuevo en la misma actividad, **entonces** la
   familia recibe otro aviso sin leer.
3. **Dado** un aviso de asistencia, **cuando** una familia lo ve, **entonces** no hay en él ni en su
   detalle nada sobre la asistencia de otros jugadores (spec 009, RF-024).
4. **Dado** el equipo del club que hace el cambio, **cuando** lo guarda, **entonces** no recibe
   ningún aviso de su propia acción ni tiene campana.
5. **Dado** un teléfono con la aplicación cerrada, **cuando** el equipo del club cambia algo,
   **entonces** no llega ningún aviso al teléfono ni cambia el número sobre el icono: la familia lo
   ve al abrir la aplicación.

---

### Casos límite

- Una familia con dos hermanos de categorías distintas, ambos en el mismo entrenamiento, recibe un
  solo aviso; si solo uno está convocado a un partido, el aviso nombra a ese jugador.
- Una familia con un hermano en espera de aprobación (§12.1.1) no recibe nada por ese hermano.
- Un jugador que cambia de categoría conserva los avisos que ya le llegaron y no recibe los
  anteriores de la categoría nueva.
- Un jugador que entra a la categoría después no recibe avisos anteriores a su entrada.
- Un jugador retirado deja de acceder al club; al reincorporarlo vuelve a ver lo que ya le había
  llegado.
- Un entrenamiento o un partido cancelado sigue existiendo (§14): su aviso abre el detalle, marcado
  como cancelado.
- Un jugador sacado de la convocatoria ya no ve el partido próximo y no recibe ningún aviso por
  ello. Si tenía sin leer el aviso de que lo convocaron, sigue ahí y, al tocarlo, dice que la
  actividad ya no está disponible (RF-021).
- Un jugador que se retira del club queda fuera de las convocatorias de partidos próximos (spec 009,
  RF-033): esa salida no genera aviso a su familia, que ya no entra al club.
- Una convocatoria que se guarda con varios cambios a la vez genera un aviso por cada familia
  afectada, y una sola vez.
- Registrar o corregir la asistencia o el resultado sin que nada cambie no genera aviso nuevo.
- Con más de 99 sin leer, el contador sigue mostrando "99+", sumando comunicados y avisos.
- Una persona que pertenece a dos clubes ve los avisos y el contador del club elegido.
- Con el club suspendido (§7.4) solo entra el PRESIDENTE: las familias ven los avisos cuando el club
  vuelva a la normalidad.
- El resultado publicado por una familia (spec 010) avisa a las demás familias de la categoría, no a
  la que lo publicó.
- Avisos de otro club: nadie los obtiene, ni por el identificador ni por el contador.

## Requisitos *(obligatorio)*

### Requisitos funcionales

**Qué genera un aviso**

- **RF-001**: El sistema DEBE generar un aviso automático, sin intervención de nadie, cuando un
  entrenamiento se programa, cambia de fecha, hora o lugar, se cancela, se reactiva o recibe su
  asistencia.
- **RF-002**: El sistema DEBE generar un aviso automático cuando un partido cambia de fecha, hora o
  lugar, se cancela, se reactiva, o recibe o corrige su resultado, y cuando un jugador queda
  convocado. Salir de la convocatoria NO DEBE generar aviso.
- **RF-003**: Un partido nuevo DEBE avisar a las familias de sus convocados; si se crea sin
  convocados, NO DEBE avisar a nadie, y cada convocatoria posterior DEBE avisar a la familia del
  jugador convocado.
- **RF-004**: Un cambio que no modifica ningún dato (guardar lo mismo que ya había) NO DEBE generar
  aviso.
- **RF-005**: Los avisos DEBEN generarse por lo que hace el equipo del club, y también por el
  resultado que publica una familia (spec 010). El aviso NO DEBE ir a quien hizo la acción.

**A quién llega**

- **RF-006**: Un aviso de un entrenamiento DEBE llegar a las cuentas con al menos un jugador activo
  en alguna de sus categorías, una sola vez por cuenta.
- **RF-007**: Un aviso de un partido próximo, incluidos sus cambios, su cancelación y su
  reactivación, DEBE llegar solo a las cuentas con un jugador convocado.
- **RF-008**: Un aviso de convocatoria DEBE llegar solo a la cuenta del jugador convocado.
- **RF-009**: Un aviso del resultado de un partido jugado DEBE llegar a las cuentas con un jugador
  activo en alguna de sus categorías.
- **RF-010**: Un aviso NO DEBE llegar a jugadores en espera ni retirados.
- **RF-011**: Al generarse, el sistema DEBE fijar quién lo recibe. Quien entra a la categoría o a
  la convocatoria después NO DEBE recibir avisos anteriores; quien sale conserva los que ya recibió.

**Qué dice**

- **RF-012**: Cada aviso DEBE ser una línea corta que diga qué ocurrió y la fecha y hora de la
  actividad, por ejemplo "Nuevo entrenamiento el sábado a las 9:00".
- **RF-013**: El aviso de un cambio DEBE decir qué cambió (fecha, hora o lugar) y cómo queda.
- **RF-014**: El aviso de un resultado DEBE llevar el marcador tal como lo ve la familia en el
  partido.
- **RF-015**: Un aviso de convocatoria DEBE nombrar al jugador al que se refiere, para distinguir
  a los hermanos.
- **RF-016**: El aviso de asistencia DEBE decir solo que se registró; NO DEBE llevar quién asistió
  ni quién faltó (spec 009, RF-024).
- **RF-017**: Un aviso NO DEBE revelar nada que la familia no pueda ver de esa actividad (§17.2).

**Campana y lista**

- **RF-018**: Los avisos DEBEN aparecer en la misma lista de la campana que los comunicados, del más
  reciente al más antiguo, y sumar al mismo contador, contando cada elemento una sola vez por
  cuenta.
- **RF-019**: Cada fila DEBE distinguir en texto un comunicado de un aviso de actividad, y lo leído
  de lo sin leer, además del color (§24).
- **RF-020**: Tocar un aviso DEBE marcarlo como leído para esa cuenta, bajar el contador y abrir el
  detalle de la actividad a la que se refiere.
- **RF-021**: Si la familia ya no puede ver la actividad (por ejemplo, salió de la convocatoria), el
  aviso DEBE poder leerse y DEBE explicar que la actividad ya no está disponible, sin abrirla.
- **RF-022**: "Marcar todos como leídos" DEBE marcar también los avisos.
- **RF-023**: Lo leído DEBE ser por cuenta y conservarse entre sesiones y aparatos, igual que en los
  comunicados.
- **RF-024**: Los avisos DEBEN mantenerse en la lista mientras la familia pueda ver la actividad a
  la que se refieren; no caducan por tiempo.

**Sin ruido**

- **RF-025**: Si una cuenta tiene sin leer un aviso sobre una actividad y se genera otro sobre esa
  misma actividad, el sistema DEBE sustituir el anterior por el nuevo, de modo que quede un solo
  aviso sin leer por actividad y el contador no suba. Un aviso ya leído NO DEBE modificarse: el nuevo
  se suma como otro aviso.
- **RF-026**: Un aviso nunca DEBE generarse dos veces por la misma acción, ni aunque la operación se
  reintente.

**Alcance acotado**

- **RF-027**: NO DEBE enviarse ningún aviso fuera de la aplicación (ni al teléfono con la aplicación
  cerrada, ni número sobre el icono, ni correo) por estos avisos.
- **RF-028**: La campana sigue siendo solo de la cuenta de jugador: el PRESIDENTE, los DIRECTIVOS y
  los ENTRENADORES NO DEBEN tener campana ni recibir estos avisos.
- **RF-029**: Los avisos NO DEBEN generarse por otros hechos (ingresos, retiros de jugadores,
  respuestas de las familias, cambios de categoría ni de ficha).

**Aislamiento e interfaz**

- **RF-030**: Todo lo de esta funcionalidad DEBE quedar limitado al club elegido; nadie de otro club
  ni el DESARROLLADOR DEBE ver avisos ni sus contadores (§7.1).
- **RF-031**: El backend DEBE aplicar todas las reglas de a quién llega cada aviso y qué dice;
  ocultar opciones en la interfaz NO DEBE ser la única barrera (§15).
- **RF-032**: Una cuenta de jugador DEBE ver solo los avisos dirigidos a sus jugadores. Que conozca
  el identificador de un aviso o de una actividad NO DEBE darle acceso (§7.5).
- **RF-033**: Todo lo nuevo DEBE funcionar en teléfono y en escritorio, en tema claro y oscuro, con
  la identidad del club (§24), sin desplazamiento horizontal.
- **RF-034**: El nombre accesible de la campana DEBE seguir diciendo cuántos elementos hay sin leer,
  contando comunicados y avisos.
- **RF-035**: Lo ya construido (publicar, leer y retirar comunicados) DEBE seguir funcionando igual.

### Entidades clave

- **Aviso de actividad**: línea automática que cuenta un hecho de un entrenamiento o de un partido:
  qué ocurrió, a qué actividad se refiere, cuándo ocurrió y a qué jugador alude, si es el caso. Se
  resuelve a quién llega en el momento en que se genera.
- **Recepción**: lo que une un aviso con una cuenta de jugador que lo recibe, y si esa cuenta lo
  leyó y cuándo. Una cuenta recibe un aviso una sola vez.
- **Campana**: vista única de comunicados y avisos que le llegan a una cuenta, con el contador de los
  sin leer.

## Criterios de éxito *(obligatorio)*

### Resultados medibles

- **CE-001**: En el 100 % de los casos probados, cada hecho de un entrenamiento o un partido genera
  su aviso exactamente a las cuentas que corresponden y a ninguna otra.
- **CE-002**: En el 100 % de los casos probados, un partido próximo avisa solo a las familias de sus
  convocados y ninguna otra familia ve ese aviso, ni por el identificador.
- **CE-003**: En el 100 % de los casos probados, el contador de la campana coincide con la suma de
  comunicados y avisos sin leer de la cuenta, cada uno contado una sola vez.
- **CE-004**: En el 100 % de los casos probados, ningún aviso contiene datos de otros jugadores ni
  asistencia individual.
- **CE-005**: En el 100 % de los casos probados, varios cambios seguidos sobre una actividad dejan
  una sola entrada sin leer por actividad en cada familia.
- **CE-006**: En el 100 % de los casos probados, ningún aviso genera nada fuera de la aplicación.
- **CE-007**: En el 100 % de los casos probados, lo que hacía la spec 011 con los comunicados sigue
  igual.
- **CE-008**: Las pantallas se leen sin desplazamiento horizontal en teléfono y en escritorio, en
  tema claro y oscuro, con los colores de cualquier club probado.

## Supuestos

- Los avisos usan la misma campana, el mismo contador y la misma lista que los comunicados; la
  familia no aprende nada nuevo.
- El aviso de asistencia es general ("Se registró la asistencia del entrenamiento del sábado") y no
  dice cómo le fue a cada jugador; la asistencia individual sigue viéndose donde la spec 009 la
  muestra. Es lo que menos expone; puede ajustarse en `/speckit-clarify`.
- "Una sola actividad sin leer por cuenta" es una regla para evitar ruido; el aviso sustituido
  desaparece y no queda historial de él.
- Las acciones que no cambian nada no avisan. Un cambio de categorías de una actividad ya
  programada no existe (spec 009).
- El resultado que publica una familia (spec 010) avisa a las demás familias de la categoría, como
  lo haría el del equipo del club; quien lo publicó no lo recibe.
- Un partido jugado lo ve todo el club (spec 009, RF-019), pero su resultado solo se avisa a las
  familias de su categoría, como se pidió; el resto del club lo ve en las listas sin recibir aviso.
- Los textos son en español, cortos y sin formato; se muestran siempre como texto.
- Los avisos no se editan ni se retiran a mano. Si una actividad deja de ser visible para la familia
  (RF-021), el aviso conserva su texto pero no abre nada.
- La aplicación está en construcción: no hay datos reales que migrar.

## Ajuste añadido en el plan: identidad de la ficha (bloque B)

Pedido del propietario el 2026-10-12, ajeno a los avisos. En la tarjeta "Identidad" de la ficha del
jugador hay dos botones, "Cambiar documento" y "Corregir nombre y fecha", que el propietario
considera redundantes. Solo cambia la interfaz: el documento sigue siendo editable con las reglas de
siempre.

- **RF-036**: la tarjeta "Identidad" DEBE tener un único control de edición, un lápiz con nombre
  accesible "Editar identidad", que abre un solo diálogo con nombres, apellidos, fecha de
  nacimiento, tipo de documento y número de documento. Los botones "Cambiar documento" y "Corregir
  nombre y fecha" DEBEN desaparecer.
- **RF-037**: cada campo DEBE poder editarlo quien hoy puede editarlo; el lápiz DEBE mostrarse a
  quien puede editar alguno, y los demás ven la tarjeta sin control de edición. Las reglas del
  documento (§10) no cambian.
- **RF-038**: la categoría y los equipos siguen siendo de solo vista en la tarjeta y se cambian como
  hasta ahora (§11); el lápiz no los toca.

**Impacto en la constitución**: ninguno. No cambia ningún permiso ni regla; es solo de interfaz.

## Fuera de alcance

- Avisos al teléfono con la aplicación cerrada, número sobre el icono y correo.
- Que el equipo del club tenga campana propia o reciba avisos.
- Avisos por respuestas de las familias, ingresos, retiros, ficha, carta o cambios de categoría.
- Preferencias para silenciar o elegir qué avisos recibir.
- Recordatorios automáticos antes de un entrenamiento o partido.
- Ver quién leyó cada aviso.
- Responder o comentar un aviso.
- Avisos a todo el club por un partido jugado.
- Avisar a una familia de que su jugador salió de una convocatoria: ningún niño debe enterarse por
  la plataforma de que ya no está convocado (§17.2).
