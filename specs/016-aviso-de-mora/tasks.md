---

description: "Lista de tareas de la funcionalidad 016: aviso fijo de mora en el inicio de la familia"
---

# Tareas: Aviso fijo de mora en el inicio de la familia

**Entrada**: documentos de diseño en `specs/016-aviso-de-mora/`

**Requisitos previos**: plan.md, spec.md, research.md, data-model.md, contracts/api.yaml,
quickstart.md

**Punto de partida**: constitución 4.15.0 (§16.7, §16.8 y §16.9 ya enmendadas) y la spec 015
construida (contrato 131 y club 105, a confirmar en T001).

**Pruebas**: incluidas. Las exigen la constitución (§20) y el plan (research §8). Por §27.1, cada
tarea de una historia entrega el backend, la pantalla que lo usa contra la API real y sus pruebas; las
tareas que solo traen pruebas, solo reglas puras o solo pantalla lo dicen.

**Organización**: tareas agrupadas por historia de usuario. Las cuatro historias son P1 y comparten un
solo cálculo, así que la regla y el endpoint (base) van primero y cada historia agrega su parte
visible y sus pruebas. Orden: preparación, base, historia 4 (qué es mora), historia 1 (aviso rojo),
historia 2 (a verde), historia 3 (quién lo ve) y cierre.

## Formato: `[ID] [P?] [Historia] Descripción`

- **[P]**: puede hacerse en paralelo (archivos distintos, sin depender de tareas sin terminar)
- **[US1]…[US4]**: historia de usuario de spec.md a la que pertenece la tarea
- Cada tarea indica las rutas exactas de sus archivos, relativas a la raíz del repositorio

## Convenciones para todas las tareas

Son las de los `tasks.md` de la 001 a la 015, que siguen vigentes. Resumen y lo que se añade:

- **Sin esquema nuevo**: no hay tabla, columna, índice ni migración (data-model.md). Si una tarea
  parece necesitar alguno, se detiene y se informa (§25).
- **Contrato**: `specs/016-aviso-de-mora/contracts/api.yaml` manda en ruta, cuerpo, estados y
  códigos. **Una sola operación nueva**: contrato 131 → 132 y club 105 → 106 (confirmar el punto de
  partida en T001 con la expresión regular de `EndpointsDeLaApi`).
- **Lo anterior no cambia**: ninguna operación existente cambia. Si una prueba de la 001 a la 015
  falla y no está claro que sea una regresión, se detiene y se pregunta (§27.2).
- **La regla es una sola** (§15): el plazo, el día de corte y el estado se deciden solo en
  `ReglaMora`, en el servidor. El frontend no calcula fechas ni mora: pinta lo que recibe.
- **Dinero** (§16.2): siempre `decimal` en C#; pesos enteros. El frontend usa `formatoMoneda`.
- **Autorización en el servidor** (§15, RF-016): solo `JUGADOR` y solo el propio jugador; lo ajeno
  (otra familia, otro club, retirado) responde `404 no_encontrado`; los demás roles, `403
  rol_no_autorizado`; el DESARROLLADOR no tiene ruta de club.
- **Sin envíos ni consecuencias** (§16.7, §16.8): el aviso no usa campana, correo ni notificaciones
  del dispositivo, y estar en mora no restringe ningún acceso.
- **Nombres** (§2.1, §2.2): `ReglaMora`, `EstadoDeAvisoDeMora`, `AvisoDeMoraDto`,
  `ServicioAvisoDeMora`, `AvisoDeMora`.
- **Documentación** (§3): toda clase nueva lleva documentación XML (o comentario de componente en el
  frontend) con qué representa, su responsabilidad y qué no hace.
- **Tamaño** (§2.3): ningún archivo escrito a mano supera las 250 líneas; medir con
  `scripts/verificar-tamano.ps1` al empezar (T001) y al cerrar (T025).
- **Bash** (memoria del proyecto): los archivos nuevos se crean con la herramienta Write, no con
  heredocs; un `testhost` colgado se mata con `Get-Process testhost | Stop-Process -Force` antes de
  compilar; la batería de integración tarda unos 12 minutos.
