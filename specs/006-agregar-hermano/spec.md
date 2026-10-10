# Especificación de funcionalidad: Agregar un hermano y elegir el jugador

**Rama de la funcionalidad**: `006-agregar-hermano`

**Creada**: 2026-10-09

**Estado**: Borrador

**Entrada**: Descripción del propietario del proyecto: "El responsable puede agregar un hermano
desde la ficha de un jugador ya registrado y elegir con cuál jugador continuar al entrar", elegida
entre las funcionalidades que la spec 005 dejó fuera de alcance.

**Plataforma**: La Pecosa

**Constitución aplicable**: versión 4.1.0, en especial §7.1, §7.3, §8 (Jugador), §10, §12.1,
§12.1.1, §12.1.2, §12.3, §12.4, §14.1, §15 y §20.

**Depende de**: spec 001 (base multiclub), spec 002 y spec 004 (sala de espera, aprobación y
rechazo por el PRESIDENTE), spec 003 (categorías del club) y spec 005 (ficha del jugador). La sala
de espera ya existe y hoy no recibe a nadie; esta funcionalidad le da su único ingreso, el hermano
agregado desde la ficha, y hace que una cuenta pueda tener varios jugadores.

## Aclaraciones

### Sesión del 2026-10-09

- P: ¿Se admite el documento de un hermano que ya usa otra cuenta en otro club? → R: Sí. El
  hermano se agrega al club de su hermano y se le da de baja automáticamente del otro club.
- P: ¿En qué momento se le da de baja del otro club? → R: Cuando el PRESIDENTE lo aprueba aquí.
  Mientras está en espera sigue como estaba en el otro club, y si lo rechazan no pierde nada.
- P: ¿Qué significa darlo de baja? → R: Queda retirado en el otro club, con el retiro que ya
  existe: conserva allí su ficha y su historial.
- P: El documento queda en dos cuentas. ¿Cuál abre quien entra con ese documento? → R: La cuenta
  nueva, donde el jugador está activo.

### Sesión del 2026-10-10

- P: Si el PRESIDENTE del otro club quiere reincorporar al jugador que se retiró de allí al
  aprobarse aquí su hermano, ¿qué pasa? → R: Se niega mientras ese documento esté activo en otra
  cuenta, con un motivo que no nombra el club ni la cuenta.
- P: ¿Quién puede retirarlo del club donde quedó activo para que el anterior lo reincorpore? → R:
  El PRESIDENTE de ese club o la familia, con una opción nueva "Retirar del club" en la ficha del
  jugador. Así la familia no depende de que un PRESIDENTE lo libere.

## Escenarios de usuario y pruebas *(obligatorio)*

### Historia de usuario 1 - La familia agrega un hermano desde la ficha (Prioridad: P1)

La familia ya tiene un hijo en el club y quiere inscribir a otro. Entra con la cuenta que ya
tiene, abre "Mi ficha" y elige "Agregar un hermano". Escribe los nombres, los apellidos, el tipo y
el número de documento y la fecha de nacimiento del nuevo jugador. No vuelve a escribir el correo,
el celular, el responsable ni la contraseña: son los de la cuenta. El nuevo jugador queda en la
sala de espera del club y la familia ve que su ingreso está pendiente de aprobación.

**Por qué esta prioridad**: es la única forma de que un segundo hijo entre al club, porque el
correo de la familia ya está registrado y no admite otra cuenta. Sin esta historia no hay nada que
elegir ni que aprobar.

**Prueba independiente**: se inicia sesión con la cuenta de un jugador aprobado, se agrega un
hermano con un documento nuevo, y se comprueba que la familia lo ve como pendiente y que aparece
en la sala de espera del PRESIDENTE.

**Escenarios de aceptación**:

1. **Dado** la cuenta de un jugador aprobado y activo, **cuando** abre "Mi ficha", **entonces** ve
   la opción "Agregar un hermano".
2. **Dado** la familia en "Agregar un hermano", **cuando** escribe nombres, apellidos, tipo y
   número de documento y fecha de nacimiento válidos y confirma, **entonces** se crea un jugador
   nuevo de esa misma cuenta, en el mismo club, en estado de espera, y la familia ve la
   confirmación de que su ingreso está pendiente de aprobación.
3. **Dado** un hermano recién agregado, **cuando** se consultan sus datos de contacto,
   **entonces** el correo, el celular y el nombre del responsable son los de la cuenta, sin que la
   familia los haya escrito de nuevo.
