<!--
Informe de impacto de sincronización
- Cambio de versión: borrador previo sin aprobar → 1.0.0
- Origen: texto entregado por el propietario del proyecto el 2026-10-06. Sustituye por completo
  al borrador anterior de siete principios, que nunca fue aprobado.
- Principios modificados: todos (se reemplaza el documento completo)
- Secciones añadidas: 1 a 29 del texto del propietario
- Secciones eliminadas: los siete principios, "Restricciones del producto", "Flujo de trabajo" y
  "Governance" del borrador
- Añadido fuera del texto del propietario: únicamente la línea final de versión
- Pendientes: las decisiones abiertas están en la sección 28

Enmienda 1.0.0 → 1.1.0 (2026-10-06), decidida por el propietario:
- §4, §5, §6.2 y §20: carpetas, proyectos y capas pasan a nombres en español
- §8 y §12.2: toda cuenta entra como JUGADOR y el rol asignado lo reemplaza como único rol
- §8, §12.2 y §20: el DIRECTIVO aprueba cualquier ingreso y puede asignar el rol ENTRENADOR a la
  cuenta que acaba de aprobar

Enmienda 1.1.0 → 1.2.0 (2026-10-06), decidida por el propietario del proyecto:
- Todo el documento: el rol PROPIETARIO pasa a llamarse PRESIDENTE
- §4 y §20: el repositorio se divide en backend/ y frontend/; desaparece Valfor.Web
- §2.2, §8, §12.3 y §12.5: nuevo rol único DESARROLLADOR, que crea usuarios con rol directo y
  contraseña por defecto de un solo uso
- §12.2: la ficha de Jugador se elimina al pasar la cuenta a ENTRENADOR o DIRECTIVO
- §6, §6.3 y §12.4: inicio de sesión completo para todos y Brevo para los correos de la cuenta
- §12.1: el registro pide correo electrónico
- §28: dos decisiones pendientes nuevas

Enmienda 1.2.0 → 2.0.0 (2026-10-07), decidida por el propietario del proyecto. Cambio mayor: se
redefine el principio de un solo club.
- Título y §1: la plataforma pasa a ser multiclub; Valfor F.C. es el primer club
- §7: "Un solo club" se reemplaza por "Varios clubes", con aislamiento entre clubes e identidad
  (escudo y colores) por club
- §8, §12.3 y §12.5: el DESARROLLADOR tiene un panel de administración de la plataforma; crea
  clubes, cambia su escudo y colores y crea sus usuarios; no accede a nada más
- §2.2: nuevo término Club
- §9, §16.7, §19 y §24: ajustes para varios clubes
- §20: pruebas de aislamiento entre clubes y del alcance del DESARROLLADOR
- §28: se cierra "Alcance del DESARROLLADOR" y se abren seis decisiones nuevas

Enmienda 2.0.0 → 2.1.0 (2026-10-07), decidida por el propietario del proyecto:
- §12.1 y §6.3: el registro pasa a ser por invitación enviada por correo
- §7.2 y §7.3: cada usuario guarda el club al que pertenece y ve su nombre; el resto de la
  configuración la editan el DESARROLLADOR y el PRESIDENTE
- §10: el documento es único por pareja "documento + club"
- §6.2: cada club recibe su dinero en su propia cuenta de la pasarela
- §7.4: suspensión y baja de clubes; congelación automática por impago de la plataforma
- §24: tema claro y oscuro (desde la 4.8.1, el cambio va en el menú del perfil)
- §20: pruebas nuevas
- §28: se cierran cinco decisiones y se abren seis de detalle

Enmienda 2.1.0 → 3.0.0 (2026-10-07), decidida por el propietario del proyecto. Cambio mayor: se
elimina la creación de usuarios con contraseña por defecto y el rol pasa a ser por club.
- §12.5: el DESARROLLADOR ya no crea usuarios ni asigna contraseñas; al crear un club invita
  obligatoriamente a su PRESIDENTE por correo
- §12.1: datos de registro (nombre, apellidos, correo del acudiente, documento, fecha de
  nacimiento)
- §12.4: inicio de sesión con correo o documento
- §7.3, §8 y §10: una persona puede pertenecer a varios clubes, con un rol en cada uno, un solo
  inicio de sesión y un desplegable para elegir club
- §7.4: estados suspendido, dado de baja y eliminado; el DESARROLLADOR levanta y aplica la
  suspensión manualmente; eliminar borra toda la información del club
- §20: pruebas ajustadas
- §28: se cierran dos decisiones y se abren cinco

Enmienda 3.0.0 → 3.1.0 (2026-10-07), decidida por el propietario del proyecto:
- §12.1: el registro vuelve a pedir celular y nombre del responsable; se explicitan tipo de
  documento y contraseña
- §8 y §12.4: con correo compartido se elige el integrante; con documento se ve solo ese
  integrante
- §12.4: una persona puede solicitar la eliminación de su cuenta
- §8: un PRESIDENTE no puede quitarse el rol ni eliminarse sin otro PRESIDENTE
- §6.1: una sola aplicación Android para todos los clubes
- §7.4: se confirma que dar de baja y eliminar son dos pasos
- §28: se cierran cinco decisiones y se abren tres

Enmienda 3.1.0 → 3.2.0 (2026-10-07), decidida por el propietario del proyecto:
- §12.1: el correo es único; no se registra una cuenta nueva con un correo ya registrado
- §12.1.2 (nueva), §8 y §12.4: los hermanos se agregan desde la ficha del jugador ya registrado y
  comparten los datos de contacto; una cuenta tiene un correo, una contraseña y varios jugadores
- §12.4: la eliminación de la cuenta se cumple sola y conserva los pagos
- §8 y §12.5: un club puede tener varios presidentes; al adicional lo elige el DESARROLLADOR u
  otro PRESIDENTE del club
- §28: se cierran tres decisiones y se abren tres de detalle

Enmienda 3.2.0 → 3.3.0 (2026-10-07), decidida por el propietario del proyecto:
- §12.1.2: el hermano agregado desde la ficha pasa por la sala de espera
- §12.4: de una cuenta eliminada solo se conservan los pagos con nombre completo y documento; se
  eliminan sus estadísticas deportivas
- §12.5: un PRESIDENTE elige a otro dándole el rol a alguien ya registrado en el club
- §28: se cierran las tres decisiones de detalle

Enmienda 3.3.0 → 3.4.0 (2026-10-07), decidida por el propietario del proyecto en las aclaraciones
de la spec 001:
- §8: el DESARROLLADOR invita presidentes adicionales, quita el rol a un presidente (eligiendo
  entre darle otro rol o eliminarlo del club) y revierte la baja de un club; solo se elimina un
  club dado de baja
- §12.4: bloqueo de la cuenta tras 5 intentos fallidos, hasta recuperar la contraseña por correo

Enmienda 3.4.0 → 3.5.0 (2026-10-07), decidida por el propietario del proyecto:
- Título y §1: el proyecto pasa a llamarse La Pecosa; Valfor F.C. es solo su primer cliente
- §1: formas del nombre sin espacios (LaPecosa, la-pecosa, lapecosa)
- §4: los proyectos del backend pasan de Valfor.* a LaPecosa.*

Enmienda 3.5.0 → 3.6.0 (2026-10-07), decidida por el propietario del proyecto tras el análisis de
la spec 001:
- §7.1: además del panel del DESARROLLADOR, se admiten como excepciones acotadas las consultas de
  la propia cuenta (iniciar sesión y listar sus clubes) y abrir una invitación por su enlace

Enmienda 3.6.0 → 3.7.0 (2026-10-07), decidida por el propietario del proyecto en las aclaraciones
de la spec 002:
- §8 y §12.1: dentro del club invitan el PRESIDENTE y los DIRECTIVOS; toda invitación sirve una
  sola vez, caduca y queda ligada a su correo
- §12.1 y §12.1.1: quien se registra con una invitación del club pasa siempre por la sala de
  espera
- §8 y §12.1.1: el PRESIDENTE o un DIRECTIVO pueden rechazar un ingreso en espera; el rechazo
  borra a la persona del club y solo puede volver con una invitación nueva
- §14: el rechazo de un ingreso en espera es una eliminación física justificada
- §20: pruebas nuevas
- §28: se cierran tres decisiones
- Principios modificados: ninguno renombrado. Secciones añadidas o eliminadas: ninguna
- Pendientes: ninguno nuevo; siguen abiertas las siete decisiones restantes de la §28

Enmienda 3.7.0 → 3.8.0 (2026-10-08), decidida por el propietario del proyecto en las aclaraciones
de la spec 003:
- §2.2 y §11.3 (nueva): una categoría puede dividirse en equipos; un jugador puede estar en varios
- §11: una categoría corresponde a un único año de nacimiento
- §12.1.1: si no existe la categoría del año, el jugador aprobado queda sin categoría hasta que se
  crea
- §8 y §12.3: un PRESIDENTE o un DIRECTIVO puede quedar asignado como entrenador de una categoría
  sin cambiar de rol
- §7.5 y §8: el entrenador ve toda su categoría, de cualquier equipo; la familia ve el nombre de
  los entrenadores de su jugador
- §8 y §14.1 (nueva): el PRESIDENTE retira del club a un jugador y lo reincorpora
- §14: borrar una categoría o un equipo que nunca se usó es una eliminación física justificada
- §20: pruebas nuevas
- Principios modificados: ninguno renombrado. Secciones añadidas: §11.3 y §14.1. Secciones
  eliminadas: ninguna
- Pendientes: ninguno nuevo; siguen abiertas las siete decisiones de la §28

Enmienda 3.8.0 → 4.0.0 (2026-10-08), decidida por el propietario del proyecto. Cambio mayor: se
elimina la sala de espera para quien se registra con una invitación y el rol pasa a venir en la
invitación.
- §8, §12.1 y §12.2: la invitación del club indica el rol; el PRESIDENTE invita JUGADOR,
  ENTRENADOR o DIRECTIVO y el DIRECTIVO solo JUGADOR o ENTRENADOR; quien se registra entra directo
  con ese rol
- §8 y §12.2: el DIRECTIVO ya no asigna el rol ENTRENADOR tras aprobar un ingreso; cambiar el rol
  de alguien ya registrado es exclusivo del PRESIDENTE
- §12.1 y §12.1.1: la sala de espera, la aprobación y el rechazo quedan solo para el hermano
  agregado desde la ficha (§12.1.2); la regla de ubicación en la categoría del año pasa a la §12.1
- §12.3: una cuenta puede tener varios jugadores (se quita "uno a uno"); solo tiene ficha de
  Jugador quien entra como JUGADOR
- §1 y §2.2: el DESARROLLADOR no crea usuarios, invita al PRESIDENTE
- §8, §16.1, §16.7 y §28: no existen becas ni descuentos generales; el PRESIDENTE puede modificar
  el valor de la mensualidad de jugadores concretos
- §14 y §14.1: referencias ajustadas
- §20: pruebas ajustadas y nuevas
- §28: se cierra la parte de descuentos y becas; queda pendiente solo la matrícula
- Principios modificados: ninguno renombrado. Secciones añadidas o eliminadas: ninguna
- Pendientes: siguen abiertas las siete decisiones de la §28. El código de la spec 002 (sala de
  espera, aprobación y rechazo de ingresos) y el de invitaciones quedan por adaptar en una spec
  nueva

Enmienda 4.0.0 → 4.1.0 (2026-10-08), decidida por el propietario del proyecto durante el plan de
la spec 004:
- §8, §12.1 y §12.2: dentro del club solo el PRESIDENTE envía invitaciones; el DIRECTIVO ya no
  invita
- §8 y §12.1.1: solo el PRESIDENTE aprueba o rechaza al jugador agregado desde la ficha de un
  hermano; el DIRECTIVO ya no aprueba ni rechaza
- §8 y §12.5: el DESARROLLADOR solo invita al primer PRESIDENTE, al crear el club; ya no invita
  presidentes adicionales. Conserva reenviar esa invitación o corregir su correo mientras no se use
- §20: pruebas ajustadas
- Principios modificados: ninguno renombrado. Secciones añadidas o eliminadas: ninguna
- Pendientes: siguen abiertas las siete decisiones de la §28. La spec 004 adapta el código a esta
  versión. Hasta que exista la elección de un presidente por otro PRESIDENTE (§12.5), cada club
  tiene un solo presidente

Enmienda 4.1.0 → 4.2.0 (2026-10-10), decidida por el propietario del proyecto en las aclaraciones
de la spec 006 (2026-10-09 y 2026-10-10):
- §7.1: cuarta excepción acotada al aislamiento: el documento de un hermano o de un jugador que se
  reincorpora puede consultarse en otros clubes, y aprobar al hermano retira de allí al jugador de
  otra cuenta, sin entregar el nombre de ese club ni datos de esa cuenta
- §10: un mismo documento puede estar en dos cuentas, solo por el hermano agregado (§12.1.2); el
  inicio de sesión con ese documento abre la cuenta donde el jugador está activo
