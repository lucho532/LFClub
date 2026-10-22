# Especificación de funcionalidad: Categorías del club

**Rama de la funcionalidad**: `003-categorias-club`

**Creada**: 2026-10-07

**Estado**: Borrador

**Entrada**: Descripción del propietario del proyecto: "Categorías del club: el PRESIDENTE o
DIRECTIVO crea categorías, les asigna entrenadores y ubica a los jugadores aprobados en ellas"

**Plataforma**: LFClub

**Constitución aplicable**: versión 3.7.0, en especial §7.1, §7.4, §7.5, §8, §11, §12.1.1, §12.3,
§14 y §18.

**Depende de**: spec 001 (base multiclub) y spec 002 (ingreso de personas al club), de la que
reutiliza los integrantes aprobados, sus roles y la aprobación del ingreso.

## Aclaraciones

### Sesión 2026-10-07

- P: La descripción dice "el PRESIDENTE o DIRECTIVO", pero la constitución deja la gestión de
  categorías solo al PRESIDENTE. ¿Qué puede hacer el DIRECTIVO con las categorías? → R: Solo
  consulta. Crea categorías, asigna entrenadores y ubica jugadores únicamente el PRESIDENTE; el
  DIRECTIVO lo ve todo sin modificar nada. La constitución no cambia.
- P: Al aprobar el ingreso de un jugador, ¿qué pasa si el club todavía no ha creado la categoría
  de su año de nacimiento? → R: El ingreso se aprueba igual y el jugador queda sin categoría, en
  una lista "Sin categoría". Cuando se crea la categoría de su año entra en ella automáticamente;
  también se le puede ubicar a mano.
- P: ¿Las familias ven quién entrena a su jugador? → R: Sí. Ven el nombre de los entrenadores de
  la categoría de su jugador.
- P: El presidente, dueño del club, muchas veces también entrena. ¿Cómo se refleja? → R: Un
  PRESIDENTE puede quedar asignado como entrenador de una categoría sin dejar de ser PRESIDENTE.
- P: A veces una categoría tiene equipo A y equipo B, o élite y B. ¿Entra en esta funcionalidad?
  → R: Sí. Una categoría puede dividirse en equipos.
- P: Con equipos, ¿cómo se asignan los entrenadores y qué ve cada uno? → R: El entrenador se
  asigna a la categoría y se indica qué equipo dirige. Ve a todos los jugadores de la categoría,
  de cualquier equipo.
- P: ¿Un jugador puede estar en más de un equipo de su categoría a la vez? → R: Sí, en varios.
- P: ¿Una categoría puede agrupar más de un año de nacimiento? → R: No. Un solo año por
  categoría; si dos años trabajan juntos, son dos categorías con los mismos entrenadores.
- P: ¿Quién decide en qué equipo juega cada jugador de una categoría? → R: Solo el PRESIDENTE.
  Los entrenadores de la categoría consultan los equipos, pero no ponen ni sacan jugadores.
- P: ¿Una categoría o un equipo creado por error se puede borrar del todo? → R: Sí, mientras
  nunca haya tenido jugadores ni entrenadores. Si alguna vez los tuvo, solo se desactiva.
- P: ¿Un DIRECTIVO también puede quedar asignado como entrenador de una categoría? → R: Sí, sin
  dejar de ser DIRECTIVO. Aparece como entrenador de la categoría; lo que pueda hacer como
  entrenador lo decidirán las funcionalidades de entrenamientos y convocatorias.
- P: Cuando un jugador se va del club, ¿el PRESIDENTE lo borra del todo o lo retira? → R: Lo
  retira, y entra en esta funcionalidad. El jugador deja de entrar a ese club, sale de su
  categoría y de sus equipos y ya no aparece en las listas; sus datos e historial se conservan y
  el PRESIDENTE puede reincorporarlo.
- P: ¿Se puede desactivar una categoría que todavía tiene jugadores? → R: No. Solo cuando ya no
  le quedan, porque se retiraron o pasaron a otra categoría.

## Escenarios de usuario y pruebas *(obligatorio)*

### Historia de usuario 1 - El presidente crea las categorías de su club (Prioridad: P1)

El PRESIDENTE entra al apartado "Categorías" de su club y crea una categoría indicando el año de
nacimiento que agrupa (2012, 2013, 2014...). Ve la lista de categorías del club, ordenada por año,
con cuántos jugadores y qué entrenadores tiene cada una. Si una categoría deja de usarse, la
desactiva; si vuelve a necesitarla, la reactiva. Si la creó por error y nunca llegó a usarse, la
borra.

**Por qué esta prioridad**: sin categorías no hay dónde ubicar jugadores ni a qué asignar
entrenadores. Todo lo demás de esta funcionalidad, y las mensualidades, los entrenamientos y las
convocatorias que vendrán después, se apoyan en ellas.

**Prueba independiente**: se inicia sesión como PRESIDENTE de un club sin categorías, se crean dos
categorías y se comprueba que aparecen en la lista ordenadas por año; se desactiva una y se
comprueba que queda marcada como inactiva.

**Escenarios de aceptación**:

1. **Dado** el PRESIDENTE de un club, **cuando** crea una categoría indicando un año de nacimiento
   válido, **entonces** la categoría aparece en la lista del club como activa, sin jugadores y sin
   entrenadores.
2. **Dado** un club que ya tiene la categoría de un año, activa o inactiva, **cuando** el
   PRESIDENTE intenta crear otra con el mismo año, **entonces** el sistema no la crea y le explica
   que ya existe; si está inactiva, le indica que puede reactivarla.
3. **Dado** el PRESIDENTE creando una categoría, **cuando** el año está vacío, no es un año o es
   posterior al año en curso, **entonces** el sistema no la crea y le explica por qué.
4. **Dado** una categoría activa sin jugadores, **cuando** el PRESIDENTE la desactiva y lo
   confirma, **entonces** queda inactiva, sigue viéndose en la lista marcada como inactiva y sus
   entrenadores dejan de estar asignados a ella.
