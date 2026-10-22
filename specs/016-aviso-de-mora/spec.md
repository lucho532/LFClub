# Especificación de funcionalidad: Aviso fijo de mora en el inicio de la familia

**Rama de la funcionalidad**: `016-aviso-de-mora`

**Creada**: 2026-10-21

**Estado**: Implementada (2026-10-21); falta la validación del recorrido con el propietario (T027 y T028)

**Entrada**: Descripción del propietario del proyecto: en la pantalla de inicio de cada jugador debe
aparecer, cuando esté en mora, un aviso rojo que le recuerde que está en mora. No es una alerta que
se cierra: es un aviso fijo que permanece hasta que el jugador se ponga al día. Cuando se ponga al
día, el aviso pasa a verde y le dice que está al día.

**Plataforma**: LFClub

**Constitución aplicable**: versión 4.15.0 (que ya incluye las enmiendas de esta spec), en especial §16.7, §16.8, §16.9 y §7.

**Depende de**: spec 015 (cargos, pagos y estado de cuenta), spec 007 (inicio por rol) y spec 001
(sesión y rol por club).

## Impacto en la constitución

Esta especificación ajusta dos reglas de la §16, ya aplicadas a la constitución (versión 4.15.0):

- **§16.7**: hoy dice que una mensualidad sin pagar después del día 5 "simplemente aparece como
  pendiente" y "es un dato informativo y no tiene consecuencias". Pasa a decir que, además, el
  inicio de la familia muestra el aviso fijo de mora. Se mantiene todo lo demás: no hay recargo,
  intereses, cargos adicionales ni restricciones de acceso, entrenamientos o convocatorias.
- **§16.8**: hoy dice que no se envían recordatorios de pago. Se mantiene para los envíos (campana,
  correo, mensajes, notificaciones del dispositivo): el aviso fijo no se envía, solo se muestra
  dentro de la aplicación, en el inicio, a la familia del jugador y no al resto del club.

## Clarificaciones

### Sesión 2026-10-21

- Q: Cuando el jugador debe una mensualidad pero aún está dentro del plazo (días 1 a 5 del mes),
  ¿qué aviso debe ver la familia en el inicio? → A: Un aviso intermedio (ámbar), también fijo:
  "Tienes la mensualidad de este mes pendiente; vence el día 5". Pasa a rojo el día 6 y a verde al
  pagar. El aviso tiene así tres estados: verde (sin nada pendiente), ámbar (pendiente dentro del
  plazo) y rojo (en mora).

## Escenarios de usuario y pruebas *(obligatorio)*

### Historia 1 - La familia en mora ve un aviso rojo fijo en su inicio (Prioridad: P1)

La familia de un jugador que está en mora ve en la parte superior de su pantalla de inicio un aviso
rojo que le recuerda que su jugador está en mora. El aviso no se puede cerrar ni ocultar, ni
desaparece con el tiempo: sigue ahí cada vez que abre el inicio hasta que el jugador se ponga al día.

**Por qué esta prioridad**: es el núcleo de lo que pide el propietario; sin esto no hay función.

**Prueba independiente**: con un jugador con la mensualidad del mes anterior sin pagar, abrir el
inicio de su cuenta y comprobar que el aviso rojo está, que no tiene botón para cerrarlo, y que
sigue ahí al recargar, cerrar sesión y volver a entrar.

**Escenarios de aceptación**:

1. **Dado** un jugador en mora, **cuando** su familia abre el inicio del club, **entonces** ve en
   la parte superior un aviso rojo que dice que el jugador está en mora y cuánto debe.
2. **Dado** el aviso rojo, **cuando** la familia intenta cerrarlo, **entonces** no puede: no tiene
   opción de cerrar, ocultar ni posponer, y reaparece al recargar o volver a entrar.
3. **Dado** el aviso rojo, **cuando** la familia lo toca, **entonces** la lleva al estado de cuenta
   de su jugador (spec 015) para ver qué debe y cómo pagar.
4. **Dado** que el color no puede ser lo único que lo dice, **cuando** se muestra el aviso, **entonces**
   incluye un texto y un ícono claros de "en mora", y puede leerlo quien no distingue colores.
5. **Dado** un jugador en mora, **cuando** su familia recorre las demás pantallas (calendario,
   entrenamientos, convocatorias, ficha), **entonces** todo funciona igual que antes: la mora no
   restringe nada (§16.7).

---

### Historia 2 - Al ponerse al día, el aviso pasa a verde (Prioridad: P1)

Cuando el PRESIDENTE registra o confirma un pago que deja al jugador sin mora, el aviso rojo se
reemplaza por uno verde que le dice a la familia que está al día. Si el jugador no está en mora, el
aviso verde es el que aparece.

