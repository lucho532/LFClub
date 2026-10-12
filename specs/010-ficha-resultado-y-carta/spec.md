# Especificación de funcionalidad: Ficha editable por la familia, resultado publicado por la familia y carta del jugador

**Rama de la funcionalidad**: `010-ficha-resultado-y-carta`

**Creada**: 2026-10-11

**Estado**: Borrador

**Entrada**: Descripción del propietario del proyecto: "Especificar la funcionalidad 010 (ficha
entera editable por la familia, resultado publicado por la familia y carta del jugador)".

**Plataforma**: LFClub

**Constitución aplicable**: versión 4.8.0, en especial §7.1, §7.2, §7.5, §8 (Presidente,
Entrenador y Jugador), §10, §11.2, §13, §14.1, §14.2, §14.3, §15, §17, §17.2, §17.3, §20, §24,
§25 y §28.

**Depende de**: spec 001 (base multiclub y colores del club), spec 005 (ficha del jugador), spec
007 (escudo y colores por el PRESIDENTE), spec 008 (nombres clicables) y spec 009 (partidos,
convocatorias, resultados y estadísticas calculadas).

**Cambia**: en la spec 005, RF-016 y RF-017 (la familia ya no tiene datos vedados de la ficha). En
la spec 009, RF-015 y RF-016 (además de quien entrena y el PRESIDENTE, la familia de un convocado
publica el resultado).

## Impacto en la constitución

Las tres partes ya están recogidas en la enmienda 4.6.0 → 4.7.0 (2026-10-11): §8 (Jugador), §14.2,
§14.3 y §17.3. No hace falta otra enmienda para construir lo que aquí se especifica.

La §28 dejaba abiertas dos decisiones que esta funcionalidad necesita: quién pone el dorsal y la
posición de la carta, y si la familia corrige lo que indicó de su propio jugador. El propietario
las tomó el 2026-10-11 (ver Aclaraciones).

**Aplicado**: la enmienda 4.7.0 → 4.8.0 (2026-10-11) las recoge en la §8, la §14.3 y la §17.3 y
las cierra en la §28.

## Aclaraciones

### Sesión 2026-10-11

- P: ¿La familia puede corregir lo que indicó de su propio jugador? → R: Sí, mientras quien
  entrena o el PRESIDENTE no hayan corregido lo de ese jugador.
- P: ¿Quién pone el dorsal y la posición de la carta? → R: La familia, quien entrena la categoría
  y el PRESIDENTE. Vale el último cambio.
- P: ¿Qué rarezas hay y con qué hitos se ganan? → R: Solo por asistencia a entrenamientos en la
  temporada: Bronce por debajo de 60 %, Plata de 60 % a 84 % y Oro desde 85 %.

## Escenarios de usuario y pruebas *(obligatorio)*

En esta especificación, "la familia" es la cuenta de un jugador (rol JUGADOR), que usa su padre,
madre o responsable. "Quien entrena" una categoría es un ENTRENADOR asignado a ella, o un
PRESIDENTE o DIRECTIVO asignado a ella (§8, "Entrenador").

### Historia de usuario 1 - La familia publica el resultado del partido de su hijo (Prioridad: P1)

Cuando un partido ya empezó, la familia de un jugador convocado puede publicar el marcador (goles
del club y goles del rival) e indicar si su propio hijo jugó y cuántos goles hizo. Vale el primer
marcador publicado: las demás familias de convocados ya no lo cambian, pero cada una puede indicar
lo de su propio hijo. Quien entrena las categorías del partido o el PRESIDENTE pueden corregir
todo después. Nadie, al publicar, ve a los demás convocados.

**Por qué esta prioridad**: le quita al entrenador la tarea de cargar cada resultado y hace que
las estadísticas del club se llenen solas el mismo día del partido.

