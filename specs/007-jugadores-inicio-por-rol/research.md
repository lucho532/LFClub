# Investigación técnica: Jugadores, inicio por rol e identidad del club por el presidente

**Funcionalidad**: `007-jugadores-inicio-por-rol` | **Fecha**: 2026-10-10

La spec no deja ningún "NEEDS CLARIFICATION". Este documento recoge las decisiones técnicas del
plan, cada una con su motivo y las alternativas descartadas, y al final los supuestos que la spec
no fija y conviene confirmar.

## 1. Dónde se guarda la foto del jugador

- **Decisión**: una tabla nueva, `FotosJugador`, con una fila por jugador: `UsuarioRolId` (clave
  primaria y foránea), `ClubId`, `Contenido` (bytes), `TipoContenido` y `GuardadaEn`. Es la misma
  forma que ya tienen `EscudosClub` y `FotosPerfil`.
- **Motivo**: separar los bytes evita cargarlos cada vez que se lee al jugador o su ficha (es la
  razón por la que la 001 separó el escudo y la foto de perfil). Al llevar `ClubId`, la tabla
  queda bajo el filtro de aislamiento del contexto (`IPerteneceAClub`) y se borra en cascada con
  el club (§7.4) y con el integrante (rechazo de un hermano en espera).
- **Por qué no en `FichaJugador`**: esa fila nace con el primer cambio y se lee en cada consulta
  de la ficha; meter ahí hasta 1 MB obligaría a leerlo siempre o a proyectar a mano en todas las
  lecturas.
- **Por qué no en disco o en un almacén de objetos**: no existe ninguno en el proyecto, añadiría un
  volumen y una tecnología (§6, §19), y el escudo, la foto de perfil y los documentos de la ficha ya
  viven en PostgreSQL.
- **Alternativas**: columna en `UsuarioRol` (la carga en cada autorización); tabla genérica de
  imágenes compartida con el escudo y el perfil (una abstracción sin necesidad, §19).

## 2. Cómo sabe la pantalla que hay foto y cuándo cambió

- **Decisión**: la lista de jugadores y la ficha llevan `fotoActualizadaEn` (fecha y hora UTC de la
  última carga, o `null` sin foto). La pantalla muestra las iniciales con `null` y, si no, pide la
  imagen a `GET …/jugadores/{id}/foto` con la sesión y la muestra desde memoria, como ya hace la
  foto de perfil (`useFotoPerfil`).
- **Motivo**: una etiqueta `<img>` no envía la cabecera de sesión, y la foto de un menor no puede
  servirse por una dirección pública (§14.2: "nunca aparece en el sitio público"). La fecha sirve
  además de llave para no volver a pedir la misma foto mientras no cambie.
- **Caché**: la respuesta lleva `Cache-Control: private, no-store`, igual que la ficha y la foto de
  perfil. Lo que se reutiliza es la copia en memoria de la pestaña, guardada por
  `usuarioRolId + fotoActualizadaEn` y vaciada al cerrar sesión.
- **Alternativas**: un contador de versión en `UsuarioRol` como `Usuario.VersionFoto` (al quitar y
  volver a cargar, el contador debe seguir subiendo y no volver a 0; la fecha lo resuelve sin
  columna nueva en una tabla que ya se lee en cada petición); la imagen en base64 dentro de la lista
  (decenas de MB en una sola respuesta JSON).

## 3. Quién ve y quién cambia la foto

- **Decisión**: las tres operaciones de la foto pasan por `AccesoAFicha` y `ReglaAccesoAFicha`, sin
  regla nueva:
  - **Ver** (`GET`): quien tiene `Ve` sobre la ficha. Es exactamente RF-010: la propia cuenta (el
    jugador de la petición, no un hermano), el PRESIDENTE, los DIRECTIVOS y el ENTRENADOR de una
    categoría activa que entrena, solo si el jugador está activo.
  - **Cargar y quitar** (`PUT`, `DELETE`): quien tiene `Cambia`, que ya es solo el PRESIDENTE y la
    cuenta del jugador de la petición (RF-009).
  - El autorizador deja pasar a todos los roles en `GET` y solo a PRESIDENTE y JUGADOR en `PUT` y
    `DELETE` (`403 rol_no_autorizado` para DIRECTIVO y ENTRENADOR, como en el resto de la ficha).
- **Respuestas**: sin alcance, o sin foto, `404 no_encontrado`, sin revelar cuál (RF-008). El
  DESARROLLADOR no es integrante de ningún club y recibe el `404` de siempre en `/api/clubes/**`.
