# Plan de implementación: mensualidades, cargos y pagos

**Rama**: `015-mensualidades-cargos-pagos` | **Fecha**: 2026-10-20 | **Spec**: [spec.md](spec.md)

**Entrada**: especificación en `specs/015-mensualidades-cargos-pagos/spec.md`, con la constitución
**4.14.0** ya enmendada (§6.2, §8, §16 y §20) y las cuatro clarificaciones de la sesión del
2026-10-20.

## Resumen

El club cobra sin pasarela. Cada mes la aplicación genera sola un cargo MENSUALIDAD por jugador
activo; la familia ve su estado de cuenta, paga al PRESIDENTE (en mano o por transferencia a la
entidad que el club publica) y, si fue por transferencia, la informa con una imagen de soporte. El
PRESIDENTE registra, confirma, rechaza o anula pagos; al confirmar, el pago queda aplicado a los
cargos, el jugador pasa a "al día" y se generan el recibo (numerado por club) y el resguardo (imagen).
El PRESIDENTE y el DIRECTIVO ven las finanzas del club; el ENTRENADOR no ve nada de esto.

Enfoque técnico: **cinco tablas nuevas y una migración** (`Cargos`, `Pagos`, `AplicacionesPago`,
`SoportesPago`, `MediosPagoClub` y cuatro columnas en `Clubes` y `UsuariosRol`). El saldo y el estado no
se guardan: se calculan de cargos y aplicaciones. La generación mensual es **una sola sentencia SQL
idempotente** (índice único por jugador y mes) que corre en un servicio en segundo plano y otra vez
al fijar el valor. Las reglas (mes de cobro, estado de cuenta, aplicación de un pago, acceso) son
puras en el dominio. El resguardo se **dibuja a demanda** con SkiaSharp, como la carta, a partir de
los datos del pago; no se guarda. En el frontend, cuatro pantallas privadas, una tarjeta en el inicio
y la columna "Al día" real. Las decisiones están en [research.md](research.md).

## Contexto técnico

**Lenguaje y versión**: C# 14 sobre .NET 10 en el backend; TypeScript 5 sobre Node 22 en el
frontend. Sin cambios respecto de la 001.

**Dependencias principales**: las de la 001, incluida SkiaSharp 3.119 (ya usada por la carta).
**No se añade ninguna.**

**Almacenamiento**: PostgreSQL 17. **Una migración** (`MensualidadesYPagos`): cinco tablas, una
columna de valor y otra de contador en `Clubes`, y dos columnas en `UsuariosRol`
([data-model.md](data-model.md)). Los soportes van en la base como las fotos (`bytea`), en su tabla.

**Pruebas**: xUnit (unitarias para las reglas puras; integración con `WebApplicationFactory` y
Testcontainers, con `IReloj` sustituible); Vitest en el frontend. Se actualiza el guardián de esquema
de la 014.

**Plataforma de destino**: la de la 001: API en contenedor Linux y aplicación web adaptable, también
empaquetada con Capacitor para Android.

**Tipo de proyecto**: aplicación web con `backend/` y `frontend/` separados (§4).

**Objetivos de rendimiento**: un club tiene decenas de jugadores; una cuenta, a lo sumo decenas de
cargos al año. La generación mensual es una sentencia por ejecución. El estado de la columna "Al día"
sale de una sola consulta agrupada por jugador, sin una consulta por fila.

**Restricciones**: código y documentación en español (§2.1, §3); ningún archivo escrito a mano por
encima de 250 líneas (§2.3); toda regla en el servidor y el ENTRENADOR fuera de todo (§15, §16.9);
dinero siempre `decimal` (§16.2); pago confirmado inmutable (§16.3); sin recordatorios (§16.8);
backend y pantalla en la misma tarea (§27.1); sin pasarela para las familias (§6.2).

**Escala y alcance**: 5 entidades nuevas; el contrato pasa de 113 a 131 operaciones y las de club de
87 a 105 (las 18 son de club); unos 25 archivos de frontend nuevos y 6 que cambian.

## Comprobación de la constitución

