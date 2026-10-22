# Especificación de funcionalidad: Mensualidades, cargos y pagos

**Rama de la funcionalidad**: `015-mensualidades-cargos-pagos`

**Creada**: 2026-10-20

**Estado**: Implementada (2026-10-20); falta la validación del recorrido de punta a punta con el propietario (T114)

**Entrada**: Descripción del propietario del proyecto: crear la spec 015 de mensualidades, cargos y
pagos a partir de la §16 de la constitución. Las specs 002, 003, 004, 005, 006, 007 y 009 dejaron
esta funcionalidad pendiente: empezar a generar la mensualidad al aprobar a un jugador, el estado de
pago real en la columna "Al día". Decisiones posteriores del propietario: el arbitraje queda fuera
de la aplicación, y por ahora no se usa pasarela de pago. La familia transfiere (Nequi, Bancolombia, Daviplata u otra)
directamente al PRESIDENTE y sube una imagen como soporte, o le paga en mano; el PRESIDENTE actualiza
el estado de cuenta y, al ponerlo al día, la aplicación genera su recibo y una imagen por defecto como resguardo
de pago de la familia.

**Plataforma**: LFClub

**Constitución aplicable**: versión 4.14.0, en especial §7.2, §8, §12.1, §12.4, §13, §16, §17.2 y §20.
La §6.2 y la §16.4 ya reflejan que las familias no pagan por pasarela.

**Depende de**: spec 001 (clubes, roles y sesión), spec 002, 004 y 006 (ingreso de jugadores y
aprobación), spec 003 (categorías y entrenadores asignados), spec 007 (lista de jugadores y columna
"Al día") y spec 014 (conservación de pagos al eliminar una cuenta).

## Impacto en la constitución

La §16 ya define las reglas de fondo. Esta especificación no las cambia; fija lo que la §16 deja
abierto, como supuestos que se pueden corregir antes de planificar:

- Cuándo se genera la mensualidad de cada mes y qué pasa con el mes en que el jugador entra
  (ver Supuestos).
- En qué orden se aplica un pago a varios cargos pendientes (ver Supuestos).

Esta especificación se aparta de la versión 4.12.0 en cuatro puntos, ya aplicados a la constitución
(versión 4.13.0: §6.2, §8, §16.3, §16.4, §16.5, §16.9 y §20):

- No hay pasarela de pago por ahora (§6.2, §16.4): se retira el medio PASARELA de la §16.3 y las
  pruebas de notificaciones idempotentes de la §20 dejan de aplicar.
- Los medios de un pago son EFECTIVO y TRANSFERENCIA; la transferencia indica la entidad (Nequi,
  Bancolombia, Daviplata u otra) y la hace la familia directamente al PRESIDENTE.
- Solo el PRESIDENTE registra pagos (§16.5 y §8 permitían también al ENTRENADOR registrar efectivo).
- El ENTRENADOR no ve nada de pagos ni de estados de cuenta (§8 le permitía consultar el estado de
  pago de sus jugadores y las finanzas generales de solo lectura). Solo el PRESIDENTE, el DIRECTIVO
  y la familia del jugador acceden a este tema.
- Se añade a la familia la posibilidad de informar una transferencia con imagen de soporte, y a
  todo pago confirmado, además del recibo, un resguardo en forma de imagen.

## Clarificaciones

### Sesión 2026-10-20

- Q: ¿Cuánto paga de mensualidad un jugador que el club aprueba a mitad de mes? → A: No paga el mes
  en curso; su primera mensualidad es la del mes siguiente, también al reincorporarse.
- Q: Si la familia quiere pagar más de lo que debe, ¿el sistema lo rechaza o guarda la diferencia
  como saldo a favor? → A: Se rechaza; un pago no puede superar lo que el jugador debe en ese
  momento y no existe saldo a favor.
- Q: Si el PRESIDENTE rechaza una transferencia informada, ¿la familia puede volver a informarla con
  otro soporte? → A: Sí, informa un pago nuevo con otro soporte; el rechazado queda en el historial
  y no se puede reabrir.
- Decisión del propietario (sobre el reparto del arbitraje): el arbitraje queda fuera de la
  aplicación; se eliminó de esta spec y de la constitución (v4.14.0), incluidos su cargo y su costo.
- Q: ¿Puede una familia tener varias transferencias "en revisión" del mismo jugador a la vez? → A:
  Sí, sin límite; el PRESIDENTE decide cuáles confirma y al confirmar solo se aplica lo pendiente.

## Escenarios de usuario y pruebas *(obligatorio)*

### Historia 1 - La mensualidad se genera sola cada mes (Prioridad: P1)

Cada mes el club tiene un cargo de tipo MENSUALIDAD por cada jugador activo, sin que el PRESIDENTE
tenga que crearlo. El valor es el del club, o el propio del jugador si el PRESIDENTE se lo cambió. Un
jugador que el club aprueba paga su primera mensualidad el mes siguiente a su aprobación.

**Por qué esta prioridad**: sin cargos no hay nada que consultar ni pagar; es la base de toda la
funcionalidad y desbloquea lo que las specs anteriores dejaron pendiente.

**Prueba independiente**: con un club de tres jugadores activos y uno retirado, llegar al primer día
de un mes y comprobar que hay exactamente tres cargos MENSUALIDAD nuevos, cada uno con el valor
vigente, y ninguno para el retirado; aprobar a un cuarto jugador, comprobar que no recibe cargo ese mes y que sí lo recibe el mes
siguiente.

