# Investigación: entrenamientos, partidos, convocatorias y estadísticas deportivas

**Funcionalidad**: `009-entrenamientos-y-partidos` | **Fecha**: 2026-10-11

Decisiones técnicas del plan. Cada una dice qué se eligió, por qué y qué se descartó. La spec no
dejó nada marcado "NEEDS CLARIFICATION"; los supuestos de diseño que podrían discutirse están al
final (§16) para que el propietario los confirme antes de `/speckit-tasks`.

## 1. Siete tablas nuevas, ninguna más

**Decisión**: `Entrenamientos`, `EntrenamientosCategoria`, `AsistenciasEntrenamiento`,
`RespuestasEntrenamiento`, `Partidos`, `PartidosCategoria` y `PartidosJugador`. Todas
implementan `IPerteneceAClub` y quedan bajo el filtro global de club de la 001. Detalle en
[data-model.md](data-model.md).

**Motivo** (§19, una necesidad por tabla):

- Entrenamiento y partido son actividades distintas: el partido tiene rival, condición, tipo,
  torneo, marcador y estado "jugado", y su visibilidad es otra (§17.2).
- Las dos tablas `...Categoria` existen porque una actividad va a una o varias categorías (§11.1).
- `PartidosJugador` es la fila que la constitución ya nombra (§13, `PartidoJugador.CategoriaId`):
  el convocado, con la categoría en que se le convocó, si jugó, sus goles y la respuesta de su
  familia.
- `AsistenciasEntrenamiento` y `RespuestasEntrenamiento` van separadas porque la spec exige que
  la respuesta de la familia y la asistencia real sean datos distintos (RF-025) y porque nacen en
  momentos distintos y las escriben personas distintas.

**Alternativas descartadas**:

- Una tabla `Actividad` con un tipo: mezcla columnas que solo tiene el partido y obliga a
  ramificar cada consulta de visibilidad.
- Guardar la respuesta de la familia al entrenamiento en la fila de asistencia con dos columnas
  nulas: cumple RF-025 a medias y complica "quién no ha respondido" frente a "quién no tiene
  asistencia".
- Una tabla aparte para la respuesta al partido: el convocado ya tiene su fila en `PartidosJugador`,
  y quitarlo de la convocatoria debe borrar también su respuesta (RF-013, "sin rastro").

## 2. Fechas y horas en hora de Colombia, sin zona horaria en la base

**Decisión**: cada actividad guarda `Fecha` (`date`) y `HoraInicio` (`time`), y el entrenamiento
también `HoraFin`, tal como las escribe quien la programa, en hora de Colombia. Una utilidad de
aplicación, `HoraDeColombia`, convierte `IReloj.AhoraUtc` a fecha y hora de Colombia con el
desfase fijo de UTC−5, y las reglas comparan siempre en esa hora.

**Motivo**: la spec fija la hora de Colombia para toda la plataforma (Supuestos). Colombia no tiene
horario de verano desde 1993, así que el desfase fijo es exacto y no depende de que la imagen de
Docker traiga la base de zonas horarias. Guardar fecha y hora "de pared" evita que un entrenamiento
de las 4:00 p. m. aparezca a otra hora por un error de conversión, y PostgreSQL ordena y filtra
`date` y `time` sin trucos.

**Alternativas descartadas**: `timestamptz` en UTC (cada lectura y cada formulario tendrían que
convertir, y una hora de fin "del mismo día" pide dos instantes); `TimeZoneInfo` con
`America/Bogota` (depende de `tzdata` en el contenedor para un resultado que ya se conoce).

**Momentos que usan las reglas** (todos en hora de Colombia):

| Momento | Se usa para |
| --- | --- |
| La actividad **ya empezó**: `Fecha + HoraInicio <= ahora` | La familia ya no responde (RF-023). El resultado del partido ya se puede registrar (RF-015) |
| El entrenamiento **ya terminó**: `Fecha + HoraFin <= ahora` | Sale de "Próximos entrenamientos" (RF-006) |
| **Su fecha ya llegó**: `Fecha <= hoy` | Se puede registrar asistencia (RF-022, escenario 4.1 "cuya fecha ya llegó") |

## 3. Quién entrena: la asignación activa, para los tres roles del club