- §8 (Jugador) y §14.1: la familia puede retirar a su propio jugador; aprobar un hermano en otro
  club retira al jugador de otra cuenta con ese documento; la reincorporación se niega mientras el
  documento esté activo con otra cuenta
- §12.1.2: se admite el documento de un JUGADOR de otra cuenta en otro club, no el de otro rol
- §20: pruebas ajustadas y nuevas
- Principios modificados: ninguno renombrado. Secciones añadidas o eliminadas: ninguna
- Pendientes: siguen abiertas las siete decisiones de la §28. La spec 006 implementa esta versión

Enmienda 4.2.0 → 4.3.0 (2026-10-10), decidida por el propietario del proyecto:
- §7.2: el escudo y los colores de un club los configuran tanto el DESARROLLADOR, desde su panel,
  como el PRESIDENTE de ese club, desde "Datos del club"
- §8 (Presidente): nueva función de configurar el escudo y los colores de su club
- §20: la prueba del panel ya no reserva al DESARROLLADOR el cambio de escudo y colores; prueba
  nueva de que solo el PRESIDENTE de ese club, además del DESARROLLADOR, puede cambiarlos
- Principios modificados: ninguno renombrado. Secciones añadidas o eliminadas: ninguna
- Pendientes: siguen abiertas las siete decisiones de la §28. La spec 007 implementa esta versión

Enmienda 4.3.0 → 4.4.0 (2026-10-10), decidida por el propietario del proyecto en la spec 007:
- §14.2 (nueva): cada jugador tiene una foto propia en su ficha, distinta de la foto de perfil de
  la cuenta, que sigue siendo privada. La cargan la familia y el PRESIDENTE; la ven quienes pueden
  ver a ese jugador. Se elimina definitivamente cuando el jugador deja de pertenecer al club
- §8 (Presidente y Jugador): cargar, cambiar y quitar la foto del jugador
- §12.2 y §14.1: la foto se elimina con la ficha al cambiar de rol y al retirar al jugador; es una
  excepción expresa a la conservación de los datos del retirado
- §20: pruebas nuevas de quién cambia y quién ve la foto, y de su eliminación
- Principios modificados: ninguno renombrado. Secciones añadidas: §14.2. Eliminadas: ninguna
- Pendientes: siguen abiertas las siete decisiones de la §28. La spec 007 implementa esta versión

Enmienda 4.4.0 → 4.5.0 (2026-10-10), decidida por el propietario del proyecto en la spec 008:
- §12.6 (nueva): ficha básica de cada PRESIDENTE, DIRECTIVO y ENTRENADOR (nombre, rol, foto de
  perfil y categorías y equipos que entrena, sin documento ni contacto), que abre cualquier
  integrante de su club; y esas personas cambian por sí mismas su celular, nombres, apellidos y
  fecha de nacimiento, pero no su documento
- §14.2: la foto de perfil de una cuenta con rol PRESIDENTE, DIRECTIVO o ENTRENADOR la ven los
  integrantes de los clubes donde tiene ese rol; la de una cuenta de jugador sigue siendo privada
- §8 (Jugador): de los entrenadores, presidentes y directivos de su club ve la ficha básica, y no
  solo el nombre y los apellidos del entrenador
- §20: pruebas ajustadas y nuevas de la ficha básica, de la foto de perfil y de los datos propios
- Principios modificados: ninguno renombrado. Secciones añadidas: §12.6. Eliminadas: ninguna
- Pendientes: siguen abiertas las siete decisiones de la §28. La spec 008 implementa esta versión

Enmienda 4.5.0 → 4.5.1 (2026-10-10), de redacción, aprobada por el propietario del proyecto al
analizar la spec 008:
- §7.1: se añade a las excepciones del límite de club la de los datos propios de §12.6, que ya
  exigía que el cambio valga en todos los clubes de la persona. No crea ninguna regla nueva: alinea
  §7.1 con §12.6
- Principios modificados: ninguno renombrado. Secciones añadidas: ninguna. Eliminadas: ninguna
- Pendientes: siguen abiertas las siete decisiones de la §28. La spec 008 implementa esta versión

Enmienda 4.5.1 → 4.6.0 (2026-10-11), decidida por el propietario del proyecto en la spec 009:
- §8 (Entrenador): se toma la decisión que estaba aplazada. Un PRESIDENTE o DIRECTIVO asignado a
  una categoría actúa en ella como entrenador en lo deportivo (entrenamientos, asistencia,
  convocatorias, partidos próximos, resultados y goles). Evaluaciones, comunicados y pagos en
  efectivo del DIRECTIVO asignado se decidirán con esas funcionalidades
- §8 (introducción y Directivo): se remite a esa regla
- §17.2: la convocatoria la crea quien entrena las categorías del partido o el PRESIDENTE, y los
  partidos próximos los ven también los PRESIDENTES y DIRECTIVOS asignados a sus categorías; un
  DIRECTIVO no asignado no los ve
- §20: prueba ajustada del DIRECTIVO asignado como entrenador
- Principios modificados: ninguno renombrado. Secciones añadidas: ninguna. Eliminadas: ninguna
- Pendientes: siguen abiertas las siete decisiones de la §28. La spec 009 implementa esta versión

Enmienda 4.6.0 → 4.7.0 (2026-10-11), decidida por el propietario del proyecto:
- §8 (Jugador): la cuenta del jugador cambia cualquier dato de la ficha de su jugador, también
  nombres, apellidos y fecha de nacimiento; el documento sigue la §10 y la categoría no cambia
- §8 (Jugador) y §17.3 (nueva): la familia de un convocado publica, en un partido ya empezado, el
  marcador y si su propio jugador jugó y sus goles, sin ver a los demás convocados; vale el primer
  marcador y después solo lo cambian quien entrena las categorías del partido o el PRESIDENTE
- §14.2 y §14.3 (nueva): carta del jugador, visible para los mismos que ven su ficha; la exportan
  como imagen la propia cuenta y el PRESIDENTE, como excepción expresa a que la foto no salga de
  la plataforma; sus estadísticas las ponen quien entrena la categoría o el PRESIDENTE y nunca se
  muestran como clasificación
- §8 (Presidente y Entrenador): funciones nuevas de carta y corrección del resultado
- §20: pruebas nuevas
- §28: dos decisiones nuevas (dorsal y posición de la carta; corrección de la familia sobre su
  propio jugador)
- Principios modificados: ninguno renombrado. Secciones añadidas: §14.3 y §17.3. Eliminadas:
  ninguna
- Pendientes: nueve decisiones abiertas en la §28. Lo implementará una spec nueva (010)

Enmienda 4.7.0 → 4.8.0 (2026-10-11), decidida por el propietario del proyecto en la spec 010:
- §8 (Jugador), §14.3 y §17.3: se cierran las dos decisiones abiertas en la 4.7.0
- §14.3 y §8: el dorsal y la posición de la carta los ponen la propia cuenta, sobre su jugador,
  quien entrena su categoría y el PRESIDENTE; vale el último cambio
- §17.3 y §8 (Jugador): la familia cambia lo que indicó de su propio jugador (si jugó y sus goles)
  mientras quien entrena las categorías del partido o el PRESIDENTE no lo hayan corregido
- §20: pruebas ajustadas
- §28: se cierran "Dorsal y posición de la carta" y "Corrección de la familia sobre su propio
  jugador"
- Principios modificados: ninguno renombrado. Secciones añadidas: ninguna. Eliminadas: ninguna
- Pendientes: siete decisiones abiertas en la §28. La spec 010 implementa esta versión

Enmienda 4.8.0 → 4.8.1 (2026-10-12), aclaración pedida por el propietario del proyecto:
- §24: el cambio entre tema claro y oscuro deja de ser un botón suelto en la barra superior y pasa
  al menú del perfil, siempre a un toque; donde no hay menú de perfil sigue como botón visible. El
  nombre accesible dice a qué tema cambia. Motivo: no saturar la barra superior, sobre todo en el
  teléfono
- §20 y §28: sin cambios. Principios modificados: ninguno. Secciones añadidas y eliminadas:
  ninguna
- Pendientes: los de la 4.8.0

Enmienda 4.8.1 → 4.9.0 (2026-10-12), decidida por el propietario del proyecto en la spec 011:
- §14.4 (nueva): comunicados del club y campana. Los publican el PRESIDENTE, los DIRECTIVOS y los
  ENTRENADORES; llegan a las cuentas de jugador con un jugador activo en la audiencia; la campana
  con contador es solo de la cuenta de jugador y vive dentro de la aplicación; se retiran, no se
  editan ni se eliminan físicamente; no hay avisos con la aplicación cerrada
- §8 (Directivo): nueva función de publicar comunicados; se cierra lo que la §8 (Entrenador)
  aplazaba para el DIRECTIVO asignado en cuanto a comunicados
- §8 (Presidente, Entrenador y Jugador): remisiones a §14.4 y función de consultar comunicados
- §14: un comunicado nunca se elimina físicamente
- §20: pruebas nuevas de quién publica, a quién llega y quién retira
- §24: la campana de la barra superior, con nombre accesible y estado por texto
- §28: sin cambios. Principios modificados: ninguno renombrado. Secciones añadidas: §14.4.
  Eliminadas: ninguna
- Pendientes: las siete decisiones de la §28. La spec 011 implementa esta versión; los avisos con la
  aplicación cerrada (teléfono, número sobre el icono) quedan para una spec posterior

Enmienda 4.9.0 → 4.10.0 (2026-10-12), decidida por el propietario del proyecto en la spec 012:
- §14.4: la campana deja de ser solo de comunicados y reúne comunicados y avisos de actividades,
  con un solo contador. Se sustituye la frase "ni se crean comunicados automáticos por otros
  eventos": un entrenamiento o un partido genera avisos automáticos a las familias que ya podían
  verlo, solo dentro de la aplicación. Un aviso no es un comunicado: no lo escribe ni lo retira
  nadie del equipo del club
- §8 (Jugador): consulta también los avisos de actividades que le llegan
- §14: un aviso de actividad no se elimina físicamente por una acción de usuario
- §17.2: los avisos de partidos respetan la visibilidad de los partidos; un partido próximo solo se
  avisa a quien está convocado
- §20: pruebas nuevas de a quién llega cada aviso, de que no revela lo que la familia no puede ver
  y de que no sale nada fuera de la aplicación
- §24: la campana distingue en texto un comunicado de un aviso de actividad
- §28: sin cambios. Principios modificados: ninguno renombrado. Secciones añadidas y eliminadas:
  ninguna
- Pendientes: las siete decisiones de la §28. La spec 012 implementa esta versión; el aviso con la
  aplicación cerrada sigue para una spec posterior

Enmienda 4.10.0 → 4.10.1 (2026-10-12), decidida por el propietario del proyecto:
- Título y §1: el proyecto pasa de llamarse La Pecosa a llamarse LFClub; se cambian todas sus formas
  (LaPecosa → LFClub, la-pecosa y lapecosa → lfclub). No cambia ninguna regla de negocio
- §4 y estructura del repositorio: los proyectos del backend pasan de LaPecosa.* a LFClub.*
- Pendientes: renombrar la carpeta del proyecto y el repositorio, que se hace al final

Enmienda 4.10.1 → 4.11.0 (2026-10-12), decidida por el propietario del proyecto en la spec 013:
- §11.1: una actividad puede dirigirse, además de a categorías, a equipos de ellas, y puede sumar a
  jugadores concretos de otros equipos o de otras categorías sin cambiarles nada
- §11.3: el entrenador puede dirigir una actividad a cualquier equipo de sus categorías, aunque su
  asignación indique que dirige solo algunos
- §7.5: un jugador añadido a una actividad de otra categoría ve solo esa actividad
- §8 (Entrenador): al crear una convocatoria o dirigir un entrenamiento puede sumar jugadores de
  otras categorías; no es "gestionarlos". §8 (Jugador): ve los entrenamientos de su categoría que van
  a sus equipos o a todos, y los de otra categoría a los que fue añadido
- §14.4: los avisos de entrenamientos llegan a quien puede ver la actividad, también a los añadidos
- §17.2: sustituye la frase "visibles para todos los jugadores de las categorías a las que van
  dirigidos, sin excepción": con equipos, solo los ven los jugadores de esos equipos y los añadidos.
  Quién suma jugadores de otra categoría: el PRESIDENTE y quien entrena la actividad. Un entrenamiento
  de un equipo desactivado sigue visible para quienes eran de ese equipo hasta que se realiza
- §20: pruebas nuevas de visibilidad por equipo y por jugador añadido
- §28: sin cambios. Principios modificados: ninguno renombrado. Secciones añadidas y eliminadas:
  ninguna
- Pendientes: ninguna decisión nueva. La spec 013 implementa esta versión

Enmienda 4.11.0 → 4.12.0 (2026-10-13), decidida por el propietario del proyecto en la spec 014:
- §12.4: la eliminación de la cuenta incluye a todos sus jugadores en todos los clubes; lo
  publicado se conserva con "Persona eliminada"; el presidente único no puede eliminarse; cierra
  sesiones y libera correo y documentos; existe la página pública /eliminar-cuenta