**Prueba independiente**: con un partido empezado y tres convocados, la familia A publica 2-1 y
que su hijo jugó e hizo un gol; la familia B intenta publicar 3-1 y solo puede indicar lo de su
hijo; el entrenador corrige a 3-1 y el gol de B; se comprueba en cada paso lo que ven las
familias, un no convocado y las estadísticas.

**Escenarios de aceptación**:

1. **Dado** un partido cuya hora de inicio ya llegó, sin resultado, y la familia de un convocado,
   **cuando** publica el marcador e indica que su hijo jugó e hizo un gol, **entonces** el partido
   queda como jugado con ese marcador, su hijo cuenta como que jugó y el gol suma a sus
   estadísticas.
2. **Dado** un partido cuya hora de inicio todavía no llega, **cuando** la familia de un convocado
   intenta publicar el resultado, **entonces** el sistema lo rechaza.
3. **Dado** un partido al que el jugador no fue convocado, **cuando** su familia intenta publicar
   el resultado, **entonces** el sistema responde igual que si el partido no existiera (§17.2).
4. **Dado** un partido con un marcador ya publicado por otra familia, **cuando** una familia de
   otro convocado lo abre, **entonces** ve el marcador publicado, no puede cambiarlo y puede
   indicar si su propio hijo jugó y sus goles.
5. **Dado** el formulario de la familia, **cuando** lo abre, **entonces** ve solo a su propio
   jugador: ni la lista de convocados, ni quiénes jugaron, ni lo que indicaron otras familias.
6. **Dado** un marcador de 2 goles del club con un gol ya asignado a otro jugador, **cuando** una
   familia indica que su hijo hizo 2 goles, **entonces** el sistema lo rechaza con un motivo que
   dice que los goles superan el marcador, sin nombrar al otro goleador.
7. **Dado** una familia que indica goles para su hijo, **cuando** indica a la vez que no jugó,
   **entonces** el sistema lo rechaza.
8. **Dado** un resultado publicado por una familia, **cuando** quien entrena las categorías del
   partido o el PRESIDENTE lo abre, **entonces** ve el marcador, quién lo publicó y cuándo, y lo que
   cada familia indicó de su hijo, y puede corregir todo, también el marcador.
9. **Dado** un resultado corregido por quien entrena o el PRESIDENTE, **cuando** se consultan las
   estadísticas y los goleadores, **entonces** reflejan la corrección de inmediato.
10. **Dado** un resultado ya registrado o corregido por quien entrena o el PRESIDENTE, **cuando**
    una familia intenta cambiar el marcador, **entonces** el sistema lo rechaza.
11. **Dado** un partido jugado, **cuando** lo consulta cualquier integrante del club, **entonces**
    ve marcador y goleadores como en la spec 009, sin que se sepa qué familia publicó cada dato,
    salvo quien entrena y el PRESIDENTE.
12. **Dado** una familia que ya indicó lo de su hijo y nadie lo ha corregido, **cuando** intenta
    cambiarlo, **entonces** puede hacerlo, con las mismas validaciones. Si quien entrena o el
    PRESIDENTE ya corrigieron lo de ese jugador, el sistema se lo niega.

---

### Historia de usuario 2 - La familia cambia cualquier dato de la ficha de su hijo (Prioridad: P2)

La familia puede corregir todos los datos de la ficha de su propio jugador, también sus nombres,
sus apellidos y su fecha de nacimiento, que hasta ahora solo cambiaba el PRESIDENTE. El documento
sigue sus reglas de siempre y cambiar la fecha de nacimiento no cambia la categoría.

**Por qué esta prioridad**: evita que el PRESIDENTE tenga que corregir a mano errores de
digitación del registro. Es la parte más sencilla y no depende de las otras dos.

**Prueba independiente**: la familia corrige un apellido mal escrito y la fecha de nacimiento de
su hijo, que está en la categoría 2014; se comprueba que el cambio lo ven el PRESIDENTE, el
entrenador y los goleadores, que el jugador sigue en la 2014 y que otra familia no puede hacer lo
mismo con ese jugador.

**Escenarios de aceptación**:

1. **Dado** la familia en la ficha de su propio jugador, **cuando** cambia los nombres, los
   apellidos o la fecha de nacimiento y guarda, **entonces** el cambio queda guardado y lo ven todos
   los que pueden ver a ese jugador, en todas las pantallas donde aparece su nombre.
2. **Dado** un jugador en la categoría 2014, **cuando** la familia cambia su fecha de nacimiento a
   un día de 2013, **entonces** el jugador sigue en la 2014.
3. **Dado** un jugador sin categoría, **cuando** la familia cambia su fecha de nacimiento,
   **entonces** sigue sin categoría; ubicarlo sigue siendo del PRESIDENTE.
4. **Dado** la familia, **cuando** guarda una fecha de nacimiento futura, o deja vacíos los
   nombres o los apellidos, **entonces** el sistema lo rechaza con un motivo claro.
5. **Dado** una cuenta con dos hermanos, **cuando** cambia los apellidos de uno, **entonces** el
   otro no cambia.
6. **Dado** la familia de otro jugador, o un ENTRENADOR o DIRECTIVO, **cuando** intenta cambiar los
   nombres, los apellidos o la fecha de nacimiento de ese jugador, **entonces** el sistema se lo
   niega.
7. **Dado** un cambio guardado por la familia, **cuando** se abre la ficha, **entonces** muestra la
   fecha y el nombre de quien hizo el último cambio, como en la spec 005.

---

### Historia de usuario 3 - La carta del jugador (Prioridad: P2)

Cada jugador tiene una carta, al estilo de las de los videojuegos de fútbol, con su foto, su
nombre, el dorsal, la posición, el escudo del club, un fondo con un degradado entre los dos
colores del club y seis estadísticas (RIT, TIR, PAS, REG, DEF, FÍS) con su valoración. Quien
entrena la categoría o el PRESIDENTE ponen las estadísticas. La carta la ven los mismos que ven la
ficha.

**Por qué esta prioridad**: es lo que más ilusiona a los niños y a las familias, pero no hace
falta para operar el club.

**Prueba independiente**: el entrenador de la 2014 pone las seis estadísticas a un jugador; se
comprueba que la familia, el PRESIDENTE y el entrenador ven la carta con la valoración correcta,
que un jugador de otra familia y un entrenador de otra categoría no la obtienen y que una carta
sin estadísticas se ve sin números.

**Escenarios de aceptación**:

1. **Dado** quien puede ver la ficha de un jugador (§7.5), **cuando** abre su carta, **entonces**
   ve foto, nombre, dorsal, posición, escudo, fondo con los colores del club, las seis estadísticas
   y la valoración.
2. **Dado** quien entrena la categoría del jugador, o el PRESIDENTE, **cuando** pone las seis
   estadísticas con valores entre 1 y 99, **entonces** quedan guardadas y la valoración es su
   promedio redondeado al entero más cercano.
3. **Dado** un valor fuera de 1 a 99, o no entero, **cuando** se intenta guardar, **entonces** el
   sistema lo rechaza.
4. **Dado** un jugador sin estadísticas, **cuando** se abre su carta, **entonces** se ve completa
   pero sin números ni valoración, sin error.
5. **Dado** la familia, un DIRECTIVO o un ENTRENADOR de otra categoría, **cuando** intenta cambiar
   las estadísticas, **entonces** el sistema se lo niega.
6. **Dado** un jugador sin foto, **cuando** se abre su carta, **entonces** se ve con una figura
   genérica en lugar de la foto.
7. **Dado** la familia de otro jugador, un integrante de otro club o el DESARROLLADOR, **cuando**
   pide la carta por el identificador del jugador, **entonces** el sistema responde igual que si no
   existiera.
8. **Dado** cualquier pantalla del club, **cuando** se muestran cartas o estadísticas de la carta,
   **entonces** nunca aparecen en tablas, listas ordenadas por ellas, comparaciones ni posiciones.