**Escenarios de aceptación**:

1. **Dado** un club con jugadores activos, **cuando** empieza un mes, **entonces** cada jugador activo
   tiene un cargo MENSUALIDAD de ese mes con el valor vigente del club o el propio del jugador.
2. **Dado** que el sistema ya generó los cargos del mes, **cuando** el proceso corre otra vez,
   **entonces** no se crean cargos repetidos: un jugador tiene como máximo una MENSUALIDAD por mes.
3. **Dado** un jugador retirado, en espera de aprobación, o una cuenta de ENTRENADOR o DIRECTIVO,
   **cuando** empieza un mes, **entonces** no se le genera mensualidad.
4. **Dado** un jugador que el PRESIDENTE acaba de aprobar (ingreso o hermano agregado), **cuando** se
   aprueba, **entonces** no se le genera cargo por el mes en curso y recibe su primera mensualidad
   cuando empieza el mes siguiente.
5. **Dado** un jugador retirado que se reincorpora, **cuando** vuelve a estar activo, **entonces**
   recibe su primera mensualidad cuando empieza el mes siguiente, sin cargos por el mes en curso ni
   por los meses que estuvo fuera.
6. **Dado** un club suspendido, **cuando** empieza un mes, **entonces** sus cargos se generan igual,
   porque la suspensión congela el acceso y no los datos (§7.4).

---

### Historia 2 - El PRESIDENTE define el valor de la mensualidad (Prioridad: P1)

El PRESIDENTE define el valor de la mensualidad del club y, si lo decide, modifica el valor de uno o
varios jugadores concretos. Los cambios aplican a los cargos que se generen después; los ya
generados conservan su valor. Nadie más puede hacerlo.

**Por qué esta prioridad**: el valor es configuración de cada club (§7.2) y sin él no se pueden
generar cargos; Valfor F.C. parte de $65.000 COP.

**Prueba independiente**: cambiar el valor del club de $65.000 a $70.000, comprobar que los cargos
del mes ya generados siguen en $65.000 y que los del mes siguiente nacen en $70.000; fijar $50.000 a
dos jugadores y comprobar que solo a ellos les cambia.

**Escenarios de aceptación**:

1. **Dado** el PRESIDENTE, **cuando** abre la configuración de la mensualidad, **entonces** ve el
   valor vigente del club y puede cambiarlo.
2. **Dado** un cambio de valor, **cuando** se guarda, **entonces** los cargos ya generados conservan
   su valor y los siguientes usan el nuevo.
3. **Dado** el PRESIDENTE, **cuando** selecciona a uno o varios jugadores y fija un valor propio,
   **entonces** los cargos futuros de cada uno nacen con ese valor, y puede volver a que usen el del
   club.
4. **Dado** un DIRECTIVO, ENTRENADOR o cuenta de jugador, **cuando** intenta cambiar el valor del
   club o de un jugador, **entonces** no puede, también si lo intenta llamando directamente al
   servidor.
5. **Dado** un valor vacío, cero, negativo o con decimales de centavo, **cuando** el PRESIDENTE
   intenta guardarlo, **entonces** se rechaza con un mensaje claro.
6. **Dado** un club sin valor definido, **cuando** empieza un mes, **entonces** no se generan cargos
   de ese club y el PRESIDENTE ve un aviso que le pide definirlo.

---

### Historia 3 - Consultar el estado de cuenta (Prioridad: P1)

Cada rol ve lo que le corresponde de las cuentas. La familia ve el estado de cuenta de su jugador:
sus cargos, sus pagos, su saldo y si está "al día", con "pago parcial" o "debe". El PRESIDENTE y el
DIRECTIVO ven el de todos los jugadores del club. El ENTRENADOR no ve ningún estado de cuenta. La
columna "Al día" de la lista de jugadores (spec 007) deja de estar vacía para el PRESIDENTE y el
DIRECTIVO.

**Por qué esta prioridad**: es lo que hace útil la funcionalidad aunque la familia aún no informe sus pagos;
el club ya puede llevar sus cuentas con los pagos que el PRESIDENTE registre.

**Prueba independiente**: con un jugador con dos mensualidades de $65.000 y un abono de $90.000,
comprobar que su saldo es $40.000, que su estado es "pago parcial" y que la familia ve el detalle
mientras otra familia no ve nada de él.

**Escenarios de aceptación**:

1. **Dado** una cuenta de jugador, **cuando** abre su estado de cuenta, **entonces** ve los cargos
   (tipo, mes, valor, cuánto está pagado), los pagos con su medio y fecha, y el saldo.
2. **Dado** un jugador sin cargos pendientes, **cuando** se consulta, **entonces** figura "al día";
   con algún pago aplicado pero cargos sin cubrir, "pago parcial"; sin ningún pago aplicado a lo
   que debe, "debe".
3. **Dado** una mensualidad sin pagar después del día 5, **cuando** se consulta, **entonces** aparece
   como pendiente, sin recargo, sin intereses y sin ninguna restricción para el jugador.
4. **Dado** el PRESIDENTE o un DIRECTIVO, **cuando** abre las finanzas, **entonces** consulta el
   estado de cuenta de cualquier jugador de su club; el DIRECTIVO solo en consulta.
