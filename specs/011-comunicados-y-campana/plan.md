# Plan de implementación: comunicados del club y campana de notificaciones

**Rama**: `011-comunicados-y-campana` | **Fecha**: 2026-10-12 | **Spec**: [spec.md](spec.md)

**Entrada**: especificación de la funcionalidad en `specs/011-comunicados-y-campana/spec.md`

## Resumen

El PRESIDENTE, los DIRECTIVOS y los ENTRENADORES publican comunicados (título y texto plano)
dirigidos a todo el club, a categorías o a equipos de una categoría. Los reciben las cuentas con
un jugador activo en la audiencia, que ven en la barra superior de todas sus pantallas una campana
con el contador de los sin leer, abren la lista, leen y marcan como leído. Quien publica ve lo que
publicó y lo retira; un comunicado no se edita ni se borra. No hay avisos fuera de la aplicación.

Enfoque técnico: **tres tablas nuevas** (`Comunicados`, `ComunicadosDestino` y
`ComunicadosDestinatario`) y **una migración** (`ComunicadosYCampana`). La audiencia se resuelve
al publicar y queda guardada como destinatarios, así "quien llega después no recibe lo anterior" no
necesita regla. El leído es por cuenta aunque la fila sea del jugador. La publicación es idempotente
por una clave que genera la pantalla. La campana consulta un contador barato al entrar, al volver
al primer plano, al cambiar de pantalla y cada 60 segundos. 8 endpoints nuevos y ninguno que
cambie. Las decisiones y sus alternativas están en [research.md](research.md).

## Contexto técnico

**Lenguaje y versión**: C# 14 sobre .NET 10 en el backend; TypeScript 5 sobre Node 22 en el
frontend. Sin cambios respecto de la 001.

**Dependencias principales**: las de la 001 (ASP.NET Core 10, Entity Framework Core 10 con Npgsql,
JWT, React 19, React Router, Vite). **No se añade ninguna.** Ni SkiaSharp ni ningún servicio de
avisos: el aviso con la aplicación cerrada es de otra spec.

**Almacenamiento**: PostgreSQL 17. Una migración con tres tablas, un índice único y uno parcial, y
sus restricciones de comprobación ([data-model.md](data-model.md)).

**Pruebas**: xUnit (unitarias e integración con `WebApplicationFactory` y Testcontainers, con el
`IReloj` y el servicio de correo sustituidos); Vitest en el frontend.

**Plataforma de destino**: la de la 001: API en contenedor Linux y aplicación web adaptable,
también empaquetada con Capacitor para Android.

**Tipo de proyecto**: aplicación web con `backend/` y `frontend/` separados (§4).

**Objetivos de rendimiento**: el contador es una consulta por índice parcial
`(UsuarioId, ClubId) WHERE LeidoEn IS NULL`; la lista, una consulta por cursor sobre
`(UsuarioId, ClubId, ComunicadoId)`. Publicar es una unidad de trabajo con una inserción por
destinatario (decenas por club).

**Restricciones**: código y documentación en español (§2.1, §3); ningún archivo escrito a mano por
encima de 250 líneas (§2.3); toda regla en el servidor (§15, RF-031); backend y pantalla de cada
historia en la misma tarea (§27.1); ningún aviso fuera de la aplicación (RF-028); ninguna
notificación automática por otros eventos (RF-029); el texto nunca se interpreta (RF-036).

**Escala y alcance**: decenas de familias y comunicados por club (§19). 3 entidades nuevas y 1
enumeración nueva; 8 operaciones de API (el contrato pasa de 100 a 108; las de club, de 76 a 84);
unos 12 archivos de frontend nuevos y unos 4 que cambian.

## Comprobación de la constitución

*Puerta: debe pasar antes de la fase 0 y se revisa de nuevo tras el diseño de la fase 1.*

Evaluada contra la versión **4.9.0**, que ya recoge la funcionalidad (§8, §14.4, §20 y §24). No
hace falta otra enmienda.

