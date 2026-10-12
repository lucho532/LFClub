# Plan de implementación: ficha editable por la familia, resultado publicado por la familia y carta del jugador

**Rama**: `010-ficha-resultado-y-carta` | **Fecha**: 2026-10-11 | **Spec**: [spec.md](spec.md)

**Entrada**: especificación de la funcionalidad en `specs/010-ficha-resultado-y-carta/spec.md`

## Resumen

La familia de un convocado publica, en un partido que ya empezó, el marcador y lo de su propio
jugador (si jugó y sus goles), sin ver a los demás convocados. Vale el primer marcador; las demás
familias indican lo de sus hijos, y quien entrena o el PRESIDENTE corrigen todo después. La familia
corrige también los nombres, los apellidos y la fecha de nacimiento de su jugador, sin que cambie la
categoría. Cada jugador tiene una carta al estilo de los videojuegos de fútbol, con foto, nombre,
dorsal, posición, escudo, degradado con los colores del club, seis estadísticas, valoración y
rareza por asistencia; la exportan como imagen solo su familia y el PRESIDENTE.

Enfoque técnico: **una tabla nueva** (`CartasJugador`), columnas nuevas en `Partidos` y
`PartidosJugador` y **una migración** (`FichaResultadoYCarta`). El resultado de la familia va por un
endpoint propio con una regla pura (`ReglaResultadoDeFamilia`) y el bloqueo del club para que "vale
el primero" y la suma de goles aguanten publicaciones simultáneas. La familia corrige la identidad
con el endpoint que ya usa el PRESIDENTE, con un permiso más. La carta reutiliza la visibilidad de
la ficha (`AccesoAFicha`) y añade una regla pura para sus permisos; valoración y rareza se calculan
al consultar. La imagen la **dibuja la API** con SkiaSharp para que la barrera de la exportación sea
real (§15); pantalla e imagen siguen el mismo plano. 5 endpoints nuevos y 3 respuestas que suman
campos. Las decisiones y sus alternativas están en [research.md](research.md).

## Contexto técnico

**Lenguaje y versión**: C# 14 sobre .NET 10 en el backend; TypeScript 5 sobre Node 22 en el
frontend. Sin cambios respecto de la 001.

**Dependencias principales**: las de la 001 (ASP.NET Core 10, Entity Framework Core 10 con Npgsql,
JWT, React 19, React Router, Vite). **Se añade una**: SkiaSharp (MIT) con
`SkiaSharp.NativeAssets.Linux.NoDependencies` para el contenedor, solo en `LFClub.Infraestructura`
(research §11). Se incluye la fuente Barlow Condensed (OFL) como recurso. El frontend no añade
ninguna.

**Almacenamiento**: PostgreSQL 17. Una migración con una tabla, ocho columnas en `Partidos` y
`PartidosJugador` y sus restricciones de comprobación ([data-model.md](data-model.md)).

**Pruebas**: xUnit (unitarias e integración con `WebApplicationFactory` y Testcontainers, con el
`IReloj` sustituido para mover el "ahora"); Vitest en el frontend.

**Plataforma de destino**: la de la 001: API en contenedor Linux y aplicación web adaptable. La
imagen de la API (`mcr.microsoft.com/dotnet/aspnet:10.0`) no necesita paquetes del sistema con los
activos nativos "NoDependencies".

**Tipo de proyecto**: aplicación web con `backend/` y `frontend/` separados (§4).

**Objetivos de rendimiento**: la carta son tres consultas por clave (jugador con acceso, carta y
conteo de asistencias de la temporada sobre el índice por `UsuarioRolId`). La imagen se dibuja en
memoria en cada petición (750 × 1050), sin caché: se pide pocas veces y lleva datos de un menor.

**Restricciones**: código y documentación en español (§2.1, §3); ningún archivo escrito a mano por
encima de 250 líneas (§2.3); toda regla en el servidor (§15, RF-033); backend y pantalla de cada
historia en la misma tarea (§27.1); una cuenta de jugador nunca recibe convocados, quiénes jugaron
ni lo indicado por otras familias (RF-005); ningún endpoint entrega estadísticas de carta de más de
un jugador (RF-025).

**Escala y alcance**: decenas de jugadores y partidos por club (§19). 1 entidad nueva, 2 que
cambian y 3 enumeraciones nuevas; 5 operaciones de API (el contrato pasa de 94 a 99; las de club,
de 70 a 75); unos 10 archivos de frontend nuevos y unos 8 que cambian.

