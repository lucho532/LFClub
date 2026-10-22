# Especificación de funcionalidad: Ingreso de personas al club

**Rama de la funcionalidad**: `002-ingreso-club`

**Creada**: 2026-10-07

**Estado**: Borrador

**Entrada**: Descripción del propietario del proyecto: "Ingreso de personas al club: invitación por
correo desde el club, registro por el enlace, sala de espera y aprobación del ingreso por el
PRESIDENTE o un DIRECTIVO"

**Plataforma**: LFClub

**Constitución aplicable**: versión 3.7.0, en especial §7.1, §7.3, §8, §10, §12.1, §12.1.1, §12.2
y §14.

**Depende de**: spec 001 (base multiclub), de la que reutiliza las invitaciones, el registro por
enlace, el inicio de sesión y la elección de club.

## Aclaraciones

### Sesión 2026-10-07

- P: ¿Qué roles del club pueden enviar invitaciones de registro? → R: El PRESIDENTE y los
  DIRECTIVOS, los mismos que aprueban los ingresos.
- P: ¿Qué pasa con una cuenta en espera que el club no quiere aceptar? → R: Se rechaza y se borra
  del club; la persona puede volver a registrarse si recibe una invitación nueva. Rechazan los
  mismos que aprueban.
- P: ¿Cuándo es obligatorio el nombre del padre, madre o responsable en el registro? → R: Cuando,
  por su fecha de nacimiento, la persona es menor de 18 años; para los adultos es opcional.
- P: Mientras un club está suspendido, ¿se puede seguir invitando y registrándose en él? → R: Sí.
  Su PRESIDENTE invita, aprueba y rechaza, y quien tiene un enlace válido se registra y queda en
  espera. Los DIRECTIVOS no pueden hacer nada porque no entran al club.
- P: Al rechazar a una persona, ¿qué pasa con la invitación que usó para registrarse? → R: Se
  borra junto con la persona; en la lista de invitaciones del club no queda rastro de ese correo.
- P: ¿Dónde ve el club quién aprobó cada ingreso, cuándo y con qué rol? → R: En una lista de
  ingresos aprobados, de solo lectura, dentro del apartado "Ingresos". La ven el PRESIDENTE y los
  DIRECTIVOS.
- P: Cuando el club rechaza a una persona, ¿se le avisa por correo? → R: No. No se envía ningún
  correo ni aviso; si su cuenta se borró, al intentar entrar ve el mensaje normal de datos
  incorrectos.

## Escenarios de usuario y pruebas *(obligatorio)*

### Historia de usuario 1 - El club invita a una persona por correo (Prioridad: P1)

Alguien autorizado del club entra al apartado "Ingresos" de su club, escribe el correo de la
persona que quiere invitar y envía la invitación. La plataforma manda a ese correo un enlace para
registrarse en ese club. En el mismo apartado ve la lista de invitaciones enviadas y su estado
(pendiente, usada, vencida), y puede reenviar o cancelar las que siguen pendientes.

**Por qué esta prioridad**: es la única puerta de entrada al club. Sin invitación nadie puede
registrarse, así que sin esta historia el club no puede tener directivos, entrenadores ni
jugadores.

**Prueba independiente**: se inicia sesión como PRESIDENTE de un club, se envía una invitación a
un correo y se comprueba que llega, que aparece como pendiente en la lista y que el enlace lleva
al registro de ese club.

**Escenarios de aceptación**:

1. **Dado** alguien autorizado a invitar en su club, **cuando** escribe un correo válido y envía
   la invitación, **entonces** la invitación aparece en la lista como pendiente, con el correo, la
   fecha de envío y la fecha en que vence, y se envía el correo con el enlace.
2. **Dado** alguien enviando una invitación, **cuando** el correo está vacío o no es válido,
   **entonces** el sistema no envía nada y le explica por qué.
3. **Dado** un correo que ya pertenece a un integrante de ese club, o a alguien que está en su
   sala de espera, **cuando** se intenta invitarlo, **entonces** el sistema no envía nada y le
   explica que esa persona ya está en el club o esperando aprobación.