*Puerta: debe pasar antes de la fase 0 y se revisa de nuevo tras el diseño de la fase 1.*

Evaluada contra la versión **4.14.0**, que ya recoge toda la funcionalidad.

| Principio | Cómo lo cumple el plan | Resultado |
| --- | --- | --- |
| §2.1 y §2.2 Español y vocabulario | `ServicioCuentaDelJugador`, `ReglaEstadoDeCuenta`, "cargo", "pago", "recibo", "resguardo", "soporte" | Cumple |
| §2.3 Máximo 250 líneas | Servicios y repositorios divididos por tarea (consulta, registro, revisión, anulación, finanzas); `verificar-tamano` al empezar | Cumple |
| §3 Documentación XML | En cada clase nueva, con lo que no debe asumir | Cumple |
| §4 y §5 Capas | Reglas puras en `Dominio/Reglas`; servicios en `Aplicacion`; SQL y dibujo en `Infraestructura`; los controladores delegan | Cumple |
| §6 y §19 Tecnologías y simplicidad | Sin dependencias nuevas; reutiliza SkiaSharp, `ValidadorImagen`, `IUnidadDeTrabajo`; sin tabla de saldos ni de historial de estados | Cumple |
| §6.2 y §16.4 Sin pasarela | Ninguna llamada a Wompi; el dinero va al PRESIDENTE y la aplicación solo registra | Cumple |
| §7.1 Aislamiento entre clubes | Todas las entidades implementan `IPerteneceAClub` y heredan el filtro; la generación mensual cruza clubes con `IgnoreQueryFilters` sin devolver datos (nota 3) | Cumple, con la nota 3 |
| §7.2 Configuración del club | El valor de la mensualidad es una columna de `Clubes`, no código | Cumple |
| §7.4 Club suspendido | La generación sigue en un club suspendido (RF-008); solo entra su PRESIDENTE | Cumple |
| §8 y §16.9 Roles | `IntegranteDelClub` deja fuera al ENTRENADOR (403); la familia solo ve su jugador (404 en otro); el DESARROLLADOR no tiene ruta | Cumple |
| §12.1 y §14.1 Ingreso y retiro | `CobraDesde` se fija al entrar, aprobar o reincorporar; el retiro no borra cargos ni pagos | Cumple |
| §12.4 Eliminación de cuenta | Pagos y recibos se conservan con nombre y documento copiados; soportes se borran; cargos caen en cascada | Cumple |
| §13 Histórico | Cada cargo guarda su valor y su categoría; cada pago guarda nombre, documento y concepto | Cumple |
| §15 Reglas en backend | Autorización, importes, estados y topes en el servidor; el frontend solo los refleja | Cumple |
| §16.1 a §16.9 | Un cargo por jugador y mes; saldo calculado; abonos; inmutabilidad; sin mora ni recordatorios | Cumple |
| §18 Estados | Solo el estado actual del pago, con quién y cuándo de cada paso (excepción expresa de §16.3) | Cumple |
| §20 Pruebas | Generación y saldo, aplicación y anulación, autorización por rol, ENTRENADOR fuera, soporte privado | Cumple |
| §22 Migraciones | Una migración EF Core | Cumple |
| §23 API | DTOs propios; `problem+json` con códigos nuevos; `multipart` solo en el soporte | Cumple |
| §24 Diseño visual | Teléfono y escritorio, dos temas, estados por texto además del color | Cumple, con la nota 5 |
| §25 y §28 Decisiones | No se resuelve ninguna decisión pendiente; las dudas van como notas | Cumple |
| §27 Definición de terminado | Tareas por historia con backend, pantalla y pruebas | Cumple |

**Resultado de la puerta**: pasa. Sin violaciones; no hay nada que justificar en el seguimiento de
complejidad.

**Nota 1 (para el propietario) — meses sin cobrar nunca se recuperan.** La generación solo crea el
cargo del mes en curso. Si el club no tenía valor definido en octubre y lo fija en diciembre, octubre y
noviembre no se cobran en la aplicación (RF-008, "no hay cargos retroactivos"). Es lo que dice la
spec; el PRESIDENTE puede registrar esa deuda fuera de la aplicación. Si se quiere recuperar meses, es
una decisión nueva.

