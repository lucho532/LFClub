---

description: "Lista de tareas de la funcionalidad 014: eliminar mi cuenta"
---

# Tareas: Eliminar mi cuenta

**Entrada**: documentos de diseño en `specs/014-eliminar-cuenta/`

**Requisitos previos**: plan.md, spec.md, research.md, data-model.md, contracts/api.yaml,
quickstart.md

**Pruebas**: incluidas. Las exigen la constitución (§20, versión 4.12.0) y el plan. Por §27.1, cada
tarea de una historia entrega el backend, la pantalla que lo usa contra la API real y sus pruebas;
las tareas que solo traen pruebas, solo reglas puras o solo pantalla lo dicen.

**Organización**: las tareas se agrupan por historia de usuario. Orden de construcción: base,
historia 1 (P1), historia 2 (P1), historia 3 (P1), historia 4 (P2), historia 5 (P2) y cierre.

## Formato: `[ID] [P?] [Historia] Descripción`

- **[P]**: puede hacerse en paralelo (archivos distintos, sin depender de tareas sin terminar)
- **[US1]…[US5]**: historia de usuario de spec.md a la que pertenece la tarea
- Cada tarea indica las rutas exactas de sus archivos, relativas a la raíz del repositorio

## Convenciones para todas las tareas

Son las de los `tasks.md` de la 001 a la 013, que siguen vigentes. Resumen y lo que se añade:

- **Esquema** (data-model.md): **no hay migración, tablas, columnas ni índices nuevos.** Si una tarea
  parece necesitar uno, se detiene y se informa (§25).
- **Contrato**: `specs/014-eliminar-cuenta/contracts/api.yaml` manda en rutas, cuerpos, estados y
  códigos de error. **Punto de partida esperado: contrato 111 y club 87** (confirmarlo en T002 con
  la expresión regular de `EndpointsDeLaApi` antes de fiarse). Recorrido: 111/87 → **112/87**
  (T013, `POST /api/cuenta/eliminacion`) → **113/87** (T019, `GET /api/cuenta/eliminacion`). El
  contrato ya describe las dos operaciones; el recuento real sube al construir cada una.
- **Lo anterior no cambia**: ninguna operación existente cambia. Si una prueba de la 001 a la 013
  falla y no está claro que sea una regresión, se detiene y se pregunta (§27.2).
- **Todo o nada** (RF-011): una sola `IUnidadDeTrabajo.EnTransaccionAsync`; ningún paso se confirma
  por separado.
- **Nada sale de la cuenta**: las respuestas no llevan documento ni datos de contacto (research §5);
  una credencial desconocida sigue respondiendo lo mismo que antes (RF-013).
- **Nombres** (§2.1, §2.2): todo en español: `ReglaEliminacionDeCuenta`, `NombresDeAutoria`,
  `ServicioEliminacionCuenta`, `ServicioResumenEliminacionCuenta`, `RepositorioEliminacionCuenta`.
- **Documentación** (§3): toda clase nueva lleva documentación XML (o comentario de componente en el
  frontend) con qué representa, su responsabilidad y qué no hace.
- **Tamaño** (§2.3): ningún archivo escrito a mano supera las 250 líneas. Medir con
  `scripts/verificar-tamano.ps1` al empezar (T001) y al cerrar.
- **Bash** (memoria del proyecto): los archivos nuevos se crean con la herramienta Write, no con
  heredocs; un `testhost` colgado se mata con `Get-Process testhost | Stop-Process -Force` antes de
  compilar; la batería de integración tarda unos 12 minutos.

---

## Fase 1: Preparación

**Propósito**: comprobar el punto de partida.

- [X] T001 Ejecutar `scripts/verificar-tamano.ps1` y anotar los archivos por encima de 240 líneas que
      esta funcionalidad vaya a tocar (`frontend/src/cuenta/MiPerfil.tsx`, `Entrar.tsx`,
      `Privacidad.tsx`, `frontend/src/App.tsx`); partir antes de añadir cualquiera que lo exceda.
