# Plan de implementación: aviso fijo de mora en el inicio de la familia

**Rama**: `016-aviso-de-mora` | **Fecha**: 2026-10-21 | **Spec**: [spec.md](spec.md)

**Entrada**: especificación en `specs/016-aviso-de-mora/spec.md` y su clarificación del 2026-10-21
(tres estados: verde, ámbar y rojo).

## Resumen

La familia ve, en la parte superior de su inicio, un aviso fijo del estado de su jugador: **rojo** si
está en mora (una mensualidad sin pagar después del día 5 de su mes), **ámbar** si tiene la
mensualidad pendiente pero dentro del plazo, **verde** si no debe nada. No se puede cerrar.

Enfoque técnico: **no hay tablas ni migración**. El estado se calcula al consultarlo, con los cargos y
pagos de la spec 015. Una regla pura nueva en el dominio (`ReglaMora`) decide el estado con la fecha de
Colombia; un servicio nuevo la aplica a los cargos del jugador; **una operación nueva de club**
(`GET …/jugadores/{usuarioRolId}/cuenta/aviso`) lo entrega. En el frontend, un componente en el inicio
de la cuenta de jugador. El cálculo de la fecha es del servidor, no del aparato (CE-004). Las decisiones
están en [research.md](research.md).

## Contexto técnico

**Lenguaje y versión**: C# 14 sobre .NET 10 en el backend; TypeScript 5 sobre Node 22 en el
frontend. Sin cambios respecto de la 001.

**Dependencias principales**: las de la 001. **No se añade ninguna.**

**Almacenamiento**: ninguno nuevo. **Sin migración.** El estado no se guarda (spec, entidades).

**Pruebas**: xUnit (unitarias de `ReglaMora`; integración con `WebApplicationFactory`, Testcontainers
e `IReloj` sustituible, para fijar el día); Vitest en el frontend. Se actualiza la cuenta de
operaciones de `EndpointsDeLaApi`.

**Plataforma de destino**: la de la 001 (API en contenedor Linux, web adaptable y Capacitor).

**Tipo de proyecto**: aplicación web con `backend/` y `frontend/` separados (§4).

**Objetivos de rendimiento**: una consulta de los cargos de un jugador (decenas al año) y una de sus
pagos; se pide una vez al abrir el inicio.

**Restricciones**: código y documentación en español (§2.1, §3); archivos escritos a mano de hasta
250 líneas (§2.3); regla de acceso en el servidor (§15, §16.9); sin recargos ni restricciones por deber
(§16.7); sin envíos (§16.8); backend y pantalla en la misma tarea (§27.1).

**Escala y alcance**: 1 operación nueva (el contrato pasa de 131 a 132 y las de club de 105 a 106;
**confirmar contando al empezar**); 2 archivos de dominio, 3 de aplicación, 1 controlador nuevos o
cambiados; unos 5 archivos de frontend.

## Comprobación de la constitución

*Puerta: debe pasar antes de la fase 0 y se revisa de nuevo tras el diseño de la fase 1.*

Evaluada contra la versión **4.15.0**, que ya recoge las **dos enmiendas** pedidas por la spec (§16.7 y §16.8,
nota 1), que se aprueban antes de implementar.

| Principio | Cómo lo cumple el plan | Resultado |
| --- | --- | --- |
| §2.1 y §2.2 Español y vocabulario | `ReglaMora`, `ServicioAvisoDeMora`, "mora", "aviso", "plazo" | Cumple |
| §2.3 Máximo 250 líneas | Una regla, un servicio, un componente; todos cortos | Cumple |
| §3 Documentación XML | En cada clase nueva | Cumple |
| §4 y §5 Capas | Regla pura en `Dominio/Reglas`; servicio en `Aplicacion`; el controlador delega; sin infraestructura nueva | Cumple |
| §6 y §19 Simplicidad | Sin tabla, sin proceso en segundo plano, sin dependencias; reutiliza `IRepositorioCargos` e `IReloj` | Cumple |
| §7.1 Aislamiento | Los repositorios ya filtran por club; un jugador de otro club responde 404 | Cumple |
| §8 y §16.9 Roles | Solo `JUGADOR` y solo el propio jugador; PRESIDENTE, DIRECTIVO y ENTRENADOR reciben 403, el DESARROLLADOR no tiene ruta | Cumple |
| §13 Histórico | Usa el valor y el mes que cada cargo conservó | Cumple |
| §15 Reglas en backend | La mora, el plazo y la fecha se deciden en el servidor | Cumple |
| §16.1 a §16.6 | Saldo calculado de cargos y pagos confirmados no anulados; sin tocar pagos | Cumple |
| §16.7 Mensualidad | Hoy dice que deber "no tiene consecuencias". El aviso es una consecuencia visible | Cumple, con la enmienda aplicada (nota 1) |
| §16.8 Recordatorios | Hoy no hay recordatorios. El aviso no se envía, solo se muestra | Cumple, con la enmienda aplicada (nota 1) |
| §18 Estados | El estado no se guarda; se deriva | Cumple |
| §20 Pruebas | Regla por fechas, autorización por rol, aislamiento, ENTRENADOR fuera | Cumple |
| §23 API | DTO propio, `problem+json` con códigos ya existentes | Cumple |
| §24 Diseño visual | Teléfono y escritorio, dos temas; el estado se dice con texto e ícono además del color | Cumple, con la nota 3 |
| §25 y §28 Decisiones | No se resuelve ninguna decisión pendiente | Cumple |
| §27 Definición de terminado | Tareas por historia con backend, pantalla y pruebas | Cumple |