## Comprobación de la constitución

*Puerta: debe pasar antes de la fase 0 y se revisa de nuevo tras el diseño de la fase 1.*

Evaluada contra la versión **4.8.0**, que ya recoge las tres partes (enmienda 4.7.0) y las dos
decisiones del propietario de esta spec (enmienda 4.8.0). No hace falta otra enmienda.

| Principio | Cómo lo cumple el plan | Resultado |
| --- | --- | --- |
| §2.1 y §2.2 Español y vocabulario | `CartaJugador`, `OrigenResultado`, `PosicionJugador`, `RarezaCarta`, `ReglaResultadoDeFamilia`, `ServicioResultadoFamilia`, `CartaJugador.tsx`; "familia" solo en textos de pantalla, el rol sigue siendo JUGADOR | Cumple |
| §2.3 Máximo 250 líneas | Servicios nuevos separados desde el principio; el dibujo dividido en generador y plano (research §14) | Cumple |
| §3 Documentación XML | En cada clase nueva; se reescribe la de `Partido`, `PartidoJugador`, `ReglaAccesoAFicha`, `AlcanceDeFicha`, `ServicioIdentidadJugador`, `ServicioResultado`, `RepositorioConvocatoria` y `ControladorFichaJugador` | Cumple |
| §4 y §5 Capas | Reglas puras en `Dominio/Reglas`; la aplicación conoce solo `IGeneradorImagenCarta`; SkiaSharp vive en `Infraestructura`; controladores que delegan | Cumple |
| §6 y §19 Tecnologías y simplicidad | Una sola dependencia nueva, justificada por RF-031 y §15 (ver "Seguimiento de complejidad"). Una tabla nueva; valoración y rareza calculadas; sin historial | Cumple, con la nota 1 |
| §7.1 Aislamiento entre clubes | `CartaJugador` implementa `IPerteneceAClub`; las cinco rutas nuevas bajo `/api/clubes/{clubId}` entran solas en las pruebas de aislamiento | Cumple |
| §7.5 Aislamiento dentro del club | La carta se ve por `AccesoAFicha`; la familia escribe solo la fila de su jugador y nunca recibe la de otros | Cumple |
| §8 Roles | Tabla de permisos de research §8; familia: identidad, dorsal y posición, resultado de su convocado; PRESIDENTE y quien entrena: estadísticas y corrección; DESARROLLADOR: 404 | Cumple |
| §9 Sitio público | Ninguna ruta pública; la carta nunca sale en el sitio público | Cumple |
| §10 Documento | El cambio de documento por la familia sigue el endpoint y las reglas de la 005, sin cambios | Cumple |
| §11.2 Cambio de categoría | La familia corrige la fecha de nacimiento sin tocar la categoría; el PRESIDENTE sigue con la regla de la 005 (research §7) | Cumple |
| §13 Histórico | Autores del marcador, de cada indicación y de la carta copiados por nombre | Cumple |
| §14.1 y §14.2 Retiro y foto | El retirado no llega al club; su carta queda con la figura genérica porque su foto ya se borró; el PRESIDENTE la sigue exportando | Cumple |
| §14.3 Carta | Visible por la ficha; exporta solo la cuenta y el PRESIDENTE; estadísticas por quien entrena y el PRESIDENTE; dorsal y posición también por la familia; nunca como clasificación | Cumple |
| §15 Reglas en backend | Permisos, momentos, primer marcador, suma de goles y exportación en la API; la pantalla solo oculta por comodidad | Cumple |
| §17 Datos deportivos | Lo de la familia alimenta las estadísticas de la 009 sin cambios en el cálculo; las estadísticas de la carta no se calculan de lo registrado ni lo alteran | Cumple |
| §17.2 y §17.3 Convocatorias y resultado de la familia | Endpoint propio que solo admite el jugador de la petición; 404 al no convocado; primer marcador bajo el bloqueo del club; corrección por fila (research §2) | Cumple, con la nota 2 |
| §18 Estados | Solo estado actual y origen del último cambio; la primera publicación se guarda porque RF-009 la pide, no como historial | Cumple |
| §20 Pruebas | Las nuevas de §20 (familia cambia identidad sin cambiar categoría; resultado de la familia; carta, exportación y "nunca ordenadas") como pruebas de integración, y las reglas puras como unitarias | Cumple |
| §22 Migraciones | Una migración controlada, sin datos que migrar | Cumple |
| §23 API | DTOs propios; partes que no aplican omitidas; `problem+json` con códigos estables nuevos (`falta_marcador`, `marcador_ya_publicado`, `indicacion_corregida`, `no_entrena_categoria`); Swagger | Cumple |
| §24 Diseño visual | Teléfono y escritorio, dos temas, estados por texto, rareza escrita; la carta no está en el lienzo | Cumple, con la nota 3 |
| §25 Decisiones no tomadas | Los cuatro supuestos de research §12 los aprobó el propietario el 2026-10-11 | Cumple, con la nota 2 |
| §27 Definición de terminado | Tareas por historia, cada una con backend, pantalla y pruebas | Cumple |
| §28 Decisiones pendientes | No se resuelve ninguna; las dos de esta spec ya las cerró la 4.8.0 | Cumple |