- **Reloj**: las pruebas de integración que cruzan el día 5 y el día 6 sustituyen `IReloj`.

## Fase 1: Preparación

- [X] T001 Confirmar el punto de partida: contar las operaciones con la expresión regular de `backend/pruebas/Integracion/Base/EndpointsDeLaApi.cs` (esperado contrato 131 y club 105 en `backend/pruebas/Integracion/Aislamiento/AccesoClubPruebas.cs`) y medir tamaños con `scripts/verificar-tamano.ps1`; si algo no coincide, detenerse y avisar
- [X] T002 [P] Verificar que la rama es `016-aviso-de-mora`, que `backend` compila y que las pruebas de `ReglaMesDeCobro` y `ReglaEstadoDeCuenta` en `backend/pruebas/Unitarias/Reglas/` pasan

## Fase 2: Base (bloquea a todas las historias)

**Propósito**: la regla pura, el servicio y la operación que entrega el estado. Sin ellos ninguna
historia tiene qué mostrar.

- [X] T003 [P] Crear la enumeración `EstadoDeAvisoDeMora` (`AL_DIA`, `PENDIENTE_EN_PLAZO`, `EN_MORA`) en `backend/src/LFClub.Dominio/Enumeraciones/EstadoDeAvisoDeMora.cs`, con documentación XML; verificar cómo se serializan las demás enumeraciones de pagos (`EstadoDeCuenta`) y hacer igual
- [X] T004 Añadir `DiaEnColombia(DateTime ahoraUtc)` (devuelve `DateOnly` con el desfase fijo de UTC−5, igual que `MesEnCurso`) en `backend/src/LFClub.Dominio/Reglas/ReglaMesDeCobro.cs`
- [X] T005 Crear `ReglaMora` en `backend/src/LFClub.Dominio/Reglas/ReglaMora.cs`: recibe los cargos del jugador (tipo, mes, valor, aplicado) y el instante UTC; solo cuenta `MENSUALIDAD` con `Mes` y pendiente > 0 (pendiente con `ReglaEstadoDeCuenta.Pendiente`); el plazo de la mensualidad del mes M termina el día 5 de M inclusive en hora de Colombia, vencida desde el día 6; devuelve estado (`EN_MORA` si hay alguna vencida, si no `PENDIENTE_EN_PLAZO` si hay alguna con pendiente, si no `AL_DIA`), `ValorVencido`, `MesesVencidos`, `ValorEnPlazo` y `VenceEl` (día 5 del mes pendiente más cercano dentro del plazo, nulo si no hay); depende de T003 y T004
- [X] T006 [P] Crear `AvisoDeMoraDto` (`Estado`, `ValorVencido`, `MesesVencidos`, `ValorEnPlazo`, `VenceEl` nulo, `PagosEnRevision`) en `backend/src/LFClub.Aplicacion/DTOs/AvisoDeMoraDto.cs`, con los nombres y tipos de `contracts/api.yaml`
- [X] T007 [P] Crear `IServicioAvisoDeMora` (`ObtenerAsync(Guid usuarioRolId, UsuarioRol quienPregunta, CancellationToken)`) en `backend/src/LFClub.Aplicacion/Servicios/IServicioAvisoDeMora.cs`
- [X] T008 Crear `ServicioAvisoDeMora` en `backend/src/LFClub.Aplicacion/Implementaciones/ServicioAvisoDeMora.cs`: lee al jugador con `IRepositorioJugadores`; responde `ExcepcionDeAplicacion.NoEncontrado()` si no es un `Rol.JUGADOR` aprobado o si `ReglaAccesoACuenta.PuedeVer(quienPregunta.Rol, esElMismo, jugador.Activo)` es falso (misma comprobación que `ServicioCuentaDelJugador`), y siempre si `quienPregunta.Rol` no es `JUGADOR`; lee los cargos con `IRepositorioCargos.ListarConAplicadoAsync(jugador.Id)`, los pagos con `IRepositorioPagos.ListarDelJugadorAsync` para contar los de estado `EN_REVISION`, y el instante con `IReloj`; arma el DTO con `ReglaMora`; depende de T005 a T007
- [X] T009 Registrar `IServicioAvisoDeMora` en `backend/src/LFClub.Api/Configuracion/RegistroDeCasosDeUso.cs`
- [X] T010 Añadir `GET …/jugadores/{usuarioRolId}/cuenta/aviso` a `backend/src/LFClub.Api/Controladores/Club/ControladorCuentaJugador.cs` con `[IntegranteDelClub(Rol.JUGADOR)]`, `Cache-Control: private, no-store` y `ProducesResponseType<AvisoDeMoraDto>`; si el archivo se acerca a 250 líneas, crear `ControladorAvisoDeMora.cs` en la misma carpeta con la misma ruta base; depende de T008 y T009
- [X] T011 [P] Añadir `AvisoDeMoraDto` y `EstadoDeAvisoDeMora` a `frontend/src/compartido/api/tiposPagos.ts` con los nombres de `contracts/api.yaml` (el importe como `number`, `venceEl` como `string | null`)
- [X] T012 [P] Crear `apiAvisoDeMora.obtener(clubId, usuarioRolId)` en `frontend/src/privado/pagos/apiAvisoDeMora.ts`, al estilo de `frontend/src/privado/pagos/apiCuenta.ts`
- [X] T013 Actualizar los recuentos de `backend/pruebas/Integracion/Aislamiento/AccesoClubPruebas.cs` y de `backend/pruebas/Integracion/Base/EndpointsDeLaApi.cs` para la operación nueva (contrato 132, club 106) y comprobar que la prueba de cobertura de roles la exige para el rol JUGADOR

