# Especificación de funcionalidad: Jugadores, inicio por rol e identidad del club por el presidente

**Rama de la funcionalidad**: `007-jugadores-inicio-por-rol`

**Creada**: 2026-10-10

**Estado**: Borrador

**Entrada**: Descripción del propietario del proyecto: "Opción 'Jugadores' en el menú para
PRESIDENTE, DIRECTIVO y ENTRENADOR: todos los jugadores del club ordenados por categoría, cada uno
con su foto en miniatura, nombre completo, equipo y una columna 'Al día' con espacio reservado
hasta que existan los pagos. Al abrir un jugador se ve toda su información con la foto grande.
Inicio distinto por rol: resumen del club (jugadores, equipos, entrenadores, categorías) para
PRESIDENTE y DIRECTIVO, sus categorías para el ENTRENADOR y su jugador para la familia, con una
sección 'Próximos entrenamientos' con espacio reservado hasta que existan los entrenamientos. El
PRESIDENTE puede cambiar el escudo (PNG, JPEG o WebP de hasta 1 MB, como en la 001) y los colores
de su club desde 'Datos del club', con el mismo aviso de contraste del panel del desarrollador; el
DESARROLLADOR los sigue cambiando desde su panel. Esto sustituye el escenario 2 de la historia 4 de
la spec 001."

**Plataforma**: La Pecosa

**Constitución aplicable**: versión 4.4.0, en especial §7.1, §7.2, §7.5, §8, §11.2, §11.3, §12.2,
§12.3, §14.1, §14.2, §15, §20 y §24.

**Depende de**: spec 001 (base multiclub: identidad del club, aviso de contraste, foto de
perfil), spec 003 (categorías y equipos), spec 005 (ficha del jugador) y spec 006 (varios
jugadores por cuenta). Los jugadores, sus categorías y equipos, sus fichas y la identidad de cada
club ya existen; esta funcionalidad añade una lista de jugadores para el club, un inicio propio de
cada rol y que el PRESIDENTE cambie la identidad de su club.

