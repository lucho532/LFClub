# Plan de implementación: pago del club por el uso de la plataforma

**Rama**: `017-pago-de-la-plataforma` | **Fecha**: 2026-10-21 | **Spec**: [spec.md](spec.md)

**Entrada**: especificación en `specs/017-pago-de-la-plataforma/spec.md` y su clarificación del
2026-10-21 (5 días de gracia, mes de inicio del cobro por club, la suspensión manual no se levanta con un
pago).

## Resumen

Cada mes la plataforma genera sola el cobro de cada club (conteo de integrantes activos por rol × precio
por rol). El PRESIDENTE lo ve y lo paga con un botón que abre el checkout de **Wompi**; el cobro solo se
da por pagado cuando el backend recibe y verifica la **notificación firmada** de Wompi. Si pasan los días
de gracia sin pago, el club se **suspende solo** (motivo `IMPAGO`) y vuelve a la normalidad en cuanto se
confirma el pago; una suspensión manual del DESARROLLADOR nunca se levanta con un pago. El DESARROLLADOR
fija precios, días de gracia y el mes de inicio de cada club, y ve estado e historial de cobros.

Enfoque técnico: **cuatro tablas, una fila de configuración y dos columnas en `Club`** (una migración).
Un `BackgroundService` nuevo (`TrabajoCobrosPlataforma`), copia del patrón de `TrabajoCargosMensuales`,
genera cobros y aplica la suspensión. La pasarela queda detrás de `IPasarelaDePago` (Aplicacion), con
`PasarelaWompi` en `Infraestructura/Pagos`: **arma el enlace de checkout firmado y verifica la firma de la
notificación; no hace llamadas salientes a Wompi**. Dos reglas puras en el dominio
(`ReglaCobroPlataforma`, `ReglaSuspensionPorImpago`). 11 operaciones nuevas. Las decisiones están en
[research.md](research.md).

## Contexto técnico

**Lenguaje y versión**: C# 14 sobre .NET 10 en el backend; TypeScript 5 sobre Node 22 en el frontend. Sin
cambios respecto de la 001.

**Dependencias principales**: las de la 001. **No se añade ninguna** (SHA-256 y comparación de tiempo
constante vienen en `System.Security.Cryptography`; no hay `HttpClient` hacia Wompi).

**Almacenamiento**: PostgreSQL. **Una migración** (`PagoDeLaPlataforma`): `Club.MesInicioCobro` y
`Club.MotivoSuspension`; tablas `ConfiguracionPlataforma`, `CobroPlataforma` (con `SuspensionOmitida`), `IntentoPagoPlataforma`,
`PagoPlataforma` y `NotificacionPasarela` (ver [data-model.md](data-model.md)). La migración marca como
`MANUAL` los clubes ya suspendidos y deja `MesInicioCobro` vacío en todos los existentes.

**Pruebas**: xUnit (unitarias de las reglas y de la firma con vectores conocidos; integración con
`WebApplicationFactory`, Testcontainers, `IReloj` e `IPasarelaDePago` sustituibles); Vitest en el
frontend. Se actualizan `AccesoClubPruebas`, `AccesoPlataformaPruebas` y `EsquemaDeEliminacionPruebas`
(014).

**Plataforma de destino**: la de la 001 (API en contenedor Linux, web adaptable y Capacitor). El checkout
de Wompi se abre en el navegador del aparato; en la aplicación Android se abre con el navegador del
sistema y el retorno vuelve a la dirección web (ver research §2).

**Tipo de proyecto**: aplicación web con `backend/` y `frontend/` separados (§4).

**Objetivos de rendimiento**: el trabajo recorre los clubes una vez cada 30 minutos (cientos de clubes
como mucho) con una consulta de conteo por club; la notificación se procesa en una transacción corta. Un
club suspendido por impago vuelve a activo en menos de 1 minuto desde la notificación (CE-005).

**Restricciones**: código y documentación en español (§2.1, §3); archivos escritos a mano de hasta 250
líneas (§2.3); reglas en el servidor (§15); el retorno del navegador no es prueba de pago y las llaves no
van en el repositorio (§7.4); backend y pantalla en la misma tarea (§27.1).

