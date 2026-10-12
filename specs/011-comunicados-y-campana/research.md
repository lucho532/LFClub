# Investigación técnica: comunicados y campana

**Funcionalidad**: `011-comunicados-y-campana` | **Fecha**: 2026-10-12

Cada decisión sigue el formato: decisión, razón y alternativas descartadas. Lo que no se menciona
queda como en las specs 001 a 010. Todo se apoya en el código ya construido (rutas, autorización por
integrante, filtro de club, reglas puras del dominio).

## 1. Tres tablas: comunicado, destinos y destinatarios

**Decisión**: tres tablas nuevas y una migración (`ComunicadosYCampana`):

- `Comunicados`: título, texto, quién lo publicó (cuenta de integrante, nombre y rol copiados),
  cuándo, alcance (`CLUB`, `CATEGORIAS`, `EQUIPOS`), clave de publicación y datos del retiro.
- `ComunicadosDestino`: una fila por categoría o equipo al que va dirigido (vacía si el alcance es
  `CLUB`). Guarda también los nombres copiados.
- `ComunicadosDestinatario`: una fila por comunicado y por jugador activo que lo recibe, con la
  cuenta dueña y si esa cuenta lo leyó.

**Razón**: la spec fija quién recibe al publicar (RF-009), así que la audiencia es una foto del
momento. Guardar a los destinatarios hace que "quien llega después no recibe lo anterior" y "quien
cambia de categoría conserva lo que ya recibió" salgan solos, sin recalcular la audiencia en cada
consulta. El volumen es el de §19: decenas de familias por club.

**Alternativas descartadas**:

- Calcular los destinatarios al leer, a partir de la audiencia: obliga a decidir qué pasa con quien
  cambia de categoría o entra después, y el contador cambiaría solo cuando alguien se mueve.
- Una sola fila por cuenta y comunicado (sin el jugador): no puede respetar la sesión limitada a un
  jugador (§12.4 y research §2).
- Una columna con la lista de identificadores de destinatarios: no se puede indexar ni limitar por
  club.

## 2. Destinatario: fila por jugador, leído por cuenta

**Decisión**: la fila de destinatario es del jugador (`UsuarioRolId`) y lleva la cuenta (`UsuarioId`)
y `LeidoEn`. El contador y la lista cuentan **comunicados distintos** de la cuenta. Abrir un
comunicado, o "marcar todos", actualiza `LeidoEn` en todas las filas de esa cuenta para ese
comunicado (RF-013, RF-022).

**Razón**: la spec dice que un comunicado llega una sola vez por cuenta aunque le llegue por dos
hermanos, y que lo leído es por cuenta. Con la fila por jugador se respeta además la limitación
de la sesión iniciada con el documento de un jugador (§12.4): esa sesión solo ve las filas de sus
integrantes, mientras que marcar leído sigue siendo de la cuenta.

**Alternativas descartadas**: fila por cuenta (no respeta la limitación); `LeidoEn` por jugador (un
comunicado leído desde un hermano seguiría "sin leer" desde el otro, contra RF-022).

## 3. Quién es destinatario

**Decisión**: al publicar se resuelve en una sola consulta del club el conjunto de integrantes con
rol `JUGADOR`, ingreso `APROBADO` y `RetiradoEn` nulo, así:

- `CLUB`: todos, tengan o no categoría (RF-007).
- `CATEGORIAS`: los de esas categorías.
- `EQUIPOS`: los que están en esos equipos (`JugadorEquipo`).

Se insertan las filas de destinatarios y la de destinos en la misma unidad de trabajo que el
comunicado.

**Lectura posterior**: una fila solo cuenta si su jugador sigue activo (aprobado y no retirado). Al
retirarlo no se borra nada: al reincorporarlo vuelve a ver lo que ya le había llegado (casos límite
de la spec). Un jugador en espera no tiene filas, porque no era activo al publicar.

**Razón**: reutiliza la noción de "jugador activo" de la 009 y no añade ningún borrado en el retiro.

## 4. Quién publica y a quién puede dirigirlo

**Decisión**: una regla pura nueva, `ReglaAudienciaDeComunicado`, decide con el rol:

| Rol | Alcance permitido |
| --- | --- |
| PRESIDENTE | Cualquier audiencia: todo el club, categorías o equipos del club |
| DIRECTIVO | La misma que el PRESIDENTE (§14.4) |
| ENTRENADOR | `CATEGORIAS` o `EQUIPOS` solo de categorías **activas con asignación activa** (`ReglaAlcanceDeCategorias.PuedeVer`); `CLUB` nunca |
| JUGADOR / DESARROLLADOR | Ninguna |