5. **Dado** un ENTRENADOR, **cuando** recorre la aplicación, **entonces** no ve cargos, pagos,
   saldos, estados de cuenta, recibos, resguardos, soportes ni finanzas de ningún jugador, tampoco
   los de las categorías que tiene asignadas, y no puede consultarlos por identificador.
6. **Dado** una cuenta de jugador, **cuando** intenta ver la cuenta de otro jugador de otro club o
   de otra familia, **entonces** no existe para ella, ni por su identificador.
7. **Dado** la lista de jugadores (spec 007), **cuando** se muestra al PRESIDENTE o al DIRECTIVO,
   **entonces** la columna "Al día" refleja el estado real de cada jugador; al ENTRENADOR no se le
   muestra esa columna.
8. **Dado** un jugador que debe, **cuando** consulta su calendario, convocatorias o entrenamientos,
   **entonces** no cambia nada: deber no restringe ningún acceso (§16.7).
9. **Dado** un saldo, **cuando** se calcula, **entonces** sale solo de cargos y pagos; nadie edita un
   saldo a mano.

---

### Historia 4 - El PRESIDENTE registra un pago recibido y pone al día al jugador (Prioridad: P1)

Cuando la familia le pagó al PRESIDENTE directamente, en efectivo o por transferencia a su cuenta
(Nequi, Bancolombia, Daviplata u otra), el PRESIDENTE abre la cuenta del jugador, registra el pago y
lo deja aplicado a sus cargos. Si cubre lo que debía, el jugador queda "al día". Al registrarlo, la
aplicación genera automáticamente el recibo y el resguardo de pago de la familia (ver Historia 8).

**Por qué esta prioridad**: es el camino principal de cobro mientras no exista pasarela: el club ya
cobra así y solo necesita llevar las cuentas dentro de la aplicación.

**Prueba independiente**: registrar $65.000 en efectivo a un jugador con una mensualidad pendiente y
comprobar que queda "al día", que se generaron su recibo y su resguardo de pago y que el pago muestra quién lo
registró y cuándo.

**Escenarios de aceptación**:

1. **Dado** un jugador con cargos pendientes, **cuando** el PRESIDENTE registra un pago recibido en
   efectivo o por transferencia, **entonces** el pago queda confirmado, aplicado a sus cargos, y el
   saldo baja.
2. **Dado** un pago que cubre todo lo pendiente, **cuando** se registra, **entonces** el jugador
   pasa a "al día"; si cubre solo una parte, a "pago parcial".
3. **Dado** un pago registrado, **cuando** se confirma, **entonces** se generan el recibo y el resguardo de pago
   sin que el PRESIDENTE haga nada más, y la familia lo ve en la cuenta de su jugador.
4. **Dado** que el PRESIDENTE elige "transferencia", **cuando** registra el pago, **entonces**
   indica por qué entidad llegó (Nequi, Bancolombia, Daviplata u otra) y puede añadir una
   referencia; si elige "efectivo" no se le pide entidad.
5. **Dado** un valor mayor al total pendiente, **cuando** se intenta registrar, **entonces** se
   rechaza con un mensaje que indica cuánto debe el jugador (ver Supuestos).
6. **Dado** un jugador sin cargos pendientes, **cuando** se intenta registrar un pago, **entonces**
   se rechaza: un pago siempre se aplica a algún cargo.
7. **Dado** un DIRECTIVO, un ENTRENADOR (también el de la categoría del jugador) o una cuenta de
   jugador, **cuando** intentan registrar un pago, **entonces** no pueden, también si llaman
   directamente al servidor.
8. **Dado** un pago registrado, **cuando** se consulta, **entonces** muestra quién lo registró,
   cuándo, el medio, la entidad (si fue transferencia) y los cargos que cubre.

---

### Historia 5 - El PRESIDENTE publica dónde se le paga (Prioridad: P2)

El PRESIDENTE deja configurados los medios por los que el club recibe pagos: por ejemplo Nequi,
Bancolombia o Daviplata, con el nombre del titular y el número de cuenta, celular o llave. Las
familias del club los ven en la cuenta de su jugador, para saber a dónde transferir.

**Por qué esta prioridad**: sin esto la familia tiene que preguntar por fuera de la aplicación a
dónde pagar, pero el cobro en sí funciona sin ello.

**Prueba independiente**: el PRESIDENTE agrega un Nequi y una cuenta Bancolombia; la familia los ve
en su cuenta y otro club no los ve.

**Escenarios de aceptación**:

1. **Dado** el PRESIDENTE, **cuando** agrega un medio con entidad, titular y número o llave,
   **entonces** queda guardado y lo ven las familias de su club.
2. **Dado** un medio ya publicado, **cuando** el PRESIDENTE lo edita, lo desactiva o lo quita,
   **entonces** el cambio se refleja de inmediato; los pagos ya registrados con esa entidad no
   cambian.
3. **Dado** una familia, **cuando** abre su cuenta, **entonces** ve la lista de medios activos del
   club, con la indicación de enviar el soporte después de pagar.
4. **Dado** un club sin medios publicados, **cuando** la familia abre su cuenta, **entonces** se le
   indica que consulte al PRESIDENTE cómo pagar.
5. **Dado** un DIRECTIVO, ENTRENADOR o cuenta de jugador, **cuando** intentan crear, cambiar o quitar
   un medio, **entonces** no pueden.
