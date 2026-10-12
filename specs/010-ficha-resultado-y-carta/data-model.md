# Modelo de datos: ficha, resultado y carta por la familia

**Funcionalidad**: `010-ficha-resultado-y-carta` | **Fecha**: 2026-10-11

Una tabla nueva (`CartasJugador`), columnas nuevas en `Partidos` y `PartidosJugador`, y una
migración: `FichaResultadoYCarta`. Todo bajo el filtro de club de la 001 (§7.1). Las decisiones
están en [research.md](research.md); el contrato, en [contracts/api.yaml](contracts/api.yaml).

Convenciones de las specs anteriores: `ClubId` obligatorio con foránea a `Clubes`, fechas de
registro en UTC (`...En`), autores por cuenta (`...PorUsuarioId`, a nulo si se elimina) y por nombre
copiado en el momento (§13).

## Enumeraciones nuevas (`LaPecosa.Dominio/Enumeraciones`)

| Enumeración | Valores | Guardada como |
| --- | --- | --- |
| `OrigenResultado` | `FAMILIA`, `GESTION` | texto |
| `PosicionJugador` | `PORTERO`, `DEFENSA`, `MEDIOCAMPISTA`, `DELANTERO` | texto |
| `RarezaCarta` | `BRONCE`, `PLATA`, `ORO` | no se guarda (calculada) |

`GESTION` es quien entrena las categorías del partido o el PRESIDENTE.

## Partido (`Partidos`): columnas nuevas

| Campo | Tipo | Reglas |
| --- | --- | --- |
| `OrigenResultado` | `OrigenResultado` | Nulo hasta el resultado. `FAMILIA` cuando lo publica una familia; `GESTION` en cuanto quien entrena o el PRESIDENTE lo registra o corrige, y ya no vuelve a `FAMILIA` |
| `PublicadoEn` | timestamptz | Primera vez que se puso el marcador; no cambia después |
| `PublicadoPorUsuarioId` | uuid | Cuenta que lo puso primero; foránea a `Usuarios`, a nulo si se elimina |
| `PublicadoPorNombre` | texto ≤ 161 | Nombre del integrante que lo puso (el jugador, si fue su familia), copiado (§13) |
| `PublicadoPorFamilia` | bool | Verdadero si la primera publicación fue de una familia |

Restricción de base nueva: `OrigenResultado`, `PublicadoEn` y `PublicadoPorNombre` tienen valor si
y solo si `Estado = JUGADO`. Las columnas `ResultadoCambiado…` de la 009 siguen siendo el último
cambio del marcador.

**Transiciones** (amplía las de la 009):

```text
PROXIMO ──familia publica (ya empezó)───────────▶ JUGADO  (origen FAMILIA, publicado = familia)
PROXIMO ──quien entrena registra (ya empezó)────▶ JUGADO  (origen GESTION, publicado = gestión)
JUGADO  ──quien entrena corrige─────────────────▶ JUGADO  (origen GESTION)
JUGADO  ──familia indica lo de su jugador───────▶ JUGADO  (marcador y origen sin cambio)
JUGADO  ──familia intenta cambiar el marcador───▶ 409 marcador_ya_publicado
```

## PartidoJugador (`PartidosJugador`): columnas nuevas

| Campo | Tipo | Reglas |
| --- | --- | --- |
| `IndicadoPor` | `OrigenResultado` | Nulo: nadie indicó nada (RF-011). `FAMILIA` si lo indicó su familia; `GESTION` si lo corrigió quien entrena o el PRESIDENTE (research §2) |
| `IndicadoEn` | timestamptz | Última vez que se indicó |
| `IndicadoPorUsuarioId` | uuid | Foránea a `Usuarios`, a nulo si se elimina |
| `IndicadoPorNombre` | texto ≤ 161 | Copiado (§13) |

Restricción de base nueva: los tres datos `Indicado…` (menos la cuenta) son todos nulos o todos con
valor, y `IndicadoPor` es nulo si el partido no está jugado (lo garantiza el servicio; la base solo
comprueba la coherencia de la fila).

**Reglas**:

- La familia escribe solo la fila de su jugador, y solo si `IndicadoPor` no es `GESTION` (RF-012).
- Al guardar quien entrena, una fila pasa a `GESTION` si su `Jugo` o sus `Goles` cambian, o si queda
  como que jugó sin indicación previa. Si queda igual, conserva su origen (research §2).
- `Goles = 0` si no `Jugo`, y la suma de `Goles` del partido `<= GolesClub` (sin cambio de la 009).
- Al convocar, todo nulo. El retiro y la baja de la convocatoria borran la fila como en la 009.

## CartaJugador (`CartasJugador`): nueva

