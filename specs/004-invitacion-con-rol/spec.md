# Especificación de funcionalidad: Invitación con rol e ingreso directo al club

**Rama de la funcionalidad**: `004-invitacion-con-rol`

**Creada**: 2026-10-08

**Estado**: Borrador

**Entrada**: Descripción del propietario del proyecto: "con la adaptación a la constitución 4.0.0."

**Plataforma**: LFClub

**Constitución aplicable**: versión 4.1.0, en especial §8, §12.1, §12.1.1, §12.2, §12.3, §14.1 y
§20. La 4.1.0 nació de esta spec: deja las invitaciones y la aprobación de ingresos solo al
PRESIDENTE.

**Depende de**: spec 001 (base multiclub), spec 002 (ingreso de personas al club) y spec 003
(categorías del club). Esta funcionalidad no añade un apartado nuevo: cambia el comportamiento de
lo que construyeron la 002 y la 003 para que cumpla la constitución 4.1.0.

## Aclaraciones

### Sesión 2026-10-08

- P: Como quien se registra con una invitación ya no pasa por la sala de espera, y agregar
  hermanos todavía no existe, ¿qué hacemos con la sala de espera y sus pantallas de aprobar y
  rechazar? → R: Se conserva todo como está: siguen en "Ingresos", aunque la sala quede vacía
  hasta que existan los hermanos.
- P: ¿Qué pasa con las personas que ya estaban en la sala de espera cuando entre este cambio? →
  R: Nada: la aplicación está en construcción y no hay nadie en espera. No se construye ningún
  tratamiento para datos anteriores al cambio.
- P: Cuando el PRESIDENTE envía una invitación con el rol DIRECTIVO, ¿qué puede hacer otro
  DIRECTIVO con esa invitación? → R: La ve en la lista, pero no puede reenviarla ni cancelarla.
  (Sustituida por la sesión del plan: el DIRECTIVO ya no ve ni gestiona ninguna invitación.)
- P: En el registro, ¿a quién se le pide el nombre del padre, madre o responsable? → R: Solo en
  invitaciones de JUGADOR: obligatorio si es menor de 18 años, opcional si es adulto. A
  entrenadores y directivos no se les pide.
- P: Ahora que casi nadie pasa por una aprobación, ¿dónde ve el club quién entró, con qué rol y
  quién lo invitó? → R: En la lista de invitaciones, como invitación usada, con su rol y quién la
  envió. "Ingresos aprobados" sigue aparte y solo muestra las aprobaciones de la sala de espera.

### Sesión 2026-10-08, durante el plan

- P: ¿Quién envía invitaciones y quién aprueba o rechaza ingresos dentro del club? → R: Solo el
  PRESIDENTE, para simplificar toda la aplicación. El DIRECTIVO ya no invita, no aprueba ni
  rechaza y no entra al apartado "Ingresos". La constitución lo recoge en la versión 4.1.0.
- P: ¿El DIRECTIVO puede ver las listas del apartado "Ingresos" (invitaciones, sala de espera y
  aprobados)? → R: No. Tampoco las ve.
- P: ¿El DESARROLLADOR sigue pudiendo invitar presidentes adicionales a un club que ya existe? →
  R: No. Solo invita al primer PRESIDENTE, al crear el club. Entra en el alcance de esta
  funcionalidad.

### Sesión 2026-10-09, tras la revisión del código

- P: Una cuenta que ya existe sin responsable (se creó como entrenador o directivo) y es de una
  persona menor de 18 años acepta una invitación de JUGADOR: ¿entra sin responsable? → R: No. La
  pantalla de aceptación le pide el nombre del padre, madre o responsable antes de dejarla entrar,
  y lo guarda en su cuenta. En los demás casos la aceptación sigue sin pedir ningún dato.
  Sustituye al supuesto 3 del plan.
- P: Cuando el primer PRESIDENTE de un club se registra con la invitación del DESARROLLADOR, ¿la
  pantalla le debe pedir el nombre del padre, madre o responsable? → R: No. No se le pide ni se
  guarda: el responsable es solo para invitaciones de JUGADOR. Confirma el supuesto 1 del plan.

## Escenarios de usuario y pruebas *(obligatorio)*

### Historia de usuario 1 - El club invita indicando el rol (Prioridad: P1)