4. **Dado** un hermano recién agregado, **cuando** se revisa el club, **entonces** no tiene
   categoría ni equipos y no aparece en ninguna lista de jugadores del club; solo aparece en la
   sala de espera.
5. **Dado** un número de documento que ya tiene otro integrante del mismo club, activo, retirado o
   en espera, **cuando** la familia intenta agregar un hermano con ese número, **entonces** el
   sistema no lo crea y explica que ese documento ya está registrado en el club.
6. **Dado** la familia en "Agregar un hermano", **cuando** deja vacío un dato obligatorio o
   escribe una fecha de nacimiento futura, **entonces** el sistema no lo crea y explica el motivo.
7. **Dado** una cuenta sin nombre de responsable (jugador adulto), **cuando** agrega un hermano
   menor de 18 años, **entonces** el sistema le pide el nombre del responsable antes de crearlo.
8. **Dado** un jugador retirado o en espera, **cuando** su familia lo tiene elegido, **entonces**
   no ve la opción de agregar un hermano desde él.
9. **Dado** un PRESIDENTE, un DIRECTIVO o un ENTRENADOR, **cuando** intenta agregar un hermano,
   incluso llamando directamente a la operación, **entonces** el sistema se lo niega.
10. **Dado** la familia que confirma dos veces seguidas el mismo hermano, **cuando** el sistema
    procesa las dos solicitudes, **entonces** queda un solo jugador nuevo.
11. **Dado** un número de documento que tiene un jugador de otra cuenta en otro club, **cuando**
    la familia agrega un hermano con ese número, **entonces** el sistema lo crea en espera, le
    avisa de que al aprobarse dejará de estar en el otro club, y ese jugador sigue en el otro club
    como estaba.
12. **Dado** un número de documento que tiene, en otro club y con otra cuenta, un integrante que
    no es JUGADOR, **cuando** la familia intenta agregar un hermano con ese número, **entonces**
    el sistema no lo crea y explica que ese documento ya está registrado con otra cuenta.

---

### Historia de usuario 2 - La familia elige con cuál jugador continuar (Prioridad: P1)

Cuando la cuenta tiene más de un jugador en el club y la familia entra con el correo, la
aplicación le muestra sus jugadores y le pide elegir con cuál continuar. Desde ese momento todo lo
que ve es de ese jugador. Puede cambiar a otro de sus jugadores sin cerrar sesión. Si entra con el
documento de uno de ellos, ve solo a ese jugador.

**Por qué esta prioridad**: en cada momento se ve la información de un único jugador
(constitución §8). Sin elegir, la familia no podría llegar al segundo hijo ni saber de cuál de los
dos es lo que tiene en pantalla.

**Prueba independiente**: con una cuenta que tiene dos jugadores aprobados se inicia sesión con el
correo, se elige al segundo y se comprueba que "Mi ficha" es la suya; se cambia al primero y se
comprueba que la ficha cambia; se inicia sesión con el documento del segundo y se comprueba que no
se ofrece cambiar.

**Escenarios de aceptación**:

1. **Dado** una cuenta con dos o más jugadores en el club, **cuando** inicia sesión con el correo,
   **entonces** antes de cualquier otra pantalla ve la lista de sus jugadores, con nombres,
   apellidos y estado de cada uno (activo, pendiente de aprobación o retirado), y debe elegir uno.
2. **Dado** la familia en la lista de jugadores, **cuando** elige uno, **entonces** la aplicación
   muestra el nombre del jugador elegido de forma visible y "Mi ficha" y el resto de pantallas son
   las de ese jugador.
3. **Dado** una cuenta con un solo jugador en el club, **cuando** inicia sesión con el correo,
   **entonces** entra directamente con ese jugador, sin pantalla de elección.
4. **Dado** la familia con un jugador elegido tras entrar con el correo, **cuando** usa "Cambiar
   de jugador", **entonces** vuelve a ver la lista y puede elegir otro sin cerrar sesión.
5. **Dado** una cuenta con varios jugadores, **cuando** inicia sesión con el documento de uno de
   ellos y la contraseña de la cuenta, **entonces** entra directamente con ese jugador, no ve la
   lista ni la opción de cambiar de jugador.
6. **Dado** una sesión iniciada con el documento de un jugador, **cuando** intenta ver o cambiar
   datos de un hermano, incluso llamando directamente a la operación, **entonces** el sistema
   responde como si ese jugador no existiera.
