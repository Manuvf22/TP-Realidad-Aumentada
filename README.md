# Amueblá tu pieza

Trabajo práctico de Realidad Aumentada para la materia Simulaciones (UAI).

App Android que detecta el piso con ARCore y permite colocar muebles 3D a escala real para ver cómo quedarían en una habitación. Los muebles se pueden mover, rotar, escalar, apilar, subir y bajar, recolorear, cambiar de material, duplicar y borrar, y cualquier acción se puede deshacer.

**Video demo:** [YouTube](https://youtu.be/LINK_PENDIENTE)

## Funcionalidades

### Colocar muebles
- Detección de planos horizontales con visual semitransparente.
- Vista previa ("fantasma") semitransparente del mueble elegido: sigue el centro de la pantalla o el dedo, se pone verde si el lugar está libre y rojo si choca con otro mueble.
- Se coloca soltando el dedo sobre el piso, con una animación de escala con rebote, un sonido y una vibración corta.
- Se puede apilar: si se apunta a la tapa de una mesa, estante o rack, el mueble queda apoyado arriba.

### Catálogo
- 9 categorías con variantes, todas hechas con primitivas y medidas reales en metros:
  - Mesa: comedor, ratona, escritorio, redonda.
  - Silla: madera, oficina (con ruedas), banqueta, gamer.
  - Sillón: 2 cuerpos, 3 cuerpos, puff, esquinero.
  - Lámpara: de pie, de mesa, colgante (con luz).
  - Planta: maceta chica, planta alta, cactus.
  - Cama: 1 plaza, 2 plazas.
  - Estante: biblioteca, estante bajo.
  - TV: 55" y 43" con rack.
  - Alfombra: rectangular y redonda.
- El menú se arma solo a partir de un catálogo (`Assets/Datos/Catalogo.asset`). Cada mueble es un ScriptableObject `FurnitureData` con nombre, categoría, prefab e ícono, así que para agregar un mueble alcanza con crear el asset y sumarlo a la lista.

### Mueble seleccionado
- Indicador naranja animado debajo del mueble y nombre con sus medidas (ancho × alto × profundidad en cm según la escala actual).
- Mover arrastrando, con movimiento suavizado.
- Rotar con dos dedos con saltos de 15° (con un "tick" en cada salto).
- Escalar con pellizco (de 0.5x a 2x).
- Botones: Rotar 45°, Color, Material (Madera / Metal / Tela), Duplicar y Borrar (con animación de salida).
- Botones ▲ ▼ para subir o bajar el mueble de a 5 cm. Si se mantienen apretados, repiten. No puede quedar debajo del plano.

### General
- Deshacer la última acción (colocar, mover, rotar, escalar, subir/bajar, cambiar color o material, borrar y borrar todo).
- Captura: oculta la UI, guarda la foto en la galería (`Pictures/AmueblaTuPieza`) y vuelve a mostrar la UI.
- Mostrar/ocultar planos y borrar todo.
- Contador de planos y muebles, mensajes de ayuda según lo que se está haciendo y estado de la sesión AR.
- Sombra de contacto (blob shadow) debajo de cada mueble, que se aclara si el mueble está levantado.
- Light Estimation: la intensidad y el color de la luz direccional siguen la luz real que estima ARCore.
- Sonidos generados por código (pop al colocar, click en botones) y vibración corta al colocar, seleccionar y borrar.
- Tocar la UI no coloca muebles.

## Controles

| Acción | Gesto |
|---|---|
| Elegir mueble | Tocar una categoría abajo y después una variante |
| Colocar | Arrastrar el dedo por el piso y soltar (el fantasma tiene que estar verde) |
| Apilar | Soltar el fantasma arriba de una mesa, estante o rack |
| Seleccionar | Tocar un mueble |
| Deseleccionar | Tocar un lugar vacío |
| Mover | Arrastrar el mueble con un dedo |
| Rotar | Girar con dos dedos (cada 15°) |
| Escalar | Pellizcar con dos dedos |
| Subir / bajar | Botones ▲ ▼ del panel (mantener para repetir) |
| Rotar 45° / Color / Material / Duplicar / Borrar | Botones del panel del seleccionado |
| Deshacer / Captura / Planos / Borrar todo | Botones de arriba a la derecha |

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

## Scripts

- `AmueblarUI.cs`: maneja toda la UI. Arma el menú de categorías y variantes desde el catálogo y maneja el panel del seleccionado, los contadores, la ayuda, los avisos y el estado de la sesión. Usa el `ARTemplateMenuManager` y el `ObjectSpawner` del template.
- `ColocadorMuebles.cs`: fantasma de previsualización, validación del lugar (verde/rojo), colocación al soltar y bloqueo de toques sobre la UI.
- `Mueble.cs`: va en cada prefab. Tiene el nombre, las partes que cambian de color o material, el indicador, la sombra, las medidas y las animaciones de entrada y salida.
- `TransformadorMueble.cs`: grab transformer que corre después del `ARTransformer`. Hace el snap de 15°, apoya el mueble sobre el plano u otro mueble y respeta la altura extra.
- `Apoyo.cs`: calcula sobre qué está apoyado un mueble (plano de AR o tapa de otro mueble).
- `Historial.cs`: pila para deshacer.
- `Feedback.cs`: genera los sonidos por código y maneja la vibración.
- `CapturaPantalla.cs`: oculta la UI, saca la foto y la guarda en la galería con MediaStore.
- `LuzEstimada.cs`: aplica la estimación de luz de ARCore a la luz direccional.
- `BotonRepetir.cs`: botón que repite mientras se mantiene apretado.
- `FurnitureData.cs` y `CatalogoMuebles.cs`: ScriptableObjects del catálogo.

Del template se reutilizan `ObjectSpawner`, `ARTemplateMenuManager` y el sistema de interacción de XRI (`XRGrabInteractable` + `ARTransformer`).

## Decisiones técnicas

- Para no reescribir la manipulación se mantuvo el `ARTransformer` del template y se agregó un transformer propio después, en la lista de transformers del `XRGrabInteractable`. Así el snap de giro, el apilado y la altura se resuelven sin pelear con XRI.
- El suavizado del movimiento usa el smoothing de posición, rotación y escala del `XRGrabInteractable`. Al soltar, el mueble termina de llegar a su posición final para que no quede a mitad de camino.
- El giro se acumula a partir del giro de los dedos y se redondea cada 15°. Si se redondeara la rotación del objeto directamente, los giros chicos se perderían.
- Para apilar se tira un raycast hacia abajo contra los colliders de los otros muebles y otro contra los planos de AR, y se usa el más alto. La lámpara colgante no permite apilar cosas encima.
- Para colocar se usa `ObjectSpawner.TrySpawnObject` con el punto del fantasma, así el mueble queda igual que la vista previa.
- El fantasma copia solo las mallas del prefab (sin colliders ni interactables) y usa un material transparente con property blocks para cambiar de color.
- Color y material se cambian con property blocks y materiales compartidos, sin crear un material nuevo por mueble.
- Borrar no destruye el objeto enseguida: lo desactiva para poder deshacerlo, y se destruye cuando sale del historial.
- Los íconos del menú se generaron renderizando cada prefab con una cámara.
- La captura se guarda con `MediaStore` (Android 10+), así que no hace falta pedir permiso de almacenamiento.

## Dificultades

- El chequeo de UI del template (`IsPointerOverGameObject`) no siempre detecta los toques en pantalla táctil, así que se hace un raycast a la UI con la posición de cada toque.
- El `ARTransformer` recalcula la rotación desde la rotación actual del objeto cada frame, por eso el snap tuvo que acumular el giro aparte.
- El menú del template depende de animators y botones propios, así que se dejó funcionando pero con su UI oculta, y se armó una UI nueva encima.
- Al no tener celular conectado durante el desarrollo, las pruebas se hicieron con XR Simulation en el editor.