- **Validación**: `ValidadorImagen` de la 001 (PNG, JPEG o WebP por su firma, hasta 1 MB), con sus
  propios códigos: `400 foto_no_es_imagen` y `400 foto_demasiado_grande`, los mismos textos que la
  foto de perfil. Si se rechaza, no se toca la foto anterior.
- **Alternativas**: un permiso propio en `AlcanceDeFicha` para la foto (daría lo mismo que `Ve` y
  `Cambia`; un indicador más que mantener alineado); dejar ver la foto a quien ve la lista pero no
  la ficha (no existe ese caso: la lista se limita igual que la ficha).

## 4. La foto de un jugador retirado

- **Decisión**: cargar una foto a un jugador retirado se niega con `409 jugador_retirado`
  **(supuesto 2)**. Ver o quitar responden como siempre: el retirado no tiene foto, así que
  `GET` da `404` y `DELETE` no hace nada y devuelve la ficha.
- **Motivo**: el PRESIDENTE ve la ficha de los retirados (spec 005), así que `Cambia` es verdadero
  para él, pero §14.2 dice que la foto de un menor no se guarda cuando ya no está en el club.
- **Alternativa**: dejarla cargar y borrarla al reincorporar (guarda justo lo que §14.2 prohíbe).

## 5. Cargar o quitar la foto es un cambio de la ficha

- **Decisión**: `ServicioFotoJugador` sigue el patrón de la subida de documentos de la 005: resuelve
  el acceso, valida fuera de la transacción y, dentro, bloquea el club, guarda o borra la fila de
  `FotosJugador` y llama a `IRepositorioFichas.SellarCambioAsync` (fecha y autor del último cambio,
  RF-009). Devuelve la ficha actualizada.
- **Concurrencia**: el bloqueo del club ordena dos cargas simultáneas; queda la última (casos
  límite de la spec). No hay comprobación de versión.
- **Por qué devolver la ficha**: la pantalla ya repinta la ficha con lo que devuelven sus otras
  operaciones; así recibe a la vez la nueva `fotoActualizadaEn` y el nuevo último cambio.

## 6. La foto se borra cuando el jugador deja el club

§14.2 enumera los casos. El plan los resuelve así:

| Caso | Dónde se borra | Estado |
| --- | --- | --- |
| Lo retira el PRESIDENTE | `ServicioRetiroJugador.RetirarAsync`, en la misma transacción que el retiro | Se construye en esta funcionalidad |
| Lo retira su familia | El mismo servicio: la 006 (research §18, T033) reutiliza esa operación | Queda cubierto con el anterior |
| Queda retirado al aprobarse un hermano en otro club | `IRepositorioRetiroEntreClubes.RetirarDeOtrasCuentasAsync` de la 006 (research §13), en la misma transacción que el retiro | Se construye en esta funcionalidad: ver §14 |
| Su ficha se elimina por cambio de rol (§12.2) | La funcionalidad que cambie el rol de JUGADOR a otro | No existe todavía ninguna operación que lo haga; se documenta en la entidad |
| Se rechaza al hermano en espera | Cascada al borrar el integrante | No puede tener foto: la autorización no le deja llegar a la ficha |
| Se elimina el club | Cascada desde `Clubes` | Automático |

- **Reincorporar** no recupera nada: la fila ya no existe y vuelve sin foto (RF-009).
- **Alternativas**: un desencadenador en la base de datos al poner `Activo = false` (regla de
  negocio escondida en la base, fuera de §5); borrar en `RepositorioJugadores.RetirarAsync` (mezcla
  dos tablas en una sentencia de repositorio y no cubre el retiro entre clubes).

## 7. La lista de jugadores

- **Decisión**: `GET /api/clubes/{clubId}/jugadores`, para PRESIDENTE, DIRECTIVO y ENTRENADOR
  (`403 rol_no_autorizado` para JUGADOR). Devuelve los jugadores ya agrupados por categoría:
  `GrupoDeJugadoresDto[]`, cada grupo con su categoría (o ninguna, para "Sin categoría") y sus
  jugadores.
- **Quién entra** (RF-002, RF-003): solo jugadores del club (`EsJugadorDelClub`: rol JUGADOR,
  aprobado y activo). Para un ENTRENADOR, solo los de las categorías activas en las que tiene una
  asignación activa, que se consultan en cada petición con `IRepositorioAsignaciones.CategoriasDeAsync`
  y se filtran con `ReglaAlcanceDeCategorias.PuedeVer`, la regla del apartado "Categorías". El
  grupo "Sin categoría" solo lo reciben los roles con `VeListasDelClub` (RF-004).