| Campo | Tipo | Reglas |
| --- | --- | --- |
| `UsuarioRolId` | uuid | Clave y foránea al jugador, en cascada: una carta por jugador y club |
| `ClubId` | uuid | Obligatorio |
| `Dorsal` | int | Opcional; de 1 a 99. Puede repetirse entre jugadores |
| `Posicion` | `PosicionJugador` | Opcional |
| `Ritmo`, `Tiro`, `Pase`, `Regate`, `Defensa`, `Fisico` | int | Opcionales; de 1 a 99. Las seis nulas o las seis con valor (RF-022) |
| `EstadisticasCambiadasEn` | timestamptz | Último cambio de las seis; nulo si nunca se pusieron |
| `EstadisticasCambiadasPorUsuarioId` | uuid | A nulo si la cuenta se elimina |
| `EstadisticasCambiadasPorNombre` | texto ≤ 161 | Copiado (§13) |
| `DorsalCambiadoEn` | timestamptz | Último cambio de dorsal o posición |
| `DorsalCambiadoPorUsuarioId` | uuid | A nulo si la cuenta se elimina |
| `DorsalCambiadoPorNombre` | texto ≤ 161 | Copiado (§13) |

Restricciones de base: rangos 1–99; las seis estadísticas todas nulas o todas con valor; cada sello
completo o vacío. La fila nace con el primer cambio: un jugador sin fila tiene una carta sin dorsal,
sin posición y sin números.

**Reglas**:

- Estadísticas: las ponen quien entrena la categoría activa del jugador y el PRESIDENTE, sobre un
  jugador activo (research §8 y §12.2). Guardar las seis vacías las borra (vuelve a "sin números").
- Dorsal y posición: además, la cuenta del jugador sobre su jugador. Vale el último cambio.
- Si nada cambió, no se crea la fila ni se mueve el sello (como la ficha de la 005).
- No se calculan de partidos ni de entrenamientos ni los alteran (RF-024).
- Se conserva al cambiar de categoría (caso límite 7) y al retirarse; se borra en cascada si se
  borra el integrante (cambio de rol, §12.2).

## Entidades que cambian

| Entidad | Cambio |
| --- | --- |
| `Partido` | Las columnas de arriba; la documentación dice que también lo publica una familia (§17.3) |
| `PartidoJugador` | Las columnas de arriba; la documentación dice quién indica y quién corrige |
| `UsuarioRol` | Solo documentación: de él cuelga su carta |

## Vistas calculadas (sin tabla)

### CartaDto

```text
usuarioRolId, nombres, apellidos
fotoActualizadaEn         la de la foto del jugador, o ausente (figura genérica)
dorsal, posicion          o ausentes
estadisticas              { ritmo, tiro, pase, regate, defensa, fisico } o ausente
valoracion                ReglaValoracionDeCarta: round(promedio, mitad hacia arriba) o ausente
rareza                    ReglaRarezaDeCarta(porcentaje de la temporada)
permisos                  { cambiaEstadisticas, cambiaDorsalYPosicion, exporta }
ultimoCambioEstadisticas  UltimoCambioDto; solo si permisos.cambiaEstadisticas (RF-028)
```

El escudo y los colores no van en la carta: la pantalla ya los tiene de la identidad del club
(spec 007) y la imagen los lee al dibujarse, así un cambio del PRESIDENTE se ve al instante
(escenario 3.9).

### Porcentaje de la temporada

```text
temporada = año de la fecha de hoy en hora de Colombia
registrados = AsistenciasEntrenamiento del jugador, de entrenamientos no cancelados,
              con Fecha entre el 1 de enero y el 31 de diciembre de la temporada
asistidos   = de esas, con Presente
porcentaje  = CalculoDeAsistencia.Porcentaje(asistidos, registrados)
rareza      = registrados = 0 → BRONCE; < 60 → BRONCE; 60..84 → PLATA; >= 85 → ORO
```

### Participación propia (`MiParticipacionDto`)

Solo para la cuenta de un jugador convocado a un partido que ya empezó y no está cancelado:
`jugo`, `goles`, `indicadoPor` (o ausente) y `puedeCambiar = indicadoPor != GESTION`. Nunca lleva
datos de otro convocado ni la suma de los demás.

## Validación del resultado de la familia (`ReglaResultadoDeFamilia`)

En este orden, el primer problema:

```text
Estado = CANCELADO                                  → 409 actividad_cancelada
Estado = PROXIMO y no empezó                         → 409 partido_sin_empezar
GolesClub nulo y no se envía marcador                → 400 falta_marcador
GolesClub con valor y se envía marcador              → 409 marcador_ya_publicado
fila.IndicadoPor = GESTION                           → 409 indicacion_corregida
goles o marcador fuera de 0..99                      → 400 datos_invalidos
no jugó y goles > 0                                  → 400 goles_de_quien_no_jugo
goles + goles de los demás > goles del club          → 400 goles_superan_marcador (sin nombres)
```