**Escala y alcance**: 11 operaciones nuevas (el contrato pasa de N a N+11; **confirmar contando al
empezar**); 5 entidades y 4 enumeraciones nuevas; unos 3 reglas/utilidades de dominio; 6 servicios y 1
interfaz de pasarela en Aplicacion; `PasarelaWompi` y el trabajo en Infraestructura; 3 controladores; unos
12 archivos de frontend.

## Comprobación de la constitución

*Puerta: debe pasar antes de la fase 0 y se revisa de nuevo tras el diseño de la fase 1.*

Evaluada contra la versión **4.16.0**, que ya recoge la enmienda pedida por la spec (nota 1).

| Principio | Cómo lo cumple el plan | Resultado |
| --- | --- | --- |
| §2.1 y §2.2 Español y vocabulario | `CobroPlataforma`, `ServicioNotificacionPasarela`, "impago", "gracia" | Cumple |
| §2.3 Máximo 250 líneas | Un servicio por responsabilidad; controladores delgados; el trabajo es corto | Cumple |
| §3 Documentación XML | En cada clase nueva | Cumple |
| §4 y §5 Capas | Reglas en `Dominio/Reglas`; servicios e `IPasarelaDePago` en `Aplicacion`; `PasarelaWompi` en `Infraestructura/Pagos`; los controladores delegan | Cumple |
| §6.2 Pasarela | Wompi, solo para este cobro, detrás de una interfaz de Aplicacion | Cumple |
| §6 y §19 Simplicidad | Sin dependencias nuevas, sin llamadas salientes, sin programador externo; patrón de trabajo ya existente | Cumple |
| §7.1 Aislamiento | Los cobros no usan el filtro global (research §6); los repositorios filtran por club; prueba de aislamiento entre clubes | Cumple, con justificación en Complejidad |
| §7.4 Pago y suspensión | Solo el PRESIDENTE lo ve, también suspendido; confirmación solo por notificación verificada; suspensión automática y vuelta inmediata; llaves en configuración | Cumple, con la nota 1 |
| §8 Roles | El PRESIDENTE paga; DIRECTIVO, ENTRENADOR y JUGADOR reciben `403`; el DESARROLLADOR ve solo conteos y totales | Cumple |
| §13 y §14 Inmutabilidad | `PagoPlataforma` no se edita ni se borra; el cobro conserva su detalle | Cumple |
| §15 Reglas en backend | Cálculo, vencimiento, suspensión y confirmación se deciden en el servidor | Cumple |
| §16 Pagos de las familias | Entidades, rutas y pantallas separadas de los pagos de la 015; ninguna mezcla | Cumple |
| §18 Estados | Estado del cobro y motivo de suspensión explícitos; el estado de pago del club se deriva | Cumple |
| §20 Pruebas | Reglas por tabla, firma con vector conocido, notificación en todos sus resultados, suspensión y reactivación, roles, aislamiento, ausencia de secretos | Cumple |
| §23 API | DTO propio, `problem+json` con códigos estables | Cumple |
| §24 Diseño visual | Teléfono y escritorio, dos temas; el estado también con texto e ícono | Cumple, con la nota 3 |
| §25 y §28 Decisiones | Cierra "Pago por el uso de la plataforma"; no abre otras salvo la nota 2 | Cumple |
| §27 Definición de terminado | Tareas por historia con backend, pantalla y pruebas | Cumple |

**Resultado de la puerta**: pasa, con la enmienda de la nota 1 ya aplicada.

**Nota 1 — enmienda de la constitución (aplicada).** 4.15.0 → 4.16.0, aprobada y aplicada el
2026-10-21: §7.4 concreta valor, periodicidad, gracia, mes de inicio y motivo de la suspensión (el pago
levanta solo la de impago); §13 y §14 declaran inmutables los pagos de la plataforma; §28 cierra "Pago
por el uso de la plataforma" y abre "Recordatorios del cobro de la plataforma".

**Nota 2 (decidida) — levantar a mano una suspensión por impago.** El club queda activo hasta el
siguiente cobro: los cobros vencidos en ese momento se marcan `SuspensionOmitida`, siguen pendientes y
pagables, y no vuelven a suspender (research §8).

**Nota 3**: el lienzo no tiene estas pantallas. Su diseño sigue [research.md](research.md) §12 con los
tokens y componentes del proyecto y lo revisa el propietario en la validación.

