# Especificación de funcionalidad: Base multiclub y panel de administración de la plataforma

**Rama de la funcionalidad**: `001-base-multiclub`

**Creada**: 2026-10-07

**Estado**: Borrador

**Entrada**: Descripción del propietario del proyecto: "Base multiclub y panel de administración de
la plataforma: el DESARROLLADOR crea clubes asignándoles obligatoriamente un presidente, al que se
invita por correo a registrarse; configura el escudo y los colores de cada club y puede
suspenderlos, darlos de baja o eliminarlos. Cada integrante ve la aplicación con la identidad de
su club y, si pertenece a varios, elige cuál ver. Toda la interfaz tiene tema claro y oscuro."

**Plataforma**: LFClub

**Constitución aplicable**: versión 3.6.0, en especial §7, §8, §10, §12.1, §12.4, §12.5 y §24.

## Aclaraciones

### Sesión 2026-10-07

- P: ¿Puede la persona invitada como presidente registrarse con un correo distinto al correo al
  que le llegó la invitación? → R: No. El correo viene ya escrito en el registro, es el mismo al
  que se envió la invitación y no se puede cambiar; si está mal, el DESARROLLADOR lo corrige y
  reenvía la invitación.
- P: ¿Se puede reactivar un club que está dado de baja, y hace falta darlo de baja antes de poder
  eliminarlo? → R: La baja se puede revertir y el club vuelve a estar activo; solo se puede
  eliminar un club que ya está dado de baja.
- P: ¿Qué debe pasar cuando alguien se equivoca de contraseña varias veces seguidas al iniciar
  sesión? → R: Tras 5 intentos fallidos seguidos, la cuenta queda bloqueada hasta que la persona
  recupere su contraseña por correo. Por eso "olvidé mi contraseña" entra en el alcance de esta
  funcionalidad.
- P: ¿Qué puede hacer el DESARROLLADOR si la persona registrada como presidente de un club no es
  la correcta o deja el club? → R: Puede quitarle el rol de presidente desde su panel, siempre que
  el club conserve al menos otro presidente; si es el único, primero invita al nuevo.
- P: ¿Qué pasa con la persona a la que el DESARROLLADOR le quita el rol de presidente en un club?
  → R: El DESARROLLADOR elige en ese momento entre asignarle otro rol en ese club o eliminarla del
  club por completo.
- P: ¿Qué pasa si el DESARROLLADOR invita como presidente a alguien que ya es integrante de ese
  club con otro rol? → R: Al aceptar la invitación, su rol en ese club pasa a ser PRESIDENTE, que
  reemplaza al anterior. No se crea un segundo integrante.

### Sesión 2026-10-07 (plan)

- P: ¿Puede un mismo documento estar en dos cuentas distintas? → R: No. Una persona es única y
  tiene un único inicio de sesión.
- P: ¿Qué archivos se admiten como escudo? → R: PNG, JPEG o WebP de hasta 1 MB. Lo mismo vale
  para la foto de perfil que puede agregar cada usuario registrado.

### Sesión 2026-10-07 (tareas)

- P: ¿Se puede invitar como presidente al correo de la cuenta DESARROLLADOR? → R: No. Esa cuenta
  usa un único correo, que no se usa para nada más: el sistema rechaza crear un club o enviar una
  invitación con ese correo.
- P: ¿Importan las mayúsculas o los espacios al escribir el correo o el documento para entrar? →
  R: No. Correo y documento se guardan y se buscan sin espacios en los extremos y sin distinguir
  mayúsculas de minúsculas; el documento, además, sin espacios ni puntos. La contraseña no se
  toca: se compara tal como se escribió.

## Escenarios de usuario y pruebas *(obligatorio)*

### Historia de usuario 1 - El desarrollador crea un club e invita a su presidente (Prioridad: P1)

El DESARROLLADOR inicia sesión y llega a su panel de administración de la plataforma. Allí ve la
lista de clubes con su estado. Crea un club nuevo indicando su nombre y, obligatoriamente, el
correo de quien será su PRESIDENTE. La plataforma envía a ese correo una invitación para
registrarse como presidente del club creado.

**Por qué esta prioridad**: sin clubes no existe nada más, y un club sin presidente no puede
usarse. Es lo que permite ofrecer la plataforma a un equipo nuevo.

**Prueba independiente**: se inicia sesión como DESARROLLADOR, se crea un club con el correo de su
presidente y se comprueba que el club aparece en la lista y que la invitación llega a ese correo.

**Escenarios de aceptación**:

1. **Dado** que el DESARROLLADOR tiene su cuenta, **cuando** inicia sesión con credenciales
   correctas, **entonces** llega al panel de administración y ve la lista de clubes con el nombre
   y el estado de cada uno.
2. **Dado** el DESARROLLADOR en el panel, **cuando** crea un club con un nombre y el correo de su
   presidente, **entonces** el club aparece en la lista, con una identidad por defecto y con la
   indicación de que su presidente aún no se ha registrado, y se envía la invitación a ese correo.
3. **Dado** el DESARROLLADOR creando un club, **cuando** no indica el correo del presidente o el
   correo no es válido, **entonces** el sistema no crea el club y le explica por qué.
4. **Dado** que ya existe un club con un nombre, **cuando** el DESARROLLADOR intenta crear otro con
   el mismo nombre, **entonces** el sistema lo rechaza con un mensaje claro y no crea nada.
5. **Dado** un club cuyo presidente aún no se ha registrado, **cuando** el DESARROLLADOR vuelve a
   enviar la invitación o corrige el correo, **entonces** se envía una invitación nueva y la
   anterior deja de servir.
6. **Dado** un club con dos presidentes registrados, **cuando** el DESARROLLADOR le quita el rol a
   uno desde su panel y le asigna otro rol, **entonces** esa persona sigue en el club con el rol
   nuevo como único rol y el otro presidente sigue siéndolo.
6a. **Dado** un club con dos presidentes registrados, **cuando** el DESARROLLADOR le quita el rol
   a uno y elige eliminarlo del club, **entonces** esa persona ya no puede entrar a ese club y
   conserva su acceso a sus otros clubes, si los tiene.
7. **Dado** un club con un único presidente, **cuando** el DESARROLLADOR intenta quitarle el rol,
   **entonces** el sistema se lo niega y le indica que primero debe registrarse otro presidente.
8. **Dado** un integrante de cualquier club, sea cual sea su rol, o alguien sin sesión iniciada,
   **cuando** intenta entrar al panel de administración o usar cualquiera de sus operaciones,
   **entonces** el sistema se lo niega y no le muestra ningún dato.

---

### Historia de usuario 2 - El presidente invitado se registra y entra a su club (Prioridad: P1)

La persona invitada abre el enlace del correo. La pantalla de registro le muestra el nombre del
club al que va a entrar y el rol que tendrá. Completa sus datos, crea su contraseña y queda dentro
de la aplicación de su club como PRESIDENTE, sin pasar por la sala de espera. Desde entonces
inicia sesión con su correo o con su documento.

**Por qué esta prioridad**: cierra el alta de un club. Hasta que el presidente no entra, el club
no tiene a nadie que lo use.

**Prueba independiente**: con una invitación recién enviada, se abre el enlace, se completa el
registro y se comprueba que la persona queda como PRESIDENTE del club correcto; después se cierra
la sesión y se vuelve a entrar con el correo y con el documento.

**Escenarios de aceptación**:

1. **Dado** una invitación válida, **cuando** la persona abre el enlace, **entonces** ve la
   pantalla de registro con el nombre del club, el rol de PRESIDENTE y el correo al que se envió
   la invitación ya escrito, y no puede cambiar ninguno de los tres.
2. **Dado** la pantalla de registro, **cuando** la persona indica nombre completo, apellidos,
   correo, tipo y número de documento, fecha de nacimiento, celular y una contraseña válida,
   **entonces** queda registrada
   como PRESIDENTE de ese club, aprobada, y entra a la aplicación viendo el nombre de su club.
3. **Dado** una invitación ya usada, vencida o reemplazada por otra, **cuando** alguien abre su
   enlace, **entonces** el sistema le explica que la invitación ya no sirve y no permite
   registrarse.
4. **Dado** alguien sin invitación, **cuando** busca cómo registrarse, **entonces** no encuentra
   ninguna forma de crear una cuenta.
5. **Dado** un integrante registrado, **cuando** inicia sesión con su correo y su contraseña, o
   con su documento y su contraseña, **entonces** entra a la aplicación de su club.
6. **Dado** una persona que ya es integrante de un club, **cuando** recibe y acepta la invitación
   para ser PRESIDENTE de otro club, **entonces** no se crea una segunda cuenta: la misma persona
   queda como integrante de los dos clubes, con su rol en cada uno.
7. **Dado** una persona que pertenece a varios clubes, **cuando** entra a su panel, **entonces**
   ve un desplegable con sus clubes, puede elegir cuál ver y solo ve los datos del club elegido.
8. **Dado** una persona que pertenece a un solo club, **cuando** entra a su panel, **entonces** ve
   directamente ese club.
