# Investigación técnica: avisos de entrenamientos y partidos en la campana

**Funcionalidad**: `012-avisos-de-actividades` | **Fecha**: 2026-10-12

Cada decisión sigue el formato: decisión, razón y alternativas descartadas. Lo que no se menciona
queda como en las specs 001 a 011. Todo se apoya en el código ya construido: los servicios de la 009
y la 010, el patrón de destinatarios de la 011 y el filtro de club de la 001.

## 1. Dos tablas nuevas, separadas de los comunicados

**Decisión**: dos tablas nuevas y una migración (`AvisosDeActividad`):

- `AvisosDeActividad`: una fila por aviso: qué ocurrió (`Hecho`), a qué actividad se refiere (tipo e
  identificador), el texto ya redactado y cuándo se generó.
- `AvisosDeActividadDestinatario`: una fila por aviso y por jugador activo que lo recibe, con la
  cuenta dueña y si esa cuenta lo leyó. Es la misma forma que `ComunicadosDestinatario`.

**Razón**: un aviso no tiene autor, título, audiencia ni retiro, y un comunicado sí. Meterlos en
`Comunicados` obligaría a poner nulos en media tabla y a filtrar por tipo en cada consulta de la 011.
Con tablas propias la 011 no cambia y la lista unificada se arma con dos consultas simples
(§3). Guardar a los destinatarios hace que "quien entra después no recibe lo anterior" (RF-011) salga
solo, igual que en la 011.

**Alternativas descartadas**:

- Una columna `Tipo` en `Comunicados` con título y autor nulos: ensucia la tabla y toda su
  consulta, y el retiro y la edición no tienen sentido para un aviso.
- Una tabla genérica de "notificaciones" para ambos: es el patrón que la 011 descartó (§19) y
  obligaría a migrar lo ya construido.
- Calcular los destinatarios al leer: el contador cambiaría solo cuando alguien cambia de categoría
  o sale de una convocatoria (misma razón que la 011, research §1).

## 2. La actividad se guarda como tipo e identificador, sin foránea

**Decisión**: `ActividadTipo` (`ENTRENAMIENTO`, `PARTIDO`) y `ActividadId`, sin clave foránea, con un
índice `(ClubId, ActividadTipo, ActividadId)`.

**Razón**: una actividad nunca se borra (§14), así que no hay filas huérfanas que una foránea deba
evitar, y dos foráneas opcionales con una restricción de "exactamente una" cuestan más código que el
problema que resuelven. El aislamiento lo da `ClubId`, que sí tiene foránea en cascada.

**Alternativas descartadas**: dos columnas `EntrenamientoId` y `PartidoId` con foráneas y una
restricción de comprobación; una sola foránea a una tabla común de actividades que no existe.

## 3. Lista unificada: dos consultas y una mezcla por identificador

**Decisión**: la campana pide a cada repositorio, con el mismo cursor `antesDe` y el límite más uno,
sus filas ordenadas por `Id` descendente, y el servicio las mezcla y toma el límite. `hayMas` es
verdadero si cualquiera de las dos trajo más. El contador es la suma de los dos contadores.

**Razón**: los dos `Id` son `Guid.CreateVersion7()`, que ordena por creación, así que el orden por
`Id` es el orden por fecha y sirve de cursor único (misma idea que la 011, research §9). No hace falta
una vista ni una unión en SQL.

**Trampa a evitar**: `Guid.CompareTo` de .NET **no** ordena igual que PostgreSQL (.NET compara los
campos de la estructura; PostgreSQL compara los 16 bytes sin signo en orden de red). La mezcla en
memoria debe comparar con `ToByteArray(bigEndian: true)`. Hay una prueba unitaria con parejas cuyo
orden difiere entre los dos criterios.

**Alternativas descartadas**: unir las dos tablas con `UNION ALL` en SQL (EF Core lo traduce mal con
agrupaciones y el modelo de lectura de la 011 ya devuelve entidades); ordenar por fecha y
desempatar por identificador (cursor compuesto sin ganancia).

## 4. Qué endpoints cambian: la campana pasa a tener su propia ruta

**Decisión**: una ruta nueva `/api/clubes/{clubId}/campana` con cuatro operaciones: lista unificada,
contador, marcar todos y leer un aviso. De la 011 salen tres operaciones que dejan de tener sentido
solas (la lista de recibidos, su contador y "marcar todos"), y se quedan dos: el detalle de un
comunicado recibido y su lectura (`PUT …/comunicados/recibidos/{id}/lectura`). El contrato pasa de
108 a 109 operaciones y las de club, de 84 a 85.

