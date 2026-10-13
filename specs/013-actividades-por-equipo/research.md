# Investigación: entrenamientos y partidos dirigidos a equipos y a jugadores añadidos

**Funcionalidad**: `013-actividades-por-equipo` | **Fecha**: 2026-10-12

Cada decisión sigue el formato: decisión, razón, alternativas descartadas. No quedó ninguna marca
"NEEDS CLARIFICATION". Lo que se leyó del código está en la sección de cada decisión; las reglas de
negocio son las de la spec y la constitución 4.11.0, y ninguna decisión técnica las cambia.

## 1. Cómo se guarda a quién se dirige un entrenamiento

**Decisión**: dos tablas hijas de `Entrenamientos`, ambas con `ClubId` e `IPerteneceAClub`:

- `EntrenamientosEquipo` (`EntrenamientoId`, `EquipoId`): los equipos elegidos. Sin filas, el
  entrenamiento es de toda la categoría (RF-002, RF-025): los datos ya guardados no necesitan
  migrarse.
- `EntrenamientosJugador` (`EntrenamientoId`, `UsuarioRolId`, `Origen`, `EquipoDeOrigenId`): los
  jugadores que entran sin ser de la audiencia natural. `Origen` es `ANADIDO` (los suma una persona)
  o `EQUIPO_DESACTIVADO` (los congela el sistema, §6).

**Razón**: la audiencia por equipos se calcula con los equipos actuales de cada jugador (supuesto
de la spec), pero los añadidos son una lista guardada. Una fila por pareja evita columnas de lista y
permite que el repositorio traduzca la visibilidad a un `EXISTS`, como ya hace con
`EntrenamientosCategoria`.

**Alternativas**: una columna `uuid[]` en `Entrenamientos` (no se puede indexar por jugador con
claves foráneas ni cascadas); reutilizar `ComunicadoDestino` (modela otra cosa y tiene copia de
nombres).

## 2. Los partidos no necesitan tablas nuevas

**Decisión**: un jugador añadido a un partido es un convocado más: una fila de `PartidosJugador`.
Hoy `ServicioConvocatoria.GuardarAsync` exige que el convocado sea de las categorías del partido; esa
restricción es lo único que se relaja (RF-013). El jugador "añadido" que muestra la planilla (RF-021)
es el convocado cuya categoría copiada no está entre las del partido: es un dato calculado, no una
columna.

**Razón**: la visibilidad de un partido próximo ya es "convocado" (`ReglaVisibilidadDePartido`), la
respuesta, el resultado de la familia y las estadísticas ya funcionan por fila y copian la categoría
del jugador (§11.2, §13). Una categoría copiada distinta de las del partido no rompe nada: nadie
filtra los convocados por las categorías del partido (se revisó `RepositorioEstadisticas`,
`RepositorioPartidos` y `ServicioResultado`).

**Alternativas**: una columna `Anadido` en `PartidosJugador` (duplica lo que ya dice la categoría
copiada y obliga a decidir qué pasa con quien cambia de categoría después).

## 3. La regla de visibilidad de un entrenamiento

**Decisión**: la regla pura `ReglaVisibilidadDeEntrenamiento.LaVe` pasa a recibir tres hechos más
(el entrenamiento tiene equipos, el jugador está en alguno de ellos, el jugador está añadido) y se
apoya en una regla nueva, `ReglaAudienciaDeEntrenamiento.EnLaAudiencia`, que dice si un jugador
pertenece a la audiencia (la usan también responder, avisos y planilla):

| Quien pregunta | Entrenamiento sin equipos | Entrenamiento con equipos |
| --- | --- | --- |
| PRESIDENTE | lo ve | lo ve |
| DIRECTIVO o ENTRENADOR que entrena alguna de sus categorías | lo ve | lo ve |
| DIRECTIVO sin asignar | lo ve (RF-025: como hoy) | **no lo ve** (RF-004, §17.2) |
| ENTRENADOR sin esa categoría | no lo ve | no lo ve |
| JUGADOR | su categoría actual es una de las del entrenamiento | está en alguno de sus equipos |
| JUGADOR añadido | lo ve | lo ve |
| JUGADOR con asistencia registrada | lo sigue viendo | lo sigue viendo |

La última fila es la regla vigente de la 009: quien cambió de categoría o de equipo conserva lo que
ya asistió.

