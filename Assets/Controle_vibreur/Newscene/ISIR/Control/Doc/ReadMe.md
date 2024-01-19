# WAVY Contrôler Haptique

Système de contrôle du dispositif haptique dans Unity. Permets de contrôler les vibreurs indépendamment (appelé HapticDevice dans Unity). Le système s'occupe du protocole de communication et la connexion (COM Série) avec le dispositif. Il permet également de définir des signaux qui pourront être envoyés aux vibreurs et de développer des comportements pour activer ceux-ci.  


## Générateur de signaux - Haptic Clip

Les clips haptiques sont basées sur des générateurs de signaux. Il en existe plusieurs générateurs prédéfinis. Ils sont tous basés sur la classe abstraite AbstractSamplesGenerator. Suivant le device qui sera utilisé et si le générateur le nécessite il est nécessaire de définir la fréquence de base à utiliser.

| Haptic Clip     |     Description     |        Inspecteur |
| :------------ | :-------------: | -------------: |
| AbstractSamplesGenerator       |     Classe abstraite servant de base à tous les générateurs     |         |
| ConstSamplesGenerator        |     Génère un signal constant (Attention certains dispositifs ne supportent par les signaux constants)      |         ![ConstSamplesGenerator Inspector](Ressources\ConstSamplesGenerator.PNG "ConstSamplesGenerator Inspector") |
| WaveformSamplesGenerator        |     Génère des signaux forme d'onde : Sinus, Carré, Triangle      |         ![WaveformSamplesGenerator Inspector](Ressources\WaveformSamplesGenerator.PNG "WaveformSamplesGenerator Inspector") |
| AudioSamplesGenerator     |     Génère un signal à partir d'un fichier audio      |         ![AudioSamplesGenerator Inspector](Ressources\AudioSamplesGenerator.PNG "AudioSamplesGenerator Inspector") |
| CurveSamplesGenerator        |     Génère un signal à partir d'une courbe      |         ![CurveSamplesGenerator Inspector](Ressources\CurveSamplesGenerator.PNG "CurveSamplesGenerator Inspector") |
| MixerSamplesGenerator        |     Génère un signal en mixant deux signaux, opération possible : Moyenne, Multiplier, Somme      |         ![MixerSamplesGenerator Inspector](Ressources\MixerSamplesGenerator.PNG "MixerSamplesGenerator Inspector") |

Les clips haptic sont utilisés sous forme de prefab par les HapticSource pour générer le signal. 

## Haptic Source

Une source haptique est un script permettant la gestion d'un (ou plusieurs) clip haptique qui pourra être joué par un HapticDevice.

![HapticSource Inspector](Ressources\HapticSource.PNG "HapticSource Inspector")

Une source haptique prend une liste de prefab de HapticClip. Les clips seront copiés/instanciés au début de l'utilisation (c'est-à-dire qu’un même clip peut être donné à plusieurs sources sans interférences). Chaque clip possède un volume permettant de le moduler vis-à-vis des autres clips. Au niveau global, la source à un volume qui module le signal généré, un volume max permettant de limiter le volume final. Lorsque la source est stoppée ou mise en pause, la source retourne un signal nul, sinon elle retourne le flux du signal généré par le clip haptique. On peut choisir entre trois façons de mixer les signaux : SUM (somme des signaux), PRODUCT (produit des signaux) ou AVERAGE (moyenne des signaux). Le signal peut être joué de plusieurs manières : Loop (signal joué en boucle), Duration (joué juste le temps défini), Once (joué juste une fois).
Les sources peuvent être contrôlées par script grâce à plusieurs méthodes publiques.
| Méthode publique       |     description    |
| :------------ | :-------------: |
| start()       |     Démarrer la source (automatique si *Start on Awake* activé), ou annule la pause.    |
| stop()     |   Stopper la source    |
| pause()        |     Mettre en pause la source      |
| togglePause()        |     Passer de pause à play et inversement     |
| reset()        |     Réinitialiser le signal      |