4. **Dado** un correo con una invitación pendiente en ese club, **cuando** se le vuelve a invitar
   o se reenvía la invitación, **entonces** se envía una invitación nueva y la anterior deja de
   servir.
5. **Dado** una invitación pendiente, **cuando** quien invita la cancela, **entonces** su enlace
   deja de servir y la invitación aparece como cancelada.
6. **Dado** un integrante del club que no está autorizado a invitar, o alguien sin sesión,
   **cuando** intenta enviar, ver, reenviar o cancelar invitaciones, **entonces** el sistema se lo
   niega y no le muestra ningún dato.
7. **Dado** alguien autorizado a invitar en un club, **cuando** intenta ver, reenviar o cancelar
   una invitación de otro club, incluso conociendo su identificador, **entonces** el sistema se lo
   niega.

---

### Historia de usuario 2 - La persona invitada se registra y queda en la sala de espera (Prioridad: P1)

La persona abre el enlace del correo. La pantalla de registro le muestra el nombre del club al que
va a entrar y el correo ya escrito. Completa sus datos, crea su contraseña y queda registrada en
ese club, en espera. Desde ese momento, cada vez que entra a ese club ve una única pantalla que le
dice que su ingreso está pendiente de aprobación. No ve nada más del club.

**Por qué esta prioridad**: es la otra mitad del ingreso. Sin registro no hay a quién aprobar.

**Prueba independiente**: con una invitación recién enviada, se abre el enlace, se completa el
registro y se comprueba que la persona solo ve la pantalla de espera; se cierra la sesión, se
vuelve a entrar y se comprueba que sigue viendo lo mismo.

**Escenarios de aceptación**:

1. **Dado** una invitación válida del club, **cuando** la persona abre el enlace, **entonces** ve
   la pantalla de registro con el nombre del club y el correo de la invitación ya escrito, y no
   puede cambiar ninguno de los dos.
2. **Dado** la pantalla de registro, **cuando** la persona indica nombre completo, apellidos, tipo
   y número de documento, fecha de nacimiento, celular, nombre del padre, madre o responsable (si
   es menor de 18 años) y una contraseña válida, **entonces** queda registrada en ese club, en
   espera, y ve la pantalla de sala de espera con el nombre del club.
3. **Dado** una invitación ya usada, vencida, cancelada o reemplazada por otra, **cuando** alguien
   abre su enlace, **entonces** el sistema le explica que la invitación ya no sirve y no permite
   registrarse.
4. **Dado** una cuenta en espera, **cuando** inicia sesión con su correo o su documento,
   **entonces** ve solamente la pantalla de sala de espera.
5. **Dado** una cuenta en espera, **cuando** intenta obtener cualquier información del club
   (integrantes, configuración u otra), incluso llamando directamente a las operaciones,
   **entonces** el sistema se lo niega.
6. **Dado** una persona que se registra con un documento que ya existe en ese club, **cuando**
   envía el registro, **entonces** el sistema lo rechaza y le explica el motivo.
7. **Dado** una persona que ya tiene cuenta en la plataforma por pertenecer a otro club,
   **cuando** abre una invitación de este club, **entonces** no se crea una segunda cuenta: inicia
   sesión con la suya y queda en la sala de espera de este club, sin perder nada en el otro.
8. **Dado** una persona aprobada en un club y en espera en otro, **cuando** entra, **entonces**
   usa con normalidad el club donde está aprobada y ve la pantalla de espera solo al elegir el
   otro.
9. **Dado** una persona que, por su fecha de nacimiento, es menor de 18 años, **cuando** envía el
   registro sin el nombre del padre, madre o responsable, **entonces** el sistema lo rechaza y le
   explica que ese dato es obligatorio; a una persona adulta no se lo exige.

---

### Historia de usuario 3 - El presidente o un directivo aprueba el ingreso (Prioridad: P1)

El PRESIDENTE o un DIRECTIVO entra al apartado "Ingresos" y ve la sala de espera de su club: la
lista de personas registradas pendientes de aprobación, con los datos con los que se registraron.
Revisa a cada una y aprueba su ingreso. Al aprobar indica cómo entra la persona: como jugador o
con un rol del club. La persona aprobada deja de ver la pantalla de espera y entra a la aplicación
de su club.