El PRESIDENTE entra al apartado "Ingresos", escribe el correo de la persona y elige con qué rol va
a entrar al club: JUGADOR, ENTRENADOR o DIRECTIVO. Nadie más del club invita. La lista de
invitaciones muestra el rol de cada una.

**Por qué esta prioridad**: desde la constitución 4.0.0 el rol viene en la invitación. Sin esto no
se puede quitar la sala de espera, porque no habría forma de saber cómo entra cada persona.

**Prueba independiente**: se inicia sesión como PRESIDENTE, se envía una invitación con el rol
ENTRENADOR y se comprueba que aparece en la lista como pendiente con ese rol; se inicia sesión como
DIRECTIVO y se comprueba que no tiene el apartado "Ingresos" ni puede invitar.

**Escenarios de aceptación**:

1. **Dado** el PRESIDENTE de un club, **cuando** va a enviar una invitación, **entonces** el
   sistema le pide el correo y el rol, y le ofrece JUGADOR, ENTRENADOR y DIRECTIVO.
2. **Dado** un DIRECTIVO de un club, **cuando** entra a la aplicación, **entonces** no ve el
   apartado "Ingresos" ni ninguna forma de invitar.
3. **Dado** un DIRECTIVO, un ENTRENADOR o un JUGADOR, **cuando** intenta enviar, ver, reenviar o
   cancelar una invitación, con cualquier rol e incluso llamando directamente a la operación,
   **entonces** el sistema se lo niega y no envía nada.
4. **Dado** el PRESIDENTE de un club, **cuando** intenta enviar una invitación con el rol
   PRESIDENTE o DESARROLLADOR, **entonces** el sistema se lo niega.
5. **Dado** alguien enviando una invitación, **cuando** no elige ningún rol, **entonces** el
   sistema no envía nada y le explica que el rol es obligatorio.
6. **Dado** una invitación enviada, **cuando** se consulta la lista de invitaciones del club,
   **entonces** aparece con su correo, su rol, su estado, quién la envió, cuándo y cuándo vence.
7. **Dado** una invitación pendiente, **cuando** se reenvía, **entonces** la invitación nueva
   conserva el rol de la anterior.
8. **Dado** un correo con una invitación pendiente, **cuando** se le invita de nuevo con otro rol,
   **entonces** la invitación anterior deja de servir y la nueva lleva el rol nuevo.
9. Retirado el 2026-10-08: el DIRECTIVO ya no ve ni gestiona ninguna invitación (escenario 3).
   Los demás escenarios conservan su número.
10. **Dado** la persona que abre el correo de la invitación, **cuando** lo lee, **entonces** el
    correo le dice a qué club la invitan y con qué rol.

---

### Historia de usuario 2 - La persona invitada se registra y entra directamente (Prioridad: P1)

La persona abre el enlace del correo. La pantalla de registro le muestra el club, su correo y el
rol con el que va a entrar. Completa sus datos, crea su contraseña y entra a la aplicación de su
club con ese rol. No pasa por ninguna sala de espera: alguien del club ya la invitó. Si entra como
JUGADOR queda, además, en la categoría de su año de nacimiento.

**Por qué esta prioridad**: es el cambio principal de la constitución 4.0.0. Elimina el paso de
aprobación para todo el que fue invitado.

**Prueba independiente**: con una invitación de JUGADOR y la categoría de su año creada, se
completa el registro y se comprueba que la persona ve de inmediato la aplicación de su club y
aparece en esa categoría; con una invitación de ENTRENADOR se comprueba que entra como ENTRENADOR
y no aparece en ninguna categoría ni en "Sin categoría".

**Escenarios de aceptación**:

1. **Dado** una invitación válida, **cuando** la persona abre el enlace, **entonces** ve el nombre
   del club, el correo y el rol de la invitación, y no puede cambiar ninguno de los tres.
2. **Dado** una invitación con el rol JUGADOR, **cuando** la persona completa el registro,
   **entonces** queda en el club con el rol JUGADOR, aprobada, y ve la aplicación de su club sin
   pantalla de espera.
3. **Dado** una invitación con el rol ENTRENADOR o DIRECTIVO, **cuando** la persona completa el
   registro, **entonces** queda en el club con ese rol como único rol, aprobada, y ve la
   aplicación de su club sin pantalla de espera.
4. **Dado** un club con la categoría activa de un año, **cuando** se registra con una invitación
   de JUGADOR una persona nacida ese año, **entonces** queda en esa categoría sin ninguna acción
   de nadie.