**Resultado de la puerta**: pasa.

**Nota 1**: SkiaSharp es una tecnología nueva. Se justifica porque RF-031 exige que el servidor
niegue la imagen a quien la pida directamente, y eso solo es posible si la imagen la produce el
servidor (research §11).

**Nota 2**: cuatro supuestos (research §12): qué es "corregir" lo de un jugador, que la carta de un
retirado no se edita, que un marcador repetido también se rechaza y que la carta no muestra el
porcentaje de asistencia. Son lecturas de la spec, no reglas nuevas, y cada una cambia en una regla
sin tocar el modelo. El propietario los aprobó el 2026-10-11.

**Nota 3**: el lienzo no tiene la carta. Su diseño sigue [contracts/carta.md](contracts/carta.md)
con los tokens y componentes del proyecto, y lo revisa el propietario en la validación.

**Revisión tras el diseño**: el modelo añade una tabla justificada, columnas en dos existentes y una
dependencia aislada en infraestructura. Los 5 endpoints quedan bajo `/api/clubes/{clubId}` y entran
en las pruebas de aislamiento existentes, que se descubren solas desde el contrato. La puerta sigue
pasando.

## Estructura del proyecto

### Documentación de esta funcionalidad

```text
specs/010-ficha-resultado-y-carta/
├── plan.md              # Este archivo
├── research.md          # Decisiones técnicas y supuestos
├── data-model.md        # Tabla nueva, columnas nuevas, cálculos y validaciones
├── quickstart.md        # Guía de validación de extremo a extremo
├── contracts/
│   ├── api.yaml         # Cinco endpoints nuevos y tres que cambian
│   └── carta.md         # Plano común de la carta en pantalla y en imagen
└── tasks.md             # Lo genera /speckit-tasks
```

### Código fuente

Solo se listan los archivos nuevos (`+`) y los que cambian (`~`). La estructura es la de la 001.

