# Especificación de funcionalidad: Entrenamientos y partidos dirigidos a equipos y a jugadores añadidos

**Rama de la funcionalidad**: `013-actividades-por-equipo`

**Creada**: 2026-10-12

**Estado**: Borrador

**Entrada**: Descripción del propietario del proyecto: si la categoría 2015 tiene equipo A y equipo B
y entrenan en horarios distintos algunos días, no hay cómo publicar un entrenamiento solo para uno.
Se pide poder elegir, además de la categoría, uno o varios equipos; dentro del equipo, todos los
jugadores o algunos; y poder sumar a jugadores de otros equipos o de otras categorías cuando los del
equipo no alcanzan.

**Plataforma**: LFClub

**Constitución aplicable**: versión 4.11.0, en especial §7.5, §8 (Presidente, Directivo, Entrenador
y Jugador), §11.1, §11.3, §14.4, §17.2, §20, §24 y §28.

**Depende de**: spec 009 (entrenamientos, partidos, convocatorias y asistencia), spec 011
(comunicados dirigidos a equipos y campana) y spec 012 (avisos de actividades).

**Cambia**: la spec 009 dirige los entrenamientos y los partidos a categorías completas y dejó fuera
de alcance las "actividades dirigidas a un equipo concreto"; la §17.2 dice que los entrenamientos son
visibles "para todos los jugadores de las categorías a las que van dirigidos, sin excepción". Esta
especificación añade a quién se dirige un entrenamiento (equipos de la categoría) y permite sumar a
una actividad a jugadores que no son de sus categorías. No cambia la categoría ni los equipos de
ningún jugador, ni la visibilidad de los partidos de la spec 009 para quienes no están convocados.

## Impacto en la constitución

Esta funcionalidad necesitaba una enmienda (4.10.1 → 4.11.0), porque la constitución decía lo
contrario. Ya está aplicada:

- §17.2, "Convocatoria a entrenamientos": deja de decir "sin excepción". Un entrenamiento sin
  equipos sigue siendo visible para todos los jugadores de sus categorías; con equipos, lo ven los
  jugadores de esos equipos y los que el equipo del club haya añadido a ese entrenamiento.
- §11.1: una actividad puede dirigirse, además de a categorías, a uno o varios equipos de ellas, y
  puede sumar a jugadores concretos de otros equipos o de otras categorías sin cambiarles nada.
- §11.3 y §8 (Entrenador): aclaran que el entrenador, aunque su asignación indique qué equipos
  dirige, puede dirigir una actividad a cualquier equipo de sus categorías asignadas, y que sumar a
  un jugador de otra categoría a una actividad no es gestionarlo.
- §7.5: un jugador añadido a una actividad de otra categoría ve solo esa actividad, no el resto de la
  categoría; el aislamiento por categoría sigue intacto.
- §8 (Jugador): ve y responde las actividades a las que fue incluido, aunque no sean de su categoría.
- §14.4 (avisos de actividades): llegan a quien puede ver la actividad, también a los añadidos.
- §20: pruebas nuevas de visibilidad por equipo y por jugador añadido, y de que nada de esto cambia
  la categoría ni los equipos de un jugador.
- §28: sin decisiones nuevas.

**Aplicada**: enmienda 4.10.1 → 4.11.0 (2026-10-12). Nada pendiente antes de `/speckit-plan`.

## Aclaraciones

### Sesión 2026-10-12

- P: ¿Cuál es el caso que no se puede resolver hoy? → R: La categoría 2015 tiene equipo A y equipo B
  que entrenan en horarios distintos algunos días; hoy un entrenamiento solo se puede dirigir a la
  categoría entera.
- P: ¿Qué se quiere elegir? → R: Además de la categoría, uno o varios equipos; dentro del equipo,
  todos los jugadores o algunos; y poder sumar a jugadores de otros equipos o categorías.
- P: ¿Qué pasa si no se elige ningún equipo? → R: El entrenamiento sigue siendo de toda la
  categoría, como hasta ahora.
- P: ¿Quién ve un entrenamiento dirigido a un equipo? → R: Los jugadores de ese equipo, los que se
  añadieron, quienes entrenan la categoría y el PRESIDENTE.
- P: ¿Quién puede sumar jugadores de otra categoría? → R: El PRESIDENTE y quien entrena la
  actividad (recomendación aceptada).