7. **Dado** la familia con un jugador elegido, **cuando** intenta ver o cambiar la ficha o los
   archivos de otro de sus jugadores sin cambiar de jugador, **entonces** el sistema no se los
   entrega.
8. **Dado** una cuenta, **cuando** intenta elegir un jugador que no es suyo, aunque conozca su
   identificador, **entonces** el sistema responde como si ese jugador no existiera.
9. **Dado** la familia que elige un jugador en espera, **cuando** continúa, **entonces** solo ve
   la pantalla de ingreso pendiente de aprobación y la opción de cambiar de jugador.
10. **Dado** la familia que elige un jugador retirado, **cuando** continúa, **entonces** solo ve
    el aviso de que ya no está en el club y la opción de cambiar de jugador.
11. **Dado** la familia que cierra sesión y vuelve a entrar con el correo, **cuando** entra,
    **entonces** vuelve a ver la lista y elige de nuevo.
12. **Dado** un documento que está en dos cuentas, retirado en una y activo en la otra, **cuando**
    alguien inicia sesión con ese documento, **entonces** solo entra con la contraseña de la
    cuenta donde el jugador está activo.
13. **Dado** un documento que está activo en una cuenta y en espera en otra, **cuando** alguien
    inicia sesión con ese documento, **entonces** solo entra con la contraseña de la cuenta donde
    está activo; la otra familia llega al hermano en espera entrando con el correo.

---

### Historia de usuario 3 - El PRESIDENTE aprueba o rechaza al hermano (Prioridad: P2)

El PRESIDENTE abre la sala de espera y ve al hermano agregado: sus datos, quién es el responsable
y de qué jugador del club es hermano. Si lo aprueba, el jugador queda en la categoría de su año de
nacimiento y la familia ya puede usar su ficha. Si lo rechaza, ese jugador desaparece del club y
la cuenta y sus demás jugadores quedan como estaban.

**Por qué esta prioridad**: la sala de espera, aprobar y rechazar ya existen desde las specs 002 y
004. Aquí solo se comprueba que funcionan con un jugador que comparte cuenta con otro y que el
club ve de dónde viene.

**Prueba independiente**: con un hermano en espera, el PRESIDENTE lo aprueba y se comprueba que
queda en la categoría de su año y que la familia ve su ficha; con otro hermano en espera, lo
rechaza y se comprueba que desaparece de la lista de la familia y que el primer hijo sigue igual.

**Escenarios de aceptación**:

1. **Dado** un hermano en espera, **cuando** el PRESIDENTE abre la sala de espera, **entonces** lo
   ve con sus nombres, apellidos, documento, fecha de nacimiento, el correo, el celular y el
   responsable de la cuenta, y el nombre del jugador del club desde cuya ficha se agregó.
2. **Dado** un hermano en espera, **cuando** el PRESIDENTE lo aprueba, **entonces** queda aprobado
   con el rol JUGADOR, en la categoría activa de su año de nacimiento o sin categoría si no
   existe, sin equipo, y sale de la sala de espera.
3. **Dado** un hermano recién aprobado, **cuando** la familia lo elige, **entonces** ve la
   aplicación de su club con ese jugador y su ficha propia, con el contacto de emergencia, la
   seguridad social, los datos clínicos y los documentos vacíos.
4. **Dado** un hermano en espera, **cuando** el PRESIDENTE lo rechaza, **entonces** ese jugador se
   borra del club sin dejar datos suyos, y la cuenta, su contraseña y sus demás jugadores quedan
   intactos.
5. **Dado** una familia cuyo hermano fue rechazado y a la que le queda un solo jugador, **cuando**
   entra con el correo, **entonces** entra directamente con ese jugador, sin lista.
6. **Dado** un hermano rechazado, **cuando** la familia vuelve a agregarlo con el mismo documento,
   **entonces** el sistema lo acepta y queda de nuevo en espera.
7. **Dado** un DIRECTIVO, un ENTRENADOR o un JUGADOR, **cuando** intenta ver, aprobar o rechazar a
   un hermano en espera, **entonces** el sistema se lo niega.
8. **Dado** la familia con el hermano elegido y en la pantalla de pendiente, **cuando** el
   PRESIDENTE lo aprueba y la familia vuelve a cargar, **entonces** ya ve la aplicación con ese
   jugador.
9. **Dado** un hermano en espera cuyo documento tiene un jugador activo de otra cuenta en otro
   club, **cuando** el PRESIDENTE abre la sala de espera, **entonces** ve el aviso de que ese
   documento está activo en otro club y de que aprobarlo lo retirará de allí, sin el nombre del
   otro club ni datos de la otra cuenta.
