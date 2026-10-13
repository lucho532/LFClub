# Investigación: eliminar mi cuenta

**Rama**: `014-eliminar-cuenta` | **Spec**: [spec.md](spec.md) | **Plan**: [plan.md](plan.md)

Cada punto tiene decisión, razón y alternativas. No quedan `NEEDS CLARIFICATION`.

## 1. Hallazgos del código (lo que el plan da por cierto)

Comprobado leyendo el repositorio el 2026-10-13:

- `Usuario` (cuenta) → `UsuarioRol` (integrante) con `OnDelete(Restrict)`: hay que borrar los
  integrantes antes que la cuenta.
- Cuelgan **en cascada** de `UsuarioRol`: ficha, documentos, foto del jugador, carta, equipos del
  jugador, respuestas a entrenamientos, jugadores añadidos a entrenamientos, destinatarios de
  comunicados y de avisos, asignaciones de entrenador (y por ellas `EntrenadorEquipo`). Cuelgan en
  cascada de `Usuario`: foto de perfil, solicitudes de recuperación y destinatarios de comunicados y
  avisos.
- Cuelgan con **`SetNull`**: quién publicó o retiró un comunicado, quién creó un entrenamiento o un
  partido, quién cambió o publicó un resultado, quién aprobó o retiró a un integrante, quién cambió
  la ficha o la carta, de dónde se agregó un hermano.
- Cuelgan con **`NoAction`** y **bloquean** el borrado: `PartidoJugador.UsuarioRolId` y
  `AsistenciaEntrenamiento.UsuarioRolId`. Hoy solo caen porque el club las lleva en cascada; al borrar
  una cuenta suelta hay que borrarlas primero (de ahí salen los goles y la asistencia, RF-006).
- `Invitacion.CreadaPorUsuarioId` **no tiene clave foránea**: sobrevivirían a la cuenta.
- Varias tablas guardan **el nombre copiado como texto** (§13): `Comunicado.PublicadoPorNombre` y
  `RetiradoPorNombre`; `Partido.PublicadoPorNombre` y `ResultadoCambiadoPorNombre`;
  `PartidoJugador.IndicadoPorNombre`; `CartaJugador.EstadisticasCambiadasPorNombre` y
  `DorsalCambiadoPorNombre`; `FichaJugador.UltimoCambioPorNombre` (obligatorio);
  `UsuarioRol.AprobadoPorNombre` y `RetiradoPorNombre`. El `SetNull` no las toca: sin trabajo extra,
  el nombre de la persona eliminada seguiría escrito en ellas.
- `ValidadorSesion` rechaza todo token cuya cuenta ya no existe: las demás sesiones dejan de servir
  solas (RF-010), sin lista de sesiones ni sello nuevo.
- `ServicioSesion.IniciarAsync` ya responde igual a una credencial desconocida, con el mismo coste de
  hash (RF-013): borrar la cuenta basta.
- `ServicioEliminacionClub` borra el club por cascada y luego las cuentas que se quedan sin club. Una
  cuenta que se queda sin club no tiene ya nada publicado en otro club, así que ese camino cumple lo
  que dice el supuesto de la spec sin cambios.
- `ReglaUltimoPresidente`, `ContarPresidentesAsync` y `ObtenerBloqueandoAsync` ya existen y se usan en
  el retiro de un presidente.
- No existen tablas de pagos (§16).

## 2. Un servicio de eliminación de cuenta, con regla pura y repositorio propio

**Decisión**: `ServicioEliminacionCuenta` (contraseña, regla, transacción) y
`ServicioResumenEliminacionCuenta` (lectura para la pantalla), con `ReglaEliminacionDeCuenta` en el
dominio y `IRepositorioEliminacionCuenta` en aplicación.

**Razón**: la regla del presidente único se prueba sin base de datos; la lectura y la escritura
comparten la regla, así que la pantalla y el servidor no pueden discrepar. El borrado masivo toca
tablas de seis áreas y no cabe en ningún repositorio existente.

**Alternativas**: ampliar `EliminadorDeCuentaSinClub` (es de otra regla: cuentas que se quedan sin
club) y borrar con `DbContext` en el servicio (rompe §5).

## 3. Atomicidad y orden de borrado

