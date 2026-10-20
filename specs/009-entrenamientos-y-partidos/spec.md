# Especificación de funcionalidad: Entrenamientos, partidos, convocatorias y estadísticas deportivas

**Rama de la funcionalidad**: `009-entrenamientos-y-partidos`

**Creada**: 2026-10-11

**Estado**: Borrador

**Entrada**: Descripción del propietario del proyecto: "Revisa la constitución con las
funcionalidades faltantes para abrir una nueva spec." Entre las pendientes, el propietario eligió
entrenamientos y partidos.

**Plataforma**: LFClub

**Constitución aplicable**: versión 4.6.0, en especial §1, §7.1, §7.5, §8, §11.1, §11.2, §11.3,
§13, §14, §14.1, §15, §17, §17.2, §20 y §24.

**Depende de**: spec 001 (base multiclub), spec 003 (categorías, equipos, asignación de
entrenadores, cambio de categoría y retiro), spec 005 (ficha del jugador), spec 007 (inicio por
rol, con la sección "Próximos entrenamientos" reservada) y spec 008 (nombres clicables).

**Completa**: en la spec 007, la sección "Próximos entrenamientos" del inicio, que hasta ahora
mostraba un espacio reservado. A partir de esta funcionalidad muestra los entrenamientos reales.

## Aclaraciones

### Sesión 2026-10-11

- P: ¿Entran los torneos y las tablas de posiciones (§17.1)? → R: No. El partido indica si es
  amistoso o de torneo, con el nombre del torneo como texto libre. Gestionar torneos y digitar las
  tablas de posiciones queda para otra funcionalidad.
- P: ¿Qué puede hacer como entrenador un DIRECTIVO asignado a una categoría (§8, rol Entrenador)?
  → R: Actúa como entrenador en las categorías que tiene asignadas: programa entrenamientos,
  registra asistencia, convoca, ve los partidos próximos de esas categorías y registra resultados,
  igual que un ENTRENADOR. Fuera de ellas sigue con su acceso de solo consulta.
- P: ¿Se puede reactivar una actividad cancelada? → R: Sí. Quien la gestiona puede reactivar un
  entrenamiento o un partido cancelado mientras no haya empezado. Vuelve a programado o a próximo
  con sus categorías, su convocatoria y las respuestas de las familias tal como estaban.

### Ajustes del propietario 2026-10-11 (después de implementar)

- P: ¿Qué pasa cuando no se sabe todavía contra quién se juega? → R: El rival es opcional. Si no se
  escribe, el partido se muestra "Contra Por definir" y el rival se pone después al editarlo.
- P: ¿Cómo evita el entrenador escribir el mismo entreno día tras día? → R: Al programar un
  entrenamiento puede elegir varios días. Se crea un entrenamiento independiente por cada día, que
  después se edita, cancela o reactiva por separado. No hay series enlazadas.
- P: ¿Cómo sabe la familia en el teléfono qué días de la semana tienen actividad? → R: En la tira de
  días, los días con actividad se pintan de otro color, además del número de actividades escrito.
- P: ¿Cómo se corrige un aviso del calendario si cambia la hora, la fecha u otro dato? → R: Ya se
  podía desde el detalle. El botón pasa a llamarse "Editar" y también aparece en la tarjeta del
  calendario para quien gestiona la actividad. Los permisos no cambian (RF-002, RF-008).
- P: ¿Se puede ver más de un día en el calendario del teléfono? → R: Sí. Se ven dos días, uno al
  lado del otro: el elegido y el siguiente (lunes y martes, luego martes y miércoles…). El domingo
  se ve con el sábado.
- P: ¿Cómo responde la familia con menos espacio? → R: Arriba un texto corto, "Confirma
  asistencia", y en una línea una casilla verde con un chulo ("Asistirá") y una roja con una X ("No
  asistirá"). La opción elegida se rellena de su color y se escribe al lado.
- P: ¿Cómo se cargan las fotos sin tanto texto ni cuadros? → R: Con un lápiz pequeño dentro del
  círculo de la foto del jugador, de la foto de perfil y del escudo del club. Sin foto abre el
  selector de archivos; con foto ofrece cambiarla o quitarla. Es un cambio visual de las
  pantallas de las specs 005, 007 y 008; las reglas de quién carga y quita no cambian (§14.2).

## Impacto en la constitución