5. **Dado** una categoría activa con jugadores, **cuando** el PRESIDENTE intenta desactivarla,
   **entonces** el sistema se lo niega y le explica que antes debe pasar a sus jugadores a otra
   categoría o retirarlos del club.
6. **Dado** una categoría inactiva, **cuando** el PRESIDENTE la reactiva, **entonces** vuelve a
   estar activa, sin entrenadores asignados.
7. **Dado** dos clubes, **cuando** cada uno crea la categoría del mismo año, **entonces** son dos
   categorías distintas y ninguna aparece en el otro club.
8. **Dado** un DIRECTIVO, un ENTRENADOR, un JUGADOR, una cuenta en espera o alguien sin sesión,
   **cuando** intenta crear, desactivar, reactivar o borrar una categoría, **entonces** el sistema
   se lo niega.
9. **Dado** una categoría que nunca ha tenido jugadores ni entrenadores, **cuando** el PRESIDENTE
   la borra y lo confirma, **entonces** desaparece de la lista junto con sus equipos y el club
   puede volver a crear la categoría de ese año.
10. **Dado** una categoría que tiene o tuvo alguna vez jugadores o entrenadores, **cuando** el
    PRESIDENTE intenta borrarla, **entonces** el sistema se lo niega y le explica que solo puede
    desactivarla.

---

### Historia de usuario 2 - Los jugadores aprobados quedan en la categoría de su año (Prioridad: P1)

Cuando el club aprueba el ingreso de una persona como jugador, queda ubicada sola en la categoría
de su año de nacimiento. Si el club todavía no tiene esa categoría, el jugador queda en la lista
"Sin categoría"; en cuanto el PRESIDENTE crea la categoría de ese año, los jugadores sin categoría
nacidos ese año entran en ella sin que nadie tenga que moverlos. Lo mismo pasa con los jugadores
que el club aprobó antes de que existiera esta funcionalidad.

**Por qué esta prioridad**: es la regla que evita ubicar a mano a cada jugador. Sin ella las
categorías quedarían vacías o dependerían de que el PRESIDENTE acomodara uno por uno a todos los
jugadores del club.

**Prueba independiente**: con la categoría 2014 creada, se aprueba el ingreso de un jugador nacido
en 2014 y se comprueba que aparece en ella; se aprueba otro nacido en 2016, se comprueba que queda
en "Sin categoría", se crea la categoría 2016 y se comprueba que pasa a ella.

**Escenarios de aceptación**:

1. **Dado** un club con la categoría activa de un año, **cuando** se aprueba como JUGADOR el
   ingreso de una persona nacida ese año, **entonces** queda en esa categoría sin ninguna acción
   adicional.
2. **Dado** un club sin la categoría de un año, o con ella inactiva, **cuando** se aprueba como
   JUGADOR el ingreso de una persona nacida ese año, **entonces** el ingreso se aprueba igual y el
   jugador aparece en la lista "Sin categoría".
3. **Dado** jugadores aprobados sin categoría, **cuando** el PRESIDENTE crea o reactiva la
   categoría de su año de nacimiento, **entonces** esos jugadores quedan en ella y el sistema le
   informa de cuántos entraron.
4. **Dado** una persona cuyo ingreso se aprueba con el rol ENTRENADOR o DIRECTIVO, **cuando** se
   aprueba, **entonces** no se le asigna categoría y no aparece en "Sin categoría".
5. **Dado** una persona en espera, **cuando** se consultan las categorías y la lista "Sin
   categoría", **entonces** no aparece en ninguna.
6. **Dado** un jugador que el PRESIDENTE ubicó a mano en una categoría distinta a la de su año,
   **cuando** se crea o reactiva la categoría de su año, **entonces** el jugador se queda donde
   estaba.
7. **Dado** un DIRECTIVO que aprueba un ingreso como JUGADOR, **cuando** existe la categoría del
   año, **entonces** el jugador queda en ella igual que si lo hubiera aprobado el PRESIDENTE.

---

### Historia de usuario 3 - El presidente asigna entrenadores a las categorías (Prioridad: P2)

El PRESIDENTE abre una categoría y elige, entre los entrenadores de su club, quiénes quedan a
cargo de ella. Una categoría puede tener varios entrenadores y un entrenador puede tener varias
categorías. Como en muchos clubes el presidente y los directivos también entrenan, puede
asignarse a sí mismo, a otro PRESIDENTE o a un DIRECTIVO del club, sin que ninguno cambie de rol. Cuando un entrenador deja de
llevar una categoría, el PRESIDENTE le retira la asignación.

**Por qué esta prioridad**: define qué ve y qué puede hacer cada entrenador en todo lo que viene
después. Va detrás de las dos primeras porque un club puede tener sus categorías y jugadores
ordenados antes de repartir a los entrenadores.

**Prueba independiente**: con una categoría creada y un integrante aprobado con el rol ENTRENADOR,
se le asigna la categoría y se comprueba que aparece como su entrenador; se le retira y se
comprueba que deja de aparecer.

**Escenarios de aceptación**:

1. **Dado** una categoría activa, **cuando** el PRESIDENTE va a asignarle un entrenador,
   **entonces** el sistema le ofrece únicamente a los integrantes aprobados de su club con el rol
   ENTRENADOR, DIRECTIVO o PRESIDENTE que todavía no están asignados a ella, incluido él mismo.
2. **Dado** un entrenador ofrecido, **cuando** el PRESIDENTE lo asigna, **entonces** aparece como
   entrenador de esa categoría.
3. **Dado** un entrenador ya asignado a una categoría, **cuando** el PRESIDENTE lo asigna a otra,
   **entonces** queda como entrenador de las dos.
4. **Dado** una categoría con un entrenador, **cuando** el PRESIDENTE le asigna otro más,
   **entonces** la categoría queda con los dos.
5. **Dado** un entrenador asignado a una categoría, **cuando** el PRESIDENTE le retira la
   asignación y lo confirma, **entonces** deja de aparecer como entrenador de esa categoría y
   conserva las demás que tenga.
