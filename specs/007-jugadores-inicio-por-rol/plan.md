# Plan de implementación: Jugadores, inicio por rol e identidad del club por el presidente

**Rama**: `007-jugadores-inicio-por-rol` | **Fecha**: 2026-10-10 | **Spec**: [spec.md](spec.md)

**Entrada**: especificación de la funcionalidad en `specs/007-jugadores-inicio-por-rol/spec.md`

## Resumen

El PRESIDENTE, los DIRECTIVOS y los ENTRENADORES tienen en el menú una lista "Jugadores" con los
jugadores activos agrupados por categoría (el ENTRENADOR, solo los de sus categorías), cada uno con
su foto en miniatura, su nombre, sus equipos y una columna "Al día" reservada. Al abrir a un
jugador se ve su ficha de la 005 con la foto en grande. Cada jugador tiene una foto propia, que
cargan la familia y el PRESIDENTE y que se borra al dejar el club. El inicio cambia por rol:
resumen del club, categorías del entrenador o el jugador de la familia, todos con "Próximos
entrenamientos" reservado. El PRESIDENTE cambia el escudo y los colores de su club desde "Datos
del club", con la misma vista previa y el mismo aviso de contraste que el panel.

Enfoque técnico: una tabla nueva, `FotosJugador`, con la forma de las que ya guardan el escudo y la
foto de perfil, y su acceso resuelto por la regla de la ficha (`AccesoAFicha`), sin regla nueva. Cinco
operaciones de API para la lista, la foto (ver, cargar y quitar) y el resumen, y dos para que el PRESIDENTE cambie la
identidad, que reutilizan el servicio y los validadores del panel. El detalle del jugador es la
ficha que ya existe; el inicio del ENTRENADOR y el de la familia se arman con endpoints que ya
existen. El formulario de identidad del panel pasa a ser un componente compartido. Las decisiones
y sus alternativas están en [research.md](research.md).

## Contexto técnico

**Lenguaje y versión**: C# 14 sobre .NET 10 en el backend; TypeScript 5 sobre Node 22 en el
frontend. Sin cambios respecto de la 001.

**Dependencias principales**: las de la 001 (ASP.NET Core 10, Entity Framework Core 10 con Npgsql,
JWT, React 19, React Router, Vite). No se añade ninguna: ni biblioteca de imágenes ni almacén de
archivos (research §1 y §13).

**Almacenamiento**: PostgreSQL 17. Una migración, `FotosDeJugadores`, que crea `FotosJugador`. El
resumen y la lista no se guardan: se calculan al consultar.

**Pruebas**: xUnit (unitarias e integración con `WebApplicationFactory` y Testcontainers); Vitest
en el frontend.

**Plataforma de destino**: la de la 001: API en contenedor Linux y aplicación web adaptable.

**Tipo de proyecto**: aplicación web con `backend/` y `frontend/` separados (§4).

**Objetivos de rendimiento**: la lista hace dos consultas (jugadores con categoría y equipos, y
fechas de foto), no una por jugador. Las fotos se piden solo al entrar en pantalla (research §13).

**Restricciones**: código y documentación en español (§2.1, §3); ningún archivo escrito a mano por
encima de 250 líneas (§2.3); toda autorización y todo aislamiento en el servidor (§15, RF-003,
RF-021); backend y pantalla de cada historia en la misma tarea (§27.1); ningún dato que parezca un
estado de pago o un entrenamiento (RF-006, RF-014, CE-007).

**Escala y alcance**: decenas de jugadores por club (§19). Esta funcionalidad tiene 1 entidad
nueva; 7 operaciones de API nuevas y 2 que cambian (el contrato pasa de 61 a 68); unos 15
componentes y archivos de frontend nuevos y 8 que cambian.

## Comprobación de la constitución

*Puerta: debe pasar antes de la fase 0 y se revisa de nuevo tras el diseño de la fase 1.*

Evaluada contra la versión **4.4.0**.