La §8 (rol Entrenador) dice que lo que puede hacer como entrenador un DIRECTIVO asignado "se
decidirá con las funcionalidades de entrenamientos y convocatorias". Esta especificación toma esa
decisión: en sus categorías asignadas actúa como entrenador. Conviene enmendar la §8 para
registrarlo y precisar en la §17.2 que "los entrenadores de las categorías involucradas" incluye a
los PRESIDENTES y DIRECTIVOS asignados a ellas. Es un cambio de alcance de un rol: debe quedar
enmendado antes de implementar.

**Aplicado**: la enmienda 4.5.1 → 4.6.0 (2026-10-11) incorpora esta decisión en la §8 y la §17.2.

## Escenarios de usuario y pruebas *(obligatorio)*

En esta especificación, "quien entrena una categoría" es cualquier integrante asignado a ella: un
ENTRENADOR, o un PRESIDENTE o DIRECTIVO que además entrena. El PRESIDENTE puede hacer en todas las
categorías lo que quien entrena hace en las suyas.

### Historia de usuario 1 - Programar entrenamientos y que la familia los vea (Prioridad: P1)

Quien entrena una categoría programa un entrenamiento con fecha, hora de inicio, hora de fin,
lugar y una nota opcional, dirigido a una o a varias de sus categorías. Todos los jugadores
activos de esas categorías lo ven en su calendario y en "Próximos entrenamientos" del inicio. Se
puede modificar o cancelar; un entrenamiento cancelado sigue a la vista, marcado como cancelado, y
se puede reactivar mientras no haya empezado.

**Por qué esta prioridad**: es la actividad más frecuente del club y la que la familia más
necesita saber. Llena el espacio que la spec 007 dejó reservado.

**Prueba independiente**: un ENTRENADOR asignado a las categorías 2014 y 2015 programa un
entrenamiento conjunto para las dos; se comprueba que lo ven todas las familias de esas dos
categorías, el PRESIDENTE y los DIRECTIVOS, y que no lo ve la familia de un jugador de la 2016.

**Escenarios de aceptación**:

1. **Dado** quien entrena la categoría 2014, **cuando** programa un entrenamiento para la 2014 con
   fecha, horas y lugar, **entonces** queda guardado y aparece en el calendario y en "Próximos
   entrenamientos" de todas las familias de jugadores activos de la 2014.
2. **Dado** quien entrena la 2014 y la 2015, **cuando** programa un entrenamiento dirigido a las
   dos, **entonces** lo ven las familias de ambas categorías y ningún jugador cambia de categoría.
3. **Dado** un ENTRENADOR asignado solo a la 2014, **cuando** intenta dirigir un entrenamiento a la
   2016, **entonces** el sistema se lo niega.
4. **Dado** el PRESIDENTE, **cuando** programa un entrenamiento para cualquier categoría activa de
   su club, **entonces** queda guardado aunque no esté asignado a ella.
5. **Dado** un entrenamiento programado, **cuando** el PRESIDENTE o quien entrena todas sus
   categorías cambia la fecha, las horas, el lugar o la nota, **entonces** las familias ven los
   datos nuevos.
6. **Dado** un entrenamiento programado, **cuando** se cancela, **entonces** sigue apareciendo a
   quienes lo veían, marcado como cancelado con texto, y ya no admite asistencia.
7. **Dado** un entrenamiento con fin anterior a su inicio, o sin categoría, **cuando** se intenta
   guardar, **entonces** el sistema lo rechaza con un motivo claro.
8. **Dado** un integrante de otro club, **cuando** intenta consultar un entrenamiento de este club
   por su identificador, **entonces** el sistema se lo niega sin revelar si existe.
9. **Dado** "Próximos entrenamientos" en el inicio, **cuando** no hay entrenamientos programados,
   **entonces** se indica que no hay entrenamientos próximos, sin datos de ejemplo.
10. **Dado** un entrenamiento cancelado que todavía no empieza, **cuando** quien lo gestiona lo
    reactiva, **entonces** vuelve a estar programado y las familias lo ven como antes de cancelarse.
    Si ya empezó, el sistema lo rechaza.
11. **Dado** quien entrena la 2014, **cuando** programa un entrenamiento eligiendo tres días,
    **entonces** quedan tres entrenamientos independientes con los mismos datos; cancelar o editar
    uno no cambia los otros.

---

### Historia de usuario 2 - Convocar a un partido sin que nadie se entere de que no fue convocado (Prioridad: P1)

Quien entrena una categoría crea un partido con rival, fecha, hora, lugar, si el club es local o
visitante y si es amistoso o de torneo (con el nombre del torneo), dirigido a una o a varias de sus
categorías. Elige de la lista de jugadores activos de esas categorías a quiénes convoca. Solo los
convocados, quienes entrenan esas categorías y los PRESIDENTES ven el partido mientras no se ha
jugado. Para cualquier otra cuenta ese partido no existe.

