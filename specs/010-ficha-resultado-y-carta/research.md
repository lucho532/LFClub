# Investigación y decisiones: ficha, resultado y carta por la familia

**Funcionalidad**: `010-ficha-resultado-y-carta` | **Fecha**: 2026-10-11

Cada decisión sigue el formato Decisión / Motivo / Alternativas. Ninguna queda como "NEEDS
CLARIFICATION": las dos que la spec necesitaba las tomó el propietario (constitución 4.8.0). Las
que interpretan la spec sin una regla expresa están en el §12; el propietario las aprobó el
2026-10-11.

## §1. Qué se guarda del resultado publicado por la familia

**Decisión**: sin tabla nueva. `Partido` suma el **origen** del último cambio del marcador
(`FAMILIA` o `GESTION`) y los datos de la **primera publicación** (cuándo, quién y si fue una
familia), que nunca se sobrescriben. `PartidoJugador` suma la **indicación** de cada convocado:
origen (`FAMILIA`, `GESTION` o nulo si nadie indicó nada), cuándo, quién (cuenta) y su nombre
copiado (§13). Lo que ya existe (`ResultadoCambiadoEn/Por…`) sigue siendo el último cambio.

**Motivo**: RF-009 pide quién publicó y cuándo, y quién indicó lo de cada jugador y cuándo; RF-012
necesita saber si lo de un jugador ya lo corrigió quien entrena. Son atributos del partido y del
convocado, uno a uno, sin historial (§18).

**Alternativas**: una tabla `PublicacionResultado` con cada envío: es historial, que §18 no pide.
Un solo campo "último autor" en el partido: pierde quién publicó primero cuando quien entrena
corrige.

## §2. Qué es "corregir lo de un jugador" (RF-012)

**Decisión**: al guardar el resultado, quien entrena o el PRESIDENTE **corrigen** a un convocado
cuando su guardado **cambia** lo que tenía (si jugó o sus goles), o cuando lo marca como que jugó
sin que nadie lo hubiera indicado. Esa fila pasa a origen `GESTION` y la familia ya no la cambia.
Si el guardado deja una fila igual que estaba, esa fila conserva su origen: la de una familia
sigue siendo suya; la que nadie indicó sigue sin indicar. La decisión por fila es una regla pura,
`ReglaCorreccionDeIndicacion`, que recibe la fila antes y después y devuelve el origen nuevo.

**Motivo**: "corregir" es cambiar. Así un entrenador que solo ajusta el marcador no le quita a las
demás familias la posibilidad de indicar lo de sus hijos (RF-004), y un convocado sin indicar puede
completarlo su familia o quien entrena (caso límite 3). El formulario de quien entrena ya envía
todas las filas; comparar antes y después es local a la transacción.

**Alternativas**: todo guardado de quien entrena corrige todas las filas: deja sin voz a las
familias que todavía no habían indicado nada y contradice el caso límite 3. Un botón "corregir"
por fila: más pantalla sin necesidad (§19). Es un supuesto (§12.1).

## §3. "Vale el primero" con publicaciones simultáneas

**Decisión**: todas las escrituras del resultado (familia y quien entrena) toman el bloqueo del
club (`IRepositorioClub.BloquearAsync`, el mismo de la ficha y la foto) dentro de su transacción y
vuelven a leer el partido y los convocados después de bloquear. La segunda familia encuentra el
marcador puesto y recibe `409 marcador_ya_publicado`; la pantalla recarga y le deja indicar solo
lo de su jugador.

**Motivo**: la suma de goles de todos los convocados no puede pasar del marcador (RF-006). Dos
familias que suman goles a la vez, o una familia y el entrenador, podrían dejarla incoherente si
cada una valida sin ver a la otra. El bloqueo del club ya es el patrón del proyecto y el volumen es
de decenas de partidos (§19).

**Alternativas**: `UPDATE … WHERE GolesClub IS NULL` y contar filas: resuelve el marcador pero no la
suma de goles. Concurrencia optimista con versión de fila: otra técnica para lo mismo.

## §4. Un endpoint para la familia

**Decisión**: `PUT /api/clubes/{clubId}/partidos/{partidoId}/resultado/mi-jugador`, solo rol
JUGADOR (403 `rol_no_autorizado` a los demás). Lleva `jugo`, `goles` y, opcionalmente,
`golesClub` y `golesRival`.

- Partido sin marcador: el marcador es obligatorio; se publica junto con lo del jugador.
- Partido con marcador: el marcador no se envía; si se envía, `409 marcador_ya_publicado`, aunque
  sea igual.