**Por qué esta prioridad**: cierra el ingreso. Hasta que alguien aprueba, la persona registrada no
puede usar nada.

**Prueba independiente**: con una cuenta en espera, se inicia sesión como DIRECTIVO, se aprueba el
ingreso y se comprueba que esa cuenta ya entra a la aplicación del club y desaparece de la sala de
espera.

**Escenarios de aceptación**:

1. **Dado** el PRESIDENTE o un DIRECTIVO de un club, **cuando** abre la sala de espera,
   **entonces** ve a todas las personas en espera de su club, con nombre, apellidos, documento,
   fecha de nacimiento, correo, celular, responsable y fecha de registro, de la más antigua a la
   más reciente.
2. **Dado** una persona en espera, **cuando** el PRESIDENTE o un DIRECTIVO aprueba su ingreso como
   jugador, **entonces** queda aprobada con el rol JUGADOR, desaparece de la sala de espera y, al
   entrar, ya ve la aplicación de su club.
3. **Dado** una persona en espera, **cuando** el PRESIDENTE aprueba su ingreso y le asigna el rol
   ENTRENADOR o DIRECTIVO, **entonces** queda aprobada con ese rol como único rol en el club y no
   queda como jugador.
4. **Dado** una persona en espera, **cuando** un DIRECTIVO aprueba su ingreso y le asigna el rol
   ENTRENADOR, **entonces** queda aprobada con ese rol como único rol en el club.
5. **Dado** un DIRECTIVO, **cuando** intenta asignar el rol DIRECTIVO o PRESIDENTE a una persona,
   o el rol ENTRENADOR a alguien cuyo ingreso no acaba de aprobar él, **entonces** el sistema se
   lo niega.
6. **Dado** un ENTRENADOR, un JUGADOR, una cuenta en espera o alguien sin sesión, **cuando**
   intenta ver la sala de espera o aprobar un ingreso, **entonces** el sistema se lo niega y no le
   muestra ningún dato.
7. **Dado** el PRESIDENTE o un DIRECTIVO de un club, **cuando** intenta ver o aprobar a una
   persona en espera de otro club, incluso conociendo su identificador, **entonces** el sistema se
   lo niega.
8. **Dado** una persona con la pantalla de espera abierta, **cuando** su ingreso se aprueba,
   **entonces** al actualizar o volver a entrar ya ve la aplicación de su club, sin tener que
   registrarse de nuevo.
9. **Dado** un ingreso ya aprobado, **cuando** el PRESIDENTE o un DIRECTIVO abre la lista de
   ingresos aprobados del apartado "Ingresos", **entonces** ve a esa persona con su nombre, el rol
   con el que entró, quién la aprobó y cuándo, y no puede modificar nada desde esa lista.
10. **Dado** dos personas autorizadas con la sala de espera abierta, **cuando** las dos aprueban a
    la misma persona, **entonces** el ingreso se aprueba una sola vez y la segunda ve que ya
    estaba aprobado.

---

### Historia de usuario 4 - El club no acepta a una persona en espera (Prioridad: P2)

El PRESIDENTE o un DIRECTIVO ve en la sala de espera a alguien que no debe entrar: un registro
equivocado, un documento mal escrito o una persona que el club no acepta. Rechaza su ingreso, el
sistema le pide confirmación y esa persona desaparece del club. Si más adelante el club quiere que
entre, le envía una invitación nueva y la persona se registra otra vez.

**Por qué esta prioridad**: sin ella la sala de espera acumula registros equivocados o no
deseados, pero no impide que el club empiece a recibir personas.

**Prueba independiente**: con una cuenta en espera, se rechaza su ingreso y se comprueba que
desaparece de la sala de espera y ya no entra a ese club; después se le invita de nuevo y se
comprueba que puede registrarse otra vez.

**Escenarios de aceptación**:

1. **Dado** una persona en espera, **cuando** el PRESIDENTE o un DIRECTIVO rechaza su ingreso,
   **entonces** el sistema le pide confirmación antes de hacerlo y le advierte de que el registro
   se borrará.
