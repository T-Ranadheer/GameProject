using UnityEngine;

public sealed class PrototypeHud : MonoBehaviour
{
    private PrototypeGameManager gameManager;

    public void Configure(PrototypeGameManager manager)
    {
        gameManager = manager;
    }

    private void OnGUI()
    {
        if (gameManager == null || gameManager.ActiveHealth == null || gameManager.Mission == null)
        {
            return;
        }

        var scale = Mathf.Clamp(Screen.width / 1280f, 0.75f, 1.35f);
        var oldMatrix = GUI.matrix;
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one * scale);

        var health = gameManager.ActiveHealth;
        var healthPercent = health.MaxHealth > 0f ? health.CurrentHealth / health.MaxHealth : 0f;
        GUI.color = Color.white;
        GUI.Box(new Rect(22f, 22f, 430f, 170f), GUIContent.none);
        GUI.Label(new Rect(40f, 36f, 380f, 28f), "NOVA HERO // METRO BLOCK");
        GUI.Label(new Rect(40f, 64f, 380f, 22f), "CHARACTER  " + gameManager.ActiveCharacterName);
        GUI.Label(new Rect(40f, 90f, 380f, 22f), "HEALTH  " + Mathf.CeilToInt(health.CurrentHealth) + " / " + Mathf.CeilToInt(health.MaxHealth));
        GUI.color = healthPercent > 0.35f ? new Color(0.15f, 0.85f, 1f) : new Color(1f, 0.25f, 0.18f);
        GUI.DrawTexture(new Rect(40f, 118f, 370f * healthPercent, 10f), Texture2D.whiteTexture);
        GUI.color = Color.white;

