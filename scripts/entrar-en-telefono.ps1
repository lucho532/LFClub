<#
  Inicia sesión en la app LFClub del teléfono conectado por USB con una de las cuentas de prueba.
  Lee correo y contraseña de C:\Users\usuario\.lfclub-keys\cuentas-de-prueba.txt (fuera del repo);
  este script no contiene ninguna credencial.

  Uso:  .\scripts\entrar-en-telefono.ps1 -Rol PRESIDENTE
        (roles: PRESIDENTE, DIRECTIVO, ENTRENADOR, JUGADOR)

  Antes: el teléfono desbloqueado, con la depuración USB activa y la app LFClub abierta en la
  pantalla "Iniciar sesión". Las coordenadas son de una pantalla de 1080x2240.
#>
param(
    [Parameter(Mandatory)]
    [ValidateSet('PRESIDENTE', 'DIRECTIVO', 'ENTRENADOR', 'JUGADOR')]
    [string]$Rol,
    [string]$Archivo = 'C:\Users\usuario\.lfclub-keys\cuentas-de-prueba.txt'
)

$linea = Get-Content $Archivo | Where-Object { $_ -match "^$Rol\s" } | Select-Object -First 1
if (-not $linea) { throw "No hay una cuenta $Rol en $Archivo" }
$partes = $linea -split '\s+'
$clave = $partes[2]

# Se entra con el documento (solo dígitos), no con el correo: el teclado del teléfono corrige y
# pone mayúsculas en los correos ("Presidente. Prueba@..."), y el campo acepta "correo o documento".
$documentos = @{ PRESIDENTE = '901000101'; DIRECTIVO = '902000102'; ENTRENADOR = '903000103'; JUGADOR = '904000104' }
$correo = $documentos[$Rol]

# Cuadros de la pantalla de entrada
$campoCorreo = '540 660'
$campoClave = '540 918'
$botonEntrar = '540 1098'

adb shell input tap $campoCorreo.Split(' ')[0] $campoCorreo.Split(' ')[1]
adb shell input keyevent KEYCODE_MOVE_END
adb shell input keyevent (@('KEYCODE_DEL') * 40)   # borra lo que hubiera
adb shell input text $correo
adb shell input tap $campoClave.Split(' ')[0] $campoClave.Split(' ')[1]
adb shell input keyevent KEYCODE_MOVE_END
adb shell input keyevent (@('KEYCODE_DEL') * 40)
adb shell input text $clave
adb shell input keyevent KEYCODE_BACK    # cierra el teclado
adb shell input tap $botonEntrar.Split(' ')[0] $botonEntrar.Split(' ')[1]
Write-Host "Sesión enviada como $Rol ($correo)."