| Principio | Cómo lo cumple el plan | Resultado |
| --- | --- | --- |
| §2.1 y §2.2 Español y vocabulario | `Comunicado`, `ComunicadoDestino`, `ComunicadoDestinatario`, `AlcanceComunicado`, `ReglaAudienciaDeComunicado`, `ServicioPublicacionComunicados`, `CampanaComunicados.tsx`; "familia" solo en textos de pantalla, el rol sigue siendo JUGADOR | Cumple |
| §2.3 Máximo 250 líneas | Servicios y controladores separados por responsabilidad desde el principio (publicación, consulta, lectura) | Cumple |
| §3 Documentación XML | En cada clase nueva; se amplía la de `UsuarioRol`, `Categoria` y `Equipo` | Cumple |
| §4 y §5 Capas | Reglas puras en `Dominio/Reglas`; servicios en `Aplicacion`; repositorio en `Infraestructura`; controladores que delegan | Cumple |
| §6 y §19 Tecnologías y simplicidad | Sin dependencias nuevas; sondeo en lugar de conexiones persistentes; sin tabla genérica de notificaciones | Cumple |
| §7.1 Aislamiento entre clubes | Las tres entidades implementan `IPerteneceAClub`; las 8 rutas bajo `/api/clubes/{clubId}` entran solas en las pruebas de aislamiento | Cumple |
| §7.5 Aislamiento dentro del club | El ENTRENADOR publica solo a sus categorías y ve solo lo suyo; la cuenta ve solo las filas de sus jugadores, y la sesión limitada solo las del suyo | Cumple |
| §8 Roles | Presidente y directivo: cualquier audiencia; entrenador: sus categorías; jugador: recibe y lee; DESARROLLADOR: 404 | Cumple |
| §9 Sitio público | Ninguna ruta pública | Cumple |
| §13 Histórico | Autor, rol y nombres de destino copiados al publicar | Cumple |
| §14 y §14.4 Desactivables y comunicados | Se retira, no se borra; no se edita; llega solo a jugadores activos; campana solo de la cuenta de jugador; sin avisos fuera de la aplicación | Cumple |
| §15 Reglas en backend | Permisos, audiencia, destinatarios, retiro y visibilidad en la API; la pantalla solo oculta por comodidad | Cumple |
| §18 Estados | Solo "publicado" y "retirado"; el leído es por cuenta y no tiene historial | Cumple |
| §20 Pruebas | Las tres nuevas de §20 como pruebas de integración, y las reglas puras como unitarias | Cumple |
| §22 Migraciones | Una migración controlada, sin datos que migrar | Cumple |
| §23 API | DTOs propios; `problem+json` con códigos estables nuevos (`audiencia_invalida`, `sin_destinatarios`, `clave_en_uso`, `comunicado_retirado`, `no_retira_comunicado`); Swagger | Cumple |
| §24 Diseño visual | Teléfono y escritorio, dos temas, leído y retirado por texto, insignia con contraste; un solo icono nuevo en la barra | Cumple, con la nota 1 |
| §25 Decisiones no tomadas | Los cuatro supuestos de research §17 son técnicos y no alteran ninguna regla de la spec | Cumple |
| §27 Definición de terminado | Tareas por historia, cada una con backend, pantalla y pruebas | Cumple |
| §28 Decisiones pendientes | No se resuelve ninguna | Cumple |

**Resultado de la puerta**: pasa.

**Nota 1**: el lienzo no tiene la campana ni los comunicados. Su diseño sigue
[contracts/campana.md](contracts/campana.md) con los tokens y componentes del proyecto, y lo revisa
el propietario en la validación.

**Revisión tras el diseño**: el modelo añade tres tablas justificadas por la audiencia fija y el
leído por cuenta; ninguna dependencia nueva. Los 8 endpoints quedan bajo `/api/clubes/{clubId}` y
entran en las pruebas de aislamiento existentes, que se descubren solas desde el contrato. La
puerta sigue pasando.

## Estructura del proyecto

### Documentación de esta funcionalidad