9. **Dado** el PRESIDENTE cambia los colores o el escudo del club, **cuando** se abre una carta,
   **entonces** muestra los nuevos.
10. **Dado** un jugador con asistencias registradas en la temporada, **cuando** se abre su carta, **entonces**
    muestra la rareza que le corresponde según su porcentaje de asistencia a entrenamientos en la
    temporada: Bronce por debajo de 60 %, Plata de 60 % a 84 % y Oro desde 85 %.
11. **Dado** una asistencia corregida, **cuando** se abre la carta, **entonces** la
    rareza se recalcula; las seis estadísticas no cambian, porque no se calculan de lo registrado
    (§14.3).
12. **Dado** el dorsal y la posición del jugador, **cuando** los cambia la familia, quien entrena su
    categoría o el PRESIDENTE, **entonces** quedan guardados y vale el último cambio. Un
    DIRECTIVO o un ENTRENADOR de otra categoría no pueden cambiarlos.

---

### Historia de usuario 4 - Exportar la carta como imagen (Prioridad: P3)

La familia descarga la carta de su hijo como imagen para imprimirla o compartirla. El PRESIDENTE
puede descargar la de cualquier jugador de su club, por ejemplo para imprimir las de toda una
categoría. Nadie más la exporta.

**Por qué esta prioridad**: depende de la carta y es la única salida de la foto de un menor fuera
de la plataforma, por eso va con reglas estrictas.

**Prueba independiente**: la familia y el PRESIDENTE descargan la carta y obtienen una imagen
igual a la que ven en pantalla; un ENTRENADOR y un DIRECTIVO, que sí ven la carta, no encuentran
la opción y el sistema les niega la descarga si la piden directamente.

**Escenarios de aceptación**:

1. **Dado** la familia en la carta de su jugador, **cuando** elige exportarla, **entonces** obtiene
   un archivo de imagen con la carta tal como se ve en pantalla.
2. **Dado** el PRESIDENTE en la carta de cualquier jugador de su club, también retirado, **cuando**
   la exporta, **entonces** obtiene la imagen.
3. **Dado** un ENTRENADOR o un DIRECTIVO, también asignado a la categoría, **cuando** abre la carta,
   **entonces** no ve la opción de exportar y, si pide la imagen directamente, el sistema se la
   niega.
4. **Dado** un jugador retirado, **cuando** su familia intenta exportar su carta, **entonces** no
   puede, porque ya no accede a ese club (§14.1).

---

### Casos límite

- Dos familias publican el marcador casi a la vez: vale el que se guardó primero; la segunda ve el
  marcador ya publicado y solo puede indicar lo de su hijo.
- Una familia indica que su hijo jugó y hace goles, y después quien entrena baja el marcador por
  debajo de la suma de goles: la corrección de quien entrena debe dejar todo coherente, ajustando
  los goles de los jugadores en la misma corrección; si no, se rechaza (RF-017 de la spec 009).
- Un convocado cuya familia no indicó nada no cuenta como que jugó mientras nadie lo marque; quien
  entrena puede completarlo después.
- Una cuenta con dos hermanos convocados al mismo partido indica lo de cada uno, eligiendo el
  jugador como en el resto de la plataforma; en cada momento ve solo uno.
- Un jugador retirado después de convocado no puede publicar: su familia ya no accede al club.
- Un partido cancelado no admite resultado de ninguna familia.
- Un jugador que cambia de categoría conserva sus estadísticas de carta; la carta no depende de la
  categoría, pero solo puede cambiarlas quien entrena su categoría actual o el PRESIDENTE.
- Un entrenador que pierde la asignación a una categoría deja de ver y de editar las cartas de
  esos jugadores; lo que puso se conserva.
- Al retirarse un jugador se elimina su foto (§14.2): su carta queda con la figura genérica.
- Un nombre muy largo debe caber en la carta sin romperla.
- Colores del club poco contrastados entre sí: la carta debe seguir siendo legible en tema claro y
  oscuro.

