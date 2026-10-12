# Plan de implementación: Ficha del jugador

**Rama**: `005-ficha-jugador` | **Fecha**: 2026-10-09 | **Spec**: [spec.md](spec.md)

**Entrada**: especificación de la funcionalidad en `specs/005-ficha-jugador/spec.md`

## Resumen

Cada jugador aprobado de un club pasa a tener una ficha: la identidad y el contacto que ya dio al
registrarse, más el contacto de emergencia, la seguridad social, los datos clínicos y dos
documentos pedidos (copia del documento de identidad y certificado de afiliación a salud). La
familia la consulta y la mantiene desde "Mi ficha"; el club la abre desde las listas de jugadores
y cada rol ve lo que le corresponde: el PRESIDENTE todo, el ENTRENADOR de la categoría todo menos
los archivos, y el DIRECTIVO todo menos los datos clínicos. La familia puede cambiar el documento
de identidad; nombres, apellidos y fecha de nacimiento solo los corrige el PRESIDENTE, que además
puede cambiar cualquier otro dato. La ficha muestra quién la cambió por última vez y cuándo.

Enfoque técnico: dos tablas nuevas (`FichasJugador` y `DocumentosJugador`) que cuelgan del
integrante con borrado en cascada, y una migración. La identidad sigue en `UsuarioRol` y el celular
y el responsable en `Usuario`. Una sola regla de dominio, `ReglaAccesoAFicha`, decide qué ve y qué
cambia cada quien; los datos que alguien no puede ver no viajan en la respuesta. Seis endpoints
nuevos bajo `/api/clubes/{clubId}/jugadores/{usuarioRolId}/ficha`, que usa también la familia. Los
archivos se guardan en PostgreSQL, como el escudo y la foto, hasta 10 MB en PDF, JPEG, PNG o WebP.
Todo cambio se hace con el club bloqueado, el mecanismo de la 003. Las decisiones y sus
alternativas están en [research.md](research.md).

## Contexto técnico

**Lenguaje y versión**: C# 14 sobre .NET 10 en el backend; TypeScript 5 sobre Node 22 en el
frontend. Sin cambios respecto de la 001.

**Dependencias principales**: las de la 001 (ASP.NET Core 10, Entity Framework Core 10 con Npgsql,
JWT, React 19, React Router, Vite). No se añade ninguna.

**Almacenamiento**: PostgreSQL 17. Una migración, `FichaDelJugador`, con dos tablas nuevas. Los
archivos se guardan en la base de datos; no hay disco ni servicio de archivos.

**Pruebas**: xUnit (unitarias e integración con `WebApplicationFactory` y Testcontainers); Vitest
en el frontend.

**Plataforma de destino**: la de la 001: API en contenedor Linux y aplicación web adaptable.

**Tipo de proyecto**: aplicación web con `backend/` y `frontend/` separados (§4).

**Objetivos de rendimiento**: los de la 001. El estado de documentación de una lista se calcula con
una consulta por lista, no por jugador, y ninguna lista lee el contenido de un archivo.

**Restricciones**: código y documentación en español (§2.1, §3); ningún archivo escrito a mano por
encima de 250 líneas (§2.3); toda autorización y todo aislamiento en el servidor, dato por dato
(§15, RF-015); backend y pantalla de cada historia en la misma tarea (§27.1); los datos clínicos y
los archivos son de menores: no se escriben en el registro de la API y se sirven sin caché; no
romper nada de la 001 a la 004 (§27.2).

**Escala y alcance**: decenas de jugadores por club (§19), dos archivos de hasta 10 MB por jugador.
Esta funcionalidad tiene 2 entidades, 2 enumeraciones y 1 regla de dominio nuevas; 6 operaciones
de API nuevas y 4 que cambian; 1 pantalla nueva y 5 que cambian.

## Comprobación de la constitución

*Puerta: debe pasar antes de la fase 0 y se revisa de nuevo tras el diseño de la fase 1.*

Evaluada contra la versión **4.1.0**.