**Razón**: si la lista, el contador y "marcar todos" siguieran en `/comunicados/recibidos` con otro
significado (solo comunicados), habría dos contadores con números distintos y la pantalla tendría que
elegir. Es mejor un solo lugar. No hay datos reales ni otros clientes (la aplicación está en
construcción), así que mover las tres no rompe a nadie; hay que migrar sus pruebas de integración y
las del frontend.

**Alternativas descartadas**:

- Extender `GET …/comunicados/recibidos` para que devuelva avisos: una ruta llamada "comunicados"
  que devuelve avisos, y filas con `titulo` nulo en un DTO que hoy lo exige.
- Dejar las tres operaciones de la 011 sin cambios y añadir las nuevas: dos contadores distintos y
  código que nadie usa (§19).

## 5. Dónde se generan los avisos

**Decisión**: un servicio nuevo, `ServicioAvisosDeActividad`, con una operación por hecho. Los
servicios de la 009 y la 010 lo llaman **dentro de su propia transacción**, después de guardar el
cambio y solo si hubo un cambio real:

| Servicio existente | Hecho | Aviso |
| --- | --- | --- |
| `ServicioEntrenamientos.ProgramarAsync` | programado | `ENTRENAMIENTO_PROGRAMADO` |
| `ServicioEntrenamientos.CambiarAsync` | fecha, hora o lugar distintos | `ENTRENAMIENTO_CAMBIADO` |
| `ServicioEntrenamientos.CancelarAsync` / `ReactivarAsync` | cambio de estado | `…_CANCELADO` / `…_REACTIVADO` |
| `ServicioAsistencia.RegistrarAsync` | alguna asistencia nueva o distinta | `ASISTENCIA_REGISTRADA` |
| `ServicioPartidos.CambiarAsync` | fecha, hora o lugar distintos | `PARTIDO_CAMBIADO` |
| `ServicioPartidos.CancelarAsync` / `ReactivarAsync` | cambio de estado | `…_CANCELADO` / `…_REACTIVADO` |
| `ServicioConvocatoria.GuardarAsync` | altas (las bajas no avisan) | `JUGADOR_CONVOCADO` |
| `ServicioResultado.RegistrarAsync` | marcador nuevo o distinto | `RESULTADO_PUBLICADO` |
| `ServicioResultadoFamilia.PublicarAsync` | primer marcador | `RESULTADO_PUBLICADO` |

`ServicioPartidos.CrearAsync` no genera aviso: un partido nace sin convocados (la convocatoria es
otra operación) y un partido próximo solo se avisa a los convocados (RF-003). El primer aviso del
partido es el de su primera convocatoria.

**Razón**: las reglas de a quién y qué dice quedan en un solo lugar (RF-031), cada servicio añade
unas pocas líneas y todo ocurre en la misma transacción que el cambio, de modo que no puede quedar un
cambio sin aviso ni un aviso sin cambio.

**Alternativas descartadas**:

- Eventos de dominio o un interceptor de `SaveChanges`: los servicios actuales cambian con
  `ExecuteUpdate`, que no pasa por el seguimiento de cambios, así que el interceptor no vería nada.
- Un servicio en segundo plano que compare estados: añade un proceso y un retraso, y no sabe
  quién hizo el cambio.
- Poner la lógica de avisos dentro de cada servicio: duplicaría destinatarios y textos en seis
  clases y varias pasarían de 250 líneas (§2.3).

**Consecuencia en los servicios**: `CambiarAsync`, `CancelarAsync` y `ReactivarAsync` de las dos
actividades, `RegistrarAsync` de asistencia y `GuardarAsync` de la convocatoria pasan a ejecutarse
en `EnTransaccionAsync`, tomando el bloqueo del club (`IRepositorioClub.BloquearAsync`) y volviendo
a leer la actividad dentro, como ya hacen `ServicioResultado` y `ServicioResultadoFamilia` (spec 010,
research §3). Así dos cambios a la vez no comparan contra datos viejos.

## 6. Detectar "no cambió nada" (RF-004) y la idempotencia (RF-026)

**Decisión**:

- La regla pura `ReglaCambiosDeActividad` compara fecha, hora (inicio o fin) y lugar de antes y de
  después y devuelve qué cambió. Cambiar solo la nota del entrenamiento, o el rival, la condición o
  el torneo del partido, no genera aviso (RF-029: la spec solo pide fecha, hora y lugar).
- `IRepositorioAsistencias.RegistrarAsync` pasa a devolver las categorías en las que alguna
  asistencia fue nueva o cambió de valor.
- `ServicioConvocatoria.GuardarAsync` ya calcula las altas y las bajas; sin altas, no hay aviso.
- `ServicioResultado.RegistrarAsync` avisa solo si el partido no estaba jugado o si algún goles del
  marcador cambia. Corregir solo los goles de un jugador, o el rival, no avisa.
- No hay clave de idempotencia: reintentar la misma operación no vuelve a cambiar nada (o falla con
  409, como cancelar dos veces), así que no genera otro aviso. Es el mismo cambio detectado, no un
  mecanismo aparte.

**Razón**: la spec exige que un cambio idéntico no avise y que un reintento no duplique; las dos
cosas salen del mismo dato (si algo cambió) sin guardar claves.

**Alternativas descartadas**: una clave generada por la pantalla como en los comunicados (research
011 §6): allí publicar es una operación nueva cada vez; aquí la operación ya es idempotente por su
efecto.

## 7. A quién llega

**Decisión**: `ReglaDestinatariosDeAviso` decide la cobertura y el repositorio la resuelve en una
consulta del club:

| Hecho | Cobertura | Quiénes |
| --- | --- | --- |
| Cualquiera de entrenamiento | `CATEGORIAS` | jugadores activos en las categorías del entrenamiento |
| Asistencia registrada | `CATEGORIAS` | solo las categorías en las que cambió una asistencia |
| Partido próximo cambiado, cancelado o reactivado | `CONVOCADOS` | jugadores activos con fila en `PartidosJugador` |
| Jugador convocado | `ALUDIDOS` | solo ese jugador |
| Resultado | `CATEGORIAS` | jugadores activos en las categorías del partido |

"Activo" es `Rol = JUGADOR`, `EstadoIngreso = APROBADO`, `Activo` y con categoría (RF-010).
Siempre se excluyen las filas de la cuenta que hizo la acción (RF-005); en la práctica solo cuenta
cuando publica una familia, porque el equipo del club no tiene filas de jugador.

**Razón**: son las mismas reglas de visibilidad de la 009 (`ReglaVisibilidadDePartido` y
`ReglaVisibilidadDeEntrenamiento`), aplicadas al generar. Un partido jugado ya lo ve todo el club
(009, RF-019), pero la spec limita su aviso a la categoría.

**Un partido cancelado y reactivado**: se avisa a los convocados, aunque el partido siga como
`PROXIMO`/`CANCELADO`; la convocatoria se conserva al cancelar (009, RF-021).

## 8. Una sola entrada sin leer por actividad (RF-025)

**Decisión**: al generar un aviso, para cada cuenta destinataria se borran sus filas de
`AvisosDeActividadDestinatario` **sin leer** que sean de la misma actividad, y después se borran los
avisos que quedaron sin ningún destinatario, todo en la misma transacción. Un aviso ya leído no se
toca: el nuevo se suma.

**Razón**: el aviso es compartido por varias familias y cada una lo lee por separado, así que no se
puede reescribir el texto: se reemplaza la fila de la cuenta. El contador no sube porque entra una
fila y sale otra. El borrado es del sistema, no de una acción de usuario (§14, enmienda 4.10.0).

**Consecuencia aceptada, a revisar en `/speckit-clarify` si molesta**: la sustitución pierde el
aviso viejo aunque cuente algo distinto. Si la familia no leyó "cambió la hora" y llega
"convocado", solo ve el último. Es lo que la spec pide (CE-005) y lo que evita la avalancha.

## 9. Convocatoria: un aviso por cuenta y por guardado