**Nota 4 (riesgo externo).** Los nombres exactos de los parámetros del checkout y de las propiedades
firmadas de Wompi se contrastan con su documentación vigente al implementar (research §2); queda aislado
en `PasarelaWompi` y cubierto por una prueba de vector conocido. Hace falta una cuenta de comercio en el
ambiente de pruebas de Wompi para la validación de punta a punta (quickstart).

**Revisión tras el diseño**: 11 operaciones, 5 tablas y 2 columnas, ninguna dependencia nueva. La puerta
sigue pasando con las mismas condiciones.

## Estructura del proyecto

### Documentación de esta funcionalidad

```text
specs/017-pago-de-la-plataforma/
├── plan.md              # Este archivo
├── research.md          # Decisiones técnicas y hallazgos del código
├── data-model.md        # Tablas, columnas, estados y reglas
├── quickstart.md        # Recorrido de validación
├── contracts/
│   └── api.yaml         # Las 11 operaciones nuevas
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
│   │   │   ├── Club.cs                              # CAMBIA: MesInicioCobro, MotivoSuspension
│   │   │   ├── ConfiguracionPlataforma.cs           # NUEVA
│   │   │   ├── CobroPlataforma.cs                   # NUEVA
│   │   │   ├── IntentoPagoPlataforma.cs             # NUEVA
│   │   │   ├── PagoPlataforma.cs                    # NUEVA
│   │   │   └── NotificacionPasarela.cs              # NUEVA
│   │   ├── Enumeraciones/
│   │   │   ├── MotivoSuspension.cs                  # NUEVA
│   │   │   ├── EstadoCobroPlataforma.cs             # NUEVA
│   │   │   ├── OrigenPagoPlataforma.cs              # NUEVA
│   │   │   ├── ResultadoNotificacion.cs             # NUEVA
│   │   │   └── EstadoDePagoClub.cs                  # NUEVA
│   │   └── Reglas/
│   │       ├── ReglaCobroPlataforma.cs              # NUEVA: total y detalle por rol
│   │       └── ReglaSuspensionPorImpago.cs          # NUEVA: suspender, reactivar o nada
│   ├── LFClub.Aplicacion/
│   │   ├── Interfaces/
│   │   │   ├── IPasarelaDePago.cs                   # NUEVA: enlace firmado y verificación
│   │   │   ├── IRepositorioCobrosPlataforma.cs      # NUEVA
│   │   │   └── IRepositorioConfiguracionPlataforma.cs  # NUEVA
│   │   ├── DTOs/                                    # NUEVOS: PagoPlataformaDto, CobroPlataformaDto,
│   │   │                                            #   IntentoDePagoDto, TarifasDto, …
│   │   ├── Servicios/                               # NUEVOS: IServicioCobrosPlataforma (generar y
│   │   │                                            #   consultar), IServicioPagoPlataforma (intentos),
│   │   │                                            #   IServicioNotificacionPasarela, IServicioTarifas,
│   │   │                                            #   IServicioSuspensionPorImpago, IServicioCobrosDeUnClub
│   │   ├── Implementaciones/                        # NUEVAS: sus implementaciones
│   │   └── Implementaciones/ServicioEstadoClub.cs   # CAMBIA: guarda el motivo MANUAL y omite la suspensión de los cobros al levantar una de impago
│   │   └── Implementaciones/ServicioCreacionClub.cs # CAMBIA: MesInicioCobro del club nuevo
│   ├── LFClub.Infraestructura/
│   │   ├── Pagos/
│   │   │   ├── PasarelaWompi.cs                     # NUEVA: enlace de checkout y firma
│   │   │   ├── FirmaWompi.cs                        # NUEVA: SHA-256 y comparación segura
│   │   │   └── OpcionesWompi.cs                     # NUEVA: configuración (sin valores)
│   │   ├── Trabajos/TrabajoCobrosPlataforma.cs      # NUEVA: genera y suspende
│   │   ├── Repositorios/                            # NUEVOS: cobros y configuración
│   │   └── Datos/
│   │       ├── Configuraciones/                     # NUEVAS: una por entidad
│   │       └── Migraciones/…_PagoDeLaPlataforma.cs  # NUEVA
│   └── LFClub.Api/
│       ├── appsettings.json                         # CAMBIA: sección "Wompi" con valores vacíos
│       ├── Configuracion/RegistroDeServicios.cs     # CAMBIA: pasarela, repositorios, trabajo
│       ├── Configuracion/RegistroDeCasosDeUso.cs    # CAMBIA: servicios
│       └── Controladores/
│           ├── Club/ControladorPagoPlataforma.cs    # NUEVO: ver, ver cobro, iniciar pago
│           └── Plataforma/
│               ├── ControladorTarifas.cs            # NUEVO
│               ├── ControladorCobrosPlataforma.cs   # NUEVO: inicio, historial, pago manual,
│               │                                    #   anulación, notificaciones
│               └── ControladorNotificacionesPasarela.cs  # NUEVO: webhook anónimo
└── pruebas/
    ├── Unitarias/
    │   ├── Reglas/ReglaCobroPlataformaPruebas.cs    # NUEVA
    │   ├── Reglas/ReglaSuspensionPorImpagoPruebas.cs # NUEVA
    │   └── Pagos/FirmaWompiPruebas.cs               # NUEVA
    └── Integracion/
        ├── Plataforma/CobrosPlataformaPruebas.cs    # NUEVA: generación, idempotencia, mes de inicio
        ├── Plataforma/NotificacionPasarelaPruebas.cs # NUEVA: todos los resultados
        ├── Plataforma/SuspensionPorImpagoPruebas.cs # NUEVA: suspender, reactivar, manual
        ├── Pagos/PagoPlataformaPresidentePruebas.cs # NUEVA: roles, intento, sin secretos
        └── Aislamiento/AislamientoDePagoPlataformaPruebas.cs # NUEVA

frontend/
├── src/compartido/api/tiposPagoPlataforma.ts        # NUEVO: DTO del contrato
├── src/privado/pagoPlataforma/
│   ├── PagoPlataforma.tsx                           # NUEVO: resumen, detalle y botón
│   ├── RetornoPagoPlataforma.tsx                    # NUEVO: "en verificación" y consulta
│   ├── TarjetaCobro.tsx                             # NUEVO
│   └── AvisoPagoPlataforma.tsx                      # NUEVO: aviso arriba del inicio del PRESIDENTE
├── src/privado/InicioClub.tsx                       # CAMBIA: el aviso, solo para el PRESIDENTE
├── src/privado/MenuDelClub.tsx                      # CAMBIA: entrada "Pago de la plataforma"
├── src/privado/AvisoClubNoDisponible.tsx            # (sin cambio; se comprueba)
└── src/plataforma/
    ├── PantallaTarifas.tsx                          # NUEVA
    ├── SeccionCobros.tsx                            # NUEVA: historial, pago manual, anular
    ├── SeccionInicioDelCobro.tsx                    # NUEVA
    └── ListaClubes.tsx / DetalleClub.tsx            # CAMBIAN: estado de pago y accesos
```

**Decisión de estructura**: sin proyectos nuevos; todo en las capas de la 001. La integración con Wompi
vive en `Infraestructura/Pagos`, tal como ya prevé §4. El detalle de las rutas del frontend y de los
archivos se ajusta al abrir cada tarea.

## Complejidad

| Decisión | Por qué se necesita | Alternativa más simple descartada |
| --- | --- | --- |
| Los cobros no implementan `IPerteneceAClub` | El trabajo en segundo plano y la notificación de la pasarela no tienen club en la petición, y el filtro global falla cerrado | Filtro global con `IgnoreQueryFilters` en cada consulta de plataforma: más propenso a error (research §6) |
| Cinco tablas | El detalle del cobro es histórico e inmutable; el intento, el pago y la notificación tienen ciclos de vida distintos y la idempotencia necesita un índice propio | Una sola tabla de cobros con todo: mezcla comprobante, intentos y auditoría, y no permite pagos duplicados visibles |
| Un trabajo nuevo con el mismo patrón que `TrabajoCargosMensuales` | Es el único mecanismo de segundo plano del proyecto; cada trabajo tiene su ciclo y su interruptor | Un solo trabajo con dos responsabilidades: acopla dos funcionalidades |
