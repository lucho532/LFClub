---

description: "Lista de tareas de la funcionalidad 007: jugadores, inicio por rol e identidad del club por el presidente"
---

# Tareas: Jugadores, inicio por rol e identidad del club por el presidente

**Entrada**: documentos de diseño en `specs/007-jugadores-inicio-por-rol/`

**Requisitos previos**: plan.md, spec.md, research.md, data-model.md, contracts/api.yaml, quickstart.md

**Pruebas**: incluidas. Las exigen la constitución (§20, versión 4.4.0) y el plan (research §15).
Por §27.1, cada tarea de una historia entrega el backend, la pantalla que lo usa contra la API real
y sus pruebas; las tareas que solo traen pruebas cubren algo ya construido.

**Organización**: las tareas se agrupan por historia de usuario. Las historias 1 y 2 son las dos
P1 y se construyen en ese orden. De las P2, **la historia 4 va antes que la 3** (plan, "Orden de
construcción sugerido"): no depende de ella y cierra la contradicción con la spec 001.

## Formato: `[ID] [P?] [Historia] Descripción`

- **[P]**: puede hacerse en paralelo (archivos distintos, sin depender de tareas sin terminar)
- **[US1]…[US4]**: historia de usuario de spec.md a la que pertenece la tarea
- Cada tarea indica las rutas exactas de sus archivos, relativas a la raíz del repositorio

## Convenciones para todas las tareas

Son las de los `tasks.md` de la 001 a la 006, que siguen vigentes. Resumen y lo que se añade:

- **Esquema** (data-model.md): solo la tabla `FotosJugador` de T002 y su migración
  `FotosDeJugadores`. Ninguna otra tarea crea tablas, columnas ni índices; si parece necesitarlos,
  se detiene y se informa (§25).
- **Contrato**: `specs/007-jugadores-inicio-por-rol/contracts/api.yaml` manda en rutas, cuerpos,
  códigos de estado y códigos de error. `AccesoClubPruebas` lee los contratos de todas las specs:
  T001 deja comentadas las siete operaciones nuevas y cada tarea que construye una la descomenta y
  sube los dos contadores (contrato y endpoints de club). Recorrido: 61/39 → 62/40 (T006) →
  63/41 (T009) → 65/43 (T010) → 67/45 (T014) → 68/46 (T018).
- **Nada cambia para lo anterior** (§27.2): las pruebas de la 001 a la 006 no se tocan, salvo las
  que nombra cada tarea (`IdentidadPruebas` en T015 y los contadores de `AccesoClubPruebas`). Si
  una falla y no está claro si es una regresión, se detiene y se pregunta.
- **Quién ve a un jugador** (research §3 y §7): la lista se limita con `EsJugadorDelClub`,
  `IRepositorioAsignaciones.CategoriasDeAsync` y `ReglaAlcanceDeCategorias.PuedeVer`; la foto, con
  `AccesoAFicha` y `ReglaAccesoAFicha` (`Ve` para ver, `Cambia` para cargar y quitar). **Ninguna
  regla nueva.** El ENTRENADOR se consulta en cada petición, nunca desde el token.
- **Como si no existiera** (RF-008): un jugador fuera de alcance, un hermano no elegido, un
  jugador sin foto y un identificador mal formado responden el mismo `404 no_encontrado`.
- **Espacios reservados** (RF-006, RF-014, CE-007): "Al día" y "Próximos entrenamientos" son
  textos fijos de la pantalla. Ninguna respuesta de la API lleva un campo de pago ni de
  entrenamientos, y la pantalla no usa los colores de estado de pago para ellos (§24).
- **Nombres** (§2.1, §2.2): todo en español. "Jugador", "entrenador", "categoría", "equipo"; no se
  crea ninguna entidad `Jugador`, `Alumno` ni `Acudiente` (Seguimiento de complejidad del plan).
- **Documentación** (§3): toda clase nueva lleva documentación XML (o comentario de componente en
  el frontend) que diga qué representa, su responsabilidad y qué no hace; toda clase que cambia de
  responsabilidad la actualiza en la misma tarea.
- **Tamaño** (§2.3): ningún archivo escrito a mano supera las 250 líneas.
  `frontend/src/compartido/api/tipos.ts` tiene 239: los tipos nuevos van en `tiposJugadores.ts`.
  `frontend/src/privado/DisposicionClub.tsx` tiene 209: solo gana el enlace.
- **Flujo** (§5): Controlador → `IServicio` → `IRepositorio` → EF Core. Los controladores solo
  delegan.
- **Aislamiento** (§7.1): todo bajo `/api/clubes/{clubId}` con el filtro activo. La única
  escritura que cruza clubes es la que ya hace `RetirarDeOtrasCuentasAsync` (T012).
- **Pantallas** (CE-008, §24): componentes `Tarjeta`, `Tabla`, `Avatar`, `Aviso`, `Boton`,
  `EtiquetaEstado` y `DialogoConfirmacion` existentes; legibles a 360 px y en los dos temas, sin
  desplazamiento horizontal de la página.
- **Terminado** (§27.2): una tarea se marca hecha cuando compila, pasan
  `dotnet test backend/LaPecosa.sln` y `npm --prefix frontend test`, `tsc` y `eslint` no dan
  errores y pasa `scripts/verificar-tamano.ps1`.

## Supuestos del plan que estas tareas aplican

El plan pidió confirmarlos (research.md, "Supuestos por confirmar"). Las tareas los dan por buenos;
si el propietario cambia alguno, cambia solo la tarea indicada.

| # | Supuesto | Tarea | Estado |
| --- | --- | --- | --- |
| 1 | El resumen cuenta los equipos activos de categorías activas | T018 | Por confirmar |
| 2 | A un jugador retirado no se le puede cargar foto (`409 jugador_retirado`); su ficha no muestra botones de foto | T010 | Por confirmar |
| 3 | En el inicio del ENTRENADOR cada categoría enlaza a su detalle de "Categorías", no a la lista "Jugadores" | T016 | Por confirmar |
| 4 | Cargar y quitar la foto devuelven la ficha completa | T010 | Por confirmar |
| 5 | "Al día" dice "Sin datos aún", con el título accesible "El estado de pago todavía no está disponible" | T006 | Por confirmar |

---

## Fase 1: Preparación (infraestructura compartida)

**Propósito**: que la batería siga en verde mientras el contrato de la 007 ya existe en `specs/`.

- [X] T001 Comentar en `specs/007-jugadores-inicio-por-rol/contracts/api.yaml` las siete operaciones nuevas (`GET /api/clubes/{clubId}/jugadores`; `GET`, `PUT` y `DELETE` de `/jugadores/{usuarioRolId}/foto`; `GET /resumen`; `PUT /colores`; `PUT /escudo`), cada una con una marca `# 007-T0NN` que diga qué tarea la descomenta. Las rutas de la ficha y del retiro ya existen y quedan sin comentar. Comprobar que `AccesoClubPruebas` (`backend/pruebas/Integracion/Aislamiento/AccesoClubPruebas.cs`) sigue en 61 y 39 y que la batería entera pasa

---

## Fase 2: Cimientos (requisitos que bloquean todas las historias)

**Propósito**: la tabla de fotos, su repositorio, el sembrador de pruebas y los tipos del frontend.

**⚠️ CRÍTICO**: ninguna historia empieza hasta terminar esta fase.

- [X] T002 Crear la entidad `FotoJugador` en `backend/src/LaPecosa.Dominio/Entidades/FotoJugador.cs` con la forma de `FotoPerfil` y `EscudoClub`: `UsuarioRolId` (`Guid`, "Clave primaria y foránea a `UsuarioRol`, borrado en cascada. Una foto por jugador y club"), `ClubId` (`Guid`, "Foránea a `Club`, borrado en cascada", implementa `IPerteneceAClub`), `Contenido` (`byte[]`, "Obligatorio. Máximo 1 MB"), `TipoContenido` (`string`, "máx. 20", "Obligatorio. `image/png`, `image/jpeg` o `image/webp`") y `GuardadaEn` (`DateTime` UTC, "Obligatorio. Se reescribe en cada carga"). Su configuración en `backend/src/LaPecosa.Infraestructura/Datos/Configuraciones/ConfiguracionFotoJugador.cs` (tabla `FotosJugador`, `bytea`, las dos foráneas en cascada, sin índices adicionales), el `DbSet<FotoJugador> FotosJugador` en `backend/src/LaPecosa.Infraestructura/Datos/ContextoLaPecosa.cs` bajo el filtro de club, y la migración generada `FotosDeJugadores` en `backend/src/LaPecosa.Infraestructura/Datos/Migraciones/`. Documentación XML: la entidad dice que es independiente de la foto de perfil y que se borra físicamente (§14.2); actualizar la de `UsuarioRol.cs` (de él cuelga la foto, que se borra al retirarlo o al cambiar de rol) y la de `FichaJugador.cs` (cargar o quitar la foto sella el último cambio aunque viva en otra tabla)
- [X] T003 [P] Crear `IRepositorioFotosJugador` en `backend/src/LaPecosa.Aplicacion/Interfaces/IRepositorioFotosJugador.cs` y `RepositorioFotosJugador` en `backend/src/LaPecosa.Infraestructura/Repositorios/RepositorioFotosJugador.cs`, con el filtro de club activo: `ObtenerAsync(usuarioRolId)` (contenido y tipo), `FechaAsync(usuarioRolId)` y `FechasAsync(usuarioRolIds)` (solo `GuardadaEn`, sin leer los bytes, en una sola consulta), `GuardarAsync(usuarioRolId, clubId, contenido, tipo, guardadaEn)` (alta o reemplazo) y `BorrarAsync(usuarioRolId)` (no hace nada si no hay fila). Sin reglas. Registrarlo en `backend/src/LaPecosa.Api/Configuracion/RegistroDeServicios.cs`
- [X] T004 [P] Crear `SembradorJugadores` en `backend/pruebas/Integracion/Base/SembradorJugadores.cs` (máx. 250 líneas, apoyado en `SembradorCategorias` y `SembradorHermanos`) que arme el club de quickstart.md, "Datos de partida": presidente, directivo asignado como entrenador de la 2015, entrenador E1 solo de la 2014 dirigiendo el equipo "A", entrenador E2 sin categorías; categorías 2014 (equipos "A" y "B") y 2015 activas y 2016 desactivada con un equipo activo; jugadores Ana (2014, A y B), Beto (2014, sin equipo), Caro (2015), Dani (sin categoría), Eva (retirada) y Luis (hermano de Ana en espera, en la cuenta de Ana). Más `PonerFotoAsync(usuarioRol, bytes)` que escribe una fila de `FotosJugador` directamente, para las pruebas que solo leen la foto
- [X] T005 [P] Crear `frontend/src/compartido/api/tiposJugadores.ts` con `GrupoDeJugadoresDto` (`categoria: CategoriaDeGrupoDto | null`, `jugadores`), `CategoriaDeGrupoDto` (`categoriaId`, `anio`), `JugadorDeListaDto` (`usuarioRolId`, `nombres`, `apellidos`, `equipos: string[]`, `fotoActualizadaEn: string | null`) y `ResumenClubDto` (`jugadores`, `equipos`, `entrenadores`, `categorias`), copiados de `contracts/api.yaml`, y añadir `fotoActualizadaEn: string | null` a la ficha en `frontend/src/compartido/api/tiposFicha.ts`

**Punto de control**: la migración se aplica al arrancar, ningún jugador tiene foto y la batería de la 001 a la 006 sigue en verde.

---

## Fase 3: Historia 1 - El club consulta la lista de jugadores (Prioridad: P1) 🎯 MVP

**Objetivo**: PRESIDENTE, DIRECTIVO y ENTRENADOR tienen "Jugadores" en el menú y ven a los activos agrupados por categoría, con iniciales, nombre, equipos y "Al día" reservado.

**Prueba independiente**: con el club de `SembradorJugadores`, entrar como PRESIDENTE, DIRECTIVO, E1, E2 y la cuenta de Ana y comprobar qué ve cada uno (quickstart, bloque 1).

- [X] T006 [US1] Lista del PRESIDENTE y del DIRECTIVO, de punta a punta. Backend: DTOs `GrupoDeJugadoresDto`, `CategoriaDeGrupoDto` y `JugadorDeListaDto` en `backend/src/LaPecosa.Aplicacion/DTOs/` (sin documento, contacto, salud, documentación ni pago); `IRepositorioJugadores.ListarDelClubAsync` en `backend/src/LaPecosa.Aplicacion/Interfaces/IRepositorioJugadores.cs` y su implementación en `backend/src/LaPecosa.Infraestructura/Repositorios/RepositorioJugadores.cs` (una consulta: `UsuarioRol` con `Rol = JUGADOR`, `EstadoIngreso = APROBADO` y `Activo = true`, con su categoría y sus equipos activos); `MapperListaJugadores` en `backend/src/LaPecosa.Aplicacion/Mappers/MapperListaJugadores.cs` (grupos por `Categoria.Anio` ascendente, "Sin categoría" con `categoria: null` al final, jugadores con `OrdenDePersonas.PorApellidos`, equipos por nombre, sin grupos vacíos); `IServicioListaJugadores` y `ServicioListaJugadores` en `backend/src/LaPecosa.Aplicacion/Servicios/` e `Implementaciones/`, que completa `fotoActualizadaEn` con una sola llamada a `IRepositorioFotosJugador.FechasAsync` y entrega "Sin categoría" solo a los roles con `VeListasDelClub`; `GET /api/clubes/{clubId}/jugadores` en `backend/src/LaPecosa.Api/Controladores/Club/ControladorJugadores.cs`, para PRESIDENTE, DIRECTIVO y ENTRENADOR (`403 rol_no_autorizado` al JUGADOR); registro en `backend/src/LaPecosa.Api/Configuracion/RegistroDeCasosDeUso.cs`; descomentar la operación en el contrato y subir los contadores a 62 y 40. Pantalla: `IconoJugadores` en `frontend/src/compartido/componentes/Iconos.tsx`; enlace "Jugadores" en `frontend/src/privado/DisposicionClub.tsx` solo para PRESIDENTE, DIRECTIVO y ENTRENADOR; ruta `jugadores` en `frontend/src/App.tsx`; `frontend/src/privado/jugadores/ListaJugadores.tsx` (una `Tarjeta` por grupo, título con el año o "Sin categoría"; con `403` muestra un `Aviso` y no la lista); `frontend/src/privado/jugadores/TablaDeGrupo.tsx` (iniciales con `Avatar`, nombre completo que abre `jugadores/:usuarioRolId/ficha` con `state: { desde: 'jugadores' }`, equipos separados por coma o "Sin equipo", columna "Al día"; nombres largos se cortan sin desbordar a 360 px); `frontend/src/privado/jugadores/textos.ts` con el texto de "Al día" del supuesto 5 y "Sin equipo". Pruebas: `backend/pruebas/Integracion/Jugadores/ListaJugadoresPruebas.cs` — PRESIDENTE y DIRECTIVO reciben exactamente Ana, Beto, Caro y Dani (CE-001); orden de grupos y de jugadores; "Sin categoría" al final; Ana con "A" y "B", Beto con lista vacía; un equipo desactivado no aparece; sin Eva ni Luis; club nuevo da lista vacía; la cuenta de Ana recibe `403 rol_no_autorizado`; el JSON no contiene `documento`, `correo`, `telefono`, `salud`, `documentacion` ni ningún campo de pago
- [X] T007 [US1] Alcance del ENTRENADOR en la lista, de punta a punta. Backend: en `backend/src/LaPecosa.Aplicacion/Implementaciones/ServicioListaJugadores.cs`, para quien no tiene `VeListasDelClub`, filtrar con `IRepositorioAsignaciones.CategoriasDeAsync` (consultado en cada petición) y `ReglaAlcanceDeCategorias.PuedeVer`: solo categorías activas con asignación activa, todos sus equipos, nunca "Sin categoría"; sin categorías, lista vacía. Pantalla: en `frontend/src/privado/jugadores/ListaJugadores.tsx`, si el rol es ENTRENADOR y la lista viene vacía, el mensaje "Todavía no tienes categorías asignadas." (escenario 9). Pruebas: `backend/pruebas/Integracion/Jugadores/ListaDelEntrenadorPruebas.cs` — E1 recibe solo la 2014 con Ana y Beto, también Beto aunque no esté en el equipo que dirige; sin 2015 ni "Sin categoría"; E2 recibe lista vacía; al desactivar la asignación de E1, la siguiente petición viene vacía (CE-002); el directivo que entrena la 2015 sigue recibiendo la lista completa
- [X] T008 [P] [US1] Pruebas puras: `backend/pruebas/Unitarias/Utilidades/OrdenDeGruposPruebas.cs` sobre `MapperListaJugadores` (años desordenados salen ascendentes, "Sin categoría" siempre último, apellidos con tildes y mayúsculas según `OrdenDePersonas`, grupos vacíos fuera) y `frontend/pruebas/textosJugadores.test.ts` (el texto de "Al día" y su título no contienen "al día", "debe", "deuda" ni "pendiente" como valor, y es el mismo para cualquier jugador)

**Punto de control**: quickstart, bloque 1, completo. Todos los jugadores muestran iniciales.

---

## Fase 4: Historia 2 - El club abre a un jugador y ve toda su información (Prioridad: P1)

**Objetivo**: la ficha de la 005 con la foto en grande arriba; la familia y el PRESIDENTE cargan, cambian y quitan la foto; la foto se ve en miniatura en la lista y se borra al dejar el club.

**Prueba independiente**: abrir a Ana como PRESIDENTE, DIRECTIVO y E1; cargar, rechazar y quitar fotos como el PRESIDENTE y como la cuenta de Ana; retirar y reincorporar a Beto (quickstart, bloque 2).

- [X] T009 [US2] Ver la foto, de punta a punta. Backend: `FotoActualizadaEn` (`DateTime?`) al final de `backend/src/LaPecosa.Aplicacion/DTOs/FichaJugadorDto.cs`, llenado en `backend/src/LaPecosa.Aplicacion/Mappers/MapperFicha.cs` con la fecha que `backend/src/LaPecosa.Aplicacion/Implementaciones/ServicioConsultaFicha.cs` lee con `IRepositorioFotosJugador.FechaAsync` (vale para todas las operaciones de la ficha que devuelven el DTO); `IServicioFotoJugador` en `backend/src/LaPecosa.Aplicacion/Servicios/IServicioFotoJugador.cs` y `ServicioFotoJugador` en `backend/src/LaPecosa.Aplicacion/Implementaciones/ServicioFotoJugador.cs` con `ObtenerAsync`: resuelve `AccesoAFicha` y exige `Ve`; sin alcance o sin foto, `404 no_encontrado`; `GET /api/clubes/{clubId}/jugadores/{usuarioRolId}/foto` en `backend/src/LaPecosa.Api/Controladores/Club/ControladorFotoJugador.cs`, abierto a todos los roles del club, que devuelve los bytes con su `TipoContenido` y `Cache-Control: private, no-store`; registro; descomentar el `GET` en el contrato y subir los contadores a 63 y 41. Pantalla: `frontend/src/privado/jugadores/fotosJugador.ts` (pide la foto con la sesión, como `useFotoPerfil`; caché en memoria por `usuarioRolId + fotoActualizadaEn`; con `null` no pide nada; `vaciarFotosJugador()`), llamado al cerrar sesión desde `frontend/src/compartido/sesion/ProveedorSesion.tsx`; `frontend/src/privado/jugadores/FotoJugador.tsx` (tamaño `miniatura` o `grande`, iniciales con `Avatar` sin foto o si falla la carga); `frontend/src/privado/ficha/SeccionFoto.tsx` con la foto en grande; en `frontend/src/privado/ficha/FichaJugador.tsx`, `SeccionFoto` arriba y, si `location.state.desde === 'jugadores'`, el enlace de volver dice "← Jugadores" y lleva a la lista (sin la marca, lo de la 005). Pruebas: `backend/pruebas/Integracion/Jugadores/QuienVeLaFotoPruebas.cs` (con `PonerFotoAsync`) — la ven la cuenta de Ana con Ana elegida, el PRESIDENTE, el DIRECTIVO y E1; `404` para E2, para el ENTRENADOR de la 2015 con Ana, para la cuenta de Ana con Luis elegido, para la cuenta de otro jugador y para un jugador sin foto; la ficha trae `fotoActualizadaEn` igual a `GuardadaEn` o `null`; la cabecera `Cache-Control`. `frontend/pruebas/fotosJugador.test.ts` — la llave cambia con la fecha, `null` no pide, y la caché queda vacía tras `vaciarFotosJugador()`
- [X] T010 [US2] Cargar, cambiar y quitar la foto, de punta a punta. Backend: códigos `foto_no_es_imagen`, `foto_demasiado_grande` (los textos de la foto de perfil) y `jugador_retirado` en `backend/src/LaPecosa.Aplicacion/Utilidades/ErroresDeFicha.cs`; en `ServicioFotoJugador`, `GuardarAsync` y `QuitarAsync` con el patrón de `ServicioDocumentosJugador`: resuelve `AccesoAFicha` y exige `Cambia`; valida con `ValidadorImagen` fuera de la transacción ("PNG, JPEG o WebP reconocidos por su firma binaria y de hasta 1 MB"); niega la carga a un retirado con `409 jugador_retirado` (supuesto 2); dentro de la transacción bloquea el club, guarda o borra la fila con `GuardadaEn` en UTC y llama a `IRepositorioFichas.SellarCambioAsync`; devuelve la ficha (supuesto 4). Quitar sin foto no falla y también sella. `PUT` (multipart, campo `archivo`) y `DELETE` en `ControladorFotoJugador`, solo PRESIDENTE y JUGADOR (`403 rol_no_autorizado` a DIRECTIVO y ENTRENADOR); descomentarlos en el contrato y subir los contadores a 65 y 43. Pantalla: en `frontend/src/privado/ficha/SeccionFoto.tsx`, si `permisos.puedeCambiar` y el jugador no está retirado, "Cargar foto" o "Cambiar foto" y "Quitar foto" (con `DialogoConfirmacion`); el `Aviso` del motivo si se rechaza y la foto anterior a la vista; tras guardar, repinta la ficha con la respuesta. Pruebas: `backend/pruebas/Integracion/Jugadores/FotoJugadorPruebas.cs` — carga por la cuenta de Ana y por el PRESIDENTE; reemplazo cambia `fotoActualizadaEn`; formato y tamaño rechazados (`Imagenes.cs`) conservan la anterior; quitar deja `null`; el último cambio dice quién lo hizo; foto de Ana sin cambiar la de Luis ni la foto de perfil de la cuenta (`GET /api/cuenta/foto` intacta); DIRECTIVO y E1 `403`; la cuenta de Ana sobre Beto `404`; el PRESIDENTE sobre Eva `409 jugador_retirado`; quitar sobre Eva `200` sin cambios de foto
- [X] T011 [US2] Miniaturas en la lista: en `frontend/src/privado/jugadores/TablaDeGrupo.tsx`, `FotoJugador` en tamaño miniatura que pide la foto solo cuando su fila entra en pantalla (`IntersectionObserver`, con un `useEnPantalla` en `frontend/src/privado/jugadores/fotosJugador.ts` o en un archivo propio si este pasara de 250 líneas) y reutiliza la caché de T009; sin `fotoActualizadaEn`, las iniciales sin petición. Comprobar a mano que tras cambiar la foto en la ficha y volver con "← Jugadores" la lista muestra la nueva
- [X] T012 [US2] La foto se borra al dejar el club. Backend: en `backend/src/LaPecosa.Aplicacion/Implementaciones/ServicioRetiroJugador.cs`, `IRepositorioFotosJugador.BorrarAsync` en la misma transacción que el retiro (cubre al PRESIDENTE y a la familia, research §6), con su documentación XML actualizada; en `backend/src/LaPecosa.Infraestructura/Repositorios/Plataforma/RepositorioRetiroEntreClubes.cs`, `RetirarDeOtrasCuentasAsync` borra también las filas de `FotosJugador` de los jugadores que retira, en la misma transacción y sin filtro de club, como el resto de lo que hace (research §14); en la documentación de `FotoJugador`, que el borrado por cambio de rol (§12.2) queda para la funcionalidad que lo construya. Pantalla: nada nuevo (la reincorporación de la 003 ya muestra las iniciales). Pruebas: `backend/pruebas/Integracion/Jugadores/FotoAlRetirarPruebas.cs` — retirar a Beto con foto por el PRESIDENTE borra la fila; la familia que se retira pierde la foto; un jugador con foto que queda retirado al aprobarse su hermano en otro club pierde la foto (el escenario de `SembradorHermanos`); al reincorporar, `fotoActualizadaEn` es `null` y `GET …/foto` da `404`; la foto de otro jugador del mismo club no se toca

**Punto de control**: quickstart, bloque 2, completo. Historias 1 y 2 juntas forman el mínimo útil.

---

## Fase 5: Historia 4 - El presidente cambia el escudo y los colores de su club (Prioridad: P2)

**Objetivo**: el PRESIDENTE cambia escudo y colores desde "Datos del club" con el mismo editor, vista previa y aviso de contraste que el panel.

**Prueba independiente**: el PRESIDENTE de A cambia escudo y colores; el panel del DESARROLLADOR lo muestra y el club B no cambia (quickstart, bloque 4).

- [X] T013 [P] [US4] Separar el formulario de identidad del panel en `frontend/src/compartido/componentes/EditorIdentidad.tsx`: recibe la identidad, el nombre del club y dos funciones (`guardarColores`, `guardarEscudo`) que devuelven el club actualizado; conserva la vista previa en los dos temas y el aviso de `temasDondeSePierde`, que no impide guardar. `frontend/src/plataforma/SeccionIdentidad.tsx` pasa a usarlo con las rutas `/api/plataforma/clubes/{clubId}/…`, sin cambios visibles. Comprobar a mano el panel y que `frontend/pruebas/contraste.test.ts` y `coloresClub.test.ts` siguen en verde
- [X] T014 [US4] Identidad por el PRESIDENTE, de punta a punta. Backend: en `backend/src/LaPecosa.Aplicacion/Servicios/IServicioIdentidadClub.cs` y `backend/src/LaPecosa.Aplicacion/Implementaciones/ServicioIdentidadClub.cs`, las entradas del propio club (`ActualizarColoresDelClubAsync`, `ActualizarEscudoDelClubAsync`) que usan el club de la petición vía `IRepositorioClub`, con la misma validación (`ValidadorColores`, `ValidadorImagen`), los mismos códigos (`datos_invalidos`, `escudo_no_es_imagen`, `escudo_demasiado_grande`) y el mismo aumento de `VersionEscudo` que las del panel, compartiendo el código; `GuardarAsync` en `backend/src/LaPecosa.Aplicacion/Interfaces/IRepositorioEscudos.cs` y `backend/src/LaPecosa.Infraestructura/Repositorios/RepositorioEscudos.cs` (alta o reemplazo del escudo del club del contexto, con el filtro activo); `backend/src/LaPecosa.Api/Controladores/Club/ControladorIdentidadDelClub.cs` con `PUT /api/clubes/{clubId}/colores` y `PUT /api/clubes/{clubId}/escudo`, solo PRESIDENTE (también con el club suspendido, como "Datos del club"), que devuelven `ClubDto`; registro; descomentar las dos operaciones y subir los contadores a 67 y 45. Documentación XML actualizada en `ServicioIdentidadClub`, `IServicioIdentidadClub`, `IRepositorioEscudos`, `backend/src/LaPecosa.Api/Controladores/Plataforma/ControladorIdentidadClub.cs` (ya no es el único), `backend/src/LaPecosa.Dominio/Entidades/Club.cs` y `EscudoClub.cs` (dejan de decir que solo el DESARROLLADOR los cambia). Pantalla: `frontend/src/privado/ConfiguracionClub.tsx` muestra, bajo el formulario de la 001, `EditorIdentidad` con las rutas del club; tras guardar actualiza el club de la pantalla y recarga la sesión para que el menú, la barra y el desplegable muestren la identidad nueva. Pruebas: `backend/pruebas/Integracion/Identidad/IdentidadPorElPresidentePruebas.cs` — el PRESIDENTE cambia colores y escudo y `GET /api/plataforma/clubes/{clubId}` y `GET /api/clubes/{clubId}` lo muestran, con `urlEscudo` nueva (CE-005); escudo de más de 1 MB y no imagen rechazados y el anterior intacto; colores mal formados `400 datos_invalidos`; DIRECTIVO, ENTRENADOR y JUGADOR `403 rol_no_autorizado`; el PRESIDENTE de B sobre A `404` (CE-006); club suspendido admite el cambio; el DESARROLLADOR cambia después y queda el suyo; el club B no cambia
- [X] T015 [US4] Ajustar `backend/pruebas/Integracion/Club/IdentidadPruebas.cs`: retirar o reescribir solo las afirmaciones de que el PRESIDENTE no puede cambiar la identidad (escenario 2 de la historia 4 de la 001, que la spec deja sin efecto); las del panel del DESARROLLADOR no cambian. Si alguna otra prueba de la 001 afirma lo mismo, se ajusta igual y se nombra en el mensaje de commit

**Punto de control**: quickstart, bloque 4, completo.

---

## Fase 6: Historia 3 - Cada rol tiene su propio inicio (Prioridad: P2)

**Objetivo**: resumen del club para PRESIDENTE y DIRECTIVO, categorías para el ENTRENADOR y el jugador para la familia, todos con "Próximos entrenamientos" reservado.

**Prueba independiente**: entrar al club A con cada rol y comparar el inicio con lo sembrado (quickstart, bloque 3).

- [X] T016 [P] [US3] Inicio del ENTRENADOR en `frontend/src/privado/inicio/InicioEntrenador.tsx`, sin endpoint nuevo: lee `GET /api/clubes/{clubId}/categorias` (que ya le da solo sus categorías activas), y por cada una muestra el año, `numeroJugadores` y "Dirige: …" con los equipos de su propia asignación (`usuarioRolId === miUsuarioRolId`) o "Toda la categoría"; cada categoría enlaza a `categorias/:categoriaId` (supuesto 3). Sin categorías, "Todavía no tienes categorías asignadas." Aún no se monta en el inicio: lo hace T018
- [X] T017 [P] [US3] Tarjeta del jugador para la familia en `frontend/src/privado/inicio/TarjetaMiJugador.tsx`, sin endpoint nuevo: lee `GET …/jugadores/{miUsuarioRolId}/ficha` (el jugador elegido, con la cabecera de la 006 que ya envía el cliente) y muestra `FotoJugador` en grande o las iniciales, nombre completo, categoría o "Sin categoría", equipos o "Sin equipo" y el enlace "Abrir mi ficha". Aún no se monta en el inicio: lo hace T018
- [X] T018 [US3] Resumen del club e inicio por rol, de punta a punta. Backend: `ResumenClubDto` en `backend/src/LaPecosa.Aplicacion/DTOs/ResumenClubDto.cs`; `IRepositorioResumenClub` en `backend/src/LaPecosa.Aplicacion/Interfaces/IRepositorioResumenClub.cs` y `RepositorioResumenClub` en `backend/src/LaPecosa.Infraestructura/Repositorios/RepositorioResumenClub.cs`, con una operación que calcula en el club de la petición — jugadores: `EsJugadorDelClub`; equipos: activos de categorías activas (supuesto 1); entrenadores: `UsuarioRolId` distintos de la unión de asignaciones activas en categorías activas e integrantes aprobados y activos con `Rol = ENTRENADOR`; categorías: activas; `IServicioResumenClub` y `ServicioResumenClub`; `GET /api/clubes/{clubId}/resumen` en `backend/src/LaPecosa.Api/Controladores/Club/ControladorClub.cs`, solo PRESIDENTE y DIRECTIVO (`403 rol_no_autorizado` a ENTRENADOR y JUGADOR); registro; descomentar y subir los contadores a 68 y 46. Pantalla: `frontend/src/privado/inicio/ResumenDelClub.tsx` (cuatro cifras en `Tarjeta`); `frontend/src/privado/inicio/TarjetaProximosEntrenamientos.tsx` ("Los entrenamientos todavía no están disponibles en la aplicación.", sin datos de ejemplo); `frontend/src/privado/inicio/TarjetaTuClub.tsx` con lo que hoy muestra el inicio (nombre del club, rol, contacto y "Editar los datos del club" para el PRESIDENTE); `frontend/src/privado/InicioClub.tsx` queda como selector corto por `miRol` — PRESIDENTE y DIRECTIVO el resumen (aunque entrenen, RF-015), ENTRENADOR `InicioEntrenador`, JUGADOR `TarjetaMiJugador` y la `TarjetaMiCategoria` de la 003 — y todos `TarjetaTuClub` y `TarjetaProximosEntrenamientos`. Pruebas: `backend/pruebas/Integracion/Inicio/ResumenClubPruebas.cs` — club nuevo en 0, 0, 0, 0; con `SembradorJugadores`, 4 jugadores, 3 equipos, 3 entrenadores (E1, E2 y el directivo que entrena, sin contar dos veces a quien tiene dos asignaciones) y 2 categorías (CE-003); al desactivar un equipo o retirar a un jugador, la cifra baja; E1 y la cuenta de Ana `403`; el resumen de A no cuenta nada del club B

**Punto de control**: quickstart, bloque 3, completo. Las cuatro historias funcionan.

---

## Fase 7: Cierre y aspectos transversales

**Propósito**: aislamiento, contrato, pantallas y validación de extremo a extremo.

- [X] T019 Comprobar que no queda ninguna operación comentada en `specs/007-jugadores-inicio-por-rol/contracts/api.yaml`, que `AccesoClubPruebas` pasa con 68 y 46 (los siete endpoints nuevos responden `404` al integrante de otro club y al DESARROLLADOR, y `401` sin sesión) y que Swagger (`/swagger`) describe las siete operaciones con sus DTOs y sus `problem+json`
- [X] T020 [P] Revisión de pantallas a 360 px y en escritorio, en tema claro y oscuro (CE-008): lista con un nombre y un equipo muy largos, ficha con foto grande, los cuatro inicios y "Datos del club" con el editor. Los ajustes van en `frontend/src/compartido/tema/fichas.css` o `frontend/src/compartido/tema/movil.css`. Comprobar que "Al día" y "Próximos entrenamientos" no usan los colores de estado de pago (§24) y, si el lienzo de §24 ya está disponible, contrastar con él (plan, nota 1)
- [X] T021 [P] Revisar la documentación XML y los comentarios de componente de todo lo nuevo y de las clases que nombra el plan en §3 (`ServicioIdentidadClub`, `IServicioIdentidadClub`, `IRepositorioEscudos`, `ControladorIdentidadClub`, `ServicioRetiroJugador`, `UsuarioRol`, `FichaJugador`, `ConfiguracionClub.tsx`), y que ningún archivo pase de 250 líneas con `scripts/verificar-tamano.ps1`
- [X] T022 Recorrer `specs/007-jugadores-inicio-por-rol/quickstart.md` entero en un proyecto de Compose aparte (`docker compose -p la-pecosa-validacion …` con `Brevo__Llave` vacía, para no enviar correos reales con la llave del `.env`), incluidas las comprobaciones directas contra la API. Si Docker se duerme sin contenedores, mantener uno mínimo en marcha y borrarlo al final junto con `docker compose -p la-pecosa-validacion down -v`
- [X] T023 Batería completa: `dotnet test backend/LaPecosa.sln`, `npm --prefix frontend test`, `tsc`, `eslint` y `./scripts/verificar-tamano.ps1`, con las pruebas de la 001 a la 006 en verde

---

## Dependencias y orden de ejecución

### Entre fases

- **Preparación (T001)**: sin dependencias.
- **Cimientos (T002 a T005)**: después de T001. Bloquean todas las historias.
- **Historia 1 (T006 a T008)**: después de los cimientos.
- **Historia 2 (T009 a T012)**: después de T006 (abre la ficha desde la lista y la miniatura vive en `TablaDeGrupo`). T009 y T010 se pueden probar también desde "Mi ficha" sin la lista.
- **Historia 4 (T013 a T015)**: después de los cimientos; no depende de las historias 1 ni 2.
- **Historia 3 (T016 a T018)**: T016 después de los cimientos; T017 después de T009 (usa `FotoJugador` y `fotoActualizadaEn`); T018 después de T016 y T017.
- **Cierre (T019 a T023)**: después de todas las historias.

### Entre tareas

- T002 → T003 (el repositorio usa la entidad) y → T004 (el sembrador escribe fotos).
- T006 → T007 (mismo servicio y misma pantalla) → T011.
- T006 → T008 (el mapper que prueba).
- T009 → T010 (mismo servicio, mismo controlador y `SeccionFoto`) → T012.
- T009 → T011 (caché y componente de la foto).
- T013 → T014 (`ConfiguracionClub` usa el editor) → T015.
- T016, T017 → T018 (el selector los monta).
- Los contadores de `AccesoClubPruebas` suben en el orden T006, T009, T010, T014, T018. Si se cambia el orden de las historias, cada tarea sube los suyos sobre lo que haya.

### Dentro de cada tarea

- Backend (DTO → interfaz → repositorio → servicio → controlador → registro → contrato), luego
  pantalla contra la API real, luego pruebas. La tarea no se marca hecha sin sus pruebas en verde.

---

## Ejemplos de trabajo en paralelo

```text
# Cimientos, tras T002
T003  IRepositorioFotosJugador y RepositorioFotosJugador
T004  SembradorJugadores
T005  tiposJugadores.ts y fotoActualizadaEn en tiposFicha.ts

# Historia 1, tras T006
T007  Alcance del ENTRENADOR
T008  OrdenDeGruposPruebas y textosJugadores.test.ts

# Historia 4 mientras avanza la historia 2 (archivos distintos)
T013  EditorIdentidad (frontend)
T009  Ver la foto

# Historia 3
T016  InicioEntrenador
T017  TarjetaMiJugador (tras T009)

# Cierre
T020  Revisión a 360 px y en los dos temas
T021  Documentación y tamaño
```

---

## Estrategia de implementación

### Primero lo mínimo

1. Fase 1 y fase 2.
2. Historia 1 (T006 a T008).
3. **Parar y validar**: quickstart, bloque 1. La lista ya sirve sola, con iniciales.
4. Historia 2 (T009 a T012): la lista y el detalle juntos son el mínimo útil de la spec.

### Entrega incremental

1. Cimientos → nada cambia para nadie; la batería anterior en verde.
2. Historia 1 → "Jugadores" en el menú, con iniciales y "Al día" reservado.
3. Historia 2 → foto en la ficha y en la lista; se borra al retirar. **No se publica T010 sin
   T012**: se podría cargar la foto de un jugador que luego se retira y quedaría guardada, justo lo
   que §14.2 prohíbe.
4. Historia 4 → el PRESIDENTE cambia su identidad. T015 va con T014 en el mismo commit.
5. Historia 3 → el inicio de cada rol.
6. Cierre → contrato, aislamiento, 360 px, quickstart y batería.

Cada paso deja la batería en verde y no rompe lo anterior.

---

## Notas

- La API aplica la migración `FotosDeJugadores` al arrancar; no cambia ningún dato existente y
  todos los jugadores empiezan con sus iniciales.
- Abrir la lista en el teléfono puede descargar varios MB de fotos (research §13, riesgo aceptado).
  Si resulta lento con datos reales, se anota: la miniatura en el servidor es otra funcionalidad.
- El borrado de la foto por cambio de rol (§12.2) no tiene tarea: no existe todavía ninguna
  operación que cambie a un JUGADOR de rol. Queda documentado en `FotoJugador` (T012).
- Si una prueba de la 001 a la 006 falla al tocar `ServicioRetiroJugador`,
  `RetirarDeOtrasCuentasAsync`, `ServicioIdentidadClub`, `MapperFicha` o `InicioClub` y no está
  claro si es una regresión o un cambio previsto, se detiene esa parte y se pregunta (§25).
- Confirmar el cambio en git al terminar cada tarea o cada historia.