```text
backend/src/
├── LFClub.Dominio/
│   ├── Entidades/         + CartaJugador
│   │                      ~ Partido, PartidoJugador (columnas), UsuarioRol (documentación)
│   ├── Enumeraciones/     + OrigenResultado, PosicionJugador, RarezaCarta
│   └── Reglas/            + ReglaResultadoDeFamilia, ReglaCorreccionDeIndicacion,
│                            ReglaAccesoACarta, AlcanceDeCarta, ReglaValoracionDeCarta,
│                            ReglaRarezaDeCarta, ReglaEstadisticasDeCarta
│                          ~ ReglaAccesoAFicha y AlcanceDeFicha (la familia corrige la identidad)
├── LFClub.Aplicacion/
│   ├── DTOs/              + ResultadoDeFamiliaDto, MiParticipacionDto, PublicacionResultadoDto,
│   │                        IndicacionDto, CartaDto, ValoresCartaDto, EstadisticasCartaDto,
│   │                        DorsalYPosicionDto, PermisosDeCartaDto
│   │                      ~ PartidoDto, PermisosDeActividadDto, PlanillaPartidoDto (ConvocadoDto)
│   ├── Interfaces/        + IRepositorioCartas, IGeneradorImagenCarta
│   │                      ~ IRepositorioConvocatoria (fila propia, suma de los demás, indicación),
│   │                        IRepositorioEstadisticas (asistencia de la temporada)
│   ├── Servicios/         + IServicioResultadoFamilia, IServicioConsultaCarta, IServicioCarta,
│   │                        IServicioImagenCarta
│   ├── Implementaciones/  + uno por interfaz de Servicios
│   │                      ~ ServicioResultado (bloqueo del club, origen y corrección por fila),
│   │                        ServicioIdentidadJugador (sin ubicar cuando corrige la familia),
│   │                        ServicioConsultaPartidos (participación propia y publicación)
│   ├── Mappers/           + MapperCarta; ~ MapperPartidos
│   └── Utilidades/        + AccesoACarta, ErroresDeCarta; ~ ErroresDeActividades, AccesoAActividad
├── LFClub.Infraestructura/
│   ├── Datos/             ~ ContextoLFClub (DbSet de cartas)
│   │   ├── Configuraciones/ + ConfiguracionCartaJugador; ~ las de Partido y PartidoJugador
│   │   └── Migraciones/   + FichaResultadoYCarta
│   ├── Imagenes/          + GeneradorImagenCarta, PlanoCarta, ContrasteDeColor
│   │   └── Fuentes/       + BarlowCondensed-Bold.ttf, BarlowCondensed-ExtraBold.ttf (recurso)
│   └── Repositorios/      + RepositorioCartas; ~ RepositorioConvocatoria, RepositorioEstadisticas
└── LFClub.Api/
    ├── Configuracion/     ~ RegistroDeCasosDeUso, RegistroDeServicios
    └── Controladores/Club/ + ControladorCarta, ControladorResultadoFamilia
                           ~ ControladorFichaJugador (identidad también para JUGADOR)

backend/pruebas/
├── Unitarias/
│   ├── Reglas/            + una clase de pruebas por regla nueva; ~ ReglaAccesoAFichaPruebas
│   └── Utilidades/        + ContrasteDeColorPruebas
└── Integracion/
    ├── Partidos/          + ResultadoFamiliaPruebas, PrimerMarcadorPruebas,
    │                        CorreccionPorFilaPruebas, VisibilidadResultadoFamiliaPruebas
    │                      ~ ResultadoPruebas (origen y bloqueo)
    ├── Ficha/             ~ CorreccionDeIdentidadPruebas (la familia corrige; categoría fija)
    ├── Carta/             + ConsultaCartaPruebas, EstadisticasCartaPruebas,
    │                        DorsalYPosicionPruebas, RarezaPruebas, ExportarCartaPruebas,
    │                        SinClasificacionPruebas (ningún esquema de lista lleva valores de carta)
    └── Aislamiento/       ~ AccesoClubPruebas (75 de club; contrato de 99)

frontend/src/
├── App.tsx                ~ ruta de la carta
├── compartido/
│   ├── api/               + tiposCarta.ts; ~ tiposActividades.ts
│   └── tema/              + carta.css
├── privado/
│   ├── carta/             + PantallaCarta.tsx, CartaJugador.tsx, FiguraGenerica.tsx,
│   │                        FormularioEstadisticas.tsx, FormularioDorsalYPosicion.tsx,
│   │                        BotonDescargarCarta.tsx, plano.ts, textos.ts
│   ├── partidos/          + SeccionResultadoFamilia.tsx
│   │                      ~ DetallePartido.tsx, SeccionResultado.tsx (publicación y estado por
│   │                        texto), FormularioResultado.tsx (indicación por fila), textos.ts
│   └── ficha/             ~ FichaJugador.tsx ("Ver carta"), SeccionIdentidad.tsx (la familia
│                            corrige)
└── (frontend/pruebas/)    + plano.test.ts (texto y contraste), SeccionResultadoFamilia.test.tsx
```

**Decisión de estructura**: la de la 001, con carpetas nuevas `Infraestructura/Imagenes/`,
`privado/carta/` y, en las pruebas de integración, `Carta/`.

### Pantallas

| Ruta | Qué muestra | Quién |
| --- | --- | --- |
| `/club/:clubId/partidos/:id` | Familia de un convocado: formulario de marcador y de su jugador, o su jugador con el marcador ya publicado, o el aviso de que quien entrena lo corrigió. Quien entrena y PRESIDENTE: quién publicó, estado por texto e indicación por fila | Convocados; quien ve la planilla |
| `/club/:clubId/jugadores/:id/ficha` | "Ver carta"; la familia puede corregir nombre y fecha | Quien ve la ficha |
| `/club/:clubId/jugadores/:id/carta` | **Nueva**: la carta, dorsal y posición, estadísticas con su último cambio, "Descargar imagen" | Quien ve la ficha; acciones según permisos |

## Trazabilidad