| Principio | Cómo lo cumple el plan | Resultado |
| --- | --- | --- |
| §2.1 y §2.2 Español y vocabulario | `FichaJugador`, `DocumentoJugador`, `ReglaAccesoAFicha`; Jugador, Entrenador, Directivo; "responsable" como campo, sin entidad Acudiente | Cumple |
| §2.3 Máximo 250 líneas | La ficha se reparte en cuatro servicios y dos controladores; los tipos del frontend van en `tiposFicha.ts` porque `tipos.ts` tiene 241 líneas | Cumple |
| §3 Documentación XML | En cada clase nueva, y se reescribe en las que cambian de responsabilidad (`UsuarioRol`, `Usuario`, `ControladorJugadores`, los dos DTO de listas, que hoy dicen "no lleva ningún otro dato") | Cumple |
| §4 y §5 Capas y dependencias | La regla en `Dominio/Reglas`; los controladores solo delegan; los repositorios no deciden permisos | Cumple |
| §6 Tecnologías base | Ninguna nueva; los archivos van a PostgreSQL | Cumple |
| §7.1 Aislamiento entre clubes | Las dos tablas implementan `IPerteneceAClub`; ninguna consulta nueva se salta el filtro | Cumple, con la nota 1 |
| §7.3 Varios clubes | Una ficha por jugador y club; celular y responsable, de la cuenta | Cumple |
| §7.4 Suspensión y baja | Lo resuelve el atributo de la 001, sin código nuevo | Cumple |
| §7.5 Aislamiento dentro del club | El entrenador, solo sus categorías, comprobado en cada petición; la cuenta de jugador, solo sus jugadores; conocer un identificador no da acceso | Cumple |
| §8 Roles | PRESIDENTE gestiona; DIRECTIVO consulta documentación y seguridad social, sin datos clínicos; ENTRENADOR consulta la ficha con datos médicos y de emergencia; JUGADOR actualiza contacto, médicos y documentos; DESARROLLADOR, nada | Cumple |
| §9 Sitio público | Ningún endpoint público nuevo ni cambiado | Cumple |
| §10 Documento | El cambio actualiza tipo y número sobre el mismo registro; único por club | Cumple |
| §11.2 y §12.3 Entidad Jugador | La categoría sigue en `UsuarioRol.CategoriaId`; la ficha cuelga del integrante | Desviación heredada (Seguimiento de complejidad) |
| §12.1 Ubicación por año | Corregir la fecha solo ubica a quien no tiene categoría, con el ubicador de la 003 | Cumple |
| §12.3 Alcance del JUGADOR | El acceso se resuelve por la cuenta del jugador (`UsuarioId`) | Cumple |
| §13 Histórico | El nombre de quien hizo el último cambio se copia; los nombres ya copiados en aprobaciones y retiros no se tocan | Cumple |
| §14 y §14.1 Desactivables y retiro | El retiro conserva ficha y archivos; solo se borran con el jugador o con el club | Cumple |
| §15 Reglas en backend | Permisos, validaciones y formatos se deciden en la API; la pantalla solo refleja `permisos` | Cumple |
| §18 Sin historial de estados | Solo el último cambio; ni valores anteriores ni versiones de archivos | Cumple |
| §19 Simplicidad | Dos tablas, cada una con su necesidad (research §1 y §7); sin almacenamiento externo, sin versionado, sin contadores guardados | Cumple |
| §20 Pruebas | Autorización por rol, aislamiento entre clubes, entre categorías y entre cuentas de jugador, alcance del DESARROLLADOR y endpoints públicos (research §12) | Cumple |
| §22 Migraciones | Una migración de Entity Framework; sin cambios manuales | Cumple |
| §23 API | HTTP semántico, DTOs, `problem+json`, Swagger; el DTO de la ficha omite lo que no se puede ver | Cumple |
| §24 Diseño visual | Componentes y temas existentes; estados con texto además de color | Cumple, con la nota 2 |
| §25 Decisiones no tomadas | Cinco detalles que la spec no fija quedan como supuestos en research.md, cada uno con la regla existente que aplica | Cumple, con supuestos por confirmar |
| §27 Definición de terminado | Tareas por historia, cada una con backend, pantalla y pruebas | Cumple |
| §28 Decisiones pendientes | No se resuelve ninguna; "Ficha con historial" sigue abierta (nota 3) | Cumple |

**Resultado de la puerta**: pasa, con la misma desviación de la letra de §11.2 que ya justificaron
la 003 y la 004.

**Nota 1**: `PUT …/ficha` escribe el celular y el responsable en `Usuario`, que no es un dato de
club, así que el cambio del PRESIDENTE de un club se ve en los demás clubes de esa persona. No es
una consulta sin filtro: se llega a la cuenta solo a través de un jugador del club de la petición y
solo se escriben esos dos campos. Lo decidió el propietario en la spec (RF-039).