**Decisión**: "quien entrena una categoría" es cualquier integrante con una
`AsignacionEntrenadorCategoria` activa en ella, sea ENTRENADOR, DIRECTIVO o PRESIDENTE (constitución
4.6.0, §8 "Entrenador" y §17.2). Una sola consulta por petición trae las categorías que entrena el
integrante (`IRepositorioAsignaciones.CategoriasDeAsync`, que ya existe) y dos reglas puras de
dominio deciden con ese conjunto:

- `ReglaGestionDeActividad.PuedeGestionar(rol, categoriasDeLaActividad, categoriasQueEntrena)`:
  el PRESIDENTE siempre; cualquier otro integrante solo si entrena **todas** las categorías de la
  actividad (RF-002, RF-008, RF-016); el JUGADOR nunca.
- `ReglaAlcanceDeAsistencia.CategoriasMarcables(rol, categoriasDelEntrenamiento, categoriasQueEntrena)`:
  el PRESIDENTE marca todas; quien entrena, solo la intersección (RF-022, escenario 4.2).

**Motivo**: la asignación se lee en cada petición, igual que en la ficha de la 005: quien pierde la
asignación deja de gestionar y de ver los partidos próximos al instante (caso límite de la spec).
Las reglas no tocan la base y se prueban con pruebas unitarias.

**Alternativa descartada**: ampliar `ReglaAlcanceDeCategorias` de la 003. Esa regla decide qué
categorías se **listan** por rol y dice expresamente que la asignación no da más alcance a un
PRESIDENTE o a un DIRECTIVO; mezclarla con lo deportivo cambiaría lo que ya hace la pantalla de
categorías.

## 4. Visibilidad: dos reglas puras y un único 404

**Decisión**: `ReglaVisibilidadDeEntrenamiento` y `ReglaVisibilidadDePartido`, en
`LaPecosa.Dominio/Reglas`, reciben ya resueltos el rol, el estado y los hechos de la persona
(si entrena alguna de sus categorías, si es su categoría, si está convocado) y devuelven si la ve.

| Quién | Entrenamiento (RF-003) | Partido próximo o cancelado (RF-010, RF-014) | Partido jugado (RF-019) |
| --- | --- | --- | --- |
| PRESIDENTE | Todos | Todos | Todos |
| DIRECTIVO | Todos | Solo si entrena alguna de sus categorías | Todos |
| ENTRENADOR | Si entrena alguna de sus categorías | Si entrena alguna de sus categorías | Todos |
| JUGADOR | Si su categoría actual está entre las del entrenamiento, o si tiene asistencia registrada en él | Solo si está convocado | Todos |

Cada consulta de listas aplica la misma condición **en SQL** (un `Where` que es la traducción de la
regla) para no traer filas que no se pueden ver ni contarlas; la consulta por identificador lee la
fila y aplica la regla en memoria. Si no la ve, `404 no_encontrado`, el mismo de un identificador
que no existe o que es de otro club (RF-011, escenario 1.8). Una prueba de integración compara,
para cada rol, la lista y la consulta por identificador, para que las dos traducciones no se
separen.

**Motivo**: la regla de §17.2 es la que más protege a los niños; tenerla en una sola función pura,
probada fila por fila de la tabla, la hace legible. La consulta en SQL evita que un contador o un
listado deje pasar un partido (CE-001).

**Alternativa descartada**: un filtro global de EF por persona como el de club. El filtro de club
sabe el club de la petición; el de partidos tendría que saber el rol, las asignaciones y el jugador
elegido, y se aplicaría también a los procesos que necesitan ver todo (el retiro, la estadística).

**Jugador elegido**: con varios hermanos en la cuenta, "convocado" y "su categoría" se evalúan con
el integrante de la petición que ya resuelve `IntegranteDelClubAttribute` (cabecera
`X-Jugador-Elegido` de la 006), nunca con la cuenta. Así cada hermano ve solo lo suyo (caso límite
de la spec).

## 5. Qué ve cada uno dentro de un partido o un entrenamiento

**Decisión**: el detalle es un DTO único con partes opcionales que se omiten del JSON cuando quien
pregunta no puede verlas (como `FichaJugadorDto` de la 005, `JsonIgnore` cuando es nulo):

- **Una cuenta de jugador** recibe los datos del partido o del entrenamiento, su propia respuesta,
  su propia asistencia y, si el partido está jugado, el marcador y la lista de goleadores (nombre y
  goles). Nunca la lista de convocados, de quiénes jugaron, ni respuestas o asistencias de otros
  (RF-012, RF-024, CE-002). No hay ningún campo vacío que lo insinúe.