**Nota para el propietario**: hoy un DIRECTIVO sin asignar ve todos los entrenamientos (consulta).
El texto de la §17.2 y el RF-004 dicen que uno dirigido a equipos solo lo ven los jugadores de esos
equipos, los añadidos, quienes entrenan alguna de sus categorías y el PRESIDENTE. Se implementa
literal: el DIRECTIVO sin asignar sigue viendo los de toda la categoría y no ve los de equipos. Si
se prefiere que los vea, es un cambio de una línea en la regla y de una condición en la consulta,
pero contradice la constitución tal como está escrita.

**Cómo llega a SQL**: `QuienPregunta` gana `EquipoIds` (los equipos actuales del jugador de la
petición; vacío para los demás roles), que `AccesoAActividad` lee con una consulta nueva de
`IRepositorioEquipos`. `RepositorioEntrenamientos.Visibles` añade:

```text
|| (esJugador && ( entrenamiento.Jugadores.Any(fila => fila.UsuarioRolId == yo)
                   || asistencia propia
                   || (entrenamiento.Equipos.Any()
                         ? entrenamiento.Equipos.Any(fila => misEquipos.Contains(fila.EquipoId))
                         : entrenamiento.Categorias.Any(fila => fila.CategoriaId == suCategoria))))
```

y el DIRECTIVO deja de entrar en `todos`: ve los que no tienen equipos o los que entrena. Una prueba
de integración compara la regla pura con la consulta por rol, como en la 009.

## 4. Quién dirige: una regla pequeña aparte

**Decisión**: `ReglaDireccionDeActividad.PuedeDirigir(rol, categorias, categoriasQueEntrena)`:
PRESIDENTE siempre; DIRECTIVO o ENTRENADOR si entrena **alguna** de las categorías de la actividad.
Se usa para cambiar equipos y añadidos de un entrenamiento (RF-016, RF-020).

**Razón**: la spec define "quien dirige" así (§17.2) y es distinta de `PuedeGestionar`, que exige
entrenar **todas** las categorías. Se mantienen las dos: programar, cambiar fecha o lugar, cancelar
y convocar siguen siendo de quien gestiona (RF-025); solo la audiencia de un entrenamiento es de
quien dirige. En un partido, sumar a un jugador es convocar, así que sigue siendo de quien gestiona.

**Nota para el propietario**: en un entrenamiento de las categorías 2015 y 2016, el entrenador de
solo la 2015 podrá cambiar equipos y añadidos aunque no pueda cambiarle la hora. Es lo que dice la
spec; si se prefiere que lo haga solo quien gestiona, se cambia la regla por `PuedeGestionar` sin
tocar el resto.

`PermisosDeActividadDto` gana `Dirige` (aditivo) para que la pantalla muestre u oculte el editor.

## 5. Dónde viven las operaciones nuevas

**Decisión**: dos operaciones nuevas y varios cambios aditivos sobre las de la 009:

- `PUT /entrenamientos/{id}/audiencia` con `{ equipoIds, jugadorIds }`: reemplaza los equipos
  activos y los añadidos. Va aparte de `PUT /entrenamientos/{id}` porque lo hace otra persona
  (quien dirige, no quien gestiona) y porque sus avisos son otros (§8).
- `GET /actividades/jugadores-sumables?busqueda=`: búsqueda de jugadores que se pueden sumar, para
  entrenamientos y partidos. Un solo endpoint para ambos.
- `POST /entrenamientos` acepta `equipoIds` y `jugadorAnadidoIds` (ausentes = ninguno).
  `PUT /entrenamientos/{id}` los acepta solo si coinciden con los actuales, igual que ya hace con las
  categorías (`categorias_no_cambian`); si no, 400 `datos_invalidos` (la validación de campos del proyecto) apuntando a la audiencia.
- El detalle, los resúmenes y el calendario añaden `equipos` (nombres). El detalle añade `anadidos`
  solo para quien dirige.

**Contador de contrato**: 109 → 111 operaciones y 85 → 87 de club. El punto de partida de la 012
es 109/85; **confirmar contando con la misma expresión regular de `EndpointsDeLaApi` antes de fiar
de estos números** (la 011 partía de un número equivocado).

**Alternativas**: meter la audiencia en el `PUT` de datos (mezcla permisos y avisos); un endpoint
por entrenamiento y otro por partido para buscar jugadores (duplicado).

## 6. Un equipo que se desactiva (RF-026)

**Hallazgo**: `RepositorioEquipos.DesactivarAsync` borra las filas de `JugadoresEquipo`. Si la
audiencia se calculara solo con los equipos actuales, el entrenamiento próximo de ese equipo dejaría
de verlo todo el mundo.