9. **Dado** un correo que ya está registrado, **cuando** alguien intenta registrar una cuenta
   nueva con ese correo, **entonces** el sistema lo rechaza y le explica que ya hay alguien
   registrado con ese correo.
10. **Dado** un club que ya tiene presidente, **cuando** el DESARROLLADOR invita desde su panel a
    un presidente adicional y esa persona se registra, **entonces** el club queda con dos
    presidentes.
11. **Dado** una cuenta, **cuando** alguien falla la contraseña 5 veces seguidas, **entonces** la
    cuenta queda bloqueada, ya no admite ni siquiera la contraseña correcta, y la pantalla indica
    cómo recuperarla por correo.
12. **Dado** una persona que olvidó su contraseña o tiene la cuenta bloqueada, **cuando** pide
    recuperarla, **entonces** recibe en su correo un enlace con el que crea una contraseña nueva,
    y con ella la cuenta queda desbloqueada.
13. **Dado** alguien que pide recuperar la contraseña de un correo que no está registrado,
    **cuando** envía la solicitud, **entonces** ve el mismo mensaje que si existiera y no se envía
    ningún correo.
14. **Dado** el único PRESIDENTE de un club, **cuando** intenta quitarse su rol o eliminar su
    cuenta, **entonces** el sistema se lo niega y le explica que antes debe existir otro
    PRESIDENTE.
15. **Dado** un DIRECTIVO de un club, **cuando** el DESARROLLADOR lo invita como PRESIDENTE de ese
    mismo club y acepta la invitación, **entonces** pasa a ser PRESIDENTE como único rol y el club
    no tiene dos integrantes de la misma persona.

---

### Historia de usuario 3 - Cada club se ve con su propia identidad (Prioridad: P2)

El DESARROLLADOR configura desde su panel el escudo y los colores de un club. A partir de ese
momento, cualquier integrante de ese club ve la aplicación con ese escudo, esos colores y el
nombre de su club. Quien pertenece a varios clubes ve la identidad del club que tiene elegido.

**Por qué esta prioridad**: es lo que hace que cada equipo sienta la aplicación como propia y lo
que permite ofrecerla a varios clubes. No bloquea el acceso, por eso va después de las historias 1
y 2.

**Prueba independiente**: se configuran identidades distintas en dos clubes, se entra con un
integrante de cada uno y se comprueba que cada quien ve la de su club.

**Escenarios de aceptación**:

1. **Dado** un club existente, **cuando** el DESARROLLADOR le carga un escudo y le define sus
   colores, **entonces** el cambio queda guardado y se muestra en el panel.
2. **Dado** un club con identidad configurada, **cuando** un integrante de ese club inicia sesión,
   **entonces** ve la aplicación con el escudo, los colores y el nombre de su club.
3. **Dado** una persona que pertenece a dos clubes con identidades distintas, **cuando** cambia de
   club en el desplegable, **entonces** la aplicación pasa a mostrarse con el escudo, los colores
   y el nombre del club elegido.
4. **Dado** un club recién creado y sin escudo ni colores definidos, **cuando** un integrante suyo
   inicia sesión, **entonces** ve la aplicación con una identidad neutra por defecto y el nombre
   de su club.
5. **Dado** el DESARROLLADOR definiendo los colores de un club, **cuando** elige una combinación
   en la que el texto no se leería bien, **entonces** el sistema le avisa antes de guardar.

---

### Historia de usuario 4 - El presidente mantiene los datos de su club (Prioridad: P2)

El PRESIDENTE de un club entra a la configuración de su club y actualiza el nombre, la sede, la
dirección y los datos de contacto. El DESARROLLADOR puede hacer lo mismo desde su panel. El
PRESIDENTE no puede cambiar el escudo ni los colores.

**Por qué esta prioridad**: el club necesita mantener sus propios datos sin depender del
DESARROLLADOR para cada cambio.

**Prueba independiente**: un PRESIDENTE cambia la dirección de su club y se comprueba que el
cambio se ve en su aplicación y en el panel del DESARROLLADOR.

**Escenarios de aceptación**:

1. **Dado** el PRESIDENTE de un club, **cuando** edita el nombre, la sede, la dirección o los
   datos de contacto de su club, **entonces** el cambio queda guardado y se muestra a los
   integrantes de ese club.
2. **Dado** el PRESIDENTE de un club, **cuando** intenta cambiar el escudo o los colores,
   **entonces** el sistema se lo niega.