6. **Dado** un integrante con el rol JUGADOR, una persona en espera o alguien de otro club,
   **cuando** se intenta asignarlo como entrenador de una categoría, incluso conociendo su
   identificador, **entonces** el sistema se lo niega.
7. **Dado** una categoría inactiva, **cuando** se intenta asignarle un entrenador, **entonces** el
   sistema se lo niega.
8. **Dado** un DIRECTIVO, un ENTRENADOR, un JUGADOR, una cuenta en espera o alguien sin sesión,
   **cuando** intenta asignar o retirar un entrenador, **entonces** el sistema se lo niega.
9. **Dado** un PRESIDENTE asignado como entrenador de una categoría, **cuando** entra a su club,
   **entonces** conserva el rol PRESIDENTE y todo lo que puede hacer con él, y aparece como
   entrenador de esa categoría.
10. **Dado** un DIRECTIVO asignado como entrenador de una categoría, **cuando** entra a su club,
    **entonces** conserva el rol DIRECTIVO, sigue sin poder modificar nada de las categorías y
    aparece como entrenador de esa categoría.

---

### Historia de usuario 4 - El presidente ubica o cambia de categoría a un jugador (Prioridad: P2)

El PRESIDENTE ve la lista "Sin categoría" y los jugadores de cada categoría. Puede ubicar a un
jugador sin categoría en cualquiera de las categorías activas, y puede pasar a un jugador de una
categoría a otra, por ejemplo cuando juega con los de un año mayor. Cuando la categoría de un
jugador no coincide con su año de nacimiento, la pantalla lo señala.

**Por qué esta prioridad**: cubre las excepciones a la regla automática. El club funciona sin
ella mientras todos sus jugadores estén en la categoría de su año.

**Prueba independiente**: con dos categorías y un jugador en una de ellas, se le pasa a la otra y
se comprueba que aparece solo en la nueva y que la pantalla señala que no es la de su año.

**Escenarios de aceptación**:

1. **Dado** un jugador sin categoría, **cuando** el PRESIDENTE lo ubica en una categoría activa,
   **entonces** aparece en ella y desaparece de "Sin categoría".
2. **Dado** un jugador de una categoría, **cuando** el PRESIDENTE lo pasa a otra categoría activa
   y lo confirma, **entonces** aparece solo en la nueva: sigue siendo el mismo jugador, con los
   mismos datos.
3. **Dado** un jugador ubicado en una categoría que no es la de su año de nacimiento, **cuando**
   se consulta esa categoría, **entonces** el jugador aparece señalado con su año de nacimiento.
4. **Dado** una categoría inactiva o de otro club, **cuando** se intenta ubicar en ella a un
   jugador, incluso conociendo su identificador, **entonces** el sistema se lo niega.
5. **Dado** un integrante con el rol ENTRENADOR, DIRECTIVO o PRESIDENTE, o una persona en espera,
   **cuando** se intenta ubicarlo en una categoría, **entonces** el sistema se lo niega: solo los
   jugadores aprobados tienen categoría.
6. **Dado** un DIRECTIVO, un ENTRENADOR, un JUGADOR, una cuenta en espera o alguien sin sesión,
   **cuando** intenta ubicar o cambiar de categoría a un jugador, **entonces** el sistema se lo
   niega.

---

### Historia de usuario 5 - El presidente divide una categoría en equipos (Prioridad: P3)

Algunas categorías tienen más de un equipo: A y B, o élite y B. El PRESIDENTE abre una categoría y
crea sus equipos, cada uno con un nombre. Después indica en qué equipos juega cada jugador de esa
categoría, que pueden ser varios a la vez, y qué equipos dirige cada entrenador de la categoría.
Una categoría sin equipos sigue funcionando igual que antes.

**Por qué esta prioridad**: no todos los clubes ni todas las categorías se dividen en equipos. El
club funciona sin ellos, y los que los usan necesitan antes tener categorías, jugadores y
entrenadores.

**Prueba independiente**: en una categoría con jugadores y un entrenador, se crean los equipos "A"
y "B", se pone a un jugador en los dos y a otro solo en el "B", se indica que el entrenador dirige
el "A" y se comprueba que la categoría muestra cada equipo con sus jugadores y su entrenador.

**Escenarios de aceptación**:

1. **Dado** una categoría activa, **cuando** el PRESIDENTE crea un equipo con un nombre,
   **entonces** el equipo aparece dentro de esa categoría, sin jugadores y sin entrenadores.
2. **Dado** una categoría que ya tiene un equipo con un nombre, **cuando** el PRESIDENTE intenta
   crear otro con el mismo nombre, aunque cambien las mayúsculas, **entonces** el sistema no lo
   crea y le explica que ya existe. En otra categoría el mismo nombre sí se admite.
3. **Dado** un jugador de una categoría con equipos, **cuando** el PRESIDENTE lo pone en un
   equipo de esa categoría, **entonces** aparece en ese equipo y sigue en los demás equipos en los
   que ya estaba.
4. **Dado** un jugador de un equipo, **cuando** el PRESIDENTE lo saca de él, **entonces** deja de
   aparecer en ese equipo y sigue en su categoría y en sus demás equipos.
5. **Dado** un jugador, **cuando** se intenta ponerlo en un equipo de una categoría que no es la
   suya, incluso conociendo los identificadores, **entonces** el sistema se lo niega.
6. **Dado** un jugador con equipos, **cuando** el PRESIDENTE lo pasa a otra categoría,
   **entonces** sale de todos los equipos de la categoría anterior y queda sin equipo en la nueva.
7. **Dado** un entrenador asignado a una categoría con equipos, **cuando** el PRESIDENTE indica
   qué equipos dirige, **entonces** aparece como entrenador de esos equipos y sigue viendo a todos
   los jugadores de la categoría.
8. **Dado** un entrenador de una categoría con equipos al que no se le indica ningún equipo,
   **cuando** se consulta la categoría, **entonces** aparece como entrenador de la categoría en
   general.
9. **Dado** un equipo, **cuando** el PRESIDENTE le cambia el nombre, **entonces** se muestra con
   el nombre nuevo y conserva sus jugadores y entrenadores.