**Resultado de la puerta**: pasa, con las dos enmiendas de la nota 1 ya aplicadas. Sin ellas, la
implementación contradice la constitución.

**Nota 1 — enmiendas de la constitución (aplicadas).** Redacción acordada, ver la constitución 4.15.0:
- §16.7, párrafo "Una mensualidad sin pagar después del día 5…": *"…aparece como pendiente en el
  estado de cuenta y el inicio de la familia muestra un aviso fijo de mora. No tiene otras
  consecuencias."* y, en el párrafo siguiente: *"No existe recargo por mora…"* se mantiene igual.
- §16.8: *"No se envían recordatorios de pago, ni automáticos ni manuales, por ningún canal (campana,
  correo, mensaje, notificación del dispositivo). El aviso de mora del inicio de la familia (§16.7) no
  es un recordatorio: se muestra dentro de la aplicación y no se envía."*

Aprobadas y aplicadas el 2026-10-21 (4.14.0 → 4.15.0).

**Nota 2 (para el propietario) — estados de cuenta y avisos pueden diferir.** El estado de cuenta de
la 015 dice "Debe" desde el día 1 si hay una mensualidad sin pagar; el aviso de la 016 solo dice "mora"
desde el día 6. Es lo que se acordó (ámbar en medio). El texto de ambos debe evitar que parezcan
contradecirse: el aviso habla de plazo, la cuenta de saldo.

**Nota 3**: el lienzo no tiene este aviso. Su diseño sigue [research.md](research.md) §6 con los tokens
y componentes del proyecto y lo revisa el propietario en la validación.

**Revisión tras el diseño**: una operación, ninguna tabla y ninguna dependencia nuevas. La puerta
sigue pasando con las mismas dos condiciones.

## Estructura del proyecto

### Documentación de esta funcionalidad

```text
specs/016-aviso-de-mora/
├── plan.md              # Este archivo
├── research.md          # Decisiones técnicas y hallazgos del código
├── data-model.md        # Sin tablas: el estado calculado y sus reglas
├── quickstart.md        # Recorrido de validación
├── contracts/
│   └── api.yaml         # La operación nueva
├── checklists/
│   └── requirements.md  # (de /speckit-specify)
└── tasks.md             # Fase 2 (/speckit-tasks; no lo crea este comando)
```

### Código fuente (raíz del repositorio)

```text
backend/
├── src/
│   ├── LFClub.Dominio/
│   │   ├── Enumeraciones/EstadoDeAvisoDeMora.cs      # NUEVA: AL_DIA, PENDIENTE_EN_PLAZO, EN_MORA
│   │   └── Reglas/
│   │       ├── ReglaMora.cs                          # NUEVA: estado, valor vencido y meses
│   │       └── ReglaMesDeCobro.cs                    # CAMBIA: DiaEnColombia(ahoraUtc)
│   ├── LFClub.Aplicacion/
│   │   ├── DTOs/AvisoDeMoraDto.cs                    # NUEVO
│   │   ├── Servicios/IServicioAvisoDeMora.cs         # NUEVO
│   │   └── Implementaciones/ServicioAvisoDeMora.cs   # NUEVO
│   └── LFClub.Api/
│       ├── Configuracion/RegistroDeCasosDeUso.cs     # CAMBIA: registra el servicio
│       └── Controladores/Club/ControladorCuentaJugador.cs   # CAMBIA: GET …/cuenta/aviso
└── pruebas/
    ├── Unitarias/Reglas/ReglaMoraPruebas.cs          # NUEVA
    └── Integracion/Pagos/AvisoDeMoraPruebas.cs       # NUEVA: estados, día 5/6, roles, aislamiento

frontend/
├── src/compartido/api/tiposPagos.ts                  # CAMBIA: AvisoDeMoraDto
├── src/privado/pagos/apiAvisoDeMora.ts               # NUEVO
├── src/privado/inicio/AvisoDeMora.tsx                # NUEVO: el aviso fijo, tres estados
├── src/privado/InicioClub.tsx                        # CAMBIA: lo muestra al JUGADOR, arriba
└── pruebas/pagos/AvisoDeMora.test.tsx                # NUEVA: Vitest
```

**Decisión de estructura**: se mantiene la separación `backend/` y `frontend/`. Se añade a
`ControladorCuentaJugador` (misma ruta base, mismo permiso de lectura) en lugar de un controlador
nuevo. El servicio es separado de `ServicioCuentaDelJugador` para que el aviso no cargue pagos, medios
de pago ni soportes.

## Seguimiento de la complejidad

No hay violaciones de la constitución que justificar más allá de las dos enmiendas de la nota 1.
