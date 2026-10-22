# Especificación de funcionalidad: Pago del club por el uso de la plataforma

**Rama de la funcionalidad**: `017-pago-de-la-plataforma`

**Creada**: 2026-10-21

**Estado**: Borrador

**Entrada**: Descripción del propietario del proyecto: cada club le paga al proveedor del software
(el DESARROLLADOR) por el uso de la plataforma mediante la pasarela Wompi. La tarifa es mensual y se
calcula por integrantes activos y rol, con un precio por rol que fija el DESARROLLADOR. Cada mes se
genera el cobro con el valor calculado. Solo el PRESIDENTE ve el cobro pendiente y su botón de pago,
también cuando el club está suspendido. El pago solo se da por confirmado cuando el servidor recibe y
verifica la notificación de la pasarela. Si pasan unos días de gracia sin pago confirmado, el club se
suspende solo; al confirmarse el pago, vuelve a la normalidad de inmediato. El DESARROLLADOR ve en su
panel el estado de pago de cada club y su historial de cobros. Las llaves y secretos de la pasarela
son configuración de la plataforma y nunca se escriben en el repositorio ni se devuelven por la API.

**Plataforma**: LFClub

**Constitución aplicable**: versión 4.16.0 (que ya incluye las enmiendas de esta spec), en especial
§6.2 (pasarela de pago), §7.4 (suspensión y pago por el uso de la plataforma), §8 (roles), §13 y §28.

**Depende de**: spec 001 (clubes, estados y roles), spec 015 (los pagos de las familias al club, que
son un asunto distinto y no pasan por la pasarela) y el resumen de integrantes activos por rol del
panel del DESARROLLADOR (ya existente).

## Impacto en la constitución

Esta especificación cierra la decisión abierta de §28 "Pago por el uso de la plataforma" (valor,
periodicidad, fecha de corte y quién paga) y la concreta en §7.4. La enmienda 4.15.0 → 4.16.0 ya
está aplicada a la constitución:

- **Valor**: suma de integrantes activos por rol multiplicados por el precio de cada rol.
- **Periodicidad**: mensual.
- **Quién paga**: el PRESIDENTE del club (ya dicho en §7.4).
- **Corte y gracia**: el cobro se genera el día 1 de cada mes (hora de Colombia) y el club se
  suspende cuando pasan los días de gracia sin pago confirmado.

Se mantiene todo lo demás de §6.2 y §7.4: la pasarela solo se usa para este cobro, el retorno del
navegador no es prueba de pago y las llaves nunca se escriben en el repositorio.

## Clarificaciones

### Sesión 2026-10-21

- Q: ¿Cuántos días de gracia tiene por defecto un club después de generarse su cobro antes de
  suspenderse por impago? → A: 5 días; el club se suspende al empezar el día 6 (hora de Colombia).
  El DESARROLLADOR puede cambiar ese valor y el cambio aplica a los cobros que se generen después.
- Q: Si el DESARROLLADOR levanta a mano una suspensión por impago mientras el cobro sigue vencido,
  ¿qué pasa? → A: El club queda activo hasta el siguiente cobro: los cobros que ya estaban vencidos
  siguen pendientes y se pueden pagar, pero ya no suspenden al club (RF-016a).
