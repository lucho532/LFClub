# Especificación de funcionalidad: Ficha del jugador

**Rama de la funcionalidad**: `005-ficha-jugador`

**Creada**: 2026-10-09

**Estado**: Borrador

**Entrada**: Descripción del propietario del proyecto: "Ficha del jugador", elegida entre las
funcionalidades que la spec 004 dejó fuera de alcance ("Ficha del jugador, datos médicos y
documentos").

**Plataforma**: LFClub

**Constitución aplicable**: versión 4.1.0, en especial §1, §7.1, §7.5, §8, §9, §10, §12.3, §12.4,
§14.1, §15 y §20.

**Depende de**: spec 001 (base multiclub), spec 003 (categorías del club) y spec 004 (invitación
con rol). Los jugadores, sus categorías, la asignación de entrenadores y el retiro ya existen; esta
funcionalidad añade la ficha de cada jugador y quién puede verla y cambiarla.

## Aclaraciones

### Sesión 2026-10-09

- P: La constitución habla de la "documentación" del jugador. ¿Qué es en esta funcionalidad? → R:
  Archivos de una lista fija que sube la familia: la copia del documento de identidad y el
  certificado de afiliación a salud. El club ve a quién le falta cada uno.
- P: ¿El DIRECTIVO ve los datos clínicos del jugador (alergias, enfermedades, medicamentos)? → R:
  No. Ve la identidad, el contacto, la documentación y la seguridad social. Los datos clínicos
  solo los ven la familia, el entrenador de la categoría y el PRESIDENTE.
- P: ¿Quién puede cambiar los datos de identidad del jugador? → R: La familia cambia solo el tipo
  y el número de documento (paso de registro civil a tarjeta de identidad). Los nombres, los
  apellidos y la fecha de nacimiento solo los corrige el PRESIDENTE.

### Sesión 2026-10-09, aclaraciones

- P: ¿El ENTRENADOR puede abrir los archivos entregados por la familia de los jugadores de sus
  categorías? → R: No. No ve ni abre ningún archivo, ni sabe si están entregados o pendientes.
- P: ¿Qué puede cambiar el PRESIDENTE en la ficha de un jugador de su club, además de corregir
  nombres, apellidos y fecha de nacimiento? → R: Todo: contacto, contacto de emergencia, seguridad
  social, datos clínicos, documento de identidad y subir archivos en nombre de la familia.
- P: Como la familia y el PRESIDENTE pueden cambiar la misma ficha, ¿la ficha debe mostrar quién
  la cambió por última vez y cuándo? → R: Sí. Muestra la fecha del último cambio y quién lo hizo,
  a todos los que pueden ver la ficha. Es solo el último cambio, no un historial.
- P: ¿Hay algún dato nuevo de la ficha que la familia esté obligada a llenar? → R: No. El contacto
  de emergencia, la seguridad social y los datos clínicos son todos opcionales, y el club no
  recibe ningún aviso de datos faltantes; solo de documentos pendientes.
- P: El celular y el nombre del responsable son de la cuenta; cuando el PRESIDENTE los cambia
  desde la ficha de un jugador, ¿a quién afecta ese cambio? → R: Siguen siendo de la cuenta y el
  PRESIDENTE los cambia: el cambio se ve en todos los clubes de la persona y en todos sus
  jugadores.

## Escenarios de usuario y pruebas *(obligatorio)*

### Historia de usuario 1 - La familia consulta y mantiene la ficha de su jugador (Prioridad: P1)

La familia entra con la cuenta del jugador y abre "Mi ficha". Ve los datos de su jugador
agrupados: identidad, contacto, contacto de emergencia, seguridad social y datos clínicos. Puede
actualizar el celular, el nombre del responsable, el contacto de emergencia, la entidad de salud,
el lugar donde lo atienden y los datos clínicos. Los nombres, los apellidos y la fecha de
nacimiento los ve, pero no los cambia.

**Por qué esta prioridad**: sin los datos no hay ficha. Es la familia quien los conoce y quien
debe mantenerlos al día; todo lo demás de esta funcionalidad consiste en consultar lo que la
familia escribió aquí.

**Prueba independiente**: se inicia sesión con la cuenta de un jugador, se abre "Mi ficha", se
escriben la entidad de salud, una alergia y un contacto de emergencia, se guarda, se cierra sesión,
se vuelve a entrar y se comprueba que los datos siguen ahí.

**Escenarios de aceptación**:

1. **Dado** la cuenta de un jugador aprobado y activo, **cuando** abre "Mi ficha", **entonces** ve
   los nombres, los apellidos, el tipo y el número de documento, la fecha de nacimiento, la
   categoría y los equipos de su jugador, el correo, el celular y el nombre del responsable.
2. **Dado** la familia en la ficha, **cuando** escribe o cambia el contacto de emergencia, la
   entidad de salud, el lugar donde lo atienden, el grupo sanguíneo, las alergias, las
   enfermedades o condiciones, los medicamentos o las observaciones y guarda, **entonces** los
   datos quedan guardados y se ven la próxima vez.
3. **Dado** la familia en la ficha, **cuando** cambia el celular o el nombre del responsable,
   **entonces** el cambio queda guardado.
4. **Dado** la familia en la ficha, **cuando** deja vacío cualquier dato de salud o el contacto de
   emergencia, **entonces** la ficha se guarda igual: ninguno de esos datos es obligatorio.
5. **Dado** un jugador menor de 18 años, **cuando** la familia intenta dejar vacío el nombre del
   responsable, **entonces** el sistema no lo guarda y explica que es obligatorio.
6. **Dado** la familia en la ficha, **cuando** intenta cambiar los nombres, los apellidos o la
   fecha de nacimiento, incluso llamando directamente a la operación, **entonces** el sistema se
   lo niega y no cambia nada.
7. **Dado** la cuenta de un jugador, **cuando** intenta ver o cambiar la ficha de otro jugador,
   aunque conozca su identificador, **entonces** el sistema responde como si esa ficha no
   existiera.
8. **Dado** un jugador retirado del club, **cuando** su familia entra, **entonces** solo ve el
   aviso de que ya no está en el club y no accede a la ficha.
9. **Dado** una ficha que el PRESIDENTE acaba de cambiar, **cuando** la familia la abre,
   **entonces** ve la fecha de ese cambio y el nombre del PRESIDENTE como autor del último cambio.
10. **Dado** una ficha que nadie ha cambiado desde el registro, **cuando** alguien la abre,
    **entonces** no se muestra ningún último cambio.

---

### Historia de usuario 2 - El club consulta la ficha según el rol (Prioridad: P1)

Desde la lista de jugadores de una categoría, el PRESIDENTE, un DIRECTIVO o el entrenador de esa
categoría abre la ficha de un jugador. Cada uno ve lo que su rol le permite: el PRESIDENTE todo,
el entrenador todo menos los archivos, y el DIRECTIVO todo menos los datos clínicos.

**Por qué esta prioridad**: la ficha existe para que el entrenador sepa a quién llamar y qué le
pasa a un niño en la cancha, y para que el club tenga sus datos. El acceso restringido es la regla
central de la constitución sobre la ficha (§1 y §7.5).

**Prueba independiente**: con un jugador que tiene alergias y contacto de emergencia escritos, se
abre su ficha como entrenador de su categoría y se comprueba que se ven; se abre como DIRECTIVO y
se comprueba que los datos clínicos no aparecen; se intenta abrir como entrenador de otra
categoría y se comprueba que no se puede.

**Escenarios de aceptación**:

1. **Dado** el PRESIDENTE de un club, **cuando** abre la ficha de cualquier jugador de su club,
   con categoría, sin categoría o retirado, **entonces** ve la ficha completa.
2. **Dado** un ENTRENADOR asignado a una categoría, **cuando** abre la ficha de un jugador de esa
   categoría, de cualquier equipo, **entonces** ve la identidad, el contacto, el contacto de
   emergencia, la seguridad social y los datos clínicos.
3. **Dado** un ENTRENADOR, **cuando** intenta abrir la ficha de un jugador de una categoría que no
   tiene asignada, de un jugador sin categoría o de un jugador retirado, incluso por su
   identificador, **entonces** el sistema responde como si esa ficha no existiera.
4. **Dado** un DIRECTIVO, **cuando** abre la ficha de cualquier jugador de su club, incluidos los
   retirados, **entonces** ve la identidad, el contacto, el contacto de emergencia y la seguridad
   social.
5. **Dado** un DIRECTIVO, **cuando** consulta una ficha, incluso llamando directamente a la
   operación, **entonces** no recibe el grupo sanguíneo, las alergias, las enfermedades o
   condiciones, los medicamentos ni las observaciones.
6. **Dado** un DIRECTIVO asignado como entrenador de una categoría, **cuando** abre la ficha de un
   jugador de esa categoría, **entonces** ve lo mismo que cualquier DIRECTIVO: la asignación no le
   da acceso a los datos clínicos.
7. **Dado** un ENTRENADOR o un DIRECTIVO, **cuando** intenta cambiar cualquier dato de una ficha,
   incluso llamando directamente a la operación, **entonces** el sistema se lo niega.
8. **Dado** un jugador que cambia de categoría, **cuando** el entrenador de la categoría anterior
   intenta abrir su ficha, **entonces** ya no puede, y el de la categoría nueva sí.
9. **Dado** un integrante de un club, **cuando** intenta abrir la ficha de un jugador de otro
   club, **entonces** el sistema responde como si esa ficha no existiera.
10. **Dado** el DESARROLLADOR, **cuando** intenta consultar cualquier ficha, **entonces** el
    sistema se lo niega.
11. **Dado** un visitante sin sesión, **cuando** consulta el sitio o cualquier operación pública,
    **entonces** no recibe ningún dato de contacto, de salud ni de documento de ningún jugador.

---

### Historia de usuario 3 - La familia entrega la documentación del jugador (Prioridad: P2)

En la ficha hay un apartado "Documentos" con dos documentos pedidos: la copia del documento de
identidad y el certificado de afiliación a salud. La familia sube un archivo para cada uno, ve
cuáles ya entregó y puede reemplazarlos. El PRESIDENTE y los DIRECTIVOS abren esos archivos y ven,
en la lista de jugadores, a quién le falta alguno.

**Por qué esta prioridad**: evita que el club pida y guarde papeles por fuera de la plataforma.
La ficha ya es útil sin los archivos, por eso va después de los datos.

**Prueba independiente**: con la cuenta de un jugador se sube la copia del documento de identidad;
se entra como DIRECTIVO, se comprueba que el jugador aparece con el certificado de salud pendiente
y que el archivo entregado se puede abrir.

**Escenarios de aceptación**:

1. **Dado** la familia en la ficha, **cuando** abre "Documentos", **entonces** ve los dos
   documentos pedidos y, de cada uno, si está entregado o pendiente.
2. **Dado** un documento pendiente, **cuando** la familia sube un archivo válido, **entonces**
   queda entregado, con la fecha en que se subió, y la familia puede abrirlo.
3. **Dado** un documento entregado, **cuando** la familia sube otro archivo para ese mismo
   documento, **entonces** el nuevo reemplaza al anterior y el anterior deja de existir.
4. **Dado** un archivo que no es una imagen ni un PDF, o que supera el tamaño máximo, **cuando**
   la familia intenta subirlo, **entonces** el sistema lo rechaza, explica el motivo y conserva el
   archivo que hubiera antes.
5. **Dado** el PRESIDENTE o un DIRECTIVO, **cuando** abre la ficha de un jugador de su club,
   **entonces** ve qué documentos entregó y puede abrir cada archivo.
6. **Dado** el PRESIDENTE o un DIRECTIVO, **cuando** consulta la lista de jugadores de una
   categoría, **entonces** ve de cada jugador si tiene la documentación completa o cuántos
   documentos le faltan.
7. **Dado** un ENTRENADOR, **cuando** abre la ficha de un jugador de su categoría, **entonces** no
   ve el apartado "Documentos", no sabe qué documentos están entregados ni puede abrir ningún
   archivo, incluso llamando directamente a la operación.
8. **Dado** la cuenta de un jugador, **cuando** intenta abrir o reemplazar un archivo de otro
   jugador, **entonces** el sistema responde como si no existiera.

---

### Historia de usuario 4 - Cambio del documento de identidad y corrección de la identidad (Prioridad: P3)

Cuando el niño pasa de registro civil a tarjeta de identidad, la familia cambia en la ficha el
tipo y el número de documento. Sigue siendo el mismo jugador, con su misma categoría y su mismo
historial, y desde ese momento entra con el número nuevo. Si el club se equivocó en un nombre, un
apellido o la fecha de nacimiento, el PRESIDENTE los corrige desde la ficha. El PRESIDENTE también
puede cambiar cualquier otro dato de la ficha cuando la familia no puede hacerlo.

**Por qué esta prioridad**: ocurre pocas veces por jugador, pero hoy no hay forma de hacerlo y un
documento desactualizado impide entrar con el documento nuevo.

**Prueba independiente**: con la cuenta de un jugador registrado con registro civil se cambia a
tarjeta de identidad con otro número; se cierra sesión, se entra con el número nuevo y se
comprueba que el jugador conserva su categoría.

**Escenarios de aceptación**:

1. **Dado** la familia en la ficha, **cuando** cambia el tipo y el número de documento por otros
   válidos, **entonces** el cambio se guarda sobre el mismo jugador, que conserva su categoría,
   sus equipos y todos sus datos.
2. **Dado** un jugador al que se le cambió el documento, **cuando** inicia sesión con el número
   nuevo y su contraseña, **entonces** entra; con el número anterior ya no entra.
3. **Dado** un número de documento que ya tiene otro integrante del mismo club, activo o retirado,
   **cuando** alguien intenta ponérselo a un jugador, **entonces** el sistema no lo guarda y
   explica que ese documento ya está registrado en el club.
4. **Dado** el PRESIDENTE en la ficha de un jugador de su club, **cuando** corrige los nombres,
   los apellidos o la fecha de nacimiento, **entonces** el cambio se guarda.
5. **Dado** un jugador con categoría, **cuando** el PRESIDENTE corrige su fecha de nacimiento a
   otro año, **entonces** el jugador sigue en la categoría que tenía.
6. **Dado** un jugador sin categoría, **cuando** el PRESIDENTE corrige su fecha de nacimiento a un
   año cuya categoría está activa, **entonces** el jugador entra en esa categoría.
7. **Dado** una fecha de nacimiento futura, **cuando** el PRESIDENTE intenta guardarla,
   **entonces** el sistema no la guarda y explica el motivo.
8. **Dado** el PRESIDENTE en la ficha de un jugador de su club, **cuando** cambia el celular, el
   responsable, el contacto de emergencia, los datos de salud o sube un documento, **entonces** el
   cambio se guarda igual que si lo hubiera hecho la familia.

---

### Casos límite

- Una persona que es jugador en dos clubes tiene una ficha en cada uno. Los datos de salud, el
  contacto de emergencia y los archivos de un club no se ven ni cambian desde el otro. El celular
  y el nombre del responsable son de la cuenta y son los mismos en todos sus clubes: si la familia
  o el PRESIDENTE de uno de ellos los cambia, el cambio se ve también en los demás clubes.
- Una cuenta con varios jugadores tiene un solo celular y un solo responsable: cambiarlos en la
  ficha de un jugador los cambia para todos los jugadores de esa cuenta.
- Un jugador sin categoría no tiene entrenador: su ficha solo la ven su familia, el PRESIDENTE y
  los DIRECTIVOS.
- Un entrenador deja de estar asignado a una categoría, o la categoría se desactiva: pierde de
  inmediato el acceso a las fichas de esos jugadores.
- Un jugador retirado conserva su ficha completa y sus archivos; al reincorporarse, la familia la
  encuentra como la dejó.
- Un JUGADOR adulto usa su ficha igual que una familia; el nombre del responsable es opcional.
- Un PRESIDENTE, un DIRECTIVO o un ENTRENADOR no tienen ficha de jugador: "Mi ficha" no les
  aparece.
- Dos personas guardan cambios en la misma ficha casi a la vez: queda el último cambio guardado y
  no se corrompe ningún dato.
- En un club suspendido solo entra el PRESIDENTE (spec 001): los demás no consultan fichas
  mientras dure la suspensión.
- Falla la subida de un archivo a mitad de camino: el documento conserva el archivo anterior, o
  sigue pendiente si no había ninguno.

## Requisitos *(obligatorio)*

### Requisitos funcionales

**Contenido de la ficha**

- **RF-001**: Cada jugador aprobado de un club DEBE tener una ficha con estos grupos de datos:
  identidad (nombres, apellidos, tipo y número de documento, fecha de nacimiento, categoría y
  equipos), contacto (correo, celular y nombre del responsable), contacto de emergencia (nombre,
  parentesco y celular), seguridad social (entidad de salud y lugar donde lo atienden), datos
  clínicos (grupo sanguíneo, alergias, enfermedades o condiciones, medicamentos y observaciones)
  y documentos.
- **RF-002**: El contacto de emergencia, la seguridad social y los datos clínicos DEBEN ser
  opcionales: una ficha con esos datos vacíos es válida. El sistema no marca ni avisa a nadie de
  que falten.
- **RF-003**: La ficha DEBE pertenecer a un solo club. Los datos de salud, el contacto de
  emergencia y los documentos de un jugador en un club NO DEBEN verse ni cambiarse desde otro
  club.
- **RF-039**: El celular y el nombre del responsable DEBEN ser únicos por cuenta. Un cambio hecho
  desde la ficha de un jugador, por la familia o por el PRESIDENTE de su club, DEBE verse en todos
  los jugadores de esa cuenta y en todos los clubes a los que pertenece. Es el único dato de la
  ficha que un club puede cambiar con efecto en otro.
- **RF-004**: Solo DEBEN tener ficha los integrantes con el rol JUGADOR. Un PRESIDENTE, un
  DIRECTIVO o un ENTRENADOR no tienen ficha.

**Quién consulta**

- **RF-005**: La cuenta de un jugador DEBE poder consultar la ficha completa de su propio jugador,
  y de ningún otro.
- **RF-006**: El PRESIDENTE DEBE poder consultar la ficha completa de cualquier jugador de su
  club: con categoría, sin categoría o retirado.
- **RF-007**: Un ENTRENADOR DEBE poder consultar la ficha de los jugadores activos de las
  categorías que tiene asignadas, de cualquier equipo, con todos los grupos de datos excepto los
  documentos: no abre ningún archivo ni recibe si están entregados o pendientes.
- **RF-008**: Un ENTRENADOR NO DEBE acceder a la ficha de un jugador de una categoría que no tiene
  asignada, de un jugador sin categoría ni de un jugador retirado.
- **RF-009**: Un DIRECTIVO DEBE poder consultar la ficha de cualquier jugador de su club,
  incluidos los retirados, con la identidad, el contacto, el contacto de emergencia, la seguridad
  social y los documentos.
- **RF-010**: El sistema NO DEBE entregar a un DIRECTIVO ningún dato clínico, tampoco cuando está
  asignado como entrenador de la categoría del jugador.
- **RF-011**: El DESARROLLADOR NO DEBE acceder a ninguna ficha ni a ningún archivo de ningún club.
- **RF-012**: Cuando alguien pide una ficha o un archivo que no puede ver, el sistema DEBE
  responder igual que si no existiera, sin revelar que existe.
- **RF-013**: La cuenta de un jugador retirado NO DEBE acceder a su ficha en ese club mientras
  dure el retiro.
- **RF-014**: Ninguna operación pública DEBE devolver datos de contacto, de salud ni documentos de
  un jugador.
- **RF-015**: Todas las restricciones de consulta y de cambio DEBEN aplicarse en el servidor, por
  cada dato, y no solo ocultando elementos en pantalla.

**Quién cambia**

- **RF-016**: La cuenta de un jugador DEBE poder cambiar, de su propio jugador: el celular, el
  nombre del responsable, el contacto de emergencia, la seguridad social, los datos clínicos, los
  documentos y el tipo y número de documento de identidad.
- **RF-017**: La cuenta de un jugador NO DEBE poder cambiar los nombres, los apellidos ni la fecha
  de nacimiento.
- **RF-018**: El PRESIDENTE DEBE poder cambiar cualquier dato de la ficha de un jugador de su
  club: los nombres, los apellidos, la fecha de nacimiento, el tipo y número de documento, el
  celular, el nombre del responsable, el contacto de emergencia, la seguridad social y los datos
  clínicos, y subir o reemplazar sus documentos. No cambia el correo desde la ficha.
- **RF-019**: Un DIRECTIVO y un ENTRENADOR NO DEBEN poder cambiar ningún dato de ninguna ficha ni
  subir documentos.
- **RF-020**: El nombre del responsable DEBE seguir siendo obligatorio mientras el jugador sea
  menor de 18 años, y opcional si es adulto.
- **RF-021**: La fecha de nacimiento NO DEBE poder ser futura.
- **RF-038**: Cada cambio guardado en la ficha, incluido subir o reemplazar un documento, DEBE
  dejar registrados la fecha y el nombre de quien lo hizo, sustituyendo a los del cambio anterior.
  La ficha DEBE mostrar ese último cambio a todo el que puede verla. No se guarda historial ni el
  valor anterior de ningún dato.

**Documento de identidad**

- **RF-022**: Cambiar el tipo o el número de documento DEBE actualizar al mismo jugador, sin crear
  otro y sin alterar su categoría, sus equipos ni el resto de su ficha.
- **RF-023**: El sistema NO DEBE permitir un número de documento que ya tenga otro integrante del
  mismo club, esté activo o retirado.
- **RF-024**: Después del cambio, el inicio de sesión con documento DEBE funcionar con el número
  nuevo y dejar de funcionar con el anterior.

**Fecha de nacimiento y categoría**

- **RF-025**: Corregir la fecha de nacimiento de un jugador que ya tiene categoría NO DEBE
  cambiarlo de categoría.
- **RF-026**: Corregir la fecha de nacimiento de un jugador sin categoría DEBE ubicarlo en la
  categoría activa del año nuevo, si existe; si no existe, sigue sin categoría.

**Documentos**

- **RF-027**: La ficha DEBE pedir dos documentos: la copia del documento de identidad y el
  certificado de afiliación a salud. La lista es la misma para todos los clubes y no se amplía
  desde la aplicación.
- **RF-028**: Cada documento pedido DEBE mostrar si está entregado o pendiente y, si está
  entregado, la fecha en que se subió.
- **RF-029**: Cada documento pedido DEBE tener como máximo un archivo: subir uno nuevo reemplaza
  al anterior, que deja de existir.
- **RF-030**: El sistema DEBE aceptar solo imágenes y archivos PDF, hasta un tamaño máximo, y
  rechazar los demás explicando el motivo y sin perder el archivo anterior.
- **RF-031**: Los archivos DEBEN poder abrirlos únicamente la cuenta del jugador, el PRESIDENTE y
  los DIRECTIVOS de su club.
- **RF-032**: El PRESIDENTE y los DIRECTIVOS DEBEN ver, en las listas de jugadores de su club, si
  cada jugador tiene la documentación completa o cuántos documentos le faltan.

**Acceso desde la aplicación**

- **RF-033**: La cuenta de un jugador DEBE tener a la vista una entrada a la ficha de su jugador.
- **RF-034**: El PRESIDENTE, los DIRECTIVOS y los ENTRENADORES DEBEN poder abrir la ficha desde
  las listas de jugadores que ya ven: la de cada categoría y, el PRESIDENTE y los DIRECTIVOS,
  también la de jugadores sin categoría y la de retirados.
- **RF-035**: La ficha DEBE usarse en teléfono y en escritorio, y en tema claro y oscuro.

**Conservación**

- **RF-036**: Retirar a un jugador NO DEBE borrar ni cambiar su ficha ni sus archivos.
- **RF-037**: Cuando se borra a un jugador del club, se elimina su cuenta o se elimina el club,
  los datos de su ficha y sus archivos DEBEN borrarse con él.

### Entidades clave

- **Ficha del jugador**: los datos de un jugador en un club. Reúne la identidad y el contacto que
  ya existen desde el registro, y añade el contacto de emergencia, la seguridad social y los datos
  clínicos. Hay una por jugador y club. Guarda la fecha y el autor de su último cambio.
- **Contacto de emergencia**: persona a la que se llama si le pasa algo al jugador: nombre,
  parentesco y celular.
- **Seguridad social**: entidad de salud a la que está afiliado el jugador y lugar donde lo
  atienden.
- **Datos clínicos**: grupo sanguíneo, alergias, enfermedades o condiciones, medicamentos y
  observaciones. Son los datos más restringidos de la ficha.
- **Documento del jugador**: archivo entregado para uno de los documentos pedidos (copia del
  documento de identidad o certificado de afiliación a salud), con la fecha en que se subió.
  Pertenece a un jugador de un club.

## Criterios de éxito *(obligatorio)*

### Resultados medibles

- **CE-001**: En el 100 % de las fichas probadas, lo que la familia guarda es exactamente lo que
  ve al volver a entrar y lo que ve el PRESIDENTE.
- **CE-002**: En el 100 % de los intentos probados, una cuenta de jugador no obtiene ningún dato
  ni archivo de otro jugador.
- **CE-003**: En el 100 % de los intentos probados, un ENTRENADOR no obtiene ningún dato de un
  jugador que no está en sus categorías, ni ningún archivo de ningún jugador.
- **CE-004**: En el 100 % de los intentos probados, un DIRECTIVO no obtiene ningún dato clínico,
  esté o no asignado como entrenador.
- **CE-005**: En el 100 % de los intentos probados, un DIRECTIVO o un ENTRENADOR no consigue
  cambiar ningún dato de una ficha.
- **CE-006**: En el 100 % de los intentos probados, nadie obtiene datos ni archivos de la ficha de
  un jugador de otro club, y el DESARROLLADOR no obtiene los de ninguno.
- **CE-007**: En el 100 % de los cambios de documento probados, el jugador conserva su categoría,
  sus equipos y su ficha, y entra con el número nuevo.
- **CE-008**: En el 100 % de los intentos probados, no queda guardado un número de documento
  repetido dentro de un club.
- **CE-009**: En el 100 % de los jugadores probados, el estado de documentación que ven el
  PRESIDENTE y los DIRECTIVOS coincide con los archivos realmente entregados.
- **CE-010**: En el 100 % de los archivos no admitidos probados, el sistema los rechaza y conserva
  el archivo anterior.
- **CE-011**: La ficha se usa sin desplazamiento horizontal en un teléfono y el texto se lee bien
  en ambos temas.
- **CE-012**: En el 100 % de los cambios probados, la ficha muestra después la fecha y el autor de
  ese cambio, y no los de uno anterior.

## Supuestos

- Los datos concretos de la ficha no están en la constitución. Se toman los habituales de una
  escuela deportiva: contacto de emergencia (nombre, parentesco, celular), entidad de salud, lugar
  de atención, grupo sanguíneo, alergias, enfermedades o condiciones, medicamentos y
  observaciones.
- El DIRECTIVO ve el contacto de emergencia: es un dato de contacto, no clínico.
- El correo se muestra en la ficha pero no se cambia desde ella.
- La familia reemplaza un documento entregado, pero no lo deja de nuevo pendiente.
- No se guarda historial de cambios de la ficha ni versiones anteriores de los archivos
  (constitución §18): solo la fecha y el autor del último cambio (RF-038).
- El tamaño máximo de cada archivo y los formatos exactos de imagen se fijan en el plan.
- El jugador en espera (hermano agregado desde la ficha) todavía no existe; su ficha se tratará
  con esa funcionalidad.
- La aplicación está en construcción: los jugadores que ya existen quedan con la ficha vacía y sin
  documentos, sin ningún tratamiento especial.

## Fuera de alcance

- Agregar un hermano desde la ficha de un jugador ya registrado y elegir con cuál jugador
  continuar al entrar (constitución §12.1.2 y §12.4).
- Cambiar el correo de la cuenta.
- Foto del jugador visible para el club: la foto de perfil sigue siendo de la cuenta y solo la ve
  su dueño.
- Evaluaciones, estadísticas, asistencia, calendario y convocatorias del jugador.
- Estado de cuenta, cargos, pagos y mensualidades.
- Que cada club defina su propia lista de documentos, fechas de vencimiento de los documentos y
  avisos o recordatorios por documentación pendiente.
- Aprobación o revisión de los documentos por parte del club.
- Búsqueda de usuarios por documento y cambio de roles (constitución §12.2), incluido eliminar la
  ficha de Jugador al cambiar de rol.
- Datos de jugadores en el sitio público.
