---

description: "Lista de tareas de la funcionalidad 010: ficha editable por la familia, resultado publicado por la familia y carta del jugador"
---

# Tareas: Ficha editable por la familia, resultado publicado por la familia y carta del jugador

**Entrada**: documentos de diseño en `specs/010-ficha-resultado-y-carta/`

**Requisitos previos**: plan.md, spec.md, research.md, data-model.md, contracts/api.yaml,
contracts/carta.md, quickstart.md

**Pruebas**: incluidas. Las exigen la constitución (§20, versión 4.8.0) y el plan (tabla de
trazabilidad). Por §27.1, cada tarea de una historia entrega el backend, la pantalla que lo usa
contra la API real y sus pruebas; las tareas que solo traen pruebas o solo pantalla lo dicen.

**Organización**: las tareas se agrupan por historia de usuario en orden de prioridad: historia 1
(P1), historias 2 y 3 (P2), historia 4 (P3), y después el cierre. La historia 2 no depende de
ninguna otra y el plan sugiere hacerla primero por ser la más pequeña: se puede adelantar sin
cambiar nada (ver "Estrategia de implementación").

## Formato: `[ID] [P?] [Historia] Descripción`

- **[P]**: puede hacerse en paralelo (archivos distintos, sin depender de tareas sin terminar)
- **[US1]…[US4]**: historia de usuario de spec.md a la que pertenece la tarea
- Cada tarea indica las rutas exactas de sus archivos, relativas a la raíz del repositorio

## Convenciones para todas las tareas

Son las de los `tasks.md` de la 001 a la 009, que siguen vigentes. Resumen y lo que se añade:

- **Esquema** (data-model.md): una tabla nueva (`CartasJugador`), cinco columnas en `Partidos`,
  cuatro en `PartidosJugador` y una sola migración, `FichaResultadoYCarta` (T005). Si una tarea
  posterior parece necesitar otra tabla, columna o índice, se detiene y se informa (§25).
- **Contrato**: `specs/010-ficha-resultado-y-carta/contracts/api.yaml` manda en rutas, cuerpos,
  códigos de estado y códigos de error. `AccesoClubPruebas` lee los contratos de todas las specs y
  `EndpointsDeLaApi` ya agrupa las rutas repetidas, así que las tres marcadas "CAMBIA" no se
  cuentan dos veces. T001 deja comentadas las 5 operaciones nuevas y cada tarea que construye una
  la descomenta y sube los contadores de
  `backend/pruebas/Integracion/Aislamiento/AccesoClubPruebas.cs` (contrato y endpoints de club).
  Recorrido: 94/70 → 95/71 (T013) → 96/72 (T017) → 97/73 (T018) → 98/74 (T019) → 99/75 (T022).
- **Lo anterior cambia solo donde la spec lo dice** (§27.2): la spec cambia RF-016 y RF-017 de la
  005 (la familia corrige la identidad) y RF-015 y RF-016 de la 009 (la familia publica el
  resultado). Las pruebas de la 005 y la 009 que afirman lo contrario se ajustan en la tarea que
  hace el cambio (T011, T013, T015) y la tarea lo dice. Cualquier otra prueba de la 001 a la 009
  que falle y no esté claro si es una regresión: se detiene y se pregunta.
- **La familia nunca ve a los demás** (RF-005, §17.2): una cuenta de jugador nunca recibe
  convocados, quiénes jugaron, lo indicado por otras familias, quién publicó el resultado ni la
  suma de goles de los demás. Esos campos se omiten del JSON (`JsonIgnore` cuando es nulo), no van
  vacíos. Las pruebas lo comprueban sobre el **texto** de la respuesta. "Convocado" y "su jugador"
  se evalúan con el **integrante de la petición** (cabecera `X-Jugador-Elegido` de la 006).
- **Quien entrena** (research §8): cualquier integrante con una `AsignacionEntrenadorCategoria`
  activa, sea ENTRENADOR, DIRECTIVO o PRESIDENTE, leída en cada petición con
  `IRepositorioAsignaciones.CategoriasDeAsync`. En el resultado, corregir exige ser PRESIDENTE o
  entrenar **todas** las categorías del partido (`ReglaGestionDeActividad` de la 009, sin cambios).
  En la carta, cuenta la **categoría activa del jugador**.
- **Vale el primero** (research §3): toda escritura del resultado, de la familia o de quien
  entrena, toma `IRepositorioClub.BloquearAsync` dentro de su transacción y vuelve a leer el
  partido y sus convocados **después** de bloquear.
- **La carta se ve por la ficha** (RF-020): `AccesoACarta` usa `AccesoAFicha` sin cambiarlo; quien
  no ve la ficha recibe el mismo `404 no_encontrado` que un identificador inventado.
- **Nunca como clasificación** (RF-025, CE-007): ningún endpoint ni pantalla entrega, ordena ni
  compara estadísticas o valoraciones de carta de más de un jugador.
- **Hora de Colombia**: "ya empezó" y la temporada se calculan con `HoraDeColombia` y
  `ReglaMomentoDeActividad` de la 009; en las pruebas se mueve el "ahora" con `RelojDePrueba`.
- **Lo copiado no cambia** (§13): `PublicadoPorNombre`, `IndicadoPorNombre`,
  `EstadisticasCambiadasPorNombre` y `DorsalCambiadoPorNombre` se copian en el momento; cuando
  escribe la familia, el nombre copiado es el del jugador (como `UltimoCambioDto` de la 005).
- **Nombres** (§2.1, §2.2, RF-035): todo en español; "familia" solo en textos de pantalla, el rol
  sigue siendo JUGADOR. Los nombres de personas usan `NombrePersona` con el `EnlaceFichaDto` de
  `ResolutorDeEnlaces` (008).
- **Documentación** (§3): toda clase nueva lleva documentación XML (o comentario de componente en
  el frontend) con qué representa, su responsabilidad y qué no hace; se reescribe la de las clases
  que cambian según el plan (§3 de su tabla).
- **Tamaño** (§2.3): ningún archivo escrito a mano supera las 250 líneas. Vigilar
  `ServicioIdentidadJugador.cs` (175), `AccesoAActividad.cs` (166), `RepositorioConvocatoria.cs`
  (139), `FormularioResultado.tsx` (135) y `tiposActividades.ts` (202): si una tarea los llevaría
  por encima, lo nuevo va a un archivo propio (por ejemplo `tiposResultadoFamilia.ts`). El
  resultado de la familia va en `ServicioResultadoFamilia`, no en `ServicioResultado` (122).
- **Flujo** (§5): Controlador → `IServicio` → `IRepositorio` → EF Core. Controladores con
  `[IntegranteDelClub]` que solo delegan; reglas en `Dominio/Reglas`, puras; SkiaSharp solo en
  `LaPecosa.Infraestructura`, detrás de `IGeneradorImagenCarta`.
- **Aislamiento** (§7.1): todo bajo `/api/clubes/{clubId}` con el filtro de club activo;
  `CartaJugador` implementa `IPerteneceAClub`.