- **Quien entrena alguna de sus categorías y el PRESIDENTE** piden además la planilla
  (`GET .../jugadores`), con cada jugador, su respuesta y su asistencia o si jugó y sus goles.
- **El DIRECTIVO que no entrena ninguna** ve el entrenamiento y el partido jugado como cualquier
  integrante, sin planilla: la spec le da consulta de la actividad, no de la convocatoria.

**Motivo**: el aislamiento queda en el servidor (§15, RF-032) y en una sola respuesta por pantalla.

## 6. Convocatoria y planillas: listas completas que se reemplazan

**Decisión**: la convocatoria se guarda con `PUT /partidos/{id}/convocatoria` y la lista completa
de identificadores; el servidor calcula altas y bajas en una transacción. Igual el resultado
(`PUT .../resultado` con el marcador y cada convocado con `jugo` y `goles`). La asistencia se
guarda con `PUT .../asistencia` y solo las filas enviadas (alta o corrección), porque quien entrena
una sola categoría de un entrenamiento conjunto no ve ni toca las demás (escenario 4.2).

**Motivo**: es lo que hace la pantalla (marcar casillas y guardar), es idempotente y evita una
operación por casilla.

**Reglas que valida el servidor**:

- Convocatoria: solo partidos próximos; cada jugador debe ser un jugador activo del club cuya
  categoría actual sea una de las del partido (RF-009). Al convocarlo se copia esa categoría en
  `PartidosJugador.CategoriaId` (RF-018). Quien ya estaba convocado conserva su fila, su categoría
  copiada y su respuesta aunque haya cambiado de categoría después (RF-034; caso límite de la
  spec).
- Resultado: partido próximo o jugado, no cancelado, que ya empezó; goles de 0 a 99; cada fila debe
  ser de un convocado; goles solo para quien jugó; la suma no supera los goles del club (RF-017).
  Lo valida `ReglaResultadoDePartido`, pura.
- Asistencia: entrenamiento programado cuya fecha ya llegó; cada jugador debe estar en una
  categoría marcable por quien registra (§3) o tener ya una asistencia allí. La categoría se copia
  en la primera vez y no cambia al corregir (§13).

## 7. Estados y transiciones

**Decisión**: `EstadoEntrenamiento { PROGRAMADO, CANCELADO }` y
`EstadoPartido { PROXIMO, CANCELADO, JUGADO }`, solo el estado actual (§18). `ReglaTransicionPartido`
admite PROXIMO → CANCELADO, CANCELADO → PROXIMO (reactivar), PROXIMO → JUGADO (al registrar el
resultado) y JUGADO → JUGADO (al corregirlo); un JUGADO no se cancela ni vuelve a PROXIMO (RF-021).
El entrenamiento va de PROGRAMADO a CANCELADO y de vuelta.

**Reactivar** (RF-038, aclaración del propietario del 2026-10-11): quien gestiona la actividad la
reactiva mientras no haya empezado (`Fecha + HoraInicio > ahora`) y todas sus categorías sigan
activas. Se expresa como quitar la cancelación: `DELETE .../cancelacion`, el par de
`POST .../cancelacion`. No se toca ninguna otra fila: categorías, convocatoria y respuestas de las
familias quedan como estaban, porque cancelar tampoco las borró. Los convocados que se retiraron
mientras estaba cancelado ya salieron por el retiro (§10), así que no reaparecen. Rechazos: 409
`actividad_no_cancelada`, `actividad_empezada` o `categoria_inactiva`.

Un partido cuya hora pasó sin resultado sigue PROXIMO y la API lo marca con
`faltaResultado: true` para quienes lo ven (Supuestos de la spec). No hay ningún proceso en segundo
plano.

**Motivo**: §17 prohíbe cambiar un registro manual sin una regla; cada cambio de estado ocurre solo
por una acción de quien gestiona.

**Alternativas descartadas**: un `PUT` del estado (abre transiciones que no existen); borrar las
respuestas al cancelar (perdería lo que la familia ya dijo si se reactiva).

## 8. El último cambio del resultado

**Decisión**: `Partido` guarda `ResultadoCambiadoEn`, `ResultadoCambiadoPorUsuarioId` (foránea a
`Usuario`, a nulo si la cuenta se elimina) y `ResultadoCambiadoPorNombre` (copiado en ese momento,
§13). El detalle lo devuelve como el `UltimoCambioDto` de la 005/008, con el enlace a la ficha
básica que calcula `ResolutorDeEnlaces` (RF-020, RF-037).