```text
specs/011-comunicados-y-campana/
├── plan.md              # Este archivo
├── research.md          # Decisiones técnicas y supuestos
├── data-model.md        # Tablas nuevas, cálculos y validaciones
├── quickstart.md        # Guía de validación de extremo a extremo
├── contracts/
│   ├── api.yaml         # Ocho endpoints nuevos
│   └── campana.md       # Comportamiento de la campana y de las pantallas
└── tasks.md             # Lo genera /speckit-tasks
```

### Código fuente

Solo se listan los archivos nuevos (`+`) y los que cambian (`~`). La estructura es la de la 001.

```text
backend/src/
├── LFClub.Dominio/
│   ├── Entidades/         + Comunicado, ComunicadoDestino, ComunicadoDestinatario
│   │                      ~ UsuarioRol, Categoria, Equipo (documentación)
│   ├── Enumeraciones/     + AlcanceComunicado
│   └── Reglas/            + ReglaAudienciaDeComunicado, ReglaRetiroDeComunicado,
│                            ReglaVisibilidadDeComunicadosPublicados
├── LFClub.Aplicacion/
│   ├── DTOs/              + PublicarComunicadoDto, ComunicadoPublicadoDto, ComunicadoRecibidoDto,
│   │                        AutorDeComunicadoDto, DestinoDeComunicadoDto, RetiroDeComunicadoDto,
│   │                        PaginaDePublicadosDto, PaginaDeRecibidosDto, ContadorDeComunicadosDto
│   ├── Interfaces/        + IRepositorioComunicados
│   ├── Servicios/         + IServicioPublicacionComunicados, IServicioConsultaComunicados,
│   │                        IServicioLecturaComunicados
│   ├── Implementaciones/  + uno por interfaz de Servicios
│   ├── Mappers/           + MapperComunicados
│   ├── Validadores/       + ValidadorPublicarComunicado
│   └── Utilidades/        + ErroresDeComunicados
├── LFClub.Infraestructura/
│   ├── Datos/             ~ ContextoLFClub (tres DbSet)
│   │   ├── Configuraciones/ + ConfiguracionComunicado, ConfiguracionComunicadoDestino,
│   │   │                      ConfiguracionComunicadoDestinatario
│   │   └── Migraciones/   + ComunicadosYCampana
│   └── Repositorios/      + RepositorioComunicados
└── LFClub.Api/
    ├── Configuracion/     ~ RegistroDeCasosDeUso, RegistroDeServicios
    └── Controladores/Club/ + ControladorComunicados, ControladorComunicadosRecibidos

backend/pruebas/
├── Unitarias/
│   ├── Reglas/            + una clase de pruebas por regla nueva
│   └── Validadores/       + ValidadorPublicarComunicadoPruebas
└── Integracion/
    ├── Comunicados/       + PublicarComunicadoPruebas, AudienciaPruebas,
    │                        CampanaYContadorPruebas, LecturaPruebas, RetiroPruebas,
    │                        IdempotenciaPruebas, SinAvisosExternosPruebas
    └── Aislamiento/       ~ AccesoClubPruebas (84 de club; contrato de 108)

frontend/src/
├── App.tsx                ~ rutas de comunicados
├── compartido/
│   ├── api/               + tiposComunicados.ts
│   ├── componentes/       + CampanaComunicados.tsx
│   └── tema/              + comunicados.css
├── privado/
│   ├── comunicados/       + PantallaComunicados.tsx, ListaRecibidos.tsx, ListaPublicados.tsx,
│   │                        DetalleComunicado.tsx, FormularioComunicado.tsx,
│   │                        SelectorAudiencia.tsx, useContadorComunicados.ts, textos.ts
│   ├── DisposicionClub.tsx ~ la campana junto al perfil, solo para JUGADOR
│   └── MenuDelClub.tsx    ~ entrada "Comunicados"
└── (frontend/pruebas/)    + CampanaComunicados.test.tsx, FormularioComunicado.test.tsx
```

**Decisión de estructura**: la de la 001, con carpetas nuevas `privado/comunicados/` y, en las
pruebas de integración, `Comunicados/`.