- La fila del jugador ya corregida por quien entrena: `409 indicacion_corregida`.
- El jugador de la petición no está convocado: `404 no_encontrado`, también si el partido ya está
  jugado y lo ve todo el club (escenario 1.3).

La respuesta es el `PartidoDto` de siempre, con la parte nueva `miParticipacion`. Nunca la
planilla.

**Motivo**: un solo formulario para los dos momentos (publicar e indicar después), con la regla en
el servidor (§15). Separarlo del `PUT …/resultado` de quien entrena deja intacta la autorización de
la 009 y hace imposible que una cuenta de jugador mande filas de otros (RF-005).

**Alternativas**: abrir el `PUT …/resultado` a la cuenta del jugador con filas filtradas: mezcla dos
contratos y obliga a la familia a mandar estructuras con identificadores de otros. Dos endpoints
(publicar e indicar): duplica validaciones sin ganar nada.

## §5. Regla pura del resultado de la familia

**Decisión**: `ReglaResultadoDeFamilia.Validar(...)` en `Dominio/Reglas`, pura, que recibe el
estado del partido, si ya empezó, el marcador actual (o nulo), el marcador enviado (o nulo), el
origen actual de la fila, `jugo`, `goles` y la suma de goles de los **demás** convocados, y devuelve
el primer problema: `PartidoCancelado`, `PartidoSinEmpezar`, `FaltaMarcador`,
`MarcadorYaPublicado`, `IndicacionCorregida`, `DatosFueraDeRango`, `GolesDeQuienNoJugo`,
`GolesSuperanMarcador`. `ReglaResultadoDePartido` (la de la 009) no cambia: sigue siendo la de
quien entrena.

**Motivo**: §20 pide pruebas de reglas; las del escenario 1 se prueban sin base de datos. El
motivo `goles_superan_marcador` ya existe y no nombra a nadie (RF-006): la familia solo recibe un
texto genérico, y la suma de los demás nunca sale del servidor.

**Alternativas**: validar en el servicio: no se prueba aislado y crece por encima de 250 líneas.

## §6. Qué ve cada uno del resultado

**Decisión**:

- `PartidoDto` suma `miParticipacion` (solo para la cuenta de un jugador convocado a un partido
  que ya empezó): `jugo`, `goles`, `indicadoPor` (`FAMILIA`, `GESTION` o ausente) y `puedeCambiar`.
  Nunca lleva datos de otro convocado.
- `PermisosDeActividadDto` suma `indicaResultado`: es la cuenta de un convocado, el partido ya
  empezó, no está cancelado y su fila no está corregida. En los entrenamientos siempre es falso.
- `PartidoDto` suma `publicacion` solo para quien ve la planilla (quien entrena alguna categoría
  y el PRESIDENTE): cuándo, por quién y si fue una familia, y `origenResultado` (`FAMILIA` o
  `GESTION`), con el que la pantalla escribe "Publicado por una familia" o "Corregido por quien
  entrena" (RF-034).
- `ConvocadoDto` de la planilla suma `indicacion`: origen, nombre y fecha (RF-009).
- Para los demás integrantes, un partido jugado sigue como en la 009: marcador y goleadores, sin
  saber qué familia publicó (escenario 1.11).

**Motivo**: RF-005, RF-009 y RF-011. Son campos nuevos en respuestas existentes; las partes que no
aplican se omiten del JSON, no van vacías (§23).

**Alternativas**: un endpoint aparte para la participación propia: otra petición para lo mismo que
ya trae el detalle.

## §7. La familia corrige la identidad (historia 2)

**Decisión**: `ReglaAccesoAFicha` da `CorrigeIdentidad` también a la cuenta del jugador sobre su
propio jugador. `PUT …/ficha/identidad` admite el rol JUGADOR. `ServicioIdentidadJugador.CorregirAsync`
solo ubica en categoría a un jugador sin categoría cuando corrige el PRESIDENTE; cuando corrige la
familia, la categoría no se toca en ningún caso (RF-015, supuesto 2 de la spec). Validaciones,
sello del último cambio y documento siguen como en la 005.

**Motivo**: es un cambio de permiso, no un formulario nuevo: la pantalla de corrección de la 005
ya existe y se muestra según `permisos.corrigeIdentidad`.

**Alternativas**: un endpoint propio para la familia: duplicaría validaciones y sello.

## §8. Quién "entrena la categoría" para la carta

**Decisión**: `AccesoACarta` (nuevo, en `Aplicacion/Utilidades`) usa `AccesoAFicha` para decidir si
quien pregunta **ve** la carta (RF-020), y después consulta si tiene una asignación activa en la
categoría activa del jugador, **sea cual sea su rol** (ENTRENADOR, DIRECTIVO o PRESIDENTE), como
en §8 "Entrenador". Con eso, la regla pura `ReglaAccesoACarta` da `CambiaEstadisticas`,
`CambiaDorsalYPosicion` y `Exporta`:

| Quien pregunta | Ve | Estadísticas | Dorsal y posición | Exporta |
| --- | --- | --- | --- | --- |
| PRESIDENTE | Sí, cualquier jugador | Sí, si está activo | Sí, si está activo | Sí, también retirado |
| DIRECTIVO asignado a su categoría | Sí | Sí | Sí | No |
| DIRECTIVO no asignado | Sí | No | No | No |
| ENTRENADOR de su categoría | Sí, si está activo | Sí | Sí | No |
| ENTRENADOR de otra categoría | No (404) | — | — | — |
| Cuenta del jugador, su jugador | Sí | No | Sí | Sí |
| Otra cuenta de jugador, DESARROLLADOR, otro club | No (404) | — | — | — |

`AccesoAFicha` no cambia: la ficha sigue viéndose como hoy.

**Motivo**: la spec define "quien entrena" como el ENTRENADOR, PRESIDENTE o DIRECTIVO asignado, y
el escenario 3.5 niega a "un DIRECTIVO" en el sentido de no asignado. Que la carta se vea por
`AccesoAFicha` garantiza "exactamente los mismos que ven la ficha" (RF-020) sin una segunda regla de
visibilidad.

**Alternativas**: extender `ReglaAccesoAFicha` con los permisos de carta: mezcla dos cosas y obliga
a tocar las pruebas de la ficha.

## §9. Modelo de la carta

**Decisión**: una tabla nueva `CartasJugador`, una fila por jugador y club, que nace con el primer
cambio (como la ficha). Dorsal (1–99, opcional), posición (enumeración `PosicionJugador`: `PORTERO`,
`DEFENSA`, `MEDIOCAMPISTA`, `DELANTERO`, opcional), seis estadísticas (1–99, todas o ninguna, con
una restricción de comprobación en la base) y dos sellos: el del último cambio de las estadísticas
y el del último cambio de dorsal y posición. La valoración y la rareza no se guardan. Los rangos
y el "todas o ninguna" los valida la regla pura `ReglaEstadisticasDeCarta`, y la base los repite con
restricciones de comprobación.

**Motivo**: son datos que no son de la ficha ni del integrante, los escriben roles distintos y
tienen sellos propios (RF-028). Separados de `FichaJugador`, cambiar la carta no mueve el "último
cambio" de la ficha.

**Alternativas**: columnas en `FichaJugador`: mezcla sellos y obliga a decidir qué cambio "sella"
la ficha. Columnas en `UsuarioRol`: tabla central de identidad, la más leída.

## §10. Valoración y rareza

**Decisión**: dos reglas puras.

- `ReglaValoracionDeCarta`: promedio de las seis, redondeado a la mitad hacia arriba
  (`MidpointRounding.AwayFromZero`, como `CalculoDeAsistencia`). Sin estadísticas, nula.
- `ReglaRarezaDeCarta`: con el porcentaje de `CalculoDeAsistencia` sobre los entrenamientos no
  cancelados de la temporada (año calendario de la fecha de hoy en hora de Colombia) con asistencia
  registrada del jugador: menos de 60, `BRONCE`; de 60 a 84, `PLATA`; 85 o más, `ORO`. Sin
  registros, `BRONCE`. Se compara el porcentaje ya redondeado, el mismo que muestra la sección de
  estadísticas de la 009 (84,5 % da 85 y es Oro).

El conteo de la temporada es una consulta nueva de `IRepositorioEstadisticas` con el rango de
fechas, sin tocar la de la 009.

**Motivo**: RF-023 y RF-026; la rareza se recalcula sola porque no se guarda (escenario 3.11).

**Alternativas**: guardar la rareza y recalcularla al registrar asistencia: un dato derivado que
§17 pide calcular.

## §11. Exportar la carta como imagen

**Decisión**: la API **dibuja la imagen** en `GET …/jugadores/{id}/carta/imagen`, que responde
`image/png` solo a la cuenta del jugador (su jugador) y al PRESIDENTE; a DIRECTIVOS y
ENTRENADORES, 403 `rol_no_autorizado`; a cualquier otra cuenta, 404. El dibujo lo hace
`GeneradorImagenCarta` en `Infraestructura` con **SkiaSharp** (MIT) y los activos nativos de Linux
sin dependencias para el contenedor, con la fuente **Barlow Condensed** (licencia OFL) incluida como
recurso, la misma de la pantalla. La aplicación solo conoce la interfaz `IGeneradorImagenCarta`.
La pantalla dibuja la carta con HTML y CSS siguiendo el mismo **plano** (tamaño, posiciones,
tipografías, degradado y contraste), que se fija en [contracts/carta.md](contracts/carta.md).
La imagen mide 750 × 1050 px: una carta de 2,5 × 3,5 pulgadas a 300 ppp, lista para imprimir.