**Decisión**: todo dentro de `IUnidadDeTrabajo.EnTransaccionAsync`, en este orden:

1. Bloquear las filas de los clubes de la cuenta en orden de identificador y volver a contar a los
   presidentes de cada uno (regla del presidente único).
2. Anonimizar la autoría (§6).
3. Anular las invitaciones pendientes de la cuenta (§7).
4. Borrar `PartidoJugador` y `AsistenciaEntrenamiento` de sus integrantes.
5. Borrar sus `UsuarioRol` (la cascada limpia el resto).
6. Borrar la `Usuario` (cascada de foto de perfil y solicitudes).

**Razón**: o se borra todo o nada (RF-011). Ordenar los bloqueos por identificador evita el
interbloqueo cuando dos presidentes de los mismos clubes se eliminan a la vez; el bloqueo hace que
no puedan dejar el club sin presidente (el segundo ve al primero ya borrado y se encuentra como
único).

**Alternativas**: borrar con el contexto de seguimiento de EF (carga y marca todo: lento y propenso
a olvidar tablas) y confiar solo en la cascada del club (no existe por cuenta).

## 4. La contraseña y los intentos fallidos

**Decisión**: la contraseña se verifica con `IHashContrasena`. Un fallo responde **400**
`contrasena_incorrecta` (no 401, que el cliente toma por sesión caducada y lo saca de la aplicación) y
**cuenta como fallo de inicio de sesión** (`RegistrarFalloDeSesionAsync`); al quinto la cuenta queda
bloqueada hasta recuperar la contraseña, como en §12.4. Con la cuenta ya bloqueada responde 403
`cuenta_bloqueada` y no admite ni la contraseña correcta. Un acierto reinicia el contador.

**Razón**: quien tiene un token robado no debe poder adivinar la contraseña sin límite; reutilizar el
contador es la regla que ya existe, no una nueva.

**Alternativas**: sin contador (permite fuerza bruta con una sesión abierta) y un contador aparte
(otra regla que explicar).

## 5. Sesión limitada a un jugador por documento

**Decisión**: se permite eliminar con una sesión limitada, y el resumen lista **todos** los jugadores
de la cuenta (nota 1 del plan).

**Razón**: quien confirma debe saber qué borra. La contraseña es la de la cuenta, de modo que quien la
tiene es la familia.

**Alternativas**: exigir el correo (más seguro frente a un jugador adulto que comparte la contraseña
y es otra persona que la familia; añade un paso a quien solo conoce el documento del niño).

## 6. "Persona eliminada" por actualización masiva, antes de borrar

**Decisión**: constante `NombresDeAutoria.PersonaEliminada = "Persona eliminada"`. Antes del borrado,
una actualización por tabla escribe esa constante en las diez columnas de §1, filtrando por la
cuenta (`...PorUsuarioId`) o por sus integrantes (`...PorUsuarioRolId`). Después el `SetNull` suelta
el vínculo.

**Razón**: tras el borrado ya no hay con qué encontrar las filas. Los comunicados siguen vigentes y la
familia sigue viendo el texto (escenario 3.3 y caso límite). No se cambia ninguna regla de lectura:
la pantalla ya muestra el nombre guardado.

**Alternativas**: dejar el nombre (incumple RF-008 y RF-005) y una vista que sustituya al leer (toca
todas las lecturas).

**Limitación conocida**: cuando una cuenta se elimina por el camino "se quedó sin club" (retiro de un
presidente, rechazo de un ingreso), el vínculo se pierde antes y su nombre queda en lo que publicó en
ese club. No es lo que pide esta spec y el club es el que se borra; se deja anotado.

## 7. Invitaciones sin aceptar

**Decisión**: `AnuladaEn = ahora` en las invitaciones con `CreadaPorUsuarioId` de la cuenta, sin
usar y sin anular. No se borran: la regla de "una invitación sirve una vez" las conserva como
anuladas, igual que cuando otra las reemplaza.

**Razón**: RF-012. Una invitación enviada por quien ya no existe no puede seguir aceptándose.

**Nota**: el modelo no ata una invitación a categorías o equipos; "su asignación" de RF-012 se refiere
a las asignaciones del propio integrante, que caen en cascada. No hay nada más que retirar.