**Por qué esta prioridad**: es la regla que más protege a los niños (§17.2) y la razón de ser de
la convocatoria. Sin ella no hay partidos.

**Prueba independiente**: en la categoría 2014 con diez jugadores, quien la entrena convoca a
siete; se comprueba con la cuenta de cada uno de los diez qué aparece en su calendario, en su
inicio, en los contadores y al pedir el partido por su identificador.

**Escenarios de aceptación**:

1. **Dado** quien entrena la 2014, **cuando** crea un partido para la 2014 y convoca a siete de sus
   diez jugadores, **entonces** los siete lo ven en su calendario con rival, fecha, hora, lugar y
   condición de local o visitante.
2. **Dado** uno de los tres jugadores no convocados, **cuando** consulta su calendario, su inicio,
   cualquier listado o contador, o pide el partido por su identificador, **entonces** el partido no
   aparece y el sistema responde como si no existiera.
3. **Dado** un jugador convocado, **cuando** abre el partido, **entonces** no ve la lista de los
   demás convocados.
4. **Dado** un DIRECTIVO no asignado a ninguna de las categorías del partido, **cuando** consulta
   los partidos del club, **entonces** no ve ese partido próximo. **Dado** un DIRECTIVO asignado a
   la 2014, **entonces** sí lo ve y puede gestionarlo como quien entrena.
5. **Dado** el PRESIDENTE, **cuando** consulta los partidos próximos, **entonces** ve todos los del
   club con sus convocados.
6. **Dado** un partido dirigido a la 2014 y la 2015, **cuando** quien entrena ambas convoca,
   **entonces** puede elegir jugadores de las dos categorías y de ninguna otra.
7. **Dado** un partido dirigido a la 2014 y la 2015 y un ENTRENADOR asignado solo a la 2014,
   **cuando** consulta el partido, **entonces** lo ve, pero no puede crearlo, cambiar sus datos ni
   su convocatoria, porque no entrena todas sus categorías.
8. **Dado** un partido próximo, **cuando** quien lo gestiona agrega un jugador a la convocatoria,
   **entonces** ese jugador empieza a verlo; **cuando** quita a un convocado, **entonces** el
   partido desaparece para él sin aviso ni rastro.
9. **Dado** un partido próximo, **cuando** se cancela, **entonces** quienes lo veían lo ven marcado
   como cancelado, y para los demás sigue sin existir.
10. **Dado** un partido de torneo, **cuando** se guarda sin el nombre del torneo, **entonces** el
    sistema lo rechaza; un amistoso no lo pide.
11. **Dado** un partido cancelado que todavía no empieza, **cuando** quien lo gestiona lo reactiva,
    **entonces** vuelve a estar próximo con la misma convocatoria y las mismas respuestas, y sigue
    sin existir para los no convocados.
12. **Dado** un partido creado sin rival, **cuando** lo ve cualquiera que puede verlo, **entonces**
    aparece "Contra Por definir"; **cuando** quien lo gestiona lo edita y escribe el rival,
    **entonces** todos ven el rival nuevo.

---

### Historia de usuario 3 - Registrar el resultado, los goles y quién jugó (Prioridad: P1)

Después del partido, quien lo gestiona registra el marcador (goles del club y goles del rival),
marca cuáles convocados jugaron y anota los goles de cada jugador del club. Desde ese momento el
partido está jugado: su resultado y sus goleadores los ve todo el club. Se puede corregir después,
y las estadísticas se recalculan solas.

**Por qué esta prioridad**: el resultado es lo que alimenta las estadísticas y lo único del
partido que se vuelve público dentro del club.

**Prueba independiente**: se registra un 3-1 con dos goles de un jugador y uno de otro; se
comprueba lo que ve un convocado, un no convocado, un DIRECTIVO no asignado y otro club; luego se
corrige a 3-2 cambiando un goleador y se comprueba que las estadísticas cambian.

**Escenarios de aceptación**:

1. **Dado** un partido cuya hora de inicio ya llegó, **cuando** quien lo gestiona registra el marcador,
   quiénes jugaron y los goles de cada jugador, **entonces** el partido queda como jugado.
2. **Dado** un partido cuya hora de inicio todavía no llega, **cuando** se intenta registrar su resultado,
   **entonces** el sistema lo rechaza.
