# Plan de implementación: Nombres clicables y ficha básica de entrenadores, presidentes y directivos

**Rama**: `008-fichas-de-personas` | **Fecha**: 2026-10-10 | **Spec**: [spec.md](spec.md)

**Entrada**: especificación de la funcionalidad en `specs/008-fichas-de-personas/spec.md`

## Resumen

Cada PRESIDENTE, DIRECTIVO y ENTRENADOR tiene una ficha básica en su club (nombre, rol, foto de
perfil o iniciales, y las categorías y equipos activos que entrena) que abre cualquier integrante
del club, también la familia, y nadie de fuera. Los nombres de personas de las pantallas del club
pasan a ser enlaces a la ficha que corresponde (la del jugador o la básica) cuando quien mira
puede abrirla, y texto normal cuando no. La familia ve en su inicio la miniatura de los
entrenadores de su jugador. "Mi perfil" deja a un PRESIDENTE, DIRECTIVO o ENTRENADOR ver sus datos
y cambiar nombres, apellidos, fecha de nacimiento y celular en todos sus clubes, sin tocar el
documento ni el correo; a una cuenta de jugador la lleva a "Mi ficha".

Enfoque técnico: **sin tablas ni migraciones**. La ficha básica se arma al consultar con
`UsuarioRol`, `Usuario.VersionFoto` y las asignaciones de la 003. Cuatro operaciones nuevas: la
ficha básica y la foto de perfil ante el club (bajo `/api/clubes/{clubId}/personas`), y leer y
cambiar los datos propios (bajo `/api/cuenta`). Cinco respuestas existentes ganan campos: el
identificador y la versión de foto del entrenador en "Mi categoría", y un `EnlaceFichaDto` que la
API calcula para los autores y personas que hoy salen solo como texto. En la pantalla, un único
componente `NombrePersona` pinta enlace o texto, y un gancho `useVolver` lleva de vuelta a la
pantalla de origen. Las decisiones y sus alternativas están en [research.md](research.md).

## Contexto técnico

**Lenguaje y versión**: C# 14 sobre .NET 10 en el backend; TypeScript 5 sobre Node 22 en el
frontend. Sin cambios respecto de la 001.

**Dependencias principales**: las de la 001 (ASP.NET Core 10, Entity Framework Core 10 con Npgsql,
JWT, React 19, React Router, Vite). No se añade ninguna.

**Almacenamiento**: PostgreSQL 17. Ninguna migración: no cambia el esquema (data-model.md).

**Pruebas**: xUnit (unitarias e integración con `WebApplicationFactory` y Testcontainers); Vitest
en el frontend.

**Plataforma de destino**: la de la 001: API en contenedor Linux y aplicación web adaptable.

**Tipo de proyecto**: aplicación web con `backend/` y `frontend/` separados (§4).

**Objetivos de rendimiento**: la ficha básica hace una consulta por la persona y otra por sus
asignaciones con equipos; los enlaces de autores se resuelven con una consulta por lista (todos
los `UsuarioId` de la página a la vez), no una por fila. Las miniaturas se piden solo si
`versionFoto > 0` y quedan en memoria por versión.

**Restricciones**: código y documentación en español (§2.1, §3); ningún archivo escrito a mano por
encima de 250 líneas (§2.3); toda autorización y todo aislamiento en el servidor (§15, RF-003,
RF-004, RF-019); backend y pantalla de cada historia en la misma tarea (§27.1); la ficha básica no
entrega documento, nacimiento ni contacto (RF-002).

**Escala y alcance**: decenas de personas por club (§19). Ninguna entidad nueva; 4 operaciones de
API nuevas y 5 respuestas que ganan campos (el contrato pasa de 68 a 72; las de club, de 46 a 48);
unos 10 archivos de frontend nuevos y unos 15 que cambian.

## Comprobación de la constitución

*Puerta: debe pasar antes de la fase 0 y se revisa de nuevo tras el diseño de la fase 1.*