3. **Dado** el PRESIDENTE de un club, **cuando** intenta consultar o editar la configuración de un
   club al que no pertenece, incluso conociendo su identificador, **entonces** el sistema se lo
   niega.
4. **Dado** un integrante que no es PRESIDENTE, **cuando** intenta editar la configuración de su
   club, **entonces** el sistema se lo niega.
5. **Dado** el DESARROLLADOR en su panel, **cuando** edita el nombre, la sede, la dirección o los
   datos de contacto de cualquier club, **entonces** el cambio queda guardado.

---

### Historia de usuario 5 - El desarrollador suspende, da de baja o elimina un club (Prioridad: P3)

El DESARROLLADOR cambia el estado de un club desde su panel. Puede suspenderlo y levantar la
suspensión cuantas veces haga falta, darlo de baja o eliminarlo.

- **Suspendido**: solo entra su PRESIDENTE; el resto ve un aviso. Todo permanece intacto.
- **Dado de baja**: no entra nadie.
- **Eliminado**: desaparece toda la información del club.

**Por qué esta prioridad**: es necesaria para administrar la plataforma como negocio, pero no hace
falta para que el primer club empiece a usarla.

**Prueba independiente**: se suspende un club, se comprueba quién puede entrar y qué ve el resto,
se levanta la suspensión y se comprueba que todo sigue exactamente como antes.

**Escenarios de aceptación**:

1. **Dado** un club activo, **cuando** el DESARROLLADOR lo suspende, **entonces** el club aparece
   como suspendido en el panel y todos sus datos se conservan intactos.
2. **Dado** un club suspendido, **cuando** su PRESIDENTE inicia sesión, **entonces** entra con
   normalidad y ve que el club está suspendido.
3. **Dado** un club suspendido, **cuando** un integrante que no es su PRESIDENTE intenta entrar a
   ese club, **entonces** no entra y ve un aviso de incidencia temporal que le pide comunicarse
   con el presidente.
4. **Dado** una persona que pertenece a un club suspendido y a otro activo, **cuando** entra,
   **entonces** usa con normalidad el club activo y ve el aviso solo al elegir el suspendido.
5. **Dado** un club suspendido, **cuando** el DESARROLLADOR levanta la suspensión, **entonces** el
   club vuelve a estar activo y sus integrantes y datos están exactamente como antes.
6. **Dado** un club dado de baja, **cuando** cualquiera de sus integrantes, incluido su
   PRESIDENTE, intenta entrar a ese club, **entonces** no entra.
7. **Dado** un club dado de baja, **cuando** el DESARROLLADOR lo reactiva, **entonces** el club
   vuelve a estar activo con todos sus datos e integrantes como antes.
8. **Dado** un club activo o suspendido, **cuando** el DESARROLLADOR busca cómo eliminarlo,
   **entonces** no puede: primero tiene que darlo de baja.
9. **Dado** el DESARROLLADOR a punto de eliminar un club dado de baja, **cuando** elige eliminarlo,
   **entonces** el sistema le exige una confirmación expresa escribiendo el nombre del club y le
   advierte de que no se puede deshacer.
10. **Dado** un club eliminado, **cuando** se consulta la plataforma, **entonces** no queda ningún
    dato de ese club y los demás clubes no han cambiado en nada.
11. **Dado** una persona que pertenecía al club eliminado y a otro club, **cuando** inicia sesión,
   **entonces** sigue entrando a su otro club con normalidad.

Dar de baja y eliminar son dos pasos distintos: dar de baja bloquea a todos pero conserva los
datos; eliminar los borra.

---

### Historia de usuario 6 - Tema claro y tema oscuro (Prioridad: P3)

Cualquier persona que usa el panel de administración o la aplicación de un club cambia entre tema
claro y tema oscuro con un botón visible. La próxima vez que entra, la aplicación recuerda su
elección.

**Por qué esta prioridad**: es una comodidad pedida expresamente, pero no condiciona ninguna otra
historia.

**Prueba independiente**: se pulsa el botón de tema en cualquier pantalla, se comprueba que toda
la interfaz cambia, se cierra y se vuelve a abrir, y se comprueba que el tema se mantiene.

**Escenarios de aceptación**:

1. **Dado** cualquier pantalla de la plataforma, incluidas las de inicio de sesión y de registro,
   **cuando** la persona pulsa el botón de tema, **entonces** toda la interfaz cambia al otro tema
   de inmediato.
2. **Dado** una persona que eligió un tema, **cuando** vuelve a abrir la aplicación en el mismo
   dispositivo, **entonces** la aplicación se muestra con el tema elegido.