### Pantallas

| Ruta | Qué muestra | Quién |
| --- | --- | --- |
| Barra superior de todo `/club/:clubId/**` | **Nueva**: campana con insignia y nombre accesible | JUGADOR |
| `/club/:clubId/comunicados` | JUGADOR: comunicados recibidos, leído y sin leer, "Marcar todos". Equipo del club: comunicados publicados, estado, "Nuevo comunicado" y "Retirar" | Todos los roles del club; vista según el rol |
| `/club/:clubId/comunicados/nuevo` | **Nueva**: título, texto y para quién | PRESIDENTE, DIRECTIVO y ENTRENADOR |
| `/club/:clubId/comunicados/:id` | **Nueva**: detalle del comunicado recibido o publicado | Según el rol |

## Trazabilidad

| Historia | Requisitos | Backend | Pantallas | Pruebas clave |
| --- | --- | --- | --- | --- |
| 1. Publicar | RF-001 a 011 | `ControladorComunicados`, `ServicioPublicacionComunicados`, `ReglaAudienciaDeComunicado`, `ValidadorPublicarComunicado`, `RepositorioComunicados` | `FormularioComunicado`, `SelectorAudiencia` | Tabla de audiencia rol por rol; entrenador fuera de sus categorías 403; equipos de dos categorías 400; sin destinatarios 400; en espera, retirado y sin categoría; misma clave dos veces |
| 2. Campana | RF-012 a 019 | `ControladorComunicadosRecibidos`, `ServicioConsultaComunicados`, `RepositorioComunicados` | `CampanaComunicados`, `ListaRecibidos`, `DetalleComunicado`, `useContadorComunicados` | Contador igual a los sin leer; hermanos cuentan uno; "99+"; otra cuenta 404; sin campana para el equipo del club; sin avisos externos |
| 3. Leído | RF-020 a 023 | `ServicioLecturaComunicados` | `ListaRecibidos` ("Marcar todos"), `DetalleComunicado` | Leer baja el contador; por cuenta; marcar todos; persiste entre sesiones; sesión limitada |
| 4. Publicados y retiro | RF-024 a 027 | `ServicioConsultaComunicados`, `ServicioPublicacionComunicados`, `ReglaRetiroDeComunicado`, `ReglaVisibilidadDeComunicadosPublicados` | `ListaPublicados`, `DetalleComunicado` | Entrenador ve solo los suyos; presidente y directivo, todos; retiro por autor y PRESIDENTE; directivo ajeno 403; retirado sale de la campana y sigue en publicados |
| Transversal | RF-028 a 036 | Aislamiento | Todas, a 360 px y en los dos temas | Contrato de 108; 84 de club con 404 a otro club y al DESARROLLADOR; texto sin interpretar; la 001 a la 010 en verde |

## Orden de construcción sugerido

Para `/speckit-tasks`. Cada bloque deja algo que se puede probar de extremo a extremo (§27.1).

1. **Base**: contrato comentado hasta que exista cada endpoint (como en la 008, la 009 y la 010,
   para que `AccesoClubPruebas` siga en verde); enumeración, entidades, configuraciones y
   migración; reglas puras con sus pruebas.
2. **Historia 1**: publicar, con el formulario; la lista de publicados mínima para comprobarlo.
3. **Historia 2**: recibidos, contador y campana en la barra superior.
4. **Historia 3**: leído, "marcar todos" y su persistencia.
5. **Historia 4**: lista completa del equipo del club y retiro.
6. **Cierre**: contrato sin marcas contra Swagger (107), aislamiento (83), revisión a 360 px en los
   dos temas, recorrido de [quickstart.md](quickstart.md) en un proyecto de Compose aparte y
   batería entera.

## Seguimiento de complejidad

Sin desviaciones: no hay dependencias nuevas ni patrones nuevos. La única decisión que se aparta de
lo más corto es guardar a los destinatarios en lugar de calcularlos al leer, y se justifica en
research §1.