3. **Dado** el registro de un resultado, **cuando** se marca a quién jugó, **entonces** la lista
   ofrece solo a los convocados, todos marcados de entrada.
4. **Dado** el registro de goles, **cuando** la suma de los goles anotados a jugadores supera los
   goles del club, o se anota un gol a alguien que no jugó, **entonces** el sistema lo rechaza. Si
   la suma es menor, se acepta: la diferencia son goles sin autor registrado (por ejemplo, en
   propia puerta del rival).
5. **Dado** un partido jugado, **cuando** lo consulta cualquier integrante del club, también la
   cuenta de un jugador no convocado o un DIRECTIVO no asignado, **entonces** ve rival, fecha,
   condición de local o visitante, tipo, marcador y goleadores del club.
6. **Dado** un partido jugado, **cuando** lo consulta una cuenta de jugador, **entonces** no ve la
   lista de convocados ni la de quiénes jugaron, solo los goleadores.
7. **Dado** un partido jugado, **cuando** quien lo gestiona corrige el marcador, quiénes jugaron o
   los goles, **entonces** las estadísticas derivadas cambian de inmediato y queda registrado
   quién hizo el último cambio y cuándo.
8. **Dado** un jugador que jugó un partido con la categoría 2014 y después pasó a la 2013,
   **cuando** se consulta ese partido o sus estadísticas, **entonces** el partido sigue contando
   para la 2014.

---

### Historia de usuario 4 - Registrar la asistencia a entrenamientos (Prioridad: P2)

Quien entrena registra, para cada entrenamiento, qué jugadores de sus categorías asistieron. La
lista muestra a los jugadores activos de las categorías del entrenamiento.

**Por qué esta prioridad**: alimenta la estadística de asistencia y es parte del trabajo diario
del entrenador, pero los entrenamientos ya sirven sin ella.

**Prueba independiente**: en un entrenamiento de la 2014 y la 2015, se marcan presentes y
ausentes; se comprueba la estadística de asistencia de varios jugadores y lo que ve cada rol.

**Escenarios de aceptación**:

1. **Dado** un entrenamiento cuya fecha ya llegó, **cuando** quien entrena alguna de sus
   categorías abre la asistencia, **entonces** ve a los jugadores activos de las categorías del
   entrenamiento y marca a cada uno presente o ausente.
2. **Dado** un entrenamiento conjunto de la 2014 y la 2015 y un ENTRENADOR asignado solo a la
   2014, **cuando** registra la asistencia, **entonces** solo puede marcar a los jugadores de la
   2014.
3. **Dado** un entrenamiento futuro o cancelado, **cuando** se intenta registrar asistencia,
   **entonces** el sistema lo rechaza.
4. **Dado** una asistencia ya registrada, **cuando** se corrige, **entonces** la estadística del
   jugador cambia de inmediato.
5. **Dado** una cuenta de jugador, **cuando** consulta sus entrenamientos pasados, **entonces** ve
   solo su propia asistencia, nunca la de otros jugadores.

---

### Historia de usuario 5 - La familia confirma si su jugador asistirá (Prioridad: P2)

La cuenta del jugador confirma, para cada entrenamiento próximo de su categoría y para cada
partido al que fue convocado, si su jugador asistirá o no. Quien entrena ve las respuestas antes
de la actividad.

**Por qué esta prioridad**: la constitución lo da a la cuenta del jugador (§8) y ayuda al
entrenador a planear, pero no bloquea nada.

**Prueba independiente**: tres familias de convocados responden "asistirá", "no asistirá" y nada;
se comprueba lo que ve quien entrena y que ninguna familia ve las respuestas de las demás.

**Escenarios de aceptación**:

1. **Dado** un entrenamiento próximo de su categoría o un partido próximo al que fue convocado,
   **cuando** la familia responde "asistirá" o "no asistirá", **entonces** queda guardado y puede
   cambiarlo hasta la hora de inicio.
2. **Dado** quien entrena alguna de las categorías de la actividad, o el PRESIDENTE, **cuando** la
   consulta, **entonces** ve cuántos y quiénes de los jugadores de su planilla respondieron cada
   opción y quiénes no han respondido.
3. **Dado** una actividad que ya empezó o fue cancelada, **cuando** la familia intenta responder,
   **entonces** el sistema lo rechaza.
4. **Dado** una familia, **cuando** consulta la actividad, **entonces** no ve las respuestas de
   otras familias.
5. **Dado** una respuesta de la familia, **cuando** quien entrena registra la asistencia real o
   quiénes jugaron, **entonces** la respuesta no la reemplaza: son datos distintos.