**Nota 2**: no he podido abrir el lienzo de §24 desde esta sesión. La pantalla se compone con los
componentes ya aprobados; si el lienzo trae una pantalla de ficha, se contrasta con ella al
construir la historia 1.

**Nota 3**: §12.2 dice que la ficha de Jugador se elimina cuando la cuenta pasa a ENTRENADOR o
DIRECTIVO. Ese cambio de rol no existe todavía y la spec lo deja fuera; cuando se construya tendrá
que borrar las filas de estas dos tablas.

**Revisión tras el diseño**: el modelo de datos y el contrato no añaden excepciones al aislamiento,
tecnologías ni patrones respecto de lo evaluado, y ninguna respuesta entrega datos clínicos a un
DIRECTIVO ni archivos a un ENTRENADOR. La puerta sigue pasando.

## Estructura del proyecto

### Documentación de esta funcionalidad

```text
specs/005-ficha-jugador/
├── plan.md              # Este archivo
├── research.md          # Decisiones técnicas y supuestos del plan
├── data-model.md        # Las dos tablas nuevas y qué cambia en las existentes
├── quickstart.md        # Guía de validación de extremo a extremo
├── contracts/
│   └── api.yaml         # Contrato OpenAPI: seis endpoints nuevos y cuatro que cambian
└── tasks.md             # Lo genera /speckit-tasks
```

### Código fuente

Solo se listan los archivos nuevos (`+`) y los que cambian (`~`). La estructura es la de la 001.