**Por qué esta prioridad**: es la otra mitad de lo pedido; la familia necesita ver el resultado de
haber pagado.

**Prueba independiente**: con un jugador en mora, registrar como PRESIDENTE un pago que cubre lo
vencido y comprobar que el inicio de la familia pasa de rojo a verde con el texto "al día", sin que
la familia haga nada más que abrir o recargar el inicio.

**Escenarios de aceptación**:

1. **Dado** un jugador en mora, **cuando** el PRESIDENTE registra o confirma un pago que cubre todo
   lo vencido, **entonces** el inicio de la familia muestra el aviso verde "al día" en lugar del rojo.
2. **Dado** un jugador en mora, **cuando** un pago cubre solo una parte y todavía queda algo vencido,
   **entonces** el aviso sigue en rojo y muestra el valor que aún debe.
3. **Dado** una transferencia informada por la familia que está "en revisión", **cuando** el
   PRESIDENTE aún no la confirma, **entonces** el aviso sigue en rojo (un pago en revisión no cambia
   el saldo, spec 015) y indica que hay un pago en revisión.
4. **Dado** un pago confirmado que dejaba al jugador al día y que el PRESIDENTE luego anula,
   **cuando** se anula, **entonces** si vuelve a haber algo vencido el aviso vuelve a rojo.
5. **Dado** un jugador que nunca ha tenido cargos vencidos, **cuando** su familia abre el inicio,
   **entonces** ve el aviso verde "al día".
6. **Dado** el aviso verde, **cuando** se muestra, **entonces** incluye un texto claro de "al día"
   y un ícono, y tampoco depende solo del color.

---

### Historia 3 - Cada jugador tiene su aviso, y solo su familia lo ve (Prioridad: P1)

El aviso es de un jugador y se muestra en el inicio de quien lo representa. Una cuenta con varios
hermanos ve el aviso del jugador con el que entró. Nadie más del club ve el aviso ni lo que dice.

**Por qué esta prioridad**: la información de pagos es privada (§16.9) y el aviso no puede filtrarla.

**Prueba independiente**: con dos hermanos, uno en mora y otro al día, entrar con cada uno y
comprobar que cada inicio muestra su propio aviso; comprobar que el inicio del PRESIDENTE, el
DIRECTIVO y el ENTRENADOR no muestra ninguno y que otra familia no ve nada.

**Escenarios de aceptación**:

1. **Dado** una cuenta con dos hermanos, uno en mora y otro al día, **cuando** entra con el primero,
   **entonces** ve el aviso rojo, y con el segundo ve el verde; al cambiar de jugador el aviso cambia.
2. **Dado** el PRESIDENTE, el DIRECTIVO o el ENTRENADOR, **cuando** abren su inicio, **entonces** no
   ven ningún aviso de mora ni de al día de ningún jugador. Quien además es familia de un jugador
   ve el aviso solo cuando entra como ese jugador.
3. **Dado** una familia, **cuando** consulta la información del aviso, **entonces** solo recibe la de
   su propio jugador, también si lo intenta por el identificador de otro jugador o de otro club (§15,
   §16.9).
4. **Dado** un jugador retirado, **cuando** su familia abre el inicio, **entonces** no se muestra
   aviso de mora; ve lo que ya ve un retirado hoy (spec 001).
5. **Dado** el aviso, **cuando** se muestra, **entonces** no se envía por la campana, correo ni
   notificación del dispositivo (§16.8): solo está en el inicio.

---

### Historia 4 - Qué cuenta como estar en mora (Prioridad: P1)

La mora tiene una regla única y comprensible, para que la familia, el PRESIDENTE y el aviso digan lo
mismo. Un jugador está en mora cuando le queda sin pagar una mensualidad cuyo plazo ya venció. El
plazo de cada mensualidad es el día 5 del mes al que corresponde (§16.7). Hasta ese día no está en
mora, y las mensualidades del mes en curso no cuentan antes de esa fecha.

**Por qué esta prioridad**: el aviso no tiene sentido sin una definición exacta de cuándo se enciende.

**Prueba independiente**: con una mensualidad del mes en curso sin pagar, comprobar que el día 5 el
aviso es ámbar y el día 6 pasa a rojo; pagarla y comprobar que pasa a verde.

**Escenarios de aceptación**:

1. **Dado** una mensualidad de este mes sin pagar, **cuando** es del día 1 al día 5 (hora de
   Colombia), **entonces** el jugador no está en mora y el aviso es ámbar: indica que la
   mensualidad de este mes está pendiente y vence el día 5.
2. **Dado** esa misma mensualidad sin pagar, **cuando** llega el día 6, **entonces** el jugador está
   en mora y el aviso es rojo, sin que nadie tenga que hacer nada.