**Decisión**: al desactivar, en la misma transacción, antes de borrar la pertenencia, el repositorio
copia a `EntrenamientosJugador` con `Origen = EQUIPO_DESACTIVADO` y `EquipoDeOrigenId` a cada
jugador activo del equipo, en cada entrenamiento dirigido a él con `Fecha >= hoy` (cancelados
incluidos, porque se pueden reactivar). El entrenamiento conserva su fila de `EntrenamientosEquipo`,
así que sigue "con equipos" y no pasa a toda la categoría, y no se cancela.

- `ServicioEquipos.DesactivarAsync` recibe `IReloj` para calcular `hoy` en hora de Colombia.
- La planilla y el detalle tratan esas filas como audiencia natural, no como "añadidos" (RF-021).
- Los equipos desactivados que ya estaban en el entrenamiento se conservan al cambiar la audiencia
  (el formulario solo ofrece equipos activos): "sin cambiar de audiencia" (RF-026). Para dejar de
  incluirlos, se cancela y se programa otro.
- Un equipo dirigido por un entrenamiento se marca `Usado` (como ya hace `MarcarUsadaAsync` con las
  categorías) para que `BorrarSiNuncaSeUsoAsync` no lo elimine.

**Alternativa descartada**: no borrar `JugadoresEquipo` al desactivar (cambia la 003 y el recuento de
jugadores del equipo).

## 7. Respuesta, planilla y asistencia

**Hallazgo**: `ServicioRespuestas.ResponderEntrenamientoAsync` y el permiso `Responde` comparan la
categoría del jugador con las del entrenamiento; la planilla de `ServicioAsistencia` trae todos los
jugadores activos de las categorías marcables.

**Decisión**:

- Responder y `Responde` usan `ReglaAudienciaDeEntrenamiento.EnLaAudiencia`: la audiencia natural
  más los añadidos. Tener solo asistencia registrada permite ver, no responder (como hoy).
- La planilla (`FilasAsync`) parte de la audiencia: con equipos, los jugadores activos de esos
  equipos (y de los congelados) que estén en una categoría marcable; sin equipos, como hoy; más los
  añadidos, que aparecen para quien dirige y marcan los que él puede marcar; más los que ya tienen
  asistencia en una categoría marcable. Cada fila lleva `anadido` (RF-021).
- Registrar asistencia exige estar en la planilla, como hoy (`jugador_fuera_de_la_actividad`).
- La asistencia de un añadido copia **su** categoría, así que cuenta en sus estadísticas igual que
  la de cualquier jugador (RF-019).

## 8. Avisos (spec 012)

**Decisión**:

- Nueva cobertura `AUDIENCIA_DE_ENTRENAMIENTO` en `ReglaDestinatariosDeAviso`: los jugadores activos
  de la audiencia (equipos o categorías) más los añadidos, resuelta por `entrenamientoId` en una
  sola consulta del club. Sustituye a `CATEGORIAS` para los hechos de entrenamiento; para uno sin
  equipos y sin añadidos da exactamente lo de hoy (RF-025).
- `AsistenciaRegistrada` avisa a la audiencia cuya categoría copiada está entre las que cambiaron.
- **Cambiar la audiencia** (RF-022): se calcula la audiencia antes y después dentro del bloqueo del
  club. A los que **entran** se les genera el aviso de "nuevo entrenamiento" con cobertura `ALUDIDOS`
  (hecho `ENTRENAMIENTO_PROGRAMADO`, mismo texto; no hace falta un hecho nuevo ni cambiar la
  restricción de la base). A los que **salen** no se les avisa ni se les deja nada (§17.2); sus
  avisos sin leer de esa actividad se borran como borrado del sistema, y su respuesta si ya no ven
  el entrenamiento.
- En un partido ya jugado, la cobertura `CATEGORIAS` suma a los convocados, para que un añadido de
  otra categoría reciba el aviso del resultado (§14.4: "también a los añadidos").
- Sacar a un añadido de un partido ya no avisa (la 012 ya lo cumple).

## 9. Qué jugadores se pueden sumar y qué se muestra de ellos

**Decisión**: el buscador devuelve jugadores activos del club con categoría (los sin categoría no
se pueden convocar hoy: `PartidosJugador.CategoriaId` es obligatoria), solo `usuarioRolId`,
`nombres`, `apellidos` y la categoría (año). Mínimo 2 letras, 20 resultados como máximo, sin
foto, sin enlace a la ficha, sin equipos de otra categoría. Quien dirige ya ve los de sus propias
categorías con su ficha; el buscador no cambia eso.