Un PRESIDENTE o DIRECTIVO asignado como entrenador usa la fila de su rol (RF-006). Un ENTRENADOR que
manda a un equipo puede dirigirse a **cualquier equipo activo de su categoría**, no solo a los que
dirige (§14.4: "equipos de ellas").

**Errores**: fuera de su alcance, `403 no_entrena_categoria` (código ya existente de la 010).
Categoría o equipo de otro club, inexistente o inactivo, `400 audiencia_invalida`. Equipos de
categorías distintas, `400 audiencia_invalida` (la spec dice "equipos de una categoría").

**Razón**: la autorización por rol ya está en `IntegranteDelClub`; la parte que depende de datos
(asignaciones) va en el servicio con una regla pura que se prueba sin base de datos.

## 5. Sin destinatarios

**Decisión**: si la audiencia resuelta no tiene ningún jugador activo, `400 sin_destinatarios` y no
se guarda nada (RF-008). Con varios equipos o categorías basta con que haya al menos uno.

## 6. Idempotencia de la publicación

**Decisión**: la petición lleva una `clave` (uuid) que genera la pantalla al abrir el formulario.
Hay un índice único `(ClubId, Clave)`. Una segunda petición con la misma clave y el mismo integrante
devuelve el comunicado ya creado con `200`; con otro integrante, `409 clave_en_uso`. Si dos llegan a
la vez, la restricción única hace perder a una, que relee y devuelve el existente
(`IndicesUnicos` ya traduce esa violación).

**Razón**: RF-010 pide que un doble toque no cree dos comunicados. Deshabilitar el botón no
alcanza: la segunda petición puede salir por reintento de red.

**Alternativas descartadas**: comparar título y texto en una ventana de tiempo (frágil y ambigua
con comunicados legítimamente repetidos); bloqueo por club como en la 010 (la clave es más barata y
no serializa a todo el club).

## 7. Retiro

**Decisión**: `POST …/comunicados/{id}/retiro`. Lo puede quien lo publicó (comparando
`PublicadoPorUsuarioRolId` con el integrante de la petición) y el PRESIDENTE. Retirado, el
comunicado deja de salir en la campana y el contador de todas las cuentas porque las consultas de
las familias filtran `RetiradoEn` nulo; sigue en la lista de lo publicado, marcado, con quién y
cuándo (RF-026). No hay "des-retirar": no existe el verbo contrario (RF-027).

**Errores**: ya retirado, `409 comunicado_retirado`. Un ENTRENADOR que no es el autor y pide
retirar uno que no ve, `404`; un DIRECTIVO que lo ve y no es el autor, `403 no_retira_comunicado`.