Evaluada contra la versión **4.5.1**, que ya incorpora esta funcionalidad (§7.1, §8 Jugador, §12.6 y
§14.2).

| Principio | Cómo lo cumple el plan | Resultado |
| --- | --- | --- |
| §2.1 y §2.2 Español y vocabulario | `FichaBasicaDto`, `ServicioFichaBasica`, `EnlaceFichaDto`, `DatosPropiosDto`, `NombrePersona`, `useVolver`; "entrenador", "presidente", "directivo", sin Profesor ni Acudiente | Cumple |
| §2.3 Máximo 250 líneas | Lo nuevo va en archivos propios. `MiPerfil.tsx` (110) se divide: la foto pasa a `SeccionFotoPerfil.tsx` y los datos a `SeccionDatosPropios.tsx`. `fotosJugador.ts` (151) se generaliza y se adelgaza. `tipos.ts` (239) no crece: los tipos nuevos van en `tiposPersonas.ts` | Cumple |
| §3 Documentación XML | En cada clase nueva, y se reescribe donde cambia la responsabilidad: `FotoPerfil`, `IRepositorioFotosPerfil`, `ControladorFotoPerfil` (ya no "solo la ve su dueño"), `EntrenadorParaFamiliaDto` (ya lleva identificador), `UsuarioRol` y `Usuario` (quién cambia nombres y celular) | Cumple |
| §4 y §5 Capas | Controladores que solo delegan; servicios con interfaces; repositorios sin reglas; la resolución del enlace es una regla de dominio pura (`ReglaEnlaceDeFicha`) | Cumple |
| §6 y §19 Tecnologías y simplicidad | Ninguna tecnología, tabla ni migración nueva; la caché de fotos de la 007 se generaliza en vez de duplicarse | Cumple |
| §7.1 Aislamiento entre clubes | La ficha básica y la foto están bajo `/api/clubes/{clubId}` con el filtro de club; la foto se resuelve a través del integrante de ese club. Los datos propios leen y escriben en los integrantes PRESIDENTE, DIRECTIVO o ENTRENADOR de la propia cuenta en varios clubes, la excepción que §7.1 recoge desde la 4.5.1 | Cumple |
| §7.5 Aislamiento dentro del club | Abrir una ficha desde un nombre sigue pasando por `AccesoAFicha`; el enlace no concede acceso (RF-012). La familia abre fichas básicas de todo el club, como dice §8 | Cumple |
| §8 Roles | Ficha básica para todos los roles del club; el DESARROLLADOR, 404. Solo la propia cuenta cambia sus datos; nadie cambia el documento | Cumple |
| §9 Sitio público | Ninguna ruta pública nueva; la foto de perfil nunca es pública | Cumple |
| §10 Documento | El documento no se puede cambiar por "Mi perfil": el DTO de entrada no lo tiene | Cumple |
| §12.6 Ficha básica y datos propios | Exactamente lo que enumera, con las reglas del registro y en todos los clubes | Cumple |
| §13 Histórico | Los nombres copiados en retiros, aprobaciones y últimos cambios no cambian; el enlace lleva a la persona actual | Cumple |
| §14.2 Foto de perfil | La de un PRESIDENTE, DIRECTIVO o ENTRENADOR la ven los de su club solo en la ficha básica y en la miniatura junto a su nombre; la de una cuenta de jugador, 404 por la ruta del club | Cumple |
| §15 Reglas en backend | Quién abre la ficha, la foto y los enlaces, y qué datos propios cambian, lo decide la API | Cumple |
| §20 Pruebas | Las tres nuevas de §20 (ficha básica, foto de perfil, datos propios) y las de aislamiento ajustadas | Cumple |
| §22 Migraciones | No hay | Cumple |
| §23 API | DTOs propios; `FichaBasicaDto` sin campos privados, ni vacíos; `problem+json`; Swagger | Cumple |
| §24 Diseño visual | Teléfono y escritorio, temas claro y oscuro, nombres largos sin desplazamiento horizontal | Cumple, con la nota 1 |
| §25 Decisiones no tomadas | El supuesto de la sala de espera (research §8) lo confirmó el propietario | Cumple |
| §27 Definición de terminado | Tareas por historia, cada una con backend, pantalla y pruebas | Cumple |
| §28 Decisiones pendientes | No se resuelve ninguna | Cumple |