- Q: Al activarse la funcionalidad, ¿cómo se empieza a cobrar a los clubes que ya existen? → A: El
  DESARROLLADOR elige, club por club, desde qué mes se le empieza a cobrar ("mes de inicio del
  cobro"). Un club sin mes de inicio no recibe cobros. Un club nuevo queda con el mes siguiente al
  de su creación como mes de inicio, que el DESARROLLADOR puede cambiar.
- Q: ¿Un pago confirmado levanta la suspensión de un club suspendido a mano por el DESARROLLADOR? →
  A: No. El pago solo levanta la suspensión causada por impago; la suspensión manual se mantiene hasta
  que el DESARROLLADOR la levante (RF-018, Historia 3 escenarios 5 y 7).

## Escenarios de usuario y pruebas *(obligatorio)*

### Historia 1 - El PRESIDENTE ve el cobro del mes y lo paga (Prioridad: P1)

El PRESIDENTE entra a su club y ve que hay un cobro de la plataforma pendiente: el periodo, el
detalle por rol (cuántos integrantes y a qué precio), el total y hasta cuándo puede pagar sin
suspensión. Con un botón lo lleva a la pasarela, donde paga con PSE, Nequi o tarjeta. Al volver, ve
el estado del cobro.

**Por qué esta prioridad**: es el núcleo de lo que pide el propietario; sin esto el club no puede
pagar.

**Prueba independiente**: con un club con un cobro pendiente, entrar como PRESIDENTE, comprobar el
detalle y el total, pulsar pagar, completar el pago de prueba en la pasarela y volver a la aplicación.

**Escenarios de aceptación**:

1. **Dado** un club con un cobro pendiente, **cuando** el PRESIDENTE abre la sección de pago de la
   plataforma, **entonces** ve el periodo, los integrantes por rol con su precio, el total y la fecha
   límite antes de la suspensión.
2. **Dado** el cobro pendiente, **cuando** el PRESIDENTE pulsa pagar, **entonces** es llevado a la
   pasarela con el valor exacto del cobro y puede elegir PSE, Nequi o tarjeta.
3. **Dado** que el PRESIDENTE volvió de la pasarela, **cuando** el servidor aún no ha recibido la
   confirmación, **entonces** el cobro se muestra "en verificación" y no "pagado".
4. **Dado** un club con varios presidentes, **cuando** cualquiera de ellos abre la sección, **entonces**
   ve lo mismo; cuando uno paga, el cobro queda pagado para todos.
5. **Dado** un cobro ya pagado, **cuando** el PRESIDENTE lo consulta, **entonces** ve el comprobante
   (fecha, valor, referencia) y no puede pagarlo otra vez.
6. **Dado** un club sin cobro pendiente, **cuando** el PRESIDENTE abre la sección, **entonces** ve que
   está al día y el historial de sus cobros anteriores.

---

### Historia 2 - El pago solo cuenta cuando la pasarela lo confirma (Prioridad: P1)

Un cobro se marca pagado únicamente cuando el servidor recibe la notificación de la pasarela y
comprueba que es auténtica y que coincide con el cobro (referencia y valor). Que el navegador vuelva
a la aplicación diciendo "pagado" no prueba nada.

**Por qué esta prioridad**: protege el dinero del proveedor y la integridad de la suspensión; es una
regla de la constitución (§7.4).

**Prueba independiente**: enviar al servidor una notificación auténtica de pago aprobado y comprobar
que el cobro pasa a pagado; enviar una falsa, alterada o de otro valor y comprobar que se rechaza y el
cobro no cambia; volver del navegador con parámetros de "aprobado" sin notificación y comprobar que no
cambia nada.

**Escenarios de aceptación**:

1. **Dado** una notificación auténtica de pago aprobado cuya referencia y valor coinciden, **cuando**
   el servidor la recibe, **entonces** el cobro queda pagado, con la fecha y la referencia de la
   pasarela.
2. **Dado** una notificación cuya firma no se puede verificar, **cuando** llega, **entonces** se
   rechaza y no cambia nada.
3. **Dado** una notificación auténtica con un valor distinto al cobro o con una referencia desconocida,
   **cuando** llega, **entonces** no marca el cobro como pagado y queda registrada para revisión.
4. **Dado** una notificación repetida del mismo pago, **cuando** llega otra vez, **entonces** no
   produce un segundo pago ni cambia lo ya confirmado.
5. **Dado** un pago rechazado, anulado o expirado en la pasarela, **cuando** llega su notificación,
   **entonces** el cobro sigue pendiente y el PRESIDENTE puede intentar de nuevo.
6. **Dado** que el PRESIDENTE regresa al navegador con un mensaje de éxito sin notificación, **cuando**
   consulta el cobro, **entonces** no figura como pagado.
7. **Dado** que la notificación llega antes de que el PRESIDENTE vuelva a la aplicación, **cuando**
   vuelve, **entonces** ya ve el cobro pagado.

---

### Historia 3 - Sin pago, el club se suspende solo, y al pagar vuelve (Prioridad: P1)

Si el cobro de un periodo sigue sin pago confirmado cuando terminan los días de gracia, el club queda
suspendido automáticamente (§7.4): solo entra su PRESIDENTE, que sigue viendo el cobro y el botón de
pago; los demás ven el aviso de incidencia temporal. Al confirmarse el pago, el club vuelve a la
normalidad de inmediato.

**Por qué esta prioridad**: es la consecuencia que da sentido al cobro; ya existe la suspensión manual
y esto la automatiza.

**Prueba independiente**: dejar vencer los días de gracia de un cobro sin pagar y comprobar que el club
pasa a suspendido; entrar como PRESIDENTE y como otro rol; pagar y comprobar que el club vuelve a
activo.

**Escenarios de aceptación**:

1. **Dado** un cobro pendiente, **cuando** pasan los días de gracia (hora de Colombia), **entonces** el
   club pasa a suspendido sin que nadie intervenga.
2. **Dado** un club suspendido por impago, **cuando** entra su PRESIDENTE, **entonces** ve claramente
   que está suspendido por falta de pago, el cobro y el botón de pago.
3. **Dado** un club suspendido por impago, **cuando** entra cualquier otro integrante, **entonces** ve
   el aviso de incidencia temporal que le pide comunicarse con el presidente, sin ninguna información
   del cobro ni de su valor.
4. **Dado** un club suspendido por impago, **cuando** se confirma el pago, **entonces** el club vuelve
   a activo de inmediato, sin intervención del DESARROLLADOR, y todos sus datos siguen intactos.
5. **Dado** un club suspendido manualmente por el DESARROLLADOR, **cuando** se confirma un pago,
   **entonces** el club sigue suspendido: el pago no levanta una suspensión manual.
6. **Dado** un club dado de baja, **cuando** pasa el periodo, **entonces** no se le genera cobro ni se
   cambia su estado.
7. **Dado** un club con el cobro del mes ya pagado, **cuando** el DESARROLLADOR lo suspende a mano y
   luego lo levanta, **entonces** el club no queda suspendido por impago.
8. **Dado** un club suspendido por impago con un cobro vencido, **cuando** el DESARROLLADOR levanta la
   suspensión, **entonces** el club queda activo, el cobro sigue pendiente y se puede pagar, y el club
   no vuelve a suspenderse por ese cobro; el siguiente cobro que venza sin pago sí lo suspende.
9. **Dado** un club con más de un cobro vencido, **cuando** se paga uno, **entonces** el club vuelve a
   la normalidad solo si ya no queda ningún cobro vencido pasado el plazo de gracia.

---

### Historia 4 - El cobro mensual se genera solo con el valor correcto (Prioridad: P1)

Cada mes, sin que nadie lo pida, la plataforma genera para cada club activo el cobro del periodo con
los integrantes activos por rol de ese momento y los precios vigentes. Una vez generado, el cobro no
cambia aunque después entren o salgan integrantes o cambien los precios.

**Por qué esta prioridad**: sin cobros generados no hay nada que pagar ni que vencer.

**Prueba independiente**: con precios definidos y un club con integrantes de varios roles, ejecutar la
generación del periodo y comprobar el detalle y el total; repetirla y comprobar que no duplica; cambiar
precios y comprobar que el cobro ya generado no cambia.

**Escenarios de aceptación**:

1. **Dado** el día 1 del mes (hora de Colombia), **cuando** corre la generación, **entonces** cada club
   activo recibe un cobro del periodo con el conteo de integrantes activos por rol, el precio de cada
   rol y el total.
2. **Dado** que la generación se ejecuta más de una vez para el mismo periodo (reinicio, fallo, doble
   ejecución), **cuando** corre otra vez, **entonces** no se crea un segundo cobro del mismo club y
   periodo.
3. **Dado** un cobro generado, **cuando** cambian los precios o los integrantes, **entonces** el cobro
   conserva el detalle y el total con que se generó.
4. **Dado** un club suspendido, **cuando** corre la generación, **entonces** también recibe su cobro
   del periodo, porque sigue usando la plataforma y debe poder ponerse al día.
5. **Dado** un club dado de baja o eliminado, **cuando** corre la generación, **entonces** no recibe
   cobro.
6. **Dado** que la plataforma estuvo fuera de servicio el día 1, **cuando** vuelve a funcionar,
   **entonces** genera los cobros que faltan sin duplicar y la fecha de vencimiento cuenta desde el
   día de generación.
7. **Dado** un periodo cuyo total es cero (club sin integrantes activos o precios en cero), **cuando**
   corre la generación, **entonces** no se genera un cobro por pagar.
8. **Dado** que no hay precios definidos todavía, **cuando** corre la generación, **entonces** no se
   genera ningún cobro y no se suspende ningún club.

---

### Historia 5 - El DESARROLLADOR fija los precios y ve cómo van los clubes (Prioridad: P2)

En su panel, el DESARROLLADOR fija el precio mensual de cada rol (jugador, entrenador, directivo,
presidente) y los días de gracia. También ve, por club, si está al día, con un cobro pendiente o
suspendido por impago, y puede abrir el historial de cobros de cada club.

**Por qué esta prioridad**: sin precios no hay cobros (por eso es necesaria), pero la parte de
consulta del historial puede venir después de que el flujo de pago funcione.

**Prueba independiente**: cambiar un precio y comprobar que el cobro siguiente lo usa; abrir el panel y
comprobar el estado de pago de cada club y su historial.

**Escenarios de aceptación**:

1. **Dado** el panel del DESARROLLADOR, **cuando** fija el precio de un rol o los días de gracia,
   **entonces** el valor queda guardado y aplica a los cobros que se generen desde ese momento, sin
   alterar los ya generados.
2. **Dado** un precio no válido (negativo o no numérico), **cuando** intenta guardarlo, **entonces** se
   rechaza con un mensaje claro.
3. **Dado** la lista de clubes, **cuando** la abre, **entonces** ve por cada club el estado de pago
   (al día, cobro pendiente, vencido o suspendido por impago) y el valor adeudado.
4. **Dado** un club, **cuando** abre su historial, **entonces** ve cada cobro con su periodo, detalle,
   total, estado, fecha de pago y referencia.
5. **Dado** el panel, **cuando** el DESARROLLADOR consulta un club, **entonces** no ve datos personales
   de integrantes ni datos internos del club (§8): solo los conteos por rol y los cobros.
6. **Dado** un club, **cuando** el DESARROLLADOR fija su mes de inicio del cobro, **entonces** el
   primer cobro se genera el día 1 de ese mes; un club sin mes de inicio no recibe cobros, y un mes de
   inicio ya pasado no genera cobros retroactivos.
7. **Dado** un cobro que el DESARROLLADOR sabe que se pagó por fuera de la pasarela, **cuando** lo marca
   como pagado manualmente, **entonces** queda marcado con la indicación de pago manual, su fecha y quién
   lo hizo, y tiene el mismo efecto sobre la suspensión.
8. **Dado** un cobro pendiente, **cuando** el DESARROLLADOR lo anula (por ejemplo por un error de
   conteo), **entonces** queda anulado con su motivo, no se puede pagar y no cuenta para la suspensión.

---

### Historia 6 - Las llaves de la pasarela están protegidas (Prioridad: P1)

Las llaves y secretos de la pasarela son configuración de la plataforma. Nunca se escriben en el
repositorio ni en el código, y ninguna respuesta del servidor las devuelve, ni siquiera al DESARROLLADOR.

**Por qué esta prioridad**: su filtración permitiría falsificar pagos o cobros a nombre del proveedor.

**Prueba independiente**: revisar el repositorio y las respuestas de la API en busca de llaves; arrancar
sin la configuración y comprobar que el pago se indica como no disponible sin romper el resto.

**Escenarios de aceptación**:

1. **Dado** el repositorio, **cuando** se revisa, **entonces** no contiene llaves, secretos ni
   enlaces con credenciales de la pasarela; solo la lista de qué valores deben configurarse y un valor
   de ejemplo vacío.
2. **Dado** cualquier respuesta de la API, **cuando** se inspecciona, **entonces** no incluye llaves ni
   secretos; solo lo necesario para llevar al PRESIDENTE a pagar.
3. **Dado** que la plataforma arranca sin la configuración de la pasarela, **cuando** el PRESIDENTE abre
   la sección, **entonces** ve el cobro y un aviso de que el pago en línea no está disponible, el
   botón no aparece y no se produce ningún error en el resto de la aplicación.
4. **Dado** un cobro vencido y la pasarela no disponible, **cuando** pasan los días de gracia, **entonces**
   el club igual se suspende según la regla, y se levanta con el primer pago confirmado o la marca
   manual del DESARROLLADOR.
5. **Dado** el registro de eventos del servidor, **cuando** se revisa, **entonces** no contiene llaves,
   secretos ni datos de tarjetas o cuentas.

---

### Casos límite

- El PRESIDENTE paga dos veces el mismo cobro (dos pestañas): el segundo pago confirmado no se aplica
  al cobro ya pagado; queda registrado como pago duplicado para que el DESARROLLADOR lo revise y
  devuelva.
- La notificación llega mucho después del pago o fuera de orden (rechazado después de aprobado): el
  estado final lo define el último estado de la pasarela verificado, sin pasar un cobro pagado a
  pendiente por una notificación vieja.
- El PRESIDENTE abre el enlace de pago, no paga y vuelve: el cobro sigue pendiente; puede iniciar uno
  nuevo y los intentos anteriores no ensucian el estado.
- Cambia el valor entre que se abre el pago y se confirma: el pago se compara con el valor del cobro; si
  no coincide, no se confirma.
- El PRESIDENTE es eliminado o pierde el rol antes de pagar: el nuevo presidente ve el cobro; el pago ya
  iniciado se asocia al cobro, no a la persona.
- Un club con un solo presidente que no puede entrar: el DESARROLLADOR puede marcar el pago como
  manual.
- Cambio de mes con un cobro anterior aún vencido: se genera el nuevo cobro, ambos se muestran y se
  suman en el total por pagar.
- Un club creado a mitad de mes: su mes de inicio queda en el mes siguiente y su primer cobro se
  genera el día 1 de ese mes; no se le cobra proporcional.
- Un club existente sin mes de inicio: no recibe cobros ni se suspende por impago hasta que el
  DESARROLLADOR le fije el mes.
- La pasarela está caída: el pago no está disponible y se informa; no se cambia ningún estado por ello.
- Club en eliminación (spec 001): al eliminarse el club se borran también sus cobros (§7.4).
- La hora del servidor difiere de la de Colombia: toda fecha de corte y vencimiento se cuenta en hora de
  Colombia.

## Requisitos *(obligatorio)*

### Requisitos funcionales

#### Precios y cálculo del cobro

- **RF-001**: El DESARROLLADOR DEBE poder fijar el precio mensual de cada rol (JUGADOR, ENTRENADOR,
  DIRECTIVO, PRESIDENTE) y los días de gracia antes de la suspensión.
- **RF-002**: Los precios DEBEN ser valores en pesos colombianos, no negativos, y los cambios aplican
  solo a los cobros que se generen después.
- **RF-003**: El total de un cobro DEBE ser la suma, por cada rol, de los integrantes activos de ese
  rol en el club al generarse el cobro multiplicados por el precio del rol. Se cuenta igual que el
  resumen de integrantes del panel del DESARROLLADOR.
- **RF-004**: Un cobro DEBE guardar de forma fija su periodo, el conteo por rol, el precio por rol, el
  total y su fecha de generación y de vencimiento; después de generado, NO DEBE cambiar por cambios en
  precios ni en integrantes.

#### Generación automática

- **RF-005**: La plataforma DEBE generar solo, el día 1 de cada mes en hora de Colombia, un cobro por
  cada club ACTIVO o SUSPENDIDO con total mayor que cero cuyo mes de inicio del cobro sea ese mes o
  uno anterior.
- **RF-005a**: Cada club DEBE tener un mes de inicio del cobro que fija el DESARROLLADOR. Un club sin
  mes de inicio NO DEBE recibir cobros. Al crear un club, su mes de inicio DEBE quedar en el mes
  siguiente al de su creación. Fijar un mes de inicio pasado NO DEBE generar cobros de meses
  anteriores.
- **RF-006**: La generación DEBE ser idempotente: a lo sumo un cobro por club y periodo, aunque corra
  varias veces o se recupere tras una caída.
- **RF-007**: NO DEBE generarse cobro para clubes dados de baja ni eliminados, ni cuando no hay precios
  definidos.
- **RF-008**: Cada cobro DEBE tener una fecha límite igual a la fecha de generación más los días de
  gracia.

#### Pago por la pasarela

- **RF-009**: El PRESIDENTE DEBE poder iniciar el pago del cobro pendiente y ser llevado a la pasarela
  con el valor exacto y una referencia única del cobro; los medios disponibles son los de la pasarela
  (PSE, Nequi, tarjetas).
- **RF-010**: Un cobro DEBE marcarse pagado únicamente cuando el servidor recibe una notificación de la
  pasarela cuya autenticidad verifica y cuya referencia y valor coinciden con el cobro (§7.4).
- **RF-011**: El retorno del navegador desde la pasarela NO DEBE cambiar el estado del cobro; solo
  sirve para mostrar el estado actual.
- **RF-012**: El procesamiento de notificaciones DEBE ser idempotente y resistente al desorden: una
  notificación repetida o vieja no duplica ni revierte un pago confirmado.
- **RF-013**: Las notificaciones inválidas, de referencia desconocida o de valor distinto DEBEN
  rechazarse sin cambiar ningún cobro y quedar registradas.
- **RF-014**: Cada pago confirmado DEBE guardar la fecha, el valor, la referencia de la pasarela y el
  medio de pago, y no puede modificarse (inmutable, §13).
- **RF-015**: Un pago confirmado de más (duplicado) DEBE quedar registrado y visible para el
  DESARROLLADOR sin aplicarse a otro cobro.

#### Suspensión y reactivación

- **RF-016**: Un club DEBE pasar a SUSPENDIDO automáticamente cuando tiene un cobro sin pago
  confirmado, ni anulado, pasada su fecha límite.
- **RF-016a**: Si el DESARROLLADOR levanta a mano una suspensión por impago, los cobros que en ese
  momento estaban vencidos DEBEN seguir pendientes y pagables, pero NO DEBEN volver a suspender al
  club; solo suspende un cobro posterior que venza sin pago.
- **RF-017**: Un club suspendido por impago DEBE volver a ACTIVO de inmediato cuando ya no tiene ningún
  cobro vencido sin pagar, sin intervención del DESARROLLADOR.
- **RF-018**: Un pago NO DEBE levantar una suspensión manual del DESARROLLADOR; el sistema DEBE
  distinguir entre suspensión por impago y suspensión manual.
- **RF-019**: Un club suspendido DEBE conservar todos sus datos intactos y solo permitir la entrada de
  su PRESIDENTE (§7.4), sin cambio respecto de lo ya existente.

#### Quién ve qué

- **RF-020**: Solo el PRESIDENTE del club DEBE ver el cobro pendiente, su detalle, su historial y el
  botón de pago; ningún otro rol del club, incluido el DIRECTIVO, ve nada de esto, ni siquiera con el
  club suspendido (§7.4).
- **RF-021**: El servidor DEBE aplicar esa regla: un integrante que no es PRESIDENTE, o que es de otro
  club, no puede obtener ni iniciar el pago de un cobro, tampoco por su identificador (§7, §15).
- **RF-022**: El DESARROLLADOR DEBE ver por club su estado de pago y su historial de cobros, sin datos
  personales ni internos del club (§8).
- **RF-023**: El DESARROLLADOR DEBE poder marcar un cobro como pagado manualmente y anular un cobro con
  motivo, dejando constancia de quién y cuándo.
- **RF-024**: El pago de la plataforma NO DEBE mezclarse con los pagos de las familias al club (spec
  015): son flujos, datos y pantallas distintos.

#### Configuración y seguridad

- **RF-025**: Las llaves, secretos y direcciones de la pasarela DEBEN ser configuración de la plataforma
  y NO DEBEN escribirse en el repositorio ni en el código (§6.2, §7.4).
- **RF-026**: Ninguna respuesta de la API ni ningún registro de eventos DEBE contener llaves, secretos ni
  datos de tarjetas o cuentas.
- **RF-027**: Sin la configuración de la pasarela, el pago DEBE indicarse como no disponible sin romper
  el resto de la aplicación; las demás reglas (generación, suspensión, marca manual) siguen funcionando.
- **RF-028**: La integración con la pasarela DEBE quedar detrás de una interfaz definida en Aplicacion,
  para poder cambiar de pasarela sin tocar las reglas de negocio (§6.2).
- **RF-029**: Las fechas de generación, vencimiento y suspensión DEBEN calcularse en hora de Colombia.

### Entidades principales

- **Precios de la plataforma**: precio mensual por rol y días de gracia, definidos por el DESARROLLADOR.
  No es de ningún club.
- **Mes de inicio del cobro**: dato de cada club, fijado por el DESARROLLADOR, que indica desde qué
  mes se le cobra; vacío significa que no se le cobra.
- **Cobro de la plataforma**: lo que un club debe por un periodo mensual: club, periodo, conteo y precio
  por rol, total, fecha de generación, fecha límite y estado (pendiente, pagado, anulado).
- **Pago de la plataforma**: un pago confirmado de un cobro: valor, fecha, referencia de la pasarela,
  medio y origen (pasarela o manual). Inmutable.
- **Intento de pago**: un paso del PRESIDENTE hacia la pasarela para un cobro; no prueba nada hasta la
  notificación.
- **Notificación de la pasarela**: aviso recibido por el servidor, con su resultado de verificación,
  guardado para auditoría e idempotencia.
- **Estado de suspensión del club**: además del estado del club (spec 001), el motivo: impago o manual.

## Criterios de éxito *(obligatorio)*

### Resultados medibles

- **CE-001**: En el 100 % de los periodos probados, cada club activo o suspendido con integrantes recibe
  exactamente un cobro del mes, con el total igual al cálculo por rol, sin intervención de nadie.
- **CE-002**: Un PRESIDENTE puede pasar de abrir su club a completar un pago en la pasarela en menos de
  2 minutos y 5 pasos, sin ayuda.
- **CE-003**: En el 100 % de los casos probados, un cobro solo figura pagado después de una notificación
  auténtica y coincidente; ninguna notificación falsa, alterada, repetida o fuera de orden cambia un
  pago ya confirmado.
- **CE-004**: Un club con un cobro vencido pasada la gracia queda suspendido al inicio del siguiente
  día en Colombia, en el 100 % de las pruebas, sin intervención del DESARROLLADOR.
- **CE-005**: Un club suspendido por impago vuelve a la normalidad en menos de 1 minuto desde que la
  pasarela notifica el pago, sin intervención del DESARROLLADOR.
- **CE-006**: Una suspensión manual nunca se levanta por un pago en el 100 % de los casos probados.
- **CE-007**: El 100 % de los intentos de ver o pagar un cobro por parte de un rol distinto del
  PRESIDENTE, o de otro club, son rechazados por el servidor.
- **CE-008**: No aparece ninguna llave ni secreto de la pasarela en el repositorio ni en las respuestas
  de la API en el 100 % de las revisiones.
- **CE-009**: El DESARROLLADOR identifica en menos de 30 segundos qué clubes están en mora y cuánto
  deben desde su panel.

## Supuestos

- **Fecha de corte**: el cobro se genera el día 1 de cada mes en hora de Colombia.
- **Días de gracia**: valor configurable por el DESARROLLADOR; por defecto, 5 días desde la generación
  (confirmado en Clarificaciones): el club se suspende al empezar el día 6.
- **Valor inicial**: mientras el DESARROLLADOR no defina precios no se genera ningún cobro ni se
  suspende ningún club.
- **Primer cobro**: un club nuevo queda con el mes siguiente a su creación como mes de inicio del
  cobro; los clubes existentes no tienen mes de inicio hasta que el DESARROLLADOR se lo fije (ver
  Clarificaciones). No hay cobro proporcional.
- **Una sola moneda**: pesos colombianos; los valores se manejan en pesos enteros.
- **Presidentes**: cualquier PRESIDENTE del club puede pagar; el pago es del club, no de la persona.
- **Clubes suspendidos**: siguen recibiendo cobro cada mes porque deben ponerse al día.
- **Pago parcial y abonos**: no existen; el cobro se paga completo.
- **Sin recordatorios por correo**: avisar por correo al PRESIDENTE antes del vencimiento no está en
  esta especificación (los recordatorios de la plataforma al club se evalúan aparte).
- **Ambiente de pruebas de la pasarela**: se usa el ambiente de pruebas de Wompi durante el desarrollo
  y las llaves reales solo en producción.
- **Aislamiento**: los cobros pertenecen a un club (§7); solo el DESARROLLADOR ve los de todos, y solo
  en forma resumida.

## Fuera de alcance

- Los pagos de las familias al club (spec 015): siguen siendo registro manual y no pasan por la pasarela.
- Pagos parciales, abonos, descuentos, becas, cupones o precios distintos por club.
- Cobros proporcionales por altas o bajas dentro del mes.
- Facturación electrónica o impuestos.
- Pagos recurrentes automáticos o tarjeta guardada.
- Devoluciones por la pasarela desde la aplicación (el DESARROLLADOR las hace en el panel de la
  pasarela).
- Recordatorios de pago por correo, campana o notificación a los PRESIDENTES.
- Cambiar de pasarela: solo se deja la interfaz preparada.
- Cobrar el pago por varios meses de una vez.