**Decisión**: al guardar una convocatoria se agrupan las altas por cuenta. Cada cuenta con altas
recibe **un** aviso que nombra a sus jugadores convocados ("Mateo y Sofía fueron convocados al
partido…"), con filas de destinatario solo para esos jugadores. Las bajas no generan aviso.

**Razón**: RF-015 pide nombrar al jugador para distinguir a los hermanos y el caso límite pide "un
aviso por cada familia afectada, y una sola vez". Con un aviso por jugador, la regla de §8 dejaría
solo el último hermano. Las filas de destinatario son solo de los aludidos para que una sesión
limitada al documento de otro hermano no vea el aviso.

**Sin aviso a quien sale**: el propietario descartó avisar a quien sale de una convocatoria
(§17.2: ningún niño debe enterarse por la plataforma de que ya no está convocado). Un aviso de
convocado que siguiera sin leer se conserva y, al tocarlo, dice que la actividad ya no está
disponible (§11).

## 10. Redacción de los textos

**Decisión**: una utilidad pura `RedaccionDeAvisos` (en `Aplicacion/Utilidades`) arma cada línea en
español. La fecha se dice "el sábado" si cae en los próximos 6 días contados desde hoy en hora de
Colombia, "mañana"/"hoy" si corresponde, y "el 18 de octubre" si es más lejana; la hora, "a las
9:00" (sin cero a la izquierda, 24 horas, igual que las pantallas). Los nombres de los días y de los
meses van en una tabla propia, no en `CultureInfo`, porque la imagen de Docker puede no traer las
culturas. El texto máximo es de 300 caracteres y se recorta el lugar o el rival si hace falta. El
marcador sigue el orden de `textoDeMarcador` de la pantalla (local: club primero; visitante: rival
primero) con el nombre del club copiado al generar.

Plantillas:

| Hecho | Texto |
| --- | --- |
| `ENTRENAMIENTO_PROGRAMADO` | `Nuevo entrenamiento el sábado a las 9:00` |
| `ENTRENAMIENTO_CAMBIADO` | `Cambió la hora del entrenamiento: ahora el sábado a las 10:00` (con "la fecha", "el lugar" o varias, y el valor nuevo) |
| `ENTRENAMIENTO_CANCELADO` | `Entrenamiento del sábado cancelado` |
| `ENTRENAMIENTO_REACTIVADO` | `El entrenamiento del sábado sigue en pie` |
| `ASISTENCIA_REGISTRADA` | `Se registró la asistencia del entrenamiento del sábado` |
| `PARTIDO_CAMBIADO` | `Cambió el lugar del partido contra Rival: ahora en Cancha Minitas` |
| `PARTIDO_CANCELADO` | `Partido contra Rival del sábado cancelado` |
| `PARTIDO_REACTIVADO` | `El partido contra Rival del sábado sigue en pie` |
| `JUGADOR_CONVOCADO` | `Mateo fue convocado al partido contra Rival el sábado a las 15:00` |
| `RESULTADO_PUBLICADO` | `Resultado del partido contra Rival: LFClub 3 - 1 Rival` |

Sin rival ("Por definir"): "el partido del sábado". El texto del aviso es una instantánea: si la
actividad cambia después, el aviso leído no se reescribe (RF-025).

## 11. La actividad ya no está disponible (RF-021)

**Decisión**: la lista y la lectura devuelven, por aviso, `actividad.disponible`, calculado en el
servidor con las mismas reglas de la 009 para la cuenta (con su limitación de sesión): un
entrenamiento está disponible si alguna de sus categorías es la categoría actual de un jugador
activo de la cuenta; un partido, si está jugado o si algún jugador activo de la cuenta tiene fila
en su convocatoria. La pantalla solo decide entre navegar y mostrar el mensaje.

**Razón**: la pantalla no puede adivinarlo sin pedir el detalle (que respondería 404 y rompería el
flujo), y no debe recibir datos de la actividad que no puede ver (RF-017). Una prueba cruzada
comprueba que `disponible` coincide con que el detalle real responda 200 o 404.

**Alternativas descartadas**: abrir el detalle y capturar el 404 en la pantalla (el aviso quedaría
sin marcar y la lista no podría distinguir antes de tocar).

## 12. Campana, lista y contador

**Decisión**: el contador, la lista y "marcar todos" son los de la 011 ampliados, con la misma
actualización (al entrar, al volver a primer plano, al cambiar de pantalla y cada 60 segundos). La
campana y la pantalla del JUGADOR pasan de "Comunicados" a **"Novedades"** (nombre accesible
"Novedades, 3 sin leer"); la ruta `/club/:clubId/comunicados` se conserva. Cada fila dice "Comunicado" o
"Aviso" y "Sin leer" o "Leído" en texto.

**Razón**: la familia no aprende nada nuevo (supuesto de la spec) y no se rompen enlaces ni pruebas
de rutas. Para el equipo del club la pantalla sigue siendo "Comunicados publicados": ellos no tienen
campana ni avisos.

## 13. Efectos laterales que NO hay

- Ningún aviso sale de la aplicación: ni correo, ni notificación del sistema, ni número sobre el
  icono (RF-027). No se añade ningún cliente de salida; una prueba de integración sustituye el
  servicio de correo y comprueba que no se invoca.
- El equipo del club no recibe filas de destinatario ni tiene campana (RF-028).
- Los avisos no se editan ni se retiran; el único borrado es el reemplazo de §8.
- Ningún texto se interpreta: se pinta como texto plano.

## 14. Pruebas

- **Unitarias**: `ReglaCambiosDeActividad`, `ReglaDestinatariosDeAviso`, `RedaccionDeAvisos`
  (todas las plantillas, hoy/mañana/día/fecha, sin rival) y el comparador de identificadores por
  bytes.
- **Integración** (carpeta `Avisos/`): entrenamiento (programar, cambiar, cancelar, reactivar,
  asistencia, hermanos en dos categorías), partido (convocar, sacar sin aviso, cambiar, cancelar, resultado de
  equipo y de familia), mezcla y contador, lectura por cuenta, reemplazo y no reemplazo de leídos,
  sesión limitada, aislamiento de club, nada fuera de la aplicación y `disponible` contra el 404
  real. Se migran las pruebas de la 011 que usaban las tres rutas movidas.
- **Frontend (Vitest)**: fila de aviso, mensaje de "ya no disponible", nombre accesible de la
  campana y contador "99+".

## 15. Supuestos de este plan

Son técnicos y ninguno altera una regla de la spec:

1. Un partido jugado se avisa a las familias de **todas sus categorías**, como pide la spec, aunque
   haya categorías sin convocados.
2. El aviso de asistencia llega solo a las categorías en las que cambió una asistencia (no a las demás
   categorías de un entrenamiento conjunto), para no decir "se registró" donde no se registró nada.
3. `Texto` admite 300 caracteres: el lugar y el rival admiten 120 cada uno.
4. Corregir solo los goles de un jugador no avisa (el marcador no cambió). Es la lectura estricta de
   RF-002 y RF-014.
5. El nombre visible de la campana pasa a "Novedades" (§12); si el propietario prefiere otro, es una
   constante de texto.

## 16. Bloque B: identidad de la ficha con un solo lápiz

Pedido del propietario durante el plan, ajeno a los avisos. Se trata como un bloque aparte para que
pueda entregarse por separado. Primero se propuso volver el documento de solo vista; el propietario
lo descartó: **el documento sigue editable como hasta ahora**.

**Decisión**:

- La tarjeta "Identidad" de la ficha deja los botones "Cambiar documento" y "Corregir nombre y
  fecha" y muestra **un lápiz** que abre un solo diálogo, "Editar identidad".
- El diálogo reúne los campos de los dos diálogos actuales: nombres, apellidos y fecha de nacimiento
  (los que hoy corrige quien tiene `puedeCorregirIdentidad`) y tipo y número de documento (los que
  hoy cambia quien tiene `puedeCambiar`). Cada grupo de campos se habilita según el permiso que ya
  existe; quien tiene uno solo ve el otro como texto de solo lectura.
- El lápiz se muestra si el usuario tiene alguno de los dos permisos.
- **El backend no cambia**: el diálogo sigue llamando a `PUT …/ficha/identidad` y a
  `PUT …/ficha/documento-identidad`, solo las que correspondan a lo que cambió. Se envía primero el
  documento, que es el que puede fallar por las reglas de §10 (duplicado, otra cuenta), para que un
  fallo no deje nada cambiado a medias; si falla, el diálogo muestra el error en el campo y no
  envía lo demás.
- Se elimina `DialogoCambiarDocumento.tsx` y `DialogoCorregirIdentidad.tsx` pasa a ser
  `DialogoEditarIdentidad.tsx`.

**Razón**: las dos acciones eran la misma tarjeta con dos puertas. Un solo punto de entrada elimina
la redundancia sin tocar reglas de §10, §8 ni §11.

**Sin enmienda**: no cambia ninguna regla de la constitución ni ningún contrato de API; es solo de
interfaz.

**Alternativas descartadas**: documento de solo vista para todos (descartada por el propietario:
habría exigido una enmienda 4.11.0 y dejaba sin arreglo un documento mal escrito); un endpoint
nuevo que guarde todo en una llamada (más backend para ahorrar una llamada que casi nunca son dos).