**Punto de control**: `backend` compila, el recuento de operaciones es 132/106 y la operación responde
con la sesión de un jugador.

## Fase 3: Historia 4 — Qué cuenta como estar en mora (P1)

**Meta**: la regla única y exacta de cuándo se enciende cada color.

**Prueba independiente**: con una mensualidad del mes en curso sin pagar, el día 5 el estado es
ámbar y el día 6 pasa a rojo; pagarla lo pasa a verde (quickstart pasos 2, 3 y 7).

- [X] T014 [P] [US4] Pruebas de `ReglaMora` en `backend/pruebas/Unitarias/Reglas/ReglaMoraPruebas.cs`: sin cargos → `AL_DIA`; mensualidad del mes día 1 y día 5 → `PENDIENTE_EN_PLAZO` con `VenceEl` el día 5; día 6 → `EN_MORA`; a las 04:59 UTC del día 6 todavía es día 5 en Colombia y a las 05:00 UTC ya es día 6; mensualidad de un mes anterior sin pagar → `EN_MORA` aunque la del mes esté pagada; abono parcial → mora solo por lo que falta; varios meses vencidos → un solo estado con la suma y la cantidad; cargo `OTRO` no cuenta; fin de año (diciembre → enero); lo aplicado mayor al valor no da negativo
- [X] T015 [P] [US4] Pruebas de `DiaEnColombia` en `backend/pruebas/Unitarias/Reglas/ReglaMesDeCobroPruebas.cs`: cambio de día a las 05:00 UTC, fin de mes y de año
- [X] T016 [US4] Pruebas de integración de los tres estados por la API en `backend/pruebas/Integracion/Pagos/AvisoDeMoraPruebas.cs` con `IReloj` sustituido: verde sin cargos, ámbar el día 5, rojo el día 6, rojo con el mes anterior sin pagar, `valorVencido` y `mesesVencidos` correctos, cargo `OTRO` ignorado, el DTO coincide con `contracts/api.yaml`

## Fase 4: Historia 1 — La familia en mora ve un aviso rojo fijo (P1)

**Meta**: el aviso en la parte superior del inicio, sin forma de cerrarlo, que lleva a la cuenta.

**Prueba independiente**: con la mensualidad del mes anterior sin pagar, el inicio de la familia
muestra el aviso rojo, sin botón de cerrar, y sigue ahí al recargar o volver a entrar (quickstart
pasos 3, 4 y 11).