- **Pantallas** (RF-034, CE-008, §24): componentes `Tarjeta`, `Aviso`, `Boton`, `Campo`; legibles a
  360 px y en los dos temas, sin desplazamiento horizontal; todo estado escrito con texto ("Falta
  el resultado", "Publicado por una familia", "Corregido por quien entrena", "Registrado por quien
  entrena", "Sin indicar", "Bronce", "Plata", "Oro").
- **Terminado** (§27.2): una tarea se marca hecha cuando compila, pasan
  `dotnet test backend/LaPecosa.sln` y `npm --prefix frontend test`, `tsc` y `eslint` no dan
  errores y pasa `scripts/verificar-tamano.ps1`.

## Supuestos del plan que estas tareas aplican

Aprobados por el propietario el 2026-10-11 (research §12). Si cambia alguno, cambia solo la regla
indicada, no el modelo.

| # | Supuesto | Dónde |
| --- | --- | --- |
| 1 | Quien entrena "corrige" una fila solo si cambia `Jugo` o `Goles`, o la marca como que jugó sin indicación previa; si la guarda igual, conserva su origen | `ReglaCorreccionDeIndicacion` (T007), T015 |
| 2 | La carta de un jugador retirado no se edita: `409 jugador_retirado`; el PRESIDENTE la sigue viendo y exportando | `ReglaAccesoACarta` (T008), T018, T019, T022 |
| 3 | Un marcador repetido también se rechaza con `409 marcador_ya_publicado`, aunque sea igual | `ReglaResultadoDeFamilia` (T007), T013 |
| 4 | La carta no muestra el porcentaje de asistencia, solo la rareza | T017, T021 |

---

## Fase 1: Preparación (infraestructura compartida)

**Propósito**: que la batería siga en verde con el contrato de la 010 ya en `specs/`, y que la
dependencia nueva compile en el contenedor antes de que nada la use.

- [X] T001 Comentar en `specs/010-ficha-resultado-y-carta/contracts/api.yaml` las 5 operaciones nuevas, cada una con una marca `# 010-T0NN` que diga qué tarea la descomenta: `PUT /partidos/{partidoId}/resultado/mi-jugador` (T013); `GET /jugadores/{usuarioRolId}/carta` (T017); `PUT …/carta/dorsal-y-posicion` (T018); `PUT …/carta/estadisticas` (T019); `GET …/carta/imagen` (T022). Las tres marcadas "CAMBIA" (`GET /partidos/{partidoId}`, `GET …/jugadores` del partido y `PUT …/ficha/identidad`) quedan sin comentar porque ya existen. Comprobar que `backend/pruebas/Integracion/Aislamiento/AccesoClubPruebas.cs` sigue en 94 y 70 y que la batería entera pasa
- [X] T002 [P] Dependencia y fuentes de la imagen (research §11). En `backend/src/LaPecosa.Infraestructura/LaPecosa.Infraestructura.csproj`, añadir `SkiaSharp` y `SkiaSharp.NativeAssets.Linux.NoDependencies` (licencia MIT, versión estable fijada, la misma para los dos). Añadir `backend/src/LaPecosa.Infraestructura/Imagenes/Fuentes/BarlowCondensed-Bold.ttf` y `BarlowCondensed-ExtraBold.ttf` como `EmbeddedResource`, con `OFL.txt` (SIL Open Font License) junto a ellas. Comprobar que `backend/Dockerfile` construye la imagen `mcr.microsoft.com/dotnet/aspnet:10.0` sin paquetes del sistema y que la batería sigue en verde. No se usa todavía en ningún archivo de código

---

## Fase 2: Cimientos (requisitos que bloquean todas las historias)

**Propósito**: esquema, reglas puras con sus pruebas unitarias, errores, tipos del frontend y
ayudas de prueba. Al terminar, la migración se aplica y nada cambia para nadie.

**⚠️ CRÍTICO**: ninguna historia empieza antes de terminar esta fase.

- [X] T003 [P] Crear las tres enumeraciones en `backend/src/LaPecosa.Dominio/Enumeraciones/`, serializadas como texto como `Rol`, con documentación XML: `OrigenResultado.cs` (`FAMILIA`, `GESTION`; "`GESTION` es quien entrena las categorías del partido o el PRESIDENTE"), `PosicionJugador.cs` (`PORTERO`, `DEFENSA`, `MEDIOCAMPISTA`, `DELANTERO`) y `RarezaCarta.cs` (`BRONCE`, `PLATA`, `ORO`; "no se guarda: se calcula")
- [X] T004 Entidades (depende de T003). Nueva `backend/src/LaPecosa.Dominio/Entidades/CartaJugador.cs`, `IPerteneceAClub`, con documentación XML: `UsuarioRolId` (clave y foránea al jugador: "una carta por jugador y club"), `ClubId`, `Dorsal` (`int?`, "de 1 a 99. Puede repetirse entre jugadores"), `Posicion` (`PosicionJugador?`), `Ritmo`, `Tiro`, `Pase`, `Regate`, `Defensa`, `Fisico` (`int?`, "de 1 a 99. Las seis nulas o las seis con valor"), `EstadisticasCambiadasEn`, `EstadisticasCambiadasPorUsuarioId`, `EstadisticasCambiadasPorNombre` ("texto ≤ 161"), `DorsalCambiadoEn`, `DorsalCambiadoPorUsuarioId`, `DorsalCambiadoPorNombre` ("texto ≤ 161"). En `Partido.cs`: `OrigenResultado?`, `PublicadoEn` (`DateTime?`), `PublicadoPorUsuarioId` (`Guid?`), `PublicadoPorNombre` ("texto ≤ 161"), `PublicadoPorFamilia` (`bool`), y la documentación dice que también lo publica una familia (§17.3) y que `ResultadoCambiado…` sigue siendo el último cambio. En `PartidoJugador.cs`: `IndicadoPor` (`OrigenResultado?`, "nulo: nadie indicó nada"), `IndicadoEn`, `IndicadoPorUsuarioId`, `IndicadoPorNombre` ("texto ≤ 161"), con la documentación de quién indica y quién corrige. En `UsuarioRol.cs`, solo documentación: de él cuelga su carta
- [X] T005 Configuraciones, `DbSet` y migración (depende de T004). Nueva `backend/src/LaPecosa.Infraestructura/Datos/Configuraciones/ConfiguracionCartaJugador.cs` (tabla `CartasJugador`, clave `UsuarioRolId` con foránea en cascada a `UsuariosRol`, `ClubId` obligatorio con foránea a `Clubes`, `Posicion` como texto, nombres con `HasMaxLength(161)`, cuentas con foránea a `Usuarios` y `OnDelete(SetNull)`, restricciones de comprobación: `Dorsal` y cada estadística entre 1 y 99; las seis estadísticas todas nulas o todas con valor; cada sello —`EstadisticasCambiadasEn`/`…PorNombre` y `DorsalCambiadoEn`/`…PorNombre`— completo o vacío). En las configuraciones existentes de `Partido` y `PartidoJugador` en la misma carpeta: enumeraciones como texto, `HasMaxLength(161)`, foráneas a `Usuarios` con `SetNull`, y las restricciones "`OrigenResultado`, `PublicadoEn` y `PublicadoPorNombre` tienen valor si y solo si `Estado = JUGADO`" y "`IndicadoPor`, `IndicadoEn` e `IndicadoPorNombre` todos nulos o todos con valor". `DbSet<CartaJugador> CartasJugador` en `backend/src/LaPecosa.Infraestructura/Datos/ContextoLaPecosa.cs` con el filtro de club. Migración `FichaResultadoYCarta` en `backend/src/LaPecosa.Infraestructura/Datos/Migraciones/` (generada; sin datos que migrar). Comprobar que se aplica al arrancar y que la batería sigue en verde
- [X] T006 [P] Errores de la funcionalidad. En `backend/src/LaPecosa.Aplicacion/Utilidades/ErroresDeActividades.cs`, con título en español: `falta_marcador` (400, "Escribe el marcador del partido."), `marcador_ya_publicado` (409, "El marcador ya fue publicado: solo puedes indicar lo de tu jugador."), `indicacion_corregida` (409, "Quien entrena ya corrigió lo de tu jugador."). Nuevo `backend/src/LaPecosa.Aplicacion/Utilidades/ErroresDeCarta.cs`, como `ErroresDeFicha`: `no_entrena_categoria` (403, "Solo quien entrena la categoría del jugador o el PRESIDENTE puede cambiar esto.") y `jugador_retirado` (409, "Este jugador está retirado del club: su carta ya no se cambia."). `goles_superan_marcador` y `goles_de_quien_no_jugo` se reutilizan tal cual: su texto no nombra a nadie (RF-006)
- [X] T007 [P] Reglas del resultado de la familia, en `backend/src/LaPecosa.Dominio/Reglas/`, puras y con documentación XML. `ReglaResultadoDeFamilia.Validar(estado, yaEmpezo, golesClubActual, golesRivalActual, golesClubEnviado, golesRivalEnviado, origenDeLaFila, jugo, goles, golesDeLosDemas)` devuelve el primer problema, en este orden (data-model "Validación del resultado de la familia"): `PartidoCancelado` (CANCELADO), `PartidoSinEmpezar` (PROXIMO y no empezó), `FaltaMarcador` (sin marcador y no se envía, o se envía solo uno de los dos), `MarcadorYaPublicado` (con marcador y se envía, aunque sea igual: supuesto 3), `IndicacionCorregida` (fila con `GESTION`), `DatosFueraDeRango` (goles o marcador fuera de 0..99), `GolesDeQuienNoJugo` (no jugó y goles > 0), `GolesSuperanMarcador` (goles + goles de los demás > goles del club, el actual o el enviado). `ReglaCorreccionDeIndicacion.OrigenNuevo(origenAntes, jugoAntes, golesAntes, jugoDespues, golesDespues)`: `GESTION` si cambian `Jugo` o `Goles`, o si queda como que jugó sin indicación previa; si no, el origen de antes (supuesto 1, research §2). Pruebas: `backend/pruebas/Unitarias/Reglas/ReglaResultadoDeFamiliaPruebas.cs` (cada código, el orden entre ellos, borde 0 y 99, suma exactamente igual al marcador → válido, uno más → `GolesSuperanMarcador`) y `backend/pruebas/Unitarias/Reglas/ReglaCorreccionDeIndicacionPruebas.cs` (fila de familia igual → `FAMILIA`; fila sin indicar que sigue sin jugar → nulo; sin indicar marcada como que jugó → `GESTION`; familia con goles cambiados → `GESTION`; `GESTION` igual → `GESTION`)
- [X] T008 [P] Reglas de acceso a la carta, en `backend/src/LaPecosa.Dominio/Reglas/`, puras: `AlcanceDeCarta.cs` (registro con `CambiaEstadisticas`, `CambiaDorsalYPosicion`, `Exporta`) y `ReglaAccesoACarta.Alcance(Rol rol, bool esSuPropioJugador, bool entrenaSuCategoriaActiva, bool jugadorActivo)` según la tabla de research §8: PRESIDENTE → estadísticas y dorsal si el jugador está activo, exporta siempre (también retirado); ENTRENADOR o DIRECTIVO que entrenan su categoría activa → estadísticas y dorsal, no exporta; DIRECTIVO no asignado → nada; JUGADOR sobre su propio jugador → dorsal y exporta, nunca estadísticas. Documentación: "no decide quién ve la carta: eso es `AccesoAFicha`". Pruebas: `backend/pruebas/Unitarias/Reglas/ReglaAccesoACartaPruebas.cs`, fila por fila de la tabla, y el retirado (PRESIDENTE: solo exporta)
- [X] T009 [P] Reglas de cálculo de la carta, en `backend/src/LaPecosa.Dominio/Reglas/`, puras: `ReglaEstadisticasDeCarta.Validar(ritmo, tiro, pase, regate, defensa, fisico)` (las seis nulas → válido y "sin estadísticas"; las seis de 1 a 99 → válido; mezcladas o fuera de rango → inválido con el campo); `ReglaValoracionDeCarta.Calcular(...)` (promedio de las seis con `Math.Round(…, MidpointRounding.AwayFromZero)`, como `CalculoDeAsistencia`; sin estadísticas → nulo); `ReglaRarezaDeCarta.Calcular(int registrados, int? porcentaje)` (sin registros → `BRONCE`; porcentaje ya redondeado: < 60 `BRONCE`, 60..84 `PLATA`, ≥ 85 `ORO`). Pruebas en `backend/pruebas/Unitarias/Reglas/`: `ReglaEstadisticasDeCartaPruebas.cs` (0, 100, cinco de seis, todas nulas, 1 y 99), `ReglaValoracionDeCartaPruebas.cs` (70, 65, 80, 72, 60, 88 → 73 por el 72,5; todas 99 → 99; sin estadísticas → nulo) y `ReglaRarezaDeCartaPruebas.cs` (sin registros, 59, 60, 84, 85, y 84,5 % que redondea a 85 → `ORO` usando `CalculoDeAsistencia.Porcentaje`)
- [X] T010 [P] Tipos del frontend: `frontend/src/compartido/api/tiposCarta.ts` con `PosicionJugador`, `RarezaCarta`, `ValoresCartaDto`, `EstadisticasCartaDto`, `DorsalYPosicionDto`, `PermisosDeCartaDto` y `CartaDto` según `contracts/api.yaml` (opcionales los que el contrato omite); en `frontend/src/compartido/api/tiposActividades.ts` (o en `tiposResultadoFamilia.ts` si pasa de 250 líneas), `OrigenResultado`, `ResultadoDeFamiliaDto`, `MiParticipacionDto`, `PublicacionResultadoDto`, `IndicacionDto`, y `indicaResultado` en `PermisosDeActividadDto`, `miParticipacion`, `publicacion` y `origenResultado` opcionales en `PartidoDto` e `indicacion` opcional en el convocado de la planilla
- [X] T011 Ayudas de prueba (depende de T005). En `backend/pruebas/Integracion/Base/SembradorActividades.cs`: `PublicarPorFamiliaAsync(partido, jugador, golesClub?, golesRival?, jugo, goles)` que escribe como lo haría el servicio (para preparar escenarios sin pasar por la API), `ConvocarAsync` si falta para añadir convocados a un partido existente y un parámetro de hora en `CrearPartidoAsync` si no lo tiene (para "ya empezó" con `RelojDePrueba`). Nuevo `backend/pruebas/Integracion/Base/SembradorCartas.cs`: `PonerColoresYEscudoAsync(club, principal, acento)` (con un escudo PNG pequeño), `PonerFotoAsync(jugador)` (JPEG pequeño), `PonerCartaAsync(jugador, dorsal?, posicion?, estadisticas?)`. Ninguna prueba existente cambia

**Punto de control**: la migración se aplica, las reglas puras pasan sus pruebas y la batería de
la 001 a la 009 sigue en verde con 94/70.

---

## Fase 3: Historia 1 - La familia publica el resultado del partido de su hijo (Prioridad: P1) 🎯 MVP

**Objetivo**: en un partido que ya empezó, la familia de un convocado publica el marcador e indica
lo de su jugador; vale el primer marcador; las demás familias indican lo de sus hijos; quien
entrena o el PRESIDENTE corrigen todo y ven quién publicó y quién indicó cada cosa. Nadie de la
familia ve a los demás convocados.

**Prueba independiente**: partido X de ayer con J1, J3, J4 y J5 convocados: J3 publica 2-1 con un
gol; J4 no puede cambiar el marcador e indica lo suyo; E1 corrige a 3-1 y el gol de J4; se
comprueba en cada paso lo que ven las familias, un no convocado y las estadísticas (quickstart,
bloque 1).

- [X] T012 [US1] Pruebas primero de lo que la familia nunca recibe y de "vale el primero" (solo pruebas, que fallan hasta T013 y T014). `backend/pruebas/Integracion/Partidos/VisibilidadResultadoFamiliaPruebas.cs`: como J3 convocado a X ya empezado, el texto de `GET /partidos/{X}`, de la respuesta de `PUT …/resultado/mi-jugador` y de `GET /partidos` no contiene `convocados`, `jugo` de otro, `indicacion`, `publicacion`, `origenResultado` ni el nombre de J4 o J5 (salvo como goleador público una vez jugado); `GET /partidos/{X}/jugadores` → 403 como en la 009; J9 y D2 ven X jugado con marcador y goleadores sin `publicacion` ni `origenResultado` (escenario 1.11, CE-001); el motivo de `goles_superan_marcador` no contiene el nombre de ningún otro goleador (RF-006). `backend/pruebas/Integracion/Partidos/PrimerMarcadorPruebas.cs`: J3 y J4 envían a la vez (`Task.WhenAll`, varias repeticiones) marcadores distintos → exactamente una 200 y una `409 marcador_ya_publicado`, y el marcador guardado es el de la 200 (caso límite 1, CE-002); dos familias que suman goles a la vez no dejan la suma por encima del marcador; una familia y E1 a la vez dejan el partido coherente
- [X] T013 [US1] Publicar e indicar por la familia, de punta a punta. Backend: `ResultadoDeFamiliaDto` (`golesClub?`, `golesRival?`, `jugo`, `goles`) y `MiParticipacionDto` (`jugo`, `goles`, `indicadoPor?`, `puedeCambiar = indicadoPor != GESTION`) en `backend/src/LaPecosa.Aplicacion/DTOs/`; en `IRepositorioConvocatoria` y `RepositorioConvocatoria`, leer la fila del jugador de la petición, la suma de goles de los **demás** convocados y guardar la indicación (`Jugo`, `Goles`, `IndicadoPor = FAMILIA`, `IndicadoEn`, `IndicadoPorUsuarioId`, `IndicadoPorNombre` = nombre del jugador); `IServicioResultadoFamilia` en `backend/src/LaPecosa.Aplicacion/Servicios/` y `ServicioResultadoFamilia` en `backend/src/LaPecosa.Aplicacion/Implementaciones/`: solo JUGADOR (los demás, `403 rol_no_autorizado`); el partido debe verse y el jugador de la petición estar convocado, si no `404 no_encontrado` también si está jugado (escenario 1.3); transacción con `IRepositorioClub.BloquearAsync` y relectura; `ReglaResultadoDeFamilia` con sus códigos; si no tenía marcador, lo pone, pasa a `JUGADO` con `ReglaTransicionPartido`, `OrigenResultado = FAMILIA`, `PublicadoEn/PorUsuarioId/PorNombre`, `PublicadoPorFamilia = true` y `ResultadoCambiado…` (último cambio); la respuesta es el `PartidoDto` como lo ve la familia. En `PartidoDto`, `miParticipacion` (`JsonIgnore` si es nulo) y en `PermisosDeActividadDto`, `indicaResultado`; `ServicioConsultaPartidos`, `MapperPartidos` y `AccesoAActividad` los calculan solo para la cuenta de un convocado a un partido ya empezado y no cancelado (`indicaResultado` falso en entrenamientos y si su fila es `GESTION`). `ControladorResultadoFamilia` en `backend/src/LaPecosa.Api/Controladores/Club/ControladorResultadoFamilia.cs` con `PUT api/clubes/{clubId:guid}/partidos/{partidoId:guid}/resultado/mi-jugador`; registrar en `RegistroDeCasosDeUso`; descomentar la operación y subir los contadores a 95 y 71. Pantalla: `frontend/src/privado/partidos/SeccionResultadoFamilia.tsx` en `DetallePartido.tsx`, solo con `permisos.indicaResultado` o `miParticipacion`: sin marcador, goles del club y del rival, "¿Jugó?" (Sí/No) y goles de su jugador (deshabilitados con "No"); con marcador, el marcador publicado sin poder cambiarlo y el formulario de su jugador con lo que ya indicó; con `puedeCambiar` falso, el texto "Quien entrena corrigió lo de tu jugador" sin formulario; ante `409 marcador_ya_publicado` recarga el detalle y muestra el marcador ganador; mensajes de los 400 y 409 en `frontend/src/privado/partidos/textos.ts`. Pruebas: `backend/pruebas/Integracion/Partidos/ResultadoFamiliaPruebas.cs` — Y de mañana → `409 partido_sin_empezar` y sin `indicaResultado` (escenario 1.2); J6 no convocado → 404 en X sin jugar y en X jugado (1.3); `goles_de_quien_no_jugo` (1.7); J3 publica 2-1, jugó, 1 gol → `JUGADO` y las estadísticas de J3 cuentan el partido y el gol al instante (1.1, RF-010); J4 envía marcador → `409 marcador_ya_publicado`, también el mismo 2-1 (1.4, supuesto 3); J4 con 2 goles → `400 goles_superan_marcador`, con 1 → 200 (1.6); J5 sin indicar no cuenta como que jugó (RF-011); J3 cambia lo suyo a jugó y 0 goles → 200 (1.12); Z cancelado → `409 actividad_cancelada`; cuenta de J1 y J2 con J2 elegido (no convocado) → 404; ENTRENADOR, DIRECTIVO y PRESIDENTE → 403 `rol_no_autorizado`; PB → 404. `frontend/pruebas/SeccionResultadoFamilia.test.tsx`: los tres estados del formulario y que nunca pinta a otro convocado. Si alguna prueba de `ResultadoPruebas.cs` de la 009 afirmaba que una cuenta de jugador no puede publicar, se ajusta aquí (la spec cambia RF-015 y RF-016 de la 009) y la tarea lo dice
- [X] T014 [US1] Corrección por fila y publicación para quien entrena, de punta a punta (sin endpoints nuevos; depende de T013). Backend: en `ServicioResultado`, la escritura de quien entrena toma `IRepositorioClub.BloquearAsync` y relee; `OrigenResultado = GESTION` en cada registro o corrección y no vuelve a `FAMILIA`; si el partido no tenía marcador, `PublicadoEn/PorUsuarioId/PorNombre` con `PublicadoPorFamilia = false`; si ya tenía, la primera publicación no cambia; por cada fila, `ReglaCorreccionDeIndicacion` decide el origen nuevo y, si pasa a `GESTION`, sella `IndicadoEn/PorUsuarioId/PorNombre` con quien corrige; `ReglaResultadoDePartido` de la 009 sin cambios (bajar el marcador por debajo de la suma sin ajustar goles → `400 goles_superan_marcador`). `PublicacionResultadoDto` (`fecha`, `autor`, `porFamilia`) e `IndicacionDto` (`origen`, `autor`, `fecha`) en `backend/src/LaPecosa.Aplicacion/DTOs/`; `PartidoDto` suma `publicacion` y `origenResultado` solo en un `JUGADO` y para quien ve la planilla; el convocado de `PlanillaPartidoDto.cs` suma `indicacion`, ausente si nadie indicó nada; en `MapperPartidos` y `ServicioConsultaPartidos`. Pantalla: en `frontend/src/privado/partidos/SeccionResultado.tsx`, el estado por texto ("Falta el resultado", "Publicado por una familia", "Corregido por quien entrena" si `porFamilia` y `GESTION`, "Registrado por quien entrena" si no) y "Publicado por {autor}, {fechaYHora}" con `NombrePersona`; en `FormularioResultado.tsx`, en cada fila, "Indicado por {autor}, {fechaYHora}" o "Sin indicar"; textos en `textos.ts`. Pruebas: `backend/pruebas/Integracion/Partidos/CorreccionPorFilaPruebas.cs` — tras J3 (2-1, 1 gol) y J4 (1 gol), E1 abre X: `publicacion` con el nombre de J3 y `porFamilia`, `origenResultado` `FAMILIA`, cada fila con su `indicacion` y J5 sin ella (1.8, RF-009); E1 corrige a 3-1 y 2 goles a J4 → `origenResultado` `GESTION`, fila de J4 `GESTION`, fila de J3 sigue `FAMILIA` y J3 todavía puede cambiar lo suyo, J4 → `409 indicacion_corregida` (1.10, 1.12, supuesto 1); goleadores y estadísticas reflejan la corrección al instante (1.9, CE-003); E1 baja a 1-1 sin quitar goles → 400 (caso límite 2); E1 marca a J5 como que jugó → J5 `GESTION`; P puede hacer lo mismo; E1 registra sin familia → `porFamilia` falso. En `backend/pruebas/Integracion/Partidos/ResultadoPruebas.cs`, sumar que registrar deja `OrigenResultado = GESTION` y `publicacion`

**Punto de control**: quickstart, bloque 1, completo. Las estadísticas se llenan el día del partido
sin trabajo del entrenador.

---

## Fase 4: Historia 2 - La familia cambia cualquier dato de la ficha de su hijo (Prioridad: P2)

**Objetivo**: la familia corrige nombres, apellidos y fecha de nacimiento de su propio jugador con
el formulario y el endpoint que ya usa el PRESIDENTE, sin que cambie la categoría.

**Prueba independiente**: J3 (2014) corrige un apellido y su fecha a un día de 2013; sigue en la
2014; P, E1 y los goleadores ven el apellido nuevo; otra familia, E1 y D1 no pueden hacerlo
(quickstart, bloque 2).

- [X] T015 [US2] La familia corrige la identidad, de punta a punta (sin endpoints nuevos). Backend: en `backend/src/LaPecosa.Dominio/Reglas/ReglaAccesoAFicha.cs` y `AlcanceDeFicha.cs`, `CorrigeIdentidad` también para JUGADOR sobre su propio jugador (DIRECTIVO y ENTRENADOR siguen sin él, RF-018), con la documentación reescrita; en `ControladorFichaJugador`, `PUT …/ficha/identidad` admite el rol JUGADOR; en `ServicioIdentidadJugador.CorregirAsync`, el `UbicadorDeJugadores` solo se llama cuando corrige el PRESIDENTE: cuando corrige la familia, la categoría no se toca, tenga o no (RF-015); validaciones (nombres y apellidos no vacíos, fecha no futura, RF-014), sello del último cambio (`porLaCuentaDelJugador` verdadero) y documento sin cambios (RF-016, RF-017). Si el archivo pasa de 250 líneas, la decisión de ubicar sale a un método o clase propia. Pantalla: en `frontend/src/privado/ficha/SeccionIdentidad.tsx` y `FichaJugador.tsx`, el botón "Corregir nombre y fecha" se muestra por `permisos.corrigeIdentidad`, también a la familia; para la familia no aparece el aviso de ubicación en categoría que ve el PRESIDENTE (si existe); el último cambio dice "la familia" como en la 005. Pruebas: en `backend/pruebas/Unitarias/Reglas/ReglaAccesoAFichaPruebas.cs`, el JUGADOR sobre su jugador corrige la identidad y sobre otro no ve la ficha; en `backend/pruebas/Integracion/Ficha/CorreccionDeIdentidadPruebas.cs` — J3 corrige apellido y fecha a 2013 → 200 y sigue en la 2014 (2.1, 2.2, CE-004); un jugador sin categoría cambia su fecha a un año con categoría → sigue sin categoría (2.3); fecha futura o nombres vacíos → 400 con el motivo (2.4); cuenta de J1 y J2: cambiar J1 no toca J2, y con J1 elegido, `PUT` sobre J2 → 404 (2.5); E1 y D1 → 403 `rol_no_autorizado`, otra familia → 404 (2.6); el último cambio muestra fecha y nombre (2.7); P, E1 y los goleadores ven el apellido nuevo; el PRESIDENTE sigue ubicando a uno sin categoría (regla de la 005 intacta). Las pruebas de la 005 que afirmaban `403` para la cuenta del jugador se cambian aquí, porque la spec cambia RF-016 y RF-017 de la 005, y la tarea lo dice

**Punto de control**: quickstart, bloque 2, completo.

---

## Fase 5: Historia 3 - La carta del jugador (Prioridad: P2)

**Objetivo**: cada jugador tiene una carta con foto, nombre, dorsal, posición, escudo, degradado
del club, seis estadísticas, valoración y rareza por asistencia; la ven los que ven la ficha; quien
entrena su categoría y el PRESIDENTE ponen las estadísticas; dorsal y posición también la familia.

**Prueba independiente**: E1 pone las seis estadísticas de J3; la familia, P y E1 ven la carta con
valoración 73; J4, E1 sobre K1 y PB reciben 404; una carta sin estadísticas se ve sin números
(quickstart, bloque 3).

- [X] T016 [P] [US3] Plano de la carta en el frontend (solo pantalla y pruebas, depende de T010). `frontend/src/privado/carta/plano.ts` con las medidas de `contracts/carta.md` como porcentajes del lienzo 750 × 1050 (capas, posiciones, tamaños de letra, colores de rareza `#B0793F`, `#BFC5CC`, `#D9A62E`), la elección del color del texto (blanco o `#141414` según el contraste con el color medio del degradado, con la luminancia de `frontend/src/compartido/tema/contraste.ts`) y si lleva sombra (contraste con algún extremo < 4,5:1), el degradado sin acento (principal y su versión 35 % más oscura) y sin colores (neutros del tema claro), las abreviaturas `POR`, `DEF`, `MED`, `DEL` y el ajuste del nombre (de 64 a 36 px, luego dos líneas con "…"). `frontend/src/privado/carta/textos.ts` con los textos de la carta. Pruebas: `frontend/pruebas/plano.test.ts` — color del texto con colores oscuros, claros y dos amarillos claros (sombra); degradado sin acento; abreviaturas; un nombre de 60 caracteres no pasa de dos líneas
- [X] T017 [US3] Consultar la carta, de punta a punta (depende de T008, T009, T011 y T016). Backend: `CartaDto`, `ValoresCartaDto` y `PermisosDeCartaDto` en `backend/src/LaPecosa.Aplicacion/DTOs/` (lo que no hay se omite; `ultimoCambioEstadisticas` con el `UltimoCambioDto` de la 005 solo si `cambiaEstadisticas`, RF-028); `IRepositorioCartas` en `backend/src/LaPecosa.Aplicacion/Interfaces/` y `RepositorioCartas` en `backend/src/LaPecosa.Infraestructura/Repositorios/` (leer por `UsuarioRolId`); en `IRepositorioEstadisticas` y `RepositorioEstadisticas`, una consulta nueva de la asistencia de un jugador entre dos fechas (registrados y presentes de entrenamientos no cancelados, sobre el índice por `UsuarioRolId`), sin tocar la de la 009; `AccesoACarta` en `backend/src/LaPecosa.Aplicacion/Utilidades/AccesoACarta.cs`: ve la carta si `AccesoAFicha` ve la ficha (si no, 404), y calcula `entrenaSuCategoriaActiva` con `IRepositorioAsignaciones.CategoriasDeAsync` para `ReglaAccesoACarta`; `IServicioConsultaCarta` y `ServicioConsultaCarta`: temporada = año de hoy en hora de Colombia (1 de enero a 31 de diciembre), porcentaje con `CalculoDeAsistencia`, `ReglaRarezaDeCarta` y `ReglaValoracionDeCarta`; `fotoActualizadaEn` de la foto del jugador (spec 007), ausente si no tiene; `MapperCarta` en `backend/src/LaPecosa.Aplicacion/Mappers/MapperCarta.cs`; `ControladorCarta` en `backend/src/LaPecosa.Api/Controladores/Club/ControladorCarta.cs` con `GET api/clubes/{clubId:guid}/jugadores/{usuarioRolId:guid}/carta` y `Cache-Control: private, no-store`; registrar; descomentar y subir a 96 y 72. Pantalla: ruta `jugadores/:usuarioRolId/carta` en `frontend/src/App.tsx`; `frontend/src/privado/carta/PantallaCarta.tsx` (carga la carta y la identidad del club, muestra la carta, la rareza escrita, y deja sitio para las acciones de T018, T019 y T021); `CartaJugador.tsx` dibuja con HTML y CSS siguiendo `plano.ts` (escudo y colores de `IdentidadClub`, foto del endpoint de la 007, valoración y siglas con "–" sin estadísticas, sin porcentaje de asistencia, supuesto 4); `FiguraGenerica.tsx` (silueta SVG del color del texto al 30 %); `frontend/src/compartido/tema/carta.css` (ancho máximo 320 px en teléfono y 360 px en escritorio, proporción 5:7, Barlow Condensed 700 y 800 que ya carga la aplicación); en `frontend/src/privado/ficha/FichaJugador.tsx`, el enlace "Ver carta". Pruebas: `backend/pruebas/Integracion/Carta/ConsultaCartaPruebas.cs` — J3 (su familia), P, E1, E2, D1 y D2 la ven (3.1); sin fila → sin `dorsal`, `posicion`, `estadisticas` ni `valoracion` en el texto, sin error (3.4); J4 sin foto → sin `fotoActualizadaEn` (3.6); J4 sobre J3, E1 sobre K1, PB y el DESARROLLADOR → 404 igual que un identificador inventado (3.7); `permisos` rol por rol según research §8; `ultimoCambioEstadisticas` solo para quien las cambia; tras cambiar P colores y escudo, la carta de pantalla los toma de la identidad (3.9, en la prueba se comprueba que `CartaDto` no los lleva); respuesta con `Cache-Control: private, no-store`. `backend/pruebas/Integracion/Carta/RarezaPruebas.cs` — sin registros → `BRONCE`; 10 entrenamientos de este año con 5, 6 y 9 presentes → `BRONCE`, `PLATA`, `ORO`; 20 con 17 presentes (85 %) → `ORO`; uno cancelado no cuenta; uno del año pasado no cuenta; corregir una asistencia cambia la rareza y no las estadísticas (3.10, 3.11, RF-024)
- [X] T018 [US3] Dorsal y posición, de punta a punta (depende de T017). Backend: `DorsalYPosicionDto` (`dorsal?` "entero de 1 a 99 o nulo", `posicion?` de la lista o nula) en `backend/src/LaPecosa.Aplicacion/DTOs/`; en `IRepositorioCartas`/`RepositorioCartas`, crear la fila con el primer cambio y guardar; `IServicioCarta` y `ServicioCarta.CambiarDorsalYPosicionAsync`: quien no ve la carta → 404; `CambiaDorsalYPosicion` falso → `403 no_entrena_categoria`; jugador retirado → `409 jugador_retirado` (supuesto 2); fuera de rango o posición desconocida → `400 datos_invalidos`; si nada cambió, ni fila nueva ni sello movido; sello `DorsalCambiadoEn/PorUsuarioId/PorNombre`; responde la `CartaDto`. En `ControladorCarta`, `PUT …/carta/dorsal-y-posicion`; descomentar y subir a 97 y 73. Pantalla: `frontend/src/privado/carta/FormularioDorsalYPosicion.tsx` en `PantallaCarta.tsx`, solo con `permisos.cambiaDorsalYPosicion` (dorsal numérico 1–99 opcional, posición con las cuatro opciones escritas y "Sin posición"), mensajes de error. Pruebas: `backend/pruebas/Integracion/Carta/DorsalYPosicionPruebas.cs` — J3 pone 10 y `DELANTERO` → se ve en la carta; E2 cambia a 7 → vale el 7 (3.12); J5 también puede tener el 7; P y D1 pueden; D2 (ve la carta) → `403 no_entrena_categoria`; E1 sobre K1 (no la ve) → 404; 0, 100 y `"LIBERO"` → 400; retirado → 409 (con P); dos guardados iguales no mueven el sello
- [X] T019 [US3] Estadísticas de la carta, de punta a punta (depende de T018, mismo servicio y controlador). Backend: `EstadisticasCartaDto` (seis `int?`, "las seis enteras de 1 a 99, o las seis nulas") en `backend/src/LaPecosa.Aplicacion/DTOs/`; `ServicioCarta.CambiarEstadisticasAsync`: quien no ve la carta → 404; JUGADOR → `403 rol_no_autorizado`; `CambiaEstadisticas` falso → `403 no_entrena_categoria`; retirado → `409 jugador_retirado`; `ReglaEstadisticasDeCarta` → `400 datos_invalidos` con el campo (un decimal como 50,5 lo rechaza la lectura del cuerpo con el mismo código); las seis nulas borran (vuelve a "sin números"); sello `EstadisticasCambiadas…`; responde la `CartaDto` con `valoracion`. En `ControladorCarta`, `PUT …/carta/estadisticas`; descomentar y subir a 98 y 74. Pantalla: `frontend/src/privado/carta/FormularioEstadisticas.tsx` en `PantallaCarta.tsx`, solo con `permisos.cambiaEstadisticas` (seis campos numéricos con su sigla RIT, TIR, PAS, REG, DEF, FÍS, "Quitar estadísticas", último cambio "{autor}, {fechaYHora}"), mensajes de error. Pruebas: `backend/pruebas/Integracion/Carta/EstadisticasCartaPruebas.cs` — E1 pone 70, 65, 80, 72, 60, 88 → valoración 73 y último cambio de E1 (3.2, CE-006); 0, 100, 50,5 y cinco de seis → 400 (3.3); D1 puede, D2 → `403 no_entrena_categoria`, J3 → `403 rol_no_autorizado`, E1 sobre K1 → 404 (3.5); seis nulas → la carta vuelve sin números; J3 pasa a la 2015: conserva sus estadísticas, E1 ya no las cambia y E2 sí (caso límite 7); se desactiva la asignación de E1: deja de ver la carta y lo que puso se conserva (caso límite 8); retirado → 409; las estadísticas de partidos y entrenamientos de J3 no cambian (RF-024)
- [X] T020 [US3] Nunca como clasificación (solo pruebas, depende de T019). `backend/pruebas/Integracion/Carta/SinClasificacionPruebas.cs`: recorre los esquemas de respuesta de todos los `specs/*/contracts/api.yaml` y de Swagger, y ningún esquema de lista (array) contiene `valoracion`, `estadisticas`, `ritmo`, `tiro`, `pase`, `regate`, `defensa`, `fisico` ni `rareza`; con cartas puestas a varios jugadores, el texto de `GET /jugadores`, `GET /goleadores`, la planilla del partido y la lista de la categoría no contiene ninguno de esos campos (3.8, RF-025, CE-007)

**Punto de control**: quickstart, bloque 3, completo, salvo la revisión visual de los puntos 10 y
11, que va en T025.

---

## Fase 6: Historia 4 - Exportar la carta como imagen (Prioridad: P3)

**Objetivo**: la familia y el PRESIDENTE descargan la carta como PNG de 750 × 1050 dibujado por la
API con el mismo plano que la pantalla; nadie más la obtiene, ni pidiéndola directamente.

**Prueba independiente**: J3 y P descargan la carta y coincide con la de pantalla; E1 y D1 no ven
el botón y reciben 403 al pedir la imagen (quickstart, bloque 4).

- [X] T021 [US4] Generador de la imagen (depende de T002, T009 y T017; sin endpoint todavía). `IGeneradorImagenCarta` en `backend/src/LaPecosa.Aplicacion/Interfaces/IGeneradorImagenCarta.cs` (recibe los datos de la carta ya calculados: nombres, apellidos, dorsal, posición, estadísticas, valoración, rareza, bytes de la foto o nulo, bytes del escudo o nulo, color principal y de acento o nulos; devuelve los bytes del PNG). En `backend/src/LaPecosa.Infraestructura/Imagenes/`: `PlanoCarta.cs` (las medidas y colores de `contracts/carta.md` en unidades del lienzo, los mismos números que `plano.ts`), `ContrasteDeColor.cs` (luminancia y contraste WCAG como `contraste.ts`, color del texto y sombra) y `GeneradorImagenCarta.cs` con SkiaSharp: lienzo 750 × 1050 con esquinas de radio 48 y fuera transparente, degradado, marco de 18 del color de la rareza, valoración, posición, escudo ajustado sin deformar, foto recortada al centro o figura genérica, dorsal, nombre en mayúsculas de 64 a 36 px y luego dos líneas con "…", línea, estadísticas en dos columnas (siglas con "–" sin números), rareza escrita; fuentes Barlow Condensed leídas del recurso incrustado; lee PNG, JPEG y WebP. Nada de documento, fecha de nacimiento, contacto, salud, categoría, porcentaje ni fecha de exportación (RF-030). Registrar en `RegistroDeServicios`. Pruebas: `backend/pruebas/Unitarias/Utilidades/ContrasteDeColorPruebas.cs` (los mismos casos que `plano.test.ts`, con los mismos resultados) y una prueba de integración del generador en `backend/pruebas/Integracion/Carta/GeneradorImagenCartaPruebas.cs` (PNG válido de 750 × 1050; con y sin foto, escudo, colores y estadísticas; nombre de 60 caracteres sin excepción; esquina transparente; el color del marco en un píxel del borde es el de la rareza)
- [X] T022 [US4] Exportar, de punta a punta (depende de T021). Backend: `IServicioImagenCarta` y `ServicioImagenCarta`: quien no ve la carta → 404; DIRECTIVO y ENTRENADOR → `403 rol_no_autorizado` aunque la vean (RF-031); `Exporta` falso → 403; el PRESIDENTE exporta también la de un retirado (4.2), leyendo el jugador sin el filtro de activos solo para este caso; lee foto (la del retirado ya se borró: figura genérica), escudo y colores del club y llama a `IGeneradorImagenCarta`. En `ControladorCarta`, `GET …/carta/imagen` con `image/png`, `Content-Disposition: attachment; filename="carta-<apellidos>-<nombres>.png"` (sin tildes ni espacios, con `NormalizadorTexto`) y `Cache-Control: private, no-store`; descomentar y subir a 99 y 75. Pantalla: `frontend/src/privado/carta/BotonDescargarCarta.tsx` en `PantallaCarta.tsx`, solo con `permisos.exporta`: pide la imagen con la sesión, la guarda con el nombre de la cabecera, estado "Preparando la imagen…" y mensaje si falla. Pruebas: `backend/pruebas/Integracion/Carta/ExportarCartaPruebas.cs` — J3 sobre J3 y P sobre J4 → 200, `image/png`, 750 × 1050, cabeceras del contrato (4.1, 4.2); P sobre un retirado → 200 (4.2); E1 y D1 → 403 `rol_no_autorizado` (4.3); J4 sobre J3, PB y el DESARROLLADOR → 404; la familia de un retirado no llega al club (4.4, §14.1); la cadena de bytes del PNG no contiene el documento, el correo ni el celular del jugador (RF-030)
- [X] T023 [US4] Comprobación en el contenedor Linux (solo verificación). Construir `backend/Dockerfile` y levantar la API en un proyecto de Compose aparte (`docker compose -p la-pecosa-validacion …` con `Brevo__Llave` vacía); como P, `GET …/carta/imagen` de un jugador con foto, escudo y colores → PNG correcto, con las fuentes Barlow Condensed (no la de reserva) y sin errores de bibliotecas nativas en el registro. Bajar el proyecto al terminar

**Punto de control**: quickstart, bloque 4, completo.

---

## Fase 7: Cierre y aspectos transversales

**Propósito**: contrato, aislamiento, pantallas, documentación, validación de extremo a extremo y
batería.

- [X] T024 Comprobar que no queda ninguna marca `# 010-T0NN` ni operación comentada en `specs/010-ficha-resultado-y-carta/contracts/api.yaml`, que Swagger expone las 99 operaciones del contrato y que `AccesoClubPruebas` pasa con 99 y 75 (las 5 rutas nuevas responden 404 al integrante de otro club y al DESARROLLADOR, y 401 sin sesión) (RF-032)
- [X] T025 [P] Revisión de pantallas a 360 px y en escritorio, en tema claro y oscuro (RF-034, CE-008): `SeccionResultadoFamilia` en sus tres estados, `SeccionResultado` y `FormularioResultado` con la indicación por fila, la ficha con "Ver carta" y la corrección por la familia, la carta con y sin foto, con y sin estadísticas, con un nombre muy largo, con colores del club oscuros, claros, sin acento y dos amarillos claros, y sus tres formularios. Comparar una imagen descargada con la carta en pantalla (mismos datos, mismo orden, mismos colores, RF-030). Corregir lo que falle en `frontend/src/compartido/tema/carta.css` y los componentes, y en `PlanoCarta.cs` si la diferencia es de la imagen
- [X] T026 [P] Revisar la documentación XML y los comentarios de componente de todo lo nuevo y de las clases que cambian según el plan (`Partido`, `PartidoJugador`, `UsuarioRol`, `ReglaAccesoAFicha`, `AlcanceDeFicha`, `ServicioIdentidadJugador`, `ServicioResultado`, `RepositorioConvocatoria`, `ControladorFichaJugador`), y pasar `scripts/verificar-tamano.ps1` (ningún archivo escrito a mano por encima de 250 líneas)
- [X] T027 Recorrer `specs/010-ficha-resultado-y-carta/quickstart.md` entero en un proyecto de Compose aparte (`docker compose -p la-pecosa-validacion -f docker-compose.yml -f <override>` con `Brevo__Llave` vacía, para no enviar correos reales con la llave del `.env`), con un contenedor mínimo en marcha si Docker se duerme, y limpiar al final (`down -v` y borrar el contenedor mínimo). Anotar cualquier diferencia con la spec y detenerse a preguntar si alguna no es un error claro
- [X] T028 Batería completa: `dotnet test backend/LaPecosa.sln`, `npm --prefix frontend test`, `tsc`, `npm --prefix frontend run lint`, `npm --prefix frontend run build` y `./scripts/verificar-tamano.ps1`, con las pruebas de la 001 a la 009 en verde

---

## Dependencias y orden de ejecución

### Entre fases

- **Preparación (T001, T002)**: sin dependencias; T002 en paralelo con T001.
- **Cimientos (T003 a T011)**: después de T001. Bloquean todas las historias.
- **Historia 1 (T012 a T014)**: después de los cimientos. No depende de otras historias.
- **Historia 2 (T015)**: después de los cimientos. No depende de otras historias; se puede hacer
  antes que la historia 1.
- **Historia 3 (T016 a T020)**: después de los cimientos. No depende de las historias 1 y 2.
- **Historia 4 (T021 a T023)**: después de T002 y de T017 (necesita la carta y su acceso).
- **Cierre (T024 a T028)**: después de todo lo anterior.

### Entre tareas

- T003 → T004 → T005 → T011 (enumeraciones, entidades, esquema, ayudas de prueba).
- T006 y T007 → T013, T014. T008 y T009 → T017. T010 → T013, T016.
- T012 se escribe antes de T013 y queda en rojo hasta T014 (la parte de una familia y E1 a la vez).
- T013 → T014 (mismo `PartidoDto`, `MapperPartidos`, `ServicioConsultaPartidos` y detalle).
- T016 → T017 → T018 → T019 → T020 (mismo plano, servicio, controlador y pantalla).
- T002 y T017 → T021 → T022 → T023.
- Los contadores de `AccesoClubPruebas` suben en el orden T013, T017, T018, T019, T022. Si se
  cambia el orden, cada tarea sube los suyos sobre lo que haya.

### Dentro de cada tarea

- Backend (DTO → interfaz → repositorio → regla o utilidad → servicio → controlador → registro →
  contrato), luego pantalla contra la API real, luego pruebas. La tarea no se marca hecha sin sus
  pruebas en verde.

---

## Ejemplos de trabajo en paralelo

```text
# Preparación
T001  Contrato comentado
T002  SkiaSharp y fuentes en Infraestructura

# Cimientos, tras T001
T003  Enumeraciones              (luego T004 → T005 → T011)
T006  Errores
T007  ReglaResultadoDeFamilia y ReglaCorreccionDeIndicacion
T008  ReglaAccesoACarta y AlcanceDeCarta
T009  Estadísticas, valoración y rareza
T010  tiposCarta.ts y tipos del resultado de la familia

# Historias en paralelo tras los cimientos (archivos distintos)
T015  La familia corrige la identidad          (ficha)
T012  Pruebas primero del resultado de la familia (partidos)
T016  plano.ts y plano.test.ts                  (carta)

# Cierre
T025  Revisión a 360 px y en los dos temas
T026  Documentación y tamaño
```

---

## Estrategia de implementación

### Primero lo mínimo

1. Fase 1 y fase 2.
2. Historia 1 (T012 a T014).
3. **Parar y validar**: quickstart, bloque 1. Las familias publican el resultado y las
   estadísticas se llenan solas.

### Entrega incremental

1. Cimientos → la migración se aplica y nada cambia para nadie.
2. Historia 2 (si se adelanta, como sugiere el plan) → la familia corrige la identidad.
3. Historia 1 → resultado por la familia. **No se publica T013 sin T014**: sin la corrección por
   fila, quien entrena no ve quién publicó ni puede respetar lo indicado por las familias.
4. Historia 3 → la carta en pantalla, con dorsal, posición, estadísticas y rareza.
5. Historia 4 → descarga de la imagen, comprobada en el contenedor Linux.
6. Cierre → contrato, aislamiento, 360 px, quickstart y batería.

Cada paso deja la batería en verde y no rompe lo anterior.

---

## Notas

- La migración de esta funcionalidad es `FichaResultadoYCarta`. La aplicación está en
  construcción: no hay datos que migrar.
- Ninguna tarea modifica la constitución: la 4.8.0 ya recoge todo lo que aquí se construye.
- La única dependencia nueva es SkiaSharp (con sus activos nativos de Linux), y solo en
  `LaPecosa.Infraestructura` (plan, "Seguimiento de complejidad").
- La carta no está en el lienzo (plan, nota 3): su diseño sigue `contracts/carta.md` y lo revisa el
  propietario en T025 y T027.
- Si una prueba de la 001 a la 009 falla al tocar `ServicioResultado`, `ServicioConsultaPartidos`,
  `MapperPartidos`, `AccesoAActividad`, `ReglaAccesoAFicha`, `ServicioIdentidadJugador` o
  `FichaJugador.tsx` y no es una de las que la spec cambia a propósito (T013, T015), se detiene esa
  parte y se pregunta (§25).