3. **Dado** una persona que nunca ha elegido tema, **cuando** abre la aplicación, **entonces** se
   muestra con el tema que tenga configurado su dispositivo.
4. **Dado** un club con sus colores configurados, **cuando** se ve en tema claro y en tema oscuro,
   **entonces** en ambos el texto se lee bien y el club sigue siendo reconocible por sus colores.

---

### Historia de usuario 7 - Foto de perfil (Prioridad: P3)

Cualquier persona con cuenta entra a "Mi perfil" y agrega una foto de perfil. La ve en la cabecera
de la aplicación, en todos sus clubes. Puede cambiarla o quitarla cuando quiera.

**Por qué esta prioridad**: es una comodidad pedida expresamente y no condiciona ninguna otra
historia.

**Prueba independiente**: se carga una foto, se comprueba que aparece en la cabecera, se cambia
de club y se comprueba que sigue siendo la misma; después se quita.

**Escenarios de aceptación**:

1. **Dado** una persona con sesión iniciada, **cuando** carga una foto PNG, JPEG o WebP de hasta
   1 MB, **entonces** la foto queda guardada y se muestra en la cabecera y en "Mi perfil".
2. **Dado** una persona con sesión iniciada, **cuando** intenta cargar un archivo que no es una
   imagen admitida o que pesa más de 1 MB, **entonces** el sistema lo rechaza, explica el motivo y
   conserva la foto anterior.
3. **Dado** una persona con foto de perfil, **cuando** la quita, **entonces** la aplicación vuelve
   a mostrar sus iniciales.
4. **Dado** cualquier persona, con o sin sesión, **cuando** intenta obtener la foto de perfil de
   otra cuenta, **entonces** no la obtiene.

---

### Casos límite

- Alguien carga como foto de perfil un archivo que no es una imagen o que pesa demasiado.

- El correo de la invitación está mal escrito y la invitación nunca llega: el DESARROLLADOR lo
  corrige desde su panel y se envía una invitación nueva.
- La persona invitada intenta registrarse con un correo distinto al de la invitación: no puede,
  el correo viene fijo.
- El documento con el que se registra ya existe en ese mismo club.
- El servicio de correo falla en el momento de crear el club.
- El DESARROLLADOR carga como escudo un archivo que no es una imagen o que pesa demasiado.
- El DESARROLLADOR intenta dejar un club sin PRESIDENTE: el sistema lo impide.
- El DESARROLLADOR indica su propio correo como presidente de un club: el sistema lo rechaza.
- Alguien escribe su correo con mayúsculas o con espacios al entrar: entra igual.
- Alguien intenta iniciar sesión muchas veces seguidas con contraseñas incorrectas: al quinto
  fallo la cuenta se bloquea hasta recuperar la contraseña por correo.
- Alguien bloquea a propósito la cuenta de otra persona fallando su contraseña: la persona
  afectada la recupera por correo.
- Un club se suspende, se da de baja o se elimina mientras sus integrantes tienen la sesión
  abierta.
- Se elimina el único club al que pertenecía una persona.
- Un integrante intenta consultar un club, un integrante o una configuración de un club al que no
  pertenece usando directamente su identificador.
- Una persona que pertenece a varios clubes intenta elegir un club al que no pertenece.

## Requisitos *(obligatorio)*

### Requisitos funcionales

**Acceso y panel de administración**

- **RF-001**: El sistema DEBE tener exactamente una cuenta DESARROLLADOR, que no pertenece a ningún
  club. No DEBE ser posible crear otra ni asignar ese rol desde la aplicación. Su correo no se usa
  para nada más: el sistema DEBE rechazar la creación de un club o el envío de una invitación con
  ese correo y explicar el motivo.
- **RF-002**: El sistema DEBE permitir iniciar sesión con el correo o con el documento del
  integrante, más su contraseña. El correo DEBE ser único en la plataforma: el sistema DEBE rechazar
  el registro de una cuenta nueva con un correo ya registrado y explicar el motivo. El correo y el
  documento DEBEN compararse sin los espacios de los extremos y sin distinguir mayúsculas de
  minúsculas, y el documento también sin espacios ni puntos, tanto al guardarlos como al iniciar
  sesión. La contraseña DEBE compararse tal como se escribió.
- **RF-003**: El sistema DEBE dar acceso al panel de administración de la plataforma únicamente al
  DESARROLLADOR, y DEBE negar ese acceso y todas sus operaciones a cualquier otro rol y a quien no
  tenga sesión iniciada.
- **RF-004**: El sistema DEBE impedir que el DESARROLLADOR consulte o gestione fichas de jugadores,
  datos médicos, finanzas, pagos o información deportiva de cualquier club.