**Razón**: mismo patrón 404/403 de la 009 ("404 a quien no lo ve, 403 a quien lo ve y no lo
gestiona").

## 8. Qué ve el equipo del club

**Decisión**: `GET …/comunicados` (publicados) devuelve, según el rol:

- PRESIDENTE y DIRECTIVO: todos los del club, retirados incluidos, con autor y destinos.
- ENTRENADOR: solo los suyos (`PublicadoPorUsuarioRolId` igual a su integrante).

Si un comunicado queda sin `PublicadoPorUsuarioRolId` (el autor dejó el club o cambió de rol), el
nombre copiado se conserva y solo el PRESIDENTE lo retira.

**Razón**: es el supuesto de la spec (RF-024 y "Supuestos"). El ENTRENADOR no ve lo del PRESIDENTE
porque no tiene por qué.

## 9. Lista y paginación

**Decisión**: paginación por cursor con el identificador del comunicado (uuid v7, ordenable por
tiempo de creación): `limite` (1 a 50, por defecto 30) y `antesDe`. Respuesta `{ items, hayMas }`.
Aplica a recibidos y a publicados. El texto completo va en la lista (máximo 1.000 caracteres).

**Razón**: los comunicados no caducan (RF-018), así que la lista crece sin límite. El cursor evita
saltos cuando se publica algo mientras la persona pagina. El texto en la lista evita una consulta por
comunicado; el detalle por identificador existe para abrir un enlace directo.

**Alternativas descartadas**: sin paginar (crece sin tope); `pagina` y `tamaño` (se repiten filas si
se publica mientras se navega).

## 10. Contador y actualización sin avisos con la app cerrada

**Decisión**: `GET …/comunicados/recibidos/contador` devuelve `{ sinLeer }` exacto; la pantalla pinta
"99+" desde 100 (RF-013). La campana lo consulta:

- al montarse la zona del club (abrir la aplicación o entrar al club);
- al volver la pestaña o la aplicación a primer plano (`visibilitychange`, que Capacitor también
  dispara);
- al cambiar de pantalla;
- cada 60 segundos mientras esté visible.

Marcar leído, marcar todos y retirar actualizan el contador con la respuesta, sin otra consulta.

**Razón**: RF-017 pide ponerse al día sin recargar y RF-028 prohíbe avisos con la aplicación
cerrada, que son de otra spec (Firebase, número en el icono). Un sondeo corto es lo más simple (§19)
y no añade ninguna dependencia ni canal.

**Alternativas descartadas**: SSE o WebSocket (una conexión por familia y un servicio con estado,
para un contador); intervalos menores (más carga sin que se note).

## 11. La campana en la barra superior

**Decisión**: un componente `CampanaComunicados` en `compartido/componentes`, con la insignia del
contador, que `DisposicionClub` coloca junto al menú del perfil **solo cuando el rol es JUGADOR**
(RF-019). El componente no conoce la sesión: recibe el contador y el destino. Toca la campana,
navega a `/club/:clubId/comunicados`.

**Razón**: la 4.8.1 sacó el tema de la barra para no saturarla en el teléfono. Se añade un solo
icono de 44 px, sin texto, con nombre accesible "Comunicados: N sin leer" (RF-034). El botón de
recargar y la marca siguen.

## 12. Una pantalla de comunicados con dos caras

**Decisión**: una sola ruta, `/club/:clubId/comunicados`, que según el rol muestra:

- JUGADOR: la lista de recibidos, con leído y sin leer y "Marcar todos como leídos".
- PRESIDENTE, DIRECTIVO y ENTRENADOR: la lista de publicados, con "Nuevo comunicado" y "Retirar".

`/comunicados/nuevo` es el formulario del equipo; `/comunicados/:id` es el detalle (recibido o
publicado, según el rol). Entrada de menú "Comunicados" para los cuatro roles.

**Razón**: mismos datos, distinta vista; evita rutas por rol y sigue el patrón de partidos y
entrenamientos.

## 13. Texto plano

**Decisión**: el texto se guarda tal cual, recortado en los extremos, y se pinta como texto de React
con `white-space: pre-wrap` para conservar los saltos de línea. Nunca `dangerouslySetInnerHTML`
(RF-036). La API no interpreta nada. Los límites (100 y 1.000) se validan en el servidor y en el
formulario.

## 14. Efectos laterales que NO hay

Publicar y retirar no llaman a `IServicioCorreo` ni a ningún servicio externo (RF-028). No se crea
ninguna tabla de notificaciones genéricas: la campana de esta versión es solo de comunicados
(RF-029, §19). Cuando exista el aviso con la app cerrada, se construirá sobre `Comunicados`.

## 15. Borrado

**Decisión**: ninguna operación borra un comunicado (§14.4). Las claves foráneas son:

- `ClubId` en cascada: eliminar un club borra todo rastro (§7.4).
- Destinatarios en cascada con el comunicado y con el integrante: eliminar una cuenta o cambiar un
  rol (que borra la ficha) quita sus filas.
- `PublicadoPorUsuarioRolId` a nulo si el integrante se borra; el nombre y el rol copiados quedan
  (§13).
- Destinos: `CategoriaId` y `EquipoId` a nulo si se borra la categoría o el equipo (solo se
  borran si nunca tuvieron jugadores); los nombres copiados quedan.

## 16. Pruebas

- **Unitarias**: `ReglaAudienciaDeComunicado` (rol por alcance, entrenador con y sin asignación,
  categoría inactiva, equipos de dos categorías), `ReglaRetiroDeComunicado` (autor, PRESIDENTE,
  DIRECTIVO ajeno, ya retirado) y `ReglaVisibilidadDePublicados`.
- **Integración**: publicar por rol y alcance con cuentas de varias familias (CE-001, CE-002);
  contador con hermanos en dos categorías (CE-003); retiro (CE-004); aislamiento entre clubes y
  por identificador (CE-005); sin avisos externos con el servicio de correo sustituido por uno que
  falla si se llama (CE-006); doble envío con la misma clave; sesión limitada a un jugador;
  jugador en espera, retirado y reincorporado; cambio de categoría tras publicar.
- **Frontend (Vitest)**: insignia ("99+", sin contador, nombre accesible), formulario (límites,
  audiencia según rol, clave estable entre reintentos) y texto sin interpretación.

## 17. Supuestos de este plan

No hay decisiones de negocio nuevas que el propietario deba aprobar: todo lo de negocio está en la
spec y en la §14.4. Son decisiones técnicas que cambian sin tocar el modelo:

1. El sondeo del contador es cada 60 segundos.
2. Un ENTRENADOR dirige a cualquier equipo activo de sus categorías, no solo a los que dirige.
3. Un comunicado con alcance `EQUIPOS` admite equipos de una sola categoría.
4. La lista usa cursor por identificador y 30 por página.