**Nota 2 (para el propietario) — jugadores que ya existen.** Los jugadores de hoy reciben su primer
cargo **en el mes de la puesta en producción**, no el siguiente: la migración les pone `CobraDesde` en
el primer día de ese mes. Y la migración deja en 65.000 el valor de Valfor F.C. (§16.7); los demás
clubes quedan sin valor y su PRESIDENTE verá el aviso hasta que lo defina
([research.md](research.md) §5).

**Nota 3 (de constitución)**: la generación mensual y el contador de recibos tocan filas de todos los
clubes en una operación del sistema, no de una persona. Es la misma clase de excepción acotada que ya
usa el panel de la plataforma (§7.1): no hay ruta, no devuelve datos de ningún club y solo escribe
cargos. Si el propietario quiere que §7.1 la liste, es una enmienda de redacción.

**Nota 4**: la §16.5 dice que el resguardo se "descarga o comparte". El plan lo cumple con la
descarga del PNG y, donde el navegador o Capacitor lo permiten, con el menú de compartir; no hay
enlaces públicos (§14.2).

**Nota 5**: el lienzo no tiene estas pantallas ni el diseño del resguardo. Su diseño sigue
[research.md](research.md) §14 con los tokens y componentes del proyecto y lo revisa el propietario en
la validación.

**Nota 6 (ajuste a la spec)**: la spec decía que la familia podía elegir cargos "al informar o al
confirmar". El plan la deja elegir **solo al confirmar** (el PRESIDENTE): la familia informa valor,
fecha, entidad y soporte. Es más simple y evita pagos informados que apuntan a cargos que ya cambiaron.
Se corrigió el supuesto de la spec.

**Revisión tras el diseño**: cinco tablas y ninguna dependencia nueva; 18 operaciones nuevas de club;
las demás sin cambios salvo `JugadorDeListaDto`, que gana un campo nulo. La puerta sigue pasando.

## Estructura del proyecto

### Documentación de esta funcionalidad

```text
specs/015-mensualidades-cargos-pagos/
├── plan.md              # Este archivo
├── research.md          # Decisiones técnicas, hallazgos del código y notas al propietario
├── data-model.md        # Tablas, columnas, reglas y qué hace cada una al eliminar una cuenta
├── quickstart.md        # Recorrido de validación
├── contracts/
│   └── api.yaml         # Las 18 operaciones nuevas
├── checklists/
│   └── requirements.md  # (de /speckit-specify)
└── tasks.md             # Fase 2 (/speckit-tasks; no lo crea este comando)
```

### Código fuente (raíz del repositorio)