6. **Dado** un medio de un club, **cuando** lo consulta otro club, **entonces** no existe para él.

---

### Historia 6 - La familia informa una transferencia con su soporte (Prioridad: P2)

La familia que transfirió al PRESIDENTE lo informa desde la cuenta de su jugador: indica la entidad
por la que pagó, el valor y la fecha, y sube una imagen del soporte (captura o foto del comprobante).
El pago queda "en revisión" y no cambia el saldo hasta que el PRESIDENTE lo confirma o lo rechaza.

**Por qué esta prioridad**: la mayoría de las familias paga por transferencia; el soporte le ahorra
al PRESIDENTE tener que preguntar y verificar por fuera, pero el club puede cobrar sin él (Historia
4).

**Prueba independiente**: informar una transferencia de $65.000 con una imagen, comprobar que el
saldo no cambia; al confirmarla el PRESIDENTE, el jugador queda "al día" y la familia recibe su
resguardo; informar otra y que el PRESIDENTE la rechace con un motivo que la familia ve.

**Escenarios de aceptación**:

1. **Dado** una cuenta de jugador con cargos pendientes, **cuando** informa una transferencia con
   entidad, valor, fecha e imagen de soporte, **entonces** queda "en revisión" y se ve así en su
   estado de cuenta.
2. **Dado** que la familia no sube imagen, o sube un archivo que no es una imagen válida o supera el
   tamaño permitido, **cuando** intenta enviar, **entonces** se rechaza con un mensaje claro y no
   queda nada registrado.
3. **Dado** un pago en revisión, **cuando** el PRESIDENTE abre la solicitud, **entonces** ve el
   valor, la entidad, la fecha, la referencia, la imagen del soporte y los cargos pendientes del
   jugador.
4. **Dado** un pago en revisión, **cuando** el PRESIDENTE lo confirma, **entonces** queda
   confirmado y aplicado a los cargos, el estado de cuenta se actualiza y se generan el recibo y el resguardo.
5. **Dado** un pago en revisión, **cuando** el PRESIDENTE lo rechaza indicando el motivo, **entonces**
   no cambia ningún saldo y la familia ve el rechazo y el motivo, y puede informar un pago nuevo con otro
   soporte; el rechazado no se reabre y queda en el historial.
6. **Dado** el PRESIDENTE al confirmar, **cuando** el valor informado no es el que vio en el
   soporte, **entonces** puede corregir el valor antes de confirmar y queda registrado que lo
   corrigió.
7. **Dado** un ENTRENADOR o un DIRECTIVO, **cuando** intentan confirmar o rechazar un pago en
   revisión, **entonces** no pueden.
8. **Dado** el PRESIDENTE, **cuando** abre sus pendientes, **entonces** ve la lista de pagos en
   revisión de su club, del más antiguo al más reciente, con la cantidad de pendientes.
9. **Dado** una imagen de soporte, **cuando** alguien distinto de la familia de ese jugador, el
   PRESIDENTE o el DIRECTIVO intenta abrirla, **entonces** no existe para esa persona, tampoco por su
   dirección directa.
10. **Dado** una cuenta de jugador, **cuando** informa un pago, **entonces** solo puede hacerlo
    sobre cargos de su propio jugador.

---

### Historia 7 - Anular un pago equivocado (Prioridad: P2)

Un pago confirmado no se edita ni se borra. Si hubo un error, el PRESIDENTE lo anula indicando el
motivo. La anulación queda registrada con su autor, fecha y motivo, y los cargos vuelven a quedar
pendientes por lo que ese pago cubría. Si hay que corregir un valor, se anula y se registra uno
nuevo.

**Por qué esta prioridad**: protege el control del dinero, pero ocurre pocas veces frente a los
registros.

**Prueba independiente**: anular un pago de $65.000 y comprobar que el pago sigue visible como
anulado con autor, fecha y motivo, que el saldo vuelve a subir y que no hay forma de editarlo ni
borrarlo.

**Escenarios de aceptación**:

1. **Dado** un pago confirmado, **cuando** el PRESIDENTE lo anula con un motivo, **entonces** queda
   marcado como anulado, con autor, fecha y motivo, y el saldo se recalcula.
2. **Dado** una anulación sin motivo, **cuando** se intenta, **entonces** se rechaza.
3. **Dado** un pago anulado, **cuando** se consulta, **entonces** sigue en el historial y en el
   estado de cuenta, claramente marcado, y su recibo y su resguardo indican que fue anulado.
4. **Dado** un pago confirmado, **cuando** cualquiera intenta editar su valor, fecha o medio o
   eliminarlo, **entonces** no es posible, tampoco llamando directamente al servidor.
5. **Dado** un pago anulado, **cuando** se intenta anular otra vez, **entonces** se rechaza.
6. **Dado** un ENTRENADOR o un DIRECTIVO, **cuando** intenta anular un pago, **entonces** no puede.
7. **Dado** un pago anulado que venía de una transferencia, **cuando** se consulta, **entonces**
   conserva su imagen de soporte para quienes pueden verla.

---

### Historia 8 - Recibo y resguardo de pago (Prioridad: P2)

Cada vez que un pago queda confirmado, la aplicación genera dos constancias y conserva ambas: el
recibo de la transacción (§16.5), el registro formal y numerado del pago, y el resguardo de pago, una
imagen por defecto que la familia puede descargar o compartir. El resguardo no sustituye al recibo
ni lo elimina: el recibo siempre sigue consultable desde la cuenta del jugador.