- Motivo: Google Play exige eliminar la cuenta desde la aplicación y una dirección web pública
- §28: sin cambios. Principios renombrados, secciones añadidas y eliminadas: ninguno
- Pendientes: ninguna decisión nueva. La spec 014 implementa esta versión

-->

# Constitución del Proyecto — LFClub, plataforma multiclub de escuelas de fútbol

## 1. Propósito

Este proyecto se llama **LFClub**. Es una plataforma web y móvil para escuelas y clubes de
fútbol. Valfor F.C. no es el nombre del proyecto: es uno de sus clientes, el primero.

El nombre visible para las personas es "LFClub". En todo lugar donde un espacio o una
mayúscula pueda dar problemas se usa una de estas dos formas, y ninguna otra:

- `LFClub`: proyectos, espacios de nombres y clases del backend (por ejemplo `LFClub.Api`).
- `lfclub`: carpeta del proyecto, repositorio, paquetes del frontend, imágenes de Docker y
  direcciones web. Donde no se admita el guion (identificador de la aplicación Android, nombre de
  la base de datos) se usa `lfclub`.

Es multiclub: una
misma instalación atiende a varios clubes, cada uno con sus propios datos, su escudo y sus colores
(ver §7).

El primer club es Valfor F.C., escuela de fútbol infantil de Manizales (Caldas, Colombia) con sede
en la Cancha de Minitas. Atiende jugadores desde los 6 años; su categoría mayor es la 2012.

La plataforma tiene tres partes:

- Panel de administración de la plataforma, exclusivo del DESARROLLADOR, desde donde se crean los
  clubes, se configura su escudo y sus colores y se invita a su PRESIDENTE.
- Sitio público de cada club, sin inicio de sesión, para visitantes.
- Zona privada, con roles, para el presidente, directivos, entrenadores y jugadores. La cuenta del jugador es
  la que usa su familia.

La plataforma debe permitir, para cada club:

- Mostrar al público la información del club: historia, sede, categorías, cuerpo técnico y
  contacto.
- Publicar resultados, torneos en los que participa el club, tablas de posiciones y goleadores.
- Gestionar jugadores, entrenadores y categorías.
- Gestionar mensualidades: cargos, pagos, abonos, saldos y estado de cuenta de cada jugador.
- Saber quién debe, cuánto debe y desde cuándo.
- Recibir pagos en línea mediante una pasarela de pago y registrar pagos en efectivo o por
  transferencia.
- Programar entrenamientos, partidos y torneos.
- Registrar convocatorias, asistencia y resultados.
- Gestionar el arbitraje de cada partido y su cobro entre los convocados.
- Mantener la ficha de cada jugador con acceso restringido.
- Enviar comunicados y mensajes de convocatoria.
- Mantener históricos deportivos y financieros sin alterar información histórica.

## 2. Principios obligatorios

### 2.1. Código en español

Todo el código nuevo debe utilizar nombres en español.

Esto incluye:

- Clases.
- Interfaces.
- Métodos.
- Propiedades.
- Variables.
- Parámetros.
- DTOs.
- Enumeraciones.
- Servicios.
- Repositorios.
- Controladores.
- Validadores.
- Mappers.
- Utilidades.
- Excepciones propias.
- Comentarios.
- Documentación XML.
- Componentes, hooks y estados del frontend.

Ejemplo:

```text
Jugador
JugadorDto
CrearJugadorDto
ActualizarJugadorDto
IJugadorServicio
JugadorServicio
IJugadorRepositorio
JugadorRepositorio
JugadorMapper
JugadorValidador
```

No utilizar nombres en inglés cuando exista una denominación clara en español.

Los nombres propios de tecnologías externas pueden mantenerse según su nomenclatura oficial, por
ejemplo:

- Entity Framework Core
- PostgreSQL
- ASP.NET Core
- JWT
- Swagger
- Docker
- xUnit
- React
- PSE

### 2.2. Vocabulario del dominio

Se utilizará siempre el mismo término para cada concepto:

| Término | Significado |
| --- | --- |
| Jugador | niño o niña inscrito en el club; su cuenta la usa su padre, madre o responsable |
| Entrenador | profesor a cargo de una o varias categorías |
| Presidente | presidente y dueño del club |
| Club | escuela o equipo que usa la plataforma; todos sus datos le pertenecen solo a él |
| Desarrollador | dueño de la plataforma; crea los clubes e invita a su PRESIDENTE desde su panel |
| Directivo | miembro de la directiva del club |
| Categoria | grupo de jugadores definido por un único año de nacimiento |
| Equipo | división de una categoría, con nombre propio (A, B, Élite) |
| Mensualidad | cobro mensual por la formación de un jugador |
| Cargo | valor que un jugador debe (mensualidad, arbitraje, otro) |
| Pago | dinero recibido y aplicado a uno o varios cargos |
| Convocatoria | lista de jugadores citados a un partido |
| Arbitraje | costo del árbitro de un partido |

No se usarán sinónimos en el código (por ejemplo Alumno, Profesor, Cuota) para estos conceptos.

No existe una entidad ni un rol Acudiente: el padre, la madre o el responsable usa la cuenta del
jugador. Sus datos de contacto son campos del Jugador.

### 2.3. Tamaño máximo de las clases

Ninguna clase puede superar las 250 líneas de código.

El límite aplica a todo archivo de código escrito a mano, tanto en el backend como en el frontend:
clases, servicios, controladores, repositorios y componentes.

Cuando una clase se acerca al límite, se divide por responsabilidades. No se permite eludir el
límite comprimiendo líneas, quitando documentación ni usando clases parciales.

Quedan exentos únicamente los archivos generados automáticamente, como las migraciones de Entity
Framework Core.

## 3. Documentación obligatoria de clases

Toda clase, interfaz, enum o componente de dominio relevante debe tener documentación XML en
español.

La documentación debe explicar:

- Qué representa.
- Cuál es su responsabilidad.
- Qué responsabilidades NO debe asumir.

Ejemplo:

```csharp
/// <summary>
/// Representa el servicio encargado de gestionar las mensualidades.
/// Su responsabilidad es generar los cargos mensuales, aplicar pagos
/// y calcular el saldo de cada jugador según las reglas de negocio.
/// No debe comunicarse directamente con la pasarela de pago
/// ni acceder al contexto de Entity Framework.
/// </summary>
public class MensualidadServicio
{
}
```

La documentación no debe ser decorativa. Debe ayudar a comprender la arquitectura.

## 4. Arquitectura

La aplicación utilizará una arquitectura por capas con separación clara de responsabilidades.

El repositorio tiene dos carpetas principales y separadas: `backend/` para la API y `frontend/`
para la aplicación React. Ninguna contiene código de la otra.

Estructura principal:

```text
backend/
├── src/
│   ├── LFClub.Api/
│   │   └── Controladores/
│   │
│   ├── LFClub.Aplicacion/
│   │   ├── DTOs/
│   │   ├── Interfaces/
│   │   ├── Servicios/
│   │   ├── Implementaciones/
│   │   ├── Mappers/
│   │   ├── Validadores/
│   │   └── Utilidades/
│   │
│   ├── LFClub.Dominio/
│   │   ├── Entidades/
│   │   ├── Enumeraciones/
│   │   └── Reglas/
│   │
│   └── LFClub.Infraestructura/
│       ├── Datos/
│       ├── Repositorios/
│       ├── Pagos/
│       └── Correo/
│
└── pruebas/
    ├── Unitarias/
    └── Integracion/

frontend/
├── publico/
└── privado/
```

`LFClub.Infraestructura/Pagos` contiene la integración con la pasarela de pago y
`LFClub.Infraestructura/Correo` la integración con el servicio de correo. Ambas quedan detrás de
interfaces definidas en Aplicacion.

`frontend/` contiene la aplicación React: el sitio público y la zona privada.

El nombre definitivo de los proyectos podrá ajustarse durante el plan técnico, pero la separación
conceptual debe mantenerse.

## 5. Flujo de dependencias

El flujo normal de una operación será:

```text
Controlador
    ↓
IServicio
    ↓
Servicio
    ↓
IRepositorio
    ↓
Repositorio
    ↓
Entity Framework Core
    ↓
PostgreSQL
```

Para respuestas:

```text
Entidad
    ↓
Mapper
    ↓
DTO
    ↓
Controlador
    ↓
JSON
```

- Los controladores no deben contener reglas de negocio.
- Los repositorios no deben contener reglas de negocio propias de la aplicación.
- Los DTOs no deben utilizarse como entidades de persistencia.
- Las entidades de dominio no deben depender de ASP.NET Core.
- El frontend consume exclusivamente la API. No accede a la base de datos ni a la pasarela con
  credenciales privadas.

## 6. Tecnologías base

La primera implementación utilizará:

- C#.
- ASP.NET Core Web API.
- Entity Framework Core.
- PostgreSQL.
- JWT.
- Swagger/OpenAPI.
- Docker.
- xUnit.
- Git.
- GitHub.
- React para el frontend.
- Capacitor para empaquetar la aplicación Android.
- Wompi como pasarela de pago.
- Brevo para el envío de correos.

### 6.1. Una sola base de código para web, escritorio y móvil

El frontend es una única aplicación React adaptable.

Se distribuye de tres formas:

- Web: desde el navegador, en cualquier dispositivo.
- Escritorio: la misma web, instalable en el ordenador como aplicación (PWA).
- Android: la misma aplicación empaquetada con Capacitor y publicada en Google Play. Es una sola
  aplicación para todos los clubes; el escudo y los colores de cada club aparecen al iniciar
  sesión.

No se mantendrá una segunda base de código nativa para móvil.

iOS no forma parte de esta primera versión.

### 6.2. Pasarela de pago

Se utilizará Wompi, con sus medios de pago para Colombia (PSE, Nequi, tarjetas).

La integración queda detrás de una interfaz de Aplicacion (ver §4), de modo que cambiar de
pasarela no afecte las reglas de negocio.

Cada club recibe su dinero en su propia cuenta de la pasarela. Las llaves de la pasarela de cada
club son secretos de ese club: se guardan protegidas, nunca se devuelven por la API y nunca llegan
al frontend.

El pago de cada club por el uso de la plataforma se cobra con la misma pasarela (ver §7.4).

El desarrollo y las pruebas se hacen contra el entorno de pruebas de la pasarela. Las llaves de
producción solamente se configuran en el despliegue.

### 6.3. Correo

Se utilizará Brevo para enviar los correos de la plataforma: las invitaciones para registrarse en
un club (ver §12.1) y los de la cuenta, como el de recuperación de contraseña (ver §12.4).

La integración queda detrás de una interfaz de Aplicacion (ver §4). La llave de Brevo es un
secreto de configuración: no se incluye en el repositorio ni en el frontend.

No introducir nuevas tecnologías, frameworks o patrones arquitectónicos importantes sin justificar
su necesidad y documentar el cambio.

La simplicidad y mantenibilidad tienen prioridad sobre la cantidad de tecnologías utilizadas.

## 7. Varios clubes

La plataforma es multiclub: atiende a varios clubes desde una misma instalación. Valfor F.C. es el
primero.

### 7.1. Aislamiento entre clubes

Todo dato de un club (usuarios, jugadores, categorías, cargos, pagos, partidos, torneos,
comunicados, configuración) pertenece a exactamente un club.

Un usuario de un club nunca accede a datos de otro club: ni los ve, ni los cuenta, ni puede
consultarlos por su identificador.

Ninguna consulta ni operación puede ejecutarse sin estar limitada a un club. Las únicas
excepciones son:

- El panel de administración del DESARROLLADOR (ver §8).
- Las consultas de la propia cuenta, necesarias para iniciar sesión y elegir club (ver §7.3 y
  §12.4): encontrar la cuenta por su correo o su documento y listar los clubes a los que
  pertenece. Siempre quedan limitadas a esa cuenta.
- Los datos propios de §12.6: un PRESIDENTE, DIRECTIVO o ENTRENADOR consulta y cambia sus
  nombres, apellidos, fecha de nacimiento y celular, y el cambio se escribe en sus integrantes
  con esos roles de todos sus clubes. Queda limitado a la cuenta con sesión, no toca sus
  integrantes JUGADOR y no devuelve datos de ningún club.
- Abrir una invitación por su enlace (ver §12.1): devuelve solo esa invitación y su club.
- El documento de un jugador que pasa a otra cuenta (ver §10, §12.1.2 y §14.1). Al agregar un
  hermano, al aprobarlo y al reincorporar a un jugador retirado se consulta, por ese número de
  documento, si está en otros clubes, con qué rol y si está activo. Al aprobar al hermano, el
  jugador activo de otra cuenta con ese documento queda retirado en su club. Es la única escritura
  en un club desde otro club, y solo hace ese retiro.

Ninguna excepción entrega datos de un club a quien no pertenece a él. La cuarta solo revela que el
documento está activo en otro club: nunca el nombre de ese club ni ningún dato de la otra cuenta.