---

### Historia de usuario 6 - Estadísticas del jugador y goleadores del club (Prioridad: P3)

La ficha del jugador muestra sus estadísticas: partidos jugados, goles y porcentaje de asistencia
a entrenamientos, en total y por categoría. Una pantalla de goleadores muestra, por categoría, a
los jugadores del club con goles, de mayor a menor. Todo se calcula a partir de lo registrado.

**Por qué esta prioridad**: es el resultado visible de las historias anteriores, pero no hace
falta para operar el día a día.

**Prueba independiente**: con varios partidos y entrenamientos registrados, se comparan las
estadísticas mostradas con un conteo hecho a partir de los registros, antes y después de corregir
un partido.

**Escenarios de aceptación**:

1. **Dado** un jugador con partidos y entrenamientos registrados, **cuando** quien puede ver su
   ficha la abre, **entonces** ve partidos jugados, goles y porcentaje de asistencia, y los mismos
   datos separados por la categoría en que ocurrieron.
2. **Dado** un jugador sin registros, **cuando** se abre su ficha, **entonces** las estadísticas
   aparecen en cero, sin error.
3. **Dado** cualquier integrante del club, **cuando** abre la pantalla de goleadores, **entonces**
   ve por categoría el nombre de cada jugador con goles y su número de goles, sin otros datos.
4. **Dado** la pantalla de goleadores, **cuando** se elige una categoría, **entonces** cuenta solo
   los goles marcados con esa categoría, aunque el jugador ya esté en otra.
5. **Dado** un jugador retirado con goles, **cuando** se consulta la pantalla de goleadores,
   **entonces** sus goles siguen contando, porque su historial se conserva (§14.1).
6. **Dado** la ficha del jugador, **cuando** la consulta quien no puede verla (§7.5), **entonces**
   tampoco obtiene sus estadísticas.

---

### Casos límite

- Un jugador que cambia de categoría después de ser convocado a un partido próximo sigue
  convocado: el sistema no modifica una convocatoria manual sin una regla que lo autorice (§17).
  Quien gestiona el partido decide si lo quita.
- Un jugador retirado sale de las convocatorias de los partidos que aún no se han jugado y deja de
  aparecer en las listas de asistencia futuras. Sus partidos jugados, goles y asistencias pasadas
  se conservan.
- Un jugador nuevo en una categoría ve los entrenamientos próximos de esa categoría desde que
  entra, pero no los partidos ya convocados, salvo que lo agreguen a la convocatoria.
- Un jugador que cambia de categoría sigue viendo los entrenamientos pasados de su categoría
  anterior en los que tiene asistencia registrada. Los próximos de esa categoría dejan de
  aparecerle.
- Un ENTRENADOR que pierde la asignación a una categoría deja de ver los partidos próximos de esa
  categoría y de poder gestionar sus entrenamientos. Lo que ya registró se conserva.
- Una categoría desactivada no puede recibir actividades nuevas; sus actividades pasadas se
  conservan con ella.
- Un partido sin convocados se puede guardar: solo lo ven quienes entrenan sus categorías y los
  PRESIDENTES.
- Un jugador convocado que no jugó cuenta como convocado, pero no suma un partido jugado.
- Un partido jugado no se puede cancelar ni volver a "próximo"; solo se corrige su resultado.
- Una actividad dirigida a varias categorías donde una de ellas se desactiva después conserva
  todas sus categorías en el historial.
- Una cuenta con varios jugadores ve, en cada momento, solo las actividades del jugador elegido.
- Un jugador del club que está retirado y entra solo ve el aviso de que ya no está en el club
  (§14.1); no ve sus actividades ni sus estadísticas en ese club.

## Requisitos *(obligatorio)*

### Requisitos funcionales

**Entrenamientos**

- **RF-001**: El sistema DEBE permitir programar un entrenamiento con fecha, hora de inicio, hora
  de fin, lugar, nota opcional y una o varias categorías activas del club como destino. Al
  programarlo DEBE poder elegirse uno o varios días: cada día queda como un entrenamiento
  independiente.
- **RF-002**: Solo DEBEN poder programar, modificar y cancelar un entrenamiento los PRESIDENTES y
  quienes entrenan todas las categorías a las que va dirigido.
- **RF-003**: Un entrenamiento DEBE ser visible para todos los jugadores activos de sus
  categorías, para quienes entrenan alguna de ellas y para los PRESIDENTES y DIRECTIVOS del club,
  y para el jugador que tiene asistencia registrada en él, aunque ya no esté en esas categorías.
