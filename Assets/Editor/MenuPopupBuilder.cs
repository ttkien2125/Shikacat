using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class MenuPopupBuilder
{
    public static SettingsPopup BuildSettingsPopup(GameObject canvasObj, TMP_FontAsset tmpFont)
    {
        // 1. Root Popup GameObject
        GameObject popupObj = MenuUIFactory.CreateUIElement("SettingsPopup", canvasObj.transform);
        MenuUIFactory.StretchFull(popupObj.GetComponent<RectTransform>());
        SettingsPopup sp = popupObj.AddComponent<SettingsPopup>();

        // 2. Backdrop (dark overlay with fade and click-to-close)
        GameObject backdropObj = MenuUIFactory.CreateUIElement("Backdrop", popupObj.transform);
        MenuUIFactory.StretchFull(backdropObj.GetComponent<RectTransform>());
        Image backdropImg = backdropObj.AddComponent<Image>();
        backdropImg.color = new Color(0f, 0f, 0f, 0.72f);
        CanvasGroup backdropCg = backdropObj.AddComponent<CanvasGroup>();
        Button backdropBtn = backdropObj.AddComponent<Button>();
        backdropBtn.targetGraphic = backdropImg;

        // 3. Card Panel
        Sprite cardSpr = MenuUIFactory.LoadSprite("Panel_Popup_Body");
        GameObject cardObj = MenuUIFactory.CreateUIElement("Card", popupObj.transform);
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
        Sprite headerSpr = MenuUIFactory.LoadSprite("Panel_Popup_Header");
        GameObject headerObj = MenuUIFactory.CreateUIElement("HeaderBanner", cardObj.transform);
        RectTransform headerRt = headerObj.GetComponent<RectTransform>();
        headerRt.anchorMin = new Vector2(0.5f, 1f);
        headerRt.anchorMax = new Vector2(0.5f, 1f);
        headerRt.pivot = new Vector2(0.5f, 1f);
        headerRt.anchoredPosition = new Vector2(0f, -20f);
        headerRt.sizeDelta = new Vector2(750f, 170f);
        Image headerImg = headerObj.AddComponent<Image>();
        headerImg.sprite = headerSpr;
        headerImg.preserveAspect = true;

        GameObject titleObj = MenuUIFactory.CreateUIElement("Title", cardObj.transform);
        RectTransform titleRt = titleObj.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0.5f, 1f);
        titleRt.anchorMax = new Vector2(0.5f, 1f);
        titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.anchoredPosition = new Vector2(0f, -65f);
        titleRt.sizeDelta = new Vector2(500f, 80f);
        TextMeshProUGUI titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
        MenuUIFactory.SetupTMP(titleTmp, tmpFont, "SETTINGS", 44, TextAlignmentOptions.Center, new Color32(255, 248, 237, 255));

        // 5. Close Button ('X')
        Sprite closeSpr = MenuUIFactory.LoadSprite("Button_Close");
        Button closeBtn = MenuUIFactory.CreateImageButton("CloseButton", cardObj.transform, closeSpr, new Vector2(-34f, -52f), new Vector2(104f, 104f));
        RectTransform closeRt = closeBtn.GetComponent<RectTransform>();
        closeRt.anchorMin = new Vector2(1f, 1f);
        closeRt.anchorMax = new Vector2(1f, 1f);
        closeRt.pivot = new Vector2(0.5f, 0.5f);

        // 6. Settings Rows: Music, Sounds, Haptics
        Sprite musicSpr = MenuUIFactory.LoadSprite("Icon_Music");
        Sprite hapticsSpr = MenuUIFactory.LoadSprite("Icon_Haptics");
        ToggleSwitch musicToggle = CreateSettingRow("Row_Music", cardObj.transform, "MUSIC", musicSpr, -230f, tmpFont);
        ToggleSwitch soundsToggle = CreateSettingRow("Row_Sounds", cardObj.transform, "SOUNDS", musicSpr, -340f, tmpFont);
        ToggleSwitch hapticsToggle = CreateSettingRow("Row_Haptics", cardObj.transform, "HAPTICS", hapticsSpr, -450f, tmpFont);

        // 7. Action Button: "BACK TO MENU"
        Sprite btnSecSpr = MenuUIFactory.LoadSprite("Button_Secondary");
        Button backToMenuBtn = MenuUIFactory.CreateImageButton("MenuButton", cardObj.transform, btnSecSpr, new Vector2(0f, 170f), new Vector2(660f, 110f));
        RectTransform backRt = backToMenuBtn.GetComponent<RectTransform>();
        backRt.anchorMin = new Vector2(0.5f, 0f);
        backRt.anchorMax = new Vector2(0.5f, 0f);
        backRt.pivot = new Vector2(0.5f, 0f);
        backToMenuBtn.image.type = Image.Type.Sliced;

        GameObject backTextObj = MenuUIFactory.CreateUIElement("Text", backToMenuBtn.transform);
        MenuUIFactory.StretchFull(backTextObj.GetComponent<RectTransform>());
        TextMeshProUGUI backTmp = backTextObj.AddComponent<TextMeshProUGUI>();
        MenuUIFactory.SetupTMP(backTmp, tmpFont, "BACK TO MENU", 36, TextAlignmentOptions.Center, new Color32(255, 248, 237, 255));

        // 8. Footer (T&C, Privacy)
        GameObject footerObj = MenuUIFactory.CreateUIElement("Footer", cardObj.transform);
        RectTransform footerRt = footerObj.GetComponent<RectTransform>();
        footerRt.anchorMin = new Vector2(0f, 0f);
        footerRt.anchorMax = new Vector2(1f, 0f);
        footerRt.pivot = new Vector2(0.5f, 0f);
        footerRt.anchoredPosition = new Vector2(0f, 35f);
        footerRt.sizeDelta = new Vector2(0f, 90f);

        GameObject termsObj = MenuUIFactory.CreateUIElement("TermsButton", footerObj.transform);
        RectTransform termsRt = termsObj.GetComponent<RectTransform>();
        termsRt.anchorMin = new Vector2(0f, 0.5f);
        termsRt.anchorMax = new Vector2(0f, 0.5f);
        termsRt.pivot = new Vector2(0f, 0.5f);
        termsRt.anchoredPosition = new Vector2(60f, 0f);
        termsRt.sizeDelta = new Vector2(250f, 50f);
        TextMeshProUGUI termsTmp = termsObj.AddComponent<TextMeshProUGUI>();
        MenuUIFactory.SetupTMP(termsTmp, tmpFont, "Terms & Conditions", 22, TextAlignmentOptions.Left, new Color32(160, 140, 130, 255));
        Button termsBtn = termsObj.AddComponent<Button>();

        GameObject privacyObj = MenuUIFactory.CreateUIElement("PrivacyButton", footerObj.transform);
        RectTransform privacyRt = privacyObj.GetComponent<RectTransform>();
        privacyRt.anchorMin = new Vector2(1f, 0.5f);
        privacyRt.anchorMax = new Vector2(1f, 0.5f);
        privacyRt.pivot = new Vector2(1f, 0.5f);
        privacyRt.anchoredPosition = new Vector2(-60f, 0f);
        privacyRt.sizeDelta = new Vector2(250f, 50f);
        TextMeshProUGUI privacyTmp = privacyObj.AddComponent<TextMeshProUGUI>();
        MenuUIFactory.SetupTMP(privacyTmp, tmpFont, "Privacy Policy", 22, TextAlignmentOptions.Right, new Color32(160, 140, 130, 255));
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

    public static ToggleSwitch CreateSettingRow(string name, Transform parent, string labelText, Sprite iconSpr, float yPos, TMP_FontAsset font)
    {
        GameObject rowObj = MenuUIFactory.CreateUIElement(name, parent);
        RectTransform rowRt = rowObj.GetComponent<RectTransform>();
        rowRt.anchorMin = new Vector2(0f, 1f);
        rowRt.anchorMax = new Vector2(1f, 1f);
        rowRt.pivot = new Vector2(0.5f, 1f);
        rowRt.anchoredPosition = new Vector2(0f, yPos);
        rowRt.sizeDelta = new Vector2(-160f, 80f);

        if (iconSpr != null)
        {
            GameObject iconObj = MenuUIFactory.CreateDecorImage("Icon", rowObj.transform, iconSpr, new Vector2(40f, 0f), new Vector2(60f, 60f), Color.white);
            RectTransform iconRt = iconObj.GetComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0f, 0.5f);
            iconRt.anchorMax = new Vector2(0f, 0.5f);
            iconRt.pivot = new Vector2(0.5f, 0.5f);
        }

        GameObject labelObj = MenuUIFactory.CreateUIElement("Label", rowObj.transform);
        RectTransform labelRt = labelObj.GetComponent<RectTransform>();
        labelRt.anchorMin = new Vector2(0f, 0.5f);
        labelRt.anchorMax = new Vector2(0f, 0.5f);
        labelRt.pivot = new Vector2(0f, 0.5f);
        labelRt.anchoredPosition = new Vector2(90f, 0f);
        labelRt.sizeDelta = new Vector2(300f, 60f);
        TextMeshProUGUI labelTmp = labelObj.AddComponent<TextMeshProUGUI>();
        MenuUIFactory.SetupTMP(labelTmp, font, labelText, 32, TextAlignmentOptions.Left, new Color32(92, 54, 39, 255));

        Sprite toggleOnSpr = MenuUIFactory.LoadSprite("Toggle_On");
        Sprite toggleOffSpr = MenuUIFactory.LoadSprite("Toggle_Off");

        GameObject toggleObj = MenuUIFactory.CreateUIElement("ToggleSwitch", rowObj.transform);
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

    public static LevelsPopup BuildLevelsPopup(GameObject canvasObj, TMP_FontAsset tmpFont)
    {
        // 1. Root Popup GameObject
        GameObject popupObj = MenuUIFactory.CreateUIElement("LevelsPopup", canvasObj.transform);
        MenuUIFactory.StretchFull(popupObj.GetComponent<RectTransform>());
        LevelsPopup lp = popupObj.AddComponent<LevelsPopup>();

        // 2. Backdrop (dark overlay with fade and click-to-close)
        GameObject backdropObj = MenuUIFactory.CreateUIElement("Backdrop", popupObj.transform);
        MenuUIFactory.StretchFull(backdropObj.GetComponent<RectTransform>());
        Image backdropImg = backdropObj.AddComponent<Image>();
        backdropImg.color = new Color(0f, 0f, 0f, 0.72f);
        CanvasGroup backdropCg = backdropObj.AddComponent<CanvasGroup>();
        Button backdropBtn = backdropObj.AddComponent<Button>();
        backdropBtn.targetGraphic = backdropImg;

        // 3. Card Panel
        Sprite cardSpr = MenuUIFactory.LoadSprite("Panel_Popup_Body");
        GameObject cardObj = MenuUIFactory.CreateUIElement("Card", popupObj.transform);
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
        Sprite headerSpr = MenuUIFactory.LoadSprite("Panel_Popup_Header");
        GameObject headerObj = MenuUIFactory.CreateUIElement("HeaderBanner", cardObj.transform);
        RectTransform headerRt = headerObj.GetComponent<RectTransform>();
        headerRt.anchorMin = new Vector2(0.5f, 1f);
        headerRt.anchorMax = new Vector2(0.5f, 1f);
        headerRt.pivot = new Vector2(0.5f, 1f);
        headerRt.anchoredPosition = new Vector2(0f, -20f);
        headerRt.sizeDelta = new Vector2(750f, 170f);
        Image headerImg = headerObj.AddComponent<Image>();
        headerImg.sprite = headerSpr;
        headerImg.preserveAspect = true;

        GameObject titleObj = MenuUIFactory.CreateUIElement("Title", cardObj.transform);
        RectTransform titleRt = titleObj.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0.5f, 1f);
        titleRt.anchorMax = new Vector2(0.5f, 1f);
        titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.anchoredPosition = new Vector2(0f, -65f);
        titleRt.sizeDelta = new Vector2(500f, 80f);
        TextMeshProUGUI titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
        MenuUIFactory.SetupTMP(titleTmp, tmpFont, "LEVELS", 44, TextAlignmentOptions.Center, new Color32(255, 248, 237, 255));

        // 5. Close Button ('X')
        Sprite closeSpr = MenuUIFactory.LoadSprite("Button_Close");
        Button closeBtn = MenuUIFactory.CreateImageButton("CloseButton", cardObj.transform, closeSpr, new Vector2(-34f, -52f), new Vector2(104f, 104f));
        RectTransform closeRt = closeBtn.GetComponent<RectTransform>();
        closeRt.anchorMin = new Vector2(1f, 1f);
        closeRt.anchorMax = new Vector2(1f, 1f);
        closeRt.pivot = new Vector2(0.5f, 0.5f);

        // 6. ScrollView Container
        GameObject scrollObj = MenuUIFactory.CreateUIElement("ScrollView", cardObj.transform);
        RectTransform scrollRt = scrollObj.GetComponent<RectTransform>();
        scrollRt.anchorMin = new Vector2(0.5f, 0.5f);
        scrollRt.anchorMax = new Vector2(0.5f, 0.5f);
        scrollRt.pivot = new Vector2(0.5f, 0.5f);
        scrollRt.anchoredPosition = new Vector2(0f, -40f);
        scrollRt.sizeDelta = new Vector2(720f, 780f);

        ScrollRect scrollRect = scrollObj.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Elastic;

        // Viewport
        GameObject viewportObj = MenuUIFactory.CreateUIElement("Viewport", scrollObj.transform);
        RectTransform viewportRt = viewportObj.GetComponent<RectTransform>();
        MenuUIFactory.StretchFull(viewportRt);
        viewportObj.AddComponent<RectMask2D>();
        scrollRect.viewport = viewportRt;

        // Content
        GameObject contentObj = MenuUIFactory.CreateUIElement("Content", viewportObj.transform);
        RectTransform contentRt = contentObj.GetComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0f, 1f);
        contentRt.anchorMax = new Vector2(1f, 1f);
        contentRt.pivot = new Vector2(0.5f, 1f);
        contentRt.anchoredPosition = Vector2.zero;
        contentRt.sizeDelta = new Vector2(0f, 780f);
        scrollRect.content = contentRt;

        GridLayoutGroup grid = contentObj.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(116f, 116f);
        grid.spacing = new Vector2(20f, 20f);
        grid.padding = new RectOffset(18, 18, 16, 20);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 5;
        grid.childAlignment = TextAnchor.UpperCenter;

        ContentSizeFitter csf = contentObj.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // 7. Create Level Items dynamically using LevelProgress (queries LevelCatalog or defaults)
        int totalLevels = LevelProgress.TotalLevels;

        Sprite roundSpr = MenuUIFactory.LoadSprite("UI_RoundedRect");
        Sprite checkSpr = MenuUIFactory.LoadSprite("Icon_Checkmark");
        Sprite lockSpr = MenuUIFactory.LoadSprite("Icon_Lock");

        for (int i = 0; i < totalLevels; i++)
        {
            int levelNum = i + 1;
            GameObject itemObj = MenuUIFactory.CreateUIElement($"LevelItem_{levelNum}", contentObj.transform);
            RectTransform itemRt = itemObj.GetComponent<RectTransform>();
            itemRt.sizeDelta = new Vector2(116f, 116f);

            Image itemBg = itemObj.AddComponent<Image>();
            itemBg.sprite = roundSpr;
            itemBg.type = Image.Type.Sliced;
            itemBg.color = Color.white;

            Button itemBtn = itemObj.AddComponent<Button>();
            itemBtn.targetGraphic = itemBg;

            // Level Number Text
            GameObject numObj = MenuUIFactory.CreateUIElement("Text", itemObj.transform);
            RectTransform numRt = numObj.GetComponent<RectTransform>();
            MenuUIFactory.StretchFull(numRt);
            TextMeshProUGUI numTmp = numObj.AddComponent<TextMeshProUGUI>();
            MenuUIFactory.SetupTMP(numTmp, tmpFont, levelNum.ToString(), 38, TextAlignmentOptions.Center, Color.white);
            numTmp.fontStyle = FontStyles.Bold;

            // Tick Icon
            GameObject tickObj = MenuUIFactory.CreateUIElement("TickIcon", itemObj.transform);
            RectTransform tickRt = tickObj.GetComponent<RectTransform>();
            tickRt.anchorMin = new Vector2(1f, 0f);
            tickRt.anchorMax = new Vector2(1f, 0f);
            tickRt.pivot = new Vector2(1f, 0f);
            tickRt.anchoredPosition = new Vector2(-6f, 6f);
            tickRt.sizeDelta = new Vector2(34f, 34f);
            Image tickImg = tickObj.AddComponent<Image>();
            tickImg.sprite = checkSpr;
            tickImg.preserveAspect = true;
            tickObj.SetActive(false);

            // Lock Icon
            GameObject lockObj = MenuUIFactory.CreateUIElement("LockIcon", itemObj.transform);
            RectTransform lockRt = lockObj.GetComponent<RectTransform>();
            lockRt.anchorMin = new Vector2(0.5f, 0.5f);
            lockRt.anchorMax = new Vector2(0.5f, 0.5f);
            lockRt.pivot = new Vector2(0.5f, 0.5f);
            lockRt.anchoredPosition = Vector2.zero;
            lockRt.sizeDelta = new Vector2(44f, 44f);
            Image lockImg = lockObj.AddComponent<Image>();
            lockImg.sprite = lockSpr;
            lockImg.preserveAspect = true;
            lockObj.SetActive(false);

            LevelItemUI itemUI = itemObj.AddComponent<LevelItemUI>();
            SerializedObject soItem = new SerializedObject(itemUI);
            soItem.FindProperty("_button").objectReferenceValue = itemBtn;
            soItem.FindProperty("_background").objectReferenceValue = itemBg;
            soItem.FindProperty("_levelText").objectReferenceValue = numTmp;
            soItem.FindProperty("_tickIcon").objectReferenceValue = tickImg;
            soItem.FindProperty("_lockIcon").objectReferenceValue = lockImg;
            soItem.ApplyModifiedProperties();
        }

        // 8. Serialize references into LevelsPopup
        SerializedObject soPopup = new SerializedObject(lp);
        soPopup.FindProperty("_backdrop").objectReferenceValue = backdropCg;
        soPopup.FindProperty("_card").objectReferenceValue = cardRt;
        soPopup.FindProperty("_closeButton").objectReferenceValue = closeBtn;
        soPopup.FindProperty("_gridContainer").objectReferenceValue = contentRt;
        soPopup.FindProperty("_totalLevels").intValue = totalLevels;
        soPopup.ApplyModifiedProperties();

        popupObj.SetActive(false);
        return lp;
    }
}

