using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class MenuSceneBuilder
{
    private const string MenuScenePath = "Assets/Scenes/Menu.unity";
    private const string GameplayScenePath = "Assets/Scenes/Gameplay.unity";
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
        GameObject bgObj = CreateUIElement("Background", canvasObj.transform);
        StretchFull(bgObj.GetComponent<RectTransform>());
        Image bgImg = bgObj.AddComponent<Image>();
        bgImg.color = new Color(1f, 0.973f, 0.929f, 1f); // #FFF8ED
        bgImg.raycastTarget = false;

        // 7. Ambient Decorations
        GameObject ambientGroup = CreateUIElement("AmbientDecorations", canvasObj.transform);
        StretchFull(ambientGroup.GetComponent<RectTransform>());

        Sprite pawClusterSpr = LoadSprite("VFX_PawPrints_Cluster");
        Sprite sparklesSpr = LoadSprite("VFX_Sparkles_Cluster");

        if (pawClusterSpr != null)
        {
            var pawTL = CreateDecorImage("PawCluster_TL", ambientGroup.transform, pawClusterSpr, new Vector2(-360f, 680f), new Vector2(280f, 280f), new Color(0.85f, 0.73f, 0.65f, 0.22f));
            pawTL.transform.localRotation = Quaternion.Euler(0f, 0f, 15f);

            var pawBR = CreateDecorImage("PawCluster_BR", ambientGroup.transform, pawClusterSpr, new Vector2(370f, -620f), new Vector2(300f, 300f), new Color(0.85f, 0.73f, 0.65f, 0.20f));
            pawBR.transform.localRotation = Quaternion.Euler(0f, 0f, 40f);
        }

        if (sparklesSpr != null)
        {
            CreateDecorImage("Sparkles_TR", ambientGroup.transform, sparklesSpr, new Vector2(390f, 540f), new Vector2(150f, 150f), new Color(1f, 0.85f, 0.45f, 0.45f));
            CreateDecorImage("Sparkles_BL", ambientGroup.transform, sparklesSpr, new Vector2(-390f, -440f), new Vector2(140f, 140f), new Color(1f, 0.85f, 0.45f, 0.35f));
        }

        // 8. Top HUD (Anchored Top)
        GameObject topHudObj = CreateUIElement("TopHUD", canvasObj.transform);
        RectTransform topHudRt = topHudObj.GetComponent<RectTransform>();
        topHudRt.anchorMin = new Vector2(0.5f, 1f);
        topHudRt.anchorMax = new Vector2(0.5f, 1f);
        topHudRt.pivot = new Vector2(0.5f, 1f);
        topHudRt.anchoredPosition = new Vector2(0f, -60f);
        topHudRt.sizeDelta = new Vector2(1000f, 130f);

        // Hearts Display
        Sprite heartBarSpr = LoadSprite("HUD_Heart_Bar");
        GameObject heartBarObj = CreateUIElement("HUD_Heart_Bar", topHudObj.transform);
        RectTransform heartBarRt = heartBarObj.GetComponent<RectTransform>();
        heartBarRt.anchoredPosition = new Vector2(-340f, 1f);
        heartBarRt.sizeDelta = new Vector2(212f, 84f);
        Image heartBarImg = heartBarObj.AddComponent<Image>();
        heartBarImg.sprite = heartBarSpr;
        heartBarImg.preserveAspect = true;

        GameObject heartsTextObj = CreateUIElement("HeartsText", heartBarObj.transform);
        RectTransform heartsTextRt = heartsTextObj.GetComponent<RectTransform>();
        heartsTextRt.anchorMin = new Vector2(0.5f, 0.5f);
        heartsTextRt.anchorMax = new Vector2(0.5f, 0.5f);
        heartsTextRt.pivot = new Vector2(0.5f, 0.5f);
        heartsTextRt.anchoredPosition = new Vector2(64f, -1f);
        heartsTextRt.sizeDelta = new Vector2(60f, 44f);
        TextMeshProUGUI heartsTmp = heartsTextObj.AddComponent<TextMeshProUGUI>();
        SetupTMP(heartsTmp, tmpFont, "5/5", 28, TextAlignmentOptions.Center, new Color32(92, 54, 39, 255));

        // Coins Display
        Sprite coinCounterSpr = LoadSprite("HUD_Coin_Counter");
        GameObject coinCounterObj = CreateUIElement("HUD_Coin_Counter", topHudObj.transform);
        RectTransform coinCounterRt = coinCounterObj.GetComponent<RectTransform>();
        coinCounterRt.anchoredPosition = new Vector2(-118f, 0f);
        coinCounterRt.sizeDelta = new Vector2(184f, 84f);
        Image coinCounterImg = coinCounterObj.AddComponent<Image>();
        coinCounterImg.sprite = coinCounterSpr;
        coinCounterImg.preserveAspect = true;

        GameObject coinsTextObj = CreateUIElement("CoinsText", coinCounterObj.transform);
        RectTransform coinsTextRt = coinsTextObj.GetComponent<RectTransform>();
        coinsTextRt.anchorMin = new Vector2(0.5f, 0.5f);
        coinsTextRt.anchorMax = new Vector2(0.5f, 0.5f);
        coinsTextRt.pivot = new Vector2(0.5f, 0.5f);
        coinsTextRt.anchoredPosition = new Vector2(32f, 0f);
        coinsTextRt.sizeDelta = new Vector2(90f, 44f);
        TextMeshProUGUI coinsTmp = coinsTextObj.AddComponent<TextMeshProUGUI>();
        SetupTMP(coinsTmp, tmpFont, "450", 28, TextAlignmentOptions.Center, new Color32(92, 54, 39, 255));

        // Top-Right Utility: Sound Toggle Button
        Sprite soundSpr = LoadSprite("Btn_Sound");
        Button soundBtn = CreateImageButton("Btn_Sound", topHudObj.transform, soundSpr, new Vector2(290f, 0f), new Vector2(110f, 115f));

        // Top-Right Utility: NoAds Button
        Sprite noAdsSpr = LoadSprite("Btn_NoAds");
        Button noAdsBtn = CreateImageButton("Btn_NoAds", topHudObj.transform, noAdsSpr, new Vector2(420f, 0f), new Vector2(110f, 115f));

        // 9. Logo Section (Branding)
        GameObject logoGroup = CreateUIElement("LogoGroup", canvasObj.transform);
        RectTransform logoGroupRt = logoGroup.GetComponent<RectTransform>();
        logoGroupRt.anchoredPosition = new Vector2(0f, 490f);
        logoGroupRt.sizeDelta = new Vector2(700f, 260f);

        Sprite starGlowSpr = LoadSprite("VFX_Star_Glow_Large");
        if (starGlowSpr != null)
        {
            CreateDecorImage("LogoGlow", logoGroup.transform, starGlowSpr, new Vector2(0f, 10f), new Vector2(500f, 500f), new Color(1f, 0.88f, 0.55f, 0.35f));
        }

        Sprite logoSpr = LoadSprite("Logo_Shikacat_Full");
        GameObject logoImgObj = CreateUIElement("LogoImage", logoGroup.transform);
        RectTransform logoImgRt = logoImgObj.GetComponent<RectTransform>();
        logoImgRt.sizeDelta = new Vector2(660f, 250f);
        Image logoImg = logoImgObj.AddComponent<Image>();
        logoImg.sprite = logoSpr;
        logoImg.preserveAspect = true;

        Sprite starBurstSpr = LoadSprite("VFX_StarBurst_Small");
        if (starBurstSpr != null)
        {
            CreateDecorImage("LogoSparkle", logoGroup.transform, starBurstSpr, new Vector2(285f, 80f), new Vector2(80f, 80f), new Color(1f, 0.95f, 0.7f, 0.95f));
        }

        // 10. Center Mascot & Progress Card
        GameObject mascotPanelObj = CreateUIElement("MascotPanel", canvasObj.transform);
        RectTransform mascotPanelRt = mascotPanelObj.GetComponent<RectTransform>();
        mascotPanelRt.anchoredPosition = new Vector2(0f, 110f);
        mascotPanelRt.sizeDelta = new Vector2(860f, 500f);

        Sprite panelBgSpr = LoadSprite("Panel_Main_PawBg");
        Image panelBgImg = mascotPanelObj.AddComponent<Image>();
        panelBgImg.sprite = panelBgSpr;
        panelBgImg.type = Image.Type.Sliced;
        panelBgImg.color = Color.white;

        // Ribbon Badge
        Sprite ribbonSpr = LoadSprite("Badge_Ribbon_New");
        if (ribbonSpr != null)
        {
            CreateDecorImage("RibbonBadge", mascotPanelObj.transform, ribbonSpr, new Vector2(-360f, 180f), new Vector2(130f, 130f), Color.white);
        }

        // Mascot Burst Halo
        Sprite pawBurstSpr = LoadSprite("VFX_PawBurst_Circle");
        if (pawBurstSpr != null)
        {
            CreateDecorImage("MascotBurstHalo", mascotPanelObj.transform, pawBurstSpr, new Vector2(0f, 75f), new Vector2(360f, 360f), new Color(1f, 0.92f, 0.78f, 0.40f));
        }

        // Mascot Icon
        Sprite mascotSpr = LoadSprite("App_Icon_Mascot");
        GameObject mascotObj = CreateUIElement("App_Icon_Mascot", mascotPanelObj.transform);
        RectTransform mascotRt = mascotObj.GetComponent<RectTransform>();
        mascotRt.anchoredPosition = new Vector2(0f, 75f);
        mascotRt.sizeDelta = new Vector2(260f, 265f);
        Image mascotImg = mascotObj.AddComponent<Image>();
        mascotImg.sprite = mascotSpr;
        mascotImg.preserveAspect = true;

        // Current Level Text
        GameObject levelTextObj = CreateUIElement("CurrentLevelText", mascotPanelObj.transform);
        RectTransform levelTextRt = levelTextObj.GetComponent<RectTransform>();
        levelTextRt.anchoredPosition = new Vector2(0f, -90f);
        levelTextRt.sizeDelta = new Vector2(500f, 55f);
        TextMeshProUGUI levelTmp = levelTextObj.AddComponent<TextMeshProUGUI>();
        SetupTMP(levelTmp, tmpFont, "LEVEL 1", 44, TextAlignmentOptions.Center, new Color32(92, 54, 39, 255));

        // Stars Group
        Sprite starsSpr = LoadSprite("UI_Stars_Group_3of5");
        if (starsSpr != null)
        {
            CreateDecorImage("StarsRating", mascotPanelObj.transform, starsSpr, new Vector2(0f, -150f), new Vector2(240f, 55f), Color.white);
        }

        // Progress Bar
        Sprite progBarSpr = LoadSprite("UI_ProgressBar_Paw");
        GameObject progBarObj = CreateUIElement("ProgressBar", mascotPanelObj.transform);
        RectTransform progBarRt = progBarObj.GetComponent<RectTransform>();
        progBarRt.anchoredPosition = new Vector2(0f, -210f);
        progBarRt.sizeDelta = new Vector2(460f, 75f);
        Image progBarImg = progBarObj.AddComponent<Image>();
        progBarImg.sprite = progBarSpr;
        progBarImg.preserveAspect = true;

        // 11. Action Buttons Group
        GameObject actionGroupObj = CreateUIElement("ActionButtonsGroup", canvasObj.transform);
        RectTransform actionGroupRt = actionGroupObj.GetComponent<RectTransform>();
        actionGroupRt.anchoredPosition = Vector2.zero;
        actionGroupRt.sizeDelta = Vector2.zero;

        // Play Button (Large Central Hero CTA)
        Sprite playSpr = LoadSprite("Btn_Play");
        GameObject playBtnObj = CreateUIElement("Btn_Play", actionGroupObj.transform);
        RectTransform playBtnRt = playBtnObj.GetComponent<RectTransform>();
        playBtnRt.anchoredPosition = new Vector2(0f, -250f);
        playBtnRt.sizeDelta = new Vector2(480f, 190f);
        Image playBtnImg = playBtnObj.AddComponent<Image>();
        playBtnImg.sprite = playSpr;
        playBtnImg.type = Image.Type.Sliced;
        Button playBtn = playBtnObj.AddComponent<Button>();
        playBtn.targetGraphic = playBtnImg;

        // Secondary Action Row (Wide Pill Buttons: Daily Challenge, Levels, Shop)
        GameObject secRowObj = CreateUIElement("SecondaryRow", actionGroupObj.transform);
        RectTransform secRowRt = secRowObj.GetComponent<RectTransform>();
        secRowRt.anchoredPosition = new Vector2(0f, -420f);
        secRowRt.sizeDelta = new Vector2(980f, 115f);

        Sprite dailySpr = LoadSprite("Btn_DailyChallenge");
        GameObject dailyBtnObj = CreateUIElement("Btn_DailyChallenge", secRowObj.transform);
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
            var dailyRibbon = CreateDecorImage("DailyRibbon", dailyBtnObj.transform, ribbonSpr, new Vector2(100f, 35f), new Vector2(75f, 75f), Color.white);
            dailyRibbon.transform.localRotation = Quaternion.Euler(0f, 0f, -12f);
        }

        Sprite levelsSpr = LoadSprite("Btn_Levels");
        GameObject levelsBtnObj = CreateUIElement("Btn_Levels", secRowObj.transform);
        RectTransform levelsBtnRt = levelsBtnObj.GetComponent<RectTransform>();
        levelsBtnRt.anchoredPosition = new Vector2(0f, 0f);
        levelsBtnRt.sizeDelta = new Vector2(305f, 105f);
        Image levelsBtnImg = levelsBtnObj.AddComponent<Image>();
        levelsBtnImg.sprite = levelsSpr;
        levelsBtnImg.type = Image.Type.Sliced;
        Button levelsBtn = levelsBtnObj.AddComponent<Button>();
        levelsBtn.targetGraphic = levelsBtnImg;

        Sprite shopSpr = LoadSprite("Btn_Shop");
        GameObject shopBtnObj = CreateUIElement("Btn_Shop", secRowObj.transform);
        RectTransform shopBtnRt = shopBtnObj.GetComponent<RectTransform>();
        shopBtnRt.anchoredPosition = new Vector2(320f, 0f);
        shopBtnRt.sizeDelta = new Vector2(305f, 105f);
        Image shopBtnImg = shopBtnObj.AddComponent<Image>();
        shopBtnImg.sprite = shopSpr;
        shopBtnImg.type = Image.Type.Sliced;
        Button shopBtn = shopBtnObj.AddComponent<Button>();
        shopBtn.targetGraphic = shopBtnImg;

        // 12. Bottom Feature Dock (Square Icon Buttons: Skins, Leaderboard, Settings)
        GameObject bottomDockObj = CreateUIElement("BottomDock", canvasObj.transform);
        RectTransform bottomDockRt = bottomDockObj.GetComponent<RectTransform>();
        bottomDockRt.anchorMin = new Vector2(0.5f, 0f);
        bottomDockRt.anchorMax = new Vector2(0.5f, 0f);
        bottomDockRt.pivot = new Vector2(0.5f, 0f);
        bottomDockRt.anchoredPosition = new Vector2(0f, 65f);
        bottomDockRt.sizeDelta = new Vector2(860f, 160f);

        Sprite skinsSpr = LoadSprite("Btn_Skins");
        Sprite leaderboardSpr = LoadSprite("Btn_Leaderboard");
        Sprite settingsSpr = LoadSprite("Btn_Settings");

        Button skinsBtn = CreateImageButton("Btn_Skins", bottomDockObj.transform, skinsSpr, new Vector2(-280f, 0f), new Vector2(135f, 145f));
        Button leaderboardBtn = CreateImageButton("Btn_Leaderboard", bottomDockObj.transform, leaderboardSpr, new Vector2(0f, 0f), new Vector2(145f, 145f));
        Button settingsBtn = CreateImageButton("Btn_Settings", bottomDockObj.transform, settingsSpr, new Vector2(280f, 0f), new Vector2(140f, 145f));

        // 13. Toast Notification
        GameObject toastObj = CreateUIElement("ToastNotification", canvasObj.transform);
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

        GameObject toastTextObj = CreateUIElement("ToastText", toastObj.transform);
        StretchFull(toastTextObj.GetComponent<RectTransform>());
        TextMeshProUGUI toastTmp = toastTextObj.AddComponent<TextMeshProUGUI>();
        SetupTMP(toastTmp, tmpFont, "", 30, TextAlignmentOptions.Center, new Color32(255, 248, 237, 255));

        toastObj.SetActive(false);

        // 14. Settings Popup (Instantiate unified prefab if available, else build)
        GameObject popupPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/SettingsPopup.prefab");
        SettingsPopup settingsPopup;
        if (popupPrefab != null)
        {
            GameObject popupInstance = (GameObject)PrefabUtility.InstantiatePrefab(popupPrefab, canvasObj.transform);
            popupInstance.name = "SettingsPopup";
            StretchFull(popupInstance.GetComponent<RectTransform>());
            popupInstance.SetActive(false);
            settingsPopup = popupInstance.GetComponent<SettingsPopup>();
        }
        else
        {
            settingsPopup = BuildSettingsPopup(canvasObj, tmpFont);
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

    private static Sprite LoadSprite(string spriteName)
    {
        string[] folders = { "01_Branding", "02_Action_Buttons", "03_Feature_Buttons", "04_HUD_And_Panels", "06_VFX_And_Particles", "" };
        foreach (var f in folders)
        {
            string rel = string.IsNullOrEmpty(f) ? $"{spriteName}.png" : $"{f}/{spriteName}.png";
            string path = $"Assets/Resources/Sprites/GeneratedSprites/{rel}";
            Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (s != null) return s;
        }

        // Fallback to Resources/Sprites
        Sprite fallback = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Resources/Sprites/{spriteName}.png");
        if (fallback != null) return fallback;

        // Fallback to Resources/Sprites/UI
        Sprite uiFallback = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Resources/Sprites/UI/{spriteName}.png");
        if (uiFallback != null) return uiFallback;

        Debug.LogWarning($"[MenuSceneBuilder] Could not find Sprite '{spriteName}'!");
        return null;
    }

    private static GameObject CreateUIElement(string name, Transform parent)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.layer = LayerMask.NameToLayer("UI");
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = Vector2.zero;
        return go;
    }

    private static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = Vector2.zero;
    }

    private static GameObject CreateDecorImage(string name, Transform parent, Sprite sprite, Vector2 pos, Vector2 size, Color color)
    {
        GameObject go = CreateUIElement(name, parent);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        Image img = go.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.preserveAspect = true;
        img.raycastTarget = false;
        return go;
    }

    private static Button CreateImageButton(string name, Transform parent, Sprite sprite, Vector2 pos, Vector2 size)
    {
        GameObject go = CreateUIElement(name, parent);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        Image img = go.AddComponent<Image>();
        img.sprite = sprite;
        img.preserveAspect = true;
        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        return btn;
    }

    private static void SetupTMP(TextMeshProUGUI tmp, TMP_FontAsset font, string text, float fontSize, TextAlignmentOptions align, Color color)
    {
        if (font != null) tmp.font = font;
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = align;
        tmp.color = color;
        tmp.raycastTarget = false;
    }

    private static SettingsPopup BuildSettingsPopup(GameObject canvasObj, TMP_FontAsset tmpFont)
    {
        // 1. Root Popup GameObject
        GameObject popupObj = CreateUIElement("SettingsPopup", canvasObj.transform);
        StretchFull(popupObj.GetComponent<RectTransform>());
        SettingsPopup sp = popupObj.AddComponent<SettingsPopup>();

        // 2. Backdrop (dark overlay with fade and click-to-close)
        GameObject backdropObj = CreateUIElement("Backdrop", popupObj.transform);
        StretchFull(backdropObj.GetComponent<RectTransform>());
        Image backdropImg = backdropObj.AddComponent<Image>();
        backdropImg.color = new Color(0f, 0f, 0f, 0.72f);
        CanvasGroup backdropCg = backdropObj.AddComponent<CanvasGroup>();
        Button backdropBtn = backdropObj.AddComponent<Button>();
        backdropBtn.targetGraphic = backdropImg;

        // 3. Card Panel
        Sprite cardSpr = LoadSprite("Panel_Popup_Body");
        GameObject cardObj = CreateUIElement("Card", popupObj.transform);
        RectTransform cardRt = cardObj.GetComponent<RectTransform>();
        cardRt.anchorMin = new Vector2(0.5f, 0.5f);
        cardRt.anchorMax = new Vector2(0.5f, 0.5f);
        cardRt.pivot = new Vector2(0.5f, 0.5f);
        cardRt.anchoredPosition = Vector2.zero;
        cardRt.sizeDelta = new Vector2(820f, 1040f);
        Image cardImg = cardObj.AddComponent<Image>();
        cardImg.sprite = cardSpr;
        cardImg.type = Image.Type.Sliced;

        // 4. Header Banner & Title
        Sprite headerSpr = LoadSprite("Panel_Popup_Header");
        GameObject headerObj = CreateUIElement("HeaderBanner", cardObj.transform);
        RectTransform headerRt = headerObj.GetComponent<RectTransform>();
        headerRt.anchorMin = new Vector2(0.5f, 1f);
        headerRt.anchorMax = new Vector2(0.5f, 1f);
        headerRt.pivot = new Vector2(0.5f, 1f);
        headerRt.anchoredPosition = new Vector2(0f, -20f);
        headerRt.sizeDelta = new Vector2(750f, 170f);
        Image headerImg = headerObj.AddComponent<Image>();
        headerImg.sprite = headerSpr;
        headerImg.preserveAspect = true;

        GameObject titleObj = CreateUIElement("Title", cardObj.transform);
        RectTransform titleRt = titleObj.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0.5f, 1f);
        titleRt.anchorMax = new Vector2(0.5f, 1f);
        titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.anchoredPosition = new Vector2(0f, -65f);
        titleRt.sizeDelta = new Vector2(500f, 80f);
        TextMeshProUGUI titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
        SetupTMP(titleTmp, tmpFont, "SETTINGS", 44, TextAlignmentOptions.Center, new Color32(255, 248, 237, 255));

        // 5. Close Button ('X')
        Sprite closeSpr = LoadSprite("Button_Close");
        Button closeBtn = CreateImageButton("CloseButton", cardObj.transform, closeSpr, new Vector2(-34f, -52f), new Vector2(104f, 104f));
        RectTransform closeRt = closeBtn.GetComponent<RectTransform>();
        closeRt.anchorMin = new Vector2(1f, 1f);
        closeRt.anchorMax = new Vector2(1f, 1f);
        closeRt.pivot = new Vector2(0.5f, 0.5f);

        // 6. Settings Rows: Music, Sounds, Haptics
        Sprite musicSpr = LoadSprite("Icon_Music");
        Sprite hapticsSpr = LoadSprite("Icon_Haptics");
        ToggleSwitch musicToggle = CreateSettingRow("Row_Music", cardObj.transform, "MUSIC", musicSpr, -230f, tmpFont);
        ToggleSwitch soundsToggle = CreateSettingRow("Row_Sounds", cardObj.transform, "SOUNDS", musicSpr, -340f, tmpFont);
        ToggleSwitch hapticsToggle = CreateSettingRow("Row_Haptics", cardObj.transform, "HAPTICS", hapticsSpr, -450f, tmpFont);

        // 7. Action Button: "BACK TO MENU"
        Sprite btnSecSpr = LoadSprite("Button_Secondary");
        Button backToMenuBtn = CreateImageButton("MenuButton", cardObj.transform, btnSecSpr, new Vector2(0f, 170f), new Vector2(660f, 110f));
        RectTransform backRt = backToMenuBtn.GetComponent<RectTransform>();
        backRt.anchorMin = new Vector2(0.5f, 0f);
        backRt.anchorMax = new Vector2(0.5f, 0f);
        backRt.pivot = new Vector2(0.5f, 0f);
        backToMenuBtn.image.type = Image.Type.Sliced;

        GameObject backTextObj = CreateUIElement("Text", backToMenuBtn.transform);
        StretchFull(backTextObj.GetComponent<RectTransform>());
        TextMeshProUGUI backTmp = backTextObj.AddComponent<TextMeshProUGUI>();
        SetupTMP(backTmp, tmpFont, "BACK TO MENU", 36, TextAlignmentOptions.Center, new Color32(255, 248, 237, 255));

        // 8. Footer (T&C, Privacy)
        GameObject footerObj = CreateUIElement("Footer", cardObj.transform);
        RectTransform footerRt = footerObj.GetComponent<RectTransform>();
        footerRt.anchorMin = new Vector2(0f, 0f);
        footerRt.anchorMax = new Vector2(1f, 0f);
        footerRt.pivot = new Vector2(0.5f, 0f);
        footerRt.anchoredPosition = new Vector2(0f, 35f);
        footerRt.sizeDelta = new Vector2(0f, 90f);

        GameObject termsObj = CreateUIElement("TermsButton", footerObj.transform);
        RectTransform termsRt = termsObj.GetComponent<RectTransform>();
        termsRt.anchorMin = new Vector2(0f, 0.5f);
        termsRt.anchorMax = new Vector2(0f, 0.5f);
        termsRt.pivot = new Vector2(0f, 0.5f);
        termsRt.anchoredPosition = new Vector2(60f, 0f);
        termsRt.sizeDelta = new Vector2(250f, 50f);
        TextMeshProUGUI termsTmp = termsObj.AddComponent<TextMeshProUGUI>();
        SetupTMP(termsTmp, tmpFont, "Terms & Conditions", 22, TextAlignmentOptions.Left, new Color32(160, 140, 130, 255));
        Button termsBtn = termsObj.AddComponent<Button>();

        GameObject privacyObj = CreateUIElement("PrivacyButton", footerObj.transform);
        RectTransform privacyRt = privacyObj.GetComponent<RectTransform>();
        privacyRt.anchorMin = new Vector2(1f, 0.5f);
        privacyRt.anchorMax = new Vector2(1f, 0.5f);
        privacyRt.pivot = new Vector2(1f, 0.5f);
        privacyRt.anchoredPosition = new Vector2(-60f, 0f);
        privacyRt.sizeDelta = new Vector2(250f, 50f);
        TextMeshProUGUI privacyTmp = privacyObj.AddComponent<TextMeshProUGUI>();
        SetupTMP(privacyTmp, tmpFont, "Privacy Policy", 22, TextAlignmentOptions.Right, new Color32(160, 140, 130, 255));
        Button privacyBtn = privacyObj.AddComponent<Button>();

        // 9. Link all references into SettingsPopup via SerializedObject
        SerializedObject soPopup = new SerializedObject(sp);
        soPopup.FindProperty("_backdrop").objectReferenceValue = backdropCg;
        soPopup.FindProperty("_card").objectReferenceValue = cardRt;
        soPopup.FindProperty("_closeButton").objectReferenceValue = closeBtn;
        soPopup.FindProperty("_musicToggle").objectReferenceValue = musicToggle;
        soPopup.FindProperty("_soundsToggle").objectReferenceValue = soundsToggle;
        soPopup.FindProperty("_hapticsToggle").objectReferenceValue = hapticsToggle;
        soPopup.FindProperty("_menuButton").objectReferenceValue = backToMenuBtn;
        soPopup.FindProperty("_termsButton").objectReferenceValue = termsBtn;
        soPopup.FindProperty("_privacyButton").objectReferenceValue = privacyBtn;
        soPopup.ApplyModifiedProperties();

        popupObj.SetActive(false);
        return sp;
    }

    private static ToggleSwitch CreateSettingRow(string name, Transform parent, string labelText, Sprite iconSpr, float yPos, TMP_FontAsset font)
    {
        GameObject rowObj = CreateUIElement(name, parent);
        RectTransform rowRt = rowObj.GetComponent<RectTransform>();
        rowRt.anchorMin = new Vector2(0f, 1f);
        rowRt.anchorMax = new Vector2(1f, 1f);
        rowRt.pivot = new Vector2(0.5f, 1f);
        rowRt.anchoredPosition = new Vector2(0f, yPos);
        rowRt.sizeDelta = new Vector2(-160f, 80f);

        if (iconSpr != null)
        {
            GameObject iconObj = CreateDecorImage("Icon", rowObj.transform, iconSpr, new Vector2(40f, 0f), new Vector2(60f, 60f), Color.white);
            RectTransform iconRt = iconObj.GetComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0f, 0.5f);
            iconRt.anchorMax = new Vector2(0f, 0.5f);
            iconRt.pivot = new Vector2(0.5f, 0.5f);
        }

        GameObject labelObj = CreateUIElement("Label", rowObj.transform);
        RectTransform labelRt = labelObj.GetComponent<RectTransform>();
        labelRt.anchorMin = new Vector2(0f, 0.5f);
        labelRt.anchorMax = new Vector2(0f, 0.5f);
        labelRt.pivot = new Vector2(0f, 0.5f);
        labelRt.anchoredPosition = new Vector2(90f, 0f);
        labelRt.sizeDelta = new Vector2(300f, 60f);
        TextMeshProUGUI labelTmp = labelObj.AddComponent<TextMeshProUGUI>();
        SetupTMP(labelTmp, font, labelText, 32, TextAlignmentOptions.Left, new Color32(92, 54, 39, 255));

        Sprite toggleOnSpr = LoadSprite("Toggle_On");
        Sprite toggleOffSpr = LoadSprite("Toggle_Off");

        GameObject toggleObj = CreateUIElement("ToggleSwitch", rowObj.transform);
        RectTransform toggleRt = toggleObj.GetComponent<RectTransform>();
        toggleRt.anchorMin = new Vector2(1f, 0.5f);
        toggleRt.anchorMax = new Vector2(1f, 0.5f);
        toggleRt.pivot = new Vector2(1f, 0.5f);
        toggleRt.anchoredPosition = new Vector2(0f, 0f);
        toggleRt.sizeDelta = new Vector2(140f, 70f);

        Image toggleImg = toggleObj.AddComponent<Image>();
        toggleImg.sprite = toggleOnSpr;
        toggleImg.preserveAspect = true;

        ToggleSwitch ts = toggleObj.AddComponent<ToggleSwitch>();
        SerializedObject soTs = new SerializedObject(ts);
        soTs.FindProperty("_image").objectReferenceValue = toggleImg;
        soTs.FindProperty("_onSprite").objectReferenceValue = toggleOnSpr;
        soTs.FindProperty("_offSprite").objectReferenceValue = toggleOffSpr;
        soTs.ApplyModifiedProperties();

        return ts;
    }
}