- P: ¿Qué pasa con los entrenamientos próximos de un equipo que se desactiva? → R: Siguen visibles
  para quienes eran de ese equipo hasta que se realizan (recomendación aceptada).

## Escenarios de usuario y pruebas *(obligatorio)*

En esta especificación, "la familia" es la cuenta de un jugador (rol JUGADOR). "El equipo del club"
son el PRESIDENTE, los DIRECTIVOS y los ENTRENADORES. "Quien dirige la actividad" es el PRESIDENTE o
quien entrena alguna de las categorías de la actividad (§17.2). Un jugador "añadido" es uno que no
pertenece a la audiencia natural de la actividad (sus equipos o sus categorías) y que el equipo del
club suma a mano.

### Historia de usuario 1 - Publicar un entrenamiento solo para un equipo (Prioridad: P1)

Quien dirige la actividad programa un entrenamiento de la categoría 2015 y elige, además de la
categoría, el equipo A. Las familias del equipo B no ven ese entrenamiento ni les llega aviso; las
del equipo A sí. Si no elige ningún equipo, el entrenamiento es de toda la categoría, como hoy.

**Por qué esta prioridad**: es el caso que hoy no se puede resolver y es lo que hace que el
calendario de cada familia sea el suyo. Sola ya le sirve al club.

**Prueba independiente**: una categoría con los equipos A y B y jugadores en cada uno, en ambos y en
ninguno; se programan un entrenamiento para A, otro para B, otro para A y B y otro sin equipos; se
comprueba qué ve cada familia, qué aviso recibe y qué ve quien entrena.

**Escenarios de aceptación**:

1. **Dado** una categoría con los equipos A y B, **cuando** quien dirige programa un entrenamiento
   para el equipo A, **entonces** lo ven y reciben su aviso los jugadores activos del equipo A y
   quienes entrenan la categoría y el PRESIDENTE; un jugador solo del equipo B no lo ve, ni en el
   calendario, ni en listados, ni en la campana, ni por su identificador.
2. **Dado** un jugador que está en los equipos A y B, **cuando** se programa un entrenamiento para
   cualquiera de los dos, **entonces** lo ve; si se programa para ambos, lo ve una sola vez.
3. **Dado** un entrenamiento sin equipos, **cuando** lo consulta cualquier jugador activo de la
   categoría, incluso uno sin equipo, **entonces** lo ve, como hasta ahora.
4. **Dado** un jugador que no está en ningún equipo, **cuando** se programa un entrenamiento solo
   para un equipo, **entonces** no lo ve.
5. **Dado** un entrenamiento de un equipo, **cuando** la familia de un jugador de ese equipo
   responde "asistirá" o "no asistirá", **entonces** queda guardado y quien dirige lo ve, como en la
   spec 009.
6. **Dado** el formulario de un entrenamiento nuevo, **cuando** quien dirige elige una categoría sin
   equipos, **entonces** no se ofrece elegir equipos y el entrenamiento es de la categoría.
7. **Dado** un entrenamiento que abarca varias categorías, **cuando** se eligen equipos, **entonces**
   cada equipo pertenece a una de esas categorías y la actividad se dirige a la unión de lo elegido.

---

### Historia de usuario 2 - Armar la convocatoria de un partido por equipo (Prioridad: P2)

Al convocar a un partido, la lista de jugadores se agrupa por equipo y se puede marcar "todos" de un
equipo de una vez o elegir jugadores sueltos. El resto de la convocatoria funciona igual: solo los
convocados ven el partido (§17.2).

**Por qué esta prioridad**: ya se puede convocar a jugadores sueltos; esto lo hace rápido cuando la
categoría tiene equipos.

**Prueba independiente**: una categoría con dos equipos; se convoca "todos del equipo A", se quita a
uno y se añade uno del equipo B; se comprueba quién ve el partido.

**Escenarios de aceptación**:

1. **Dado** una categoría con equipos, **cuando** quien dirige abre la convocatoria, **entonces** los
   jugadores aparecen agrupados por equipo, con los jugadores sin equipo en su propio grupo, y un
   jugador de varios equipos aparece en cada uno de ellos.
2. **Dado** un equipo, **cuando** se marca "todos del equipo", **entonces** quedan marcados todos
   sus jugadores activos y se puede desmarcar a quien no vaya.
3. **Dado** una convocatoria armada, **cuando** se guarda, **entonces** solo los marcados ven el
   partido y el resto sigue sin enterarse, como en la spec 009.