**Por qué esta prioridad**: el recibo lo pide la §16.5 y el resguardo es la constancia que la familia
quiere guardar y mostrar; ninguno cambia los saldos.

**Prueba independiente**: confirmar un pago y comprobar que la cuenta del jugador muestra el recibo
y el resguardo del mismo pago, con el mismo número del club, y que el resguardo se puede descargar
como imagen.

**Escenarios de aceptación**:

1. **Dado** un pago confirmado, **cuando** se abre su recibo, **entonces** muestra el club, el
   jugador, el valor, el medio (y la entidad si fue transferencia), la fecha, quién lo registró o
   confirmó, los cargos que cubre, el estado de cuenta resultante y un número consecutivo propio del
   club.
2. **Dado** el mismo pago, **cuando** se abre su resguardo, **entonces** es una imagen con el nombre
   y escudo del club, el jugador, el valor, el medio, la fecha, los cargos que cubre, si el jugador
   quedó "al día" o con saldo pendiente y el mismo número del recibo.
3. **Dado** un pago que se confirma, **cuando** se genera el resguardo, **entonces** el recibo no se
   elimina ni se sustituye: ambos existen y se consultan por separado.
4. **Dado** dos pagos del mismo club, **cuando** se comparan sus recibos, **entonces** tienen números
   distintos y seguidos; dos clubes pueden tener el mismo número.
5. **Dado** un resguardo, **cuando** la familia lo descarga o comparte, **entonces** obtiene la misma
   imagen, con el diseño por defecto del club (su escudo y sus colores).
6. **Dado** un pago en revisión o rechazado, **cuando** se consulta, **entonces** no tiene recibo ni
   resguardo.
7. **Dado** un pago anulado, **cuando** se abren su recibo y su resguardo, **entonces** ambos siguen
   existiendo y se muestran claramente como ANULADO.
8. **Dado** otra familia o un jugador de otro club, **cuando** intenta abrir el recibo o el
   resguardo, **entonces** no existe para ellos.
9. **Dado** el PRESIDENTE o el DIRECTIVO, **cuando** consultan los pagos de un jugador, **entonces**
   pueden abrir su recibo y su resguardo; el ENTRENADOR, aunque sea de la categoría del jugador, no.

---

### Historia 9 - Finanzas generales del club (Prioridad: P3)

El PRESIDENTE y el DIRECTIVO ven las finanzas completas del club: el recaudo del mes, la cartera
pendiente y los totales por categoría. El ENTRENADOR no tiene apartado de finanzas.

**Por qué esta prioridad**: da la visión del club, pero se apoya en todo lo anterior y no es
necesaria para cobrar.

**Prueba independiente**: con pagos de dos categorías en el mes, comprobar que el recaudo, la cartera
y los totales por categoría coinciden con la suma de los pagos y cargos de cada una.

**Escenarios de aceptación**:

1. **Dado** el PRESIDENTE o el DIRECTIVO, **cuando** abre las finanzas, **entonces** ve el recaudo
   del mes, la cartera pendiente, los totales por categoría y la lista de pagos y de jugadores que
   deben.
2. **Dado** un ENTRENADOR, **cuando** busca las finanzas del club, **entonces** no las ve ni puede
   consultarlas por identificador.
3. **Dado** un pago anulado, **cuando** se calcula el recaudo, **entonces** no cuenta.
4. **Dado** un pago en revisión, **cuando** se calcula el recaudo, **entonces** no cuenta hasta que
   se confirme.
5. **Dado** una cuenta de jugador, **cuando** busca las finanzas del club, **entonces** no las ve:
   solo ve las de su propio jugador.
6. **Dado** el DESARROLLADOR, **cuando** intenta ver finanzas o pagos de un club, **entonces** no
   puede (§8).

---

### Casos límite

- Un jugador que cambia de categoría: sus cargos anteriores conservan la categoría con la que se
  generaron (§13) y los totales por categoría no se reordenan hacia atrás.
- Una cuenta con varios jugadores: cada uno tiene su propio estado de cuenta; la familia informa
  sus pagos por jugador y puede informar uno después de otro.
- Un jugador que entra a mitad de mes: no paga el mes en curso; su primera mensualidad es la del mes
  siguiente (ver Clarificaciones).
- Un jugador retirado que debe: conserva sus cargos y pagos, sigue apareciendo en la cartera
  pendiente, pero no genera cargos nuevos.
- Una cuenta eliminada (spec 014): sus pagos se conservan con nombre completo y documento, sus
  cargos pendientes se eliminan y la cartera del club deja de contarlos.
- El club cambia el valor de la mensualidad el día 4 del mes: los cargos de ese mes ya existen y no
  cambian.
- Se registra un pago por un valor con decimales de centavo, o negativo, o cero: se rechaza.
- Dos pagos se confirman al mismo tiempo sobre el mismo cargo: no se debe cubrir más de lo
  que el cargo vale; el segundo se rechaza o se ajusta al saldo restante y se avisa.
- Un jugador cambia de documento o de nombre: los pagos, recibos y resguardos anteriores conservan el nombre y
  documento con que se hicieron.