**Resultado de la puerta**: pasa.

**Nota 1**: el lienzo de §24 no trae una ficha de entrenador; usa el color principal del club en
botones, barras de avance y la banda del inicio móvil. La ficha básica se compone con los
componentes aprobados (`Tarjeta`, el bloque de foto de la ficha del jugador de la 007) y con la
identidad visible del club que ya pinta la aplicación (`identidad.css`).

**Revisión tras el diseño**: el modelo y el contrato no añaden tablas, tecnologías ni patrones. Las
dos rutas nuevas de club entran en las pruebas de aislamiento existentes; las dos de cuenta
trabajan solo sobre la cuenta con sesión, como permite §7.1 desde la 4.5.1. La puerta sigue
pasando.

## Estructura del proyecto

### Documentación de esta funcionalidad

```text
specs/008-fichas-de-personas/
├── plan.md              # Este archivo
├── research.md          # Decisiones técnicas y supuestos
├── data-model.md        # Vistas calculadas, enlaces y datos propios (sin tablas nuevas)
├── quickstart.md        # Guía de validación de extremo a extremo
├── contracts/
│   └── api.yaml         # Cuatro endpoints nuevos y cinco respuestas que ganan campos
└── tasks.md             # Lo genera /speckit-tasks
```

### Código fuente

Solo se listan los archivos nuevos (`+`) y los que cambian (`~`). La estructura es la de la 001.