4. **Dado** un jugador marcado dos veces por estar en dos equipos, **cuando** se guarda, **entonces**
   queda convocado una sola vez.

---

### Historia de usuario 3 - Sumar a jugadores de otros equipos o categorías (Prioridad: P2)

Cuando los jugadores del equipo no alcanzan, quien dirige la actividad suma a jugadores de otros
equipos de la categoría o de otras categorías del club. El jugador añadido ve esa actividad y
responde su asistencia, sin cambiar de categoría ni de equipo.

**Por qué esta prioridad**: el club juega y entrena con lo que tiene; sin esto tiene que mover
jugadores de categoría o dejar la lista incompleta. Es P2 porque primero hace falta dirigir por
equipo.

**Prueba independiente**: un entrenamiento del equipo A de la categoría 2015 al que se suma un
jugador del equipo B y otro de la categoría 2018; se comprueba que ambos lo ven y responden, que
siguen en su categoría y equipo, y que no ven nada más de la categoría 2015.

**Escenarios de aceptación**:

1. **Dado** un entrenamiento o un partido, **cuando** quien dirige suma a un jugador de otro equipo
   o de otra categoría, **entonces** ese jugador lo ve en su calendario, recibe su aviso y puede
   responder asistencia.
2. **Dado** un jugador añadido de otra categoría, **cuando** consulta el club, **entonces** ve solo
   esa actividad: no ve los demás entrenamientos, partidos ni jugadores de la categoría de la
   actividad, y su propia categoría no cambia.
3. **Dado** un jugador añadido, **cuando** se consulta su ficha, **entonces** su categoría y sus
   equipos son los mismos de antes.
4. **Dado** una actividad con jugadores añadidos, **cuando** la familia de otro jugador la consulta,
   **entonces** no ve la lista de convocados ni de añadidos (§17.2).
5. **Dado** un jugador añadido, **cuando** quien dirige lo quita de la actividad, **entonces** deja
   de verla y no recibe ningún aviso de que ya no está (§17.2).
6. **Dado** una persona que no es el PRESIDENTE ni entrena alguna categoría de la actividad,
   **cuando** intenta sumar a un jugador, **entonces** el sistema lo rechaza.
7. **Dado** una actividad ya jugada o realizada, **cuando** se suma a un jugador, **entonces** el
   sistema lo trata como hoy trata los cambios sobre actividades pasadas.

---

### Historia de usuario 4 - Ver y corregir a quién se dirige una actividad (Prioridad: P3)

Quien dirige la actividad ve en el detalle a qué equipos va dirigida y quiénes fueron añadidos, y
puede cambiarlo mientras la actividad no haya pasado.

**Por qué esta prioridad**: se equivocan al elegir; corregir sin cancelar y volver a crear evita
avisos duplicados.

**Prueba independiente**: se cambia el equipo de un entrenamiento próximo de A a B y se comprueba
qué ve cada familia y qué avisos nuevos hay.

**Escenarios de aceptación**:

1. **Dado** un entrenamiento próximo de un equipo, **cuando** quien dirige cambia los equipos,
   **entonces** lo ve quien ahora corresponde y deja de verlo quien ya no.
2. **Dado** un cambio de equipos, **cuando** se guarda, **entonces** las familias que lo ven a partir
   de ahora reciben su aviso (spec 012) y las que dejan de verlo no reciben nada (§17.2).
3. **Dado** una actividad dirigida a equipos, **cuando** la ve quien dirige, **entonces** el detalle
   dice a qué equipos va y los jugadores añadidos aparecen marcados como tales.

---

### Casos límite

- Un jugador cambia de equipo después de publicado un entrenamiento próximo: ve los entrenamientos
  de su equipo actual, no los del anterior; los ya realizados conservan su asistencia.
- Un jugador cambia de categoría: sale de los equipos de la anterior (§11.3) y, si estaba añadido a
  una actividad próxima, sigue en ella.
- Un equipo se desactiva con entrenamientos próximos dirigidos a él: siguen visibles para quienes
  eran de ese equipo hasta que se realizan; no pasan a toda la categoría ni se cancelan solos.
- Un entrenamiento dirigido a un equipo sin jugadores activos se puede guardar, como un partido sin
  convocados (spec 009), y solo lo ve quien dirige.