**Alternativa descartada**: un historial de resultados (§18 lo prohíbe salvo para pagos).

## 9. Estadísticas calculadas al consultar

**Decisión**: no hay tabla de estadísticas. `IRepositorioEstadisticas` agrupa en SQL, por
`CategoriaId` copiada:

- Partidos jugados: filas de `PartidosJugador` con `Jugo` en partidos JUGADO.
- Goles: suma de `Goles` en partidos JUGADO.
- Asistencia: filas de `AsistenciasEntrenamiento` en entrenamientos no cancelados, y cuántas con
  `Presente`. El porcentaje lo calcula `CalculoDeAsistencia` (puro): presentes ÷ registradas,
  redondeado al entero; 0 % con 0 registradas, sin dividir por cero (RF-030, escenario 6.2).

El total es la suma de las filas por categoría. Al corregir un resultado o una asistencia la
siguiente consulta ya lo refleja (RF-026, RF-027, CE-003).

**Goleadores**: `GET /goleadores?categoriaId=` agrupa los goles de partidos JUGADO por
`PartidosJugador.CategoriaId` y jugador, de mayor a menor y por apellido a igualdad; incluye a los
retirados (escenario 6.5) y devuelve solo nombre, apellidos, goles y el enlace de la 008 (que para
una cuenta de jugador es siempre nulo: no abre fichas ajenas). Sin categoría elegida devuelve la
lista de categorías con goles para el selector.

**Ficha**: `GET /jugadores/{id}/estadisticas` pasa por `AccesoAFicha` de la 005: quien no ve la
ficha recibe el mismo 404 (RF-028, escenario 6.6).

**Motivo**: decenas de jugadores y de partidos por club (§19); un `GROUP BY` sobre índices por
`UsuarioRolId` es inmediato y no puede desincronizarse.

## 10. El retiro saca de las convocatorias pendientes

**Decisión**: en la misma transacción que cada retiro, se borran las filas de `PartidosJugador` del
jugador en partidos que no están JUGADO, y sus `RespuestasEntrenamiento` de entrenamientos que no
han empezado (RF-033):

- Retiro por el PRESIDENTE o por la familia: `ServicioRetiroJugador.RetirarAsync`, junto a sacarlo
  de sus equipos.
- Retiro al aprobarse un hermano en otro club: `RepositorioRetiroEntreClubes.RetirarDeOtrasCuentasAsync`,
  con el mismo `IgnoreQueryFilters` acotado por documento que ya usa para equipos y foto. Es parte
  del mismo retiro que §7.1 autoriza, no una escritura nueva entre clubes.

Las asistencias pasadas y las filas de partidos jugados no se tocan. Las listas de asistencia
futuras ya no lo traen porque solo listan jugadores activos.

**Motivo**: es la "regla explícita que lo autoriza" que pide §17, y se hace donde ya se hacen los
demás efectos del retiro.

**Alternativa descartada**: filtrar a los retirados al leer. Al reincorporarlo volvería a aparecer
convocado sin que nadie lo decidiera.

## 11. Categoría desactivada y asignación perdida

**Decisión**: crear o modificar una actividad exige categorías activas del club (RF-005); una
categoría que se desactiva después queda en `...Categoria` y en el historial (caso límite).
Desactivar una categoría ya retira las asignaciones (§14), así que quien la entrenaba deja de verla
sin código nuevo. Las filas usan `OnDelete(Restrict)` hacia `Categorias`: borrar una categoría solo
es posible si nunca se usó (§14), y una categoría con actividades no se puede borrar. Para eso
`Categoria.Usada` pasa a verdadero también al dirigirle una actividad.

## 12. Calendario y "Próximos entrenamientos"

**Decisión**:

- `GET /calendario?desde=&hasta=` devuelve, para un rango de hasta 62 días, los entrenamientos y
  los partidos que quien pregunta puede ver (§4), ordenados por fecha y hora. La pantalla navega por
  semanas, como el lienzo (`M-Calendario`), y en escritorio muestra la semana en columnas.
- `GET /entrenamientos/proximos` devuelve hasta 5 entrenamientos visibles que no han terminado,
  cancelados incluidos y marcados (RF-004, RF-006). Lo usa la tarjeta del inicio de todos los roles,
  que deja de ser un espacio reservado.

**Alternativa descartada**: que el inicio pida el calendario y filtre en el navegador; traería
partidos y un rango arbitrario para una tarjeta de cinco filas.