| Principio | Cómo lo cumple el plan | Resultado |
| --- | --- | --- |
| §2.1 y §2.2 Español y vocabulario | `FotoJugador`, `ServicioListaJugadores`, `ResumenClubDto`, `EditorIdentidad`; "jugador", "entrenador", "categoría", "equipo", sin Alumno ni Acudiente | Cumple |
| §2.3 Máximo 250 líneas | Lo nuevo va en archivos propios; `DisposicionClub.tsx` (209) solo gana un enlace; `tipos.ts` (237) no crece: los tipos nuevos van en `tiposJugadores.ts`; `SeccionIdentidad.tsx` se adelgaza al sacar el editor | Cumple |
| §3 Documentación XML | En cada clase nueva, y se reescribe donde cambia la responsabilidad (`ServicioIdentidadClub`, `IServicioIdentidadClub`, `IRepositorioEscudos`, `ControladorIdentidadClub`, `ServicioRetiroJugador`, `UsuarioRol`, `FichaJugador`, `ConfiguracionClub.tsx`) | Cumple |
| §4 y §5 Capas y dependencias | Controladores que solo delegan; servicios con interfaces; repositorios sin reglas; la lista se limita con la regla de dominio que ya existe (`ReglaAlcanceDeCategorias`) | Cumple |
| §6 Tecnologías base | Ninguna nueva; se descartan expresamente las miniaturas en el servidor | Cumple |
| §7.1 Aislamiento entre clubes | Todo bajo `/api/clubes/{clubId}` con el filtro activo; `FotosJugador` implementa `IPerteneceAClub`; el PRESIDENTE guarda la identidad del club del contexto, nunca de un identificador recibido | Cumple |
| §7.2 Identidad del club | El PRESIDENTE cambia escudo y colores desde "Datos del club"; el DESARROLLADOR, desde su panel, con el mismo servicio y los mismos validadores | Cumple |
| §7.3 Varios clubes | El inicio y el menú salen del rol en el club elegido | Cumple |
| §7.5 Aislamiento dentro del club | El ENTRENADOR solo recibe sus categorías en la lista, la ficha y la foto, consultando la asignación en cada petición; la familia, solo su jugador | Cumple |
| §8 Roles | "Jugadores" para PRESIDENTE, DIRECTIVO y ENTRENADOR; foto por PRESIDENTE y familia; identidad por PRESIDENTE; el DESARROLLADOR no ve ni la lista ni la foto | Cumple |
| §9 Sitio público | La foto no tiene ruta pública | Cumple |
| §12.2 y §12.3 Roles y alcance | El inicio se elige por rol; la asignación de un PRESIDENTE o DIRECTIVO no se lo cambia | Cumple, con la desviación heredada (Seguimiento de complejidad) |
| §13 Histórico | El autor del último cambio de la ficha se copia al cargar o quitar la foto | Cumple |
| §14 y §14.2 Foto y eliminación física | La foto se borra físicamente al retirar al jugador, de las tres formas; los demás casos de §14.2 se reparten en research §6 | Cumple (nota 2) |
| §15 Reglas en backend | Quién ve la lista, quién ve y cambia la foto y quién cambia la identidad se deciden en la API; la pantalla solo esconde | Cumple |
| §19 Simplicidad | Una tabla; el detalle, el inicio del ENTRENADOR y el de la familia reutilizan endpoints existentes; ninguna regla nueva | Cumple |
| §20 Pruebas | Las nuevas de §20 sobre la foto (quién la cambia, quién la ve, que es distinta de la de perfil, que se borra al retirar y que se reincorpora sin ella) y sobre la identidad (solo el PRESIDENTE de ese club y el DESARROLLADOR) | Cumple |
| §22 Migraciones | Una migración de Entity Framework | Cumple |
| §23 API | HTTP semántico, DTOs, `problem+json`, Swagger; la lista no expone documento, contacto ni salud | Cumple |
| §24 Diseño visual | Teléfono y escritorio, temas claro y oscuro; "Al día" no usa los colores de estado de pago | Cumple, con la nota 1 |
| §25 Decisiones no tomadas | Cinco detalles que la spec no fija quedan como supuestos en research.md | Cumple, con supuestos por confirmar |
| §27 Definición de terminado | Tareas por historia, cada una con backend, pantalla y pruebas | Cumple |
| §28 Decisiones pendientes | No se resuelve ninguna | Cumple |