10. **Dado** un equipo, **cuando** el PRESIDENTE lo desactiva y lo confirma, **entonces** deja de
    mostrarse, sus jugadores siguen en la categoría y sus entrenadores siguen asignados a la
    categoría.
11. **Dado** un DIRECTIVO, un ENTRENADOR, un JUGADOR, una cuenta en espera o alguien sin sesión,
    **cuando** intenta crear, renombrar, desactivar o borrar un equipo, o cambiar sus jugadores o
    entrenadores, **entonces** el sistema se lo niega.
12. **Dado** un equipo que nunca ha tenido jugadores ni entrenadores, **cuando** el PRESIDENTE lo
    borra y lo confirma, **entonces** desaparece de la categoría; si alguna vez los tuvo, el
    sistema se lo niega y le explica que solo puede desactivarlo.

---

### Historia de usuario 6 - El presidente retira a un jugador que se fue del club (Prioridad: P3)

Un jugador deja el club: terminó su etapa o se fue a otro equipo. El PRESIDENTE lo retira. Desde
ese momento el jugador ya no aparece en su categoría, en sus equipos ni en "Sin categoría", y su
familia deja de entrar a ese club. El club conserva sus datos y lo ve en una lista de jugadores
retirados; si el jugador vuelve, el PRESIDENTE lo reincorpora sin que tenga que registrarse otra
vez.

**Por qué esta prioridad**: mantiene las categorías con los jugadores que de verdad están en el
club y permite cerrar una categoría que terminó. El club puede empezar a trabajar sin ella.

**Prueba independiente**: con un jugador en una categoría y en un equipo, se le retira y se
comprueba que desaparece de ambos, que aparece en la lista de retirados y que su cuenta ya no
entra a ese club; se le reincorpora y se comprueba que vuelve a entrar y queda en la categoría de
su año.

**Escenarios de aceptación**:

1. **Dado** un jugador aprobado, **cuando** el PRESIDENTE lo retira, **entonces** el sistema le
   pide confirmación antes de hacerlo.
2. **Dado** un retiro confirmado, **cuando** se consultan las categorías, **entonces** el jugador
   ya no está en su categoría, en ninguno de sus equipos ni en "Sin categoría", y aparece en la
   lista de retirados con su nombre, sus apellidos, su año de nacimiento, quién lo retiró y
   cuándo.
3. **Dado** un jugador retirado cuyo único club era ese, **cuando** su familia inicia sesión,
   **entonces** ve un aviso de que el jugador ya no está en el club y ningún dato del club.
4. **Dado** un jugador retirado que pertenece a otros clubes, **cuando** inicia sesión,
   **entonces** sigue entrando a sus otros clubes con normalidad y al elegir el club que lo
   retiró ve solo ese aviso.
5. **Dado** un jugador retirado, **cuando** intenta obtener cualquier información del club,
   incluso llamando directamente a las operaciones, **entonces** el sistema se lo niega.
6. **Dado** un jugador retirado, **cuando** el PRESIDENTE lo reincorpora, **entonces** vuelve a
   entrar al club con su misma cuenta y sus mismos datos, y queda en la categoría activa de su año
   de nacimiento o, si no existe, en "Sin categoría". No recupera los equipos que tenía.
7. **Dado** el correo o el documento de un jugador retirado, **cuando** alguien intenta invitarlo
   o registrarlo de nuevo en ese club, **entonces** el sistema no lo permite y explica que esa
   persona está retirada y que el PRESIDENTE puede reincorporarla.
8. **Dado** una categoría cuyos jugadores se retiraron todos, **cuando** el PRESIDENTE la
   desactiva, **entonces** el sistema lo permite.
9. **Dado** un integrante con el rol ENTRENADOR, DIRECTIVO o PRESIDENTE, o una persona en espera,
   **cuando** se intenta retirarlo, **entonces** el sistema se lo niega: el retiro solo aplica a
   jugadores aprobados.
10. **Dado** un DIRECTIVO, **cuando** abre la lista de retirados, **entonces** la ve sin ninguna
    opción para retirar ni reincorporar.
11. **Dado** un DIRECTIVO, un ENTRENADOR, un JUGADOR, una cuenta en espera, alguien sin sesión o
    alguien de otro club, **cuando** intenta retirar o reincorporar a un jugador, **entonces** el
    sistema se lo niega.
12. **Dado** un jugador retirado, **cuando** acepta una invitación de PRESIDENTE de ese club,
    **entonces** vuelve a entrar como PRESIDENTE, sin categoría ni equipos, y deja de aparecer en
    la lista de retirados.

---

### Historia de usuario 7 - Cada quien consulta las categorías que le corresponden (Prioridad: P3)

El DIRECTIVO entra al apartado "Categorías" y ve lo mismo que el PRESIDENTE, sin poder cambiar
nada. El ENTRENADOR ve solamente las categorías que tiene asignadas, con sus equipos y sus
jugadores. La familia del jugador ve, en el inicio de su club, en qué categoría y en qué equipos
está su jugador y quiénes lo entrenan.

**Por qué esta prioridad**: es lo que hace visible el resultado para el resto del club, pero no
impide que el PRESIDENTE deje sus categorías organizadas.

**Prueba independiente**: con un entrenador asignado a una de dos categorías, se inicia sesión con
su cuenta y se comprueba que ve solo esa categoría y sus jugadores; se inicia sesión como
DIRECTIVO y se comprueba que ve las dos y ninguna opción para modificarlas.

**Escenarios de aceptación**:

1. **Dado** un DIRECTIVO, **cuando** abre el apartado "Categorías", **entonces** ve todas las
   categorías de su club, sus equipos, sus entrenadores, sus jugadores y la lista "Sin categoría",
   sin ninguna opción para modificarlas.
2. **Dado** un ENTRENADOR con categorías asignadas, **cuando** abre el apartado "Categorías",
   **entonces** ve únicamente esas categorías, cada una con sus equipos y con el nombre, los
   apellidos, el año de nacimiento y los equipos de todos sus jugadores, dirija o no ese equipo.