- [X] T017 [P] [US1] Crear el hook `useAvisoDeMora(clubId, usuarioRolId)` en `frontend/src/privado/pagos/useAvisoDeMora.ts` con `useCarga` y la clave `${clubId}/aviso/${usuarioRolId}`, al estilo de `useCuentaJugador.ts`
- [X] T018 [US1] Crear el componente `AvisoDeMora` en `frontend/src/privado/inicio/AvisoDeMora.tsx`: usa `Aviso` con los tonos `error` (rojo), `aviso` (ámbar) y `exito` (verde); sin botón de cerrar ni de posponer; ícono y texto en cada estado (rojo: «Estás en mora: debes $X de N mensualidades vencidas»; ámbar: «Tienes la mensualidad de este mes pendiente. Vence el día 5»; verde: «Estás al día»); enlace «Ver mi cuenta» con `rutaDeLaCuenta` de `frontend/src/privado/pagos/textosPagos.ts`; mientras carga no muestra nada; si falla, aviso `neutro` «No pudimos consultar el estado de tu cuenta» con «Reintentar» (`recargar`), nunca verde; los textos y el ícono van en `frontend/src/privado/pagos/textosPagos.ts` junto a los demás de pagos; si se necesita CSS para el ámbar o el ícono, añadirlo al archivo de `frontend/src/compartido/tema/componentes.css` sin crear tokens nuevos
- [X] T019 [US1] Mostrar `AvisoDeMora` en `frontend/src/privado/InicioClub.tsx`, en la rama `JUGADOR` de `DelRol`, antes de `TarjetaMiJugador`, con `club.clubId` y `club.miUsuarioRolId`; no en las otras ramas; confirmar que los comentarios del archivo siguen al día
- [X] T020 [US1] Pruebas Vitest en `frontend/pruebas/pagos/AvisoDeMora.test.tsx` (usando `frontend/pruebas/pagos/ayudas.tsx`): el rojo muestra el valor y los meses, no hay botón de cerrar, el enlace lleva a la cuenta del jugador; el ámbar muestra el vencimiento; el verde dice «Estás al día»; el error muestra el aviso neutro con «Reintentar» y no el verde; el resto del inicio sigue visible; el estado se dice con texto e ícono

## Fase 5: Historia 2 — Al ponerse al día, el aviso pasa a verde (P1)

**Meta**: el aviso refleja lo que hace el PRESIDENTE con los pagos y el estado de los pagos en
revisión.

**Prueba independiente**: con un jugador en mora, el PRESIDENTE registra el pago que cubre lo
vencido y el inicio pasa de rojo a verde (quickstart pasos 6 a 9).

- [X] T021 [US2] Pruebas de integración en `backend/pruebas/Integracion/Pagos/AvisoDeMoraPruebas.cs`: un pago registrado que cubre todo lo vencido pasa a verde (o a ámbar si queda la del mes dentro del plazo); un pago parcial deja rojo con lo que falta; un pago en revisión no cambia el estado y sube `pagosEnRevision`; al confirmarlo pasa a verde y `pagosEnRevision` baja; un pago rechazado no cambia nada; anular el pago que dejaba al día vuelve a rojo
- [X] T022 [US2] Mostrar la línea «Tienes N pagos en revisión» en `frontend/src/privado/inicio/AvisoDeMora.tsx` cuando `pagosEnRevision > 0` (singular y plural), sin cambiar el color; ampliar `frontend/pruebas/pagos/AvisoDeMora.test.tsx` con ese caso

## Fase 6: Historia 3 — Cada jugador tiene su aviso y solo su familia lo ve (P1)

**Meta**: privacidad y un aviso por jugador.

**Prueba independiente**: con dos hermanos, uno en mora y otro al día, cada inicio muestra su aviso;
el PRESIDENTE, el DIRECTIVO y el ENTRENADOR no ven ninguno; otra familia no ve nada (quickstart pasos
10 y la tabla de autorización).