**Resultado de la puerta**: pasa.

**Nota 1**: no he podido abrir el lienzo de §24 desde esta sesión. Las pantallas se componen con los
componentes ya aprobados (`Tarjeta`, `Tabla`, `EtiquetaEstado`); si el lienzo trae la lista de
jugadores o un inicio por rol, se contrasta con él al revisar las pantallas.

**Nota 2**: el borrado de la foto cuando un jugador queda retirado al aprobarse un hermano en otro
club se añade a `IRepositorioRetiroEntreClubes.RetirarDeOtrasCuentasAsync`, que construyó la 006
(research §14). La 006 quedó terminada el 2026-10-10.

**Revisión tras el diseño**: el modelo de datos y el contrato no añaden excepciones al aislamiento,
tecnologías ni patrones respecto de lo evaluado. La única tabla nueva queda bajo el filtro de club;
las siete operaciones nuevas son de `/api/clubes/{clubId}` y entran en las pruebas de aislamiento
existentes. La puerta sigue pasando.

## Estructura del proyecto

### Documentación de esta funcionalidad

```text
specs/007-jugadores-inicio-por-rol/
├── plan.md              # Este archivo
├── research.md          # Decisiones técnicas y supuestos del plan
├── data-model.md        # FotoJugador y los cálculos de la lista y el resumen
├── quickstart.md        # Guía de validación de extremo a extremo
├── contracts/
│   └── api.yaml         # Contrato OpenAPI: siete endpoints nuevos y dos que cambian
└── tasks.md             # Lo genera /speckit-tasks
```

### Código fuente

Solo se listan los archivos nuevos (`+`) y los que cambian (`~`). La estructura es la de la 001.