## 8. El presidente único, calculado y bloqueado en el servidor

**Decisión**: `ReglaEliminacionDeCuenta.ClubesBloqueantes(integrantes, presidentesPorClub)` devuelve
los clubes donde la cuenta es PRESIDENTE y hay uno solo. El resumen lo muestra con el nombre del club
y la eliminación responde 409 `presidente_unico` con la lista. Cuentan los presidentes aprobados del
club, como en el retiro de presidente.

**Razón**: mismo criterio que `ReglaUltimoPresidente` (§8) para que el retiro del rol y la
eliminación nunca discrepen.

**Alternativas**: contar también invitaciones de presidente pendientes (la regla existente no las
cuenta).

## 9. La solicitud por correo y el panel del DESARROLLADOR

**Decisión**: sin acción nueva en el panel. La página dice: (1) eliminar desde la aplicación; (2) si no
recuerda la contraseña, "Olvidé mi contraseña" y luego (1); (3) si no puede ni eso, escribir desde el
correo de la cuenta a `luisrak25@gmail.com`; el plazo es de 15 días. La persona que atiende el
correo sigue el procedimiento del [quickstart](quickstart.md), fuera de la aplicación.

**Razón**: la §8 le niega al DESARROLLADOR acceso a datos de clubes; darle "eliminar cuenta" es una
enmienda de constitución que la spec no pide.

**Alternativas**: acción en el panel (necesita enmendar §8) y un script de operación (más superficie
que mantener).

## 10. Pantalla, página pública y enlaces

**Decisión**:

- `/cuenta/eliminar` (privada): título, qué se borra y qué se conserva en listas, jugadores por nombre
  y club en una lista, alternativa "Retirar del club" con enlace a la ficha (RF-016), campo de
  contraseña con el componente existente, botón destructivo "Eliminar mi cuenta para siempre".
  Con clubes bloqueantes: sin campo ni botón, solo el aviso con el club.
- Llegada: "Mi perfil" → "Eliminar mi cuenta". Menú de perfil (1), Mi perfil (2), Eliminar (3),
  contraseña con foco automático (4), botón (5): menos de 6 toques (CE-001).
- Al terminar: se limpia la sesión local, se navega a `/entrar` con un aviso "Tu cuenta se eliminó".
- `/eliminar-cuenta` (pública): pasos, qué se borra y qué se conserva, correo y plazo. Enlazada desde
  `/entrar` y `/privacidad`. La constante `CORREO_PRIVACIDAD` se reutiliza.
- El estado destructivo no depende del color (texto y botón con verbo explícito, §24).

**Razón**: reutiliza `DisposicionCuenta`, `CampoContrasena` y los tokens; sin cambios de navegación
global.

## 11. Pruebas

- **Unitarias**: regla del presidente único (uno, dos, dos clubes, otro rol, DIRECTIVO).
- **Integración**: borrado total de una familia con tres jugadores en dos clubes; marcador intacto y
  goles fuera; presidente único bloqueado y no único permitido; contraseña incorrecta y contador;
  otra sesión deja de servir; correo y documento libres y registro nuevo sin nada de lo anterior;
  autoría anónima en comunicado y resultado; invitaciones anuladas; DESARROLLADOR 404; sin cambios
  en otras cuentas ni otros clubes; atomicidad (un fallo simulado no deja nada borrado).
- **Guardián de esquema**: recorre el modelo de EF y, para cada clave foránea hacia `Usuario` o
  `UsuarioRol`, exige `Cascade` o `SetNull` (o `NoAction` con borrado explícito listado). Una tabla
  futura de pagos lo rompe a propósito.
- **Frontend**: bloqueo del presidente único, lista de jugadores, error de contraseña, éxito y
  redirección, página pública sin sesión.
- **Contrato**: las dos rutas entran solas en las pruebas de contrato y de acceso; `/api/cuenta/**`
  sin sesión da 401.

## 12. Tamaño y no regresión

`scripts/verificar-tamano.ps1` al empezar. Los dos servicios nuevos quedan por debajo de 120 líneas.
`MiPerfil.tsx` (≈60) y `Entrar.tsx` solo suman un enlace.