- La familia informa una transferencia de un cargo que el PRESIDENTE ya registró como pagado por
  otra vía: al confirmar solo se aplica lo pendiente; si no queda nada, el PRESIDENTE la rechaza
  con el motivo.
- La familia informa dos veces la misma transferencia: el PRESIDENTE confirma una y rechaza la
  otra; no hay detección automática de duplicados.
- Se informa una transferencia y, antes de que el PRESIDENTE la revise, este registra el mismo
  pago directamente: la familia debe ver el rechazo con su motivo y no un pago duplicado.
- Se elimina un club (§7.4): se borran con él sus cargos, pagos, recibos, soportes y resguardos.

## Requisitos *(obligatorio)*

### Requisitos funcionales

#### Cargos y saldo

- **RF-001**: Todo lo que un jugador debe DEBE representarse como un cargo de tipo MENSUALIDAD
  u OTRO. Todo dinero recibido DEBE representarse como un pago aplicado a uno o varios
  cargos.
- **RF-002**: El saldo de un jugador DEBE calcularse siempre a partir de sus cargos y de sus pagos
  confirmados no anulados. NO DEBE existir un saldo que alguien pueda editar a mano.
- **RF-003**: El estado de cuenta DEBE derivarse de ese cálculo: "al día" (nada pendiente), "pago
  parcial" (hay pagos aplicados y queda algo pendiente) o "debe" (hay pendientes y nada pagado).
- **RF-004**: DEBEN permitirse abonos parciales a un cargo y a la deuda total.
- **RF-005**: NO DEBEN existir becas, descuentos generales, recargos por mora ni intereses.
- **RF-006**: Los valores DEBEN manejarse en pesos colombianos (COP) con precisión decimal exacta,
  sin errores de redondeo, y mostrarse con formato de moneda colombiana.
- **RF-007**: Cada cargo DEBE conservar el valor con el que se generó, la fecha de generación, su
  tipo, el jugador, su categoría de ese momento y el mes al que corresponde.

#### Mensualidad

- **RF-008**: El sistema DEBE generar cada mes un cargo MENSUALIDAD por cada jugador activo del club,
  una sola vez por jugador y mes, sin intervención de nadie. Generar dos veces no debe duplicar.
- **RF-009**: NO DEBE generarse mensualidad a jugadores retirados, en espera de aprobación ni a
  cuentas de ENTRENADOR o DIRECTIVO, ni a una categoría en la que el jugador no esté activo.
- **RF-010**: Al aprobar a un jugador (ingreso, invitación como JUGADOR, hermano agregado) o al
  reincorporarlo, NO DEBE generarse cargo por el mes en curso; su primera mensualidad DEBE
  generarse cuando empiece el mes siguiente.
- **RF-011**: El valor de la mensualidad es configuración de cada club (valor inicial de Valfor F.C.:
  $65.000 COP) que solo el PRESIDENTE DEBE poder actualizar. NO DEBE estar fijo en el código.
- **RF-012**: El PRESIDENTE DEBE poder seleccionar a uno o varios jugadores y fijar un valor propio
  de mensualidad para cada uno, y volver a dejarlos con el valor del club. Nadie más puede hacerlo.
- **RF-013**: Un cambio de valor (del club o de un jugador) DEBE aplicar solo a los cargos que se
  generen después. Los cargos ya generados conservan su valor.
- **RF-014**: El valor de una mensualidad DEBE ser mayor que cero y múltiplo de un peso.
- **RF-015**: Una mensualidad sin pagar después del día 5 DEBE aparecer como pendiente, sin ninguna
  consecuencia. Deber NO DEBE afectar el acceso, los entrenamientos ni las convocatorias.
- **RF-016**: NO DEBEN enviarse recordatorios de pago, ni automáticos ni manuales (§16.8).

#### Pagos

- **RF-017**: Todo pago DEBE registrar quién lo registró, cuándo, por qué medio (EFECTIVO o
  TRANSFERENCIA), la entidad cuando es transferencia (Nequi, Bancolombia, Daviplata u otra), su
  valor y a qué cargos se aplicó.
- **RF-018**: Un pago confirmado NO DEBE poder editarse ni eliminarse. Un error se corrige con una
  anulación que registra autor, fecha y motivo; el motivo es obligatorio.
- **RF-019**: Solo el PRESIDENTE DEBE poder anular pagos. La anulación DEBE recalcular los saldos y
  dejar el pago y su anulación visibles en el historial.
- **RF-020**: Solo el PRESIDENTE DEBE poder registrar un pago recibido directamente (en efectivo o
  por transferencia a él) y con ello actualizar el estado de cuenta del jugador. El ENTRENADOR, el
  DIRECTIVO y las cuentas de jugador NO.
- **RF-021**: La familia DEBE poder informar una transferencia indicando entidad, valor, fecha y una
  imagen de soporte obligatoria. Ese pago DEBE quedar "en revisión" y NO DEBE alterar saldos,
  estado de cuenta ni recaudo. Un jugador PUEDE tener varios pagos en revisión a la vez, sin límite.
- **RF-022**: Solo el PRESIDENTE DEBE poder confirmar o rechazar un pago en revisión. El rechazo
  exige un motivo que la familia pueda ver. Al confirmar, el PRESIDENTE puede corregir el valor y
  esa corrección DEBE quedar registrada.
- **RF-023**: Un pago DEBE aplicarse únicamente a cargos del mismo jugador y por un valor que no
  supere lo pendiente de esos cargos.