```text
backend/src/
├── LaPecosa.Dominio/
│   ├── Enumeraciones/     + TipoFicha.cs                       # JUGADOR, BASICA
│   ├── Reglas/            + ReglaEnlaceDeFicha.cs              # a qué ficha lleva un nombre
│   └── Entidades/         ~ FotoPerfil.cs, Usuario.cs, UsuarioRol.cs (solo documentación)
├── LaPecosa.Aplicacion/
│   ├── DTOs/              + FichaBasicaDto, CategoriaDeFichaBasicaDto, EnlaceFichaDto,
│   │                        DatosPropiosDto, CambiarDatosPropiosDto
│   │                      ~ EntrenadorParaFamiliaDto, UltimoCambioDto, JugadorRetiradoDto,
│   │                        IngresoAprobadoDto, IngresoEnEsperaDto
│   ├── Interfaces/        + IRepositorioFichaBasica, IRepositorioEnlaces
│   │                      ~ IRepositorioFotosPerfil (ObtenerDeIntegranteAsync),
│   │                        IRepositorioPertenencias (CambiarDatosPropiosAsync)
│   ├── Servicios/         + IServicioFichaBasica, IServicioDatosPropios
│   ├── Implementaciones/  + ServicioFichaBasica, ServicioDatosPropios
│   │                      ~ ServicioMiCategoria, ServicioConsultaFicha, ServicioConsultaIngresos,
│   │                        ServicioConsultaCategorias (retirados)
│   ├── Mappers/           + MapperFichaBasica
│   │                      ~ MapperCategorias, MapperFicha, MapperIngresos
│   ├── Utilidades/        + ResolutorDeEnlaces                 # una consulta por lista
│   └── Validadores/       + ValidadorDatosPropios              # reutiliza ValidadorIdentidad
├── LaPecosa.Infraestructura/
│   └── Repositorios/      + RepositorioFichaBasica, RepositorioEnlaces
│                          ~ RepositorioFotosPerfil, Plataforma/RepositorioPertenencias,
│                            RepositorioCategorias (UsuarioId y foto de entrenadores para la familia)
└── LaPecosa.Api/
    ├── Configuracion/     ~ RegistroDeCasosDeUso, RegistroDeServicios
    └── Controladores/
        ├── Club/          + ControladorPersonas                # ficha básica y su foto
        └── Cuenta/        + ControladorDatosPropios
                           ~ ControladorFotoPerfil (solo documentación)

backend/pruebas/
├── Unitarias/
│   └── Reglas/            + ReglaEnlaceDeFichaPruebas
└── Integracion/
    ├── Base/              + SembradorPersonas                  # entrenadores, directivos, fotos, dos clubes
    ├── Personas/          + FichaBasicaPruebas, FichaBasicaPorRolPruebas, FichaBasicaEntreClubesPruebas,
    │                        FotoDePersonaPruebas, EnlacesDeFichaPruebas, MiCategoriaConFotoPruebas
    ├── Cuenta/            + DatosPropiosPruebas, DatosPropiosRechazosPruebas
    ├── Categorias/        ~ MiCategoriaPruebas                 # ya no afirma que no hay identificador
    └── Aislamiento/       ~ AccesoClubPruebas                  # 48 de club; contrato de 72

frontend/src/
├── App.tsx                ~ ruta `personas/:usuarioRolId`
├── compartido/
│   ├── api/               + tiposPersonas.ts                   # FichaBasicaDto, EnlaceFichaDto, DatosPropiosDto
│   │                      + tiposIngresos.ts                   # sale de tipos.ts, para que no crezca
│   │                      ~ tiposCategorias.ts, tiposFicha.ts (campos nuevos)
│   ├── fotos/             + fotosEnMemoria.ts                  # sale de privado/jugadores/fotosJugador.ts
│   ├── navegacion/        + useVolver.ts
│   └── componentes/       + NombrePersona.tsx
├── cuenta/                ~ MiPerfil.tsx                       # compone las secciones
│                          + SeccionFotoPerfil.tsx, SeccionDatosPropios.tsx, AccesosMiFicha.tsx
├── privado/
│   ├── personas/          + FichaBasica.tsx, FotoPersona.tsx
│   ├── jugadores/         ~ fotosJugador.ts (usa fotosEnMemoria), TablaDeGrupo.tsx (useVolver)
│   ├── ficha/             ~ FichaJugador.tsx (useVolver), UltimoCambio.tsx (NombrePersona)
│   ├── categorias/        ~ Categorias.tsx, SeccionEntrenadores.tsx, SeccionJugadores.tsx,
│   │                        SeccionSinCategoria.tsx, SeccionRetirados.tsx
│   ├── ingresos/          ~ SeccionIngresosAprobados.tsx, SeccionSalaDeEspera.tsx
│   ├── inicio/            ~ TarjetaMiJugador.tsx (el nombre abre "Mi ficha")
│   └── ~ TarjetaMiCategoria.tsx                          # miniatura y enlace de cada entrenador
└── (frontend/pruebas/)    + NombrePersona.test.tsx, useVolver.test.ts, fotosEnMemoria.test.ts
                           ~ fotosJugador.test.ts
```

**Decisión de estructura**: la de la 001, con carpetas nuevas `privado/personas/`,
`compartido/fotos/`, `compartido/navegacion/` y, en las pruebas, `Integracion/Personas/`.

### Pantallas

| Ruta | Qué cambia | Quién |
| --- | --- | --- |
| `/club/:clubId/personas/:usuarioRolId` | **Nueva**: ficha básica con foto grande, nombre, rol y categorías con equipos | Todos los roles del club |
| `/club/:clubId` (inicio de la familia) | Entrenadores con miniatura y enlace; el nombre del jugador abre "Mi ficha" | JUGADOR |
| `/club/:clubId/categorias` y su detalle | Nombres de entrenadores como enlace | PRESIDENTE, DIRECTIVO, ENTRENADOR |
| `/club/:clubId/categorias` (retirados) | "Retirado por" como enlace si sigue en el club | PRESIDENTE, DIRECTIVO |
| `/club/:clubId/ingresos` | Persona aprobada, "aprobado por" y "hermano de" como enlaces | PRESIDENTE |
| `/club/:clubId/jugadores/:id/ficha` | "Último cambio por" como enlace; volver a la pantalla de origen | Quien ya ve la ficha |
| `/perfil` | Datos propios con cuatro campos editables, documento y correo de solo lectura; o "Mi ficha" para la cuenta de un jugador | Toda cuenta de club |