5. **Dado** un club sin la categoría de un año, o con ella inactiva, **cuando** se registra con
   una invitación de JUGADOR una persona nacida ese año, **entonces** el registro se completa
   igual y el jugador aparece en la lista "Sin categoría".
6. **Dado** una persona que se registra con una invitación de ENTRENADOR o DIRECTIVO, **cuando**
   se consultan las categorías, **entonces** no tiene categoría, no aparece en "Sin categoría" y
   no existe ningún dato suyo como jugador.
7. **Dado** una invitación de JUGADOR, **cuando** la persona se registra, **entonces** la pantalla
   le pide el nombre del padre, madre o responsable, obligatorio si quien ingresa es menor de 18
   años; con una invitación de ENTRENADOR, DIRECTIVO o PRESIDENTE ese dato no se pide.
8. **Dado** una persona que ya tiene cuenta por pertenecer a otro club, **cuando** acepta la
   invitación, **entonces** no se crea una segunda cuenta: queda añadida a este club con el rol de
   la invitación y entra de inmediato, sin perder nada en el otro.
9. **Dado** una persona que intenta registrarse indicando un rol distinto al de su invitación,
   incluso llamando directamente a la operación, **entonces** el sistema ignora ese dato y aplica
   el rol de la invitación.
10. **Dado** una invitación ya usada, vencida, cancelada o reemplazada, **cuando** alguien abre su
    enlace, **entonces** el sistema le explica que ya no sirve y no permite registrarse.
11. **Dado** una persona que acaba de entrar con una invitación, **cuando** el PRESIDENTE abre la
    sala de espera, **entonces** esa persona no aparece en ella.
12. **Dado** una persona menor de 18 años que ya tiene cuenta y cuya cuenta no tiene responsable,
    **cuando** va a aceptar una invitación de JUGADOR, **entonces** la pantalla le pide el nombre
    del padre, madre o responsable y no la deja entrar sin él; al indicarlo entra y el nombre
    queda guardado en su cuenta. Si es adulta, si su cuenta ya tiene responsable o si la
    invitación es de ENTRENADOR o DIRECTIVO, la aceptación no le pide nada.

---

### Historia de usuario 3 - La sala de espera queda solo para jugadores, la atiende el presidente y nadie elige rol al aprobar (Prioridad: P2)

La sala de espera deja de recibir a quien se registra con una invitación. Sigue existiendo para el
único ingreso que necesita aprobación, el hermano agregado desde la ficha, que llegará con su
propia funcionalidad. Mientras tanto queda vacía. Solo el PRESIDENTE aprueba o rechaza a quien
esté en espera, y al aprobar ya no elige rol: quien está en espera entra siempre como JUGADOR.

**Por qué esta prioridad**: cierra la puerta por la que un DIRECTIVO aprobaba ingresos y asignaba
el rol ENTRENADOR, que la constitución ya no permite. Va detrás de las dos primeras porque, una
vez hechas, la sala de espera deja de recibir personas.

**Prueba independiente**: con una persona en espera preparada como dato de prueba, se inicia
sesión como PRESIDENTE, se comprueba que al aprobar no se ofrece ningún rol y que la persona entra
como JUGADOR y queda en la categoría de su año; se comprueba que un DIRECTIVO no puede aprobarla.

**Escenarios de aceptación**:

1. **Dado** una persona en espera, **cuando** el PRESIDENTE va a aprobarla, **entonces** el
   sistema no le ofrece elegir ningún rol.
2. **Dado** una persona en espera, **cuando** se aprueba su ingreso, **entonces** queda aprobada
   con el rol JUGADOR y se ubica igual que quien entra con una invitación de JUGADOR: en la
   categoría activa de su año o en "Sin categoría".
3. **Dado** el PRESIDENTE, **cuando** intenta aprobar un ingreso indicando el rol ENTRENADOR o
   DIRECTIVO, incluso llamando directamente a la operación, **entonces** el sistema se lo niega.
4. **Dado** una persona en espera, **cuando** el PRESIDENTE la rechaza, **entonces** ocurre lo
   mismo que hasta ahora: se le pide confirmación y la persona se borra del club sin dejar datos
   suyos en él.
5. **Dado** un club sin nadie en espera, **cuando** el PRESIDENTE abre la sala de espera,
   **entonces** ve que no hay ingresos pendientes.
6. **Dado** la lista de ingresos aprobados, **cuando** se consulta después del cambio,
   **entonces** sigue mostrando las aprobaciones anteriores, con el rol con el que entró cada
   persona, quién la aprobó y cuándo.