- [X] T002 Contar las operaciones del contrato (y las de club) con la expresión regular de
      `backend/pruebas/Integracion/Base/EndpointsDeLaApi.cs` sobre `specs/*/contracts/api.yaml` y
      confirmar 111/87; si difiere, corregir los números de estas convenciones y de
      `specs/014-eliminar-cuenta/contracts/api.yaml`.
- [X] T003 Compilar `backend/LFClub.sln` y correr `npm test` en `frontend/` para dejar constancia de
      que la línea base está en verde antes de empezar.

---

## Fase 2: Base (bloquea todas las historias)

**Propósito**: regla pura, constantes, contratos de capa y repositorio compartidos por las historias.

**⚠️ CRÍTICO**: ninguna historia empieza hasta terminar esta fase.

- [X] T004 [P] Crear `backend/src/LFClub.Dominio/Reglas/ReglaEliminacionDeCuenta.cs`: función pura
      `ClubesBloqueantes` que, dada la lista de integrantes de la cuenta (club, rol) y el número de
      presidentes registrados de cada club, devuelve los clubes donde la cuenta es PRESIDENTE y hay
      uno solo. Reutiliza `ReglaUltimoPresidente.PuedeDejarElRol` para que ambas nunca discrepen
      (research §8). Documentar que no consulta la base de datos.
- [X] T005 [P] Crear `backend/src/LFClub.Dominio/Reglas/NombresDeAutoria.cs` con la constante
      `PersonaEliminada = "Persona eliminada"` (research §6).
- [X] T006 [P] Crear los DTOs en `backend/src/LFClub.Aplicacion/DTOs/`: `EliminarCuentaDto`
      (`Contrasena`, obligatoria), `ResumenEliminacionCuentaDto` (`PuedeEliminar`, `Jugadores`,
      `ClubesQueLoImpiden`), `JugadorQueSeEliminaDto` (`NombreCompleto`, `ClubNombre`, `Retirado`,
      `EnEspera`; sin documento ni otro dato de la ficha) y `ClubQueImpideDto` (`ClubId`,
      `ClubNombre`), según data-model.md.
- [X] T007 Crear `backend/src/LFClub.Aplicacion/Interfaces/IRepositorioEliminacionCuenta.cs` con:
      `AnonimizarAutoriaAsync(usuarioId, integranteIds)`, `AnularInvitacionesAsync(usuarioId, ahoraUtc)`
      y `EliminarCuentaYSusIntegrantesAsync(usuarioId, integranteIds)`; documentar el orden de
      data-model.md y que no decide reglas.
- [X] T008 Crear `backend/src/LFClub.Infraestructura/Repositorios/RepositorioEliminacionCuenta.cs`
      con `ExecuteUpdate` y `ExecuteDelete` sobre `IgnoreQueryFilters()`:
      1. `AnonimizarAutoriaAsync`: las diez columnas de la tabla "Columnas con el nombre copiado" de
         data-model.md reciben `NombresDeAutoria.PersonaEliminada`, buscando por la cuenta o por sus
         integrantes; sin tocar las filas que se borrarán en el mismo paso.
      2. `AnularInvitacionesAsync`: `AnuladaEn = ahoraUtc` donde `CreadaPorUsuarioId` es la cuenta,
         `UsadaEn` es nulo y `AnuladaEn` es nulo.
      3. `EliminarCuentaYSusIntegrantesAsync`: borra `PartidosJugador` y `AsistenciasEntrenamiento`
         de esos integrantes, luego `UsuariosRol` de la cuenta y por último la `Usuario`; deja
         `ChangeTracker.Clear()` al final como `EliminarConSusCuentasAsync`.
- [X] T009 [P] Registrar `IRepositorioEliminacionCuenta` y, cuando existan, los dos servicios en
      `backend/src/LFClub.Api/Configuracion/RegistroDeCasosDeUso.cs` (y donde se registren los
      repositorios, siguiendo a `IRepositorioPertenencias`).