## Haptic Device
Un HapticDevice représente le dispositif haptique dans l'environnement virtuel (vibreur), un dispositif haptique prend une source haptique en paramètre pour connaitre le signal qu'il doit jouer. La lecture du signal est cadencée par l'HapticManager (par défaut, à la fréquence de 4kHz). La source peut être mise directement dans l'éditeur ou via script grâce à la méthode publique ```setSource(HapticSource)```. Deux HapticDevices peuvent avoir la même source, ils recevront le même signal. Chaque HapticDevice a un volume propre, il permet de limiter le volume qui sera envoyé au contrôleur (actuellement les vibreur support un emplitude max de 0.45). Le paramètre *Active* permet d'activer ou désactiver le Device, le signal retourné sera nul si desactivé.

![HapticDevice Inspector](Ressources\HapticDevice.PNG "HapticDevice Inspector")

## Haptic Grip Device
Un HapticDevice représente le dispositif haptique de type serrage dans l'environnement virtuel. Ils prennent également une source haptique en paramètre, mais ont également ce type de contrôle : SPEED ou FORCE. La lecture du signal est cadencée par l'HapticManager (à la fréquence de 500Hz). La source peut être mise directement dans l'éditeur ou via script grâce à la méthode publique ```setSource(HapticSource, gripCtrl)```. Deux HapticGripDevices peuvent avoir la même source, ils recevront le même signal.

![HapticGripDevice Inspector](Ressources\HapticGripDevice.PNG "HapticGripDevice Inspector")


## Haptic Manager

L'HapticManager contrôle l'ensemble des dispositifs haptiques. Il se charge également de récupérer les signaux auprès des HapticDevices et HapticGripDevice et de cadencer ces signaux (chaque type de device à sa propre fréquence/paramètres) pour les envoyer au contrôleur COM Série. Les signaux récupérés de chaque source contenue dans les HapticDevice sont convertis en PWM centré [0,511] et pour les HapticGripDevice entre [0,191] ou [0,255] suivant le mode (Speed ou force). Le manager peut avoir jusqu'a 28 HapticDevices.
Il est possible de charger les paramètres grâce à un fichier de configuration (json), voir le template à la fin du document.

![HapticManager Inspector](Ressources\HapticManager.PNG "HapticManager Inspector")

**Nb.** Certains vibreurs ne sont pas fait pour avoir des signaux continus (ou non centré sur 0), pour des raisons de sécurité l'HapticManager possède un mécanisme de surveillance des signaux (*pwmWatcher*), si un signal PWM envoyé au contrôleur n'a pas une moyenne de 0 pendant un certain temps, une alerte est declenchée et le HapticDevice correspondant est desactivé pour des raison de sécurité. 

*L'HapticManger ainsi que le SerialCOMManager (qui gère la communication série) sont des singletons et sont automatiquement créés si appelés par un autre script.*

## Exemple d'utilisation
Cette partie présente des exemples d'utilisation qui ont été développés pour montrer comment le système haptique peut être utilisé.


### Visualisation des signaux
Un script a été développé pour visualiser les signaux de sortie des générateurs (Haptic Clip). Il permet en mode éditeur de visualiser un Haptic Clip. Pour cela, il suffit d'ajouter le script ```EditorSamplesVisualizer``` dans la scène, de définir le signal qu'on veut voir et de le générer depuis les options.


![Editor Samples Visualizer in Inspector](Ressources\EditorSamplesVisualizer.PNG "Editor Samples Visualizer Inspector")
![Editor Samples Visualizer in Inspector](Ressources\EditorSamplesVisualizerOption.PNG "Editor Samples Visualizer Inspector")

On peut jouer sur les paramètres suivants :

| Paramètres       |     Description     |
| :------------ | :-------------: |
| Haptic Clip Prefab       |     Haptic Clip à visualiser     |
| Duration     |   Durée du signal affiché    |
| Frequency        |     Fréquence de génération du signal (default 10kHz)      |
| Precision        |     Précision de la courbe générée [0,1]     |
| Loop        |     Signal bouclé      |
| Output Curve        |     Sortie : Courbe de visualisation du signal      |