10. **Dado** ese mismo hermano, **cuando** el PRESIDENTE lo aprueba, **entonces** queda aprobado
    aquí y, en la misma operación, el jugador de la otra cuenta queda retirado en el otro club,
    con su ficha y su historial intactos.
11. **Dado** ese mismo hermano, **cuando** el PRESIDENTE lo rechaza, **entonces** se borra de este
    club y el jugador de la otra cuenta sigue activo en el otro club, sin ningún cambio.
12. **Dado** un jugador retirado del otro club por esta vía, **cuando** su familia anterior entra
    con el correo y lo elige, **entonces** ve el aviso de que ya no está en ese club.
13. **Dado** un jugador retirado del otro club por esta vía, **cuando** el PRESIDENTE de ese club
    intenta reincorporarlo mientras el documento sigue activo en la cuenta nueva, **entonces** el
    sistema se lo niega con un motivo que no nombra el club ni la cuenta donde está activo.
14. **Dado** la familia con un jugador aprobado y activo elegido, **cuando** usa "Retirar del
    club" en su ficha y lo confirma, **entonces** el jugador queda retirado de ese club con el
    retiro que ya existe y la familia ve el aviso de que ya no está en él.
15. **Dado** un jugador retirado por su familia que tenía el mismo documento retirado en otro club,
    **cuando** el PRESIDENTE de ese otro club lo reincorpora, **entonces** el sistema lo permite.
16. **Dado** un DIRECTIVO, un ENTRENADOR o la cuenta de otro jugador, **cuando** intenta retirar a
    un jugador ajeno, incluso llamando directamente a la operación, **entonces** el sistema se lo
    niega.

---

### Casos límite

- La familia agrega dos hermanos antes de que el club apruebe al primero: los dos quedan en
  espera, cada uno se aprueba o se rechaza por separado.
- Una cuenta pertenece a dos clubes: el hermano se agrega solo en el club de la ficha desde la que
  se agregó. La lista de jugadores muestra los del club que la familia tiene elegido; al cambiar
  de club en el desplegable, la elección de jugador se repite con los de ese club si son varios.
- El hermano ya es jugador de otro club con el mismo documento y la misma cuenta: se puede agregar
  aquí y sigue en los dos clubes, porque es el mismo niño inscrito por su familia en ambos.
- El hermano ya es jugador de otro club con el mismo documento y otra cuenta: se puede agregar
  aquí; al aprobarse queda retirado del otro club (RF-034 a RF-039).
- El documento tiene jugadores activos de otras cuentas en varios clubes: al aprobarse aquí queda
  retirado de todos ellos.
- El jugador de la otra cuenta ya estaba retirado en el otro club: se agrega y se aprueba aquí sin
  cambiar nada allí, y no hay aviso para el PRESIDENTE.
- El jugador de la otra cuenta se retira o vuelve a estar activo en el otro club mientras el
  hermano espera aquí: el aviso y la baja se deciden con lo que haya en el momento de abrir la
  sala de espera y en el de aprobar, no con lo que había al agregarlo.
- Dos familias de clubes distintos agregan en espera el mismo documento: cada club decide por
  separado; aprobar en uno no borra ni rechaza al que espera en el otro.
- El jugador retirado por esta vía era el único de su cuenta en el otro club: la cuenta anterior
  no se borra; sigue entrando con el correo y ve el aviso de retiro.
- La familia entra con el documento de un hermano que sigue en espera: ve solo la pantalla de
  pendiente.
- La familia entra con el documento de un hijo, agrega un hermano desde su ficha y sigue viendo
  solo a ese hijo; al hermano llega entrando con el correo o con el documento del hermano.
- El primer hijo se retira del club mientras el hermano sigue en espera o ya está aprobado: el
  hermano no cambia; el retiro es de un jugador, no de la cuenta.
- La familia cambia el celular o el responsable desde la ficha de un hijo: cambia para todos los
  jugadores de la cuenta, incluidos los que están en espera (spec 005, RF-039).
- El jugador retirado por esta vía vuelve a su club anterior: el PRESIDENTE de ese club no puede
  reincorporarlo mientras siga activo aquí; la familia o el PRESIDENTE de aquí lo retiran y
  entonces sí puede (RF-041 a RF-043).
- La familia retira por error a su único jugador del club: queda retirado como cualquier otro;
  solo el PRESIDENTE lo reincorpora.