- **RF-004**: Un entrenamiento cancelado DEBE seguir visible para quienes lo veían, identificado
  como cancelado con texto, y NO DEBE admitir asistencia ni respuestas de la familia.
- **RF-005**: El sistema DEBE rechazar un entrenamiento sin categorías, con hora de fin anterior a
  la de inicio o dirigido a una categoría inactiva o de otro club.
- **RF-006**: La sección "Próximos entrenamientos" del inicio de cada rol DEBE mostrar los
  entrenamientos próximos que ese rol puede ver, ordenados por fecha, y un mensaje claro cuando no
  hay ninguno.

**Partidos y convocatorias**

- **RF-007**: El sistema DEBE permitir crear un partido con rival opcional (si falta, se muestra
  "Por definir"), fecha, hora, lugar, condición de
  local o visitante, tipo (amistoso o de torneo), nombre del torneo cuando es de torneo, y una o
  varias categorías activas del club.
- **RF-008**: Solo DEBEN poder crear, modificar, cancelar y convocar a un partido los PRESIDENTES y
  quienes entrenan todas sus categorías.
- **RF-009**: La convocatoria DEBE elegirse de la lista de jugadores activos de las categorías del
  partido; NO DEBE admitir jugadores de otras categorías.
- **RF-010**: Un partido que no se ha jugado, incluida su convocatoria, DEBE ser visible
  únicamente para sus jugadores convocados, para quienes entrenan alguna de sus categorías y para
  los PRESIDENTES del club.
- **RF-011**: Para cualquier otra cuenta, un partido que no se ha jugado NO DEBE aparecer en
  calendarios, listados, contadores, inicios ni mensajes, y su consulta por identificador DEBE
  responder igual que la de un partido inexistente.
- **RF-012**: Una cuenta de jugador NO DEBE poder ver la lista de los demás convocados ni la de
  quiénes jugaron, en ningún estado del partido.
- **RF-013**: Agregar o quitar un convocado DEBE surtir efecto de inmediato en lo que ve ese
  jugador. Quitarlo NO DEBE dejarle aviso ni rastro del partido.
- **RF-014**: Un partido cancelado DEBE seguir visible, como cancelado, solo para quienes podían
  verlo antes de cancelarse.

**Resultados y goles**

- **RF-015**: El sistema DEBE permitir registrar el resultado de un partido cuya fecha y hora ya
  llegaron: goles del club, goles del rival, cuáles convocados jugaron y goles de cada jugador.
- **RF-016**: Solo DEBEN poder registrar y corregir el resultado los PRESIDENTES y quienes entrenan
  todas las categorías del partido.
- **RF-017**: El sistema DEBE rechazar un gol anotado a un jugador que no jugó, y que la suma de
  goles por jugador supere los goles del club.
- **RF-018**: Cada registro de un jugador en un partido DEBE conservar la categoría que el jugador
  tenía en ese momento, sin cambiar si después cambia de categoría (§13).
- **RF-019**: Un partido jugado DEBE ser visible para todo integrante del club con rival, fecha,
  condición de local o visitante, tipo, nombre del torneo, marcador y goleadores del club.
- **RF-020**: El sistema DEBE registrar quién hizo el último cambio de un resultado y cuándo.
- **RF-021**: Un partido jugado NO DEBE poder cancelarse ni volver a estado próximo.

**Asistencia y respuestas de la familia**

- **RF-022**: Quien entrena DEBE poder registrar y corregir, para un entrenamiento cuya fecha ya
  llegó y no cancelado, la asistencia de los jugadores activos de las categorías que entrena dentro
  de ese entrenamiento. El PRESIDENTE DEBE poder hacerlo para todas.
- **RF-023**: La cuenta de un jugador DEBE poder responder "asistirá" o "no asistirá" a cada
  entrenamiento próximo de su categoría y a cada partido próximo al que está convocado, y cambiar
  su respuesta hasta la hora de inicio.
- **RF-024**: Quien entrena alguna de las categorías de una actividad, y el PRESIDENTE, DEBEN ver
  las respuestas de las familias y quiénes no han respondido, entre los jugadores de su planilla.
  Ninguna cuenta de jugador DEBE ver las respuestas ni la asistencia de otros
  jugadores.
- **RF-025**: La respuesta de la familia y la asistencia registrada por quien entrena DEBEN ser
  datos distintos; la una no reemplaza a la otra.

**Estadísticas**