```text
backend/src/
├── LFClub.Dominio/
│   ├── Entidades/
│   │   ├── + FichaJugador.cs
│   │   ├── + DocumentoJugador.cs
│   │   ├── ~ UsuarioRol.cs                  # solo documentación: identidad y documento ya cambian
│   │   └── ~ Usuario.cs                     # solo documentación: celular y responsable ya cambian
│   ├── Enumeraciones/
│   │   ├── + DocumentoPedido.cs
│   │   └── + GrupoSanguineo.cs
│   └── Reglas/
│       ├── + ReglaAccesoAFicha.cs           # quién ve y quién cambia
│       └── + AlcanceDeFicha.cs              # el resultado: cinco indicadores
├── LFClub.Aplicacion/
│   ├── DTOs/              + FichaJugadorDto, ContactoDeFichaDto, ContactoEmergenciaDto,
│   │                        SeguridadSocialDto, DatosClinicosDto, DocumentoDeFichaDto,
│   │                        UltimoCambioDto, PermisosDeFichaDto, ActualizarFichaDto,
│   │                        CambiarDocumentoIdentidadDto, CorregirIdentidadDto
│   │                      ~ JugadorDeCategoriaDto, JugadorRetiradoDto (ganan DocumentosPendientes),
│   │                        ClubDto (gana MiUsuarioRolId)
│   ├── Interfaces/        + IRepositorioFichas, IRepositorioDocumentosJugador
│   │                      ~ IRepositorioJugadores  # leer al jugador con su cuenta; cambiar identidad y documento
│   ├── Servicios/         + IServicioConsultaFicha, IServicioFichaJugador,
│   │                        IServicioIdentidadJugador, IServicioDocumentosJugador
│   ├── Implementaciones/  + ServicioConsultaFicha, ServicioFichaJugador,
│   │                        ServicioIdentidadJugador, ServicioDocumentosJugador
│   │                      ~ ServicioConsultaCategorias, ServicioConsultaClub
│   ├── Mappers/           + MapperFicha
│   │                      ~ MapperCategorias, MapperClub
│   ├── Validadores/       + ValidadorFicha, ValidadorIdentidad, ValidadorArchivoDeFicha
│   │                      ~ ValidadorRegistro (usa ValidadorIdentidad),
│   │                        ValidadorImagen (expone la detección de firmas)
│   └── Utilidades/        + AccesoAFicha           # lee al jugador y aplica la regla; 404 si no la ve
│                          + ErroresDeFicha
│                          ~ LectorDeCategorias     # añade el estado de documentación si corresponde
├── LFClub.Infraestructura/
│   ├── Datos/
│   │   ├── ~ ContextoLFClub.cs            # dos DbSet
│   │   ├── Configuraciones/  + ConfiguracionFichaJugador, ConfiguracionDocumentoJugador
│   │   └── Migraciones/      + FichaDelJugador (generada)
│   └── Repositorios/      + RepositorioFichas, RepositorioDocumentosJugador
│                          ~ RepositorioJugadores
└── LFClub.Api/
    ├── Configuracion/     ~ RegistroDeCasosDeUso, RegistroDeServicios
    └── Controladores/
        ├── ~ ArchivoCargado.cs              # segundo límite de petición, para los documentos
        └── Club/          + ControladorFichaJugador      # ficha, identidad y documento de identidad
                           + ControladorDocumentosJugador # abrir y subir archivos

backend/pruebas/
├── Unitarias/
│   ├── Reglas/            + ReglaAccesoAFichaPruebas
│   └── Validadores/       + ValidadorFichaPruebas, ValidadorIdentidadPruebas,
│                            ValidadorArchivoDeFichaPruebas
│                          ~ ValidadorRegistroPruebas (siguen en verde tras la extracción)
└── Integracion/
    ├── Base/              + SembradorFichas          # ficha y documentos sembrados; archivos de prueba
    ├── Ficha/             + EscenarioFicha
    │                      + MiFichaPruebas           # la familia lee y guarda
    │                      + FichaAjenaPruebas        # otra cuenta, retirado, identidad negada
    │                      + ConsultaPorRolPruebas    # presidente, directivo, entrenador
    │                      + AlcanceDelEntrenadorPruebas
    │                      + SoloLecturaPruebas       # directivo y entrenador no cambian nada
    │                      + DocumentosPruebas        # subir, abrir, reemplazar, rechazar
    │                      + AccesoADocumentosPruebas
    │                      + DocumentacionEnListasPruebas
    │                      + CambioDeDocumentoPruebas
    │                      + CorreccionDeIdentidadPruebas
    │                      + PresidenteCambiaFichaPruebas
    │                      + UltimoCambioPruebas
    │                      + FichaEntreClubesPruebas  # RF-003 y RF-039
    │                      + ConservacionPruebas      # retiro, rechazo, eliminar club
    │                      + FichaPorEstadoDelClubPruebas
    └── Aislamiento/       ~ AccesoClubPruebas        # 60 endpoints en los contratos

frontend/src/
├── ~ App.tsx                                 # ruta de la ficha
├── compartido/api/        + tiposFicha.ts
│                          ~ tipos.ts (ClubDto.miUsuarioRolId), tiposCategorias.ts
├── privado/
│   ├── ~ DisposicionClub.tsx                 # enlace "Mi ficha" para el JUGADOR
│   ├── ~ InicioClub.tsx                      # acceso a "Mi ficha"
│   ├── ficha/
│   │   ├── + FichaJugador.tsx                # la pantalla: carga y reparte en secciones
│   │   ├── + SeccionIdentidad.tsx
│   │   ├── + FormularioFicha.tsx             # contacto, emergencia, seguridad social y clínicos
│   │   ├── + SeccionDocumentos.tsx
│   │   ├── + DialogoCambiarDocumento.tsx
│   │   ├── + DialogoCorregirIdentidad.tsx
│   │   ├── + UltimoCambio.tsx
│   │   └── + textos.ts                       # nombres de documentos, grupos sanguíneos y estados
│   └── categorias/
│       ├── ~ SeccionJugadores.tsx            # nombre enlazado; columna "Documentación"
│       ├── ~ SeccionSinCategoria.tsx
│       ├── ~ SeccionRetirados.tsx
│       └── ~ textos.ts
└── pruebas/               + documentacion.test.ts
```

**Decisión de estructura**: la de la 001, sin carpetas ni proyectos nuevos salvo `privado/ficha/`
y `pruebas/Integracion/Ficha/`. La ficha se parte en cuatro servicios (consulta, contacto y salud,
identidad, documentos) porque cada uno tiene permisos y dependencias distintos y uno solo pasaría
de 250 líneas; los cuatro comparten `AccesoAFicha`, de modo que la comprobación de acceso está en
un único lugar.

### Pantallas

| Ruta | Qué cambia | Quién |
| --- | --- | --- |
| `/club/:clubId/jugadores/:usuarioRolId/ficha` | Nueva: la ficha, con las secciones y acciones que el rol permite | Los cuatro roles |
| Menú del club | Enlace "Mi ficha" | JUGADOR |
| `/club/:clubId` | Acceso a "Mi ficha" junto a "Mi categoría" | JUGADOR |
| `/club/:clubId/categorias/:categoriaId` → Jugadores | El nombre abre la ficha; columna "Documentación" | PRESIDENTE y DIRECTIVO; el ENTRENADOR, solo el enlace |
| `/club/:clubId/categorias` → Sin categoría | El nombre abre la ficha; columna "Documentación" | PRESIDENTE, DIRECTIVO |
| `/club/:clubId/categorias` → Retirados | El nombre abre la ficha; columna "Documentación" | PRESIDENTE, DIRECTIVO |