### Exemple 1 : Zone de déclenchement
Le script ```HapticZoneTrigger``` a été développé pour montrer un cas d'utilisation du système. Le script doit être mis sur un GameObject contenant un collider en mode trigger. Il faut renseigner la source qui sera affectée aux HapticDevices lorsqu'il rentre dans la zone du trigger. Si le mode de détection est : *By Tag*, il faut renseigner le tag des objets détectés dans le trigger dans lequel les HapticDevice seront cherchés, sinon avec *By HapticDevice* le script ```HapticZoneTrigger``` cherchera dans l'objet détecté (et ses descendants) s'il contient un script HapticDevice.

Les HapticDevices sont définis sous forme d'un GameObject avec un collider et un rigidbody pour être détecté par le trigger et doivent avoir le tag même défini dans le ```HapticZoneTrigger``` (obligatoire seulement si l’option de détection est : *By Tag*). Lorsqu'il entre dans le trigger la source est attribuée à l'HapticDevice.

![GameObject for the HapticZoneTrigger example](Ressources\HapticZoneTrigger.PNG "GameObject for the HapticZoneTrigger example")
![GameObject for the HapticZoneTrigger example](Ressources\DetectedObjectExample1.PNG "GameObject for the HapticZoneTrigger example")

![View scene example 1](Ressources\SceneExample1.PNG "View scene example 1")

Lorsque l'un des HapticDevice entre dans la zone de déclenchement, la source haptique est attribuée à ce device. 

### Exemple 2 : Collision
Le script ```HapticColliderTrigger``` a été développé pour montrer un cas d'utilisation du système. Le script doit être mis sur un GameObject avec un collider et un rigidbody. Si le mode de détection est : *By Tag*, il faut renseigner le tag des objets détectés lors des collision dans lequel les HapticDevice seront cherchés, sinon avec *By HapticDevice* le script ```HapticColliderTrigger``` cherchera dans l'objet détecté (et ses descendants) s'il contient un script HapticDevice.

Les HapticDevices sont définis sous forme d'un GameObject avec un collider et un rigidbody pour être détecté lors des collisions et doivent avoir le tag même défini dans le ```HapticColliderTrigger``` (obligatoire seulement si l’option de détection est : *By Tag*). Lorsqu'un contact est détecté, la source est attribuée à l'HapticDevice. Il est possible de moduler le volume de la source en fonction de la magnitude de la vitesse résultante de contact (fonctionnalité experimental).

![GameObject for the HapticColliderTrigger example](Ressources\HapticColliderTrigger.PNG "GameObject for the HapticColliderTrigger example")


### Exemple 3 : Collision avec funneling

Le script ```TouchSensationTest``` a été développé pour montrer un cas d'utilisation du système. Le script doit être mis sur un GameObject avec un collider et un rigidbody. Le script prend le tag des objets qui peuvent être détectés pour le contact, ainsi que deux HapticDevices et pour chacun la source qui doit être attribuée. 
Lorsqu'un GameObject (avec collider et rigidbody avec le même tag que défini dans ```TouchSensationTest```), entre en contact avec l'autre objet, il calcule l'amplitude de vibration qui devra être en fonction de la position du contact par rapport à la position des dispositifs. Le script joue sur le volume de la source en fonction de la position du contact par rapport aux deux devices en mettant en place un algorithme de funneling simple. Si le contact est entre les deux devices l'amplitude de vibration est de 0.5, plus il se rapproche d'un device plus l’amplitude augmenter.

![GameObject for the TouchSensationTest example](Ressources\TouchSensationTest.PNG "GameObject for the TouchSensationTest example")

Les images suivantes montrent l'amplitude des sources attribuées aux devices (représenté par la dimension de sphère bleu et verte) suivant la position de l'objet en contact (capsule).