### 7.2. Identidad y configuración de cada club

Los datos propios de cada club (nombre, escudo, colores, sede, dirección, datos de contacto, valor
de la mensualidad) son configuración de ese club almacenada en base de datos. No deben quedar
escritos de forma fija en el código, y el código no debe asumir que el club es Valfor F.C.

El escudo y los colores de un club los pueden configurar tanto el DESARROLLADOR, desde su panel de
administración, como el PRESIDENTE de ese club, desde "Datos del club". La interfaz de cada club
se muestra con su propia identidad.

El resto de la configuración del club (nombre, sede, dirección, datos de contacto) la pueden
editar tanto el DESARROLLADOR como el PRESIDENTE de ese club.

Todo usuario ve en pantalla el nombre de su club.

### 7.3. Pertenencia a un club

Cada integrante guarda el identificador del club al que pertenece. Ese identificador se fija al
registrarse mediante la invitación (ver §12.1) y determina qué club ve.

Una misma persona puede ser integrante de varios clubes. Inicia sesión una sola vez y, dentro de
su panel, un desplegable le permite elegir de cuál club quiere ver los datos. En cada momento ve
únicamente los datos del club elegido, y solo puede elegir entre los clubes a los que pertenece.

### 7.4. Suspensión, baja, eliminación y pago por el uso de la plataforma

Cada club paga por el uso de la plataforma, mediante la misma pasarela de pago (ver §6.2).

**Suspendido (congelado).** Si un club no ha hecho ese pago, queda suspendido automáticamente
hasta ponerse al día. En cuanto el pago se confirma, el club vuelve a la normalidad de inmediato.
El DESARROLLADOR también puede, desde su panel, suspender un club manualmente, levantar una
suspensión y volver a aplicarla.

Mientras un club está suspendido:

- Solamente entra su PRESIDENTE.
- Cualquier otro integrante que intente entrar a ese club ve un aviso de incidencia temporal que
  le pide comunicarse con el presidente.
- No pasa nada más: todos los datos del club permanecen intactos.

**Dado de baja.** Lo aplica el DESARROLLADOR desde su panel. Nadie del club puede entrar, ni
siquiera su PRESIDENTE.

**Eliminado.** Eliminar un club borra todo rastro e información de ese club, sin posibilidad de
recuperarla. Es la única excepción a la conservación de históricos y a la inmutabilidad de los
pagos (ver §13, §14 y §16.3), solo puede hacerla el DESARROLLADOR y exige una confirmación
expresa.

### 7.5. Aislamiento dentro de un club

Dentro de un club, el aislamiento es entre personas y categorías:

- Un entrenador solamente accede a las categorías que tiene asignadas. Dentro de ellas ve a todos
  los jugadores, de cualquier equipo (ver §11.3).
- Una cuenta de jugador solamente accede a la información de su propio jugador. Cada familia ve
  únicamente lo de su hijo.
- Un jugador añadido a un entrenamiento o a un partido de otra categoría (ver §11.1) ve solo esa
  actividad, y no accede a nada más de esa categoría.

La aceptación del tratamiento de datos se da por hecha al inscribirse en el club. La plataforma no
gestiona autorizaciones ni consentimientos.

El backend debe aplicar este aislamiento.

No se puede confiar exclusivamente en restricciones del frontend.

El hecho de que un usuario conozca el identificador de otro jugador no le concede acceso.

## 8. Roles

Un visitante no es un rol: es cualquier persona sin sesión iniciada. Solamente accede al sitio
público (ver §9).

Existen cinco roles con cuenta:

```text
DESARROLLADOR
PRESIDENTE
DIRECTIVO
ENTRENADOR
JUGADOR
```

Toda cuenta entra al club con el rol que indica su invitación, sin sala de espera (ver §12.1 y
§12.5). La única aprobación que existe es la del hermano agregado desde la ficha (ver §12.1.2).

Dentro de un club, un integrante tiene un único rol. Una persona que pertenece a varios clubes
tiene un rol en cada uno, y pueden ser distintos. Si el PRESIDENTE le cambia el rol a alguien ya
registrado (ver §12.2), el rol nuevo reemplaza al anterior y pasa a ser su único rol. Un
entrenador que además tiene un hijo en el club usa dos cuentas:
la suya, con su documento, como ENTRENADOR, y la del niño, con el documento del niño, como JUGADOR.

Un PRESIDENTE o un DIRECTIVO que además entrena no cambia de rol ni recibe un segundo rol: queda
asignado a la categoría, conserva su único rol y en esa categoría actúa como entrenador en lo
deportivo (ver "Entrenador" y §12.3).

### Desarrollador

Es el dueño de la plataforma, que la ofrece a varios clubes. Es un rol único: existe una sola
cuenta DESARROLLADOR, y es la única que no pertenece a ningún club.

Tiene su propio panel de administración de la plataforma. Desde allí puede:

- Crear clubes. Al crear un club debe asignarle obligatoriamente un PRESIDENTE, al que se le envía
  una invitación por correo (ver §12.5).
- Modificar el escudo y los colores de cada club.
- Suspender un club, levantar su suspensión, darlo de baja, revertir la baja y eliminarlo (ver
  §7.4). Solo se puede eliminar un club que ya está dado de baja.
- Reenviar la invitación del PRESIDENTE o corregir su correo, mientras no se haya usado. No invita
  a nadie más: ni presidentes adicionales ni ningún otro rol.
- Quitarle el rol a un PRESIDENTE, siempre que el club conserve al menos otro ya registrado. Al
  hacerlo elige entre asignarle otro rol en ese club o eliminarlo del club por completo.

No puede nada más: no consulta ni gestiona fichas de jugadores, datos médicos, finanzas, pagos ni
información deportiva de ningún club.

El rol DESARROLLADOR no se asigna ni se retira desde la aplicación, y ningún otro rol accede al
panel de administración ni puede crear clubes. El DESARROLLADOR no crea usuarios ni asigna
contraseñas: solo envía invitaciones.

Los cuatro roles restantes pertenecen siempre a un club y su alcance nunca sale de él.

### Presidente

Es el presidente del club, que a su vez es su dueño. Administra el club completo.

Puede:

- Gestionar la configuración del club y el contenido del sitio público.
- Configurar el escudo y los colores de su club desde "Datos del club" (ver §7.2).
- Gestionar categorías, entrenadores y jugadores.
- Asignar entrenadores a categorías.
- Crear equipos dentro de una categoría y decidir en qué equipos juega cada jugador (ver §11.3).
- Retirar del club a un jugador que se fue y reincorporarlo (ver §14.1).
- Cargar, cambiar y quitar la foto de cualquier jugador de su club (ver §14.2).
- Poner las estadísticas, el dorsal y la posición de la carta de cualquier jugador de su club y
  exportar cualquiera de esas cartas como imagen (ver §14.3).
- Corregir el resultado de cualquier partido de su club, también el publicado por una familia (ver
  §17.3).
- Definir el valor de la mensualidad del club y modificar el de jugadores concretos (ver §16.7).
- Consultar las finanzas completas del club.
- Registrar, anular y corregir pagos.
- Gestionar torneos, partidos y arbitrajes.
- Publicar comunicados dirigidos a cualquier audiencia del club y retirar cualquiera (ver §14.4).
- Enviar invitaciones de registro a su club con el rol JUGADOR, ENTRENADOR o DIRECTIVO (ver
  §12.1).
- Aprobar o rechazar a los jugadores agregados desde la ficha de un hermano (ver §12.1.2).
- Buscar usuarios registrados por su documento de identidad y asignarles o retirarles los roles
  ENTRENADOR o DIRECTIVO.

Invitar al club, aprobar o rechazar un ingreso y cambiar el rol de alguien ya registrado son
exclusivos del PRESIDENTE (ver §12.1, §12.1.1 y §12.2). Ningún otro rol puede invitar, aprobar,
rechazar, ni asignar, cambiar o retirar roles, ni siquiera el suyo propio.

El rol PRESIDENTE solamente lo otorgan el DESARROLLADOR, al crear el club, u otro PRESIDENTE del
mismo club (ver §12.5). Un club puede tener varios presidentes.

Cada club debe conservar siempre al menos un PRESIDENTE activo; no se permite revocar el último.

Un PRESIDENTE no puede quitarse su propio rol ni eliminar su propia cuenta mientras no exista otro
PRESIDENTE en ese club.

### Directivo

Es un miembro de la directiva del club. El rol lo da el PRESIDENTE, al invitarlo o al asignárselo
después.

Puede:

- Consultar las finanzas del club: recaudo, quién debe y quién está al día.
- Consultar la documentación de cada jugador.
- Consultar la seguridad social de cada jugador: su entidad de salud y dónde lo atienden.
- Consultar las categorías del club, con sus equipos, sus entrenadores y sus jugadores, y la lista
  de jugadores retirados.
- Publicar comunicados dirigidos a cualquier audiencia del club, y retirar los suyos (ver §14.4).

Si además entrena, en las categorías que tiene asignadas actúa como entrenador en lo deportivo
(ver "Entrenador").

Su acceso a finanzas y fichas es de consulta. No puede registrar ni anular pagos, modificar el
valor de la mensualidad del club ni la de un jugador, enviar invitaciones, aprobar ni rechazar
ingresos, asignar ni retirar roles, ni buscar usuarios por documento.
Tampoco puede gestionar categorías ni equipos, asignar entrenadores, cambiar a un jugador de
categoría o de equipo, ni retirar o reincorporar jugadores.

### Entrenador

El rol llega con la invitación del PRESIDENTE, o lo asigna él después.

Está asignado a una o varias categorías mediante:

```text
AsignacionEntrenadorCategoria
```

También puede quedar asignado a una categoría un PRESIDENTE o un DIRECTIVO que además entrena.
Conserva su rol como único rol en el club. En las categorías que tiene asignadas actúa como
entrenador en lo deportivo: programa entrenamientos, registra asistencia, crea convocatorias, ve
los partidos próximos de esas categorías, registra y corrige resultados y goles y pone las
estadísticas de la carta, igual que un ENTRENADOR en las suyas (ver §14.3, §17.2 y §17.3). Para un PRESIDENTE esto no añade nada, porque su rol ya lo cubre en todo el
club. Un DIRECTIVO, fuera de sus categorías asignadas, sigue con su acceso de consulta. Los
comunicados los publica con el alcance de su rol, no con el de entrenador (ver §14.4). Lo demás
que hace un ENTRENADOR (evaluaciones, pagos en efectivo) se decidirá para el DIRECTIVO asignado
con esas funcionalidades.

Cuando la categoría tiene equipos, la asignación indica cuáles dirige (ver §11.3).

Dentro de sus categorías puede:

- Consultar la ficha de sus jugadores, incluidos los datos médicos y de contacto de emergencia.
- Programar entrenamientos.
- Registrar asistencia.
- Crear convocatorias, seleccionando de la lista los jugadores a los que se envían, y dirigir
  entrenamientos a equipos de sus categorías. Puede sumar a jugadores de otros equipos o de otras
  categorías a esa actividad (ver §11.1 y §17.2).
- Registrar resultados, goles y demás eventos del partido, y corregir el resultado publicado por
  una familia (ver §17.3).
- Poner las estadísticas, el dorsal y la posición de la carta de sus jugadores (ver §14.3).
- Registrar evaluaciones del jugador.
- Publicar comunicados dirigidos a sus categorías o a equipos de ellas, y retirar los suyos (ver
  §14.4).
- Consultar el estado de pago de sus jugadores.
- Registrar pagos en efectivo recibidos de sus jugadores.

Además dispone de un apartado de finanzas generales del club, de solo lectura: recaudo del mes,
cartera pendiente y totales por categoría.

No puede modificar el valor de la mensualidad del club ni la de un jugador, anular o corregir
pagos, ni gestionar jugadores de categorías que no tenga asignadas. Sumar a un jugador de otra
categoría a una actividad no es gestionarlo: no ve su ficha ni cambia nada de él. Tampoco decide en
qué equipo juega cada jugador: eso es del PRESIDENTE (ver §11.3).

### Jugador

Es la cuenta de un jugador inscrito. El jugador es menor de edad, por lo que la cuenta la usa su
padre, madre o responsable. No existe un rol separado para la familia.

Cada jugador tiene su propio registro de integrante, con su propio documento. Los hermanos no se
mezclan: cada uno tiene el suyo.

Una misma cuenta puede tener varios jugadores: los padres agregan a un hermano desde la ficha del
jugador ya registrado (ver §12.1.2). Quien entra con el correo elige con cuál de sus jugadores
continuar; quien entra con el documento de un jugador ve solo a ese jugador (ver §12.4). En cada
momento se ve la información de un único jugador.

Puede, únicamente sobre su propio jugador:

- Consultar la ficha, el calendario, las convocatorias, las estadísticas y las evaluaciones.
- Consultar su categoría, los equipos en los que está y quiénes son los entrenadores de su
  categoría.
- Abrir la ficha básica de cualquier PRESIDENTE, DIRECTIVO o ENTRENADOR de su club (ver §12.6).
  De ellos no ve nada más: ni documento ni datos de contacto.