| Historia | Requisitos | Backend | Pantallas | Pruebas clave |
| --- | --- | --- | --- | --- |
| 1. Resultado por la familia | RF-001 a 012 | `ControladorResultadoFamilia`, `ServicioResultadoFamilia`, `ReglaResultadoDeFamilia`, `ReglaCorreccionDeIndicacion`, `ServicioResultado`, `RepositorioConvocatoria` | `SeccionResultadoFamilia`, `SeccionResultado`, `FormularioResultado` | Antes de empezar, 409; no convocado, 404 aunque esté jugado; primer marcador gana con dos peticiones en paralelo; suma de goles sin nombres; corrección por fila; la familia nunca recibe convocados; estadísticas al instante |
| 2. Ficha por la familia | RF-013 a 018 | `ReglaAccesoAFicha`, `ServicioIdentidadJugador`, `ControladorFichaJugador` | `SeccionIdentidad`, `FichaJugador` | La familia corrige y la categoría no cambia (con y sin categoría); hermano intacto; ENTRENADOR y DIRECTIVO 403; último cambio "la familia" |
| 3. Carta | RF-019 a 028 | `ControladorCarta`, `ServicioConsultaCarta`, `ServicioCarta`, `AccesoACarta`, `ReglaAccesoACarta`, `ReglaValoracionDeCarta`, `ReglaRarezaDeCarta`, `ReglaEstadisticasDeCarta`, `RepositorioCartas` | `PantallaCarta`, `CartaJugador`, `FormularioEstadisticas`, `FormularioDorsalYPosicion` | Tabla de permisos de research §8 rol por rol; 72,5 → 73; seis o ninguna; rareza en los bordes 59/60/84/85 y sin registros; corregir asistencia cambia la rareza; ningún esquema de lista con valores de carta |
| 4. Exportar | RF-029 a 031 | `ServicioImagenCarta`, `GeneradorImagenCarta`, `PlanoCarta` | `BotonDescargarCarta` | PNG 750 × 1050 para la familia y el PRESIDENTE (también retirado); ENTRENADOR y DIRECTIVO 403; otro jugador 404; la imagen no contiene datos fuera de la carta |
| Transversal | RF-032 a 035 | Aislamiento | Todas, a 360 px y en los dos temas | Contrato de 99; 75 de club con 404 a otro club y al DESARROLLADOR; la 001 a la 009 en verde |

## Orden de construcción sugerido

Para `/speckit-tasks`. Cada bloque deja algo que se puede probar de extremo a extremo (§27.1).

1. **Base**: contrato comentado hasta que exista cada endpoint (como en la 008 y la 009, para que
   `AccesoClubPruebas` siga en verde); enumeraciones, entidad, columnas, configuraciones y
   migración; reglas puras con sus pruebas.
2. **Historia 2** (la más pequeña e independiente): la familia corrige la identidad.
3. **Historia 1**: endpoint y pantalla de la familia; después la corrección por fila y la
   publicación en la pantalla de quien entrena.
4. **Historia 3**: consulta de la carta y pantalla; dorsal y posición; estadísticas; rareza.
5. **Historia 4**: generador de la imagen, endpoint y botón de descarga; comprobación en el
   contenedor Linux.
6. **Cierre**: contrato sin marcas contra Swagger (99), aislamiento (75), revisión a 360 px en los
   dos temas, recorrido de [quickstart.md](quickstart.md) en un proyecto de Compose aparte y
   batería entera.

## Seguimiento de complejidad

| Desviación | Por qué se necesita | Alternativa más simple descartada |
| --- | --- | --- |
| Dependencia nueva (SkiaSharp) y fuentes incluidas en la API | RF-031 y §15: la API debe poder negar la imagen, y para eso debe producirla | Generar la imagen en el navegador: cualquiera que ve la carta podría generarla, y la barrera sería solo de pantalla |
| La carta se dibuja dos veces (HTML en la pantalla, SkiaSharp en la API) | La pantalla no puede usar la imagen de la API sin entregársela a quien no exporta | Mostrar el PNG de la API en pantalla: cualquiera que ve la carta la descarga. El plano común de `contracts/carta.md` y una prueba que compara los datos dibujados mantienen las dos iguales |
| Se guarda la primera publicación además del último cambio | RF-009 pide quién publicó y cuándo aunque quien entrena corrija después | Solo el último cambio: pierde quién publicó cuando quien entrena corrige |
