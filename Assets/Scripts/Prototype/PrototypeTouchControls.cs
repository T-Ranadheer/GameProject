using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public sealed class PrototypeTouchButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    public PrototypeTouchCommand Command;

    public void OnPointerDown(PointerEventData eventData)
    {
        PrototypeInput.SetTouchState(Command, true);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        PrototypeInput.SetTouchState(Command, false);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        PrototypeInput.SetTouchState(Command, false);
    }

    private void OnDisable()
    {
        PrototypeInput.SetTouchState(Command, false);
    }
}

public sealed class PrototypeTouchControls : MonoBehaviour
{
    private Button attackButton;
    private Button meleeButton;
    private Button flightButton;
    private Button transformButton;
    private Button deployButton;
    private Button brakeButton;
    private Button ability2Button;
    private Button ability3Button;
    private Button ability4Button;
    private Button switchButton;
    private Button secondaryButton;
    private Button ascendButton;
    private Button descendButton;

    public static PrototypeTouchControls Create()
    {
        var eventSystem = Object.FindFirstObjectByType<EventSystem>();
        if (eventSystem == null)
        {
            var eventSystemObject = new GameObject("EventSystem");
            eventSystem = eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<InputSystemUIInputModule>();
        }
        else if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
        {
            eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
        }

        var root = new GameObject("MobileTouchControls");
        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;
        var scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        root.AddComponent<GraphicRaycaster>();
        var controls = root.AddComponent<PrototypeTouchControls>();
        controls.Build(canvas.transform);
        return controls;
    }

    private void Build(Transform canvas)
    {
        var group = gameObject.AddComponent<CanvasGroup>();
        group.alpha = Application.isMobilePlatform ? 0.84f : 0.16f;
        group.interactable = Application.isMobilePlatform;
        group.blocksRaycasts = Application.isMobilePlatform;

        CreateButton(canvas, "MoveLeft", PrototypeTouchCommand.MoveLeft, new Vector2(110f, 105f), new Vector2(80f, 80f), "◀");
        CreateButton(canvas, "MoveRight", PrototypeTouchCommand.MoveRight, new Vector2(300f, 105f), new Vector2(80f, 80f), "▶");
        CreateButton(canvas, "MoveForward", PrototypeTouchCommand.MoveForward, new Vector2(205f, 175f), new Vector2(80f, 80f), "▲");
        CreateButton(canvas, "MoveBack", PrototypeTouchCommand.MoveBack, new Vector2(205f, 35f), new Vector2(80f, 80f), "▼");
        CreateButton(canvas, "Jump", PrototypeTouchCommand.Jump, new Vector2(-110f, 120f), new Vector2(105f, 70f), "JUMP");
        flightButton = CreateButton(canvas, "Flight", PrototypeTouchCommand.ToggleFlight, new Vector2(-110f, 35f), new Vector2(105f, 70f), "FLY");
        attackButton = CreateButton(canvas, "Attack", PrototypeTouchCommand.Attack, new Vector2(-235f, 75f), new Vector2(105f, 105f), "BOLT");
        meleeButton = CreateButton(canvas, "RobotMelee", PrototypeTouchCommand.RobotMelee, new Vector2(-355f, 75f), new Vector2(105f, 105f), "MELEE");
        transformButton = CreateButton(canvas, "Transform", PrototypeTouchCommand.Transform, new Vector2(-110f, 205f), new Vector2(105f, 70f), "SHIFT");
        deployButton = CreateButton(canvas, "DeployRobot", PrototypeTouchCommand.DeployRobot, new Vector2(80f, -95f), new Vector2(120f, 48f), "ROBOT");
        brakeButton = CreateButton(canvas, "Brake", PrototypeTouchCommand.Brake, new Vector2(-110f, 290f), new Vector2(105f, 70f), "BRAKE");
        ability2Button = CreateButton(canvas, "Ability2", PrototypeTouchCommand.Ability2, new Vector2(-235f, 205f), new Vector2(105f, 70f), "A2");
        ability3Button = CreateButton(canvas, "Ability3", PrototypeTouchCommand.Ability3, new Vector2(-355f, 205f), new Vector2(105f, 70f), "A3");
        ability4Button = CreateButton(canvas, "Ability4", PrototypeTouchCommand.Ability4, new Vector2(-235f, 290f), new Vector2(105f, 70f), "A4");
        secondaryButton = CreateButton(canvas, "SecondaryAttack", PrototypeTouchCommand.SecondaryAttack, new Vector2(-355f, 290f), new Vector2(105f, 70f), "ALT");
        ascendButton = CreateButton(canvas, "VehicleAscend", PrototypeTouchCommand.VehicleAscend, new Vector2(330f, 225f), new Vector2(105f, 70f), "UP");
        descendButton = CreateButton(canvas, "VehicleDescend", PrototypeTouchCommand.VehicleDescend, new Vector2(330f, 140f), new Vector2(105f, 70f), "DOWN");
        switchButton = CreateButton(canvas, "SwitchCharacter", PrototypeTouchCommand.SwitchCharacter, new Vector2(205f, -95f), new Vector2(120f, 48f), "ROSTER");
        CreateButton(canvas, "Pause", PrototypeTouchCommand.Pause, new Vector2(75f, -35f), new Vector2(90f, 48f), "PAUSE");
    }