## Requisitos *(obligatorio)*

### Requisitos funcionales

**Resultado publicado por la familia**

- **RF-001**: La cuenta de un jugador convocado DEBE poder publicar el resultado de ese partido
  solo cuando su hora de inicio ya llegó, el partido no está cancelado y no tiene marcador.
- **RF-002**: La publicación de la familia DEBE contener el marcador (goles del club y del rival)
  y, de su propio jugador, si jugó y cuántos goles hizo.
- **RF-003**: Vale el primer marcador publicado. Después NINGUNA familia DEBE poder cambiarlo.
- **RF-004**: Con el marcador ya publicado, la familia de cualquier otro convocado DEBE poder
  indicar si su propio jugador jugó y sus goles, sin cambiar el marcador.
- **RF-005**: La familia NO DEBE recibir, ni al publicar ni después, la lista de convocados, quiénes
  jugaron ni lo indicado por otras familias (§17.2). Solo ve el marcador y los goleadores, como
  cualquier integrante una vez jugado el partido.
- **RF-006**: El sistema DEBE rechazar lo indicado por una familia si su jugador no jugó y tiene
  goles, o si con sus goles la suma de goles por jugador supera los goles del club. El motivo NO
  DEBE revelar quiénes son los demás goleadores más allá de lo que ya es público.
- **RF-007**: Quien entrena todas las categorías del partido y el PRESIDENTE DEBEN poder corregir
  el marcador y lo indicado por cualquier familia, igual que en la spec 009.
- **RF-008**: Una vez que quien entrena o el PRESIDENTE registran o corrigen el resultado, ninguna
  familia DEBE poder cambiar el marcador.
- **RF-009**: El sistema DEBE registrar quién publicó el marcador y cuándo, y quién indicó lo de
  cada jugador y cuándo. Lo DEBEN ver quien entrena las categorías del partido y el PRESIDENTE;
  ninguna cuenta de jugador.
- **RF-010**: Lo publicado por una familia DEBE alimentar las estadísticas de inmediato, igual que
  lo registrado por quien entrena (§17).
- **RF-011**: Un convocado sin indicación de nadie NO DEBE contar como que jugó.
- **RF-012**: La familia DEBE poder cambiar lo que indicó de su propio jugador (si jugó y sus
  goles), con las validaciones de RF-006, mientras quien entrena o el PRESIDENTE no hayan
  corregido lo de ese jugador. Después de esa corrección NO DEBE poder cambiarlo.

**Ficha editable por la familia**

- **RF-013**: La cuenta de un jugador DEBE poder cambiar, de su propio jugador, todos los datos que
  el PRESIDENTE cambia desde la ficha (spec 005, RF-018): nombres, apellidos, fecha de nacimiento,
  tipo y número de documento, celular, nombre del responsable, contacto de emergencia, seguridad
  social, datos clínicos y documentos.
- **RF-014**: Los nombres y los apellidos NO DEBEN quedar vacíos, y la fecha de nacimiento NO DEBE
  ser futura.
- **RF-015**: Un cambio de fecha de nacimiento hecho por la familia NO DEBE cambiar la categoría
  del jugador, tenga categoría o no (§8, "Jugador"; §11.2).
- **RF-016**: Los cambios de documento DEBEN seguir las reglas de la spec 005 (RF-022 a RF-024) y
  de §10.
- **RF-017**: Cada cambio DEBE dejar registrados la fecha y el nombre de quien lo hizo (spec 005,
  RF-038).
- **RF-018**: Ningún otro rol que no lo tuviera ya DEBE ganar permiso de cambio sobre la ficha: los
  DIRECTIVOS y los ENTRENADORES siguen sin cambiarla.

**Carta del jugador**

- **RF-019**: Cada jugador DEBE tener una carta con foto (o una figura genérica si no tiene),
  nombre, dorsal, posición, escudo del club, fondo con un degradado entre el color principal y el
  color de acento del club, las seis estadísticas RIT, TIR, PAS, REG, DEF y FÍS, la valoración y su
  rareza.
