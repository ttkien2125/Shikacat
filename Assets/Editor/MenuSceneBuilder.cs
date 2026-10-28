using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class MenuSceneBuilder
{
    private static readonly string MenuScenePath = $"Assets/Scenes/{GameConstants.Scenes.Menu}.unity";
    private static readonly string GameplayScenePath = $"Assets/Scenes/{GameConstants.Scenes.Gameplay}.unity";
    private const string FredokaFontPath = "Assets/Resources/Fredoka-Bold SDF.asset";
    private const string FallbackFontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";

    [MenuItem("Tools/Shikacat/Build Menu Scene", priority = 10)]
    public static void BuildMenuScene()
    {
        Debug.Log("[MenuSceneBuilder] Starting Main Menu scene construction...");

        if (!Directory.Exists("Assets/Scenes"))
        {
            Directory.CreateDirectory("Assets/Scenes");
        }

        // 1. Create a fresh empty scene
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Load Font Asset
        TMP_FontAsset tmpFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FredokaFontPath)
            ?? AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FallbackFontPath);

        // 2. Main Camera
        GameObject camObj = new GameObject("Main Camera");
        Camera cam = camObj.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(1f, 0.973f, 0.929f, 1f); // #FFF8ED Warm Cream
        cam.orthographic = true;
        cam.orthographicSize = 9.6f;
        cam.nearClipPlane = 0.3f;
        cam.farClipPlane = 1000f;
        cam.depth = -1;
        camObj.tag = "MainCamera";
        camObj.AddComponent<AudioListener>();

        // 3. EventSystem
        GameObject eventObj = new GameObject("EventSystem");
        eventObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
        eventObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

        // 4. AudioManager
        GameObject audioObj = new GameObject("AudioManager");
        AudioManager audioMgr = audioObj.AddComponent<AudioManager>();
        SetupAudioManagerClips(audioMgr);

        // 5. Canvas Root
        GameObject canvasObj = new GameObject("MenuCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 0;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0f; // Match width for mobile portrait
        scaler.referencePixelsPerUnit = 100f;

        canvasObj.AddComponent<GraphicRaycaster>();
        MainMenuManager menuMgr = canvasObj.AddComponent<MainMenuManager>();

        // 6. Fullscreen Background
        GameObject bgObj = MenuUIFactory.CreateUIElement("Background", canvasObj.transform);
        MenuUIFactory.StretchFull(bgObj.GetComponent<RectTransform>());
        Image bgImg = bgObj.AddComponent<Image>();
        bgImg.color = new Color(1f, 0.973f, 0.929f, 1f); // #FFF8ED
        bgImg.raycastTarget = false;

        // 7. Ambient Decorations
        GameObject ambientGroup = MenuUIFactory.CreateUIElement("AmbientDecorations", canvasObj.transform);
        MenuUIFactory.StretchFull(ambientGroup.GetComponent<RectTransform>());

        Sprite pawClusterSpr = MenuUIFactory.LoadSprite("VFX_PawPrints_Cluster");
        Sprite sparklesSpr = MenuUIFactory.LoadSprite("VFX_Sparkles_Cluster");

        if (pawClusterSpr != null)
        {
            var pawTL = MenuUIFactory.CreateDecorImage("PawCluster_TL", ambientGroup.transform, pawClusterSpr, new Vector2(-360f, 680f), new Vector2(280f, 280f), new Color(0.85f, 0.73f, 0.65f, 0.22f));
            pawTL.transform.localRotation = Quaternion.Euler(0f, 0f, 15f);

            var pawBR = MenuUIFactory.CreateDecorImage("PawCluster_BR", ambientGroup.transform, pawClusterSpr, new Vector2(370f, -620f), new Vector2(300f, 300f), new Color(0.85f, 0.73f, 0.65f, 0.20f));
            pawBR.transform.localRotation = Quaternion.Euler(0f, 0f, 40f);
        }

        if (sparklesSpr != null)
        {
            MenuUIFactory.CreateDecorImage("Sparkles_TR", ambientGroup.transform, sparklesSpr, new Vector2(390f, 540f), new Vector2(150f, 150f), new Color(1f, 0.85f, 0.45f, 0.45f));
            MenuUIFactory.CreateDecorImage("Sparkles_BL", ambientGroup.transform, sparklesSpr, new Vector2(-390f, -440f), new Vector2(140f, 140f), new Color(1f, 0.85f, 0.45f, 0.35f));
        }

        // 8. Top HUD (Anchored Top)
        GameObject topHudObj = MenuUIFactory.CreateUIElement("TopHUD", canvasObj.transform);
        RectTransform topHudRt = topHudObj.GetComponent<RectTransform>();
        topHudRt.anchorMin = new Vector2(0.5f, 1f);
        topHudRt.anchorMax = new Vector2(0.5f, 1f);
        topHudRt.pivot = new Vector2(0.5f, 1f);
        topHudRt.anchoredPosition = new Vector2(0f, -60f);
        topHudRt.sizeDelta = new Vector2(1000f, 130f);

        // Hearts Display
        Sprite heartBarSpr = MenuUIFactory.LoadSprite("HUD_Heart_Bar");
        GameObject heartBarObj = MenuUIFactory.CreateUIElement("HUD_Heart_Bar", topHudObj.transform);
        RectTransform heartBarRt = heartBarObj.GetComponent<RectTransform>();
        heartBarRt.anchoredPosition = new Vector2(-340f, 1f);
        heartBarRt.sizeDelta = new Vector2(212f, 84f);
        Image heartBarImg = heartBarObj.AddComponent<Image>();
        heartBarImg.sprite = heartBarSpr;
        heartBarImg.preserveAspect = true;

        GameObject heartsTextObj = MenuUIFactory.CreateUIElement("HeartsText", heartBarObj.transform);
        RectTransform heartsTextRt = heartsTextObj.GetComponent<RectTransform>();
        heartsTextRt.anchorMin = new Vector2(0.5f, 0.5f);
        heartsTextRt.anchorMax = new Vector2(0.5f, 0.5f);
        heartsTextRt.pivot = new Vector2(0.5f, 0.5f);
        heartsTextRt.anchoredPosition = new Vector2(64f, -1f);
        heartsTextRt.sizeDelta = new Vector2(60f, 44f);
        TextMeshProUGUI heartsTmp = heartsTextObj.AddComponent<TextMeshProUGUI>();
        MenuUIFactory.SetupTMP(heartsTmp, tmpFont, GameConstants.Defaults.Hearts, 28, TextAlignmentOptions.Center, new Color32(92, 54, 39, 255));

        // Coins Display
        Sprite coinCounterSpr = MenuUIFactory.LoadSprite("HUD_Coin_Counter");
        GameObject coinCounterObj = MenuUIFactory.CreateUIElement("HUD_Coin_Counter", topHudObj.transform);
        RectTransform coinCounterRt = coinCounterObj.GetComponent<RectTransform>();
        coinCounterRt.anchoredPosition = new Vector2(-118f, 0f);
        coinCounterRt.sizeDelta = new Vector2(184f, 84f);
        Image coinCounterImg = coinCounterObj.AddComponent<Image>();
        coinCounterImg.sprite = coinCounterSpr;
        coinCounterImg.preserveAspect = true;

        GameObject coinsTextObj = MenuUIFactory.CreateUIElement("CoinsText", coinCounterObj.transform);
        RectTransform coinsTextRt = coinsTextObj.GetComponent<RectTransform>();
        coinsTextRt.anchorMin = new Vector2(0.5f, 0.5f);
        coinsTextRt.anchorMax = new Vector2(0.5f, 0.5f);
        coinsTextRt.pivot = new Vector2(0.5f, 0.5f);
        coinsTextRt.anchoredPosition = new Vector2(32f, 0f);
        coinsTextRt.sizeDelta = new Vector2(90f, 44f);
        TextMeshProUGUI coinsTmp = coinsTextObj.AddComponent<TextMeshProUGUI>();
        MenuUIFactory.SetupTMP(coinsTmp, tmpFont, GameConstants.Defaults.StartingCoins.ToString(), 28, TextAlignmentOptions.Center, new Color32(92, 54, 39, 255));

        // Top-Right Utility: Sound Toggle Button
        Sprite soundSpr = MenuUIFactory.LoadSprite("Btn_Sound");
        Button soundBtn = MenuUIFactory.CreateImageButton("Btn_Sound", topHudObj.transform, soundSpr, new Vector2(290f, 0f), new Vector2(110f, 115f));

        // Top-Right Utility: NoAds Button
        Sprite noAdsSpr = MenuUIFactory.LoadSprite("Btn_NoAds");
        Button noAdsBtn = MenuUIFactory.CreateImageButton("Btn_NoAds", topHudObj.transform, noAdsSpr, new Vector2(420f, 0f), new Vector2(110f, 115f));

        // 9. Logo Section (Branding)
        GameObject logoGroup = MenuUIFactory.CreateUIElement("LogoGroup", canvasObj.transform);
        RectTransform logoGroupRt = logoGroup.GetComponent<RectTransform>();
        logoGroupRt.anchoredPosition = new Vector2(0f, 490f);
        logoGroupRt.sizeDelta = new Vector2(700f, 260f);

        Sprite starGlowSpr = MenuUIFactory.LoadSprite("VFX_Star_Glow_Large");
        if (starGlowSpr != null)
        {
            MenuUIFactory.CreateDecorImage("LogoGlow", logoGroup.transform, starGlowSpr, new Vector2(0f, 10f), new Vector2(500f, 500f), new Color(1f, 0.88f, 0.55f, 0.35f));
        }

        Sprite logoSpr = MenuUIFactory.LoadSprite("Logo_Shikacat_Full");
        GameObject logoImgObj = MenuUIFactory.CreateUIElement("LogoImage", logoGroup.transform);
        RectTransform logoImgRt = logoImgObj.GetComponent<RectTransform>();
        logoImgRt.sizeDelta = new Vector2(660f, 250f);
        Image logoImg = logoImgObj.AddComponent<Image>();
        logoImg.sprite = logoSpr;
        logoImg.preserveAspect = true;

        Sprite starBurstSpr = MenuUIFactory.LoadSprite("VFX_StarBurst_Small");
        if (starBurstSpr != null)
        {
            MenuUIFactory.CreateDecorImage("LogoSparkle", logoGroup.transform, starBurstSpr, new Vector2(285f, 80f), new Vector2(80f, 80f), new Color(1f, 0.95f, 0.7f, 0.95f));
        }

        // 10. Center Mascot & Progress Card
        GameObject mascotPanelObj = MenuUIFactory.CreateUIElement("MascotPanel", canvasObj.transform);
        RectTransform mascotPanelRt = mascotPanelObj.GetComponent<RectTransform>();
        mascotPanelRt.anchoredPosition = new Vector2(0f, 110f);
        mascotPanelRt.sizeDelta = new Vector2(860f, 500f);

        Sprite panelBgSpr = MenuUIFactory.LoadSprite("Panel_Main_PawBg");
        Image panelBgImg = mascotPanelObj.AddComponent<Image>();
        panelBgImg.sprite = panelBgSpr;
        panelBgImg.type = Image.Type.Sliced;
        panelBgImg.color = Color.white;

        // Ribbon Badge
        Sprite ribbonSpr = MenuUIFactory.LoadSprite("Badge_Ribbon_New");
        if (ribbonSpr != null)
        {
            MenuUIFactory.CreateDecorImage("RibbonBadge", mascotPanelObj.transform, ribbonSpr, new Vector2(-360f, 180f), new Vector2(130f, 130f), Color.white);
        }

        // Mascot Burst Halo
        Sprite pawBurstSpr = MenuUIFactory.LoadSprite("VFX_PawBurst_Circle");
        if (pawBurstSpr != null)
        {
            MenuUIFactory.CreateDecorImage("MascotBurstHalo", mascotPanelObj.transform, pawBurstSpr, new Vector2(0f, 75f), new Vector2(360f, 360f), new Color(1f, 0.92f, 0.78f, 0.40f));
        }

        // Mascot Icon
        Sprite mascotSpr = MenuUIFactory.LoadSprite("App_Icon_Mascot");
        GameObject mascotObj = MenuUIFactory.CreateUIElement("App_Icon_Mascot", mascotPanelObj.transform);
        RectTransform mascotRt = mascotObj.GetComponent<RectTransform>();
        mascotRt.anchoredPosition = new Vector2(0f, 75f);
        mascotRt.sizeDelta = new Vector2(260f, 265f);
        Image mascotImg = mascotObj.AddComponent<Image>();
        mascotImg.sprite = mascotSpr;
        mascotImg.preserveAspect = true;

        // Current Level Text
        GameObject levelTextObj = MenuUIFactory.CreateUIElement("CurrentLevelText", mascotPanelObj.transform);
        RectTransform levelTextRt = levelTextObj.GetComponent<RectTransform>();
        levelTextRt.anchoredPosition = new Vector2(0f, -90f);
        levelTextRt.sizeDelta = new Vector2(500f, 55f);
        TextMeshProUGUI levelTmp = levelTextObj.AddComponent<TextMeshProUGUI>();
        MenuUIFactory.SetupTMP(levelTmp, tmpFont, "LEVEL 1", 44, TextAlignmentOptions.Center, new Color32(92, 54, 39, 255));

        // Stars Group
        Sprite starsSpr = MenuUIFactory.LoadSprite("UI_Stars_Group_3of5");
        if (starsSpr != null)
        {
            MenuUIFactory.CreateDecorImage("StarsRating", mascotPanelObj.transform, starsSpr, new Vector2(0f, -150f), new Vector2(240f, 55f), Color.white);
        }

        // Progress Bar
        Sprite progBarSpr = MenuUIFactory.LoadSprite("UI_ProgressBar_Paw");
        GameObject progBarObj = MenuUIFactory.CreateUIElement("ProgressBar", mascotPanelObj.transform);
        RectTransform progBarRt = progBarObj.GetComponent<RectTransform>();
        progBarRt.anchoredPosition = new Vector2(0f, -210f);
        progBarRt.sizeDelta = new Vector2(460f, 75f);
        Image progBarImg = progBarObj.AddComponent<Image>();
        progBarImg.sprite = progBarSpr;
        progBarImg.preserveAspect = true;

        // 11. Action Buttons Group
        GameObject actionGroupObj = MenuUIFactory.CreateUIElement("ActionButtonsGroup", canvasObj.transform);
        RectTransform actionGroupRt = actionGroupObj.GetComponent<RectTransform>();
        actionGroupRt.anchoredPosition = Vector2.zero;
        actionGroupRt.sizeDelta = Vector2.zero;

        // Play Button (Large Central Hero CTA)
        Sprite playSpr = MenuUIFactory.LoadSprite("Btn_Play");
        GameObject playBtnObj = MenuUIFactory.CreateUIElement("Btn_Play", actionGroupObj.transform);
        RectTransform playBtnRt = playBtnObj.GetComponent<RectTransform>();
        playBtnRt.anchoredPosition = new Vector2(0f, -250f);
        playBtnRt.sizeDelta = new Vector2(480f, 190f);
        Image playBtnImg = playBtnObj.AddComponent<Image>();
        playBtnImg.sprite = playSpr;
        playBtnImg.type = Image.Type.Sliced;
        Button playBtn = playBtnObj.AddComponent<Button>();
        playBtn.targetGraphic = playBtnImg;

        // Secondary Action Row (Wide Pill Buttons: Daily Challenge, Levels, Shop)
        GameObject secRowObj = MenuUIFactory.CreateUIElement("SecondaryRow", actionGroupObj.transform);
        RectTransform secRowRt = secRowObj.GetComponent<RectTransform>();
        secRowRt.anchoredPosition = new Vector2(0f, -420f);
        secRowRt.sizeDelta = new Vector2(980f, 115f);

        Sprite dailySpr = MenuUIFactory.LoadSprite("Btn_DailyChallenge");
        GameObject dailyBtnObj = MenuUIFactory.CreateUIElement("Btn_DailyChallenge", secRowObj.transform);
        RectTransform dailyBtnRt = dailyBtnObj.GetComponent<RectTransform>();
        dailyBtnRt.anchoredPosition = new Vector2(-320f, 0f);
        dailyBtnRt.sizeDelta = new Vector2(305f, 105f);
        Image dailyBtnImg = dailyBtnObj.AddComponent<Image>();
        dailyBtnImg.sprite = dailySpr;
        dailyBtnImg.type = Image.Type.Sliced;
        Button dailyBtn = dailyBtnObj.AddComponent<Button>();
        dailyBtn.targetGraphic = dailyBtnImg;

        // Mini ribbon on Daily Challenge
        if (ribbonSpr != null)
        {
            var dailyRibbon = MenuUIFactory.CreateDecorImage("DailyRibbon", dailyBtnObj.transform, ribbonSpr, new Vector2(100f, 35f), new Vector2(75f, 75f), Color.white);
            dailyRibbon.transform.localRotation = Quaternion.Euler(0f, 0f, -12f);
        }

        Sprite levelsSpr = MenuUIFactory.LoadSprite("Btn_Levels");
        GameObject levelsBtnObj = MenuUIFactory.CreateUIElement("Btn_Levels", secRowObj.transform);
        RectTransform levelsBtnRt = levelsBtnObj.GetComponent<RectTransform>();
        levelsBtnRt.anchoredPosition = new Vector2(0f, 0f);
        levelsBtnRt.sizeDelta = new Vector2(305f, 105f);
        Image levelsBtnImg = levelsBtnObj.AddComponent<Image>();
        levelsBtnImg.sprite = levelsSpr;
        levelsBtnImg.type = Image.Type.Sliced;
        Button levelsBtn = levelsBtnObj.AddComponent<Button>();
        levelsBtn.targetGraphic = levelsBtnImg;

        Sprite shopSpr = MenuUIFactory.LoadSprite("Btn_Shop");
        GameObject shopBtnObj = MenuUIFactory.CreateUIElement("Btn_Shop", secRowObj.transform);
        RectTransform shopBtnRt = shopBtnObj.GetComponent<RectTransform>();
        shopBtnRt.anchoredPosition = new Vector2(320f, 0f);
        shopBtnRt.sizeDelta = new Vector2(305f, 105f);
        Image shopBtnImg = shopBtnObj.AddComponent<Image>();
        shopBtnImg.sprite = shopSpr;
        shopBtnImg.type = Image.Type.Sliced;
        Button shopBtn = shopBtnObj.AddComponent<Button>();
        shopBtn.targetGraphic = shopBtnImg;

        // 12. Bottom Feature Dock (Square Icon Buttons: Skins, Leaderboard, Settings)
        GameObject bottomDockObj = MenuUIFactory.CreateUIElement("BottomDock", canvasObj.transform);
        RectTransform bottomDockRt = bottomDockObj.GetComponent<RectTransform>();
        bottomDockRt.anchorMin = new Vector2(0.5f, 0f);
        bottomDockRt.anchorMax = new Vector2(0.5f, 0f);
        bottomDockRt.pivot = new Vector2(0.5f, 0f);
        bottomDockRt.anchoredPosition = new Vector2(0f, 65f);
        bottomDockRt.sizeDelta = new Vector2(860f, 160f);

        Sprite skinsSpr = MenuUIFactory.LoadSprite("Btn_Skins");
        Sprite leaderboardSpr = MenuUIFactory.LoadSprite("Btn_Leaderboard");
        Sprite settingsSpr = MenuUIFactory.LoadSprite("Btn_Settings");

        Button skinsBtn = MenuUIFactory.CreateImageButton("Btn_Skins", bottomDockObj.transform, skinsSpr, new Vector2(-280f, 0f), new Vector2(135f, 145f));
        Button leaderboardBtn = MenuUIFactory.CreateImageButton("Btn_Leaderboard", bottomDockObj.transform, leaderboardSpr, new Vector2(0f, 0f), new Vector2(145f, 145f));
        Button settingsBtn = MenuUIFactory.CreateImageButton("Btn_Settings", bottomDockObj.transform, settingsSpr, new Vector2(280f, 0f), new Vector2(140f, 145f));

        // 13. Toast Notification
        GameObject toastObj = MenuUIFactory.CreateUIElement("ToastNotification", canvasObj.transform);
        RectTransform toastRt = toastObj.GetComponent<RectTransform>();
        toastRt.anchorMin = new Vector2(0.5f, 0f);
        toastRt.anchorMax = new Vector2(0.5f, 0f);
        toastRt.pivot = new Vector2(0.5f, 0f);
        toastRt.anchoredPosition = new Vector2(0f, 260f);
        toastRt.sizeDelta = new Vector2(700f, 90f);

        CanvasGroup toastCg = toastObj.AddComponent<CanvasGroup>();
        toastCg.alpha = 0f;

        Image toastBg = toastObj.AddComponent<Image>();
        toastBg.color = new Color32(56, 34, 24, 235); // #382218

        GameObject toastTextObj = MenuUIFactory.CreateUIElement("ToastText", toastObj.transform);
        MenuUIFactory.StretchFull(toastTextObj.GetComponent<RectTransform>());
        TextMeshProUGUI toastTmp = toastTextObj.AddComponent<TextMeshProUGUI>();
        MenuUIFactory.SetupTMP(toastTmp, tmpFont, "", 30, TextAlignmentOptions.Center, new Color32(255, 248, 237, 255));

        toastObj.SetActive(false);

        // 14. Settings Popup (Instantiate unified prefab if available, else build)
        GameObject popupPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/SettingsPopup.prefab");
        SettingsPopup settingsPopup;
        if (popupPrefab != null)
        {
            GameObject popupInstance = (GameObject)PrefabUtility.InstantiatePrefab(popupPrefab, canvasObj.transform);
            popupInstance.name = "SettingsPopup";
            MenuUIFactory.StretchFull(popupInstance.GetComponent<RectTransform>());
            popupInstance.SetActive(false);
            settingsPopup = popupInstance.GetComponent<SettingsPopup>();
        }
        else
        {
            settingsPopup = MenuPopupBuilder.BuildSettingsPopup(canvasObj, tmpFont);
        }

        // 14b. Levels Popup (Instantiate unified prefab if available, else build)
        GameObject levelsPopupPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/LevelsPopup.prefab");
        LevelsPopup levelsPopup;
        if (levelsPopupPrefab != null)
        {
            GameObject popupInstance = (GameObject)PrefabUtility.InstantiatePrefab(levelsPopupPrefab, canvasObj.transform);
            popupInstance.name = "LevelsPopup";
            MenuUIFactory.StretchFull(popupInstance.GetComponent<RectTransform>());
            popupInstance.SetActive(false);
            levelsPopup = popupInstance.GetComponent<LevelsPopup>();
        }
        else
        {
            levelsPopup = MenuPopupBuilder.BuildLevelsPopup(canvasObj, tmpFont);
        }

        // 15. Serialize references into MainMenuManager
        SerializedObject soMenu = new SerializedObject(menuMgr);
        soMenu.FindProperty("_playButton").objectReferenceValue = playBtn;
        soMenu.FindProperty("_dailyChallengeButton").objectReferenceValue = dailyBtn;
        soMenu.FindProperty("_levelsButton").objectReferenceValue = levelsBtn;
        soMenu.FindProperty("_shopButton").objectReferenceValue = shopBtn;
        soMenu.FindProperty("_skinsButton").objectReferenceValue = skinsBtn;
        soMenu.FindProperty("_leaderboardButton").objectReferenceValue = leaderboardBtn;
        soMenu.FindProperty("_settingsButton").objectReferenceValue = settingsBtn;
        soMenu.FindProperty("_soundButton").objectReferenceValue = soundBtn;
        soMenu.FindProperty("_noAdsButton").objectReferenceValue = noAdsBtn;

        soMenu.FindProperty("_currentLevelText").objectReferenceValue = levelTmp;
        soMenu.FindProperty("_coinsText").objectReferenceValue = coinsTmp;
        soMenu.FindProperty("_heartsText").objectReferenceValue = heartsTmp;

        soMenu.FindProperty("_logoTransform").objectReferenceValue = logoGroupRt;
        soMenu.FindProperty("_mascotTransform").objectReferenceValue = mascotRt;
        soMenu.FindProperty("_playButtonTransform").objectReferenceValue = playBtnRt;

        soMenu.FindProperty("_settingsPopup").objectReferenceValue = settingsPopup;
        soMenu.FindProperty("_levelsPopup").objectReferenceValue = levelsPopup;

        soMenu.FindProperty("_toastObj").objectReferenceValue = toastObj;
        soMenu.FindProperty("_toastText").objectReferenceValue = toastTmp;
        soMenu.FindProperty("_toastCanvasGroup").objectReferenceValue = toastCg;
        soMenu.ApplyModifiedProperties();

        // 16. Save Scene
        EditorSceneManager.SaveScene(scene, MenuScenePath);
        Debug.Log($"[MenuSceneBuilder] Scene successfully built and saved to {MenuScenePath}!");

        // 17. Update EditorBuildSettings
        UpdateBuildSettings();
    }

    private static void SetupAudioManagerClips(AudioManager audioMgr)
    {
        SerializedObject soAudio = new SerializedObject(audioMgr);
        soAudio.FindProperty("_bgmClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Resources/Sounds/SC_BGM_01.ogg");
        soAudio.FindProperty("_buttonClickClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Resources/Sounds/SC_SFX_UI_Button_01.ogg");
        soAudio.FindProperty("_popupOpenClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Resources/Sounds/SC_SFX_UI_Popup_Open_01.ogg");
        soAudio.FindProperty("_popupCloseClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Resources/Sounds/SC_SFX_UI_Popup_Close_01.ogg");
        soAudio.FindProperty("_boxPlacedClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Resources/Sounds/SC_SFX_Gameplay_Box_Created_01.ogg");
        soAudio.FindProperty("_levelCompletedClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Resources/Sounds/SC_SFX_UI_Win_01.ogg");
        soAudio.ApplyModifiedProperties();
    }

    private static void UpdateBuildSettings()
    {
        EditorBuildSettingsScene[] scenes = new EditorBuildSettingsScene[]
        {
            new EditorBuildSettingsScene(MenuScenePath, true),
            new EditorBuildSettingsScene(GameplayScenePath, true)
        };
        EditorBuildSettings.scenes = scenes;
        Debug.Log("[MenuSceneBuilder] Updated EditorBuildSettings.scenes with Menu (0) and Gameplay (1).");
    }
}