7. **Dado** un DIRECTIVO, un ENTRENADOR o un JUGADOR, **cuando** intenta ver la sala de espera o
   los ingresos aprobados, o aprobar o rechazar un ingreso, incluso llamando directamente a la
   operación, **entonces** el sistema se lo niega y no cambia nada.

---

### Historia de usuario 4 - El desarrollador solo invita al crear el club (Prioridad: P3)

El DESARROLLADOR invita al PRESIDENTE únicamente al crear el club. En el detalle de un club ya no
puede invitar a otro presidente. Mientras esa primera invitación no se haya usado, sigue pudiendo
reenviarla o corregir su correo, para que el club no se quede sin presidente por un correo mal
escrito o un enlace vencido.

**Por qué esta prioridad**: completa la regla "dentro de la aplicación solo invita el
PRESIDENTE". No bloquea a las otras historias.

**Prueba independiente**: se inicia sesión como DESARROLLADOR, se abre un club que ya existe y se
comprueba que no hay forma de invitar a otro presidente; se crea un club nuevo y se comprueba que
su invitación se puede reenviar y corregir.

**Escenarios de aceptación**:

1. **Dado** un club que ya existe, **cuando** el DESARROLLADOR abre su detalle, **entonces** no
   se le ofrece invitar a otro presidente.
2. **Dado** un club que ya existe, **cuando** el DESARROLLADOR intenta invitar a un presidente
   llamando directamente a la operación, **entonces** la operación no existe y no se envía nada.
3. **Dado** un club recién creado cuyo presidente todavía no se registró, **cuando** el
   DESARROLLADOR reenvía la invitación o corrige su correo, **entonces** funciona como hasta
   ahora.
4. **Dado** el DESARROLLADOR creando un club, **cuando** indica el correo del presidente,
   **entonces** la invitación se envía como hasta ahora.

---

### Casos límite

- Quien envió una invitación con el rol DIRECTIVO deja de ser PRESIDENTE antes de que se use: la
  invitación sigue sirviendo, como hasta ahora.
- El PRESIDENTE invita a alguien como ENTRENADOR y lo quería como DIRECTIVO: vuelve a invitar ese
  correo con el rol DIRECTIVO y la invitación anterior deja de servir.
- Un club tiene varios presidentes: cualquiera de ellos ve, reenvía y cancela las invitaciones
  que envió otro.
- Se invita como JUGADOR a un adulto: entra como JUGADOR y queda en "Sin categoría" si no existe
  la categoría de su año.
- Un menor que ya tiene cuenta sin responsable, porque se registró como entrenador o directivo
  en otro club, acepta una invitación de JUGADOR: no entra hasta indicar el nombre de su padre,
  madre o responsable; la invitación no se gasta mientras tanto.
- Se invita por error como JUGADOR a quien debía ser entrenador y ya se registró: cambiarle el rol
  es del PRESIDENTE y pertenece a la funcionalidad de asignación de roles, todavía sin construir.
- Se registra un jugador en el mismo momento en que se crea la categoría de su año: termina en esa
  categoría, no en "Sin categoría".
- El club está suspendido: su PRESIDENTE sigue invitando con cualquiera de los tres roles. Quien
  se registra con un enlace válido queda aprobado con su rol, pero al entrar ve el aviso de
  incidencia temporal, como cualquier integrante que no es el PRESIDENTE.
- El club está dado de baja: no se puede invitar ni registrarse.
- Se invita el correo o el documento de un jugador retirado: se sigue rechazando; al jugador lo
  reincorpora el PRESIDENTE (spec 003).
- Se invita un correo que ya es integrante del club, con cualquier rol: se sigue rechazando.
- Un entrenador que además tiene un hijo en el club: el club envía dos invitaciones a dos correos
  distintos, una de ENTRENADOR y otra de JUGADOR (constitución §8).
- La invitación de PRESIDENTE que el DESARROLLADOR envía al crear el club solo cambia en una cosa:
  su registro deja de pedir el nombre del padre, madre o responsable. Ya entraba directamente.
- El primer presidente nunca se registra y su enlace vence: el DESARROLLADOR reenvía la invitación
  o corrige el correo; no necesita invitar a "otro" presidente.
