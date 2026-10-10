# Especificación de funcionalidad: Nombres clicables y ficha básica de entrenadores, presidentes y directivos

**Rama de la funcionalidad**: `008-fichas-de-personas`

**Creada**: 2026-10-10

**Estado**: Borrador

**Entrada**: Descripción del propietario del proyecto: "Todos los nombres de personas de la
aplicación son clicables y abren la ficha de esa persona. Para un jugador, su ficha actual
(005/007). Para un entrenador, presidente o directivo, una ficha básica nueva: nombre, rol, foto de
perfil y las categorías y equipos que entrena, sin documento ni datos de contacto. La ficha básica
la puede abrir cualquier integrante del mismo club, también la familia de un jugador (esto deja
sin efecto RF-035 de la 003). Revisar si hace falta enmendar la constitución porque hoy la foto de
perfil solo la ve su dueño. Una duda que tengo es que cuando doy clic arriba en el nombre solo me
da la opción de modificar o subir la foto pero no me permite modificar los datos personales y
debería dejarme."

**Plataforma**: La Pecosa

**Constitución aplicable**: versión 4.5.0, en especial §7.1, §7.5, §8, §10, §12.3, §12.6, §14.2,
§15, §20 y §24.

**Depende de**: spec 001 (foto de perfil y "Mi perfil"), spec 003 (categorías, equipos y
asignación de entrenadores), spec 005 (ficha del jugador) y spec 007 (lista de jugadores, detalle
con foto del jugador e inicio por rol).

**Deja sin efecto**:

- En la spec 003, la parte del RF-035 que dice que la cuenta de jugador ve de los entrenadores
  solo "el nombre y los apellidos" y "NO DEBE ver ningún otro dato de los entrenadores". A partir
  de esta funcionalidad la familia también abre la ficha básica de los entrenadores de su club.
- En la spec 001, la frase del RF-038 "Nadie DEBE poder obtener la foto de perfil de otra cuenta",
  para las cuentas con rol PRESIDENTE, DIRECTIVO o ENTRENADOR en un club: su foto de perfil la ven
  los integrantes de ese club en su ficha básica.

## Aclaraciones

### Sesión 2026-10-10

- P: ¿Qué datos personales puede cambiar un PRESIDENTE, DIRECTIVO o ENTRENADOR por sí mismo? → R:
  Celular, nombres, apellidos y fecha de nacimiento. El tipo y el número de documento los ve en
  "Mi perfil" pero no los cambia, porque con ellos se inicia sesión.

## Impacto en la constitución

