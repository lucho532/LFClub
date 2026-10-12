"""
Genera el balón de LFClub en todos los sitios donde se dibuja, desde una sola geometría.

Escribe:
- frontend/src/arranque/geometriaBalon.ts: los trazos para la animación de arranque de la web.
- frontend/android/.../drawable/balon_icono.xml: primer plano del icono adaptable (108 dp).
- frontend/android/.../drawable/balon_arranque.xml y balon_arranque_animado.xml: la pantalla de
  arranque (288 dp) y su giro desde Android 12.
- frontend/android/.../mipmap-*/ic_launcher.png e ic_launcher_round.png: el icono de Android 7, que
  no usa el icono adaptable. Para rasterizarlo usa Chrome sin interfaz y Pillow.
- frontend/public/favicon.svg, favicon-32.png y apple-touch-icon.png: el icono de la web.

Uso, desde la raíz del repositorio: python scripts/generar-balon.py
El balón tiene el estilo de paneles en estrella de un balón clásico, sin ninguna marca.
"""
import math
import os
import subprocess
import tempfile

from PIL import Image

RAIZ = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
FRONTEND = os.path.join(RAIZ, 'frontend')
RES = os.path.join(FRONTEND, 'android', 'app', 'src', 'main', 'res')
CHROME = r'C:\Program Files\Google\Chrome\Application\chrome.exe'

AZUL, BLANCO, BORDE, FONDO = '#24448C', '#FFFFFF', '#0F1F45', '#231F1E'
C, R = 54.0, 30.0  # Centro y radio del balón en un lienzo de 108 x 108.


def polar(r, grados):
    """Punto a esa distancia del centro, con el ángulo medido desde arriba en el sentido del reloj."""
    a = math.radians(grados)
    return (C + r * math.sin(a), C - r * math.cos(a))


def num(v):
    return f'{v:.2f}'.rstrip('0').rstrip('.')


def triangulo(p1, p2, p3, curva):
    """Triángulo esférico: cada lado se curva hacia fuera del centroide en `curva` unidades."""
    cx, cy = (p1[0] + p2[0] + p3[0]) / 3, (p1[1] + p2[1] + p3[1]) / 3
    puntos = [p1, p2, p3]
    trazo = f'M{num(p1[0])},{num(p1[1])}'
    for i in range(3):
        a, b = puntos[i], puntos[(i + 1) % 3]
        mx, my = (a[0] + b[0]) / 2, (a[1] + b[1]) / 2
        n = math.hypot(mx - cx, my - cy) or 1
        control = (mx + (mx - cx) / n * curva, my + (my - cy) / n * curva)
        trazo += f' Q{num(control[0])},{num(control[1])} {num(b[0])},{num(b[1])}'
    return trazo + ' Z'


def circulo(cx, cy, r):
    return (f'M{num(cx - r)},{num(cy)} A{num(r)},{num(r)} 0 1,1 {num(cx + r)},{num(cy)} '
            f'A{num(r)},{num(r)} 0 1,1 {num(cx - r)},{num(cy)} Z')


def geometria():
    paneles = []
    for a in (60, 180, 300):  # Triángulos azules pegados al triángulo blanco central.
        paneles.append(triangulo(polar(9, a - 60), polar(9, a + 60), polar(25.5, a), 1.8))
    for a in (0, 120, 240):  # Triángulos azules del borde, entre las puntas de los anteriores.
        paneles.append(triangulo(polar(27.5, a - 38), polar(27.5, a + 38), polar(41, a), 1.8))
    return {
        'contorno': circulo(C, C, R),
        'paneles': ' '.join(paneles),
        # Sombra: el balón menos un círculo desplazado arriba a la izquierda (relleno par-impar).
        'sombra': circulo(C, C, R) + ' ' + circulo(C - 6, C - 6, R + 1),
        'brillo': circulo(C - 11, C - 12, 5.5),
    }


def escribir(ruta, texto):
    os.makedirs(os.path.dirname(ruta), exist_ok=True)
    with open(ruta, 'w', encoding='utf-8', newline='\n') as archivo:
        archivo.write(texto)


def vector(g, tamano_dp, comentario):
    return f'''<?xml version="1.0" encoding="utf-8"?>
<!-- {comentario} Generado por scripts/generar-balon.py. -->
<vector xmlns:android="http://schemas.android.com/apk/res/android"
    android:width="{tamano_dp}dp"
    android:height="{tamano_dp}dp"
    android:viewportWidth="108"
    android:viewportHeight="108">
    <path android:fillColor="{BLANCO}" android:pathData="{g['contorno']}" />
    <group android:name="giro" android:pivotX="54" android:pivotY="54">
        <clip-path android:pathData="{g['contorno']}" />
        <path
            android:fillColor="{AZUL}"
            android:pathData="{g['paneles']}"
            android:strokeColor="{BLANCO}"
            android:strokeLineJoin="round"
            android:strokeWidth="0.9" />
    </group>
    <group>
        <clip-path android:pathData="{g['contorno']}" />
        <path
            android:fillAlpha="0.22"
            android:fillColor="{BORDE}"
            android:fillType="evenOdd"
            android:pathData="{g['sombra']}" />
        <path android:fillAlpha="0.45" android:fillColor="{BLANCO}" android:pathData="{g['brillo']}" />
    </group>
    <path android:pathData="{g['contorno']}" android:strokeColor="{BORDE}" android:strokeWidth="1.2" />
</vector>
'''