- Un jugador retirado del club no aparece en la audiencia ni se puede añadir; si ya estaba añadido,
  deja de contar para la actividad próxima.
- Una familia con varios hermanos ve cada actividad solo para el hermano al que corresponde.
- Una actividad con equipos y con jugadores añadidos del mismo equipo no duplica a nadie.
- Programar varios días a la vez (spec 009) aplica los mismos equipos y los mismos jugadores añadidos
  a cada día; después se pueden cambiar por separado.
- Un jugador añadido a un partido de otra categoría y que juega marca goles: el gol cuenta en sus
  estadísticas como cualquier otro.

## Requisitos *(obligatorio)*

### Requisitos funcionales

#### Dirigir un entrenamiento a equipos

- **RF-001**: Al programar un entrenamiento, el sistema DEBE permitir elegir, además de las
  categorías, uno o varios equipos activos de esas categorías.
- **RF-002**: Un entrenamiento sin equipos DEBE seguir dirigiéndose a todos los jugadores activos de
  sus categorías, incluidos los que no están en ningún equipo, igual que hoy.
- **RF-003**: Un entrenamiento con equipos DEBE dirigirse a los jugadores activos de esos equipos;
  un jugador de varios equipos elegidos lo ve una sola vez.
- **RF-004**: Un entrenamiento con equipos DEBE ser visible para los jugadores a los que se dirige,
  los jugadores añadidos (RF-014), quienes entrenan alguna de sus categorías y el PRESIDENTE. Para
  cualquier otra cuenta, DEBE no existir: ni en el calendario, ni en listados, ni en contadores, ni
  en la campana, ni por su identificador.
- **RF-005**: El sistema DEBE ofrecer elegir equipos solo cuando alguna de las categorías elegidas
  tiene equipos activos.
- **RF-006**: Quien entrena una categoría DEBE poder dirigir una actividad a cualquier equipo de esa
  categoría, aunque su asignación indique que dirige solo algunos (§11.3).
- **RF-007**: Programar varios días a la vez DEBE aplicar los mismos equipos y los mismos jugadores
  añadidos a cada día, y cada entrenamiento DEBE poder cambiarse o cancelarse por separado.
- **RF-008**: La asistencia y las respuestas de las familias (spec 009) DEBEN valer para todos los
  que ven el entrenamiento, incluidos los jugadores añadidos; nadie ve las respuestas de otros.

#### Convocar por equipo

- **RF-009**: En la convocatoria de un partido, el sistema DEBE agrupar a los jugadores por equipo,
  con un grupo para los que no tienen equipo; un jugador de varios equipos aparece en cada uno.
- **RF-010**: El sistema DEBE permitir marcar a todos los jugadores de un equipo de una vez y
  desmarcar a cualquiera después, así como elegir jugadores sueltos.
- **RF-011**: Un jugador marcado por más de un camino DEBE quedar convocado una sola vez.
- **RF-012**: Marcar a un equipo completo en la convocatoria NO DEBE cambiar la visibilidad del
  partido: sigue siendo solo para los convocados, quienes entrenan sus categorías y el PRESIDENTE
  (§17.2).

#### Sumar jugadores de otros equipos o categorías

- **RF-013**: El sistema DEBE permitir sumar a un entrenamiento o a un partido a jugadores activos
  de otros equipos de sus categorías y de otras categorías del mismo club.
- **RF-014**: Un jugador añadido DEBE ver esa actividad, recibir sus avisos y responder su
  asistencia, y NO DEBE ver ninguna otra cosa de la categoría de la actividad.
- **RF-015**: Sumar a un jugador NO DEBE cambiar su categoría ni sus equipos, ni crear una
  categoría nueva (§11.1).
- **RF-016**: Solo el PRESIDENTE y quien entrena alguna de las categorías de la actividad DEBEN
  poder sumar a jugadores de otros equipos o de otras categorías; para cualquier otra cuenta, el
  sistema DEBE rechazarlo. Sumar a un jugador no es gestionarlo: quien lo suma no gana acceso a su
  ficha ni a nada más de él.
- **RF-017**: Quitar a un jugador añadido DEBE hacerle dejar de ver la actividad sin ningún aviso ni
  rastro visible para su familia (§17.2).
- **RF-018**: La familia de otro jugador NO DEBE poder ver la lista de convocados ni de añadidos.
- **RF-019**: Los goles y la asistencia de un jugador añadido DEBEN contar en sus estadísticas como
  los de cualquier otro jugador.