- **RF-005**: Tras 5 intentos fallidos seguidos de inicio de sesión, el sistema DEBE bloquear esa
  cuenta hasta que la persona recupere su contraseña por correo. Un inicio de sesión correcto
  antes del quinto fallo reinicia la cuenta de intentos. El sistema NO DEBE revelar si una cuenta
  existe.
- **RF-005a**: Toda persona con cuenta, incluido el DESARROLLADOR, DEBE poder pedir la
  recuperación de su contraseña desde la pantalla de inicio de sesión. El sistema le envía por
  correo un enlace de un solo uso y con caducidad para crear una contraseña nueva; al crearla, la
  cuenta queda desbloqueada y la contraseña anterior deja de servir.

**Clubes**

- **RF-006**: El DESARROLLADOR DEBE poder ver la lista de todos los clubes con su nombre, su
  estado y si su presidente ya se registró.
- **RF-007**: El DESARROLLADOR DEBE poder crear un club indicando su nombre y, obligatoriamente,
  el correo de su PRESIDENTE. El sistema NO DEBE crear un club sin ese correo. El nombre del club
  DEBE ser único en la plataforma.
- **RF-008**: El DESARROLLADOR DEBE poder cargar el escudo y definir los colores de cada club.
  Nadie más puede hacerlo. El escudo DEBE ser una imagen PNG, JPEG o WebP de hasta 1 MB.
- **RF-009**: El sistema DEBE avisar al DESARROLLADOR, antes de guardar, cuando los colores
  elegidos para un club no permitan leer bien el texto en alguno de los dos temas.
- **RF-010**: El DESARROLLADOR y el PRESIDENTE de un club DEBEN poder editar el nombre, la sede,
  la dirección y los datos de contacto de ese club. El PRESIDENTE solo puede hacerlo en su propio
  club. Ningún otro rol puede hacerlo.

**Invitación y registro del presidente**

- **RF-011**: Al crear un club, el sistema DEBE enviar por correo al presidente indicado una
  invitación con un enlace para registrarse como PRESIDENTE de ese club.
- **RF-012**: El DESARROLLADOR DEBE poder reenviar la invitación o corregir el correo mientras el
  presidente no se haya registrado. Una invitación nueva DEBE invalidar la anterior. El
  DESARROLLADOR invita solamente a presidentes, no a ningún otro rol, y DEBE poder invitar a un
  presidente adicional a un club que ya existe.
- **RF-013**: Una invitación DEBE servir una sola vez, DEBE caducar y DEBE quedar ligada a un
  único club, a un único rol y al correo al que se envió. La persona no puede cambiar ninguno de
  los tres: el correo aparece ya escrito en el registro y el sistema DEBE rechazar, en el
  servidor, un registro con otro correo.
- **RF-014**: El sistema NO DEBE permitir registrarse sin una invitación válida.
- **RF-015**: El registro DEBE pedir nombre completo, apellidos, correo, tipo y número de
  documento, fecha de nacimiento, celular de contacto y contraseña, y DEBE mostrar el nombre del
  club y el rol antes de confirmar. El nombre del padre, madre o responsable se pide solo cuando
  quien se registra es un jugador, por lo que no aplica al registro del presidente.
- **RF-016**: Quien se registra con la invitación de presidente DEBE quedar como PRESIDENTE de ese
  club, aprobado y sin pasar por la sala de espera.
- **RF-017**: El sistema DEBE rechazar un documento que ya exista en el mismo club.
- **RF-017a**: Una persona es única en la plataforma y tiene un único inicio de sesión: el sistema
  DEBE rechazar el registro de un documento que ya pertenece a otra cuenta y pedirle que entre con
  esa cuenta.
- **RF-018**: Si la persona invitada ya tiene cuenta, el sistema NO DEBE crearle una segunda: al
  abrir la invitación inicia sesión con su cuenta y queda añadida al nuevo club con su rol,
  conservando su único inicio de sesión. Si ya es integrante de ese mismo club con otro rol,
  aceptar la invitación reemplaza ese rol por el de la invitación y no crea un segundo integrante.
- **RF-019**: El sistema NO DEBE almacenar ni mostrar contraseñas en texto legible, y nadie asigna
  la contraseña de otra persona.