```text
backend/src/
├── LFClub.Dominio/
│   └── Entidades/         + FotoJugador.cs
│                          ~ UsuarioRol.cs, FichaJugador.cs, Club.cs, EscudoClub.cs (solo documentación)
├── LFClub.Aplicacion/
│   ├── DTOs/              + GrupoDeJugadoresDto, CategoriaDeGrupoDto, JugadorDeListaDto, ResumenClubDto
│   │                      ~ FichaJugadorDto (FotoActualizadaEn)
│   ├── Interfaces/        + IRepositorioFotosJugador, IRepositorioResumenClub
│   │                      ~ IRepositorioJugadores (ListarDelClubAsync), IRepositorioEscudos (GuardarAsync)
│   ├── Servicios/         + IServicioListaJugadores, IServicioFotoJugador, IServicioResumenClub
│   │                      ~ IServicioIdentidadClub (entradas del propio club)
│   ├── Implementaciones/  + ServicioListaJugadores, ServicioFotoJugador, ServicioResumenClub
│   │                      ~ ServicioIdentidadClub, ServicioRetiroJugador (borra la foto),
│   │                        ServicioConsultaFicha (fecha de la foto)
│   ├── Mappers/           + MapperListaJugadores
│   │                      ~ MapperFicha (FotoActualizadaEn)
│   └── Utilidades/        ~ ErroresDeFicha (foto_no_es_imagen, foto_demasiado_grande, jugador_retirado)
├── LFClub.Infraestructura/
│   ├── Datos/
│   │   ├── ~ ContextoLFClub.cs            # DbSet FotosJugador
│   │   ├── Configuraciones/  + ConfiguracionFotoJugador
│   │   └── Migraciones/      + FotosDeJugadores (generada)
│   └── Repositorios/      + RepositorioFotosJugador, RepositorioResumenClub
│                          ~ RepositorioJugadores, RepositorioEscudos,
│                            Plataforma/RepositorioRetiroEntreClubes (borra las fotos que retira)
└── LFClub.Api/
    ├── Configuracion/     ~ RegistroDeCasosDeUso, RegistroDeServicios
    └── Controladores/
        ├── Club/          + ControladorFotoJugador, ControladorIdentidadDelClub
        │                  ~ ControladorJugadores (GET de la lista), ControladorClub (GET resumen)
        └── Plataforma/    ~ ControladorIdentidadClub (solo documentación: ya no es el único)

backend/pruebas/
├── Unitarias/
│   └── Utilidades/        + OrdenDeGruposPruebas            # si el orden se extrae a una utilidad
└── Integracion/
    ├── Base/              + SembradorJugadores              # club con categorías, equipos, asignaciones
    ├── Jugadores/         + ListaJugadoresPruebas, ListaDelEntrenadorPruebas
    │                      + FotoJugadorPruebas, QuienVeLaFotoPruebas, FotoAlRetirarPruebas
    ├── Inicio/            + ResumenClubPruebas
    ├── Identidad/         + IdentidadPorElPresidentePruebas
    ├── Club/              ~ IdentidadPruebas (el panel sigue igual; ya no afirma que el presidente no puede)
    └── Aislamiento/       ~ AccesoClubPruebas               # 46 de club; contrato de 68

frontend/src/
├── App.tsx                ~ rutas `jugadores`
├── compartido/
│   ├── api/               + tiposJugadores.ts               # GrupoDeJugadoresDto, JugadorDeListaDto, ResumenClubDto
│   │                      ~ tiposFicha.ts (fotoActualizadaEn)
│   ├── componentes/       + EditorIdentidad.tsx             # sale de SeccionIdentidad
│   │                      ~ Iconos.tsx (IconoJugadores)
│   └── sesion/            ~ ProveedorSesion.tsx             # vacía la caché de fotos al cerrar sesión
├── plataforma/            ~ SeccionIdentidad.tsx            # usa EditorIdentidad
├── privado/
│   ├── ~ DisposicionClub.tsx                 # enlace "Jugadores"
│   ├── ~ InicioClub.tsx                      # elige el inicio por rol
│   ├── ~ ConfiguracionClub.tsx               # EditorIdentidad con las rutas del club
│   ├── inicio/            + ResumenDelClub.tsx, InicioEntrenador.tsx, TarjetaMiJugador.tsx,
│   │                        TarjetaProximosEntrenamientos.tsx, TarjetaTuClub.tsx
│   ├── jugadores/         + ListaJugadores.tsx, TablaDeGrupo.tsx, FotoJugador.tsx,
│   │                        fotosJugador.ts (caché y carga perezosa), textos.ts ("Al día")
│   └── ficha/             + SeccionFoto.tsx
│                          ~ FichaJugador.tsx                # foto arriba; "← Jugadores" al venir de la lista
└── (frontend/pruebas/)    + fotosJugador.test.ts, textosJugadores.test.ts
```

**Decisión de estructura**: la de la 001, con carpetas nuevas `privado/jugadores/`,
`privado/inicio/` y, en las pruebas, `Integracion/Jugadores/`, `Inicio/` e `Identidad/`.
`InicioClub.tsx` queda como un selector corto; cada inicio vive en su archivo.

### Pantallas

| Ruta | Qué cambia | Quién |
| --- | --- | --- |
| Menú del club | Enlace "Jugadores" | PRESIDENTE, DIRECTIVO, ENTRENADOR |
| `/club/:clubId/jugadores` | **Nueva**: grupos por categoría con foto, nombre, equipos y "Al día" reservado | PRESIDENTE, DIRECTIVO, ENTRENADOR |
| `/club/:clubId/jugadores/:usuarioRolId/ficha` | Foto en grande arriba; cargar, cambiar y quitar si `puedeCambiar` y no está retirado; "← Jugadores" al venir de la lista | Todos los que ya ven la ficha |
| `/club/:clubId` (inicio) | Resumen del club; categorías del entrenador; tarjeta del jugador; "Próximos entrenamientos" para todos | Cada rol el suyo |
| `/club/:clubId/configuracion` | Sección de identidad con vista previa y aviso de contraste | PRESIDENTE |
| `/plataforma/clubes/:clubId` | Sin cambios visibles: usa el editor compartido | DESARROLLADOR |

## Trazabilidad