## Trazabilidad

| Historia | Requisitos | Backend | Pantallas | Pruebas clave |
| --- | --- | --- | --- | --- |
| 1. La familia consulta y mantiene la ficha | RF-001 a 005, 012, 013, 016, 017, 020, 033, 038, 039 | `FichaJugador`, `ReglaAccesoAFicha`, `AccesoAFicha`, `ServicioConsultaFicha`, `ServicioFichaJugador`, `ControladorFichaJugador` | Ficha, menú, inicio | Lee y guarda; todo opcional; responsable según la edad; identidad ignorada en el cuerpo y `403` en su operación; ficha ajena `404`; retirado `403`; último cambio |
| 2. El club consulta según el rol | RF-006 a 012, 014, 015, 019, 034 | La misma regla y los mismos servicios; `ServicioConsultaCategorias` | Ficha; enlaces en las tres listas | PRESIDENTE completo; ENTRENADOR sin `documentos` y `404` fuera de su categoría; DIRECTIVO sin `datosClinicos`, también asignado; cuatro operaciones de cambio `403`; otro club y DESARROLLADOR `404` |
| 3. La familia entrega la documentación | RF-027 a 032, 038 | `DocumentoJugador`, `ValidadorArchivoDeFicha`, `ServicioDocumentosJugador`, `ControladorDocumentosJugador`, `LectorDeCategorias` | Apartado "Documentos"; columna "Documentación" | Subir, abrir, reemplazar; rechazos que conservan el anterior; ENTRENADOR `403`; otra cuenta `404`; estado en las tres listas; el ENTRENADOR no lo recibe |
| 4. Documento e identidad | RF-018, 021 a 026 | `ServicioIdentidadJugador`, `ValidadorIdentidad`, `UbicadorDeJugadores` | Diálogos de documento e identidad | Mismo jugador, misma categoría; entra con el número nuevo; repetido `409`; fecha con y sin categoría, futura; el PRESIDENTE cambia todo |
| Transversal | RF-003, 035 a 037 | Filtro global, cascadas, estado del club | Todas, a 360 px y en los dos temas | Dos clubes, dos fichas; celular común; retiro conserva; rechazo y eliminación del club borran; el contrato pasa a 60 endpoints |

## Orden de construcción sugerido

Para `/speckit-tasks`. Cada bloque deja algo que se puede probar de extremo a extremo (§27.1).

1. **Base**: las dos entidades, las dos enumeraciones, sus configuraciones y la migración;
   `ReglaAccesoAFicha` con sus pruebas unitarias; `AccesoAFicha`; `ClubDto.miUsuarioRolId`.
2. **Historia 1**: leer y guardar la ficha (contacto, emergencia, seguridad social, clínicos) con
   su último cambio; la pantalla de la ficha, "Mi ficha" en el menú y en el inicio.
3. **Historia 2**: la consulta por rol sobre los mismos endpoints (omisión de grupos, alcance del
   entrenador) y los enlaces desde las tres listas. En el backend es casi todo pruebas: la regla
   ya decide desde el bloque 1.
4. **Historia 3**: subir, abrir y reemplazar archivos; el apartado "Documentos"; el estado de
   documentación en las tres listas.
5. **Historia 4**: cambio del documento de identidad; corrección de la identidad con la
   extracción de `ValidadorIdentidad` y la ubicación por fecha; los dos diálogos.
6. **Cierre**: entre clubes, conservación y borrado, estados del club, revisión a 360 px en los
   dos temas, contrato frente a Swagger, documentación XML de las clases que cambiaron de
   responsabilidad y recorrido del quickstart.

Las historias 1 y 2 son el mínimo utilizable. La 2 no debe publicarse sin la regla completa: el
endpoint de lectura nace ya en la historia 1 limitado por `ReglaAccesoAFicha`, para que en ningún
momento exista una versión que entregue datos clínicos a un DIRECTIVO.

## Cambios sobre lo ya construido en la 001, la 003 y la 004

Son los puntos donde hay que cuidar la no regresión (§27.2):