#### Ver y cambiar a quién va dirigida

- **RF-020**: Quien dirige DEBE poder cambiar los equipos y los jugadores añadidos de una actividad
  que no ha pasado.
- **RF-021**: El detalle de la actividad DEBE mostrar a quien dirige a qué equipos va y quiénes
  fueron añadidos, distinguiéndolos de los jugadores de la audiencia natural.
- **RF-022**: Al cambiar la audiencia, las familias que pasan a verla DEBEN recibir su aviso (spec
  012) y las que dejan de verla NO DEBEN recibir ninguno.

#### Seguridad y alcance

- **RF-023**: El backend DEBE aplicar toda la visibilidad de este documento; no basta con ocultar en
  el frontend.
- **RF-024**: Ningún aviso, listado o contador DEBE revelar a quien no puede ver una actividad que
  existe, ni a qué equipos va, ni quiénes están en ella.
- **RF-025**: Lo que ya hacían los entrenamientos y los partidos sin equipos ni añadidos DEBE seguir
  igual, incluidos los datos ya guardados, que se interpretan como "toda la categoría".
- **RF-026**: Un entrenamiento dirigido a un equipo que se desactiva DEBE seguir siendo visible
  para los jugadores que eran de ese equipo hasta que se realiza, sin cambiar de audiencia ni
  cancelarse.

### Entidades clave

- **Equipos de una actividad**: el conjunto de equipos activos de las categorías a los que se dirige
  un entrenamiento. Vacío significa toda la categoría.
- **Jugador añadido**: relación entre una actividad y un jugador que no pertenece a su audiencia
  natural; no cambia la categoría ni los equipos del jugador.
- **Audiencia de un entrenamiento**: jugadores a los que se dirige por sus categorías o equipos, más
  los añadidos.

## Criterios de éxito *(obligatorio)*

### Resultados medibles

- **CE-001**: Un club con una categoría de dos equipos puede publicar entrenamientos distintos para
  cada equipo en un mismo día, y en el 100 % de los casos probados cada familia ve solo los suyos.
- **CE-002**: En el 100 % de los casos probados, una cuenta que no está en la audiencia ni añadida
  no encuentra el entrenamiento por ningún camino: calendario, listados, contadores, campana ni
  identificador.
- **CE-003**: En el 100 % de los casos probados, sumar a un jugador no cambia su categoría ni sus
  equipos, y un jugador añadido de otra categoría no ve nada más de ella.
- **CE-004**: En el 100 % de los casos probados, quitar a un jugador de una actividad no le genera
  ningún aviso ni deja rastro visible.
- **CE-005**: En el 100 % de los casos probados, lo que hacían los entrenamientos y los partidos sin
  equipos ni añadidos sigue igual.
- **CE-006**: Armar la convocatoria de un equipo completo requiere una sola acción, sin marcar jugador
  por jugador.
- **CE-007**: Las pantallas se leen sin desplazamiento horizontal en teléfono y en escritorio, en
  tema claro y oscuro, con los colores de cualquier club probado.

## Supuestos

- Los entrenamientos se dirigen a equipos y los partidos se arman por convocatoria; este documento
  no hace que un partido "sea de un equipo" en el listado, solo agrupa y acelera su convocatoria.
- La audiencia de un entrenamiento por equipos se calcula con los equipos actuales de cada jugador,
  igual que hoy se calcula con su categoría actual; los añadidos son una lista guardada.
- Sumar jugadores se ofrece en entrenamientos y en partidos con las mismas reglas.
- Un jugador añadido de otra categoría ve solo esa actividad; no se le da acceso a nada más de la
  categoría, y quien entrena su propia categoría no gana acceso por ello.
- Quien entrena una categoría ve todas las actividades de esa categoría, también las dirigidas a
  equipos que no dirige, porque ve a todos sus jugadores (§11.3).
- Las categorías sin equipos funcionan exactamente igual que hoy.

## Fuera de alcance

- Que un partido "pertenezca" a un equipo en el listado o en las tablas de posiciones.
- Aviso a un jugador de que fue quitado de una actividad: ningún niño debe enterarse por la
  plataforma de que no fue convocado (§17.2).
- Mover a un jugador de categoría o de equipo desde la actividad.
- Equipos entre clubes distintos.
- Aviso con la aplicación cerrada y recordatorios automáticos (spec 012).