3. **Dado** un ENTRENADOR, **cuando** intenta consultar una categoría que no tiene asignada, sus
   jugadores o la lista "Sin categoría", incluso conociendo el identificador, **entonces** el
   sistema se lo niega y no le muestra ningún dato.
4. **Dado** un ENTRENADOR sin ninguna categoría asignada, **cuando** abre el apartado
   "Categorías", **entonces** ve un mensaje que le indica que todavía no tiene categorías
   asignadas.
5. **Dado** un JUGADOR con categoría, **cuando** entra a su club, **entonces** ve en el inicio la
   categoría de su jugador, los equipos en los que está y el nombre y los apellidos de los
   entrenadores de su categoría, indicando qué equipo dirige cada uno.
6. **Dado** un JUGADOR sin categoría, **cuando** entra a su club, **entonces** ve que todavía no
   tiene categoría asignada.
7. **Dado** un JUGADOR, **cuando** intenta consultar la lista de categorías, los jugadores de una
   categoría o de un equipo, o los entrenadores de una categoría que no es la suya, incluso
   conociendo los identificadores, **entonces** el sistema se lo niega.
8. **Dado** un JUGADOR, **cuando** ve a los entrenadores de su categoría, **entonces** no ve su
   correo, su celular, su documento ni ningún otro dato suyo.
9. **Dado** un integrante de un club, **cuando** intenta consultar una categoría de otro club por
   su identificador, **entonces** el sistema se lo niega.

---

### Casos límite

- El PRESIDENTE crea por error la categoría de un año equivocado: la borra si nunca tuvo
  jugadores ni entrenadores. Si al crearla entraron solos jugadores de ese año, ya no se puede
  borrar: los pasa a otra categoría y la desactiva.
- Una categoría sin uso tiene equipos que tampoco se usaron: al borrarla se borran con ella.
- Dos presidentes del mismo club crean al mismo tiempo la categoría del mismo año: se crea una
  sola y el segundo ve que ya existe.
- Se aprueba el ingreso de un jugador en el mismo momento en que se crea la categoría de su año:
  el jugador termina en esa categoría, no en "Sin categoría".
- Se crea la categoría de un año en el que no nació ningún jugador sin categoría: se crea vacía y
  el sistema informa de que no entró ninguno.
- Un jugador adulto, o de un año muy anterior al de las demás categorías, queda en "Sin categoría"
  hasta que el PRESIDENTE lo ubica a mano o crea la categoría de su año.
- La categoría del año del jugador existe pero está inactiva: el jugador queda en "Sin categoría".
- Dos presidentes pasan al mismo jugador a categorías distintas al mismo tiempo: vale la última
  acción y el jugador queda en una sola categoría.
- Se asigna dos veces el mismo entrenador a la misma categoría: no produce ningún efecto
  adicional.
- El PRESIDENTE retira a un entrenador de su única categoría mientras este tiene la pantalla
  abierta: en su siguiente acción deja de ver esa categoría.
- Se desactiva una categoría con entrenadores asignados y sin jugadores: los entrenadores dejan de
  estar asignados y, si se reactiva, hay que volver a asignarlos.
- Un entrenador es además padre de un jugador del club: usa dos cuentas (constitución §8); con la
  de entrenador ve sus categorías asignadas y con la del jugador solo la categoría de su hijo.
- El único entrenador de una categoría es el propio PRESIDENTE: se asigna a sí mismo y las
  familias lo ven como entrenador.
- La categoría de un jugador no tiene ningún entrenador asignado: la familia ve la categoría y
  que todavía no tiene entrenador.
- Un jugador está a la vez en el equipo A y en el B: aparece en los dos y cuenta una sola vez en
  el total de jugadores de la categoría.
- Una categoría tiene equipos y un jugador suyo no está en ninguno: sigue en la categoría y la
  pantalla lo muestra sin equipo.
- Se desactiva una categoría que tiene equipos: sus equipos dejan de mostrarse con ella y vuelven,
  vacíos, si se reactiva.
- Se desactiva el único equipo que dirigía un entrenador: queda como entrenador de la categoría en
  general.
- A un entrenador se le retira la categoría: deja de dirigir todos los equipos de esa categoría.
- Se retira a un jugador que tiene la aplicación abierta: en su siguiente acción deja de ver el
  club.
- Se retira al último jugador de un equipo o de una categoría: el equipo y la categoría siguen
  existiendo, vacíos.
- Se retira dos veces al mismo jugador, o dos presidentes lo retiran a la vez: se retira una sola
  vez.
- Un jugador retirado vuelve años después: el PRESIDENTE lo reincorpora y queda en la categoría de
  su año, si sigue activa.
- Un jugador del club, retirado o no, acepta una invitación de PRESIDENTE: pasa a PRESIDENTE y
  sale de su categoría y de sus equipos.
- El club está suspendido: su PRESIDENTE sigue gestionando las categorías; los demás no entran.
- El club está dado de baja: nadie consulta ni gestiona sus categorías.
- Una persona pertenece a dos clubes: su categoría en uno no tiene relación con la del otro.

## Requisitos *(obligatorio)*

### Requisitos funcionales

**Categorías**

- **RF-001**: El PRESIDENTE de un club DEBE poder crear categorías en su club. Una categoría se
  define por un único año de nacimiento, y ese año es su nombre. Una categoría NO DEBE cubrir
  varios años.
- **RF-002**: El año de una categoría DEBE ser un año de cuatro cifras no posterior al año en
  curso. El sistema DEBE rechazar, explicando el motivo, cualquier otro valor.
- **RF-003**: En un club NO DEBE existir más de una categoría por año de nacimiento, contando las
  inactivas. Clubes distintos pueden tener cada uno la categoría del mismo año.
- **RF-004**: El PRESIDENTE DEBE poder desactivar una categoría, con confirmación previa, y
  reactivarla. Una categoría NO DEBE poder cambiar de año.
- **RF-004a**: El PRESIDENTE DEBE poder borrar, con confirmación previa, una categoría que nunca
  ha tenido jugadores ni entrenadores; sus equipos se borran con ella. Una categoría que tiene o
  tuvo alguna vez jugadores o entrenadores NO DEBE poder borrarse: solo se desactiva.