- [X] T010 [P] Pruebas unitarias de `ReglaEliminacionDeCuenta` en
      `backend/pruebas/Unitarias/Reglas/ReglaEliminacionDeCuentaPruebas.cs`: presidente único; dos
      presidentes; presidente único en un club y no en otro (devuelve solo el primero); DIRECTIVO,
      ENTRENADOR y JUGADOR nunca bloquean; cuenta sin integrantes.
- [X] T011 Prueba guardián en
      `backend/pruebas/Integracion/Cuenta/EsquemaDeEliminacionPruebas.cs`: recorre el modelo de EF y,
      para cada clave foránea cuyo principal sea `Usuario` o `UsuarioRol`, exige `Cascade` o `SetNull`;
      `NoAction` solo para `PartidoJugador.UsuarioRolId` y `AsistenciaEntrenamiento.UsuarioRolId`
      (lista explícita con el motivo). El mensaje de fallo explica que una tabla de pagos debe copiar
      nombre y documento y usar `SetNull` (RF-007).

**Punto de control**: la regla y el repositorio compilan; el guardián pasa.

---

## Fase 3: Historia 1 — Eliminar mi cuenta desde la aplicación (P1) 🎯 MVP

**Objetivo**: con la contraseña correcta, la cuenta desaparece, la sesión se cierra y se vuelve a
`/entrar`.

**Prueba independiente**: con una cuenta de familia con un jugador, eliminarla: ya no puede iniciar
sesión con correo ni con documento (mismo mensaje que cualquier credencial desconocida), y ambos
quedan libres.

- [X] T012 [US1] Crear `IServicioEliminacionCuenta` en
      `backend/src/LFClub.Aplicacion/Servicios/IServicioEliminacionCuenta.cs` y
      `backend/src/LFClub.Aplicacion/Implementaciones/ServicioEliminacionCuenta.cs`: carga la cuenta
      con `ObtenerParaCambiarAsync`; la cuenta DESARROLLADOR → 404 `no_encontrado`; cuenta bloqueada →
      403 `cuenta_bloqueada` sin comprobar la contraseña; contraseña con `IHashContrasena.Verificar`:
      fallo → `RegistrarFalloDeSesionAsync` y 400 `contrasena_incorrecta` (research §4), acierto →
      reinicia fallos si `IntentosFallidos > 0`. Dentro de `EnTransaccionAsync`: lista integrantes con
      `IRepositorioPertenencias.ListarDeUsuarioAsync`, bloquea los clubes con
      `ObtenerBloqueandoAsync` en orden de identificador, cuenta presidentes con
      `ContarPresidentesAsync`, aplica `ReglaEliminacionDeCuenta` (409 `presidente_unico` con la lista
      de clubes) y ejecuta anonimizar → anular invitaciones → borrar (research §3). Documentar que no
      accede a `DbContext`.
- [X] T013 [US1] Crear `backend/src/LFClub.Api/Controladores/Cuenta/ControladorEliminacionCuenta.cs`
      con `POST /api/cuenta/eliminacion` (204), `ProducesResponseType` de 400, 401, 403, 404 y 409
      como `ControladorDatosPropios`; sin identificador de cuenta ni de club. Comprobar que coincide con el `post`
      de `specs/014-eliminar-cuenta/contracts/api.yaml`. Contrato 112/87.
- [X] T014 [P] [US1] Crear `frontend/src/compartido/api/tiposCuenta.ts` con los tipos
      `ResumenEliminacionCuenta`, `JugadorQueSeElimina` y `ClubQueImpide`, y
      `frontend/src/cuenta/EliminarCuenta.tsx` (ruta privada `/cuenta/eliminar`): título, qué se borra
      y qué se conserva en listas, advertencia de que no se puede deshacer, campo de contraseña con
      `CampoContrasena` y foco automático, botón destructivo "Eliminar mi cuenta para siempre", error
      de contraseña en texto y estado de envío. Al terminar con éxito limpia la sesión local y navega a
      `/entrar` con el aviso "Tu cuenta se eliminó" (research §10).