- **Orden**: grupos por año de la categoría, de menor a mayor (la 2012 antes que la 2020), y "Sin
  categoría" al final; dentro de cada grupo, `OrdenDePersonas.PorApellidos` (apellidos y luego
  nombres, como las demás listas). Los equipos de cada jugador, solo los activos, ordenados por
  nombre (RF-005).
- **Lo que lleva cada jugador**: identificador, nombres, apellidos, equipos y
  `fotoActualizadaEn`. **No** lleva documento, contacto, salud ni estado de documentación: esos
  datos están en la ficha, con su propia regla.
- **"Al día"**: la API no devuelve ningún campo para ella. La columna es un texto fijo de la
  pantalla (§11 de este documento). Así ningún valor de la respuesta puede leerse como estado de
  pago (RF-006, CE-007).
- **Una sola consulta de jugadores** (`IRepositorioJugadores.ListarDelClubAsync`, con categoría y
  equipos) y otra de las fechas de foto de esos jugadores (`IRepositorioFotosJugador.FechasAsync`),
  como hace hoy la documentación en las listas con `LectorDeCategorias.EntregadosAsync`.
- **Alternativas**: una lista plana que agrupe la pantalla (el orden y el grupo "Sin categoría"
  serían reglas del frontend, §15); paginar o buscar (fuera de alcance; decenas de jugadores, §19).

## 8. El detalle del jugador

- **Decisión**: el detalle es la ficha que ya existe (`/club/:clubId/jugadores/:usuarioRolId/ficha`
  y `GET …/ficha`), con una sección nueva arriba: la foto en grande (o las iniciales) y, solo si
  `permisos.puedeCambiar` y el jugador no está retirado, "Cargar foto", "Cambiar foto" y "Quitar
  foto". La categoría y los equipos ya vienen en `FichaJugadorDto`.
- **Motivo**: RF-007 pide no ampliar ni reducir lo que la 005 deja ver a cada rol; reutilizar la
  ficha garantiza exactamente eso, y la familia gana la foto en "Mi ficha" (escenario 7 de la
  historia 2) sin otra pantalla.
- **Volver** (escenario 6): la lista abre la ficha con `state: { desde: 'jugadores' }`; con esa
  marca el enlace de volver dice "← Jugadores". Sin ella se conserva el comportamiento de la 005
  (categoría del jugador o inicio).
- **Alternativa**: una pantalla de detalle nueva con su propio endpoint (duplicaría la regla de
  alcance de la ficha y su mapper).

## 9. El resumen del club

- **Decisión**: `GET /api/clubes/{clubId}/resumen`, solo PRESIDENTE y DIRECTIVO. Devuelve
  `ResumenClubDto` con cuatro números, calculados en cada consulta y sin guardar nada:
  - **jugadores**: jugadores del club activos (`EsJugadorDelClub`), con o sin categoría.
  - **equipos**: equipos activos de categorías activas **(supuesto 1)**.
  - **entrenadores** (RF-011): personas distintas con una asignación activa en una categoría
    activa, sea cual sea su rol, más los integrantes aprobados y activos con rol ENTRENADOR sin
    asignación. Se calcula como la unión de los dos conjuntos de `UsuarioRolId`.
  - **categorías**: categorías activas.
- **Dónde**: un repositorio propio, `IRepositorioResumenClub`, con una sola operación que hace las
  cuatro cuentas en el club de la petición (filtro de aislamiento activo). No hay regla de dominio:
  son cuentas, sin decisiones.
- **RF-015**: el resumen se elige por el rol, no por las asignaciones; un PRESIDENTE o DIRECTIVO que
  entrena lo ve igual.
- **Alternativas**: calcularlo en la pantalla con las listas que ya existen (cuatro peticiones y
  reglas de conteo en el frontend); reutilizar `GET /categorias` para contar (no sabe de
  entrenadores sin asignación ni de jugadores sin categoría).

## 10. El inicio del ENTRENADOR y el de la familia

- **ENTRENADOR**: sin endpoint nuevo. `GET /api/clubes/{clubId}/categorias` ya le devuelve solo sus
  categorías activas asignadas, cada una con `numeroJugadores`, sus equipos y sus entrenadores con
  los equipos que dirige. La pantalla toma de ahí su propia asignación (`usuarioRolId` igual a
  `miUsuarioRolId`) para decir qué equipos dirige; si no dirige ninguno en concreto, "Toda la
  categoría". Cada categoría enlaza a su detalle, que ya lista a sus jugadores **(supuesto 3)**. Sin
  categorías, el mensaje de RF-012.