- Un club necesita un segundo presidente: lo elige su PRESIDENTE dándole el rol a alguien ya
  registrado, funcionalidad todavía sin construir. Hasta entonces el club tiene un solo
  presidente.

## Requisitos *(obligatorio)*

### Requisitos funcionales

**Invitación con rol**

- **RF-001**: Toda invitación del club DEBE indicar el rol con el que entra la persona. El rol es
  obligatorio y no se puede cambiar después de enviarla.
- **RF-002**: Dentro del club, solamente el PRESIDENTE DEBE poder enviar, reenviar y cancelar
  invitaciones, con el rol JUGADOR, ENTRENADOR o DIRECTIVO. Un DIRECTIVO, un ENTRENADOR o un
  JUGADOR NO DEBEN poder hacerlo con ningún rol. Nadie dentro del club DEBE poder invitar con el
  rol PRESIDENTE. El sistema DEBE comprobarlo en el servidor.
- **RF-003**: La lista de invitaciones del club DEBE mostrar el rol de cada invitación, junto a
  los datos que ya mostraba. Solamente el PRESIDENTE la ve, y ve todas las del club.
- **RF-004**: Reenviar una invitación DEBE conservar su rol. Invitar de nuevo un correo con una
  invitación pendiente DEBE invalidar la anterior y aplicar el rol de la nueva.
- **RF-005**: Retirado el 2026-10-08: lo cubre RF-002, porque el DIRECTIVO ya no gestiona ninguna
  invitación. Los demás requisitos conservan su número.
- **RF-006**: El correo de la invitación y la pantalla de registro DEBEN mostrar el rol con el que
  entra la persona.
- **RF-007**: Retirado el 2026-10-08: no hay invitaciones ni personas en espera anteriores al
  cambio que tratar. Los demás requisitos conservan su número.

**Ingreso directo**

- **RF-008**: Quien se registra con una invitación válida DEBE quedar en ese club con el rol de la
  invitación, como único rol, y en estado APROBADO desde ese momento. NO DEBE pasar por la sala de
  espera ni necesitar la aprobación de nadie.
- **RF-009**: El rol con el que entra la persona lo determina solamente la invitación. El sistema
  NO DEBE aceptar un rol indicado por quien se registra.
- **RF-010**: Si el correo invitado ya tiene cuenta en la plataforma, el sistema NO DEBE crear una
  segunda: la persona queda añadida a este club con el rol de la invitación y entra directamente,
  conservando su único inicio de sesión y todo lo que tiene en sus otros clubes.
- **RF-011**: Quien entra con una invitación de JUGADOR DEBE quedar ubicado, al registrarse, en la
  categoría activa de su año de nacimiento. Si el club no la tiene activa, DEBE quedar sin
  categoría y entrar en ella cuando el PRESIDENTE la cree o la reactive. No se le asigna ningún
  equipo.
- **RF-012**: Quien entra con una invitación de ENTRENADOR o DIRECTIVO NO DEBE tener categoría, ni
  aparecer en "Sin categoría", ni quedar registrado como jugador en ningún momento.
- **RF-013**: El registro DEBE pedir el nombre del padre, madre o responsable solamente cuando la
  invitación es de JUGADOR; es obligatorio si quien ingresa es menor de 18 años el día del
  registro y opcional si es adulto. Con una invitación de ENTRENADOR o DIRECTIVO, y con la de
  PRESIDENTE que envía el DESARROLLADOR, el sistema NO DEBE pedirlo ni guardarlo. El sistema DEBE
  comprobarlo en el servidor. Los demás datos del registro no cambian.
- **RF-014**: Las demás reglas de la invitación y del registro NO cambian: un solo uso, caducidad,
  correo fijo, documento único en el club, y rechazo de correos que ya son integrantes, de
  jugadores retirados y del correo del DESARROLLADOR.
- **RF-026**: Añadido el 2026-10-09. Un JUGADOR menor de 18 años NO DEBE entrar a un club sin
  responsable, tampoco al aceptar una invitación con una cuenta que ya existe. Cuando la
  invitación es de JUGADOR, la persona es menor de 18 años el día de la aceptación y su cuenta no
  tiene responsable, la aceptación DEBE pedir ese nombre, exigirlo en el servidor y guardarlo en
  la cuenta; sin él no entra y la invitación no se gasta. En cualquier otro caso la aceptación NO
  DEBE pedir ningún dato, y un responsable enviado de más se ignora.

**Sala de espera y aprobación**