- **RF-026**: Las estadísticas de un jugador (partidos jugados, goles y porcentaje de asistencia a
  entrenamientos) DEBEN calcularse a partir de lo registrado en partidos y entrenamientos, nunca
  digitarse como totales (§17).
- **RF-027**: Corregir un resultado o una asistencia DEBE corregir de inmediato las estadísticas
  derivadas.
- **RF-028**: La ficha del jugador DEBE mostrar sus estadísticas, en total y por categoría, solo a
  quienes pueden ver esa ficha (§7.5).
- **RF-029**: El sistema DEBE ofrecer a todo integrante del club una pantalla de goleadores por
  categoría con el nombre de cada jugador y sus goles, sin ningún otro dato del jugador.
- **RF-030**: El porcentaje de asistencia DEBE contar solo los entrenamientos con asistencia
  registrada para ese jugador.

**Aislamiento y cambios en los jugadores**

- **RF-031**: Toda consulta y operación sobre entrenamientos, partidos, asistencias, respuestas y
  estadísticas DEBE quedar limitada al club elegido; nadie de otro club ni el DESARROLLADOR DEBE
  obtener ninguno de esos datos (§7.1 y §8).
- **RF-032**: El backend DEBE aplicar todas las reglas de visibilidad y permisos de esta
  especificación; ocultar datos en la interfaz NO DEBE ser la única barrera (§15 y §17.2).
- **RF-033**: Al retirarse un jugador, el sistema DEBE quitarlo de las convocatorias de los
  partidos que no se han jugado y borrar sus respuestas a los entrenamientos que no han empezado.
  Reactivar después un partido cancelado no lo vuelve a convocar. Sus registros de partidos jugados y asistencias pasadas DEBEN
  conservarse.
- **RF-034**: Un cambio de categoría NO DEBE modificar convocatorias ya hechas ni registros
  pasados.
- **RF-035**: El sistema NO DEBE eliminar físicamente entrenamientos ni partidos; se cancelan.
- **RF-038**: Quien gestiona un entrenamiento o un partido cancelado DEBE poder reactivarlo mientras
  no haya empezado y sus categorías sigan activas. Vuelve a programado o a próximo con sus
  categorías, su convocatoria y las respuestas de las familias como estaban al cancelarse.

**Interfaz**

- **RF-036**: Toda pantalla de esta funcionalidad DEBE funcionar en teléfono y en escritorio, en
  tema claro y oscuro, con la identidad del club, y los estados (programado, cancelado, jugado,
  asistirá, no asistirá, presente, ausente) DEBEN distinguirse por texto además de por color.
- **RF-037**: Los nombres de personas en estas pantallas DEBEN seguir la regla de la spec 008:
  llevan a su ficha cuando quien mira puede abrirla y, si no, se muestran como texto.
- **RF-039**: En el teléfono, la tira de días del calendario DEBE distinguir con otro color los
  días que tienen alguna actividad visible, además de escribir cuántas tienen, y debajo DEBEN
  verse dos días: el elegido y el siguiente (el domingo, con el sábado).
- **RF-041**: La respuesta de la familia DEBE darse con una casilla verde con un chulo
  ("Asistirá") y una roja con una X ("No asistirá") bajo el texto "Confirma asistencia"; la
  elegida DEBE escribirse además con texto (RF-036).
- **RF-040**: Quien gestiona una actividad que no está cancelada ni jugada DEBE tener la opción
  "Editar" en su detalle y en su tarjeta del calendario. Es comodidad: la API sigue decidiendo.
- **RF-042**: Quien gestiona un partido ya jugado DEBE poder corregir el nombre de su rival (o
  dejarlo por definir) sin tocar el marcador ni los goles. Es lo único de un partido jugado, además
  de su resultado, que se puede cambiar (ajuste pedido por el propietario el 2026-10-12).
- **RF-043**: La lista de partidos DEBE separar en su propio bloque "Falta el resultado" a los
  partidos cuya hora ya pasó sin resultado, encima de "Próximos". Siguen sin ser "jugados" hasta que
  se registre el marcador (ajuste pedido por el propietario el 2026-10-12).

### Entidades clave

- **Entrenamiento**: actividad de práctica de un club, con fecha, horas de inicio y fin, lugar,
  nota, estado (programado o cancelado) y una o varias categorías destino.
- **Partido**: encuentro del club contra un rival (opcional mientras está por definir), con fecha, hora, lugar, condición de local o
  visitante, tipo (amistoso o de torneo), nombre del torneo, estado (próximo, cancelado o jugado),
  una o varias categorías, marcador y quién registró el último cambio del resultado y cuándo.