- Consultar el estado de cuenta y los recibos.
- Pagar mensualidades y arbitrajes.
- Confirmar asistencia a entrenamientos y partidos.
- Cambiar cualquier dato de la ficha de su jugador, también sus nombres, sus apellidos y su fecha
  de nacimiento, además de los datos de contacto, médicos y documentos. El número y el tipo de
  documento siguen las reglas de §10. Cambiar la fecha de nacimiento no cambia la categoría: eso
  sigue siendo del PRESIDENTE (ver §11.2).
- Cargar, cambiar y quitar la foto de su jugador (ver §14.2).
- Consultar la carta de su jugador, poner su dorsal y su posición y exportarla como imagen (ver
  §14.3).
- Publicar el resultado de un partido ya empezado al que su jugador fue convocado (ver §17.3).
- Consultar los comunicados y los avisos de actividades que le llegan, marcarlos como leídos y ver
  en la campana cuántos le faltan por leer (ver §14.4).
- Retirar del club a su propio jugador (ver §14.1). No puede reincorporarlo.

No accede a la ficha, la carta, los pagos ni los datos de contacto de otros jugadores.

Ve los entrenamientos de su categoría dirigidos a toda ella o a alguno de sus equipos, y los
entrenamientos y partidos de otras categorías a los que fue añadido, pero solamente los partidos a
los que fue convocado (ver §17.2).

## 9. Sitio público

Cada club tiene su propio sitio público, con su escudo y sus colores. No requiere inicio de sesión
y solamente muestra información de ese club.

Solamente expone información que el club ha decidido publicar:

- Historia, sede, categorías, horarios y cuerpo técnico.
- Resultados de partidos ya jugados.
- Torneos en los que participa el club y tablas de posiciones.
- Goleadores del club.
- Formulario de contacto o de inscripción.

Los endpoints públicos son un conjunto separado y explícito de la API. Ningún endpoint privado
debe quedar accesible sin autenticación por omisión.

El sitio público no muestra los próximos partidos ni sus convocatorias (ver §17.2).

De un jugador, el sitio público solamente muestra su nombre, su categoría y sus estadísticas
deportivas.

Los endpoints públicos nunca devuelven documento, datos de contacto, datos médicos ni información
de pagos.

## 10. Identidad mediante documento

El número de documento de identidad identifica de forma única a una persona dentro de un club.

Se almacena junto con su tipo:

```text
REGISTRO_CIVIL
TARJETA_IDENTIDAD
CEDULA_CIUDADANIA
CEDULA_EXTRANJERIA
```

El documento:

- Es único por pareja "documento + club": no puede repetirse dentro de un mismo club.
- Puede existir en dos clubes distintos: es la misma persona, integrante de ambos, con un único
  inicio de sesión (ver §7.3 y §12.4).
- No debe utilizarse como sustituto de las claves primarias internas.

Normalmente un documento pertenece a una sola cuenta. La única forma de que esté en dos es agregar
como hermano a un jugador que ya está en otra cuenta en otro club (ver §12.1.2). Al aprobarse, el
jugador queda retirado del otro club (ver §14.1) y el documento queda en las dos cuentas. Nadie
puede registrarse con un documento que ya tiene otra cuenta, ni cambiar su documento a uno así.

Cuando un documento está en más de una cuenta, el inicio de sesión con ese documento abre una sola:
la cuenta donde el jugador está activo; si no está activo en ninguna, aquella donde está en
espera. Con la contraseña de cualquier otra cuenta, el ingreso se niega como cualquier ingreso con
datos incorrectos (ver §12.4).

Un jugador puede pasar de registro civil a tarjeta de identidad. Ese cambio actualiza el tipo y el
número sobre el mismo registro; no crea otra persona.

Las relaciones entre entidades utilizarán IDs internos.

## 11. Categorías y cambio de categoría

Las categorías se definen por año de nacimiento: cada año es una categoría (2012, 2013, 2014,
2015, etc.). Actualmente el club cubre de la 2012 a la 2020.

Una categoría corresponde a un único año de nacimiento; no cubre varios. Cuando dos años trabajan
juntos son dos categorías, que pueden tener los mismos entrenadores (ver §11.1).

Las categorías las crea el PRESIDENTE de cada club. En un club no hay dos categorías del mismo
año.

### 11.1. Actividades conjuntas

En ocasiones varias categorías se juntan para un entrenamiento, un partido o una convocatoria.

Por eso un entrenamiento, un partido, una convocatoria o un mensaje de convocatoria puede
dirigirse a una o a varias categorías.

Juntar categorías para una actividad no cambia la categoría de ningún jugador ni crea una
categoría nueva.

Cuando una categoría tiene equipos (ver §11.3), un entrenamiento también puede dirigirse a uno o
varios de sus equipos. Sin equipos elegidos, la actividad es de toda la categoría.

Si los jugadores de la audiencia no alcanzan, quien dirige la actividad (ver §17.2) puede sumar a
ella a jugadores concretos de otros equipos o de otras categorías del club. Sumar a un jugador no
cambia su categoría ni sus equipos, ni crea una categoría nueva.

### 11.2. Cambio de categoría

Debe existir un único registro de Jugador por persona.

Un jugador pertenece a una única categoría actual:

```text
Jugador.CategoriaId
```

Cambiar la categoría de un jugador es una operación del PRESIDENTE.

Al cambiar de categoría:

- No se crea otro jugador.
- No se modifica información histórica.
- Los partidos, convocatorias, asistencias, goles y cargos anteriores conservan la categoría en la
  que ocurrieron.

No se implementará inicialmente una entidad independiente de historial de categorías del jugador.

El PRESIDENTE puede ubicar a un jugador en una categoría que no es la de su año de nacimiento.

### 11.3. Equipos de una categoría

Una categoría puede dividirse en equipos (A y B, élite y B). Es opcional: una categoría sin
equipos funciona igual.

- Cada equipo tiene un nombre, único dentro de su categoría, y pertenece a una sola categoría.
- Los equipos los crea, renombra y desactiva el PRESIDENTE.
- Un jugador puede estar en varios equipos de su categoría a la vez, o en ninguno. Nunca está en
  un equipo de otra categoría, y al cambiar de categoría sale de los equipos de la anterior.
- Solo el PRESIDENTE decide en qué equipos juega cada jugador.
- El entrenador se asigna a la categoría, no al equipo. La asignación indica qué equipos dirige,
  pero ve a todos los jugadores de la categoría: el aislamiento sigue siendo por categoría (ver
  §7.5). Puede dirigir una actividad a cualquier equipo de sus categorías, no solo a los que dirige.

No se guarda historial de los equipos por los que pasó un jugador.

## 12. Usuario, registro y roles

La entidad Usuario representa la cuenta de acceso.

### 12.1. Registro

El registro es por invitación. La persona recibe por correo (Brevo) un enlace de invitación de un
club; ese enlace la lleva a registrarse en ese club y la cuenta queda ligada a él (ver §7.3).

No existe un registro abierto sin invitación, y nadie elige su club al registrarse: lo determina
el enlace.

Dentro de un club, las invitaciones de registro las envía el PRESIDENTE de ese club, y nadie
más. La invitación de un PRESIDENTE la envía el DESARROLLADOR (ver §12.5).

Toda invitación sirve una sola vez, caduca y queda ligada al club y al correo al que se envió: la
persona no puede registrarse con otro correo.

Toda invitación del club indica el rol con el que entra la persona: JUGADOR, ENTRENADOR o
DIRECTIVO. Lo elige el PRESIDENTE al invitar.

Quien se registra con una invitación entra directamente al club con ese rol. No existe sala de
espera para quien fue invitado: si está dentro de la aplicación es porque alguien del club lo
invitó.

Quien entra como JUGADOR recibe al registrarse la categoría que corresponde a su año de nacimiento
y empieza a generarse su mensualidad. Si el club todavía no tiene activa la categoría de ese año,
el jugador queda sin categoría. Cuando el PRESIDENTE crea o reactiva la categoría de ese año, los
jugadores sin categoría nacidos ese año entran en ella automáticamente. Esta ubicación automática
nunca mueve a un jugador que ya tiene categoría. No se asigna ningún equipo automáticamente.

Quien entra como ENTRENADOR o DIRECTIVO no recibe ficha de Jugador, categoría ni mensualidad.

- La cuenta de un jugador se registra con el documento del jugador (el niño), no con el de su
  familia.
- Un entrenador o un directivo se registra con su propio documento.

Los datos para registrarse son:

- Nombre completo del integrante del club.
- Apellidos del integrante del club.
- Correo electrónico. Para un jugador es el de su acudiente (padre, madre o responsable).
- Tipo y número de documento del integrante del club (ver §10).
- Fecha de nacimiento del integrante del club.
- Celular de contacto.
- Nombre del padre, madre o responsable, cuando el integrante es un jugador.
- Contraseña.

El correo es necesario para recibir la invitación y para recuperar la contraseña (ver §12.4).

El correo es único: no se puede registrar una cuenta nueva con un correo que ya está registrado.
Quien lo intenta recibe un mensaje que le explica que ya hay alguien registrado con ese correo.

#### 12.1.2. Hermanos: agregar un jugador desde la ficha

Un segundo jugador de la misma familia no se registra con una cuenta nueva. Sus padres entran con
la cuenta que ya tienen y, desde la ficha del hermano, agregan un nuevo jugador.

El jugador agregado:

- Comparte todos los datos de contacto de la cuenta: correo, celular y responsable.
- Tiene sus propios datos de jugador, que los padres completan: nombre, apellidos, tipo y número
  de documento, fecha de nacimiento y el resto de su ficha.
- Es un jugador independiente, con su propia ficha, categoría, cargos y pagos.
- Pasa por la sala de espera, porque nadie del club lo invitó: no tiene categoría ni genera
  mensualidad hasta que el club lo aprueba (ver §12.1.1). Es el único ingreso que necesita
  aprobación.

Si el documento del hermano ya lo tiene un JUGADOR de otra cuenta en otro club, se admite. La
familia recibe el aviso de que, al aprobarse, dejará de estar en el otro club, y la sala de espera
se lo advierte al PRESIDENTE antes de decidir, sin nombrar ese club ni dar datos de esa cuenta.
Mientras espera, o si se le rechaza, nada cambia en el otro club; al aprobarse queda retirado de
allí (ver §14.1). Si con otra cuenta ese documento es de un PRESIDENTE, un DIRECTIVO o un
ENTRENADOR en cualquier club, no se admite y se explica el motivo. Cuando es de la misma cuenta en
otro club no hay retiro: la familia lo tiene inscrito en los dos clubes.

#### 12.1.1. Sala de espera

La sala de espera existe solamente para el jugador agregado desde la ficha de un hermano (ver
§12.1.2). Quien se registra con una invitación no pasa por ella (ver §12.1).

Mientras ese jugador está en espera, quien lo elige solamente ve una pantalla que le indica que su
ingreso está pendiente de aprobación. Los demás jugadores de la cuenta siguen con normalidad.

Mientras está en espera:

- No accede a ninguna información del club: ni entrenamientos, ni horarios, ni jugadores, ni
  pagos.
- No se le asigna categoría.
- No se le genera mensualidad.

El ingreso lo aprueba el PRESIDENTE, y nadie más.

Al aprobarse su ingreso, el jugador se ubica igual que quien entra con una invitación de JUGADOR:
recibe la categoría de su año de nacimiento, o queda sin categoría si no existe, y empieza a
generarse su mensualidad (ver §12.1).

El PRESIDENTE también puede rechazarlo. Rechazar borra a ese jugador del club, sin
dejar datos suyos en él. La cuenta y sus demás jugadores quedan intactos.

Solo se rechaza a quien está en espera; a un jugador ya aprobado no se le rechaza: se le retira
(ver §14.1).

Quien entra con una invitación queda APROBADO desde su registro.

El estado de ingreso de un integrante es:

```text
EN_ESPERA
APROBADO
```

El rechazo no es un estado: no queda registro del ingreso rechazado.

### 12.2. Asignación de roles

Cada persona entra al club con el rol de su invitación (ver §12.1). Después, el PRESIDENTE puede
cambiarlo: busca a un usuario ya registrado por su documento de identidad y decide:

- Asignarle el rol ENTRENADOR.
- Asignarle el rol DIRECTIVO.
- Dejarlo como está.

El rol asignado reemplaza al que tenía y pasa a ser su único rol.

Cuando una cuenta nueva pasa de JUGADOR a ENTRENADOR o DIRECTIVO, la ficha de Jugador que se creó
con su registro desaparece: se elimina. Es una eliminación física justificada (ver §14), porque
esa ficha nunca correspondió a un jugador real. Con ella se elimina su foto de jugador (ver
§14.2).

Un DIRECTIVO no invita ni asigna, cambia o retira roles.

Un rol solamente puede asignarse a un usuario que ya se registró. No se asignan roles a documentos
que aún no tienen cuenta.