- El club se suspende mientras hay un hermano en espera: la familia no entra a ese club (spec
  001); al reactivarse, el hermano sigue en espera.
- La familia elimina su cuenta: se van todos sus jugadores, también los que están en espera.
- El jugador elegido se retira o se rechaza mientras la familia lo tiene en pantalla: la siguiente
  acción muestra el aviso de retiro o devuelve a la lista de jugadores, sin mostrar datos de otro.
- La familia se equivoca al escribir los datos del hermano: mientras está en espera no puede
  corregirlos; el PRESIDENTE lo rechaza y la familia lo agrega de nuevo, o lo aprueba y corrige la
  identidad desde la ficha (spec 005).

## Requisitos *(obligatorio)*

### Requisitos funcionales

**Agregar un hermano**

- **RF-001**: La cuenta de un jugador aprobado y activo DEBE poder agregar, desde la ficha de ese
  jugador, un nuevo jugador a la misma cuenta y al mismo club.
- **RF-002**: Para agregarlo, el sistema DEBE pedir únicamente los nombres, los apellidos, el tipo
  y el número de documento y la fecha de nacimiento del nuevo jugador. NO DEBE pedir correo,
  celular ni contraseña.
- **RF-003**: El jugador agregado DEBE compartir el correo, el celular y el nombre del responsable
  de la cuenta, y DEBE entrar con la contraseña de la cuenta.
- **RF-004**: Si el nuevo jugador es menor de 18 años y la cuenta no tiene nombre de responsable,
  el sistema DEBE pedirlo y guardarlo en la cuenta antes de crear al jugador.
- **RF-005**: El sistema NO DEBE aceptar un número de documento que ya tenga otro integrante del
  mismo club, esté activo, retirado o en espera, y DEBE explicar el motivo.
- **RF-006**: La fecha de nacimiento NO DEBE poder ser futura y todos los datos de RF-002 DEBEN
  ser obligatorios.
- **RF-007**: Solo una cuenta con el rol JUGADOR en ese club DEBE poder agregar un hermano. Un
  PRESIDENTE, un DIRECTIVO, un ENTRENADOR y el DESARROLLADOR NO DEBEN poder hacerlo.
- **RF-008**: NO DEBE poder agregarse un hermano desde un jugador retirado ni desde uno en espera.
- **RF-009**: Una misma solicitud confirmada más de una vez DEBE crear un solo jugador.
- **RF-010**: El sistema NO DEBE limitar cuántos jugadores tiene una cuenta.

**El hermano en espera**

- **RF-011**: El jugador agregado DEBE quedar en estado de espera en ese club. Es el único ingreso
  que pasa por la sala de espera.
- **RF-012**: Mientras está en espera, el jugador NO DEBE tener categoría ni equipos, NO DEBE
  aparecer en las listas de jugadores del club y NO DEBE generar ningún cobro.
- **RF-013**: Mientras está en espera, quien lo elige DEBE ver solamente la pantalla de ingreso
  pendiente, sin ninguna información del club ni acceso a su ficha, y DEBE poder cambiar de
  jugador si entró con el correo.
- **RF-014**: Los demás jugadores de la cuenta DEBEN seguir funcionando con normalidad mientras un
  hermano está en espera.

**Aprobar y rechazar**

- **RF-015**: La sala de espera DEBE mostrar al PRESIDENTE, de cada hermano en espera, sus
  nombres, apellidos, documento y fecha de nacimiento, el correo, el celular y el responsable de
  la cuenta, y el nombre del jugador desde cuya ficha se agregó.
- **RF-016**: Aprobar a un hermano DEBE dejarlo aprobado con el rol JUGADOR y ubicarlo como a
  quien entra con una invitación de JUGADOR: en la categoría activa de su año de nacimiento, o sin
  categoría si no existe, y sin equipo.
- **RF-017**: Al aprobarse, el jugador DEBE tener su propia ficha, independiente de la de sus
  hermanos, con el contacto de emergencia, la seguridad social, los datos clínicos y los
  documentos vacíos.
- **RF-018**: Rechazar a un hermano DEBE borrar a ese jugador del club sin dejar datos suyos, y NO
  DEBE alterar la cuenta, su contraseña ni sus demás jugadores. No se envía ningún aviso.
- **RF-019**: Un documento rechazado DEBE poder volver a agregarse.
- **RF-020**: Solo el PRESIDENTE DEBE poder ver, aprobar y rechazar a los hermanos en espera, como
  ya ocurre con la sala de espera (spec 004, RF-017).