2. **Dado** un rechazo confirmado, **cuando** se consulta el club, **entonces** esa persona ya no
   está en la sala de espera ni entre los integrantes, la invitación que usó ya no aparece en la
   lista de invitaciones y no queda ningún dato suyo en ese club.
3. **Dado** una persona rechazada cuyo único club era ese, **cuando** intenta iniciar sesión,
   **entonces** no puede: su cuenta ya no existe y ve el mismo mensaje de datos incorrectos que
   cualquier inicio de sesión fallido. No recibe ningún correo ni aviso del rechazo.
4. **Dado** una persona rechazada que pertenece a otros clubes, **cuando** inicia sesión,
   **entonces** sigue entrando a sus otros clubes con normalidad y ya no ve el club que la
   rechazó.
5. **Dado** una persona rechazada, **cuando** el club le envía una invitación nueva, **entonces**
   puede registrarse otra vez con el mismo correo y el mismo documento, y vuelve a quedar en
   espera.
6. **Dado** un ENTRENADOR, un JUGADOR, una cuenta en espera, alguien sin sesión o alguien de otro
   club, **cuando** intenta rechazar un ingreso, **entonces** el sistema se lo niega.
7. **Dado** un integrante ya aprobado, **cuando** alguien intenta rechazarlo, **entonces** el
   sistema se lo niega: el rechazo solo aplica a quien está en espera.

---

### Casos límite

- El correo de la invitación está mal escrito y nunca llega: quien invitó la cancela y envía otra
  al correo correcto.
- El servicio de correo falla al enviar la invitación: la invitación queda registrada como
  pendiente y se puede reenviar desde la lista.
- La persona abre el enlace el mismo día en que vence la invitación.
- La persona intenta registrarse con un correo distinto al de la invitación: no puede, el correo
  viene fijo.
- Se invita el correo de la cuenta DESARROLLADOR: el sistema lo rechaza.
- Un entrenador o directivo que además tiene un hijo en el club necesita dos cuentas con dos
  correos distintos (constitución §8): al invitar el correo que ya usa su propia cuenta, el
  sistema le indica que ese correo ya está en el club.
- Se invita a alguien que ya es integrante aprobado del club con otro rol.
- La persona que envió la invitación deja de estar autorizada a invitar antes de que la invitación
  se use: la invitación sigue sirviendo.
- La persona en espera olvida su contraseña o bloquea la cuenta: la recupera por correo como
  cualquier otra y sigue en espera.
- El club se suspende mientras hay personas en espera o invitaciones pendientes: las invitaciones
  siguen sirviendo, quien se registra queda en espera y el PRESIDENTE sigue invitando, aprobando y
  rechazando. La suspensión no alarga el plazo de las invitaciones.
- El club se da de baja mientras hay personas en espera o invitaciones pendientes: no se puede
  invitar, registrarse, aprobar ni rechazar; los enlaces dejan de servir mientras dure la baja.
- Dos personas autorizadas aprueban al mismo tiempo a la misma persona.
- Alguien intenta aprobar su propio ingreso.
- Una persona autorizada aprueba y otra rechaza a la misma persona al mismo tiempo: vale la
  primera acción y la segunda ve que el ingreso ya no está pendiente.
- Se rechaza a alguien que tiene la pantalla de espera abierta: en su siguiente acción deja de ver
  ese club.
- Una cuenta en espera intenta consultar datos del club usando directamente identificadores.

## Requisitos *(obligatorio)*

### Requisitos funcionales

**Invitación desde el club**

- **RF-001**: El PRESIDENTE y los DIRECTIVOS de un club DEBEN poder enviar invitaciones de registro
  a ese club. Nadie más DEBE poder enviar, ver, reenviar ni cancelar invitaciones del club.
- **RF-002**: Quien está autorizado DEBE poder enviar una invitación indicando solamente el correo
  de la persona. El sistema DEBE enviar a ese correo un enlace para registrarse en ese club y
  ningún otro.