- **RF-005**: El sistema NO DEBE permitir desactivar una categoría que tiene jugadores.
- **RF-006**: Al desactivar una categoría, sus entrenadores DEBEN dejar de estar asignados a ella.
  Reactivarla no los vuelve a asignar.
- **RF-007**: Una categoría inactiva NO DEBE admitir jugadores ni entrenadores, ni por la
  ubicación automática ni a mano.

**Ubicación automática de los jugadores**

- **RF-008**: Al aprobarse el ingreso de una persona como JUGADOR, el sistema DEBE ubicarla en la
  categoría activa de su año de nacimiento, si el club la tiene.
- **RF-009**: Si el club no tiene activa la categoría de ese año, el ingreso DEBE aprobarse igual
  y el jugador DEBE quedar sin categoría.
- **RF-010**: Al crear o reactivar una categoría, el sistema DEBE ubicar en ella a todos los
  jugadores aprobados de ese club que no tienen categoría y nacieron ese año, e informar al
  PRESIDENTE de cuántos entraron.
- **RF-011**: La ubicación automática DEBE aplicarse solo a jugadores sin categoría. NO DEBE mover
  a un jugador que ya tiene una.
- **RF-012**: Solo los integrantes aprobados con el rol JUGADOR tienen categoría. Una persona en
  espera, un ENTRENADOR, un DIRECTIVO y un PRESIDENTE NO DEBEN tener categoría ni aparecer en la
  lista "Sin categoría".
- **RF-013**: Un jugador DEBE pertenecer, como máximo, a una categoría de su club en cada momento.

**Ubicación y cambio a mano**

- **RF-014**: El PRESIDENTE DEBE poder ubicar a un jugador sin categoría en cualquier categoría
  activa de su club, y pasar a un jugador de una categoría a otra con confirmación previa, sea o
  no la de su año de nacimiento.
- **RF-015**: Cambiar de categoría NO DEBE crear otro jugador ni modificar ningún otro dato suyo.
- **RF-016**: El sistema DEBE señalar, en las listas de jugadores, a quien está en una categoría
  distinta a la de su año de nacimiento.

**Entrenadores de la categoría**

- **RF-017**: El PRESIDENTE DEBE poder asignar a una categoría activa a cualquier integrante
  aprobado de su club con el rol ENTRENADOR, DIRECTIVO o PRESIDENTE, incluido él mismo, y a nadie
  más. El
  sistema DEBE comprobarlo en el servidor.
- **RF-017a**: Un PRESIDENTE o un DIRECTIVO asignado como entrenador de una categoría DEBE
  conservar su rol como único rol en el club, con el mismo alcance que tenía: ni gana ni pierde
  nada. La asignación solo lo muestra como entrenador de esa categoría.
- **RF-018**: Una categoría DEBE poder tener varios entrenadores, y un entrenador DEBE poder estar
  asignado a varias categorías.
- **RF-019**: El PRESIDENTE DEBE poder retirar a un entrenador de una categoría, con confirmación
  previa. Retirarlo NO DEBE afectar a sus demás categorías ni a su rol.
- **RF-020**: Asignar dos veces el mismo entrenador a la misma categoría NO DEBE producir ningún
  efecto adicional.
- **RF-021**: Una asignación retirada NO DEBE borrarse: deja de estar activa y el sistema conserva
  que existió.

**Equipos de la categoría**

- **RF-022**: El PRESIDENTE DEBE poder crear equipos dentro de una categoría activa de su club,
  cada uno con un nombre. Una categoría puede no tener ningún equipo.
- **RF-023**: El nombre de un equipo es obligatorio, de 30 caracteres como máximo, y NO DEBE
  repetirse dentro de la misma categoría, sin distinguir mayúsculas de minúsculas. Categorías
  distintas pueden tener equipos con el mismo nombre.
- **RF-024**: El PRESIDENTE DEBE poder cambiar el nombre de un equipo y desactivarlo, con
  confirmación previa. Un equipo NO DEBE poder pasar a otra categoría.
- **RF-024a**: El PRESIDENTE DEBE poder borrar, con confirmación previa, un equipo que nunca ha
  tenido jugadores ni entrenadores. Un equipo que los tiene o los tuvo alguna vez NO DEBE poder
  borrarse: solo se desactiva.
- **RF-025**: El PRESIDENTE DEBE poder poner a un jugador en uno o varios equipos de su categoría
  y sacarlo de cualquiera de ellos. Nadie más DEBE poder hacerlo, tampoco los entrenadores de esa
  categoría. Un jugador NO DEBE poder estar en un equipo de una categoría que no es la suya.
- **RF-026**: Estar en un equipo es opcional: un jugador de una categoría con equipos puede no
  estar en ninguno. La ubicación automática NO DEBE poner a nadie en un equipo.
- **RF-027**: Al cambiar de categoría, el jugador DEBE salir de todos los equipos de la categoría
  anterior.
- **RF-028**: El PRESIDENTE DEBE poder indicar qué equipos de una categoría dirige cada entrenador
  asignado a ella, que pueden ser varios o ninguno. Solo quien está asignado a la categoría puede
  dirigir uno de sus equipos.
- **RF-029**: Dirigir un equipo NO DEBE cambiar lo que ve el entrenador: ve a todos los jugadores
  de las categorías que tiene asignadas, de cualquier equipo.
- **RF-030**: Al desactivar un equipo, sus jugadores DEBEN seguir en la categoría y sus
  entrenadores DEBEN seguir asignados a ella. Al retirar a un entrenador de una categoría, deja de
  dirigir sus equipos. Al desactivar una categoría, sus equipos dejan de mostrarse con ella.

**Consulta**

- **RF-031**: El PRESIDENTE y los DIRECTIVOS DEBEN poder ver todas las categorías de su club,
  activas e inactivas, ordenadas por año, con sus equipos, sus entrenadores, su número de
  jugadores y la lista de sus jugadores, y la lista "Sin categoría".