**Elegir con cuál jugador continuar**

- **RF-021**: Cuando una cuenta que entra con el correo tiene más de un jugador en el club que
  está viendo, el sistema DEBE pedirle que elija uno antes de mostrar cualquier otra pantalla.
- **RF-022**: La lista DEBE mostrar todos los jugadores de la cuenta en ese club, con nombres,
  apellidos y estado: activo, pendiente de aprobación o retirado.
- **RF-023**: Cuando la cuenta tiene un solo jugador en ese club, el sistema DEBE entrar
  directamente con él, sin pedir que elija.
- **RF-024**: En cada momento la aplicación DEBE mostrar la información de un único jugador y
  DEBE dejar a la vista cuál es.
- **RF-025**: Quien entró con el correo y tiene más de un jugador DEBE poder cambiar de jugador
  sin cerrar sesión.
- **RF-026**: Quien entra con el documento de un jugador DEBE ver solamente a ese jugador durante
  toda la sesión: no ve la lista, no puede cambiar de jugador ni recibe datos de sus hermanos.
- **RF-027**: La elección NO DEBE recordarse entre sesiones: cada vez que se entra con el correo
  se elige de nuevo.
- **RF-028**: Al cambiar de club en el desplegable, el sistema DEBE aplicar de nuevo RF-021 y
  RF-023 con los jugadores de la cuenta en ese club.

**Aislamiento**

- **RF-029**: Una cuenta DEBE acceder solamente a sus propios jugadores. Ante cualquier intento de
  elegir, ver o cambiar un jugador ajeno, el sistema DEBE responder como si no existiera.
- **RF-030**: Las operaciones de la ficha y de sus archivos (spec 005) DEBEN aplicarse al jugador
  elegido y a ningún otro, tampoco a un hermano de la misma cuenta.
- **RF-031**: Todas estas restricciones DEBEN aplicarse en el servidor y no solo ocultando
  elementos en pantalla.

**El documento que ya usa otra cuenta en otro club**

- **RF-034**: El sistema DEBE aceptar como hermano un número de documento que tenga un JUGADOR de
  otra cuenta en otro club, y DEBE avisar a la familia, al confirmar, de que al aprobarse ese
  jugador dejará de estar en el otro club.
- **RF-035**: Agregar a ese hermano NO DEBE cambiar nada en el otro club mientras esté en espera,
  ni si el PRESIDENTE lo rechaza.
- **RF-036**: Al aprobar a ese hermano, el sistema DEBE dejar retirado, en la misma operación, a
  todo jugador activo de otra cuenta con ese documento en cualquier otro club. Si la baja no puede
  hacerse, la aprobación tampoco DEBE hacerse.
- **RF-037**: Esa baja DEBE ser el retiro que ya existe: el jugador conserva en el otro club su
  ficha y su historial, y su cuenta anterior, su contraseña y sus demás jugadores NO DEBEN
  cambiar. No se envía ningún aviso al otro club ni a la otra familia.
- **RF-038**: La sala de espera DEBE avisar al PRESIDENTE, antes de que decida, cuando el
  documento del hermano tiene un jugador activo de otra cuenta en otro club y de que aprobarlo lo
  retirará de allí. El aviso NO DEBE mostrar el nombre del otro club ni ningún dato de la otra
  cuenta.
- **RF-039**: El sistema NO DEBE aceptar como hermano un documento que, con otra cuenta, tenga en
  cualquier club un integrante que no sea JUGADOR, y DEBE explicar el motivo.
- **RF-040**: Cuando un documento está en más de una cuenta, el inicio de sesión con ese documento
  DEBE abrir una sola: aquella donde el jugador está activo; si no está activo en ninguna, aquella
  donde está en espera. Con la contraseña de cualquier otra cuenta, el ingreso DEBE negarse como
  cualquier ingreso con datos incorrectos.
- **RF-041**: El sistema NO DEBE reincorporar a un jugador retirado mientras su documento esté
  activo con otra cuenta en otro club, y DEBE explicar el motivo sin nombrar ese club ni dar datos
  de esa cuenta. Si el documento solo está activo con la misma cuenta, la reincorporación sigue
  como hoy.

**La familia retira a su jugador**