**Razón**: §8 permite sumar, y sumar sin poder elegir es imposible, pero §7.5 y §8 impiden que
quien suma gane acceso a la ficha o a nada más. Una búsqueda por texto, acotada y con lo mínimo,
revela el menos posible a un entrenador sobre jugadores de otras categorías. Si el propietario
prefiere que solo se pueda sumar desde una lista cerrada, hay que decidir cuál; la spec no lo pide.

## 10. Equipos de la actividad: validación y límites

- Los equipos elegidos deben existir en el club, estar activos y pertenecer a una de las categorías
  de la actividad, que a su vez deben estar activas (RF-001, escenario 1.7). Si no, 422
  `equipo_no_valido` (código nuevo; el resto reutiliza `no_encontrado`, `no_gestiona_actividad`,
  `categoria_inactiva` y `jugador_fuera_de_la_actividad`).
- Con equipos elegidos, la audiencia es **solo** la de esos equipos (RF-003, escenario 1.7: "la
  unión de lo elegido"). Una categoría de la actividad sin equipos elegidos no aporta jugadores. Para
  un entrenamiento conjunto de la 2014 y la 2015 con el equipo A de la 2014, los de la 2015 se suman
  como añadidos o se programa otro entrenamiento. La spec no da otra lectura; ver la nota 4 del plan.
- Quien dirige puede elegir cualquier equipo de sus categorías, también los que su asignación no
  indica dirigir (RF-006, §11.3); no se consulta `EntrenadoresEquipo`.
- Los añadidos deben ser jugadores activos y aprobados del club, con categoría y no retirados.
  Sumar a uno que ya está en la audiencia natural se acepta y no lo duplica (casos límite).
- Un entrenamiento dirigido a un equipo sin jugadores se guarda (casos límite).
- Programar varios días a la vez es del frontend (`SelectorFechas`: una petición por día), así que
  cada día recibe los mismos equipos y añadidos y después cambia por separado (RF-007).
- Cambiar la audiencia solo se permite en un entrenamiento `PROGRAMADO`; sobre uno cancelado, 409
  `actividad_cancelada`, como cambiarlo. No exige que no haya empezado: la spec dice "como hoy trata
  los cambios sobre actividades pasadas" y hoy cambiar solo exige `PROGRAMADO`.

## 11. Concurrencia

**Decisión**: programar y cambiar la audiencia toman el bloqueo del club (`IRepositorioClub.
BloquearAsync`), igual que ya hacen programar, cambiar, cancelar y desactivar una categoría. Así un
cambio de audiencia no se cruza con una desactivación de equipo, y los avisos se calculan sobre la
audiencia leída dentro del bloqueo.

## 12. Tamaño de los archivos (§2.3)

**Hallazgo**: `ServicioEntrenamientos` tiene 228 líneas y `ServicioAsistencia` 190; añadirles lo de
esta spec los llevaría al límite.

**Decisión**: la validación y el cálculo de la audiencia viven en clases nuevas
(`ArmadoDeAudiencia` en `Utilidades` y `ServicioAudienciaDeEntrenamiento`), y las existentes solo
suman llamadas. Se mide con `scripts/verificar-tamano.ps1` **al empezar**; si algo supera 240, se
parte antes de añadir. No se usan clases parciales para esquivar el límite.

## 13. Pantallas

**Decisión** (sin lienzo; con los tokens y componentes del proyecto, §24):

- **Formulario de entrenamiento**: bajo las categorías, "Equipos" con una casilla por equipo activo
  de las categorías elegidas, solo si alguna tiene equipos (RF-005); sin ninguno marcado dice "Toda
  la categoría". Los equipos salen de `GET /categorias`, que ya los trae.
- **Detalle**: todos ven "Para: 2015 · Equipo A"; quien dirige ve además "Jugadores añadidos" con
  "Cambiar audiencia" (casillas de equipos y el buscador). Los añadidos aparecen marcados como tales
  (RF-021).
- **Convocatoria**: la lista se agrupa por equipo (un jugador de varios equipos aparece en cada uno;
  los sin equipo, en su grupo), con "Marcar a todos" por equipo, y el buscador para sumar a otros.
  El convocado de otra categoría se lee "Añadido" en texto.
- El estado y la pertenencia se leen en texto, no solo por color (§24), a 360 px y en los dos temas.

## 14. Supuestos técnicos

- Una migración, `ActividadesPorEquipo`, con las dos tablas, sin datos que migrar.
- Sin dependencias nuevas ni servicios en segundo plano.
- Las rutas nuevas quedan bajo `/api/clubes/{clubId}` y entran solas en las pruebas de aislamiento.