- **RF-003**: La invitación del club NO lleva rol: quien se registra con ella entra como JUGADOR y
  en espera. El rol definitivo se decide al aprobar el ingreso.
- **RF-004**: Una invitación DEBE servir una sola vez, DEBE caducar y DEBE quedar ligada a un
  único club y al correo al que se envió. La persona no puede cambiar ninguno de los dos, y el
  sistema DEBE rechazar, en el servidor, un registro con otro correo.
- **RF-005**: Quien está autorizado DEBE poder ver las invitaciones de su club con su correo, su
  estado (pendiente, usada, vencida, cancelada), quién la envió, cuándo y cuándo vence.
- **RF-006**: Quien está autorizado DEBE poder reenviar y cancelar una invitación pendiente. Una
  invitación nueva al mismo correo DEBE invalidar la anterior. Una invitación cancelada o
  invalidada NO DEBE permitir registrarse.
- **RF-007**: El sistema DEBE rechazar, explicando el motivo, una invitación a un correo que ya es
  integrante de ese club o que ya está en su sala de espera, y una invitación al correo de la
  cuenta DESARROLLADOR.
- **RF-008**: Si el correo no se puede enviar, la invitación DEBE quedar registrada como pendiente
  y el sistema DEBE avisar a quien invita de que puede reenviarla.

**Registro por el enlace**

- **RF-009**: El sistema NO DEBE permitir registrarse en un club sin una invitación válida de ese
  club, y nadie DEBE poder elegir el club al registrarse.
- **RF-010**: El registro DEBE mostrar el nombre del club antes de confirmar y DEBE pedir nombre
  completo, apellidos, tipo y número de documento, fecha de nacimiento, celular de contacto y
  contraseña. DEBE permitir indicar el nombre del padre, madre o responsable, que es obligatorio
  cuando, por su fecha de nacimiento, quien ingresa es menor de 18 años el día del registro, y
  opcional para los adultos. El sistema DEBE comprobarlo en el servidor.
- **RF-011**: La cuenta de un jugador se registra con el documento del jugador y con el correo de
  su acudiente; un entrenador o directivo se registra con su propio documento. La pantalla de
  registro DEBE explicarlo con claridad.
- **RF-012**: El sistema DEBE rechazar un documento que ya exista en ese club, y un documento que
  ya pertenezca a otra cuenta, pidiéndole a la persona que entre con esa cuenta.
- **RF-013**: Si el correo invitado ya tiene cuenta en la plataforma, el sistema NO DEBE crear una
  segunda: la persona inicia sesión con su cuenta y queda añadida a este club, en espera,
  conservando su único inicio de sesión y todo lo que tiene en sus otros clubes.
- **RF-014**: Quien se registra con una invitación del club DEBE quedar con el rol JUGADOR y en
  estado EN_ESPERA en ese club.

**Sala de espera**

- **RF-015**: Una cuenta en espera en un club DEBE ver, al entrar a ese club, únicamente una
  pantalla que le indica que su ingreso está pendiente de aprobación, con el nombre del club.
- **RF-016**: Mientras está en espera, la cuenta NO DEBE acceder a ninguna información del club.
  Esta restricción DEBE aplicarse en el servidor a todas las operaciones del club; ocultar
  opciones en pantalla no cuenta como protección.
- **RF-017**: Mientras está en espera, a la persona no se le asigna categoría ni se le genera
  ningún cobro.
- **RF-018**: El estado de espera es de cada club: una persona en espera en un club DEBE seguir
  usando con normalidad los clubes en los que ya está aprobada.
- **RF-019**: La persona en espera DEBE poder cerrar sesión, cambiar el tema, recuperar su
  contraseña y cambiar de club como cualquier otra cuenta.

**Aprobación del ingreso**

- **RF-020**: El PRESIDENTE y los DIRECTIVOS de un club DEBEN poder ver la sala de espera de su
  club, con los datos de registro de cada persona y la fecha en que se registró. Ningún otro rol
  DEBE poder verla.
- **RF-021**: El PRESIDENTE y los DIRECTIVOS DEBEN poder aprobar el ingreso de cualquier persona
  en espera de su club. Ningún otro rol DEBE poder aprobar ingresos, y nadie DEBE poder aprobar el
  suyo.