- **RF-042**: La cuenta de un jugador aprobado y activo DEBE poder retirarlo del club desde su
  ficha, con la opción "Retirar del club", después de confirmarlo. Es el retiro que ya existe: el
  jugador conserva su ficha y su historial, y la cuenta y sus demás jugadores NO DEBEN cambiar.
- **RF-043**: Solo la cuenta del jugador y el PRESIDENTE de su club DEBEN poder retirarlo. La
  familia NO DEBE poder reincorporarlo; eso lo sigue haciendo solo el PRESIDENTE.

**Acceso desde la aplicación**

- **RF-032**: La opción de agregar un hermano DEBE estar en la ficha del jugador, visible solo
  para la cuenta de ese jugador.
- **RF-033**: Agregar un hermano, la lista de jugadores y el cambio de jugador DEBEN usarse en
  teléfono y en escritorio, y en tema claro y oscuro.

### Requisitos de specs anteriores que esta funcionalidad cambia

- **Spec 005, RF-005** ("la cuenta de un jugador consulta la ficha de su propio jugador"): una
  cuenta puede tener varios jugadores; consulta y cambia la del jugador elegido (RF-030).
- **Spec 005, supuesto sobre el jugador en espera**: queda resuelto por RF-013 y RF-017: no hay
  ficha accesible mientras está en espera y nace vacía al aprobarse.
- **Spec 004, RF-015**: la sala de espera vuelve a recibir personas, solo por RF-011.
- **Spec 001, inicio de sesión con documento**: un documento podía estar en una sola cuenta; ahora
  puede estar en varias y RF-040 decide cuál abre.
- **Retiro de un jugador**: hasta ahora solo lo hacía su propio club; RF-036 añade un retiro que
  provoca la aprobación en otro club, y RF-042 deja que la familia retire a su propio jugador.
- **Reincorporación de un jugador (spec 003)**: RF-041 la niega mientras el documento esté activo
  con otra cuenta en otro club.
- El registro con invitación (spec 004) y el cambio de documento desde la ficha (spec 005) NO
  cambian: siguen sin aceptar un documento que usa otra cuenta.

### Entidades clave

- **Cuenta**: el acceso de una familia: un correo, una contraseña, un celular y un responsable.
  Puede tener varios jugadores.
- **Jugador**: cada hijo inscrito en un club. Pertenece a una única cuenta y tiene su propio
  documento, su propio estado de ingreso, su propia categoría y su propia ficha.
- **Estado de ingreso**: EN_ESPERA o APROBADO. El hermano agregado nace EN_ESPERA; rechazarlo lo
  borra y no deja estado.
- **Jugador de origen**: el jugador desde cuya ficha se agregó al hermano. Sirve para que el
  PRESIDENTE sepa de quién es hermano mientras decide.
- **Documento compartido entre cuentas**: un mismo número de documento en jugadores de cuentas
  distintas y clubes distintos. Solo nace al agregar un hermano (RF-034) y, tras la aprobación,
  queda activo en una sola cuenta.
- **Jugador elegido**: el jugador con el que la familia continúa en una sesión. Vale solo para esa
  sesión.

## Criterios de éxito *(obligatorio)*

### Resultados medibles

- **CE-001**: En el 100 % de los hermanos agregados en las pruebas, el jugador queda en la sala de
  espera con los datos escritos y con el correo, el celular y el responsable de la cuenta, sin que
  la familia los escriba de nuevo.
- **CE-002**: En el 100 % de los intentos probados, no queda creado un jugador con un documento
  repetido en el club ni dos jugadores por una misma solicitud.
- **CE-003**: En el 100 % de los intentos probados, un jugador en espera no obtiene ninguna
  información del club ni aparece en ninguna lista de jugadores.
- **CE-004**: En el 100 % de las aprobaciones probadas, el hermano queda en la categoría activa de
  su año, o sin categoría si no existe, y la familia accede a su ficha.
- **CE-005**: En el 100 % de los rechazos probados, no queda ningún dato del hermano en el club y
  los demás jugadores de la cuenta conservan todos sus datos.
- **CE-006**: En el 100 % de las entradas con correo probadas, una cuenta con varios jugadores
  elige antes de ver cualquier dato, y una cuenta con uno solo entra sin elegir.
- **CE-007**: En el 100 % de las entradas con documento probadas, la sesión no obtiene ningún dato
  de un hermano.
- **CE-008**: En el 100 % de los intentos probados, una cuenta no obtiene ni cambia datos de un
  jugador que no es suyo, ni de un hermano distinto del elegido.