La búsqueda de usuarios por documento es exclusiva del PRESIDENTE.

### 12.3. Alcance de cada rol

Los roles se resuelven mediante:

```text
Usuario
   ↓
UsuarioRol
```

Usuario no contiene CategoriaId.

El alcance de cada rol se resuelve exclusivamente así:

- DESARROLLADOR: panel de administración de la plataforma; crea clubes e invita a su PRESIDENTE
  (ver §12.5).
- PRESIDENTE: todo su club.
- DIRECTIVO: consulta de todo su club, con los límites definidos en §8.
- ENTRENADOR: mediante AsignacionEntrenadorCategoria. No se crea un UsuarioRol ENTRENADOR por cada
  categoría. Un PRESIDENTE o un DIRECTIVO también puede tener una AsignacionEntrenadorCategoria;
  su alcance sigue siendo el de su rol.
- JUGADOR: mediante Jugador.UsuarioId. Una cuenta puede tener varios jugadores (ver §12.1.2 y
  §12.4). Cada jugador pertenece a una única cuenta y tiene su propio registro, y la cuenta
  solamente accede a los suyos.

La ficha de Jugador se crea al registrarse con una invitación de JUGADOR o al agregar un hermano
(ver §12.1.2). Quien entra como ENTRENADOR o DIRECTIVO no tiene ficha de Jugador.

### 12.4. Inicio de sesión y contraseñas

Todos los usuarios, sin importar su rol, tienen un inicio de sesión completo:

- Registro.
- Inicio de sesión.
- Olvidé mi contraseña, con recuperación por correo.
- Cambio de contraseña.

El inicio de sesión se hace con el correo o con el documento del integrante, más su contraseña.

Una persona tiene un único inicio de sesión aunque pertenezca a varios clubes; el club que ve lo
elige después, en el desplegable de su panel (ver §7.3).

Una cuenta tiene un único correo y una única contraseña, y puede tener varios jugadores (ver
§12.1.2):

- Quien entra con el correo elige, después de entrar, con cuál de sus jugadores continuar.
- Quien entra con el documento de un jugador ve solamente a ese jugador. La contraseña es la de la
  cuenta.

Una persona puede solicitar desde la aplicación la eliminación de su cuenta. La solicitud se cumple
sola, sin aprobación de nadie. Al eliminarse la cuenta:

- Sus pagos se conservan, para que el club mantenga el control de lo recaudado (ver §16.3). Junto
  a ellos solo se guardan el nombre completo y el documento.
- Se borran todos los demás datos personales.
- Se eliminan todas sus estadísticas deportivas.
- Se eliminan también todos los jugadores de la cuenta, en todos los clubes, con su ficha, sus
  documentos, su foto y su carta. Quien solo quiere salir de un club usa "Retirar del club" (ver
  §14.1).
- Lo que la persona publicó (comunicados y resultados) se conserva con "Persona eliminada" en lugar
  de su nombre; el marcador de un partido es del equipo y no cambia.
- Un PRESIDENTE que es el único de algún club no puede eliminar su cuenta (ver §8).
- Cierran todas sus sesiones y quedan libres su correo y el documento de cada uno de sus jugadores.

La solicitud se hace con la contraseña de la cuenta. Existe además una página pública, sin sesión,
`/eliminar-cuenta`, que explica cómo pedirla y cómo hacerlo por correo quien no puede entrar.

Además, si se elimina el único club al que pertenece una persona, su cuenta se elimina con él (ver
§7.4).

Los correos de la cuenta se envían mediante Brevo (ver §6.3).

Tras 5 intentos fallidos seguidos de inicio de sesión, la cuenta queda bloqueada hasta que la
persona recupere su contraseña por correo.

Las contraseñas nunca se almacenan ni se envían en texto plano. Nadie asigna la contraseña de otra
persona: cada quien crea la suya al registrarse.

### 12.5. Invitación del PRESIDENTE por el DESARROLLADOR

Al crear un club, el DESARROLLADOR debe asignarle obligatoriamente un PRESIDENTE. No se puede
crear un club sin él.

Para ello indica el correo de esa persona y la plataforma le envía una invitación para registrarse
como PRESIDENTE del club creado.

Quien se registra con esa invitación:

- Completa el registro con sus propios datos y crea su propia contraseña (ver §12.1).
- Queda como PRESIDENTE de ese club, sin pasar por la sala de espera.

El DESARROLLADOR solo invita a ese primer PRESIDENTE. Mientras la invitación no se haya usado
puede reenviarla o corregir su correo; no invita presidentes adicionales.

Un club puede tener más de un PRESIDENTE. A un presidente adicional lo elige otro PRESIDENTE de
ese mismo club, dándole el rol a alguien que ya está registrado en el club.

### 12.6. Ficha básica y datos propios de presidentes, directivos y entrenadores

Cada integrante con rol PRESIDENTE, DIRECTIVO o ENTRENADOR tiene una ficha básica en su club. Muestra
únicamente:

- Su nombre completo.
- Su rol en ese club.
- Su foto de perfil, o sus iniciales si no tiene (ver §14.2).
- Las categorías activas que entrena en ese club y los equipos activos que dirige en cada una.

No muestra ni entrega el documento, la fecha de nacimiento, el correo, el celular ni ningún otro
dato de la cuenta.

La abre cualquier integrante del mismo club, con cualquier rol, también la cuenta de un jugador.
Nadie de otro club la abre, tampoco el DESARROLLADOR. Desde cada club se ven solo el rol, las
categorías y los equipos de ese club.

Estas personas cambian por sí mismas, desde su perfil, su celular, sus nombres, sus apellidos y su
fecha de nacimiento, con las reglas del registro (ver §12.1). El cambio vale en todos sus clubes.
El tipo y el número de documento no los cambian, porque con ellos se inicia sesión (ver §10 y
§12.4). Los datos de una cuenta de jugador se siguen cambiando desde la ficha de su jugador.

## 13. Histórico operativo y financiero

La información histórica no debe depender exclusivamente del estado actual de una entidad.

Los datos que puedan cambiar con el tiempo y sean relevantes para un registro histórico deben
quedar almacenados en ese registro.

Por ejemplo:

```text
ConfiguracionClub.ValorMensualidad
```

representa el valor vigente.

Mientras que:

```text
Cargo.Valor
```

representa el valor cobrado específicamente en ese mes a ese jugador.

Modificar el valor de la mensualidad no debe modificar cargos ya generados.

Del mismo modo:

```text
PartidoJugador.CategoriaId
```

conserva la categoría con la que el jugador disputó ese partido, aunque después cambie de
categoría.

## 14. Entidades desactivables

Las entidades con información histórica no deben eliminarse físicamente cuando dejan de estar
disponibles.

Se utilizará el concepto de:

```text
Activo
Activa
```

según corresponda.

Esto aplica, entre otras, a:

- Jugadores.
- Entrenadores.
- Categorías.
- Torneos.
- Sedes o canchas.
- Asignaciones entre entrenador y categoría.
- Equipos de una categoría.

Un jugador retirado se desactiva; su historial deportivo y financiero se conserva (ver §14.1).

Una categoría no se desactiva mientras tenga jugadores: antes hay que pasarlos a otra o retirarlos.
Al desactivarla, sus entrenadores dejan de estar asignados a ella.

La eliminación física deberá justificarse explícitamente.

Un comunicado o un aviso de actividad no se elimina físicamente por una acción de usuario (ver
§14.4).

El rechazo de un jugador en espera (ver §12.1.1) es una eliminación física justificada: ese
jugador nunca llegó a entrar al club y no tiene historial en él.

Borrar una categoría o un equipo que nunca ha tenido jugadores ni entrenadores también es una
eliminación física justificada: se creó por error y no tiene historial. Si alguna vez los tuvo,
solo se desactiva.

### 14.1. Retiro de un jugador

Solo se retira a un jugador aprobado y activo. Un jugador queda retirado únicamente de estas tres
formas:

- Lo retira el PRESIDENTE de su club.
- Lo retira su propia cuenta, con la opción "Retirar del club" de su ficha y después de
  confirmarlo. La cuenta y sus demás jugadores no cambian.
- Queda retirado al aprobarse, en otro club, un hermano de otra cuenta con su mismo documento (ver
  §12.1.2). Ocurre en la misma operación que la aprobación: si el retiro no puede hacerse, la
  aprobación tampoco. Nadie de su club ni de su familia lo confirma ni recibe aviso, y no queda
  registrado quién lo retiró, porque es de otro club.

Nadie más puede retirarlo.

El jugador retirado:

- Sale de su categoría y de sus equipos y deja de aparecer en las listas de jugadores.
- No accede a ninguna información de ese club: al entrar solo ve un aviso de que ya no está en él.
  En sus otros clubes sigue con normalidad.
- Conserva todos sus datos y su historial, salvo su foto, que se elimina (ver §14.2). Su correo y
  su documento siguen ocupados en ese club.
- No genera mensualidad (ver §16.7).

El PRESIDENTE puede reincorporarlo: vuelve a entrar con su misma cuenta y se le ubica como a un
jugador que acaba de entrar (ver §12.1). No recupera los equipos que tenía. Solo el PRESIDENTE
reincorpora; la familia no.

La reincorporación se niega mientras el documento del jugador esté activo con otra cuenta en otro
club, con un motivo que no nombra ese club ni da datos de esa cuenta. Para recuperarlo, primero
debe retirarlo el club donde está activo, por su PRESIDENTE o por su familia. Si el documento solo
está activo con la misma cuenta, la reincorporación sigue igual.

El PRESIDENTE y los DIRECTIVOS ven la lista de jugadores retirados de su club.

El retiro no es un estado de ingreso (ver §12.1.1): se expresa con Activo.

### 14.2. Foto del jugador

Cada jugador tiene una foto propia, guardada en su ficha. Es distinta de la foto de perfil de la
cuenta: los hermanos de una misma cuenta tienen cada uno la suya.

La foto de perfil de la cuenta solo la carga, cambia y quita su dueño. La de una cuenta con rol
PRESIDENTE, DIRECTIVO o ENTRENADOR en un club la ven los integrantes de ese club, únicamente en su
ficha básica y junto a su nombre (ver §12.6). La de una cuenta de jugador es privada y solo la ve
su dueño. Nadie de otro club ni el DESARROLLADOR obtiene la foto de perfil de otra cuenta.

- La cargan, cambian y quitan la propia cuenta, sobre su jugador, y el PRESIDENTE, sobre cualquier
  jugador de su club. Nadie más.
- Debe ser una imagen PNG, JPEG o WebP de hasta 1 MB.
- La ven únicamente quienes pueden ver a ese jugador: su propia cuenta, el PRESIDENTE y los
  DIRECTIVOS de su club y los entrenadores de su categoría (ver §7.5). El DESARROLLADOR no la ve.
  Nunca aparece en el sitio público.
- No sale de la plataforma, con una única excepción expresa: la carta del jugador, que la lleva,
  puede exportarse como imagen, solo por su propia cuenta y por el PRESIDENTE de su club (ver
  §14.3). Nadie más exporta ni descarga la foto ni la carta.

Cuando el jugador deja de pertenecer al club, su foto se elimina definitivamente: al retirarlo,
de cualquiera de las tres formas de §14.1, y al eliminarse su ficha por el cambio de rol de la
cuenta (ver §12.2). Es una eliminación física justificada y una excepción expresa a la
conservación de los datos del jugador retirado: la foto de un menor no se guarda cuando ya no está
en el club. Si se le reincorpora, vuelve sin foto.

### 14.3. Carta del jugador

Cada jugador tiene una carta, al estilo de las cartas de los videojuegos de fútbol, armada con los
datos de su ficha, su foto (ver §14.2), el escudo y los colores de su club y unas estadísticas.

- La ven exactamente los mismos que ven su ficha (ver §7.5 y §14.2), y nadie más. Nunca aparece
  en el sitio público ni la ve el DESARROLLADOR.
- La exportan como imagen únicamente su propia cuenta, la de su jugador, y el PRESIDENTE,
  cualquiera de su club. Es la excepción expresa de §14.2.
- Sus estadísticas las ponen únicamente quien entrena la categoría del jugador (ver §8,
  "Entrenador") y el PRESIDENTE. La familia no las cambia.
- El dorsal y la posición los ponen la propia cuenta, sobre su jugador, quien entrena su
  categoría y el PRESIDENTE. Vale el último cambio.
- Esas estadísticas son una valoración de quien entrena, no datos del partido: no se calculan a
  partir de lo registrado ni lo alteran (ver §17).
- Nunca se muestran como clasificación: ni tablas, ni listas ordenadas por ellas, ni
  comparaciones entre jugadores, ni posiciones.

### 14.4. Comunicados y campana

Un comunicado es un mensaje escrito, con título y texto plano, que el equipo del club dirige a las
familias.

- Lo publican el PRESIDENTE, los DIRECTIVOS y los ENTRENADORES, y nadie más.
- Se dirige a todo el club, a una o varias categorías, o a uno o varios equipos de una categoría.
  El PRESIDENTE y los DIRECTIVOS pueden dirigirlo a cualquier audiencia; un ENTRENADOR, solo a sus
  categorías asignadas o a equipos de ellas (ver §7.5).