- **RF-022**: Al aprobar, el PRESIDENTE DEBE poder dejar a la persona como JUGADOR o asignarle el
  rol ENTRENADOR o DIRECTIVO. Un DIRECTIVO DEBE poder dejarla como JUGADOR o asignarle el rol
  ENTRENADOR, y nada más. El rol asignado reemplaza al de JUGADOR y pasa a ser su único rol en el
  club.
- **RF-023**: El rol PRESIDENTE NO DEBE poder asignarse al aprobar un ingreso.
- **RF-024**: Al aprobarse, la cuenta DEBE pasar a APROBADO, dejar de ver la pantalla de espera y
  entrar a la aplicación del club con su rol, sin registrarse de nuevo. El cambio DEBE aplicarse
  también a una sesión ya abierta.
- **RF-025**: El sistema DEBE registrar quién aprobó cada ingreso, cuándo y con qué rol.
- **RF-025a**: El PRESIDENTE y los DIRECTIVOS DEBEN poder ver, en el apartado "Ingresos", la lista
  de ingresos aprobados de su club, de solo lectura, con el nombre de la persona, el rol con el
  que entró, quién la aprobó y cuándo. Ningún otro rol DEBE poder verla.
- **RF-026**: Aprobar dos veces el mismo ingreso NO DEBE producir ningún efecto adicional.
- **RF-027**: El PRESIDENTE y los DIRECTIVOS DEBEN poder rechazar el ingreso de una persona en
  espera de su club, con confirmación previa. Ningún otro rol DEBE poder hacerlo, y NO DEBE ser
  posible rechazar a un integrante ya aprobado.
- **RF-027a**: Rechazar DEBE borrar a esa persona del club sin dejar datos suyos en él, incluida
  la invitación con la que se registró, que deja de aparecer en la lista de invitaciones. Si
  pertenece a otros clubes, los conserva intactos; si ese era su único club, su cuenta se elimina.
- **RF-027b**: Una persona rechazada DEBE poder volver a registrarse en ese club, con el mismo
  correo y el mismo documento, solamente si recibe una invitación nueva.

**Aislamiento y generales**

- **RF-028**: Las invitaciones, la sala de espera, las aprobaciones y la lista de ingresos
  aprobados de un club DEBEN ser
  invisibles e inaccesibles para cualquier persona de otro club, también por identificador.
- **RF-029**: El DESARROLLADOR NO DEBE poder ver la sala de espera de ningún club, aprobar
  ingresos ni enviar invitaciones que no sean las de presidente.
- **RF-030**: En un club suspendido solo entra su PRESIDENTE, que DEBE poder seguir enviando,
  reenviando y cancelando invitaciones, y aprobando y rechazando ingresos. Los DIRECTIVOS no
  pueden hacerlo mientras dure la suspensión, porque no entran al club. Una invitación válida de
  un club suspendido DEBE seguir permitiendo registrarse, y la persona queda en espera. En un club
  dado de baja no se puede invitar, registrarse, aprobar ni rechazar.
- **RF-031**: Todas las pantallas de esta funcionalidad DEBEN funcionar en teléfono y en
  escritorio, en tema claro y oscuro, con la identidad del club y en español.

### Entidades clave

- **Invitación del club**: enlace enviado por correo que permite registrarse en un club concreto.
  Guarda el correo, el club, quién la envió, cuándo, cuándo vence y su estado (pendiente, usada,
  vencida, cancelada). No lleva rol. Es la misma invitación de la spec 001, enviada ahora desde
  dentro del club. Se borra si la persona que se registró con ella es rechazada.
- **Integrante**: pertenencia de una persona a un club, con su único rol en ese club. Gana un
  estado de ingreso.
- **Estado de ingreso**: EN_ESPERA o APROBADO. Es propio de cada pertenencia a un club.
- **Aprobación del ingreso**: quién aprobó a una persona, cuándo y con qué rol. Se consulta en la
  lista de ingresos aprobados del club. Un rechazo no deja
  registro: borra a la persona del club.