- **RF-020**: La carta DEBE ser visible exactamente para quienes pueden ver la ficha del jugador
  (§7.5 y spec 005, RF-005 a RF-013) y para nadie más; nunca en el sitio público ni para el
  DESARROLLADOR.
- **RF-021**: Solo quien entrena la categoría actual del jugador y el PRESIDENTE DEBEN poder poner
  y cambiar las seis estadísticas. Cada una DEBE ser un entero de 1 a 99.
- **RF-022**: Las seis estadísticas se ponen juntas o se dejan todas vacías; una carta con algunas
  sí y otras no NO DEBE guardarse.
- **RF-023**: La valoración DEBE ser el promedio de las seis estadísticas, redondeado al entero
  más cercano. Sin estadísticas, la carta NO DEBE mostrar números ni valoración.
- **RF-024**: Las estadísticas de la carta NO DEBEN calcularse a partir de partidos ni
  entrenamientos ni alterar sus estadísticas (§14.3).
- **RF-025**: Ninguna pantalla DEBE mostrar las estadísticas o la valoración de la carta como
  clasificación: ni tablas, ni listas ordenadas por ellas, ni comparaciones, ni posiciones.
- **RF-026**: La rareza de la carta DEBE calcularse solo con el porcentaje de asistencia a
  entrenamientos de la temporada (contado como en la spec 009, RF-030): Bronce por debajo de
  60 %, Plata de 60 % a 84 % y Oro desde 85 %. Sin asistencias registradas en la temporada, la
  carta es Bronce. DEBE recalcularse al corregirse una asistencia.
- **RF-027**: El dorsal DEBE ser un entero de 1 a 99 y la posición una de una lista fija
  (portero, defensa, mediocampista, delantero). Ambos son opcionales. Los DEBEN poder poner y cambiar la
  familia, sobre su propio jugador, quien entrena su categoría actual y el PRESIDENTE; vale el
  último cambio. Varios jugadores pueden tener el mismo dorsal.
- **RF-028**: Se DEBE guardar quién cambió por última vez las estadísticas de la carta y cuándo, y
  mostrarlo a quien entrena y al PRESIDENTE.

**Exportar la carta**

- **RF-029**: Solo la cuenta del jugador, sobre su propio jugador, y el PRESIDENTE, sobre cualquier
  jugador de su club, DEBEN poder exportar la carta como imagen (§14.2 y §14.3).
- **RF-030**: La imagen exportada DEBE mostrar lo mismo que la carta en pantalla y nada más: ningún
  dato de contacto, de salud ni de documento.
- **RF-031**: La opción de exportar NO DEBE ofrecerse a quien no puede exportar, y el sistema DEBE
  negarle la exportación si la pide directamente.

**Aislamiento e interfaz**

- **RF-032**: Todo lo de esta funcionalidad DEBE quedar limitado al club elegido; nadie de otro
  club ni el DESARROLLADOR DEBE obtener resultados no publicados, fichas ni cartas (§7.1).
- **RF-033**: El backend DEBE aplicar todas las reglas de permisos y visibilidad de esta
  especificación; ocultar opciones en la interfaz NO DEBE ser la única barrera (§15).
- **RF-034**: Toda pantalla de esta funcionalidad, incluida la carta, DEBE funcionar en teléfono y
  en escritorio, en tema claro y oscuro, con la identidad del club, y los estados (pendiente de
  resultado, publicado por una familia, corregido) DEBEN distinguirse por texto además de por
  color (§24).
- **RF-035**: Los nombres de personas en estas pantallas DEBEN seguir la regla de la spec 008.

### Entidades clave

- **Resultado del partido** (amplía el de la spec 009): marcador, quién lo publicó o registró y
  cuándo, y si lo publicó una familia o lo registró o corrigió quien entrena o el PRESIDENTE.