- **RF-032**: Las listas de jugadores DEBEN mostrar nombre, apellidos, año de nacimiento y equipos
  de cada jugador, y nada más.
- **RF-033**: El DIRECTIVO NO DEBE poder crear, desactivar ni reactivar categorías, gestionar
  equipos, asignar ni retirar entrenadores, ni ubicar o cambiar de categoría o de equipo a un
  jugador.
- **RF-034**: El ENTRENADOR DEBE poder ver únicamente las categorías activas que tiene asignadas,
  con sus equipos y sus jugadores. NO DEBE ver las demás categorías, sus jugadores ni la lista
  "Sin categoría".
- **RF-035**: El JUGADOR DEBE ver en el inicio de su club la categoría de su jugador, los equipos
  en los que está y el nombre y los apellidos de los entrenadores de esa categoría, indicando qué
  equipo dirige cada uno; o que todavía no tiene categoría. NO DEBE ver ningún otro dato de los
  entrenadores, las demás categorías ni los jugadores de ninguna categoría o equipo.
- **RF-036**: Todos estos límites DEBEN aplicarse en el servidor a todas las operaciones; ocultar
  opciones en pantalla no cuenta como protección.

**Retiro de jugadores**

- **RF-041**: El PRESIDENTE DEBE poder retirar del club a un jugador aprobado, con confirmación
  previa. Nadie más DEBE poder hacerlo, y el retiro NO DEBE aplicarse a un ENTRENADOR, un
  DIRECTIVO, un PRESIDENTE ni a una persona en espera.
- **RF-042**: Al retirarse, el jugador DEBE salir de su categoría y de todos sus equipos y dejar
  de aparecer en las listas de jugadores y en "Sin categoría". Sus datos NO DEBEN borrarse. La
  lista "Ingresos aprobados" de la spec 002 no es una lista de jugadores: es el registro de las
  aprobaciones y sigue mostrando a quien después fue retirado.
- **RF-043**: Un jugador retirado NO DEBE acceder a ninguna información de ese club. Al entrar a
  él DEBE ver únicamente un aviso de que ya no está en el club. El retiro es de cada club: sigue
  usando con normalidad los demás clubes a los que pertenece. El cambio DEBE aplicarse también a
  una sesión ya abierta.
- **RF-044**: El PRESIDENTE y los DIRECTIVOS DEBEN poder ver la lista de jugadores retirados de su
  club, con nombre, apellidos, año de nacimiento, quién lo retiró y cuándo. Ningún otro rol DEBE
  poder verla.
- **RF-045**: El PRESIDENTE DEBE poder reincorporar a un jugador retirado. Al reincorporarse
  vuelve a entrar con su misma cuenta y sus mismos datos, y se le ubica como a un ingreso recién
  aprobado: en la categoría activa de su año o sin categoría. No recupera sus equipos.
- **RF-046**: El correo y el documento de un jugador retirado siguen ocupados en ese club. El
  sistema DEBE rechazar una invitación o un registro nuevo con ellos, explicando que la persona
  está retirada. La única excepción es la invitación de PRESIDENTE que envía el DESARROLLADOR:
  quien la acepta queda como PRESIDENTE activo de ese club, sin categoría ni equipos, y deja de
  estar retirado.
- **RF-047**: Retirar dos veces al mismo jugador NO DEBE producir ningún efecto adicional.

**Aislamiento y generales**

- **RF-037**: Las categorías, sus equipos, sus entrenadores y sus jugadores DEBEN ser invisibles e
  inaccesibles para cualquier persona de otro club, también por identificador.
- **RF-038**: El DESARROLLADOR NO DEBE poder ver ni gestionar las categorías ni los equipos de
  ningún club.
- **RF-039**: En un club suspendido, su PRESIDENTE DEBE poder seguir gestionando las categorías y
  los equipos. En un club dado de baja nadie los consulta ni los gestiona.
- **RF-040**: Todas las pantallas de esta funcionalidad DEBEN funcionar en teléfono y en
  escritorio, en tema claro y oscuro, con la identidad del club y en español.

### Entidades clave

- **Categoría**: grupo de jugadores de un club definido por un año de nacimiento. Guarda el club,
  el año y si está activa. Es única por club y año. Se desactiva; solo se borra si nunca tuvo
  jugadores ni entrenadores.
- **Asignación de entrenador a categoría**: vínculo entre un integrante con el rol ENTRENADOR,
  DIRECTIVO o PRESIDENTE y una categoría de su mismo club. Guarda si está activa y qué equipos de esa
  categoría dirige. Es lo que determina qué categorías ve un entrenador.
- **Equipo**: división de una categoría, con un nombre único dentro de ella ("A", "B", "Élite").
  Pertenece a una sola categoría. Se desactiva; solo se borra si nunca tuvo jugadores ni
  entrenadores.
- **Jugador**: integrante aprobado de un club con el rol JUGADOR. Gana su categoría actual, que es
  una sola o ninguna, y los equipos de esa categoría en los que juega, que pueden ser varios o
  ninguno. No se guarda historial de las categorías ni de los equipos por los que pasó.
- **Retiro**: quién retiró a un jugador del club y cuándo. Un jugador retirado conserva sus datos,
  no tiene categoría ni equipos y no entra a ese club hasta que se le reincorpora.
- **Sin categoría**: conjunto de jugadores aprobados y no retirados de un club que todavía no
  tienen categoría. No es una categoría.

## Criterios de éxito *(obligatorio)*

### Resultados medibles

Los criterios de tiempo de uso (CE-001, CE-002, CE-005, CE-010 y CE-013) los retiró el
propietario el 2026-10-08. Los demás conservan su número.

- **CE-003**: En el 100 % de los ingresos aprobados como jugador cuya categoría de año existe y
  está activa, el jugador queda en ella sin ninguna acción adicional.
- **CE-004**: En el 100 % de los casos probados, al crear una categoría entran en ella todos los
  jugadores sin categoría nacidos ese año, y ninguno más.
- **CE-006**: En el 100 % de los intentos probados, solo el PRESIDENTE del club consigue crear,
  desactivar, reactivar o borrar categorías y equipos, asignar o retirar entrenadores y ubicar
  jugadores.