- **RF-019a**: El DESARROLLADOR DEBE poder ver los presidentes de cada club y quitarle el rol a
  uno desde su panel, con confirmación previa, siempre que el club conserve al menos otro
  presidente ya registrado. Una invitación pendiente no cuenta como presidente. Al quitarlo, el
  DESARROLLADOR DEBE elegir entre asignarle otro rol en ese club (DIRECTIVO o ENTRENADOR), que
  pasa a ser su único rol allí, o eliminarlo del club por completo. Quien es eliminado del club
  deja de poder entrar a él; si era su único club, su cuenta se elimina.
- **RF-020**: Cada club DEBE conservar siempre al menos un PRESIDENTE una vez que tiene uno. Un
  PRESIDENTE NO DEBE poder quitarse su rol ni eliminar su cuenta mientras no exista otro
  PRESIDENTE en ese club.

**Pertenencia, identidad y aislamiento**

- **RF-021**: Un integrante tiene un único rol dentro de un club. Una persona puede pertenecer a
  varios clubes con un rol distinto en cada uno.
- **RF-022**: Una persona que pertenece a varios clubes DEBE poder elegir, con un desplegable en
  su panel, de cuál club ve los datos. Solo DEBE poder elegir clubes a los que pertenece.
- **RF-023**: Cada integrante DEBE ver la aplicación con el escudo y los colores del club elegido
  y DEBE ver en pantalla el nombre de ese club.
- **RF-024**: Un integrante NO DEBE poder ver, contar ni consultar por identificador ningún dato
  de un club al que no pertenece, incluidos su configuración y sus integrantes.
- **RF-025**: Toda operación de un integrante DEBE quedar limitada, en el servidor, al club
  elegido y a su rol en ese club. Ocultar opciones en pantalla no cuenta como protección.

**Estados del club**

- **RF-026**: El DESARROLLADOR DEBE poder suspender un club y levantar la suspensión, tantas veces
  como haga falta. Suspender NO DEBE eliminar ni modificar ningún dato del club.
- **RF-027**: Mientras un club está suspendido, solo DEBE poder entrar su PRESIDENTE. Cualquier
  otro integrante que intente entrar a ese club DEBE ver un aviso de incidencia temporal que le
  pide comunicarse con el presidente.
- **RF-028**: El DESARROLLADOR DEBE poder dar de baja un club y revertir la baja, con lo que el
  club vuelve a estar activo con sus datos intactos. Mientras un club está dado de baja, nadie de
  ese club DEBE poder entrar, ni siquiera su PRESIDENTE.
- **RF-029**: El DESARROLLADOR DEBE poder eliminar un club, pero solamente si ya está dado de
  baja; el sistema DEBE rechazar la eliminación de un club activo o suspendido. Eliminar DEBE borrar toda la
  información de ese club de forma irreversible, NO DEBE afectar a ningún otro club y DEBE exigir
  una confirmación expresa.
- **RF-030**: El estado de un club DEBE aplicarse también a las sesiones ya abiertas.
- **RF-031**: El sistema DEBE registrar quién cambió el estado de un club y cuándo.

**Tema claro y oscuro**

- **RF-032**: Todas las pantallas del panel de administración y de la aplicación del club,
  incluidas las de inicio de sesión y registro, DEBEN ofrecer tema claro y tema oscuro con un
  botón visible para cambiar entre ellos.
- **RF-033**: El sistema DEBE recordar el tema elegido en ese dispositivo y, si la persona nunca
  ha elegido, DEBE usar el que tenga configurado el dispositivo.
- **RF-034**: En ambos temas, el texto DEBE leerse bien y los estados DEBEN distinguirse por texto
  además de por color.

**Foto de perfil**

- **RF-036**: Toda persona con cuenta DEBE poder cargar, cambiar y quitar su foto de perfil. La
  foto es opcional y no se pide al registrarse.
- **RF-037**: La foto de perfil DEBE ser una imagen PNG, JPEG o WebP de hasta 1 MB. El sistema
  DEBE rechazar cualquier otro archivo, explicar el motivo y conservar la foto anterior.
- **RF-038**: La foto de perfil pertenece a la cuenta y es la misma en todos los clubes de la
  persona. Nadie DEBE poder obtener la foto de perfil de otra cuenta.

**Generales**

- **RF-035**: Todas las pantallas de esta funcionalidad DEBEN funcionar en teléfono y en
  escritorio, y estar en español.

### Entidades clave

- **Club**: escuela o equipo que usa la plataforma. Tiene nombre, sede, dirección, datos de
  contacto, escudo, colores y estado (activo, suspendido, dado de baja). Todos los demás datos de
  un club le pertenecen solo a él.
- **Persona con cuenta**: quien inicia sesión. Tiene nombre completo, apellidos, correo,
  documento, fecha de nacimiento y contraseña. Es una sola aunque pertenezca a varios clubes. La
  cuenta DESARROLLADOR es la única que no pertenece a ningún club.