| Qué cambia | Por qué | Cómo se protege |
| --- | --- | --- |
| `JugadorDeCategoriaDto` y `JugadorRetiradoDto` ganan `documentosPendientes` | RF-032 | Las pruebas de la 003 no comprueban la ausencia de campos; prueba nueva de que un ENTRENADOR no lo recibe. Se actualizan a la vez el DTO, `tiposCategorias.ts` y las tres secciones |
| El detalle de una categoría depende de quién pregunta | El ENTRENADOR no debe recibir el estado de documentación (RF-007) | `LectorDeCategorias` recibe si se incluye; las operaciones de PRESIDENTE lo incluyen siempre |
| `ClubDto` gana `miUsuarioRolId` | "Mi ficha" usa la ruta con identificador | Campo añadido; las pruebas de la 001 siguen en verde |
| `ValidadorRegistro` delega la identidad en `ValidadorIdentidad` | La ficha valida nombres, documento y fecha con las mismas reglas | `ValidadorRegistroPruebas` no cambia y debe seguir en verde |
| `ValidadorImagen` expone la detección de firmas | La reutiliza `ValidadorArchivoDeFicha` | `ValidadorImagenPruebas` no cambia; el límite de 1 MB del escudo y la foto se conserva |
| Nombres, apellidos, fecha de nacimiento y documento de un integrante dejan de ser inmutables | RF-016, RF-018 | Pruebas de que la categoría, los equipos, el estado de ingreso y el retiro no cambian; las de inicio de sesión de la 001 siguen en verde |
| `Usuario.Celular` y `NombreResponsable` se pueden cambiar tras el registro | RF-016, RF-039 | Prueba entre clubes; el registro y la aceptación de la 004 no cambian |
| Los nombres de las listas pasan a ser enlaces | RF-034 | Recorrido del quickstart; la protección real es la del servidor |
| `AccesoClubPruebas` espera 60 endpoints en lugar de 54 | Seis endpoints nuevos | El contrato de la 005 se añade en la misma tarea que el primer endpoint; los cuatro que cambian ya existían y la prueba no los cuenta dos veces. El parámetro `{documento}` no lleva restricción de ruta, para que las pruebas genéricas sigan recibiendo `401` y `404` |

## Seguimiento de complejidad

| Desviación | Por qué hace falta | Alternativa más simple descartada |
| --- | --- | --- |
| §11.2 y §12.3 escriben `Jugador.CategoriaId` y `Jugador.UsuarioId`; el jugador sigue siendo el `UsuarioRol` con rol JUGADOR, y `FichaJugador` cuelga de él | Heredada de la 003 y la 004. Ahora que existe la ficha, mover identidad, categoría, equipos y retiro a una entidad `Jugador` obligaría a reescribir los repositorios, las reglas y las pruebas de la 003 y la 004 sin cambiar nada de lo que ve el usuario | Dejarlo como está **es** la opción simple. La alternativa fiel a la letra (entidad `Jugador` completa) se descarta por §19 y §27.2; si el propietario la quiere, es una spec de refactorización propia |

## Consecuencias que conviene tener presentes

- **Persona en dos clubes y cambio de documento**: el documento se cambia club por club. Mientras
  otro club conserve el número anterior, ese número sigue sirviendo para iniciar sesión.
- **Tamaño de la base de datos**: los archivos viven en PostgreSQL. A la escala actual no es un
  problema; si un club llegara a cientos de jugadores convendría revisarlo.
- **Datos sensibles de menores**: la ficha guarda datos clínicos y copias de documentos de
  identidad. Este plan los protege con autorización, aislamiento y respuestas sin caché; no añade
  cifrado propio de columnas, que la constitución no pide.

## Supuestos del plan

Detallados al final de [research.md](research.md). Conviene que el propietario los confirme antes
de `/speckit-tasks` o durante `/speckit-analyze`:

1. No se admite un número de documento que ya usa otra cuenta en otro club, como en el registro.
2. Corregir la fecha de nacimiento no exige responsable aunque el jugador pase a ser menor; se
   pide al guardar el contacto.
3. Cuando el último cambio lo hizo la familia, la ficha dice "la cuenta del jugador".
4. El último cambio se registra solo en la ficha desde la que se hizo, aunque el celular o el
   responsable cambien también en otras fichas de la cuenta.
5. Cada archivo admite hasta 10 MB, en PDF, JPEG, PNG o WebP.