- **RF-015**: Ningún registro con invitación DEBE dejar a nadie en la sala de espera. La sala de
  espera, la aprobación y el rechazo DEBEN seguir visibles en el apartado "Ingresos" y funcionando
  para quien ya está en espera, aunque la sala esté vacía. El apartado "Ingresos" DEBE ser
  exclusivo del PRESIDENTE.
- **RF-016**: Al aprobar un ingreso en espera NO DEBE poder elegirse rol: la persona aprobada
  queda siempre con el rol JUGADOR y se ubica según RF-011. El sistema DEBE rechazar, en el
  servidor, una aprobación que indique otro rol.
- **RF-017**: Solamente el PRESIDENTE DEBE poder ver la sala de espera y los ingresos aprobados y
  aprobar o rechazar un ingreso. Un DIRECTIVO NO DEBE poder invitar, aprobar ni rechazar, ni
  asignar, cambiar o retirar el rol de nadie por ningún camino. El sistema DEBE comprobarlo en el
  servidor.
- **RF-018**: El rechazo de una persona en espera solo cambia en quién lo hace: el PRESIDENTE, con
  confirmación, y borra a la persona del club.
- **RF-019**: La lista de ingresos aprobados DEBE seguir mostrando las aprobaciones ya
  registradas, sin modificarlas. Quien entra directamente con una invitación no genera una
  aprobación: el club lo ve en la lista de invitaciones, como invitación usada, con su rol y quién
  la envió.

**Aislamiento y generales**

- **RF-020**: El aislamiento entre clubes NO cambia: las invitaciones y los ingresos de un club
  son invisibles e inaccesibles para cualquier persona de otro club, también por identificador.
- **RF-021**: En un club suspendido, su PRESIDENTE DEBE poder seguir invitando con cualquiera de
  sus tres roles, y una invitación válida DEBE seguir permitiendo registrarse. En un club dado de
  baja no se puede invitar ni registrarse.
- **RF-022**: Las pantallas modificadas DEBEN seguir funcionando en teléfono y en escritorio, en
  tema claro y oscuro, con la identidad del club y en español.
- **RF-023**: Lo construido en las specs 001, 002 y 003 que esta funcionalidad no menciona DEBE
  seguir funcionando igual.

**Invitación del presidente**

- **RF-024**: El DESARROLLADOR DEBE invitar al PRESIDENTE solamente al crear el club. NO DEBE
  poder invitar a un presidente a un club que ya existe; esa operación deja de existir.
- **RF-025**: El DESARROLLADOR DEBE seguir pudiendo reenviar la invitación del presidente o
  corregir su correo mientras no se haya usado.

### Requisitos de specs anteriores que esta funcionalidad reemplaza

- **Spec 002, RF-001, RF-020, RF-021, RF-025a y RF-027** (el PRESIDENTE y los DIRECTIVOS invitan,
  ven la sala de espera y los aprobados, aprueban y rechazan): desde ahora solo el PRESIDENTE,
  según RF-002, RF-003 y RF-017.
- **Spec 001, RF-012** (el DESARROLLADOR puede invitar a un presidente adicional a un club que ya
  existe): esa parte la retira RF-024; reenviar y corregir el correo se conservan (RF-025).
- **Spec 002, RF-002 y RF-003** (la invitación solo lleva el correo y no lleva rol): los
  reemplazan RF-001 y RF-002.
- **Spec 002, RF-013 y RF-014** (quien se registra queda como JUGADOR y EN_ESPERA): los reemplazan
  RF-008 y RF-010.
- **Spec 002, RF-010** (cuándo se pide el responsable): lo ajusta RF-013.
- **Spec 002, RF-022** (elegir rol al aprobar): lo reemplazan RF-016 y RF-017.
- **Spec 002, RF-030** (en un club suspendido quien se registra queda en espera): lo reemplaza
  RF-021.
- **Spec 003, RF-008 y RF-009** (ubicación en la categoría al aprobarse el ingreso): la ubicación
  ocurre ahora al registrarse, según RF-011; para quien se aprueba desde la sala de espera sigue
  ocurriendo al aprobar, según RF-016.

### Entidades clave

- **Invitación del club**: gana el rol con el que entra la persona (JUGADOR, ENTRENADOR o
  DIRECTIVO). El resto no cambia.
- **Integrante**: quien entra con una invitación nace ya APROBADO y con el rol de su invitación.
- **Estado de ingreso**: sigue siendo EN_ESPERA o APROBADO. EN_ESPERA queda reservado para el
  jugador agregado desde la ficha de un hermano.