```text
backend/
├── src/
│   ├── LFClub.Dominio/
│   │   ├── Entidades/
│   │   │   ├── Cargo.cs, Pago.cs, AplicacionPago.cs, SoportePago.cs, MedioPagoClub.cs   # NUEVAS
│   │   │   ├── Club.cs                      # CAMBIA: ValorMensualidad, UltimoRecibo
│   │   │   └── UsuarioRol.cs                # CAMBIA: ValorMensualidadPropio, CobraDesde
│   │   ├── Enumeraciones/
│   │   │   ├── TipoCargo.cs, MedioPago.cs, EstadoPago.cs, EstadoDeCuenta.cs             # NUEVAS
│   │   └── Reglas/
│   │       ├── ReglaMesDeCobro.cs           # NUEVA: mes en Colombia y primer mes de cobro
│   │       ├── ReglaEstadoDeCuenta.cs       # NUEVA: saldo y al día / parcial / debe
│   │       ├── ReglaAplicacionDePago.cs     # NUEVA: a qué cargos y cuánto cubre un pago
│   │       ├── ReglaAccesoACuenta.cs        # NUEVA: quién ve y quién opera la cuenta
│   │       └── ReglaTransicionPago.cs       # NUEVA: en revisión → confirmado / rechazado → anulado
│   ├── LFClub.Aplicacion/
│   │   ├── DTOs/                            # NUEVOS: Mensualidad*, MedioPago*, CuentaJugador*, Pago*, Finanzas*
│   │   ├── Interfaces/
│   │   │   ├── IRepositorioCargos.cs, IRepositorioPagos.cs, IRepositorioMediosPago.cs,
│   │   │   │   IRepositorioFinanzas.cs       # NUEVAS
│   │   │   └── IGeneradorImagenResguardo.cs # NUEVA
│   │   ├── Servicios/
│   │   │   ├── IServicioMensualidad.cs, IServicioMediosPago.cs, IServicioCuentaJugador.cs,
│   │   │   │   IServicioRegistroDePagos.cs, IServicioRevisionDePagos.cs,
│   │   │   │   IServicioConsultaDePagos.cs, IServicioFinanzas.cs,
│   │   │   │   IServicioGeneracionCargos.cs # NUEVAS
│   │   ├── Validadores/
│   │   │   ├── ValidadorSoportePago.cs      # NUEVO: imagen de hasta 5 MB
│   │   │   └── ValidadorPago.cs             # NUEVO: valor, fecha, entidad, referencia
│   │   └── Implementaciones/                # NUEVAS: una por servicio
│   ├── LFClub.Infraestructura/
│   │   ├── Datos/
│   │   │   ├── Configuraciones/             # NUEVAS: una por entidad
│   │   │   └── Migraciones/…MensualidadesYPagos.cs
│   │   ├── Repositorios/                    # NUEVOS: Cargos, Pagos, MediosPago, Finanzas
│   │   │   └── RepositorioEliminacionCuenta.cs   # CAMBIA: borra soportes y anonimiza autoría de pagos
│   │   ├── Imagenes/
│   │   │   ├── GeneradorImagenResguardo.cs  # NUEVO: dibuja el PNG
│   │   │   └── PlanoResguardo.cs            # NUEVO: medidas y capas
│   │   └── Trabajos/
│   │       └── TrabajoCargosMensuales.cs    # NUEVO: servicio en segundo plano
│   └── LFClub.Api/
│       └── Controladores/Club/
│           ├── ControladorMensualidad.cs, ControladorMediosPago.cs, ControladorCuentaJugador.cs,
│           │   ControladorPagos.cs, ControladorRevisionDePagos.cs,
│           │   ControladorFinanzas.cs       # NUEVOS
│           └── (ControladorJugadores: sin cambios de ruta; la lista gana el estado)
└── pruebas/
    ├── Unitarias/Reglas/                     # NUEVAS: una por regla
    └── Integracion/Pagos/                    # NUEVAS: generación, saldo, registro, revisión,
                                              #         anulación, recibo, resguardo, ENTRENADOR,
                                              #         aislamiento, soporte privado, finanzas
        Integracion/Cuenta/EsquemaDeEliminacionPruebas.cs   # CAMBIA: nuevas claves

frontend/
├── src/privado/pagos/                        # NUEVO: cuenta del jugador, informar transferencia,
│                                             #   registrar pago, revisión, medios, mensualidad, finanzas
├── src/compartido/api/tiposPagos.ts          # NUEVO
├── src/privado/jugadores/TablaDeGrupo.tsx    # CAMBIA: columna "Al día" real (no para ENTRENADOR)
├── src/privado/MenuDelClub.tsx               # CAMBIA: entrada "Pagos" por rol
├── src/privado/inicio/…                      # CAMBIA: tarjeta de pagos en revisión (PRESIDENTE)
└── pruebas/pagos/                            # NUEVO: Vitest
```

**Decisión de estructura**: se mantiene la separación `backend/` y `frontend/`. Los servicios se
dividen por tarea (consulta, registro, revisión, finanzas) y no por entidad, para que ninguno se
acerque a las 250 líneas y cada uno tenga un solo motivo para cambiar. El repositorio de cargos
contiene la sentencia de generación mensual; el de pagos, el recibo consecutivo y el bloqueo por
jugador.

## Seguimiento de la complejidad

No hay violaciones de la constitución que justificar.