- **Familia**: sin endpoint nuevo. La tarjeta del jugador lee `GET …/ficha` de `miUsuarioRolId`
  (nombres, apellidos, categoría, equipos y `fotoActualizadaEn`) y enlaza a "Mi ficha". La tarjeta
  "Mi categoría" de la 003 se queda como está (equipos y entrenadores).
- **Alternativas**: un endpoint de inicio por rol que devuelva todo junto (otro contrato para datos
  que ya existen, §19).

## 11. Los espacios reservados

- **Decisión**: textos fijos en la pantalla, sin datos de ejemplo y sin pedir nada a la API:
  - Columna "Al día": "Sin datos aún", con el título accesible "El estado de pago todavía no está
    disponible" y el estilo de texto suave, el mismo para todos **(supuesto 5)**.
  - "Próximos entrenamientos": una tarjeta con "Los entrenamientos todavía no están disponibles en
    la aplicación." para todos los roles (RF-014).
- **Motivo**: CE-007 exige que nada se pueda tomar por real. Sin campo en la API, cuando existan los
  pagos y los entrenamientos se añadirá el dato y no habrá que retirar ningún valor falso.
- **§24**: el texto no usa los colores de estado de pago (verde o rojo) para no parecer un estado.

## 12. Escudo y colores por el PRESIDENTE

- **Decisión**: dos operaciones nuevas del club, solo PRESIDENTE:
  `PUT /api/clubes/{clubId}/colores` y `PUT /api/clubes/{clubId}/escudo`, que devuelven `ClubDto`.
  Las del panel (`/api/plataforma/clubes/{clubId}/…`) no cambian.
- **Servicio**: `ServicioIdentidadClub` pasa a tener dos entradas por operación, como ya hace
  `ServicioConfiguracionClub`: la del panel, que recibe el `clubId`, y la del propio club, que usa
  el club de la petición (`IRepositorioClub`). Las dos comparten la validación (`ValidadorColores`,
  `ValidadorImagen`), los códigos de error (`escudo_no_es_imagen`, `escudo_demasiado_grande`,
  `datos_invalidos`) y el aumento de `VersionEscudo`, así que "mismas reglas" (RF-017) es literal.
- **Guardar el escudo dentro del club**: `IRepositorioEscudos` gana `GuardarAsync`, que escribe el
  escudo del club del contexto con el filtro activo. El del panel sigue en
  `IRepositorioClubesPlataforma`.
- **Aislamiento** (RF-021): el autorizador ya fija el club de la ruta y comprueba que quien pregunta
  es su PRESIDENTE; el servicio no recibe ningún otro identificador de club, así que el PRESIDENTE
  de A no puede escribir en B. Otro rol: `403 rol_no_autorizado`. Club suspendido: el PRESIDENTE
  pasa, como en "Datos del club" (escenario 8).
- **Último cambio gana** (RF-020): las dos entradas escriben las mismas columnas sin comprobar
  versión.
- **Pantalla**: el formulario de identidad del panel (`SeccionIdentidad`) se separa en un
  componente compartido, `EditorIdentidad`, que recibe la identidad, el nombre y dos funciones de
  guardado. El panel y "Datos del club" lo usan con sus rutas. El aviso de contraste
  (`temasDondeSePierde`) y la vista previa en los dos temas son así los mismos (RF-019). Tras
  guardar, "Datos del club" actualiza el club de la pantalla y recarga la sesión, que lleva la
  identidad del desplegable.
- **Alternativas**: abrir las rutas del panel al PRESIDENTE (mezcla el atributo `SoloDesarrollador`
  con la pertenencia al club); copiar el formulario (dos avisos de contraste que pueden divergir).

## 13. Las fotos en una lista larga

- **Decisión**: cada foto se pide solo cuando su fila entra en pantalla (`IntersectionObserver`), y
  se guarda en memoria por `usuarioRolId + fotoActualizadaEn`. Sin miniaturas generadas en el
  servidor.
- **Motivo**: generar miniaturas exigiría una biblioteca de imágenes (§6) para clubes de decenas de
  jugadores (§19). Reducir la imagen en el navegador antes de enviarla cambiaría el rechazo de las
  fotos de más de 1 MB que pide el escenario 8 de la historia 2.