- **RF-024**: Cuando el pago no indica a qué cargos se aplica, DEBE aplicarse a los cargos más
  antiguos primero.
- **RF-025**: Al quedar confirmado un pago, la aplicación DEBE generar y conservar dos constancias: su
  recibo, con un número consecutivo propio de cada club, y su resguardo de pago, una imagen por
  defecto con ese mismo número. La familia DEBE poder ver el recibo y ver y descargar el resguardo
  desde la cuenta de su jugador. El resguardo NO DEBE sustituir ni eliminar el recibo. En un pago
  anulado, ambos DEBEN seguir existiendo e indicar que fue anulado.
- **RF-026**: La plataforma NO DEBE cobrar, recibir ni custodiar dinero ni procesar tarjetas,
  credenciales bancarias ni pagos en línea. El dinero llega directamente al PRESIDENTE o al club;
  la aplicación solo registra lo ocurrido.

#### Medios de pago y soportes

- **RF-027**: El PRESIDENTE DEBE poder crear, editar, desactivar y quitar los medios de pago del
  club (entidad, titular, número, celular o llave, y una nota). Las familias del club DEBEN verlos
  en la cuenta de su jugador. Nadie más puede cambiarlos.
- **RF-028**: Si el club no tiene medios publicados, DEBE mostrarse a la familia un mensaje que le
  indique consultar al PRESIDENTE; aun así puede informar una transferencia.
- **RF-029**: La imagen de soporte DEBE ser JPG, PNG o WebP y no superar el tamaño permitido (ver
  Supuestos); un archivo distinto se rechaza sin registrar el pago.
- **RF-030**: La imagen de soporte DEBE poder verla únicamente la familia de ese jugador, el
  PRESIDENTE y el DIRECTIVO de su club (en consulta). No DEBE ser accesible públicamente ni por su
  dirección directa para nadie más, y el servidor DEBE aplicar la regla.

#### Consulta y autorización

- **RF-031**: La cuenta de un jugador DEBE ver únicamente el estado de cuenta, los pagos y los
  recibos y resguardos de su propio jugador, informar sus transferencias y ver cómo pagar. NO DEBE ver ni
  consultar por identificador los de otro.
- **RF-032**: El PRESIDENTE y el DIRECTIVO DEBEN ver las finanzas completas del club (el DIRECTIVO
  solo en consulta). El ENTRENADOR NO DEBE ver nada de este tema: ni estados de cuenta, ni estado
  de pago, ni cargos, pagos, recibos, resguardos, soportes, medios de pago del club ni finanzas, ni
  siquiera de los jugadores de sus categorías; el servidor DEBE aplicar la regla.
- **RF-033**: El DESARROLLADOR NO DEBE acceder a cargos, pagos, recibos, soportes, resguardos ni
  finanzas de ningún club.
- **RF-034**: La columna "Al día" de la lista de jugadores (spec 007) DEBE mostrar el estado real de
  cuenta de cada jugador.
- **RF-035**: Toda regla de autorización de este módulo DEBE aplicarse en el servidor, no solo en la
  interfaz (§15).
- **RF-036**: Ningún dato financiero de un club DEBE ser visible, contable ni consultable desde otro
  club.

#### Conservación

- **RF-037**: Al eliminar una cuenta (spec 014), sus pagos DEBEN conservarse intactos junto con el
  nombre completo y el documento del jugador, y sus cargos DEBEN dejar de contar en la cartera.
- **RF-038**: Un jugador retirado DEBE conservar sus cargos y pagos, sin generar nuevos.
- **RF-039**: Al eliminar un club (§7.4), DEBEN eliminarse con él sus cargos, pagos, recibos, soportes y resguardos.

### Entidades principales

- **Cargo**: lo que un jugador debe. Tiene tipo (MENSUALIDAD, OTRO), jugador, valor
  cobrado, mes de origen, categoría de ese momento y fecha de generación. Su saldo
  pendiente se calcula, no se guarda.
- **Pago**: dinero recibido de un jugador. Tiene valor, medio (EFECTIVO, TRANSFERENCIA), entidad si
  fue transferencia, estado (en revisión, confirmado, rechazado, anulado), quién lo registró,
  fecha y, si se anuló o rechazó, autor, fecha y motivo. Un pago confirmado no cambia.
- **Aplicación del pago**: la parte de un pago que cubre un cargo concreto; un pago puede cubrir
  varios cargos y un cargo recibir varios pagos.
- **Soporte de pago**: la imagen que la familia sube como comprobante de una transferencia. Pertenece
  al pago y solo la ven la familia, el PRESIDENTE y el DIRECTIVO.
- **Recibo**: el registro formal de un pago confirmado, con número consecutivo propio del club. Se
  conserva siempre, junto al resguardo.
- **Resguardo de pago**: la imagen por defecto que se genera al confirmar un pago, con el número del
  recibo.
- **Medio de pago del club**: entidad, titular y número o llave por los que el club recibe pagos.
- **Valor de la mensualidad**: el del club, más el valor propio que el PRESIDENTE pueda fijar a un
  jugador. Es la configuración vigente; los cargos guardan el valor que cobraron.

## Criterios de éxito *(obligatorio)*

### Resultados medibles