- **Integrante**: pertenencia de una persona a un club, con su único rol en ese club. El documento
  no se repite dentro de un club. En el código es la entidad `UsuarioRol` de la constitución §12.3.
- **Foto de perfil**: imagen opcional de una persona con cuenta.
- **Rol**: DESARROLLADOR, PRESIDENTE, DIRECTIVO, ENTRENADOR o JUGADOR.
- **Invitación**: enlace enviado por correo que permite registrarse en un club concreto con un rol
  concreto. Sirve una sola vez y caduca.
- **Preferencia de tema**: tema claro u oscuro elegido por una persona en un dispositivo.

## Criterios de éxito *(obligatorio)*

### Resultados medibles

- **CE-001**: El DESARROLLADOR crea un club con su escudo, sus colores y la invitación a su
  presidente en menos de 5 minutos.
- **CE-002**: La invitación llega al correo del presidente en menos de 2 minutos, y la persona
  completa su registro en menos de 3 minutos y sin ayuda.
- **CE-003**: En el 100 % de los intentos probados, un integrante no obtiene ningún dato de un
  club al que no pertenece, ni siquiera usando directamente identificadores.
- **CE-004**: En el 100 % de los intentos probados, nadie que no sea el DESARROLLADOR obtiene
  acceso al panel de administración ni a sus operaciones.
- **CE-005**: En el 100 % de los intentos probados, nadie consigue registrarse sin una invitación
  válida.
- **CE-006**: Una persona de varios clubes cambia de club en menos de 5 segundos y la identidad
  mostrada es siempre la del club elegido; el cambio de tema se aplica en menos de 1 segundo.
- **CE-007**: Tras suspender un club y levantar la suspensión, el 100 % de sus datos e integrantes
  queda exactamente como estaba antes.
- **CE-008**: Tras eliminar un club, no queda ningún dato suyo y el 100 % de los datos de los
  demás clubes sigue intacto.
- **CE-009**: Todas las pantallas de la funcionalidad se usan sin desplazamiento horizontal en un
  teléfono y el texto se lee bien en ambos temas.

## Supuestos

- La cuenta DESARROLLADOR existe desde la puesta en marcha de la plataforma; no se crea desde
  ninguna pantalla.
- Valfor F.C. se crea como primer club usando esta misma funcionalidad, con su escudo y sus
  colores del lienzo de diseño.
- "Colores del club" significa un color principal y un color de acento, como en el lienzo. El
  resto de la paleta (fondos, texto, bordes) lo define la plataforma para cada tema.
- En esta funcionalidad todavía no hay pantallas para quitarse el rol ni para eliminar la cuenta;
  la regla del último PRESIDENTE (RF-020) se aplica y se prueba en el servidor.
- Para saber si la persona invitada ya tiene cuenta se usa el correo de la invitación.
- La elección del integrante al entrar con el correo y la entrada con el documento de un jugador
  dependen de que existan jugadores, así que se construyen con la funcionalidad de jugadores.
- La invitación caduca a los 7 días.
- Si el servicio de correo falla al crear el club, el club se crea igualmente y la invitación
  queda pendiente de reenvío desde el panel.
- Si se elimina el único club de una persona, su cuenta se elimina con él.
- El panel de administración no está en el lienzo de diseño: se construye con el mismo estilo
  visual del panel del club (menú lateral, tarjetas y tablas) y con una identidad neutra de la
  plataforma.
- Mientras no existan las funcionalidades posteriores, la aplicación del club muestra al
  PRESIDENTE una pantalla de inicio con la identidad de su club y la configuración de su club.

## Fuera de alcance

- Invitaciones enviadas desde dentro del club a directivos, entrenadores y jugadores.
- Sala de espera y aprobación de ingresos.
- Asignación y retiro de roles por el PRESIDENTE o el DIRECTIVO.
- Cambio de contraseña desde dentro de la aplicación ("olvidé mi contraseña" sí está incluido).
- Solicitud de eliminación de la propia cuenta.
- Elección de un presidente adicional por otro PRESIDENTE del club.
- Agregar un hermano desde la ficha de un jugador ya registrado.
- Cuenta de la pasarela de pago de cada club.
- Pago por el uso de la plataforma y suspensión automática por impago; en esta funcionalidad la
  suspensión es solo manual.
- Sitio público de cada club.
- Todo lo deportivo y financiero del club: jugadores, categorías, mensualidades, calendario,
  convocatorias y torneos.