        var energy = gameManager.ActiveTransform != null ? gameManager.ActiveTransform.GetComponent<PrototypeEnergy>() : null;
        if (energy != null)
        {
            var energyPercent = energy.Maximum > 0f ? energy.Current / energy.Maximum : 0f;
            GUI.Label(new Rect(40f, 132f, 380f, 22f), "ENERGY  " + Mathf.CeilToInt(energy.Current) + " / " + Mathf.CeilToInt(energy.Maximum));
            GUI.color = new Color(0.45f, 0.8f, 1f);
            GUI.DrawTexture(new Rect(40f, 158f, 370f * energyPercent, 8f), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        var robot = gameManager.Robot;
        var transformer = gameManager.ActiveTransformer;
        var mode = transformer != null
            ? transformer.CharacterId + " " + transformer.State.ToString().ToUpperInvariant()
            : gameManager.ActiveCharacterId == PrototypeCharacterId.AtlasRunner && robot != null
                ? "ATLAS " + robot.State.ToString().ToUpperInvariant()
                : gameManager.Player != null && gameManager.Player.IsFlying ? "FLIGHT" : "GROUND";
        GUI.Label(new Rect(40f, 176f, 390f, 22f), "MODE  " + mode);

        if (gameManager.ActiveCharacterId != PrototypeCharacterId.AtlasRunner)
        {
            GUI.Box(new Rect(22f, 202f, 650f, 112f), GUIContent.none);
            GUI.Label(new Rect(40f, 208f, 120f, 20f), "ROSTER");
            if (GUI.Button(new Rect(132f, 206f, 94f, 34f), "1 NOVA"))
            {
                gameManager.TrySelectCharacter(PrototypeCharacterId.OriginalHero);
            }

            if (GUI.Button(new Rect(232f, 206f, 94f, 34f), "2 VOLT"))
            {
                gameManager.TrySelectCharacter(PrototypeCharacterId.ElectricHero);
            }

            if (GUI.Button(new Rect(332f, 206f, 94f, 34f), "3 STONE"))
            {
                gameManager.TrySelectCharacter(PrototypeCharacterId.StoneGiant);
            }

            if (GUI.Button(new Rect(432f, 206f, 94f, 34f), "4 ATLAS"))
            {
                gameManager.TrySelectRobot(PrototypeCharacterId.AtlasRunner);
            }

            if (GUI.Button(new Rect(532f, 206f, 120f, 34f), "5 HELI"))
            {
                gameManager.TrySelectRobot(PrototypeCharacterId.HelicopterTransformer);
            }

            if (GUI.Button(new Rect(132f, 248f, 94f, 34f), "6 TANK"))
            {
                gameManager.TrySelectRobot(PrototypeCharacterId.TankTransformer);
            }

            if (GUI.Button(new Rect(232f, 248f, 94f, 34f), "7 BIKE"))
            {
                gameManager.TrySelectRobot(PrototypeCharacterId.MotorcycleTransformer);
            }

            if (GUI.Button(new Rect(332f, 248f, 94f, 34f), "8 JET"))
            {
                gameManager.TrySelectRobot(PrototypeCharacterId.JetTransformer);
            }
        }

        var mission = gameManager.Mission;
        GUI.Box(new Rect(Screen.width / scale - 392f, 22f, 370f, 118f), GUIContent.none);
        GUI.Label(new Rect(Screen.width / scale - 372f, 38f, 340f, 25f), "MISSION // " + mission.State.ToString().ToUpperInvariant());
        GUI.Label(new Rect(Screen.width / scale - 372f, 68f, 340f, 34f), mission.Objective);
        if (mission.State == PrototypeMissionState.ClearEnemies)
        {
            GUI.Label(new Rect(Screen.width / scale - 372f, 104f, 340f, 22f), "SENTINELS REMAINING: " + mission.RemainingEnemies);
        }

        var ai = PrototypePoliceCoordinator.Instance;
        if (ai != null)
        {
            GUI.Box(new Rect(Screen.width / scale - 392f, 148f, 370f, 92f), GUIContent.none);
            GUI.Label(new Rect(Screen.width / scale - 372f, 162f, 340f, 22f), "WANTED  " + ai.Wanted.Level.ToString().ToUpperInvariant());
            GUI.Label(new Rect(Screen.width / scale - 372f, 188f, 340f, 22f), "AI  " + ai.PursuitStatus + "  POLICE " + ai.ActivePoliceUnits);
            GUI.Label(new Rect(Screen.width / scale - 372f, 214f, 340f, 22f), "HOSTILE ROBOTS  " + ai.ActiveHostileRobots + "  [H] encounter");
            var aiMission = Object.FindFirstObjectByType<PrototypeAiEncounterMission>();
            if (aiMission != null && aiMission.IsActive)
            {
                GUI.Label(new Rect(Screen.width / scale - 372f, 236f, 340f, 22f), aiMission.Objective);
            }
        }

        var controls = gameManager.IsRobotDeployed
            ? "WASD vehicle/robot   SPACE brake/ascend   CTRL descend   T transform   E melee   LMB primary   G secondary   R return"
            : "WASD move   SHIFT sprint   SPACE jump   1/2/3 roster   TAB next   G/C/V abilities   R robot   ESC pause";
        GUI.Label(new Rect(24f, Screen.height / scale - 62f, 1120f, 24f), controls);
        GUI.Label(new Rect(Screen.width / scale - 280f, Screen.height / scale - 62f, 250f, 24f), "Touch controls available on Android");
        GUI.Label(new Rect(Screen.width / scale * 0.5f - 12f, Screen.height / scale * 0.5f - 10f, 30f, 24f), "+");

        if (gameManager.Paused)
        {
            DrawOverlay("PAUSED", "Press ESC or tap RESUME to continue", true);
        }
        else if (mission.State == PrototypeMissionState.Failed)
        {
            DrawOverlay("MISSION FAILED", mission.Objective, false);
        }
        else if (mission.State == PrototypeMissionState.Complete)
        {
            DrawOverlay("MISSION COMPLETE", mission.Objective, false);
        }

        GUI.matrix = oldMatrix;
    }

    private void DrawOverlay(string title, string subtitle, bool paused)
    {
        var centerX = Screen.width / 2f;
        var centerY = Screen.height / 2f;
        GUI.color = new Color(0f, 0f, 0f, 0.72f);
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;
        GUI.Box(new Rect(centerX - 230f, centerY - 120f, 460f, 240f), GUIContent.none);
        GUI.Label(new Rect(centerX - 190f, centerY - 80f, 380f, 38f), title);
        GUI.Label(new Rect(centerX - 190f, centerY - 35f, 380f, 42f), subtitle);
        if (paused && GUI.Button(new Rect(centerX - 180f, centerY + 25f, 160f, 45f), "RESUME"))
        {
            gameManager.Resume();
        }

        if (GUI.Button(new Rect(centerX + 20f, centerY + 25f, 160f, 45f), "RESTART"))
        {
            gameManager.RestartMission();
        }
    }
}