- [X] T023 [US3] Pruebas de autorización y aislamiento en `backend/pruebas/Integracion/Pagos/AvisoDeMoraPruebas.cs`: el propio jugador → 200; un hermano pidiendo el del otro → 404; otra familia → 404; jugador de otro club → 404; jugador retirado → 404; PRESIDENTE, DIRECTIVO y ENTRENADOR → 403 `rol_no_autorizado` (también el ENTRENADOR de la categoría del jugador); sin sesión → 401; dos hermanos, cada uno con su estado; cabecera `Cache-Control: private, no-store`
- [X] T024 [P] [US3] Ampliar `frontend/pruebas/pagos/AvisoDeMora.test.tsx`: el inicio del PRESIDENTE, el DIRECTIVO y el ENTRENADOR no pide ni muestra el aviso (aserción sobre la API simulada), y al cambiar de jugador el aviso cambia; ampliar `backend/pruebas/Integracion/Pagos/EntrenadorSinAccesoPruebas.cs` con la operación nueva

## Fase 7: Cierre

- [X] T025 Medir tamaños con `scripts/verificar-tamano.ps1` (ningún archivo escrito a mano por encima de 250 líneas) y correr el lint y la compilación del frontend y del backend sin advertencias nuevas
- [X] T026 Correr la batería: `dotnet test backend` (unitarias, integración y el guardián de esquema de la 014, que no debe cambiar) y `npm --prefix frontend test`; todo en verde
- [ ] T027 Recorrer `specs/016-aviso-de-mora/quickstart.md` de punta a punta contra la API real (pasos 1 a 12 y la tabla de autorización), con los dos temas (claro y oscuro) y en teléfono y escritorio; verificar que no llega nada a la campana ni al correo
- [ ] T028 Validación con el propietario del diseño del aviso (colores, textos, posición), que no está en el lienzo (plan, nota 3); anotar los cambios que pida
- [X] T029 Actualizar `specs/016-aviso-de-mora/spec.md` (estado «Implementada», fecha) y marcar el resultado en `checklists/requirements.md`

## Dependencias y orden de ejecución

- **Fase 1 → Fase 2 → resto.** La fase 2 bloquea a todas las historias.
- Dentro de la fase 2: T003 y T004 → T005; T006 y T007 en paralelo con T005; T008 depende de T005 a
  T007; T009 y T010 dependen de T008; T011 y T012 son independientes del backend; T013 al final.
- **Historia 4** (T014 a T016) solo necesita la fase 2; T014 y T015 pueden empezar en cuanto exista
  T005, antes que el resto.
- **Historia 1** (T017 a T020) necesita la fase 2 completa; T017 y T020 en paralelo con T018.
- **Historia 2** (T021 y T022) necesita la historia 1 (el componente) y el backend de la fase 2.
- **Historia 3** (T023 y T024) necesita la fase 2; sus pruebas de backend son independientes de las
  pantallas.
- **Cierre** al final, en orden: T025 → T026 → T027 → T028 → T029.

## Oportunidades de paralelo

- Fase 2: T003 con T004; T006 con T007; T011 con T012.
- Historia 4: T014 con T015.
- Historia 1: T017 con T018 (T018 usa el hook por su firma).
- Historia 3: T023 con T024.
- Una persona puede llevar el backend (T003 a T010, T014 a T016, T021, T023) y otra el frontend (T011,
  T012, T017 a T020, T022, T024).

## Estrategia de implementación

- **MVP**: fases 1 y 2 más las historias 4 y 1. Con eso la familia en mora ya ve el aviso rojo fijo y
  el verde cuando está al día (el paso a verde depende solo del cálculo, no de más código).
- **Incremento**: historia 2 añade el contador de pagos en revisión y la prueba de que los pagos mueven
  el color; historia 3 cierra la privacidad con pruebas.
- **Cierre**: validar el diseño con el propietario antes de dar la funcionalidad por terminada (T028).
- **Riesgos**: (1) que `ControladorCuentaJugador.cs` supere las 250 líneas, ver T010; (2) que el
  aviso parpadee un estado falso al cargar, ver T018; (3) que alguien calcule la mora en el frontend:
  prohibido por las convenciones.