ANIMADO = '''<?xml version="1.0" encoding="utf-8"?>
<!-- El balón de la pantalla de arranque girando una vuelta. Android lo anima desde la versión 12;
     en las anteriores se ve quieto. Generado por scripts/generar-balon.py. -->
<animated-vector xmlns:android="http://schemas.android.com/apk/res/android"
    xmlns:aapt="http://schemas.android.com/aapt"
    android:drawable="@drawable/balon_arranque">
    <target android:name="giro">
        <aapt:attr name="android:animation">
            <objectAnimator
                android:duration="1000"
                android:interpolator="@android:interpolator/accelerate_decelerate"
                android:propertyName="rotation"
                android:valueFrom="0"
                android:valueTo="360"
                android:valueType="floatType" />
        </aapt:attr>
    </target>
</animated-vector>
'''


def typescript(g):
    return f'''// Geometría del balón de LFClub en un lienzo de 108 x 108, la misma que dibujan los recursos de
// Android (drawable/balon_icono.xml y drawable/balon_arranque.xml). Generado por
// scripts/generar-balon.py: si cambia el dibujo, se regeneran los dos lados a la vez.

/** Circunferencia del balón: radio 30 centrado en (54, 54). */
export const CONTORNO = '{g['contorno']}';

/** Los seis paneles azules en estrella; se recortan con el contorno. */
export const PANELES = '{g['paneles']}';

/** Sombra abajo a la derecha: el contorno menos un círculo desplazado (relleno par-impar). */
export const SOMBRA = '{g['sombra']}';

/** Brillo arriba a la izquierda. */
export const BRILLO = '{g['brillo']}';
'''


def svg_icono(g, forma):
    """El balón sobre una forma de fondo, recortado al balón y a esa forma (lienzo 12..96)."""
    return f'''<svg xmlns="http://www.w3.org/2000/svg" viewBox="12 12 84 84" width="512" height="512">
  {forma}
  <defs><clipPath id="b"><path d="{g['contorno']}"/></clipPath></defs>
  <path d="{g['contorno']}" fill="{BLANCO}"/>
  <g clip-path="url(#b)">
    <path d="{g['paneles']}" fill="{AZUL}" stroke="{BLANCO}" stroke-width="0.9" stroke-linejoin="round"/>
    <path d="{g['sombra']}" fill="{BORDE}" fill-opacity="0.22" fill-rule="evenodd"/>
    <path d="{g['brillo']}" fill="{BLANCO}" fill-opacity="0.45"/>
  </g>
  <path d="{g['contorno']}" fill="none" stroke="{BORDE}" stroke-width="1.2"/>
</svg>'''


FORMA_CUADRADA = f'<rect x="12" y="12" width="84" height="84" rx="18" fill="{FONDO}"/>'
FORMA_REDONDA = f'<circle cx="54" cy="54" r="42" fill="{FONDO}"/>'


def rasterizar(svg_texto, carpeta, nombre):
    """PNG de 512 px con fondo transparente, dibujado por Chrome sin interfaz."""
    svg = os.path.join(carpeta, nombre + '.svg')
    png = os.path.join(carpeta, nombre + '.png')
    escribir(svg, svg_texto)
    subprocess.run([CHROME, '--headless=new', '--disable-gpu', '--hide-scrollbars',
                    '--default-background-color=00000000', f'--screenshot={png}',
                    '--window-size=512,512', 'file:///' + svg.replace(os.sep, '/')],
                   check=True, capture_output=True)
    return Image.open(png).convert('RGBA')


def favicon(g):
    """El balón solo, sin fondo, en SVG para los navegadores y en PNG para los que no lo leen."""
    publica = os.path.join(FRONTEND, 'public')
    solo = svg_icono(g, '').replace('viewBox="12 12 84 84"', 'viewBox="23 23 62 62"')
    escribir(os.path.join(publica, 'favicon.svg'), solo.replace(' width="512" height="512"', ''))
    with tempfile.TemporaryDirectory() as temporal:
        rasterizar(solo, temporal, 'favicon').resize((32, 32), Image.LANCZOS).save(
            os.path.join(publica, 'favicon-32.png'))
        rasterizar(svg_icono(g, FORMA_CUADRADA.replace('rx="18"', 'rx="0"')), temporal, 'apple').resize(
            (180, 180), Image.LANCZOS).save(os.path.join(publica, 'apple-touch-icon.png'))


def iconos_png(g):
    """Rasteriza el icono cuadrado y el redondo de Android 7 y los escala a cada densidad."""
    tamanos = {'mdpi': 48, 'hdpi': 72, 'xhdpi': 96, 'xxhdpi': 144, 'xxxhdpi': 192}
    with tempfile.TemporaryDirectory() as temporal:
        for nombre, forma in (('ic_launcher.png', FORMA_CUADRADA), ('ic_launcher_round.png', FORMA_REDONDA)):
            imagen = rasterizar(svg_icono(g, forma), temporal, nombre)
            for carpeta, lado in tamanos.items():
                imagen.resize((lado, lado), Image.LANCZOS).save(os.path.join(RES, f'mipmap-{carpeta}', nombre))


def main():
    g = geometria()
    escribir(os.path.join(FRONTEND, 'src', 'arranque', 'geometriaBalon.ts'), typescript(g))
    escribir(os.path.join(RES, 'drawable', 'balon_icono.xml'),
             vector(g, 108, 'Balón de LFClub: primer plano del icono, dentro de la zona segura.'))
    escribir(os.path.join(RES, 'drawable', 'balon_arranque.xml'),
             vector(g, 288, 'Balón de LFClub: pantalla de arranque, dentro del círculo de 192 dp.'))
    escribir(os.path.join(RES, 'drawable', 'balon_arranque_animado.xml'), ANIMADO)
    iconos_png(g)
    favicon(g)
    print('Balón generado.')


if __name__ == '__main__':
    main()