- [X] T015 [US1] Añadir en `frontend/src/App.tsx` la ruta `/cuenta/eliminar` dentro de
      `RutaProtegida quien="cualquiera"`, y en `frontend/src/cuenta/MiPerfil.tsx` una sección
      "Eliminar mi cuenta" con enlace a ella, oculta para la cuenta DESARROLLADOR. Mostrar el aviso de
      T014 en `frontend/src/cuenta/Entrar.tsx`.
- [X] T016 [US1] Pruebas de integración en
      `backend/pruebas/Integracion/Cuenta/EliminarCuentaPruebas.cs` (con `Sembrador*`): eliminación
      correcta → 204 y cuenta ausente; contraseña incorrecta → 400 y nada borrado; cinco fallos →
      bloqueada y 403 aun con la correcta; otra sesión de la cuenta recibe 401 en su siguiente
      petición; iniciar sesión con el correo y con el documento del jugador → el mismo 401
      `credenciales_invalidas`; registrar de nuevo con ese correo y documento funciona y no trae nada
      anterior; otra cuenta y otro club quedan intactos; sin sesión → 401.
- [X] T017 [P] [US1] Prueba de frontend en `frontend/pruebas/EliminarCuenta.test.tsx`: contraseña
      incorrecta muestra el error y no navega; éxito limpia la sesión y navega a `/entrar`.

**Punto de control**: historia 1 funcional de punta a punta con la API real.

---

## Fase 4: Historia 2 — Qué pasa con los jugadores de la cuenta (P1)

**Objetivo**: la pantalla lista por nombre y club a quienes se van con la cuenta y la eliminación
borra a todos, en todos los clubes, sin tocar el marcador.

**Prueba independiente**: una cuenta con dos jugadores en un club y uno en otro: los tres figuran en
la pantalla y, tras eliminar, desaparecen de listas, convocatorias, asistencia, goles y carta de
ambos clubes.

- [X] T018 [US2] Crear `backend/src/LFClub.Aplicacion/Implementaciones/ServicioResumenEliminacionCuenta.cs`
      y su método en `IServicioEliminacionCuenta`: devuelve `JugadoresQueSeEliminan` con todos los
      integrantes JUGADOR de la cuenta, de cualquier club y estado (activos, en espera y retirados),
      incluso con sesión limitada por documento (research §5); `ClubesQueLoImpiden` vacía y
      `PuedeEliminar = true` por ahora. Cuenta DESARROLLADOR → 404.
- [X] T019 [US2] Añadir `GET /api/cuenta/eliminacion` en
      `backend/src/LFClub.Api/Controladores/Cuenta/ControladorEliminacionCuenta.cs` y comprobar que coincide con el
      `get` de `specs/014-eliminar-cuenta/contracts/api.yaml`. Contrato 113/87.
- [X] T020 [US2] En `frontend/src/cuenta/EliminarCuenta.tsx`, cargar el resumen con `useCarga` y
      mostrar la lista de jugadores con su club (marcando en texto "retirado" y "en espera"), la
      advertencia de que se eliminan con la cuenta y el enlace "Retirar del club" a la ficha como
      alternativa (RF-016); mantener sin cambios el resto.
- [X] T021 [US2] Pruebas de integración en `EliminarCuentaPruebas.cs`: el resumen lista los tres
      jugadores con su club y estado y no lleva documento; tras eliminar, en ambos clubes sin listas,
      convocatorias, asistencia ni carta; un partido que disputaron conserva `GolesClub` y
      `GolesRival` pero sin sus filas ni goles; un jugador en espera y uno retirado también se
      eliminan; la carta y la foto del jugador y de perfil ya no existen.