Esta funcionalidad contradecía dos textos de la versión 4.4.0: §14.2 ("la foto de perfil [...]
solo la ve su dueño") y §8, rol Jugador ("De un entrenador solamente ve el nombre y los
apellidos"). La enmienda 4.5.0 (2026-10-10) ya los corrigió y añadió §12.6, que describe la ficha
básica y los datos que un PRESIDENTE, DIRECTIVO o ENTRENADOR cambia por sí mismo.

## Escenarios de usuario y pruebas *(obligatorio)*

### Historia de usuario 1 - Ver la ficha básica de un entrenador, presidente o directivo (Prioridad: P1)

Cualquier integrante del club, también la familia de un jugador, abre la ficha básica de un
entrenador, un presidente o un directivo de su club y ve su nombre completo, su rol en el club, su
foto de perfil (o sus iniciales si no tiene) y las categorías y equipos que entrena. No ve su
documento, su fecha de nacimiento, su correo ni su celular.

**Por qué esta prioridad**: es lo nuevo de esta funcionalidad. Sin la ficha básica, los nombres de
entrenadores, presidentes y directivos no tienen a dónde llevar, y la familia no puede ponerle cara
al entrenador de su hijo.

**Prueba independiente**: con un ENTRENADOR que dirige dos equipos de una categoría, un DIRECTIVO
que entrena otra categoría y un PRESIDENTE que no entrena, se abre la ficha básica de cada uno como
familia, como ENTRENADOR, como DIRECTIVO y como PRESIDENTE, y se comprueba qué muestra y qué no.

**Escenarios de aceptación**:

1. **Dado** un ENTRENADOR con foto de perfil asignado a una categoría y a dos de sus equipos,
   **cuando** cualquier integrante de su club abre su ficha básica, **entonces** ve su nombre
   completo, el rol "Entrenador", su foto y esa categoría con los dos equipos.
2. **Dado** un DIRECTIVO o un PRESIDENTE asignado como entrenador de una categoría, **cuando** se
   abre su ficha básica, **entonces** ve su rol (Directivo o Presidente) y la categoría y los
   equipos que entrena.
3. **Dado** un PRESIDENTE o un DIRECTIVO que no entrena, **cuando** se abre su ficha básica,
   **entonces** ve su nombre, su rol y su foto, sin sección de categorías.
4. **Dado** un ENTRENADOR que todavía no tiene categorías asignadas, **cuando** se abre su ficha
   básica, **entonces** se indica que todavía no tiene categorías asignadas.
5. **Dado** una persona sin foto de perfil, **cuando** se abre su ficha básica, **entonces** se
   muestran sus iniciales en el lugar de la foto.
6. **Dado** cualquier ficha básica, **cuando** la abre cualquier rol, **entonces** no aparecen el
   tipo ni el número de documento, la fecha de nacimiento, el correo ni el celular, y el sistema
   tampoco los entrega por ningún otro camino.
7. **Dado** una cuenta de jugador, **cuando** abre la ficha básica de un entrenador, presidente o
   directivo de su club, aunque no entrene la categoría de su jugador, **entonces** la ve completa.
8. **Dado** un integrante de otro club, o el DESARROLLADOR, **cuando** intenta abrir la ficha
   básica de alguien de este club, aunque conozca su identificador, **entonces** el sistema se lo
   niega sin revelar si la persona existe, y tampoco le entrega su foto de perfil.
9. **Dado** una persona que es PRESIDENTE en un club y ENTRENADOR en otro, **cuando** se abre su
   ficha básica desde cada club, **entonces** cada una muestra solo el rol, las categorías y los
   equipos de ese club, con la misma foto de perfil.

---

### Historia de usuario 2 - Todo nombre de persona lleva a su ficha (Prioridad: P1)

En cualquier pantalla del club donde aparece el nombre de una persona del club, el nombre se puede
pulsar y abre su ficha: la ficha del jugador (specs 005 y 007) si es un jugador, o la ficha básica
(historia 1) si es un entrenador, presidente o directivo. Si quien mira no tiene permiso para
abrir esa ficha, el nombre se muestra como texto normal, sin aspecto de enlace.

**Por qué esta prioridad**: es la forma de llegar a las fichas. Sin ella, la historia 1 solo se
alcanzaría escribiendo una dirección.

**Prueba independiente**: se recorre cada pantalla que muestra nombres de personas (inicio, lista
de jugadores, categorías y su detalle, entrenadores asignados, ingresos y sala de espera,
retirados, "último cambio" de la ficha) con cada rol, y se comprueba a dónde lleva cada nombre y
cuáles no son enlace.

**Escenarios de aceptación**:

1. **Dado** la familia en su inicio, **cuando** pulsa el nombre de un entrenador de la categoría de
   su jugador, **entonces** se abre la ficha básica de ese entrenador.
2. **Dado** el PRESIDENTE, un DIRECTIVO o un ENTRENADOR en una pantalla que muestra jugadores que
   puede ver (lista de jugadores, detalle de una categoría, inicio del entrenador), **cuando** pulsa
   el nombre de un jugador, **entonces** se abre el detalle de ese jugador con lo que su rol puede
   ver (spec 007).
3. **Dado** cualquier rol en el detalle de una categoría o en un resumen que muestra entrenadores,
   **cuando** pulsa el nombre de un entrenador, **entonces** se abre su ficha básica.
4. **Dado** el nombre de un jugador que quien mira no puede abrir (por ejemplo, el ENTRENADOR que
   ve en algún lugar a un jugador de otra categoría), **cuando** se muestra, **entonces** aparece
   como texto normal y no lleva a ninguna parte.
5. **Dado** el nombre de una persona que ya no pertenece al club (por ejemplo, quien hizo el último
   cambio de una ficha y luego salió del club), **cuando** se muestra, **entonces** aparece como
   texto normal.
6. **Dado** el PRESIDENTE o un DIRECTIVO en la lista de retirados, **cuando** pulsa el nombre de un
   jugador retirado, **entonces** se abre su ficha, como ya permite la spec 005.
7. **Dado** el PRESIDENTE en la sala de espera, **cuando** pulsa el nombre de un hermano que espera
   aprobación, **entonces** se abre la ficha que hoy le permite revisar antes de decidir.
8. **Dado** una ficha abierta desde un nombre, **cuando** quien la consulta vuelve atrás,
   **entonces** regresa a la pantalla desde la que la abrió.
9. **Dado** el propio nombre de quien mira en una pantalla del club, **cuando** lo pulsa,
   **entonces** abre su propia ficha (la del jugador elegido, o su ficha básica).

---

### Historia de usuario 3 - La familia ve la cara de los entrenadores de su jugador (Prioridad: P2)

En el inicio de la familia, cada entrenador de la categoría de su jugador aparece con su foto de
perfil en miniatura (o sus iniciales), su nombre y el equipo que dirige, y desde ahí abre su ficha
básica.

**Por qué esta prioridad**: es el uso más frecuente de la ficha básica para la familia, pero sin
la miniatura la historia 2 ya le da el acceso.

**Prueba independiente**: con un jugador de una categoría que tiene dos entrenadores, uno con foto
y otro sin ella, se entra con la cuenta del jugador y se comprueba la foto de uno y las iniciales
del otro.

**Escenarios de aceptación**:

1. **Dado** la familia de un jugador de una categoría con entrenadores, **cuando** entra al inicio,
   **entonces** ve a cada entrenador con su foto en miniatura (o sus iniciales), su nombre y el
   equipo que dirige.
2. **Dado** la familia de un jugador sin categoría, **cuando** entra al inicio, **entonces** sigue
   viendo que todavía no tiene categoría, sin entrenadores.

---

### Historia de usuario 4 - Cambiar mis datos personales desde "Mi perfil" (Prioridad: P2)

Al pulsar su nombre en la barra superior y elegir "Mi perfil", la persona hoy solo puede cargar,
cambiar o quitar su foto de perfil. Con esta funcionalidad, un PRESIDENTE, un DIRECTIVO o un
ENTRENADOR ve además sus datos personales (nombres, apellidos, tipo y número de documento, fecha
de nacimiento, correo y celular) y puede cambiar los que se le permitan. Una cuenta de jugador
encuentra en "Mi perfil" el acceso a "Mi ficha", donde ya se cambian los datos de su jugador y el
contacto de la cuenta (spec 005).

La persona cambia por sí misma su celular, sus nombres, sus apellidos y su fecha de nacimiento.
El tipo y el número de documento los ve, pero no los cambia, porque con ellos se inicia sesión.

**Por qué esta prioridad**: hoy un entrenador, presidente o directivo no tiene ningún lugar donde
ver ni corregir sus propios datos; cualquier error del registro se queda para siempre. No bloquea
las historias 1 a 3.

**Prueba independiente**: un ENTRENADOR entra a "Mi perfil", ve sus datos, cambia los permitidos
y se comprueba que el cambio se ve en su ficha básica y en todos sus clubes, y que los no
permitidos no se pueden cambiar.

**Escenarios de aceptación**:

1. **Dado** un PRESIDENTE, DIRECTIVO o ENTRENADOR, **cuando** abre "Mi perfil", **entonces** ve su
   foto de perfil y sus datos personales: nombres, apellidos, tipo y número de documento, fecha de
   nacimiento, correo y celular.
2. **Dado** un ENTRENADOR en "Mi perfil", **cuando** cambia un dato que se le permite y guarda,
   **entonces** el cambio queda guardado, se confirma, y se ve en su ficha básica y en las
   pantallas de todos sus clubes.
3. **Dado** un ENTRENADOR en "Mi perfil", **cuando** deja vacío un dato obligatorio o escribe un
   valor no válido, **entonces** el sistema lo rechaza, explica el motivo y conserva el valor
   anterior.
4. **Dado** un PRESIDENTE, DIRECTIVO o ENTRENADOR en "Mi perfil", **cuando** mira su tipo y
   número de documento, **entonces** los ve sin poder editarlos, y si intenta cambiarlos por otro
   camino el sistema se lo niega.
5. **Dado** una cuenta de jugador, **cuando** abre "Mi perfil", **entonces** ve su foto de perfil y
   un acceso a "Mi ficha" del jugador elegido, y no ve un segundo formulario con los mismos datos.
6. **Dado** el correo de la cuenta, **cuando** la persona abre "Mi perfil", **entonces** lo ve sin
   poder cambiarlo desde ahí.

---

### Casos límite

- A un ENTRENADOR le quitan una categoría o un equipo: su ficha básica deja de mostrarlos en la
  siguiente consulta.
- Una categoría o un equipo desactivados no aparecen en la ficha básica.
- Al PRESIDENTE le cambian el rol de alguien (por ejemplo, de ENTRENADOR a DIRECTIVO): la ficha
  básica muestra el rol nuevo en la siguiente consulta.
- Una cuenta pasa de JUGADOR a ENTRENADOR o DIRECTIVO (§12.2): sus nombres en pantallas antiguas
  dejan de llevar a la ficha del jugador eliminada y pasan a llevar a su ficha básica.
- Alguien deja el club o se le quita el rol: su ficha básica se niega desde ese club y su nombre
  se muestra como texto normal.
- La persona cambia o quita su foto de perfil: la ficha básica muestra la nueva o las iniciales en
  la siguiente consulta, en todos sus clubes.
- Una persona que es ENTRENADOR en un club y tiene en otro club una cuenta de jugador con la misma
  cuenta: en el club donde es jugador nadie ve su foto de perfil por esta funcionalidad, porque
  allí no tiene ficha básica.
- Un club suspendido: solo entra su PRESIDENTE, que puede abrir las fichas como siempre.
- Un nombre muy largo no debe romper la ficha básica ni los enlaces, en teléfono ni en escritorio.

## Requisitos *(obligatorio)*

### Requisitos funcionales

**Ficha básica**

- **RF-001**: El sistema DEBE ofrecer una ficha básica de cada integrante del club con rol
  PRESIDENTE, DIRECTIVO o ENTRENADOR, que muestre solo: nombre completo, rol en ese club, foto de
  perfil (o iniciales si no tiene) y las categorías activas que entrena en ese club con los
  equipos activos que dirige en cada una.
- **RF-002**: La ficha básica NO DEBE mostrar ni entregar el tipo y número de documento, la fecha
  de nacimiento, el correo, el celular ni ningún otro dato de la cuenta.
- **RF-003**: La ficha básica DEBE poder abrirla cualquier integrante del mismo club con cualquier
  rol, incluida la cuenta de un jugador, sin límite de categoría. Este permiso DEBE aplicarlo el
  sistema y no solo la pantalla.
- **RF-004**: El sistema DEBE negar la ficha básica y la foto de perfil a quien no pertenece al
  club de la persona, incluido el DESARROLLADOR, aunque conozca su identificador, sin revelar si
  la persona existe.
- **RF-005**: La ficha básica de una persona con roles en varios clubes DEBE mostrar, desde cada
  club, solo el rol, las categorías y los equipos de ese club.
- **RF-006**: Cuando la persona no entrena ninguna categoría, la ficha DEBE omitir la sección de
  categorías si es PRESIDENTE o DIRECTIVO, y DEBE indicar que todavía no tiene categorías
  asignadas si es ENTRENADOR.

**Foto de perfil**

- **RF-007**: La foto de perfil de una cuenta con rol PRESIDENTE, DIRECTIVO o ENTRENADOR en un club
  DEBE poder verla cualquier integrante de ese club, únicamente a través de su ficha básica y de
  las miniaturas que esta funcionalidad muestra junto a su nombre.
- **RF-008**: La foto de perfil de una cuenta que solo es cuenta de jugador DEBE seguir siendo
  privada: solo la ve su dueño. La foto que el club ve de un jugador sigue siendo la foto del
  jugador (spec 007).
- **RF-009**: Solo el dueño de la cuenta DEBE poder cargar, cambiar o quitar su foto de perfil,
  con las reglas de la spec 001 (PNG, JPEG o WebP de hasta 1 MB).

**Nombres que llevan a la ficha**

- **RF-010**: En toda pantalla del club donde aparece el nombre de una persona del club, ese nombre
  DEBE abrir la ficha de la persona: la del jugador (specs 005 y 007) si es un jugador, o la ficha
  básica si es PRESIDENTE, DIRECTIVO o ENTRENADOR.
- **RF-011**: Un nombre DEBE mostrarse como texto normal, sin aspecto ni comportamiento de enlace,
  cuando quien mira no puede abrir esa ficha o la persona ya no pertenece al club.
- **RF-012**: Abrir una ficha desde un nombre NO DEBE ampliar lo que cada rol puede ver de un
  jugador (specs 005 y 007). Que el nombre sea pulsable no concede acceso.
- **RF-013**: Desde una ficha abierta por un nombre, volver atrás DEBE regresar a la pantalla de
  origen.
- **RF-014**: El inicio de la familia DEBE mostrar junto al nombre de cada entrenador de la
  categoría de su jugador su foto de perfil en miniatura (o sus iniciales).

**Mis datos personales**

- **RF-015**: "Mi perfil" DEBE mostrar a un PRESIDENTE, DIRECTIVO o ENTRENADOR sus datos
  personales: nombres, apellidos, tipo y número de documento, fecha de nacimiento, correo y
  celular.
- **RF-016**: La persona DEBE poder cambiar por sí misma su celular, sus nombres, sus apellidos y
  su fecha de nacimiento. NO DEBE poder cambiar el tipo ni el número de documento. Los cambios DEBEN validarse con las mismas
  reglas del registro (§10 y §12.1) y aplicarse en todos los clubes de la persona.
- **RF-017**: "Mi perfil" de una cuenta de jugador DEBE llevar a "Mi ficha" del jugador elegido en
  lugar de repetir sus datos; lo que la familia puede cambiar sigue siendo lo de la spec 005.
- **RF-018**: El correo DEBE mostrarse en "Mi perfil" sin poder cambiarse desde ahí.
- **RF-019**: El tipo y el número de documento DEBEN mostrarse de solo lectura en "Mi perfil", y el
  sistema DEBE negar su cambio por cualquier camino.

**Aislamiento**

- **RF-020**: Ninguna ficha, foto, enlace ni dato de esta funcionalidad DEBE incluir ni revelar
  datos de otro club.

### Entidades clave

- **Ficha básica**: vista pública dentro del club de un PRESIDENTE, DIRECTIVO o ENTRENADOR: nombre
  completo, rol en el club, foto de perfil y categorías y equipos que entrena. No es un dato nuevo:
  se arma con los que ya existen y depende del club desde el que se consulta.
- **Foto de perfil**: imagen de la cuenta (spec 001). Deja de ser privada para las cuentas con rol
  PRESIDENTE, DIRECTIVO o ENTRENADOR, solo ante los integrantes de sus clubes.
- **Datos personales del integrante**: nombres, apellidos, documento, fecha de nacimiento y
  celular de quien no es jugador, que hoy solo se dan al registrarse.

## Criterios de éxito *(obligatorio)*

### Resultados medibles

- **CE-001**: En el 100 % de los casos probados, la ficha básica muestra exactamente nombre, rol,
  foto (o iniciales) y categorías y equipos activos que entrena, y ningún dato de documento,
  nacimiento ni contacto.
- **CE-002**: En el 100 % de los intentos probados, nadie de otro club ni el DESARROLLADOR obtiene
  una ficha básica ni una foto de perfil de un integrante de este club.
- **CE-003**: En el 100 % de los intentos probados, la foto de perfil de una cuenta que solo es de
  jugador no la obtiene nadie más que su dueño.
- **CE-004**: En el recorrido por todas las pantallas del club con cada rol, el 100 % de los
  nombres de personas del club que el rol puede abrir llevan a la ficha correcta, y ninguno de los
  que no puede abrir aparece como enlace.
- **CE-005**: En el 100 % de los casos probados, abrir un jugador desde un nombre muestra a cada
  rol exactamente lo mismo que la spec 005 le permite ver.
- **CE-006**: En el 100 % de los cambios probados en "Mi perfil", los datos permitidos quedan
  guardados y se ven en todos los clubes de la persona, y los no permitidos se rechazan.
- **CE-007**: La ficha básica y "Mi perfil" se leen y se usan sin desplazamiento horizontal de la
  página en teléfono y en escritorio, en tema claro y en tema oscuro.

## Supuestos

- Las decisiones de contenido de la ficha básica, de quién la abre y de que la familia deja de
  estar limitada al nombre del entrenador las tomó el propietario el 2026-10-10.
- "Nombres de personas" son los de integrantes del club (jugadores, entrenadores, presidentes y
  directivos). No incluye el nombre del padre, madre o responsable, que es un dato de contacto de
  la ficha, ni el nombre del club.
- El panel del DESARROLLADOR queda fuera: allí los nombres de presidentes no se vuelven enlaces,
  porque el DESARROLLADOR no consulta fichas (§8).
- El rol que muestra la ficha básica es el único rol de la persona en ese club (§8). La asignación
  como entrenador se ve en la sección de categorías, no como un segundo rol.
- La lista de jugadores, el detalle, las categorías y los inicios de la 003 y la 007 conservan su
  contenido; esta funcionalidad solo convierte sus nombres en enlaces y añade la ficha básica.
- Los datos personales de un integrante que no es jugador son los mismos en todos sus clubes,
  porque es la misma persona con el mismo documento (§10).

## Fuera de alcance

- Cambiar el correo de la cuenta.
- Cambiar o corregir el documento de un PRESIDENTE, DIRECTIVO o ENTRENADOR.
- Ficha básica de cuentas de jugador o de la familia como personas (el club ve al jugador por su
  ficha).
- Directorio o lista de todo el personal del club con búsqueda.
- Datos de contacto de entrenadores, presidentes o directivos visibles para el club.
- Mensajería o comunicados desde la ficha básica.
- Cambios a lo que cada rol puede ver o editar de la ficha del jugador (spec 005).