3. **Dado** una mensualidad de un mes anterior sin pagar, **cuando** se consulta, **entonces** el
   jugador está en mora aunque la del mes en curso esté pagada o dentro del plazo.
4. **Dado** una mensualidad pagada parcialmente, **cuando** pasa el plazo, **entonces** el jugador
   está en mora por lo que falta, y el aviso muestra solo lo que falta.
5. **Dado** un cargo de tipo OTRO sin pagar, **cuando** se calcula la mora, **entonces** no cuenta:
   no tiene plazo definido (spec 015), y no se crea pantalla de cargos OTRO.
6. **Dado** el cambio de día a las 00:00 de Colombia, **cuando** pasa del 5 al 6 del mes, **entonces**
   el cambio de color se produce según la hora de Colombia y no según la del aparato de la familia.
7. **Dado** un jugador en espera de aprobación, **cuando** la familia abre su inicio, **entonces**
   no hay aviso de mora (aún no tiene cargos, spec 015).

---

### Casos límite

- La familia tiene varias mensualidades vencidas: un solo aviso rojo con el total vencido y la
  cantidad de meses, no un aviso por cada mes.
- El jugador está en mora y el PRESIDENTE cambia el valor de la mensualidad: los cargos ya generados
  conservan su valor; el aviso muestra el total vencido de esos cargos.
- La familia tiene la aplicación abierta cuando el PRESIDENTE la pone al día: el aviso se actualiza
  la próxima vez que se consulta el inicio; no se exige actualización instantánea en el aparato.
- No se puede cargar la información del aviso (sin conexión o error): no se muestra un verde por
  defecto, que sería falso; se muestra un aviso discreto de que no se pudo consultar el estado de
  cuenta, y nunca se bloquea el resto del inicio.
- Un jugador que cambia de categoría o de nombre: el aviso no se afecta.
- Se elimina la cuenta del jugador (spec 014): el aviso deja de existir con ella.
- Club suspendido (§7.4): no se accede al inicio; no aplica.

## Requisitos *(obligatorio)*

### Requisitos funcionales

#### Cuándo hay mora

- **RF-001**: Un jugador DEBE considerarse en mora cuando tiene un cargo MENSUALIDAD con saldo
  pendiente cuyo plazo (día 5 del mes al que corresponde, en hora de Colombia, inclusive) ya pasó.
- **RF-002**: Los cargos de tipo OTRO y las mensualidades dentro de plazo NO DEBEN contar para la mora.
- **RF-003**: El valor vencido que muestra el aviso DEBE salir de los cargos y pagos confirmados no
  anulados, igual que el saldo (spec 015, RF-002); los pagos en revisión no lo reducen.
- **RF-004**: El estado de mora DEBE calcularse en el momento de consultarlo con la fecha de
  Colombia; NO DEBE requerir que alguien o un proceso lo encienda o lo apague.
- **RF-005**: Estar en mora NO DEBE generar recargo, interés ni cargo adicional, ni limitar acceso,
  entrenamientos, convocatorias ni ninguna otra función (§16.7).

#### El aviso

- **RF-006**: El inicio de la cuenta de un jugador DEBE mostrar, en la parte superior, un aviso de
  mora en rojo cuando el jugador está en mora, con un texto que lo diga, el valor vencido y la
  cantidad de meses vencidos.
- **RF-007**: El aviso rojo NO DEBE poder cerrarse, ocultarse, posponerse ni descartarse, y DEBE
  mostrarse cada vez que se abre el inicio mientras haya mora.
- **RF-008**: Cuando el jugador no tiene ninguna mensualidad pendiente, el inicio DEBE mostrar un
  aviso verde que indique que está al día.
- **RF-008a**: Cuando el jugador no está en mora pero tiene la mensualidad del mes pendiente dentro
  del plazo (días 1 a 5), el inicio DEBE mostrar un aviso ámbar, también fijo, que indique que esa
  mensualidad está pendiente y vence el día 5. NO DEBE mostrarse verde ni rojo en ese caso.
- **RF-009**: El aviso DEBE comunicar su estado también con texto e ícono, no solo con el color.
- **RF-010**: El aviso DEBE llevar al estado de cuenta del jugador (spec 015) y, si hay pagos en
  revisión de ese jugador, indicarlo.
- **RF-011**: El aviso DEBE ser del jugador con el que la familia está en el inicio, y cambiar al
  cambiar de jugador.
- **RF-012**: Cuando no se pueda consultar el estado de cuenta, NO DEBE mostrarse el aviso verde;
  DEBE mostrarse un aviso neutro de que no se pudo consultar, sin bloquear el resto del inicio.