| Historia | Requisitos | Backend | Pantallas | Pruebas clave |
| --- | --- | --- | --- | --- |
| 1. La lista de jugadores | RF-001 a 006, 023 | `ServicioListaJugadores`, `MapperListaJugadores`, `ListarDelClubAsync`, `FechasAsync` | Enlace del menú, `ListaJugadores`, `TablaDeGrupo`, "Al día" | Exactamente los activos; orden; "Sin categoría" al final y no para el ENTRENADOR; dos equipos y "Sin equipo"; sin retirados ni en espera; ENTRENADOR sin categorías; JUGADOR `403`; sin datos de ficha ni de pago |
| 2. El detalle y la foto | RF-007 a 010, 023 | `ServicioFotoJugador`, `ControladorFotoJugador`, `RepositorioFotosJugador`, `ServicioConsultaFicha`, `ServicioRetiroJugador` | `SeccionFoto`, `FotoJugador`, volver a la lista | Carga por familia y PRESIDENTE; rechazos conservan la anterior; sello; hermanos y foto de perfil independientes; DIRECTIVO y ENTRENADOR `403`; fuera de alcance `404`; retiro borra; reincorporado sin foto; retirado `409` |
| 3. El inicio por rol | RF-011 a 016 | `ServicioResumenClub`, `RepositorioResumenClub` | `InicioClub`, `ResumenDelClub`, `InicioEntrenador`, `TarjetaMiJugador`, `TarjetaProximosEntrenamientos` | Cuatro cifras exactas con asignaciones, desactivados, retirados y en espera; club nuevo en cero; ENTRENADOR y JUGADOR `403` en el resumen |
| 4. Identidad por el PRESIDENTE | RF-017 a 022 | `ControladorIdentidadDelClub`, `ServicioIdentidadClub`, `IRepositorioEscudos.GuardarAsync` | `EditorIdentidad` en "Datos del club" y en el panel | Cambia y lo ve el panel; rechazos; otros roles `403`; otro club `404`; club suspendido; último cambio gana; otro club intacto |
| Transversal | RF-023, CE-008 | Aislamiento | Todas, a 360 px y en los dos temas | Contrato de 68; 46 de club con `404` al integrante de otro club y al DESARROLLADOR; la 001 a la 006 en verde |

## Orden de construcción sugerido

Para `/speckit-tasks`. Cada bloque deja algo que se puede probar de extremo a extremo (§27.1).

1. **Base**: la entidad `FotoJugador`, su configuración, la migración y el repositorio; el sembrador
   de pruebas con categorías, equipos, asignaciones y jugadores en todos los estados. Las pruebas
   anteriores siguen en verde.
2. **Historia 1**: la lista con su servicio, su mapper y su endpoint; el enlace del menú y la
   pantalla, con iniciales en todas las filas (la foto aún no existe) y "Al día" reservado.
3. **Historia 2**: las tres operaciones de la foto, la fecha en la ficha, el borrado al retirar (en
   `ServicioRetiroJugador` y en `RetirarDeOtrasCuentasAsync`, research §6 y §14); la
   sección de foto en la ficha, la miniatura en la lista con carga perezosa y caché, y el volver a
   la lista.
4. **Historia 4**: las dos operaciones de identidad del PRESIDENTE; el editor compartido en el panel
   y en "Datos del club". Va antes que la 3 porque no depende de ella y cierra la contradicción con
   la spec 001.
5. **Historia 3**: el resumen y su endpoint; los cuatro inicios y "Próximos entrenamientos".
6. **Cierre**: contrato contra Swagger (68), aislamiento (46), revisión a 360 px en los dos temas,
   recorrido de [quickstart.md](quickstart.md) en un proyecto de Compose aparte y batería entera.

## Seguimiento de complejidad

| Desviación | Por qué se necesita | Alternativa más simple descartada |
| --- | --- | --- |
| §12.3 dice "JUGADOR: mediante `Jugador.UsuarioId`", pero no hay entidad `Jugador`: el jugador es el `UsuarioRol` con rol JUGADOR (heredada de la 003 a la 006) | La foto cuelga del `UsuarioRol` del jugador, como su ficha y sus documentos | Crear una entidad `Jugador` solo para colgar la foto: una tabla y una migración de datos sin necesidad (§19) |
