# Amueblá tu pieza

Trabajo práctico de Realidad Aumentada para la materia Simulaciones (UAI).

App Android que detecta el piso con ARCore y permite colocar muebles 3D a escala real para ver cómo quedarían en una habitación. Los muebles se pueden mover, rotar, escalar, recolorear, duplicar y borrar.

**Video demo:** [YouTube](https://youtu.be/LINK_PENDIENTE)

## Funcionalidades

- Detección de planos horizontales con visual semitransparente.
- 5 muebles hechos con primitivas y medidas reales en metros: Mesa, Silla, Sillón, Lámpara (con luz puntual) y Planta.
- Los muebles aparecen apoyados en el piso y mirando a la cámara.
- Selección con indicador (aro naranja debajo del mueble).
- Mover arrastrando sobre el plano, rotar con dos dedos y escalar con pellizco (de 0.5x a 2x).
- Panel del mueble seleccionado: Rotar 45°, Cambiar color, Duplicar y Borrar.
- Botones para mostrar/ocultar planos y borrar todo.
- Contador de planos detectados y muebles colocados.
- Mensajes de ayuda y aviso del estado de la sesión AR cuando todavía no está lista.
- Tocar la UI no coloca muebles detrás de los botones.

## Controles

| Acción | Gesto |
|---|---|
| Elegir mueble | Tocar un botón del menú de abajo |
| Colocar | Tocar el piso detectado |
| Seleccionar | Tocar un mueble |
| Mover | Arrastrar el mueble con un dedo |
| Rotar | Girar con dos dedos |
| Escalar | Pellizcar con dos dedos |
| Rotar 45° / Color / Duplicar / Borrar | Botones del panel del seleccionado |

## Versiones

- Unity 6.4 (6000.4.1f1), template AR Mobile
- AR Foundation 6.4.2
- Google ARCore XR Plugin 6.4.2
- XR Interaction Toolkit 3.4.0
- Universal Render Pipeline 17.4.0
- Input System 1.19.0
- Android: IL2CPP, ARM64, API mínima 30, ARCore requerido
- Package name: `com.manu.tpar`

## Cómo compilar

1. Abrir el proyecto con Unity 6000.4.1f1 (con soporte Android instalado).
2. Verificar que la plataforma activa sea Android (File > Build Profiles).
3. La única escena del build es `Assets/Scenes/AmueblaTuPieza.unity`.
4. Build y guardar como `Builds/TP_AR.apk`.

## Cómo instalar

Con el celular en modo desarrollador y depuración USB activada:

```
adb install -r Builds/TP_AR.apk
```

O copiar el APK al celular y abrirlo (hay que permitir instalar apps de orígenes desconocidos). El celular tiene que ser compatible con ARCore y tener instalado Google Play Services para AR.

## Scripts principales

- `Assets/Scripts/AmueblarUI.cs`: maneja toda la UI (menú de muebles, panel del seleccionado, contadores, ayuda y estado de la sesión). Usa el `ARTemplateMenuManager` y el `ObjectSpawner` del template.
- `Assets/Scripts/Mueble.cs`: va en cada prefab. Tiene el nombre, las partes que cambian de color y el indicador de selección.
- `Assets/Scripts/BloqueoToqueUI.cs`: desactiva el spawn mientras se está tocando la UI.

Del template se reutilizan `ObjectSpawner`, `ARInteractorSpawnTrigger`, `ARTemplateMenuManager` y el sistema de interacción de XRI (`XRGrabInteractable` + `ARTransformer`).

## Decisiones técnicas

- En vez de reescribir el spawn y la manipulación se usaron los componentes del template. Los prefabs copian la configuración del interactable de ejemplo (Rigidbody kinematic, XRGrabInteractable y ARTransformer) y se les cambió el rango de escala a 0.5–2.
- El pivot de cada mueble está en la base, así quedan apoyados en el plano sin flotar ni hundirse.
- El frente de cada mueble mira hacia +Z, y el `ObjectSpawner` los orienta hacia la cámara (se sacó la rotación random).
- El mueble seleccionado se toma del `focusInteractable` del `XRInteractionGroup`, que se mantiene después de soltar el dedo.
- Cambiar color crea un material propio por mueble para no afectar a los demás.
- La detección de planos está en modo horizontal y se usa el plano "feathered" del template, que es semitransparente.
- Las suscripciones a planos usan la API de AR Foundation 6 (`trackablesChanged`).

## Dificultades

- El chequeo de UI del template (`IsPointerOverGameObject`) no siempre detecta los toques en pantalla táctil, así que se agregó un script que hace raycast a la UI con la posición de cada toque y apaga el spawn un momento.
- El menú del template depende de animators y botones propios, así que se dejó funcionando pero con su UI oculta, y se armó una UI nueva encima.
- Al no tener celular conectado durante el desarrollo, las pruebas se hicieron con XR Simulation en el editor.