- [X] T022 [P] [US2] Ampliar `frontend/pruebas/EliminarCuenta.test.tsx`: la lista muestra a los tres
      jugadores con su club y el enlace de retiro.

**Punto de control**: historias 1 y 2 funcionan juntas; los datos personales quedan borrados (RF-004
a RF-006).

---

## Fase 5: Historia 3 — Presidente, directivo y entrenador (P1)

**Objetivo**: cualquier rol elimina su cuenta; el presidente único no puede y se le dice qué club lo
impide; lo publicado queda como "Persona eliminada".

**Prueba independiente**: club con un solo presidente: bloqueo con el nombre del club; nombrar a otro
presidente y volver a intentarlo.

- [X] T023 [US3] En `ServicioResumenEliminacionCuenta` rellenar `ClubesQueLoImpiden` y
      `PuedeEliminar` con `ReglaEliminacionDeCuenta` y los recuentos de `ContarPresidentesAsync`
      (sin bloquear filas: es lectura); documentar que la decisión definitiva es la del servicio de
      eliminación (T012).
- [X] T024 [US3] En `frontend/src/cuenta/EliminarCuenta.tsx`, con `puedeEliminar` falso, mostrar el
      aviso con el nombre de cada club y "Nombra antes a otro presidente" (con enlace a
      `/club/:clubId/jugadores` o a donde se asignan roles), sin campo de contraseña ni botón.
      Manejar un 409 `presidente_unico` del `POST` mostrando lo mismo.
- [X] T025 [US3] Pruebas de integración en
      `backend/pruebas/Integracion/Cuenta/EliminarCuentaPresidentePruebas.cs`: presidente único → GET
      con `puedeEliminar` falso y el club, POST 409 `presidente_unico` y nada borrado; con otro
      presidente → se elimina; presidente único en un club y no en el otro → bloqueado por el primero;
      dos presidentes que se eliminan a la vez → exactamente uno lo logra y el club conserva uno;
      DIRECTIVO y ENTRENADOR eliminan y desaparecen de asignaciones y listas; DESARROLLADOR → 404 en
      GET y POST.
- [X] T026 [US3] Pruebas de integración en
      `backend/pruebas/Integracion/Cuenta/AutoriaAnonimaPruebas.cs`: un comunicado y un resultado
      publicados por la persona siguen visibles con "Persona eliminada" como autor, y retirados por
      ella también; ninguna de las diez columnas de data-model.md conserva su nombre; la ficha y la
      carta cambiadas por ella quedan con "Persona eliminada"; las invitaciones pendientes que envió
      quedan anuladas y no aceptan registro; las ya usadas no cambian.
- [X] T027 [P] [US3] Ampliar `frontend/pruebas/EliminarCuenta.test.tsx`: con presidente único se ve el
      club y no hay botón; la sección no aparece para DESARROLLADOR en `MiPerfil`.

**Punto de control**: las tres historias P1 cumplen RF-001 a RF-013 y RF-016.

---

## Fase 6: Historia 4 — Pedirlo desde la web, sin la aplicación (P2)

**Objetivo**: `/eliminar-cuenta` pública con los pasos, lo que se borra y se conserva, y el correo.

**Prueba independiente**: abrirla sin sesión y ver todo eso.

- [X] T028 [US4] Crear `frontend/src/cuenta/EliminarCuentaPublica.tsx` con `DisposicionCuenta`: pasos
      dentro de la aplicación (Mi perfil → Eliminar mi cuenta), lista de lo que se borra y de lo que se
      conserva (comunicados y resultados como "Persona eliminada", marcadores, pagos con nombre y
      documento cuando existan), "Olvidé mi contraseña" con enlace a `/recuperar`, y el correo
      `CORREO_PRIVACIDAD` con el plazo de 15 días y la indicación de escribir desde el correo de la
      cuenta (research §9).