- **CE-001**: Al comenzar un mes, el 100 % de los jugadores activos de un club tienen exactamente un
  cargo MENSUALIDAD de ese mes, sin que nadie lo cree y sin repetidos aunque el proceso corra más de
  una vez.
- **CE-002**: Una familia encuentra el saldo y el estado ("al día", "pago parcial" o "debe") de su
  jugador en menos de 3 toques desde el inicio.
- **CE-003**: El PRESIDENTE registra un pago recibido en menos de 1 minuto y el estado del jugador
  cambia en el acto.
- **CE-004**: Una familia informa una transferencia con su soporte en menos de 2 minutos; el 100 %
  de esos pagos quedan en revisión sin cambiar el saldo hasta que el PRESIDENTE los confirma.
- **CE-005**: Para cualquier jugador, el saldo mostrado coincide al peso con la suma de sus cargos
  menos sus pagos confirmados no anulados.
- **CE-006**: Un cambio de valor de la mensualidad no modifica ninguno de los cargos ya generados en
  el 100 % de los casos probados.
- **CE-007**: El 100 % de los intentos de roles sin permiso (DIRECTIVO, ENTRENADOR, familia ajena,
  DESARROLLADOR) de registrar, confirmar, anular o ver pagos son rechazados por el servidor.
- **CE-008**: Ningún pago confirmado puede modificarse ni eliminarse; el 100 % de las correcciones
  quedan como anulaciones con autor, fecha y motivo.
- **CE-009**: Ningún dato financiero de un club aparece en consultas de otro club.
- **CE-010**: Todo pago confirmado tiene su recibo y su resguardo disponibles para la familia en el momento de
  confirmarse, y ninguna imagen de soporte es accesible para quien no corresponde.

## Supuestos

- **Cuándo se genera el mes**: los cargos de cada mes se generan el día 1 de ese mes, en la hora de
  Colombia. Si el sistema estuvo apagado ese día, se generan en cuanto vuelve a funcionar, sin
  duplicar.
- **Mes de ingreso**: un jugador que entra a mitad de mes no paga ese mes; su primera mensualidad es
  la del mes siguiente (ver Clarificaciones). No se generan cargos retroactivos.
- **Sin saldo a favor** (confirmado en Clarificaciones): el sistema no acepta pagos por más de lo pendiente ni guarda dinero
  anticipado; un pago adelantado de meses futuros no existe porque esos cargos aún no están
  generados.
- **Orden de aplicación**: cuando un pago no indica los cargos, se aplica del más antiguo al más
  reciente. Al confirmar o registrar, el PRESIDENTE puede elegir cargos concretos; la familia no elige al informar.
- **Cargos de tipo OTRO**: existen en el modelo (§16.1) pero esta funcionalidad no incluye una
  pantalla para crearlos.
- **Estado de cuenta "al día"**: se calcula con todos los cargos generados hasta hoy, también la del
  mes en curso aunque todavía esté dentro de los primeros cinco días.
- **Sin pasarela por ahora**: no se cobra en línea; el dinero va directo al PRESIDENTE. La
  confirmación depende de que el PRESIDENTE verifique el soporte o reciba el efectivo; la
  aplicación no verifica automáticamente que la transferencia sea real.
- **Soporte de pago**: una imagen JPG, PNG o WebP de hasta 5 MB por pago.
- **Quién registra y quién ve**: el PRESIDENTE es el único que registra pagos, y solo él, el
  DIRECTIVO (en consulta) y la familia del jugador acceden a este tema. Esto retira al ENTRENADOR lo
  que la §8 y la §16.5 le daban (registrar efectivo, ver el estado de pago de sus jugadores y las
  finanzas generales).
- **Recibo y resguardo**: ambos se generan una sola vez al confirmar el pago y se conservan los dos; el resguardo usa el diseño por defecto del club;
  la familia puede descargarlo o compartirlo como imagen. No se pueden personalizar sus campos.
- **Eliminación de cuenta (spec 014)**: se conserva el pago con nombre completo y documento, pero la
  imagen de soporte, que puede mostrar datos bancarios de la familia, se elimina con la cuenta.
- **Sin recordatorios**: no se envían avisos de cobro, ni siquiera por la campana (§16.8). Sí hay un
  contador de pagos en revisión para el PRESIDENTE dentro de la aplicación, que no es un
  recordatorio de cobro.
- **Aislamiento**: toda esta información pertenece a un club; las reglas de aislamiento de la
  constitución (§7) se cumplen aquí igual que en las demás funcionalidades.

## Fuera de alcance

- Pasarela de pago y cualquier cobro en línea automático (Wompi, PSE, tarjetas). Si se retoma, será
  otra funcionalidad y exigirá aplicar a la constitución §6.2 y §16.4.
- Verificar automáticamente que una transferencia existe en el banco o billetera.
- Recordatorios de pago, automáticos o manuales (§16.8).
- Recargos por mora, intereses, becas, descuentos generales y restricciones por deuda.
- Saldo a favor y pagos anticipados.
- Reembolsos o devoluciones de dinero.
- Facturación electrónica o cumplimiento tributario.
- Cobro a los clubes por el uso de la plataforma (§7.4), que tiene su propio flujo.
- Arbitraje: su costo y su cobro se manejan fuera de la aplicación (§16.6).
- Pantalla para crear cargos de tipo OTRO.
- Exportar o imprimir informes financieros.
- Personalizar el diseño del resguardo.