- **RF-013**: El aviso NO DEBE enviarse por campana, correo, mensaje ni notificación del
  dispositivo (§16.8); solo se muestra dentro del inicio.
- **RF-014**: El aviso NO DEBE mostrarse a jugadores retirados ni en espera de aprobación.

#### Quién lo ve

- **RF-015**: Solo la cuenta del propio jugador DEBE ver el aviso de ese jugador. El PRESIDENTE, el
  DIRECTIVO y el ENTRENADOR NO DEBEN verlo en su inicio por razón de su rol, tampoco el ENTRENADOR
  de la categoría del jugador (§16.9).
- **RF-016**: El servidor DEBE aplicar esa regla: una cuenta no puede obtener el estado de mora de
  otro jugador de otra familia ni de otro club, ni por su identificador (§15, §7).
- **RF-017**: El DESARROLLADOR NO DEBE ver el aviso ni el estado de mora de ningún club (§8).

### Entidades principales

- **Estado de mora de un jugador**: valor calculado, no guardado ni editable: si está en mora o al
  día, el total vencido, la cantidad de meses vencidos y si tiene pagos en revisión. Sale de los
  cargos y pagos de la spec 015.
- **Aviso de mora**: la pieza fija del inicio de la familia que muestra ese estado; rojo si hay mora,
  ámbar si hay mensualidad pendiente dentro del plazo, verde si no hay nada pendiente. No se guarda
  ni se envía.

## Criterios de éxito *(obligatorio)*

### Resultados medibles

- **CE-001**: En el 100 % de los casos probados, un jugador con una mensualidad vencida sin pagar ve
  el aviso rojo al abrir su inicio, sin ninguna acción adicional, y no existe forma de cerrarlo.
- **CE-002**: En el 100 % de los casos probados, el aviso coincide con la situación del jugador:
  verde sin nada pendiente, ámbar con la mensualidad pendiente dentro del plazo, rojo en mora;
  nunca rojo a quien no está en mora ni verde a quien debe algo.
- **CE-003**: Después de que el PRESIDENTE pone al día a un jugador, la familia ve el aviso verde en
  su siguiente apertura o recarga del inicio, sin otra acción suya.
- **CE-004**: El cambio de ámbar a rojo ocurre exactamente al empezar el día 6 en Colombia, en el
  100 % de las pruebas, sin importar la hora del aparato.
- **CE-005**: Una familia entiende en menos de 5 segundos si está en mora o al día y puede llegar a
  su estado de cuenta con un toque.
- **CE-006**: El 100 % de los intentos de ver el estado de mora de otro jugador, de otro club, o de
  verlo desde un rol sin permiso, son rechazados por el servidor.
- **CE-007**: Ninguna función de la aplicación cambia para un jugador en mora, salvo la presencia del
  aviso.

## Supuestos

- **Definición de mora**: mensualidad sin pagar después del día 5 de su mes. Antes de esa fecha, el
  jugador con la mensualidad pendiente no está en mora y ve el aviso ámbar (ver Clarificaciones).
  El aviso habla de mora y plazo; el estado de cuenta (spec 015) sigue hablando de saldo.
- **Tres estados, todos fijos**: verde "Estás al día" (nada pendiente), ámbar (pendiente dentro del
  plazo) y rojo (en mora). Ninguno se puede cerrar.
- **Un aviso por jugador, uno por pantalla**: se muestra solo en el inicio del club; las demás
  pantallas no lo repiten.
- **Actualización**: se calcula cada vez que se abre o recarga el inicio; no hay actualización en
  tiempo real.
- **Hora**: el día cuenta en hora de Colombia (igual que la generación de cargos, spec 015).
- **Mensualidad del mes en que entra el jugador**: no existe cargo ese mes (spec 015, RF-010), por
  lo que un jugador nuevo ve el aviso verde hasta que se genere su primera mensualidad.
- **Aviso sin envío**: no hay recordatorios; este aviso es una indicación permanente dentro de la
  aplicación, no una notificación.
- **Aislamiento**: toda esta información pertenece a un club y a una familia (§7, §16.9).

## Fuera de alcance

- Enviar recordatorios o avisos de mora por campana, correo, mensaje o notificación del dispositivo.
- Que el PRESIDENTE o el DIRECTIVO vean un aviso de mora en su inicio (la lista de jugadores y las
  finanzas de la spec 015 ya les muestran quién debe).
- Recargos, intereses, bloqueos o cualquier consecuencia de estar en mora.
- Cambiar el plazo de pago (día 5) por club o por jugador.
- Permitir que la familia descarte, posponga o silencie el aviso.
- Avisos para cargos de tipo OTRO.
- Actualización en tiempo real del aviso mientras la familia lo tiene abierto.