- [X] T029 [US4] Añadir la ruta pública `/eliminar-cuenta` en `frontend/src/App.tsx` (fuera de
      `RutaProtegida`) y un enlace "Eliminar mi cuenta" en `frontend/src/cuenta/Entrar.tsx` junto al
      de privacidad.
- [X] T030 [P] [US4] Prueba de frontend en `frontend/pruebas/EliminarCuentaPublica.test.tsx`: sin
      sesión carga, muestra los pasos, lo que se borra y se conserva y el correo; `Entrar` enlaza a
      ella.

---

## Fase 7: Historia 5 — La política de privacidad lo describe (P2)

**Objetivo**: `/privacidad` coincide con lo que hace la aplicación y enlaza a la página.

**Prueba independiente**: leer "Tus derechos" y compararla con la pantalla de la historia 1.

- [X] T031 [US5] En `frontend/src/cuenta/Privacidad.tsx`, reescribir "Tus derechos" y "Seguridad y
      conservación": la cuenta se elimina desde la aplicación con la contraseña, qué se borra (cuenta,
      jugadores, fichas, documentos, fotos, carta y estadísticas), qué se conserva (comunicados y
      resultados sin nombre, marcadores, pagos con nombre y documento) y enlace a `/eliminar-cuenta`;
      actualizar `ACTUALIZADA_PRIVACIDAD` a la fecha del cambio.
- [X] T032 [P] [US5] Prueba de frontend en `frontend/pruebas/Privacidad.test.tsx`: menciona la
      eliminación desde la aplicación, lo que se conserva y enlaza a `/eliminar-cuenta`.

---

## Fase 8: Cierre

- [X] T033 [P] Actualizar `docs/play-store.md`: URL de eliminación de cuenta (`/eliminar-cuenta`) y el
      texto de privacidad con la misma redacción que T031.
- [X] T034 Ejecutar `scripts/verificar-tamano.ps1`, `dotnet build`, la batería completa de
      `dotnet test` (unitarias e integración, sin `testhost` colgado) y `npm test` y `npm run lint` en
      `frontend/`; corregir lo que falle sin tocar pruebas anteriores salvo regresión demostrada.
- [ ] T035 Recorrer `specs/014-eliminar-cuenta/quickstart.md` (escenarios A a E) en un proyecto de
      Compose aparte, sin la llave real de Brevo, y anotar el resultado; revisar con el propietario el
      diseño de la pantalla y de la página pública (nota 5 del plan).
- [X] T036 Marcar las tareas hechas, comprobar que el contrato quedó en 113/87 y que ningún archivo
      supera 250 líneas.

---

## Dependencias y orden

- **Fase 1** → **Fase 2** (bloquea todo) → historias.
- **US1** es el MVP y la única que crea el servicio de eliminación; **US2** y **US3** amplían su
  lectura y su pantalla (T018 y T023 tocan el mismo servicio de resumen: van en ese orden).
- **US4** y **US5** no dependen del backend: pueden construirse en paralelo con US2 y US3 una vez
  hecha la Fase 2 (la ruta de la pantalla privada existe desde T015).
- **Fase 8** al final.

## Oportunidades de paralelismo

- Fase 2: T004, T005, T006 y T010 a la vez; T007 → T008 → T009 → T011 en serie.
- US1: T014 en paralelo con T012/T013; T016 y T017 a la vez al final.
- US4 y US5 completas en paralelo con US2 y US3.

## Estrategia de implementación

1. **MVP**: Fases 1, 2 y 3. Ya permite eliminar la cuenta con contraseña y es lo que Google Play
   exige; no se publica sin la historia 3, porque deja a un club sin presidente si el servidor no
   bloquea (T012 ya lo bloquea; T023 a T025 lo hacen visible y lo prueban).
2. Añadir US2 y US3 (P1), después US4 y US5 (P2).
3. Cerrar con la fase 8; solo entonces cambiar la ficha de Google Play.