- **Participación del convocado** (amplía la convocatoria de la spec 009): si jugó, sus goles,
  quién lo indicó (su familia, quien entrena o el PRESIDENTE) y cuándo. Puede estar sin indicar.
- **Carta del jugador**: una por jugador y club. Dorsal, posición, quién los cambió por última vez, las seis estadísticas (todas o
  ninguna), quién las cambió por última vez y cuándo. La valoración y la rareza no se guardan como
  datos editables: se calculan.
- **Rareza**: nivel de la carta (Bronce, Plata u Oro), calculado con el porcentaje de asistencia
  del jugador a entrenamientos en la temporada.

## Criterios de éxito *(obligatorio)*

### Resultados medibles

- **CE-001**: En el 100 % de los intentos probados, una familia no obtiene, ni al publicar ni
  después, la lista de convocados, quiénes jugaron ni lo indicado por otras familias.
- **CE-002**: En el 100 % de los intentos probados, ninguna familia cambia un marcador ya publicado
  por otra o registrado por quien entrena o el PRESIDENTE.
- **CE-003**: En el 100 % de los casos probados, las estadísticas y goleadores coinciden con un
  conteo hecho a partir de lo publicado por las familias y lo corregido por quien entrena.
- **CE-004**: En el 100 % de los casos probados, un cambio de fecha de nacimiento hecho por la
  familia deja al jugador en la misma categoría.
- **CE-005**: En el 100 % de los intentos probados, solo quienes pueden ver la ficha obtienen la
  carta, y solo la familia y el PRESIDENTE la exportan.
- **CE-006**: En el 100 % de las cartas probadas, la valoración es el promedio redondeado de las
  seis estadísticas, y una carta sin estadísticas no muestra números.
- **CE-007**: Ninguna pantalla de la plataforma ordena, compara ni clasifica jugadores por las
  estadísticas o la valoración de su carta.
- **CE-008**: La carta y las pantallas de esta funcionalidad se leen sin desplazamiento horizontal
  en teléfono y en escritorio, en tema claro y oscuro, con los colores de cualquier club probado.

## Supuestos

- El correo de la cuenta no se cambia desde la ficha, ni por la familia ni por el PRESIDENTE (spec
  005, RF-018): es el dato de inicio de sesión de toda la cuenta.
- Un cambio de fecha de nacimiento hecho por la familia no ubica en categoría a un jugador sin
  categoría, aunque la spec 005 (RF-026) sí lo haga cuando corrige el PRESIDENTE: la §8 dice que la
  categoría sigue siendo del PRESIDENTE.
- El marcador publicado por una familia vuelve el partido "jugado" igual que si lo registrara quien
  entrena (spec 009).
- No se avisa a nadie por correo ni notificación cuando una familia publica un resultado; quien
  entrena lo ve al entrar.
- La valoración se redondea a la mitad hacia arriba (por ejemplo, 72,5 da 73).
- La exportación produce un archivo de imagen común (PNG) que se abre en cualquier teléfono o
  computador y sirve para imprimir.
- La temporada es el año calendario en curso, como las categorías por año de nacimiento.
- La familia, quien entrena y el PRESIDENTE comparten el dorsal y la posición sin avisarse entre
  ellos: vale el último cambio.
- La foto de la carta es la foto del jugador de §14.2; no hay una foto aparte para la carta.
- La aplicación está en construcción: no hay datos reales que migrar.

## Fuera de alcance

- Coleccionar cartas dentro de la plataforma: el coleccionismo es con las cartas impresas.
- Cartas de PRESIDENTES, DIRECTIVOS o ENTRENADORES.
- Estadísticas de carta distintas de las seis, o calculadas automáticamente.
- Historial de los valores anteriores de las estadísticas de la carta o de la ficha.
- Imprimir varias cartas a la vez en una sola hoja.
- Publicar otros eventos del partido (tarjetas, asistencias de gol, minutos).
- Avisos por correo o notificaciones de resultados.