    private void Update()
    {
        var manager = PrototypeGameManager.Instance;
        if (manager == null)
        {
            return;
        }

        var atlas = manager.ActiveCharacterId == PrototypeCharacterId.AtlasRunner;
        var transformer = manager.ActiveTransformer;
        var deployedTransformer = transformer != null || atlas && manager.Robot != null && manager.Robot.IsDeployed;
        var original = manager.ActiveCharacterId == PrototypeCharacterId.OriginalHero;
        var electric = manager.ActiveCharacterId == PrototypeCharacterId.ElectricHero;
        var giant = manager.ActiveCharacterId == PrototypeCharacterId.StoneGiant;
        SetButtonContext(flightButton, original, "FLY");
        SetButtonContext(attackButton, original || electric || atlas || transformer != null, electric ? "BOLT" : "PRIMARY");
        SetButtonContext(meleeButton, electric || giant || atlas && !deployedTransformer || transformer != null && transformer.IsRobotForm, transformer != null && transformer.IsRobotForm ? "MELEE" : atlas ? "MELEE" : giant ? "PUNCH" : "SHOCK");
        SetButtonContext(transformButton, deployedTransformer, "SHIFT");
        SetButtonContext(deployButton, !deployedTransformer, "ROBOT");
        SetButtonContext(brakeButton, atlas || transformer != null, "BRAKE");
        SetButtonContext(ability2Button, electric || giant, electric ? "STRIKE" : "SLAM");
        SetButtonContext(ability3Button, electric || giant, electric ? "CHAIN" : "WAVE");
        SetButtonContext(ability4Button, electric || giant, electric ? "BURST" : "THROW");
        SetButtonContext(secondaryButton, transformer != null && transformer.IsVehicleForm, "SECOND");
        var aerial = transformer != null && transformer.IsVehicleForm && (transformer.VehicleForm == PrototypeRobotForm.Helicopter || transformer.VehicleForm == PrototypeRobotForm.Jet);
        SetButtonContext(ascendButton, aerial, "UP");
        SetButtonContext(descendButton, aerial, "DOWN");
        SetButtonContext(switchButton, !deployedTransformer, "ROSTER");
    }

    private static void SetButtonContext(Button button, bool active, string label)
    {
        if (button == null)
        {
            return;
        }

        button.gameObject.SetActive(active);
        var text = button.GetComponentInChildren<Text>();
        if (text != null)
        {
            text.text = label;
        }
    }

    private static Button CreateButton(Transform canvas, string name, PrototypeTouchCommand command, Vector2 anchoredPosition, Vector2 size, string label)
    {
        var buttonObject = new GameObject(name);
        buttonObject.transform.SetParent(canvas, false);
        var rect = buttonObject.AddComponent<RectTransform>();
        var anchor = command == PrototypeTouchCommand.Pause || command == PrototypeTouchCommand.DeployRobot || command == PrototypeTouchCommand.SwitchCharacter
            ? new Vector2(0f, 1f)
            : command == PrototypeTouchCommand.MoveLeft || command == PrototypeTouchCommand.MoveRight || command == PrototypeTouchCommand.MoveForward || command == PrototypeTouchCommand.MoveBack
                ? new Vector2(0f, 0f)
                : new Vector2(1f, 0f);
        rect.anchorMin = anchor;
        rect.anchorMax = rect.anchorMin;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        var image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.05f, 0.1f, 0.18f, 0.85f);
        var button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        var touchButton = buttonObject.AddComponent<PrototypeTouchButton>();
        touchButton.Command = command;

        var textObject = new GameObject("Label");
        textObject.transform.SetParent(buttonObject.transform, false);
        var text = textObject.AddComponent<Text>();
        text.text = label;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.fontSize = 18;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var textRect = text.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        return button;
    }
}