![View scene example 3](Ressources\SceneExample3-2.PNG "View scene example 3")
![View scene example 3](Ressources\SceneExample3-1.PNG "View scene example 3")
![View scene example 3](Ressources\SceneExample3-3.PNG "View scene example 3")


### Exemple 4 : Serrage Haptic Grip


Le script ```GripTest``` a été développé pour montrer un cas d'utilisation du système de serrage. Le script doit être mis sur un GameObject. Le script prend la deux objets qui viennent écraser une troisième centrale (purement visuel), ainsi que d'un HapticGripDevice et d'une source avec un HapticClip de type générateur constant. 
Lorsque les deux objets sont en contact, puis écrasent l'objet central, la vitesse de serrage puis la force sont appliquées à l'HapticGripDevice. Le script GripTest vient directement modifier la valeur du clipHaptic en fonction si l'objet central est écrasé ou non.

![GameObject for the GripTest example](Ressources\SceneExample4-3.PNG "GameObject for the GripTest example")

Suivant le serrage de l'objet central, le retour de serrage sera activé.

![View scene example 4](Ressources\SceneExample4-1.PNG "View scene example 4")
![View scene example 4](Ressources\SceneExample4-2.PNG "View scene example 4")


## Placement des Haptic Devices avec OVR Hand
Il y a deux possibilités pour placer des ```HapticDevice``` sur les mains gérées par l'Oculus :
* Placer directement un GameObject dans le squelette de la main défini par Oculus. Le gameobjet devra avoir un collider et un rigidbody *kinematic* pour être détecté par les scripts d'exemple (ex : ```HapticColliderTrigger``` ou ```HapticZoneTrigger```).
![Placement des HapticDevices dans la hiérarchie des mains définies par Oculus](Ressources\OculusHandPlacement.PNG "Placement des HapticDevices dans la hiérarchie des mains définies par Oculus")
![GameObject de l'Haptic Device](Ressources\HapticDeviceGameObject.PNG "GameObject de l'Haptic Device")
* Placer l'hapticDevice directement sur le RigidBody/Collider crée par Oculus (il faut avoir activé l'option *Enable Physics Capsules* dans le script ```OVRCustomSkeleton```). Les RigidBody/Collider ne sont créés qu’au runtime, pour facilité le script ```AutoAttachHapticDevice``` qui permet d'attacher des Haptic Devices a un des RigidBody/Collider de la main générée. Le Haptic Device sera rattaché (déplacé dans la hiérarchie en tant que fils de la capsule choisie). *Pour fonctionner avec les exemples précédents, il faut mettre : By HapticDevice comme système de détection dans les scripts : ```HapticColliderTrigger``` ou ```HapticZoneTrigger```*
![Script pour attacher les HapticDevice aux capsules de la main Oculus.](Ressources\AutoAttachHapticDevice.PNG "Script pour attacher les HapticDevice aux capsules de la main Oculus")
![Position des capsules de la main d'Oculus](Ressources\OVRHandCollider.PNG "Position des capsules de la main d'Oculus").

OVRCustomSkeleton.PNG


## Fichier de configuration
Le fichier de configuration est lu par l'```HapticManager``` afin de récupérer les paramètres. Le fichier est au format JSON. Il est découpé en plusieurs sections, chaque session comprend un paramètre booléen ``` overwrite_data ``` qui définit si les autres paramètres doivent être pris pris en compte lors de la lecture.

```json
{
    "global_setting":
    {
        "debug":false,
		"use_udp":false,
    },
    "vibrotactile_device_setting":
    {
        "overwrite_data":true,
		"device_name":"VibrotactileDevice",
        "host_port":"COM6",
        "host_speed":115200,
		"auto_start":true,
		"frequency":4000,
		"nb_channel":28,
		"max_pwm": 511,
    },
	 "grip_device_setting":
    {
        "overwrite_data":false,
		"device_name":"GripDevice",
        "host_port":"COM4",
        "host_speed":115200,
		"auto_start":false,
		"frequency":500,
		"max_speed":191,
		"max_force":255,
    },
}
```