- Llega únicamente a las cuentas con al menos un jugador activo en la audiencia. Quien entra a la
  audiencia después no recibe lo publicado antes.
- No se edita. Quien lo publicó, o el PRESIDENTE, lo retira; el DIRECTIVO retira solo los suyos. No
  se elimina físicamente: queda marcado como retirado, con quién y cuándo (ver §14).
- Cada cuenta de jugador tiene una campana con un solo contador de lo que no ha leído, entre
  comunicados y avisos de actividades. Lo leído es por cuenta. El equipo del club no tiene campana.
- La campana vive dentro de la aplicación. No se envían avisos con la aplicación cerrada, ni
  número sobre el icono, ni correo por comunicados o avisos.
- Sigue el aislamiento de §7.1: ningún comunicado ni aviso sale de su club.

#### Avisos de actividades

Un aviso de actividad es una línea corta y automática que genera el sistema, no una persona, cuando
cambia un entrenamiento o un partido. Al tocarla se abre el detalle de la actividad.

- Avisan: la creación de un entrenamiento o de un partido, los cambios de fecha, hora y lugar, la
  cancelación y su reactivación, la convocatoria, el resultado y la asistencia.
- Llegan a las cuentas con al menos un jugador que puede ver la actividad: en un entrenamiento, uno
  activo en su categoría o, si va dirigido a equipos, en esos equipos, o uno añadido. En un partido
  próximo, solo a las cuentas con un jugador convocado (ver §17.2).
- Un aviso no revela nada que la familia no pueda ver de esa actividad (ver §17.2). Quien entra a
  la categoría después no recibe los avisos anteriores.
- Una actividad cancelada sigue existiendo (ver §14): su aviso abre el detalle, marcado como
  cancelado.
- Un aviso comparte con los comunicados la misma lista, el mismo contador y las mismas reglas de
  lectura, pero no se escribe, no se edita y no lo retira nadie del equipo del club.

## 15. Reglas de negocio en backend

Las reglas de negocio deben estar protegidas en el backend.

El frontend puede ayudar con validaciones de experiencia de usuario, pero nunca será la única
barrera de seguridad o integridad.

Ejemplo:

```text
React → oculta botón
```

no es suficiente.

Debe existir:

```text
API → valida autorización y regla de negocio
```

## 16. Mensualidades, cargos y pagos

### 16.1. Cargos y saldo

Todo lo que un jugador debe se representa como un Cargo:

```text
MENSUALIDAD
ARBITRAJE
OTRO
```

Todo dinero recibido se representa como un Pago, aplicado a uno o varios cargos.

El saldo de un jugador se calcula a partir de sus cargos y pagos. No existe un campo de saldo
editable a mano.

El estado de cuenta ("al día", "pago parcial", "debe") se deriva de ese cálculo.

Se permiten abonos parciales.

No existen becas ni descuentos generales. La única diferencia de valor entre jugadores es la
mensualidad que el PRESIDENTE le modifica a un jugador concreto (ver §16.7).

### 16.2. Dinero

Los valores se manejan en pesos colombianos (COP) con tipo decimal. Nunca con float ni double.

### 16.3. Inmutabilidad

Un pago confirmado no se edita ni se elimina.

Un error se corrige mediante una anulación, que queda registrada con autor, fecha y motivo.

Todo pago registra quién lo registró, cuándo y por qué medio:

```text
PASARELA
TRANSFERENCIA
EFECTIVO
```

### 16.4. Pasarela de pago

La plataforma nunca almacena ni procesa números de tarjeta, códigos de seguridad ni credenciales
bancarias. El pago ocurre en la pasarela.

Un pago en línea solamente se considera confirmado cuando el backend recibe y verifica la
notificación de la pasarela. El retorno del navegador del usuario no es prueba de pago.

El procesamiento de notificaciones debe ser idempotente: recibir dos veces la misma notificación
no puede registrar dos pagos.

Las llaves privadas de la pasarela son secretos de configuración. No se incluyen en el repositorio
ni en el frontend.

### 16.5. Transferencia y efectivo

Un pago por transferencia queda en revisión hasta que un PRESIDENTE lo confirma.

Un pago en efectivo lo registra un PRESIDENTE o el ENTRENADOR de la categoría del jugador.

Todo pago confirmado genera un recibo consultable desde la cuenta del jugador.

### 16.6. Arbitraje

El costo del arbitraje de un partido se reparte entre los jugadores convocados y genera un Cargo
de tipo ARBITRAJE para cada uno.

### 16.7. Mensualidad

La mensualidad se paga durante los primeros cinco días de cada mes.

El valor vigente en Valfor F.C. es de $65.000 COP por jugador. Es un dato de configuración de
cada club que su PRESIDENTE
puede actualizar; no debe quedar escrito de forma fija en el código.

Un cambio de valor aplica a los cargos que se generen después del cambio. Los cargos ya generados
conservan su valor (ver §13).

Todos los jugadores de un club pagan el mismo valor. La única excepción la decide el PRESIDENTE:
puede seleccionar a uno o a varios jugadores y modificar el valor de la mensualidad de cada uno.
Nadie más puede hacerlo. Los cargos de ese jugador se generan con su valor propio en lugar del
valor del club y, como con este, el cambio no altera los cargos ya generados.

Cada mes se genera un cargo de tipo MENSUALIDAD por cada jugador activo.

Una mensualidad sin pagar después del día 5 simplemente aparece como pendiente en el estado de
cuenta. Es un dato informativo y no tiene consecuencias.

No existe recargo por mora. Deber no genera intereses, cargos adicionales ni restricciones de
ningún tipo para el jugador: no afecta su acceso, sus entrenamientos ni sus convocatorias.

### 16.8. Recordatorios

En esta primera versión no se envían recordatorios de pago, ni automáticos ni manuales. El estado
de cuenta se consulta dentro de la aplicación.

## 17. Datos deportivos

Las estadísticas se calculan a partir de lo registrado en cada partido. No se digitan como
totales.

Esto aplica a:

- Goles y goleadores.
- Partidos jugados.
- Asistencia a entrenamientos.

Corregir un partido corrige automáticamente las estadísticas derivadas.

El sistema no debe modificar automáticamente una convocatoria, un resultado o una programación
registrados manualmente por un entrenador o por el presidente sin una regla explícita que lo autorice.

### 17.1. Tablas de posiciones

Las tablas de posiciones de los torneos son la excepción: en esta primera versión se digitan
manualmente, por torneo y categoría, por un PRESIDENTE o por el ENTRENADOR de la categoría.

Cada tabla registra quién la actualizó por última vez y cuándo.

La actualización automática desde una fuente de la liga no forma parte de esta versión. Si más
adelante se incorpora, debe convivir con la edición manual y no sobrescribir una corrección manual
sin una regla explícita.

### 17.2. Convocatorias y visibilidad de los partidos

Ningún niño debe enterarse por la plataforma de que no fue convocado. Esta regla prevalece sobre
la comodidad de mostrar el calendario completo.

#### Convocatoria a partidos

Quien entrena las categorías del partido, o el PRESIDENTE, crea la convocatoria seleccionando,
de la lista de jugadores agrupada por equipo, a quiénes se les envía. Puede sumar a jugadores de
otros equipos o de otras categorías (ver §11.1); ese jugador ve solo ese partido. Quien entrena una categoría es un ENTRENADOR
asignado a ella, o un PRESIDENTE o DIRECTIVO asignado a ella (ver §8, "Entrenador").

Un partido próximo y su convocatoria solamente son visibles para:

- Los jugadores convocados a ese partido.
- Quienes entrenan las categorías involucradas, incluidos los PRESIDENTES y DIRECTIVOS asignados
  a ellas.
- El presidente.

Un DIRECTIVO que no entrena ninguna de las categorías del partido no lo ve mientras no se haya
jugado.

Para una cuenta no convocada, ese partido no existe: no aparece en su calendario, ni en listados,
ni en contadores, ni en mensajes, ni en avisos de la campana (ver §14.4), ni puede consultarse por
su identificador.

Nadie puede ver desde una cuenta de jugador la lista de los demás convocados.

El cargo de arbitraje solamente se genera para los convocados.

#### Convocatoria a entrenamientos

Un entrenamiento sin equipos es visible para todos los jugadores activos de las categorías a las
que va dirigido, incluso los que no están en ningún equipo.

Un entrenamiento dirigido a equipos es visible solo para los jugadores de esos equipos, los
jugadores que se le añadieron, quienes entrenan alguna de sus categorías y el PRESIDENTE. Para
cualquier otra cuenta no existe, en las mismas condiciones que un partido sin convocar. Un jugador
ve los entrenamientos de su equipo actual.

Quien puede dirigir un entrenamiento y sumar a él a jugadores de otras categorías es el PRESIDENTE
y quien entrena alguna de sus categorías. Quitar a un jugador añadido no le genera ningún aviso.

Un entrenamiento próximo dirigido a un equipo que se desactiva sigue visible para quienes eran de
ese equipo hasta que se realiza.

#### Partidos ya jugados

Una vez jugado, el resultado del partido es público y alimenta las estadísticas (goles,
goleadores, posiciones).

El backend aplica esta visibilidad. No basta con ocultar los partidos en el frontend.

### 17.3. Resultado publicado por la familia

Además de quien entrena las categorías del partido y del PRESIDENTE, la cuenta de un jugador
convocado puede publicar el resultado de ese partido, solo cuando ya empezó:

- Publica el marcador del partido.
- Indica únicamente si su propio jugador jugó y cuántos goles hizo. No ve, ni para publicar, a los
  demás convocados ni lo que otras familias indicaron de los suyos (ver §17.2).

Vale el primer marcador publicado. Después ninguna familia lo cambia: solo lo cambian quien
entrena las categorías del partido o el PRESIDENTE, que además pueden corregir todo lo que publicó
una familia, también lo de su jugador. La corrección se propaga a las estadísticas como cualquier
otra (ver §17).

La familia puede cambiar lo que indicó de su propio jugador mientras quien entrena las categorías
del partido o el PRESIDENTE no lo hayan corregido. Después de esa corrección ya no lo cambia.

## 18. Estados

Los estados representan el estado actual de una entidad.

No se debe crear automáticamente un historial de estados para cada entidad.

Para esta primera versión se utilizará únicamente el estado actual, con una excepción expresa: los
pagos y sus anulaciones conservan siempre su trazabilidad completa (ver §16.3).

## 19. Simplicidad

No sobreingenierizar.

Antes de crear:

- Una nueva entidad.
- Un nuevo servicio.
- Un nuevo patrón.
- Una nueva abstracción.
- Una nueva tecnología.
- Una nueva tabla.

debe existir una necesidad funcional o técnica clara.

El sistema es para varios clubes pequeños, cada uno con decenas de jugadores, no con miles. La arquitectura debe ser
suficientemente sólida para crecer, pero no innecesariamente compleja.

## 20. Pruebas

Las reglas de negocio importantes deben tener pruebas automatizadas.

Se utilizarán:

```text
backend/pruebas/Unitarias
backend/pruebas/Integracion
```

Las pruebas deben validar principalmente:

- Generación de cargos y cálculo de saldos.
- Aplicación de pagos, abonos parciales y anulaciones.
- Idempotencia de las notificaciones de la pasarela.
- Autorización por rol.
- Que solamente el PRESIDENTE puede buscar usuarios por documento, asignar el rol DIRECTIVO y
  retirar roles.
- Que un DIRECTIVO no envía invitaciones, no aprueba ni rechaza ingresos y no asigna, cambia ni
  retira roles.
- Que quien entra como ENTRENADOR o DIRECTIVO no tiene ficha de Jugador, categoría ni mensualidad.
- Que solamente el PRESIDENTE modifica el valor de la mensualidad de un jugador, y que el cambio
  no altera los cargos ya generados.
- Que un integrante nunca tiene más de un rol dentro de un mismo club.
- Que una persona de varios clubes solo puede elegir entre sus clubes y solo ve los datos del
  club elegido.
- Que no se puede crear un club sin asignarle un PRESIDENTE, y que quien se registra con esa
  invitación queda como PRESIDENTE de ese club.
- Que el DESARROLLADOR no puede invitar a un presidente a un club que ya existe.
- Que en un club suspendido solo entra su PRESIDENTE y los demás ven el aviso; que en un club dado
  de baja no entra nadie.
- Que eliminar un club borra toda su información y no afecta a ningún otro club.
- Que no es posible registrarse sin una invitación válida y que la cuenta queda en el club de la
  invitación.
- Que el mismo documento no puede repetirse en un club y sí puede existir en dos clubes.
- Que un club suspendido por impago vuelve a la normalidad al confirmarse su pago, y que un club
  suspendido conserva todos sus datos.
- Que las llaves de la pasarela de un club nunca se devuelven por la API.
- Aislamiento entre clubes: que un usuario de un club no puede ver, contar ni consultar por
  identificador ningún dato de otro club.