- **Aprobación del ingreso**: ya no guarda una decisión de rol; quien se aprueba desde la sala de
  espera es siempre JUGADOR. Las aprobaciones anteriores se conservan como están.
- **Jugador**: solo lo es quien entra con una invitación de JUGADOR o se aprueba desde la sala de
  espera. Se ubica en su categoría al entrar.

## Criterios de éxito *(obligatorio)*

### Resultados medibles

- **CE-001**: En el 100 % de los registros probados con una invitación válida, la persona ve la
  aplicación de su club al terminar el registro, sin pantalla de espera y sin que nadie la
  apruebe.
- **CE-002**: En el 100 % de los registros probados, la persona queda con el rol de su invitación
  y con ningún otro.
- **CE-003**: En el 100 % de los intentos probados, un DIRECTIVO no consigue enviar ni gestionar
  una invitación, aprobar ni rechazar un ingreso, ni cambiar el rol de nadie.
- **CE-004**: En el 100 % de los registros probados con una invitación de JUGADOR cuya categoría
  de año existe y está activa, el jugador queda en ella sin ninguna acción adicional.
- **CE-005**: En el 100 % de los registros probados con una invitación de ENTRENADOR o DIRECTIVO,
  la persona no tiene categoría ni ningún dato como jugador.
- **CE-006**: Después del cambio, ningún registro con invitación deja a nadie en la sala de
  espera.
- **CE-007**: En el 100 % de las aprobaciones probadas desde la sala de espera, la persona queda
  como JUGADOR.
- **CE-008**: Todas las pruebas de las specs 001, 002 y 003 que no contradicen la constitución
  4.1.0 siguen pasando.
- **CE-009**: Las pantallas modificadas se usan sin desplazamiento horizontal en un teléfono y el
  texto se lee bien en ambos temas.
- **CE-010**: En el 100 % de los intentos probados, el DESARROLLADOR no consigue invitar a un
  presidente a un club que ya existe.
- **CE-011**: En el 100 % de las aceptaciones probadas, un menor de 18 años no queda como JUGADOR
  de un club sin que su cuenta tenga responsable.

## Supuestos

- "La adaptación a la constitución 4.0.0" es lo que la propia enmienda deja pendiente: adaptar el
  código de la spec 002 (sala de espera, aprobación y rechazo) y el de las invitaciones. Lo demás
  que trae la 4.0.0 son funcionalidades que todavía no existen y quedan fuera de alcance.
- La sala de espera no se elimina ni se oculta, por decisión del propietario: la constitución
  (§12.1.1) la conserva para el hermano agregado desde la ficha. Sus pantallas y operaciones se
  mantienen, aunque dejen de recibir personas hasta que exista esa funcionalidad.
- Entrar como JUGADOR empieza a generar la mensualidad (constitución §12.1), pero las
  mensualidades todavía no existen: llegará con su funcionalidad.
- No se envía ningún correo ni aviso al club cuando alguien completa su registro: se ve en la
  lista de invitaciones.
- La aplicación está en construcción: no hay clubes en producción, nadie en espera ni
  invitaciones reales pendientes. No se construye ninguna migración ni regla especial para datos
  anteriores al cambio; los datos de desarrollo se pueden recrear.

## Fuera de alcance

- Agregar un hermano desde la ficha de un jugador ya registrado, y elegir con cuál jugador
  continuar al entrar (constitución §12.1.2 y §12.4).
- Búsqueda de usuarios por documento y cambio o retiro de roles a personas ya registradas
  (constitución §12.2), incluido eliminar la ficha de Jugador al cambiar de rol.
- Mensualidades, cargos y el valor de la mensualidad por jugador (constitución §16.7).
- Elección de un presidente adicional por otro PRESIDENTE del club.
- Ficha del jugador, datos médicos y documentos.
- Cualquier cambio en la invitación de PRESIDENTE que envía el DESARROLLADOR al crear el club
  distinto de los de RF-013, RF-024 y RF-025, y en quitarle el rol a un presidente.

---

**Nota de seguimiento (2026-10-20)**: lo que este documento deja pendiente sobre mensualidades, cargos,
pagos y el estado de cuenta lo construye la funcionalidad
[015-mensualidades-cargos-pagos](../015-mensualidades-cargos-pagos/spec.md). Este documento no se reescribe.