## 13. Pantallas, siguiendo el lienzo

El lienzo de §24 trae `Convocatoria` (escritorio del entrenador: tipo Partido/Entrenamiento,
categorías como botones, rival, fecha, hora, lugar, lista de jugadores con casillas, "Seleccionar
todos" y "Ninguno", y el aviso "Solo los jugadores que selecciones verán este partido") y
`M-Calendario` (teléfono de la familia: tira de la semana, tarjetas con etiqueta "Entreno" o
"Partido" y dos botones de respuesta). Se toman su estructura y sus textos, con estos ajustes:

- Los botones de respuesta dicen "Asistirá" y "No asistirá", como la spec, y el estado elegido se
  lee también como texto (RF-036).
- Se quitan del formulario el costo del arbitraje, el mensaje para los convocados y "Guardar
  borrador": arbitraje, mensajes de convocatoria y borradores están fuera de alcance.
- Entrenamiento y partido se crean desde el mismo formulario con el selector de tipo, pero cada uno
  manda su DTO a su endpoint.
- Las categorías que se ofrecen son las que la persona gestiona: el PRESIDENTE, todas las activas;
  los demás, aquellas en las que figuran en la lista de entrenadores que ya trae
  `GET /categorias`. Es solo comodidad: el servidor lo vuelve a comprobar.

## 14. Tamaño de archivos (§2.3)

- `DisposicionClub.tsx` (216 líneas) no recibe las tres entradas nuevas del menú: el menú pasa a
  `MenuDelClub.tsx`, y la disposición baja.
- `ContextoLaPecosa.cs` (99) gana siete `DbSet`; queda en unas 125.
- Los tipos nuevos del frontend van en `tiposActividades.ts` y `tiposEstadisticas.ts`; `tipos.ts`
  no crece.
- Los servicios se dividen por responsabilidad desde el principio (consulta, gestión, convocatoria,
  resultado, asistencia, respuestas, estadísticas) para no acercarse a 250.

## 15. Pruebas

- **Unitarias** (`Unitarias/Reglas/`): las cuatro reglas de §3 y §4 fila por fila, las transiciones,
  `ReglaResultadoDePartido`, `CalculoDeAsistencia` y `HoraDeColombia` (medianoche, fin de día).
- **Integración** (`Integracion/Entrenamientos/`, `Partidos/`, `Estadisticas/`): un
  `SembradorActividades` con el escenario de la spec (2014 con diez jugadores, 2015, 2016, un
  ENTRENADOR de la 2014, otro de 2014 y 2015, un DIRECTIVO asignado a la 2014, otro sin asignar,
  el PRESIDENTE y un segundo club). Para cada una de las diez cuentas de la 2014 se comprueba
  calendario, inicio, listas y consulta por identificador (CE-001). El reloj de pruebas
  (`IReloj` sustituido) mueve el "ahora" para probar antes y después de empezar.
- **Aislamiento**: `AccesoClubPruebas` sube a 70 endpoints de club y el contrato a 94; los nuevos
  responden 404 al integrante de otro club y al DESARROLLADOR (CE-005).
- **Frontend** (Vitest): el formato de fecha y hora en español, la etiqueta de estado con texto y la
  navegación de semanas.

## 16. Supuestos de diseño para confirmar

La spec no los decide. Se eligió la opción más simple y reversible; si el propietario prefiere otra,
cambia una regla, no el modelo.

1. **Las categorías de una actividad no se cambian después de crearla.** Se corrige fecha, horas,
   lugar y nota (y en el partido rival, condición y tipo); para cambiar categorías se cancela y se
   crea otra. Así una convocatoria o una asistencia nunca queda con jugadores de una categoría que
   la actividad ya no tiene.
2. **Los datos de un partido jugado no se cambian**; solo se corrige su resultado (marcador,
   quiénes jugaron, goles).
3. ~~Cancelar es definitivo~~. **Resuelto por el propietario el 2026-10-11**: una actividad
   cancelada se puede reactivar mientras no haya empezado (RF-038, §7).
4. **Se puede programar una actividad con fecha pasada**, por ejemplo para registrar la asistencia
   de un entrenamiento que no se cargó a tiempo. La spec no lo prohíbe.
5. **La asistencia se puede registrar desde el día del entrenamiento**, no desde su hora de
   inicio ("cuya fecha ya llegó", escenario 4.1).