## Trazabilidad

| Historia | Requisitos | Backend | Pantallas | Pruebas clave |
| --- | --- | --- | --- | --- |
| 1. Ficha básica | RF-001 a 009, 020 | `ControladorPersonas`, `ServicioFichaBasica`, `RepositorioFichaBasica`, `MapperFichaBasica`, `ObtenerDeIntegranteAsync` | `FichaBasica`, `FotoPersona` | Contenido exacto por rol; sin datos privados en el JSON; desactivados y retirados no salen; JUGADOR, otro club y DESARROLLADOR 404; en espera y retirado 403; dos clubes; foto de jugador privada |
| 2. Nombres que llevan a la ficha | RF-010 a 013, 020 | `EnlaceFichaDto`, `ReglaEnlaceDeFicha`, `ResolutorDeEnlaces`, `RepositorioEnlaces`, mappers que cambian | `NombrePersona`, `useVolver`, las pantallas de la tabla de research §6 | Enlace presente o `null` en cada campo nuevo según la persona siga o no; la familia no abre fichas de otros jugadores aunque tenga un identificador |
| 3. Miniaturas de la familia | RF-014 | `ServicioMiCategoria`, `EntrenadorParaFamiliaDto` | `TarjetaMiCategoria`, `FotoPersona` | `usuarioRolId` y `versionFoto` de cada entrenador; jugador sin categoría igual que antes |
| 4. Mi perfil | RF-015 a 019 | `ControladorDatosPropios`, `ServicioDatosPropios`, `ValidadorDatosPropios`, `CambiarDatosPropiosAsync` | `MiPerfil`, `SeccionDatosPropios`, `AccesosMiFicha` | Cambio en todos los clubes; rechazos sin cambios; documento y correo intactos; jugadores de la cuenta intactos; cuenta solo de jugador 404 |
| Transversal | RF-020, CE-007 | Aislamiento | Todas, a 360 px y en los dos temas | Contrato de 72; 48 de club con 404 al integrante de otro club y al DESARROLLADOR; la 001 a la 007 en verde |

## Orden de construcción sugerido

Para `/speckit-tasks`. Cada bloque deja algo que se puede probar de extremo a extremo (§27.1).

1. **Base**: `TipoFicha`, `EnlaceFichaDto`, el sembrador de personas, `NombrePersona`, `useVolver`
   y la generalización de la caché de fotos (la 007 sigue en verde).
2. **Historia 1**: la ficha básica y su foto, con su pantalla y la ruta; documentación de la foto
   de perfil según §14.2 4.5.0.
3. **Historia 3**: "Mi categoría" con identificador y versión de foto; miniaturas y enlaces en el
   inicio de la familia. Va antes que la 2 porque da el primer camino real a la ficha básica.
4. **Historia 2**: `ReglaEnlaceDeFicha`, `ResolutorDeEnlaces` y los cinco campos nuevos; los
   nombres de todas las pantallas de research §6 con `NombrePersona` y el volver genérico.
5. **Historia 4**: datos propios (lectura y cambio) y "Mi perfil" dividido en secciones.
6. **Cierre**: contrato contra Swagger (72), aislamiento (48), revisión a 360 px en los dos temas,
   recorrido de [quickstart.md](quickstart.md) en un proyecto de Compose aparte y batería entera.

## Seguimiento de complejidad

| Desviación | Por qué se necesita | Alternativa más simple descartada |
| --- | --- | --- |
| §12.3 dice "JUGADOR: mediante `Jugador.UsuarioId`", pero no hay entidad `Jugador`: el jugador es el `UsuarioRol` con rol JUGADOR (heredada de la 003 a la 007) | La ficha básica y los enlaces usan el `UsuarioRol` como hasta ahora | Crear una entidad `Jugador`: una tabla y una migración de datos sin necesidad (§19) |