- **Sala de espera**: conjunto de integrantes de un club en estado EN_ESPERA.

## Criterios de éxito *(obligatorio)*

### Resultados medibles

- **CE-001**: Quien invita envía una invitación en menos de 30 segundos, y la invitación llega al
  correo en menos de 2 minutos.
- **CE-002**: La persona invitada completa su registro en menos de 3 minutos y sin ayuda.
- **CE-003**: El PRESIDENTE o un DIRECTIVO aprueba un ingreso en menos de 30 segundos desde que
  abre la sala de espera.
- **CE-004**: En el 100 % de los intentos probados, nadie consigue registrarse en un club sin una
  invitación válida de ese club.
- **CE-005**: En el 100 % de los intentos probados, una cuenta en espera no obtiene ningún dato
  del club, ni siquiera usando directamente identificadores.
- **CE-006**: En el 100 % de los intentos probados, solo el PRESIDENTE o un DIRECTIVO del club
  consigue ver la sala de espera y aprobar un ingreso, y nadie de otro club lo consigue.
- **CE-007**: En el 100 % de los intentos probados, un DIRECTIVO no consigue asignar ningún rol
  distinto de ENTRENADOR.
- **CE-008**: Una persona aprobada entra a la aplicación de su club en su siguiente acceso, sin
  repetir ningún paso del registro.
- **CE-009**: Todas las pantallas de la funcionalidad se usan sin desplazamiento horizontal en un
  teléfono y el texto se lee bien en ambos temas.

## Supuestos

- Quien se registra con una invitación del club pasa siempre por la sala de espera. Así lo pide la
  descripción de esta funcionalidad y queda cerrada la decisión "Invitación y sala de espera" de
  la §28 de la constitución.
- La invitación del club se comporta igual que la de presidente de la spec 001: sirve una sola
  vez, caduca a los 7 días y lleva el correo fijo. Con esto queda cerrada la parte de caducidad y
  un solo uso de la decisión "Quién invita dentro del club".
- El rol se elige en el momento de aprobar (RF-022). Se incluye aquí porque, sin ello, un
  entrenador o un directivo recién registrado quedaría aprobado como jugador. Buscar usuarios por
  documento para asignar o retirar roles más adelante sigue fuera de alcance.
- Asignar la categoría por año de nacimiento y empezar a generar la mensualidad al aprobar a un
  jugador (constitución §12.1.1) se construye con las funcionalidades de categorías y de
  mensualidades, que todavía no existen. Esta funcionalidad deja al jugador aprobado y con sus
  datos de registro guardados para entonces.
- La ficha de Jugador con el resto de sus datos (médicos, documentos) pertenece a la funcionalidad
  de jugadores.
- No se envía ningún correo ni aviso a quien aprueba cuando alguien se registra, ni a la persona
  cuando se aprueba o se rechaza su ingreso: cada quien lo ve al entrar.
- Las invitaciones se envían de una en una; no hay carga masiva de correos.
- Mientras no existan las funcionalidades posteriores, la persona aprobada ve la pantalla de
  inicio de su club que dejó la spec 001.

## Fuera de alcance

- Agregar un hermano desde la ficha de un jugador ya registrado.
- Elegir con cuál jugador continuar al entrar con el correo, y entrar con el documento de un
  jugador.
- Búsqueda de usuarios por documento y asignación o retiro de roles a personas ya aprobadas.
- Elección de un presidente adicional por otro PRESIDENTE del club.
- Paso a ENTRENADOR o DIRECTIVO de una cuenta que ya tiene historial como jugador (decisión
  abierta en la §28).
- Categorías, asignación de entrenadores a categorías, mensualidades y cargos.
- Formulario de inscripción del sitio público.
- Solicitud de eliminación de la propia cuenta y cambio de contraseña desde dentro de la
  aplicación.

---

**Nota de seguimiento (2026-10-20)**: lo que este documento deja pendiente sobre mensualidades, cargos,
pagos y el estado de cuenta lo construye la funcionalidad
[015-mensualidades-cargos-pagos](../015-mensualidades-cargos-pagos/spec.md). Este documento no se reescribe.