- **CE-009**: En el 100 % de los cambios de jugador probados, todo lo que se muestra después
  pertenece al jugador elegido.
- **CE-010**: Agregar un hermano, la lista de jugadores y el cambio de jugador se usan sin
  desplazamiento horizontal en un teléfono y el texto se lee bien en ambos temas.
- **CE-011**: En el 100 % de las aprobaciones probadas de un hermano cuyo documento estaba activo
  con otra cuenta en otro club, el jugador queda aprobado aquí y retirado allí; en el 100 % de los
  rechazos y de las esperas probadas, sigue activo allí sin ningún cambio.
- **CE-012**: En el 100 % de los ingresos probados con un documento que está en dos cuentas, se
  abre únicamente la cuenta donde el jugador está activo.
- **CE-013**: En el 100 % de los intentos probados, ningún documento queda activo en dos cuentas:
  la reincorporación se niega mientras esté activo en otra, y se permite cuando el PRESIDENTE o la
  familia lo retiran de allí.

## Supuestos

- Al agregar al hermano solo se piden los datos de identidad; el resto de la ficha (contacto de
  emergencia, salud y documentos) lo completa la familia después de la aprobación, porque en
  espera solo se ve la pantalla de pendiente (constitución §12.1.1).
- El hermano se agrega al club de la ficha desde la que se agregó. Para inscribirlo en otro club
  hace falta una ficha aprobada en ese otro club.
- La lista de jugadores muestra los del club elegido, no los de todos los clubes de la cuenta; el
  club se sigue eligiendo con el desplegable que ya existe.
- La familia no cancela ni corrige un hermano en espera; lo resuelve el PRESIDENTE aprobando o
  rechazando.
- No se envía ningún correo nuevo: ni al club cuando se agrega un hermano, ni a la familia cuando
  se aprueba o se rechaza (igual que en la spec 002).
- Los jugadores retirados siguen en la lista de la cuenta, con su estado, para que la familia vea
  el aviso de retiro.
- Un JUGADOR adulto agrega a otro jugador con las mismas reglas; "hermano" es el nombre de la
  opción, no una comprobación de parentesco.
- Todavía no existen cargos ni mensualidades; cuando existan, el hermano empezará a generarlos al
  aprobarse y no antes (constitución §12.1.2).
- La baja automática del otro club solo alcanza a jugadores. Si el documento es de un PRESIDENTE,
  un DIRECTIVO o un ENTRENADOR de otra cuenta, el hermano no se admite (RF-039): retirar a quien
  dirige o entrena en otro club no puede depender de una familia ajena.
- Nadie del otro club ni de la otra familia confirma la baja. El único control es la aprobación
  del PRESIDENTE de este club, que decide con el aviso de RF-038 a la vista.
- La ficha del otro club no viaja: el jugador nace aquí con su ficha vacía, como cualquier hermano
  (RF-017).
- Si el otro club quiere recuperarlo, primero debe retirarse del club donde quedó activo, por su
  PRESIDENTE o por su familia (RF-042); después el otro club lo reincorpora como a cualquier
  jugador retirado (RF-041). No hay solicitud ni aviso entre clubes.
- "Retirar del club" vale para cualquier jugador aprobado y activo de la cuenta, no solo para los
  que vinieron de otro club: es la misma baja que hoy hace el PRESIDENTE.
- Cuando el documento es de la misma cuenta en otro club no hay baja: la familia lo tiene inscrito
  en los dos clubes a propósito.

## Fuera de alcance

- Que un PRESIDENTE, un DIRECTIVO o un ENTRENADOR inscriba a un hijo desde su propia cuenta.
- Pasar un jugador de una cuenta a otra, o separar hermanos en cuentas distintas. La baja de
  RF-036 no traslada nada: el jugador de la otra cuenta queda retirado allí y aquí nace uno nuevo.
- Que el otro club o la otra familia autoricen, reciban aviso o deshagan la baja automática.
- Una solicitud a la familia para aceptar la reincorporación en otro club; se resuelve con RF-042.
- Que la familia reincorpore a un jugador retirado.
- Que la familia cancele, corrija o elimine a un hermano en espera.
- Avisos o correos al club o a la familia sobre el ingreso pendiente, la aprobación o el rechazo.
- Cambiar el correo de la cuenta.
- Búsqueda de usuarios por documento y cambio de roles (constitución §12.2).
- Estado de cuenta, cargos, pagos y mensualidades de cada jugador.
- Evaluaciones, estadísticas, asistencia, calendario y convocatorias.