- **Riesgo aceptado**: abrir la lista en el teléfono puede descargar varios MB. Si resulta lento con
  datos reales, se planteará la miniatura como funcionalidad aparte.

## 14. Relación con la 006

Las tareas T023 a T037 de la 006 (documento compartido entre cuentas, retiro por la familia y
reincorporación) quedaron terminadas el 2026-10-10, antes de empezar esta funcionalidad. Dos de
ellas tocan la foto:

- **El retiro por la familia** reutiliza `ServicioRetiroJugador`: el borrado de la foto que se
  añade ahí lo cubre.
- **La baja al aprobar un hermano en otro club** escribe con
  `IRepositorioRetiroEntreClubes.RetirarDeOtrasCuentasAsync`, fuera del servicio de retiro. Esa
  operación debe borrar también las filas de `FotosJugador` de los jugadores que retira, en la misma
  transacción y sin filtro de club, como el resto de lo que hace.

La prueba: un jugador con foto que queda retirado al aprobarse su hermano en otro club pierde la
foto, y la recupera sin foto si su club lo reincorpora.

## 15. Pruebas

Integración (xUnit, `WebApplicationFactory` y Testcontainers), en carpetas nuevas
`pruebas/Integracion/Jugadores/`, `Inicio/` e `Identidad/`, más los cambios en `Club/` y
`Aislamiento/`:

- **Lista**: PRESIDENTE y DIRECTIVO reciben exactamente los activos (CE-001), con el grupo "Sin
  categoría" al final; el orden de grupos y de jugadores; dos equipos, "Sin equipo" (lista vacía) y
  equipo desactivado fuera; sin retirados ni hermanos en espera; ENTRENADOR solo sus categorías y
  sin "Sin categoría"; ENTRENADOR sin categorías, lista vacía; al quitarle la asignación, deja de
  ver (CE-002); JUGADOR `403`; la respuesta no contiene documento, contacto ni ningún campo de pago.
- **Foto**: carga por la familia y por el PRESIDENTE; formato y tamaño rechazados conservan la
  anterior; quitar; sello del último cambio; dos hermanos con fotos independientes y la foto de
  perfil intacta; DIRECTIVO y ENTRENADOR no cargan ni quitan (`403`); ENTRENADOR de otra categoría
  y hermano no elegido no la ven (`404`); la familia no ve la foto de otro jugador; retirar por el
  PRESIDENTE la borra y reincorporar vuelve sin foto; cargar a un retirado `409`; otro club `404`.
- **Resumen**: club nuevo en cero; las cuatro cifras con un caso que incluye un PRESIDENTE que
  entrena, un ENTRENADOR sin categoría, una categoría y un equipo desactivados, un retirado y un
  hermano en espera (CE-003); ENTRENADOR y JUGADOR `403`.
- **Identidad**: el PRESIDENTE cambia colores y escudo y el detalle del panel lo muestra (CE-005);
  formato y tamaño rechazados; DIRECTIVO, ENTRENADOR y JUGADOR `403`; PRESIDENTE de otro club `404`
  (CE-006); club suspendido admite el cambio; el DESARROLLADOR cambia después y queda el suyo;
  otro club no cambia.
- **Aislamiento**: el contrato pasa de 61 a **68** endpoints y los de club de 39 a **46**; las
  pruebas de `AccesoClubPruebas` recorren los nuevos (`404` para el integrante de otro club y para
  el DESARROLLADOR).
- **Unitarias**: el orden de los grupos de la lista (si se extrae a una utilidad pura) y la cuenta
  de entrenadores sin repetir.
- **Frontend (Vitest)**: la llave de la caché de fotos y su vaciado al cerrar sesión; el texto de
  "Al día" no contiene "al día", "debe" ni "pendiente" como valor.

## Supuestos por confirmar

1. **Equipos del resumen**: cuentan los equipos activos de categorías activas. Un equipo activo de
   una categoría desactivada no cuenta.
2. **Foto de un retirado**: no se le puede cargar (`409 jugador_retirado`); el PRESIDENTE ve su
   ficha sin foto ni botones de foto.
3. **Inicio del ENTRENADOR**: cada categoría enlaza a su detalle del apartado "Categorías", que ya
   lista sus jugadores, y no a la lista "Jugadores".
4. **Respuesta de la foto**: cargar y quitar devuelven la ficha completa, como el resto de
   operaciones de la ficha.
5. **Texto de "Al día"**: "Sin datos aún", con la explicación accesible de que el estado de pago
   todavía no está disponible.