- **Convocatoria** (jugador convocado a un partido): relación entre un partido y un jugador, con
  la categoría que el jugador tenía al convocarlo, si jugó y sus goles en ese partido.
- **Asistencia a entrenamiento**: presente o ausente de un jugador en un entrenamiento, con la
  categoría que tenía ese día.
- **Respuesta de la familia**: "asistirá" o "no asistirá" de un jugador ante un entrenamiento o
  un partido próximo.
- **Estadísticas del jugador**: datos calculados, no guardados como totales editables: partidos
  jugados, goles y porcentaje de asistencia, en total y por categoría.

## Criterios de éxito *(obligatorio)*

### Resultados medibles

- **CE-001**: En el 100 % de los intentos probados, una cuenta no convocada no encuentra ningún
  rastro de un partido próximo: ni en calendario, inicio, listados, contadores, mensajes ni al
  pedirlo por su identificador.
- **CE-002**: En el 100 % de los intentos probados, ninguna cuenta de jugador obtiene la lista de
  convocados, la de quiénes jugaron, ni la asistencia o las respuestas de otros jugadores.
- **CE-003**: En el 100 % de los casos probados, las estadísticas mostradas coinciden con un conteo
  hecho a partir de los registros, también después de corregir un resultado o una asistencia.
- **CE-004**: En el 100 % de los casos probados, un partido jugado antes de un cambio de categoría
  sigue contando para la categoría en que se jugó.
- **CE-005**: En el 100 % de los intentos probados, nadie de otro club ni el DESARROLLADOR obtiene
  entrenamientos, partidos, asistencias o estadísticas de este club.
- **CE-006**: En el 100 % de los intentos probados, un ENTRENADOR o DIRECTIVO no puede crear,
  cambiar ni registrar nada en categorías que no entrena.
- **CE-007**: Todas las familias de una categoría ven un entrenamiento nuevo de esa categoría en
  su inicio y su calendario en cuanto se guarda, sin que nadie tenga que avisarles.
- **CE-008**: Las pantallas de esta funcionalidad se leen y se usan sin desplazamiento horizontal
  de la página en teléfono y en escritorio, en tema claro y en tema oscuro.

## Supuestos

- El lugar de un entrenamiento o de un partido es texto libre. Gestionar sedes o canchas como
  entidades (§14) queda para otra funcionalidad.
- Los entrenamientos y los partidos se dirigen a categorías completas, no a equipos (§11.1 habla
  de categorías). La convocatoria a un partido ya permite elegir a los jugadores de un equipo.
- Al programar se pueden elegir varios días, y cada uno queda como un entrenamiento independiente.
  No hay repetición automática ni series enlazadas.
- Las fechas y horas son las de Colombia, como el resto de la plataforma.
- Un partido pasa a "jugado" solo cuando se registra su resultado; si su hora de inicio pasó sin resultado,
  sigue como próximo para quienes lo veían, con una indicación de que falta el resultado.
- No se envían correos ni notificaciones por entrenamientos, convocatorias o cambios: cada quien
  los ve al entrar. Los "mensajes de convocatoria" de §1 quedan para la funcionalidad de
  comunicados.
- Los DIRECTIVOS no asignados consultan todos los entrenamientos y los partidos jugados del club,
  pero no los partidos próximos (§17.2).
- La aplicación está en construcción: no hay datos reales que migrar.

## Fuera de alcance

- Torneos como entidades y tablas de posiciones (§17.1).
- Arbitraje y su cargo: quedan fuera de la aplicación (§16.6).
- Mensualidades, cargos y pagos.
- Evaluaciones del jugador.
- Comunicados a las familias, mensajes de convocatoria y avisos por correo.
- Sitio público con resultados y goleadores (§9).
- Gestión de sedes o canchas.
- Repetición automática de entrenamientos y series enlazadas (editar varios a la vez).
- Otros eventos del partido distintos de los goles (tarjetas, asistencias de gol, cambios,
  minutos).
- Exportar o sincronizar el calendario con otras aplicaciones.
- Actividades dirigidas a un equipo concreto en lugar de a categorías.
- Qué pasa con las convocatorias, asistencias y respuestas de una cuenta que pasa de JUGADOR a
  ENTRENADOR o DIRECTIVO (§12.2). Lo decide la funcionalidad del cambio de rol. Con el modelo
  actual, la base de datos impide borrar la ficha de un jugador que ya tiene partidos o
  asistencias registrados.