- Que solamente el DESARROLLADOR accede al panel de administración y crea clubes, y que solo
  existe una cuenta DESARROLLADOR.
- Que el escudo y los colores de un club solo los cambian el DESARROLLADOR y el PRESIDENTE de ese
  club; ningún otro rol ni el PRESIDENTE de otro club.
- Que el DESARROLLADOR no accede a fichas, datos médicos, finanzas ni pagos de ningún club.
- Que la ficha de Jugador se elimina cuando la cuenta pasa a ENTRENADOR o DIRECTIVO.
- Recuperación de contraseña.
- Que un jugador en espera no accede a ninguna información del club ni genera mensualidad.
- Que solamente el PRESIDENTE puede aprobar o rechazar a un jugador agregado desde la ficha de un
  hermano.
- Que solamente el PRESIDENTE puede enviar invitaciones de registro a su club.
- Que una invitación no sirve dos veces, no sirve vencida y no admite otro correo.
- Que quien se registra con una invitación del club entra directamente con el rol de la
  invitación, sin sala de espera.
- Que el jugador agregado desde la ficha de un hermano queda en espera hasta que el club lo
  aprueba.
- Que rechazar a un jugador en espera borra solo a ese jugador y no afecta a la cuenta ni a sus
  demás jugadores.
- Aislamiento entre categorías y entre cuentas de jugador.
- Que solamente el PRESIDENTE crea, desactiva, reactiva y borra categorías y equipos, asigna
  entrenadores, cambia a un jugador de categoría o de equipo y reincorpora jugadores.
- Que solamente el PRESIDENTE y la propia cuenta del jugador lo retiran del club, y que retirarlo
  desde la cuenta no cambia la cuenta ni sus demás jugadores.
- Que se admite como hermano el documento de un JUGADOR de otra cuenta en otro club, y no el de un
  PRESIDENTE, un DIRECTIVO o un ENTRENADOR.
- Que mientras ese hermano espera, o si se le rechaza, el otro club no cambia; y que al aprobarlo
  el jugador de la otra cuenta queda retirado en la misma operación, o no se aprueba.
- Que los avisos a la familia y al PRESIDENTE no revelan el nombre del otro club ni datos de la
  otra cuenta.
- Que con un documento en dos cuentas el inicio de sesión abre la del jugador activo, o la del que
  está en espera, y niega la contraseña de la otra.
- Que no se reincorpora a un jugador cuyo documento está activo con otra cuenta en otro club, y sí
  cuando solo lo está con la misma cuenta.
- Que no se puede registrar ni cambiar a un documento que ya tiene otra cuenta, aunque esté en dos.
- Que un jugador que entra al club queda en la categoría de su año, o sin categoría si no existe,
  y que entra en ella al crearse.
- Que un jugador nunca está en dos categorías ni en un equipo de otra categoría.
- Que un PRESIDENTE o un DIRECTIVO asignado como entrenador conserva su único rol, y que un
  DIRECTIVO asignado actúa como entrenador en lo deportivo solo en sus categorías asignadas y no
  ve los partidos próximos de las demás.
- Que un entrenador no accede a una categoría que no tiene asignada y sí ve todos los equipos de
  las suyas.
- Que una cuenta de jugador solo ve su categoría, sus equipos, sus entrenadores y la ficha básica
  de los presidentes, directivos y entrenadores de su club.
- Que la ficha básica no entrega documento, fecha de nacimiento ni contacto, que la abre cualquier
  integrante del club y que nadie de otro club ni el DESARROLLADOR la obtiene.
- Que la foto de perfil de un PRESIDENTE, DIRECTIVO o ENTRENADOR solo la ven los integrantes de su
  club, y que la de una cuenta de jugador solo la ve su dueño.
- Que un PRESIDENTE, DIRECTIVO o ENTRENADOR cambia su celular, nombres, apellidos y fecha de
  nacimiento, y que nadie cambia su documento.
- Que solo se borra una categoría o un equipo que nunca tuvo jugadores ni entrenadores.
- Que un jugador retirado no accede a ninguna información del club y conserva todos sus datos,
  salvo su foto.
- Que solamente la propia cuenta y el PRESIDENTE cargan, cambian y quitan la foto de un jugador,
  que solo la ven quienes pueden ver a ese jugador, y que es distinta de la foto de perfil de la
  cuenta.
- Que la foto de un jugador se elimina al retirarlo, de cualquiera de las tres formas, y al
  eliminarse su ficha por cambio de rol, y que al reincorporarlo vuelve sin foto.
- Que una cuenta no convocada no puede ver ni consultar un partido próximo, y que sí ve los
  entrenamientos de su categoría dirigidos a toda ella o a sus equipos.
- Que un entrenamiento dirigido a un equipo no lo ve ni lo consulta un jugador de otro equipo ni uno
  sin equipo, y que un jugador añadido de otra categoría ve solo esa actividad sin que cambien su
  categoría ni sus equipos.
- Que la cuenta de un jugador cambia sus nombres, apellidos y fecha de nacimiento, que ese cambio
  no le cambia la categoría, y que no cambia los datos de otro jugador.
- Que solo la cuenta de un convocado publica el resultado de un partido, y solo si ya empezó; que
  indica únicamente lo de su propio jugador sin recibir la lista de convocados; que vale el primer
  marcador; que después solo lo cambian quien entrena las categorías del partido o el
  PRESIDENTE; y que la familia cambia lo de su propio jugador solo mientras nadie lo haya
  corregido.
- Que la carta de un jugador la ven solo quienes ven su ficha; que la exportan solo su propia
  cuenta y el PRESIDENTE de su club; que sus estadísticas las ponen solo quien entrena su categoría
  y el PRESIDENTE; que el dorsal y la posición los ponen además su propia cuenta; y que ningún endpoint entrega esas estadísticas ordenadas ni comparadas entre
  jugadores.
- Que solo el PRESIDENTE, los DIRECTIVOS y los ENTRENADORES publican comunicados, que un
  ENTRENADOR no los dirige fuera de sus categorías, y que solo quien lo publicó y el PRESIDENTE
  retiran un comunicado.
- Que un comunicado llega solo a las cuentas con un jugador activo en su audiencia, no se ve desde
  otro club ni con su identificador, y que retirarlo lo quita del contador de todas las cuentas.
- Que publicar un comunicado no envía ningún aviso fuera de la aplicación.
- Que un aviso de actividad llega solo a las cuentas con un jugador activo en la categoría y, en un
  partido próximo, solo a las de los convocados; que no revela lo que la familia no puede ver; que
  no se ve desde otro club ni con su identificador; y que no sale nada fuera de la aplicación.
- Que los endpoints públicos no exponen documento, contacto, datos médicos ni pagos.
- Cálculo de estadísticas deportivas.
- Transiciones de estados.
- Persistencia e integración con PostgreSQL cuando corresponda.

## 21. Docker

La aplicación debe poder ejecutarse mediante Docker.

La configuración de Docker debe permitir reproducir el entorno de desarrollo y facilitar
posteriormente el despliegue.

No se deben introducir dependencias externas innecesarias únicamente para dockerizar el proyecto.

## 22. Migraciones de base de datos

Entity Framework Core será responsable de gestionar el modelo de persistencia y las migraciones.

Las modificaciones estructurales de la base de datos deben realizarse mediante migraciones
controladas.

No se deben realizar cambios manuales arbitrarios en producción como mecanismo habitual de
evolución del esquema.

## 23. API

La API debe utilizar:

- HTTP semántico.
- DTOs.
- Validación de entrada.
- Respuestas coherentes.
- Autorización.
- Manejo controlado de errores.
- Swagger/OpenAPI.

No exponer directamente entidades de persistencia como contrato público de la API cuando un DTO
sea apropiado.

Los DTOs públicos y los privados de una misma entidad son tipos distintos. Un DTO público de
jugador no contiene campos privados, ni siquiera vacíos.

## 24. Diseño visual

El diseño aprobado en el lienzo del proyecto es la referencia visual:

<https://claude.ai/artifact/PiDB5BQXRYCeN6nnB4TikR>

Ese lienzo define la estructura y el comportamiento de las pantallas para todos los clubes. El
escudo y los colores no son fijos: cada club se muestra con los suyos (ver §7.2). Los de Valfor
F.C. son su escudo y los colores naranja, vinotinto, dorado y negro.

El panel de administración de la plataforma no está en el lienzo; su diseño está pendiente.

Toda pantalla debe funcionar en teléfono y en escritorio.

Toda la interfaz ofrece tema claro y tema oscuro. El cambio entre ellos está siempre al alcance
con un toque: dentro del menú del perfil de la barra superior y, en las pantallas que no tienen
menú de perfil (inicio de sesión, registro, sala de espera y similares), como un botón visible. Su
nombre accesible dice con palabras a qué tema cambia. Los colores de cada club deben verse bien en los dos temas. El lienzo solo muestra el tema claro.

En el diseño, las pantallas móviles rotuladas para "acudientes" corresponden a la cuenta del
jugador.

Los estados de pago deben distinguirse por texto además de por color.

La campana (ver §14.4) va en la barra superior de la cuenta de jugador, sin saturarla en el
teléfono. Su nombre accesible dice cuántos avisos hay sin leer, y leído y sin leer se distinguen por
texto además de por color. Un comunicado y un aviso de actividad se distinguen por texto, no solo
por icono o color.

## 25. Regla para Claude Code

Claude Code actúa como implementador de las decisiones definidas en la especificación.

No debe cambiar unilateralmente decisiones de negocio ya aprobadas.

Si detecta una contradicción, ambigüedad o requisito faltante que afecte significativamente la
arquitectura o el comportamiento:

- Debe identificar el problema.
- Debe documentarlo.
- Debe detener la implementación de esa parte.
- Debe solicitar una decisión antes de inventar una regla de negocio.

No debe introducir funcionalidades no solicitadas bajo la premisa de que "podrían ser útiles".

Los valores de ejemplo del diseño (nombres de jugadores, cifras, torneos, valor de la mensualidad)
no son reglas de negocio ni datos reales.

## 26. Trazabilidad

Cada funcionalidad implementada debe poder relacionarse con:

```text
Requisito
   ↓
Especificación
   ↓
Plan
   ↓
Tarea
   ↓
Código
   ↓
Prueba
```

Las tareas deben ser suficientemente concretas para comprobar si fueron completadas.

## 27. Definición de terminado y no regresión

### 27.1. Backend y frontend se construyen juntos

Cada funcionalidad se construye completa, de extremo a extremo: su parte de backend y su pantalla
de frontend se hacen en la misma tarea.

No se avanza a la siguiente funcionalidad dejando un endpoint sin pantalla que lo use, ni una
pantalla sin su endpoint real.

No se construye primero todo el backend y después todo el frontend.

### 27.2. Criterios

Una tarea no se considera terminada únicamente porque el código compile.

Cuando corresponda, debe incluir:

- Implementación del backend.
- Pantalla de frontend que usa esa funcionalidad contra la API real.
- Documentación en español de cada clase (ver §3).
- Ninguna clase por encima de 250 líneas (ver §2.3).
- Validaciones.
- Pruebas.
- Integración.
- Documentación.
- Migraciones.
- Actualización de contratos.
- Verificación de compilación.
- Verificación de pruebas.

Una tarea debe marcarse como completada solamente después de verificar sus criterios.

Una modificación no debe romper funcionalidades previamente implementadas.

Antes de considerar una funcionalidad terminada se debe ejecutar la batería de pruebas relevante.

Cuando sea necesario, se deben agregar pruebas para evitar que el defecto reaparezca.

## 28. Decisiones pendientes

Estas decisiones no están tomadas. Según §25, no deben resolverse por cuenta propia durante la
implementación.

- **Dirección del sitio público.** Cómo llega un visitante al sitio público de un club concreto.
- **Pago por el uso de la plataforma.** Valor, periodicidad, fecha de corte y quién lo paga dentro
  del club.
- **Sitio público de un club suspendido.** Si el sitio público de un club suspendido o dado de
  baja sigue visible.
- **Matrícula.** Si existe un cobro de inscripción.
- **Fuente automática de posiciones.** Todavía no se conoce ningún enlace o servicio de las ligas
  del que se puedan leer las tablas. Mientras no exista, se mantienen manuales (ver §17.1).
- **Recordatorios de pago.** Quedan fuera de esta versión (ver §16.8). Si más adelante se quieren,
  habrá que decidir el canal.
- **Ficha con historial.** Qué pasa si una cuenta que ya tiene cargos, pagos o partidos como
  jugador pasa a ENTRENADOR o DIRECTIVO.

## 29. Principio final

El sistema debe priorizar:

```text
Claridad
↓
Correctitud
↓
Seguridad
↓
Mantenibilidad
↓
Escalabilidad
```

La solución más sencilla que cumpla correctamente las reglas de negocio será preferible a una
solución más compleja.

**Versión**: 4.12.0 | **Ratificada**: 2026-10-06 | **Última enmienda**: 2026-10-13