**Deja sin efecto**: el escenario 2 de la historia de usuario 4 de la spec 001 ("el PRESIDENTE no
puede cambiar el escudo ni los colores") y la frase "Nadie más puede hacerlo" del RF-008 de esa
misma spec. A partir de esta funcionalidad el PRESIDENTE sí puede cambiarlos en su propio club
(constitución §7.2, enmienda 4.3.0).

## Aclaraciones

### Sesión 2026-10-10

- P: ¿De dónde sale la foto del jugador que se ve en la lista, en el detalle y en el inicio de la
  familia? → R: Es una foto propia de cada jugador, guardada en su ficha. La suben la familia y el
  PRESIDENTE, en PNG, JPEG o WebP de hasta 1 MB. La foto de perfil de la cuenta sigue siendo
  privada y no se usa para esto.

## Escenarios de usuario y pruebas *(obligatorio)*

### Historia de usuario 1 - El club consulta la lista de jugadores (Prioridad: P1)

El PRESIDENTE, un DIRECTIVO o un ENTRENADOR elige "Jugadores" en el menú y ve a los jugadores
activos del club agrupados por categoría, de la más antigua a la más reciente, y dentro de cada
categoría por apellidos y nombres. De cada jugador ve su foto en miniatura (o sus iniciales si no
tiene), su nombre completo, el equipo o los equipos en los que juega y una columna "Al día" que,
mientras no existan los pagos, muestra un espacio reservado. El ENTRENADOR solo ve las categorías
que tiene asignadas (constitución §7.5).

**Por qué esta prioridad**: hoy el club solo llega a un jugador entrando categoría por categoría.
Tener a todos a la vista en una sola lista es lo que más se usa en el día a día y es la base de la
historia 2.

**Prueba independiente**: con jugadores en dos categorías, uno de ellos en dos equipos y otro sin
equipo, se entra como PRESIDENTE, como DIRECTIVO y como ENTRENADOR de una sola de las categorías,
y se comprueba qué ve cada uno.

**Escenarios de aceptación**:

1. **Dado** un club con jugadores activos en varias categorías, **cuando** el PRESIDENTE o un
   DIRECTIVO abre "Jugadores", **entonces** ve a todos los jugadores activos agrupados por
   categoría, cada uno con su foto en miniatura, su nombre completo, sus equipos y la columna "Al
   día".
2. **Dado** un ENTRENADOR asignado a una sola categoría, **cuando** abre "Jugadores", **entonces**
   ve únicamente a los jugadores de esa categoría, de todos sus equipos, y no ve las demás
   categorías ni el grupo "Sin categoría".
3. **Dado** un jugador que juega en dos equipos de su categoría, **cuando** aparece en la lista,
   **entonces** se muestran los nombres de ambos equipos; si no juega en ningún equipo, la columna
   lo indica con el texto "Sin equipo".
4. **Dado** un jugador sin foto, **cuando** aparece en la lista, **entonces** se muestran sus
   iniciales en lugar de la foto.
5. **Dado** jugadores sin categoría (spec 003), **cuando** el PRESIDENTE o un DIRECTIVO abre
   "Jugadores", **entonces** aparecen al final, en un grupo "Sin categoría".
6. **Dado** cualquier jugador de la lista, **cuando** se mira la columna "Al día", **entonces**
   muestra un espacio reservado que indica que el estado de pago aún no está disponible, igual
   para todos los jugadores, sin dar a entender que el jugador está al día ni que debe.
7. **Dado** un jugador retirado o un hermano que todavía espera aprobación, **cuando** se abre
   "Jugadores", **entonces** no aparece en la lista.
8. **Dado** una cuenta con rol JUGADOR, **cuando** entra a su club, **entonces** no ve la opción
   "Jugadores" en el menú, y si intenta abrir la lista directamente el sistema se lo niega.
9. **Dado** un ENTRENADOR sin categorías asignadas, **cuando** abre "Jugadores", **entonces** ve un
   mensaje que le explica que todavía no tiene categorías asignadas.

---

### Historia de usuario 2 - El club abre a un jugador y ve toda su información (Prioridad: P1)

Desde la lista, quien la consulta abre a un jugador y ve su foto en grande junto con toda la
información de su ficha que su rol le permite ver (spec 005): identidad, categoría, equipos,
contacto, contacto de emergencia, seguridad social, datos clínicos y documentación.

**Por qué esta prioridad**: la lista sin el detalle obliga a volver a buscar al jugador por su
categoría. Juntas forman el mínimo útil.

**Prueba independiente**: se abre al mismo jugador como PRESIDENTE, como DIRECTIVO y como su
ENTRENADOR, y se comprueba que cada uno ve la foto grande y solo los datos que su rol permite.

**Escenarios de aceptación**:

1. **Dado** el PRESIDENTE en la lista de jugadores, **cuando** abre a un jugador, **entonces** ve
   su foto en grande, su categoría, sus equipos y toda su ficha, y puede editarla como ya permite
   la spec 005.
2. **Dado** un DIRECTIVO, **cuando** abre a un jugador, **entonces** ve la foto en grande y la
   ficha sin los datos clínicos, y no puede cambiar nada (spec 005).
3. **Dado** un ENTRENADOR, **cuando** abre a un jugador de sus categorías, **entonces** ve la foto
   en grande y la ficha con los datos clínicos y de emergencia, pero sin archivos ni estado de la
   documentación, y no puede cambiar nada (spec 005).
4. **Dado** un ENTRENADOR que conoce el identificador de un jugador de otra categoría, **cuando**
   intenta abrirlo, **entonces** el sistema se lo niega y no le revela ningún dato, tampoco la
   foto.
5. **Dado** un jugador sin foto, **cuando** se abre, **entonces** se muestran sus iniciales en el
   lugar de la foto grande.
6. **Dado** el detalle de un jugador, **cuando** quien lo consulta vuelve atrás, **entonces**
   regresa a la lista de jugadores.
7. **Dado** la familia en "Mi ficha" o el PRESIDENTE en el detalle de un jugador, **cuando** carga
   una foto PNG, JPEG o WebP de hasta 1 MB, **entonces** queda guardada como foto de ese jugador y
   se ve en la lista, en el detalle y en el inicio de la familia.
8. **Dado** la familia o el PRESIDENTE, **cuando** intenta cargar como foto un archivo que no es una
   imagen admitida o que pesa más de 1 MB, **entonces** el sistema lo rechaza, explica el motivo y
   conserva la foto anterior.
9. **Dado** un jugador con foto, **cuando** la familia o el PRESIDENTE la quita, **entonces** vuelven
   a mostrarse sus iniciales.
10. **Dado** una cuenta con dos hermanos, **cuando** la familia le pone foto a uno, **entonces** el
    otro conserva la suya (o sus iniciales), y la foto de perfil de la cuenta no cambia.
11. **Dado** un DIRECTIVO o un ENTRENADOR, **cuando** intenta cargar o quitar la foto de un
    jugador, **entonces** el sistema se lo niega.

---

### Historia de usuario 3 - Cada rol tiene su propio inicio (Prioridad: P2)

Al entrar al club, cada rol ve un inicio pensado para él:

- PRESIDENTE y DIRECTIVO ven un resumen del club: cuántos jugadores, equipos, entrenadores y
  categorías tiene.
- El ENTRENADOR ve sus categorías, con cuántos jugadores tiene cada una y qué equipos dirige.
- La familia ve a su jugador: su foto, su nombre, su categoría, sus equipos y la entrada a su
  ficha.

Todos ven además una sección "Próximos entrenamientos" que, mientras no existan los
entrenamientos, muestra un espacio reservado.

**Por qué esta prioridad**: mejora la primera impresión de cada rol, pero no da acceso a nada que
no se pueda consultar ya por el menú.

**Prueba independiente**: se entra al mismo club con un integrante de cada rol y se comprueba que
cada uno ve su inicio, con cifras que coinciden con lo que hay en el club.

**Escenarios de aceptación**:

1. **Dado** un club con jugadores, equipos, entrenadores y categorías, **cuando** el PRESIDENTE o
   un DIRECTIVO entra al inicio, **entonces** ve las cuatro cifras del club y cada una coincide con
   lo que hay en el club en ese momento.
2. **Dado** un club recién creado, **cuando** el PRESIDENTE entra al inicio, **entonces** ve las
   cuatro cifras en cero, sin errores.
3. **Dado** un ENTRENADOR asignado a dos categorías, **cuando** entra al inicio, **entonces** ve
   esas dos categorías, el número de jugadores activos de cada una y los equipos que dirige en
   ellas, y desde cada categoría llega a sus jugadores.
4. **Dado** un ENTRENADOR sin categorías asignadas, **cuando** entra al inicio, **entonces** ve un
   mensaje que le explica que todavía no tiene categorías asignadas.
5. **Dado** una cuenta de jugador, **cuando** entra al inicio, **entonces** ve la foto, el nombre,
   la categoría y los equipos del jugador elegido y la entrada a su ficha; si la cuenta tiene
   varios jugadores, ve el que eligió al entrar (spec 006).
6. **Dado** cualquier rol del club, **cuando** entra al inicio, **entonces** ve la sección
   "Próximos entrenamientos" con un espacio reservado que indica que los entrenamientos aún no
   están disponibles.
7. **Dado** un PRESIDENTE o DIRECTIVO que además entrena una categoría, **cuando** entra al
   inicio, **entonces** ve el resumen del club, que es el inicio de su rol.

---

### Historia de usuario 4 - El presidente cambia el escudo y los colores de su club (Prioridad: P2)

El PRESIDENTE entra a "Datos del club" y, además del nombre, la sede, la dirección y el contacto,
cambia el escudo y los colores de su club, con la misma vista previa y el mismo aviso de
contraste que tiene el DESARROLLADOR en su panel. El DESARROLLADOR los sigue pudiendo cambiar
desde su panel.

**Por qué esta prioridad**: hoy el club depende del DESARROLLADOR para algo tan propio como su
escudo. No bloquea a ningún otro rol.

**Prueba independiente**: un PRESIDENTE cambia el escudo y los colores de su club y se comprueba
que todos los integrantes de ese club ven la nueva identidad, que el panel del DESARROLLADOR la
muestra y que otro club no cambia.

**Escenarios de aceptación**:

1. **Dado** el PRESIDENTE en "Datos del club", **cuando** carga un escudo PNG, JPEG o WebP de
   hasta 1 MB, **entonces** queda guardado y todos los integrantes del club lo ven, igual que en
   el panel del DESARROLLADOR.
2. **Dado** el PRESIDENTE, **cuando** intenta cargar como escudo un archivo que no es una imagen
   admitida o que pesa más de 1 MB, **entonces** el sistema lo rechaza, explica el motivo y
   conserva el escudo anterior.
3. **Dado** el PRESIDENTE, **cuando** cambia los colores del club, **entonces** ve la vista previa
   en tema claro y en tema oscuro, y al guardar todos los integrantes del club ven los colores
   nuevos.
4. **Dado** el PRESIDENTE eligiendo colores, **cuando** la combinación no permite leer bien el
   texto en alguno de los dos temas, **entonces** recibe, antes de guardar, el mismo aviso que el
   DESARROLLADOR, y puede guardar igual.
5. **Dado** un DIRECTIVO, un ENTRENADOR o una cuenta de jugador, **cuando** intenta cambiar el
   escudo o los colores de su club, **entonces** el sistema se lo niega.
6. **Dado** el PRESIDENTE de un club, **cuando** intenta cambiar el escudo o los colores de otro
   club, incluso conociendo su identificador, **entonces** el sistema se lo niega.
7. **Dado** que el PRESIDENTE cambió la identidad de su club, **cuando** el DESARROLLADOR la
   cambia después desde su panel, **entonces** queda la del DESARROLLADOR; vale siempre el último
   cambio, venga de quien venga.
8. **Dado** un club suspendido, **cuando** su PRESIDENTE cambia el escudo o los colores,
   **entonces** el cambio queda guardado, igual que el resto de los datos del club.

---

### Casos límite

- Un jugador cambia de categoría o de equipo mientras alguien tiene la lista abierta: al volver a
  cargarla aparece en su nueva categoría o con sus nuevos equipos.
- A un ENTRENADOR le quitan una categoría mientras tiene abierta la lista o el detalle de un
  jugador de ella: la siguiente consulta se le niega y no ve más datos de ese jugador.
- Una categoría o un equipo desactivados: los equipos desactivados no se muestran en la columna
  de equipos ni cuentan en el resumen.
- Un nombre de jugador o de equipo muy largo no debe romper la lista ni en teléfono ni en
  escritorio.
- Un club con muchos jugadores (cientos): la lista sigue siendo usable y se puede recorrer por
  categorías.
- Una persona que pertenece a dos clubes con roles distintos ve en cada club el inicio y el menú
  de su rol en ese club, y al cambiar de club en el desplegable cambia de inicio.
- Un PRESIDENTE que pierde el rol mientras está en "Datos del club" y luego intenta guardar el
  escudo o los colores: el sistema se lo niega.
- El PRESIDENTE y el DESARROLLADOR cambian los colores casi a la vez: queda el último guardado.
- La familia y el PRESIDENTE cambian la foto del mismo jugador casi a la vez: queda la última
  guardada.
- Un jugador con foto deja de pertenecer al club (lo retiran, su familia lo retira, queda retirado
  al aprobarse en otro club como hermano o su cuenta pasa a otro rol): su foto se elimina
  definitivamente. Si después lo reincorporan, vuelve sin foto y se muestran sus iniciales hasta
  que la familia o el PRESIDENTE carguen una nueva.

## Requisitos *(obligatorio)*

### Requisitos funcionales

**Lista de jugadores**

- **RF-001**: El menú del club DEBE mostrar la opción "Jugadores" al PRESIDENTE, a los DIRECTIVOS
  y a los ENTRENADORES, y NO DEBE mostrarla a las cuentas de jugador.
- **RF-002**: La lista DEBE incluir solo jugadores activos del club elegido: no incluye retirados
  ni hermanos que esperan aprobación.
- **RF-003**: Para el PRESIDENTE y los DIRECTIVOS, la lista DEBE incluir a todos los jugadores
  activos del club. Para un ENTRENADOR DEBE incluir solo a los de las categorías que tiene
  asignadas, de todos sus equipos. Este límite DEBE aplicarlo el sistema y no solo la pantalla.
- **RF-004**: La lista DEBE agrupar a los jugadores por categoría, de la del año de nacimiento más
  antiguo a la del más reciente, y dentro de cada categoría ordenarlos por apellidos y nombres.
  Los jugadores sin categoría DEBEN ir al final, en un grupo "Sin categoría", visible solo para el
  PRESIDENTE y los DIRECTIVOS.
- **RF-005**: Cada jugador de la lista DEBE mostrar su foto en miniatura (o sus iniciales si no
  tiene), su nombre completo, los nombres de los equipos activos en los que juega (o "Sin
  equipo") y la columna "Al día".
- **RF-006**: Mientras no existan los pagos, la columna "Al día" DEBE mostrar para todos los
  jugadores el mismo espacio reservado, con un texto que diga que el estado de pago aún no está
  disponible. NO DEBE mostrar ningún valor que pueda leerse como "al día" o "en deuda".

**Detalle del jugador**

- **RF-007**: Desde la lista, quien la consulta DEBE poder abrir a un jugador y ver su foto en
  grande (o sus iniciales si no tiene), su categoría, sus equipos y la información de su ficha que
  su rol permite ver según la spec 005, sin ampliar ni reducir lo que esa spec permite a cada rol.
- **RF-008**: El sistema DEBE negar el detalle y la foto de un jugador a quien no lo tiene en su
  lista (RF-003), aunque conozca su identificador, sin revelar si el jugador existe.

**Foto del jugador**

- **RF-009**: Cada jugador DEBE tener su propia foto, guardada en su ficha y distinta de la foto
  de perfil de la cuenta, que sigue siendo privada (spec 001). La familia, sobre su propio
  jugador, y el PRESIDENTE, sobre cualquier jugador de su club, DEBEN poder cargarla, cambiarla y
  quitarla. La foto DEBE ser una imagen PNG, JPEG o WebP de hasta 1 MB; si no lo es, el sistema
  DEBE rechazarla, explicar el motivo y conservar la anterior. Ningún otro rol puede cambiarla.
  Cargar, cambiar o quitar la foto cuenta como un cambio de la ficha (fecha y autor del último
  cambio, spec 005). Cuando el jugador deja de pertenecer al club, el sistema DEBE eliminar su
  foto definitivamente; no se conserva para una posible reincorporación.
- **RF-010**: La foto de un jugador DEBE poder verla únicamente quien puede ver a ese jugador: su
  propia cuenta, el PRESIDENTE y los DIRECTIVOS de su club y los ENTRENADORES de su categoría.

**Inicio por rol**

- **RF-011**: El inicio del PRESIDENTE y de los DIRECTIVOS DEBE mostrar cuatro cifras del club
  elegido: jugadores activos, equipos activos, entrenadores y categorías activas. "Entrenadores"
  cuenta a las personas distintas asignadas como entrenador a alguna categoría activa del club,
  sea cual sea su rol, más los integrantes con rol ENTRENADOR que todavía no tienen categoría.
- **RF-012**: El inicio de un ENTRENADOR DEBE mostrar sus categorías asignadas, con el número de
  jugadores activos de cada una y los equipos que dirige en ella, y DEBE permitirle llegar desde
  cada categoría a sus jugadores. Sin categorías asignadas, DEBE mostrar un mensaje que lo
  explique.
- **RF-013**: El inicio de una cuenta de jugador DEBE mostrar, del jugador elegido, su foto (o sus
  iniciales), su nombre completo, su categoría, sus equipos y la entrada a su ficha.
- **RF-014**: El inicio de todos los roles del club DEBE incluir la sección "Próximos
  entrenamientos" con un espacio reservado que diga que los entrenamientos aún no están
  disponibles, sin mostrar entrenamientos de ejemplo.
- **RF-015**: Un PRESIDENTE o un DIRECTIVO asignado como entrenador DEBE ver el inicio de su rol
  (RF-011); la asignación no le cambia el inicio.
- **RF-016**: El inicio DEBE seguir mostrando el nombre del club y el rol de quien entra.

**Escudo y colores por el presidente**

- **RF-017**: El PRESIDENTE DEBE poder cambiar, desde "Datos del club", el escudo y los colores de
  su propio club, con las mismas operaciones, reglas y vista previa que el DESARROLLADOR tiene en
  su panel (spec 001).
- **RF-018**: El escudo DEBE ser una imagen PNG, JPEG o WebP de hasta 1 MB. Si no lo es, el
  sistema DEBE rechazarlo, explicar el motivo y conservar el escudo anterior.
- **RF-019**: Antes de guardar los colores, el sistema DEBE dar al PRESIDENTE el mismo aviso de
  contraste que al DESARROLLADOR cuando el texto no se leería bien en alguno de los dos temas. Es
  un aviso: no impide guardar.
- **RF-020**: El DESARROLLADOR DEBE poder seguir cambiando el escudo y los colores de cualquier
  club desde su panel. Prevalece el último cambio guardado, sea del PRESIDENTE o del
  DESARROLLADOR.
- **RF-021**: Ningún otro rol DEBE poder cambiar el escudo ni los colores, y el PRESIDENTE solo
  DEBE poder cambiar los de su propio club. Estos límites DEBE aplicarlos el sistema y no solo la
  pantalla.
- **RF-022**: Un cambio de escudo o de colores hecho por el PRESIDENTE DEBE verse en los mismos
  lugares y de la misma forma que un cambio hecho por el DESARROLLADOR: en toda la aplicación de
  ese club y en el panel del DESARROLLADOR.

**Aislamiento**

- **RF-023**: Ninguna lista, cifra, detalle, foto ni identidad de esta funcionalidad DEBE incluir
  ni revelar datos de otro club. El DESARROLLADOR NO DEBE poder consultar la lista ni el detalle de
  los jugadores de ningún club.

### Entidades clave

- **Jugador en la lista**: vista de un jugador activo del club con su foto en miniatura, su nombre
  completo, su categoría, sus equipos activos y su estado "Al día" (reservado). No es un dato
  nuevo: se arma con los que ya existen.
- **Foto del jugador**: imagen propia de cada jugador, guardada en su ficha, que lo identifica ante
  el club. La cargan la familia y el PRESIDENTE. Es independiente de la foto de perfil de la
  cuenta. Se muestra en miniatura en la lista y en grande en el detalle y en el inicio de su
  familia.
- **Resumen del club**: las cuatro cifras del inicio del PRESIDENTE y los DIRECTIVOS (jugadores,
  equipos, entrenadores y categorías). Se calcula al consultarlo; no se guarda.
- **Identidad del club**: escudo y colores del club (spec 001). Ahora la cambian el DESARROLLADOR y
  el PRESIDENTE de ese club.

## Criterios de éxito *(obligatorio)*

### Resultados medibles

- **CE-001**: En el 100 % de los casos probados, la lista del PRESIDENTE y de un DIRECTIVO incluye
  exactamente a los jugadores activos del club, ni uno más ni uno menos.
- **CE-002**: En el 100 % de los intentos probados, un ENTRENADOR no obtiene ningún dato ni foto
  de un jugador que no está en sus categorías, y una cuenta de jugador no obtiene la lista.
- **CE-003**: En el 100 % de los casos probados, las cuatro cifras del resumen del club coinciden
  con lo que hay en el club en ese momento.
- **CE-004**: En el 100 % de los casos probados, el detalle de un jugador muestra a cada rol
  exactamente los datos que la spec 005 le permite ver.
- **CE-005**: En el 100 % de los cambios probados, el escudo o los colores guardados por el
  PRESIDENTE se ven igual en la aplicación del club y en el panel del DESARROLLADOR, y no cambian
  los de ningún otro club.
- **CE-006**: En el 100 % de los intentos probados, ningún rol distinto del PRESIDENTE del club y
  del DESARROLLADOR consigue cambiar el escudo o los colores de un club.
- **CE-007**: Ninguna pantalla muestra en "Al día" ni en "Próximos entrenamientos" un dato que
  pueda tomarse por real mientras esas funcionalidades no existan.
- **CE-008**: La lista, el detalle y el inicio se leen y se usan sin desplazamiento horizontal de
  la página en teléfono y en escritorio, en tema claro y en tema oscuro.

## Supuestos

- El alcance del ENTRENADOR en "Jugadores" lo fija la constitución (§7.5): solo sus categorías
  asignadas, con todos los jugadores de cada una, de cualquier equipo.
- El detalle del jugador reutiliza la ficha de la spec 005 con sus permisos por rol; esta
  funcionalidad solo le añade la foto grande, la categoría y los equipos, y la entrada desde la
  lista.
- Los permisos para editar la ficha no cambian: el PRESIDENTE edita, el DIRECTIVO y el ENTRENADOR
  solo consultan.
- La lista no tiene búsqueda, filtros ni paginación en esta versión: el tamaño de los clubes
  actuales (de la categoría 2012 a la 2020) la hace manejable agrupada por categoría.
- La columna "Al día" y la sección "Próximos entrenamientos" se llenarán cuando existan los pagos
  (§16) y los entrenamientos (§17); hasta entonces solo muestran el espacio reservado.
- El inicio de cada rol conserva lo que hoy muestra el inicio del club (nombre del club, rol,
  datos de contacto) y la entrada a "Datos del club" para el PRESIDENTE.
- El escudo y los colores del club usan exactamente las reglas de la spec 001 (formatos, tamaño,
  identidad neutra si faltan, aviso de contraste que no impide guardar).
- El PRESIDENTE de un club suspendido puede cambiar la identidad de su club, como el resto de sus
  datos, porque es el único que entra mientras dura la suspensión.

## Fuera de alcance

- Estado de pago real en la columna "Al día", cargos, pagos y mensualidades.
- Entrenamientos reales en "Próximos entrenamientos", su programación y la asistencia.
- Búsqueda, filtros, exportación o impresión de la lista de jugadores.
- Cambiar desde la lista la categoría o los equipos de un jugador (sigue haciéndose desde
  "Categorías").
- Jugadores retirados en la lista (siguen en la lista de retirados de la spec 003).
- Cambios a lo que cada rol puede ver o editar de la ficha (spec 005).
- Que otros roles distintos del PRESIDENTE y el DESARROLLADOR cambien la identidad del club.