**Motivo**: RF-031 pide que el sistema niegue la imagen a quien la pida directamente (§15). Si la
imagen se armara en el navegador, cualquiera que ve la carta tendría en su equipo todo lo necesario
para generarla, y el servidor no podría negarle nada. Dibujarla en el servidor hace real la
barrera, y el plano compartido hace que coincida con la pantalla (RF-030). SkiaSharp lee PNG, JPEG y
WebP, los tres formatos de la foto y del escudo (§14.2, spec 007).

**Alternativas**:

- Generarla en el navegador (por ejemplo con una biblioteca de captura del DOM) y que la API solo
  "autorice": la barrera sería solo de pantalla, que §15 no admite.
- Que la API dibuje la carta también para mostrarla en pantalla: entonces cualquiera que la ve
  descarga el mismo PNG, que es exportar.
- ImageSharp: su licencia actual no es libre para todo uso comercial; SkiaSharp sí lo es.
- Dibujar SVG en el servidor y convertirlo: necesita igualmente un motor de dibujo.

Es la única dependencia nueva de la funcionalidad (ver "Seguimiento de complejidad" del plan).

## §12. Supuestos para el propietario

**Aprobados por el propietario el 2026-10-11, tal como están.** No cambian el modelo; cada uno
se ajusta en una regla.

1. **Qué es corregir lo de un jugador** (§2): solo cuando quien entrena cambia sus datos o lo marca
   como que jugó. Si guarda la fila igual, la familia la sigue pudiendo cambiar.
2. **La carta de un jugador retirado no se edita**: estadísticas, dorsal y posición se niegan con
   `409 jugador_retirado`, como la foto (spec 007). El PRESIDENTE la sigue viendo y exportando
   (escenario 4.2).
3. **El marcador repetido también se rechaza**: una familia que llega segunda con el mismo marcador
   recibe `409 marcador_ya_publicado` y la pantalla le muestra el publicado; no se compara.
4. **La carta no muestra el porcentaje de asistencia**: solo la rareza. El porcentaje sigue en la
   sección de estadísticas de la ficha (spec 009), y la imagen exportada no lo lleva.

## §13. Pantallas

**Decisión**:

- **Carta**: pantalla nueva `/club/:clubId/jugadores/:id/carta`, a la que se llega desde la ficha
  ("Ver carta"). Muestra la carta, la rareza escrita además del color del marco (RF-034), y debajo,
  según los permisos: "Dorsal y posición", "Estadísticas" con el último cambio y "Descargar imagen".
  El botón de descargar pide la imagen a la API y la guarda como `carta-<apellidos>-<nombres>.png`.
- **Partido** (cuenta de un convocado): `SeccionResultadoFamilia` en `DetallePartido`. Sin marcador:
  marcador, "¿Jugó?" y goles. Con marcador: el marcador publicado y lo de su jugador, o el texto
  "Quien entrena corrigió lo de tu jugador" sin formulario.
- **Partido** (quien entrena y PRESIDENTE): `SeccionResultado` muestra quién publicó y cuándo, y el
  estado por texto ("Falta resultado", "Publicado por una familia", "Corregido por quien entrena");
  la planilla del resultado muestra en cada fila quién lo indicó y cuándo.
- **Ficha**: la familia ve el botón de corregir la identidad que ya usa el PRESIDENTE.

El lienzo (§24) no tiene la carta. Su estructura sigue el estilo de cartas de los videojuegos que
pide §14.3 con los componentes y tokens del proyecto; la versión final la revisa el propietario en
la validación, como en las specs anteriores.

## §14. Tamaño de los archivos (§2.3)

**Decisión**: el resultado de la familia va en un servicio propio (`ServicioResultadoFamilia`), no
en `ServicioResultado` (122 líneas). La carta se reparte en `ServicioConsultaCarta`,
`ServicioCarta` (estadísticas, dorsal y posición) y `ServicioImagenCarta`. El dibujo se divide en
`GeneradorImagenCarta` (orquesta) y `PlanoCarta` (medidas y colores). En la pantalla, la carta se
divide en `CartaJugador.tsx` (dibujo), `PantallaCarta.tsx`, `FormularioEstadisticas.tsx` y
`FormularioDorsalYPosicion.tsx`.

**Motivo**: ningún archivo por encima de 250 líneas desde el principio.