- **CE-007**: En el 100 % de los intentos probados, un ENTRENADOR no obtiene ningún dato de una
  categoría que no tiene asignada, ni siquiera usando directamente identificadores.
- **CE-008**: En el 100 % de los intentos probados, nadie obtiene datos de las categorías de otro
  club.
- **CE-009**: Ningún jugador aparece en dos categorías a la vez en ninguna de las pruebas.
- **CE-011**: En el 100 % de los casos probados, la familia de un jugador con categoría ve el
  nombre de todos los entrenadores de esa categoría, y ningún otro dato de ellos.
- **CE-012**: En el 100 % de los intentos probados, un PRESIDENTE o un DIRECTIVO asignado como
  entrenador conserva exactamente lo que podía hacer con su rol.
- **CE-014**: En el 100 % de los intentos probados, un jugador retirado no obtiene ningún dato del
  club, ni siquiera usando directamente identificadores, y no se pierde ningún dato suyo.
- **CE-015**: Todas las pantallas de la funcionalidad se usan sin desplazamiento horizontal en un
  teléfono y el texto se lee bien en ambos temas.

## Supuestos

- La descripción menciona al DIRECTIVO como gestor, pero el propietario confirmó que solo
  consulta. Se mantiene la constitución (§8 y §11.2): gestiona el PRESIDENTE.
- Una categoría es un año de nacimiento y nada más (constitución §11). No lleva nombre libre,
  horario, sede ni cupo. Lo que la divide son sus equipos, que sí llevan nombre.
- Un PRESIDENTE o un DIRECTIVO que también entrena no cambia de rol ni recibe un segundo rol:
  sigue teniendo un único rol en el club (constitución §8) y solo queda asignado a la categoría.
  Lo que un DIRECTIVO asignado pueda hacer como entrenador (programar entrenamientos, convocar) lo
  decidirán esas funcionalidades.
- Un entrenador ve toda la categoría aunque dirija un solo equipo, así que el aislamiento sigue
  siendo por categoría (constitución §7.5).
- Los equipos son una forma de organizar a los jugadores de una categoría. Cómo se usan en
  entrenamientos, partidos y convocatorias lo decidirán esas funcionalidades.
- El valor de la mensualidad lo define el PRESIDENTE de cada club (constitución §7.2 y §16.7) y
  llega con la funcionalidad de mensualidades. Esta funcionalidad no lo hace depender de la
  categoría ni del equipo.
- Con esta funcionalidad se construye la parte de categorías de la §12.1.1: asignar la categoría
  al aprobar a un jugador. La parte de mensualidades sigue pendiente de su funcionalidad.
- El PRESIDENTE puede ubicar a un jugador en una categoría que no es la de su año, porque la
  constitución (§11.2) trata el cambio de categoría como una decisión suya y no lo limita.
- No hace falta dejar a un jugador sin categoría una vez que tiene una: solo se le pasa a otra.
- La ficha completa del Jugador (datos médicos, documentos) pertenece a la funcionalidad de
  jugadores. Aquí el jugador es el integrante aprobado con el rol JUGADOR y solo gana su
  categoría.
- Retirar a un jugador no borra nada (constitución §14): el club conserva sus datos y, cuando
  existan, sus pagos y su historial deportivo. Borrar del todo sigue siendo solo para el ingreso
  rechazado y para la cuenta que su dueño pide eliminar.
- Cuando existan las mensualidades, a un jugador retirado no se le generan cargos, porque solo se
  generan para jugadores activos (constitución §16.7).
- No se envía ningún correo ni aviso al jugador cuando se le retira o se le reincorpora.
- Todavía no existe el retiro de roles ni la baja de entrenadores o directivos. Cuando existan,
  esas funcionalidades decidirán qué pasa con las asignaciones de un entrenador que deja de serlo.
- Las categorías no avanzan ni cambian solas con el paso de los años: un jugador nacido en 2014
  está en la categoría 2014 todos los años.
- No se envía ningún correo ni aviso al asignar un entrenador o al cambiar a un jugador de
  categoría: cada quien lo ve al entrar.
- Las categorías se crean de una en una; no hay creación masiva por rango de años.
- "El año en curso" es el de la fecha del servidor (UTC). Solo difiere de la hora de Colombia
  durante las últimas cinco horas del 31 de diciembre.
- Volver a asignar a un entrenador que ya estuvo en una categoría recupera su asignación anterior;
  vuelve sin equipos.
- Un equipo desactivado no se puede reactivar ni se muestra, y su nombre queda libre para crear
  otro en la misma categoría.
- Un ENTRENADOR que pide una categoría que no tiene asignada recibe la misma respuesta que si no
  existiera.
- Al reincorporar a un jugador no se conserva quién lo retiró ni cuándo.
- Un jugador retirado de un club suspendido ve el aviso de incidencia temporal, no el de retiro.
- Si el DESARROLLADOR elimina del club a un presidente que entrenaba, sus asignaciones se borran
  con él y la categoría sigue contando como usada.

## Fuera de alcance

- Mensualidades y cargos, incluido empezar a generar la mensualidad al aprobar a un jugador.
- Ficha del jugador, datos médicos y documentos.
- Entrenamientos, partidos, convocatorias y actividades conjuntas de varias categorías.
- Búsqueda de usuarios por documento y asignación o retiro de roles a personas ya aprobadas.
- Baja de un entrenador, un directivo o un presidente del club.
- Borrar del todo a un jugador retirado.
- Historial de las categorías y de los equipos por los que pasó un jugador.
- Mostrar a las familias datos de contacto de los entrenadores.
- Mostrar las categorías y el cuerpo técnico en el sitio público.
- Agregar un hermano desde la ficha de un jugador ya registrado.

---

**Nota de seguimiento (2026-10-20)**: lo que este documento deja pendiente sobre mensualidades, cargos,
pagos y el estado de cuenta lo construye la funcionalidad
[015-mensualidades-cargos-pagos](../015-mensualidades-cargos-pagos/spec.md). Este documento no se reescribe.
