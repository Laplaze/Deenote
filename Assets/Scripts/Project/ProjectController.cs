﻿using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using NAudio.Wave;
using UnityEngine.Events;

public class ProjectController : MonoBehaviour
{
    //-Declaration-
    //-Panel control buttons-
    public GameObject filePanelButton;
    public GameObject infoPanelButton;
    public GameObject chartsPanelButton;
    public GameObject editPanelButton;
    public GameObject settingsPanelButton;
    //-Panels-
    public GameObject filePanel;
    public GameObject newProjectPanel;
    public GameObject infoPanel;
    public GameObject chartsPanel;
    public GameObject editPanel;
    public GameObject settingsPanel;
    //-Canvas-
    public GameObject directorySelectorCanvas;
    public GameObject aboutCanvas;
    //-File Panel-
    public LocalizedText songSelectButtonText;
    public LocalizedText fileSelectButtonText;
    public Button songSelectConfirmButton;
    public string dragAndDropFileName = null;
    //-Info Panel-
    public InputField infoProjectNameInputField;
    public InputField charterNameInputField;
    public Text songNameText;
    //-Charts Panel-
    public InputField[] lvlInputFields;
    //-Settings Panel-
    private float lastAutoSaveTime = 0.0f;
    private float autoSaveTime = 300.0f;
    public Toggle autoSaveToggle;
    public Toggle vSyncToggle;
    private bool autoSaveState = true;
    private bool vSync = true;
    //-About the project-
    private AudioClip songAudioClip; // Audio clip of the song
    private byte[] audioFileBytes; // All bytes of the audio file
    private string projectFileName; // Name of the project file
    private string projectFolder; // Where the project file is at
    private FileInfo songFile; // Where the song is saved, only used when loading song for the 1st time
    public Project project = null; // The project itself
    //-Other scripts-
    public StageController stage;
    public ProjectSaveLoad projectSL;
    public DirectorySelectorController directorySelectorController;
    public FileOpener fileOpener;
    //-Flags-
    private bool clearStageNewProjectMode;
    //-Stage-
    private int currentInStage = -1;
    public Text stageUIProjectName;
    public Text stageUILvl;
    public Text stageUIScore;
    public TextMesh stageStaveProjectName;
    public Image stageUIDiff;
    private string[] diffNames = { "Easy", "Normal", "Hard", "Extra" };
    public Color[] textColors = new Color[4];
    public Image timeSliderImage;
    //-Data from assets-
    public Sprite[] diffImage = new Sprite[4];
    //-Others-
    public GameObject leftBackgroundImage;
    public GameObject aboutWindow;
    public Text debugText;
    public UnityEvent resolutionChange;
    public int screenWidth = 1280;
    public int screenHeight = 720;
    public Dropdown languageDropdown;
    //-Functions-
    //-Initialization-
    public void PanelSelectionInit()
    {
        filePanelButton.SetActive(true);
        infoPanelButton.SetActive(false);
        chartsPanelButton.SetActive(false);
        editPanelButton.SetActive(false);
        settingsPanelButton.SetActive(true);
        infoPanel.SetActive(false);
        chartsPanel.SetActive(false);
        editPanel.SetActive(false);
        settingsPanel.SetActive(false);
    }
    //-File Panel-
    public void NewProject() //Project - New
    {
        if (project != null)
        {
            stage.StopPlaying();
            stage.editor.pianoSoundEditor.Deactivate(false);
            MessageScreen.Activate(
                new string[] { "Current project will be closed when you start a new project", "启动新项目时当前的项目会被关闭" },
                new string[] { "<color=#ff5555>Make sure that you have SAVED your project!</color>", "<color=#ff5555>请确认你已经保存当前的项目文件!</color>" },
                new string[] { "Start a new project now!", "启动新项目!" }, ClearStageStartNewProject,
                new string[] { "Take me back to my project", "返回到当前项目" }, delegate { dragAndDropFileName = null; });
            clearStageNewProjectMode = true;
            return;
        }
        songFile = null;
        projectFileName = null;
        projectFolder = null;
        songSelectButtonText.color = new Color(25.0f / 64, 25.0f / 64, 25.0f / 64, 0.5f);
        fileSelectButtonText.color = new Color(25.0f / 64, 25.0f / 64, 25.0f / 64, 0.5f);
        songSelectButtonText.SetStrings("Select the song file", "选择音乐文件");
        fileSelectButtonText.SetStrings("Create a new project file", "创建新工程文件");
        newProjectPanel.SetActive(true);
        filePanel.SetActive(false);
        CheckConfirmButton();
    }
    public void CloseFilePanel() //Close the file panel if new project panel is currently on
    {
        if (newProjectPanel.activeInHierarchy)
            filePanel.SetActive(false);
    }
    public void SelectSong() //Project - New - Select Song
    {
        string[] acceptedExtension = { ".wav", ".ogg", ".mp3" };
        directorySelectorController.ActivateSelection(acceptedExtension, SongSelected);
    }
    public void SelectFile() //Project - New - Select Folder
    {
        string[] acceptedExtension = { ".dsproj" };
        directorySelectorController.ActivateSelection(acceptedExtension, FileSelected, true);
    }
    private void SongSelected() //Called by directory selector controller when song selected
    {
        songFile = new FileInfo(directorySelectorController.selectedItemFullName);
        songSelectButtonText.color = new Color(0.0f, 0.0f, 0.0f, 1.0f);
        songSelectButtonText.SetStrings(songFile.Name);
        directorySelectorController.DeactivateSelection();
        CheckConfirmButton();
    }
    private void FileSelected() //Called by directory selector controller when song selected
    {
        projectFolder = directorySelectorController.selectedItemFullName;
        projectFileName = directorySelectorController.fileName;
        fileSelectButtonText.color = new Color(0.0f, 0.0f, 0.0f, 1.0f);
        fileSelectButtonText.SetStrings(projectFileName + ".dsproj");
        directorySelectorController.DeactivateSelection();
        CheckConfirmButton();
    }
    private void CheckConfirmButton() //Update the state of the confirm button
    {
        if (songFile != null && projectFileName != null && projectFolder != null)
            songSelectConfirmButton.interactable = true;
        else
            songSelectConfirmButton.interactable = false;
    }
    public void CancelCreateProject() //Project - New - Cancel
    {
        newProjectPanel.SetActive(false);
    }
    public void ClearStageStartNewProject() //Create/open a new project when a project in currently opened
    {
        CloseProject();
        if (clearStageNewProjectMode)
            NewProject();
        else
            LoadProject();
    }
    public void ConfirmButtonPressed() //Project - New - Confirm
    {
        project = new Project();
        audioFileBytes = File.ReadAllBytes(songFile.FullName);
        songAudioClip = AudioLoader.LoadFromBuffer(audioFileBytes, songFile.Extension);
        project.songName = songFile.Name;
        project.charts = new ChartOld[4];
        for (int i = 0; i < 4; i++)
            project.charts[i] = new ChartOld
            {
                difficulty = i,
                level = "1"
            };
        stage.musicSource.clip = songAudioClip;
        infoPanelButton.SetActive(true);
        chartsPanelButton.SetActive(true);
        newProjectPanel.SetActive(false);
        infoPanel.SetActive(true);
        currentInStage = -1;
        InfoInitialization();
        LvlInputFieldInit();
    }
    public void SaveProject() //Project - Save
    {
        SavePlayerPrefs();
        if (project != null)
        {
            if (currentInStage != -1)
                foreach (ChartOld chart in project.charts) chart.beats = project.charts[currentInStage].beats;
            StartCoroutine(projectSL.SaveProjectIntoFile(project, audioFileBytes, projectFolder + projectFileName + ".dsproj"));
        }
    }
    public void SaveAs() //Project - Save As
    {
        string[] acceptedExtension = { ".dsproj" };
        if (project != null)
            directorySelectorController.ActivateSelection(acceptedExtension, SaveAsFileSelected, true);
    }
    private void SaveAsFileSelected()
    {
        string asFolder = directorySelectorController.selectedItemFullName;
        string asFile = directorySelectorController.fileName;
        string asFileFullName = asFolder + asFile + ".dsproj";
        directorySelectorController.DeactivateSelection();
        if (project != null)
        {
            if (currentInStage != -1)
                foreach (ChartOld chart in project.charts) chart.beats = project.charts[currentInStage].beats;
            projectSL.SaveProjectIntoFile(project, audioFileBytes, asFileFullName);
        }
    }
    public void LoadProject() //Project - Open
    {
        if (project != null)
        {
            stage.StopPlaying();
            stage.editor.pianoSoundEditor.Deactivate(false);
            MessageScreen.Activate(
                new string[] { "Current project will be closed when you start a new project", "启动新项目时当前的项目会被关闭" },
                new string[] { "<color=#ff5555>Make sure that you have SAVED your project!</color>", "<color=#ff5555>请确认你已经保存当前的项目文件!</color>" },
                new string[] { "Start a new project now!", "启动新项目!" }, ClearStageStartNewProject,
                new string[] { "Take me back to my project", "返回到当前项目" }, delegate { dragAndDropFileName = null; });
            clearStageNewProjectMode = false;
            return;
        }
        if (dragAndDropFileName == null || dragAndDropFileName == "")
        {
            string[] extensions = { ".dsproj" };
            directorySelectorController.ActivateSelection(extensions, ProjectToLoadSelected());
        }
        else
            StartCoroutine(ProjectToLoadSelected(dragAndDropFileName));
    }
    public IEnumerator ProjectToLoadSelected(string fileName = null)
    {
        string projectFullDir = fileName ?? directorySelectorController.selectedItemFullName;
        string audioType = null;
        FileInfo projectFile;
        directorySelectorController.DeactivateSelection();
        dragAndDropFileName = null;
        yield return StartCoroutine(projectSL.LoadProjectFromFile(res => project = res,
            res => audioFileBytes = res, res => audioType = res, projectFullDir));
        songAudioClip = AudioLoader.LoadFromBuffer(audioFileBytes, audioType);
        infoPanelButton.SetActive(true);
        chartsPanelButton.SetActive(true);
        songAudioClip.LoadAudioData();
        stage.musicSource.clip = songAudioClip;
        editPanelButton.SetActive(false);
        projectFile = new FileInfo(projectFullDir);
        projectFileName = projectFile.Name.Remove(projectFile.Name.Length - 7, 7);
        projectFolder = projectFile.FullName.Remove(projectFile.FullName.Length - projectFile.Name.Length, projectFile.Name.Length);
        filePanel.SetActive(false);
        currentInStage = -1;
        InfoInitialization();
        LvlInputFieldInit();
        yield return new WaitForSeconds(3.0f);
        projectSL.loadCompleteText.SetActive(false);
    }
    public void CloseProject()
    {
        stage.ClearStage();
        project = null;
        stage.stageActivated = false;
        stage.editor.activated = false;
        leftBackgroundImage.SetActive(true);
        stage.editor.activated = false;
        PanelSelectionInit();
    }
    public void DragAndDropFileAccept(List<string> paths)
    {
        try
        {
            if (paths.Count != 1) return;
            FileInfo file = new FileInfo(paths[0]);
            string extension = file.Extension;
            if (extension == ".dsproj")
            {
                dragAndDropFileName = paths[0];
                LoadProject();
            }
        }
        catch
        {
            return;
        }
    }
    //-Info Panel-
    private void InfoInitialization() //After creating a project, initialize info panel
    {
        infoProjectNameInputField.text = project.name;
        songNameText.text = project.songName;
        charterNameInputField.text = project.chartMaker;
    }
    public void InfoProjectNameFinishedEditing()
    {
        project.name = infoProjectNameInputField.text;
        stageUIProjectName.text = project.name;
        stageStaveProjectName.text = project.name;
    }
    public void CharterNameFinishedEditing()
    {
        project.chartMaker = charterNameInputField.text;
    }
    public void ChangeMusicFile()
    {
        string[] acceptedExtension = { ".wav", ".ogg", ".mp3" };
        directorySelectorController.ActivateSelection(acceptedExtension, NewSongSelected);
    }
    private void NewSongSelected()
    {
        songFile = new FileInfo(directorySelectorController.selectedItemFullName);
        songNameText.text = songFile.Name;
        project.songName = songNameText.text;
        directorySelectorController.DeactivateSelection();
        audioFileBytes = File.ReadAllBytes(songFile.FullName);
        songAudioClip = AudioLoader.LoadFromBuffer(audioFileBytes, songFile.Extension);
        stage.musicSource.clip = songAudioClip;
        stage.timeSlider.value = 0.0f;
        stage.timeSlider.maxValue = songAudioClip.length;
        stage.OnSliderValueChanged();
    }
    //-Chart Panel-
    private void LvlInputFieldInit()
    {
        for (int i = 0; i < 4; i++) lvlInputFields[i].text = project.charts[i].level;
    }
    public void NewLvl(int diff)
    {
        project.charts[diff].level = lvlInputFields[diff].text;
        if (currentInStage == diff) stageUILvl.text = diffNames[diff] + " Lv" + project.charts[diff].level;
    }
    public void JSONFileSelect(int diff)
    {
        string[] allowedExtension = { ".json", ".txt", ".cytus" };
        directorySelectorController.ActivateSelection(allowedExtension, delegate { ImportChart(diff); });
    }
    public void ImportChart(int diff)
    {
        if (directorySelectorController.selectedItemFullName.EndsWith(".cytus"))
            ImportChartFromCytusChart(diff);
        else
            ImportChartFromJSONFile(diff);
    }
    public void ImportChartFromJSONFile(int diff)
    {
        byte[] bytes = File.ReadAllBytes(directorySelectorController.selectedItemFullName); //JSON file bytes
        char[] bytechars = new char[bytes.Length + 1];
        string str; //JSON string
        string level;
        int length = bytes.Length, i = 0, offset = 0;
        while (bytes[i] != 0x7B) i++;
        offset = i;
        for (; i < length && bytes[i] != 0x00; i++) bytechars[i - offset] = (char)bytes[i];
        str = new string(bytechars);
        JSONChart jchart = Utility.JSONtoJChart(str);
        level = project.charts[diff].level;
        project.charts[diff] = Utility.JCharttoChart(jchart);
        project.charts[diff].level = level;
        directorySelectorController.DeactivateSelection();
    }
    public void ImportChartFromCytusChart(int diff)
    {
        string[] cychart = File.ReadAllLines(directorySelectorController.selectedItemFullName);
        JSONChart jchart = Utility.CytusChartToJChart(cychart);
        string level;
        level = project.charts[diff].level;
        project.charts[diff] = Utility.JCharttoChart(jchart);
        project.charts[diff].level = level;
        directorySelectorController.DeactivateSelection();
    }
    public void ExportAllJSONCharts(int diff)
    {
        if (project != null)
            JSONExportDirectorySelect(diff + 4);
        else
            MessageScreen.Activate(new string[] { "No project file is opened!", "目前没有已经打开的项目文件!" },
                new string[] { "<color=ff7f7f>What are you expecting to be exported???</color>",
                    "<color=ff7f7f>你认为这样能导出什么东西呢???</color>" },
                new string[] { "Back", "返回" }, delegate { });
    }
    public void JSONExportDirectorySelect(int diff)
    {
        string[] allowedExtension = { ".json" };
        string[] difficultyStrings = { "easy", "normal", "hard", "extra" };
        if (project.charts[diff % 4].notes.Count > 0)
        {
            directorySelectorController.ActivateSelection(allowedExtension, delegate { ExportChartToJSONChart(diff); }, true);
            directorySelectorController.SetInitialFileName(projectFileName + "." + difficultyStrings[diff % 4]);
        }
    }
    public void ExportChartToJSONChart(int diff)
    {
        FileStream fs = new FileStream(directorySelectorController.selectedItemFullName + directorySelectorController.fileName + ".json", FileMode.Create);
        directorySelectorController.DeactivateSelection();
        Utility.WriteCharttoJSON(project.charts[diff % 4], fs);
        fs.Close();
        if (diff >= 4 && diff < 7) ExportAllJSONCharts(diff % 4 + 1);
    }
    public void LoadToStage(int diff)
    {
        leftBackgroundImage.SetActive(false);
        stage.editor.activated = true;
        editPanelButton.SetActive(true);
        stageUIDiff.sprite = diffImage[diff];
        stageUIProjectName.text = project.name;
        stageUILvl.text = diffNames[diff] + " Lv" + project.charts[diff].level;
        stageUILvl.color = textColors[diff];
        timeSliderImage.color = textColors[diff];
        stageUIScore.text = "0.00 %";
        stageStaveProjectName.text = project.name;
        stage.StopPlaying();
        stage.ClearStage();
        stage.InitializeStage(project, diff, this);
        if (currentInStage != -1)
            foreach (ChartOld chart in project.charts) chart.beats = project.charts[currentInStage].beats;
        currentInStage = diff;
    }
    //-Settings Panel-
    public void ToggleAutoSave()
    {
        autoSaveState = autoSaveToggle.isOn;
        if (autoSaveState) lastAutoSaveTime = Time.time;
    }
    public void ToggleVSync(bool on)
    {
        vSync = on;
        QualitySettings.vSyncCount = on ? 1 : 0;
    }
    public void OpenAbout()
    {
        CurrentState.ignoreAllInput = true;
        CurrentState.ignoreScroll = true;
        aboutWindow.SetActive(true);
        stage.StopPlaying();
    }
    public void CloseAbout()
    {
        CurrentState.ignoreAllInput = false;
        CurrentState.ignoreScroll = false;
        aboutWindow.SetActive(false);
    }
    //-Other-
    private void UpdateDirectorySelectorCanvas() //Update the stupid canvas so the user can actually see the window
    {
        directorySelectorCanvas.SetActive(false);
        directorySelectorCanvas.SetActive(true);
    }
    private void UpdateAboutCanvas()
    {
        aboutCanvas.SetActive(false);
        aboutCanvas.SetActive(true);
    }
    private void LoadPlayerPrefs()
    {
        autoSaveToggle.isOn = Utility.PlayerPrefsGetBool("Autosave", autoSaveToggle.isOn);
        ToggleAutoSave();
        vSyncToggle.isOn = Utility.PlayerPrefsGetBool("VSync On", vSyncToggle.isOn);
        ToggleVSync(vSyncToggle.isOn);
        languageDropdown.value = PlayerPrefs.GetInt("Language", 0);
    }
    public void SavePlayerPrefs()
    {
        Utility.PlayerPrefsSetBool("Autosave", autoSaveState);
        Utility.PlayerPrefsSetBool("Light Effect", stage.lightEffectState);
        Utility.PlayerPrefsSetBool("Show FPS", stage.showFPS);
        Utility.PlayerPrefsSetBool("VSync On", vSync);
        PlayerPrefs.SetInt("Mouse Wheel Sensitivity", stage.mouseSens);
        PlayerPrefs.SetInt("Note Speed", stage.chartPlaySpeed);
        PlayerPrefs.SetInt("Music Speed", stage.musicPlaySpeed);
        PlayerPrefs.SetInt("Effect Volume", stage.effectVolume);
        PlayerPrefs.SetInt("Music Volume", stage.musicVolume);
        PlayerPrefs.SetInt("Piano Volume", stage.pianoVolume);
        Utility.PlayerPrefsSetBool("Show Link Line", stage.linkLineParent.gameObject.activeSelf);
        PlayerPrefs.SetInt("XGrid Count", stage.editor.xGrid);
        PlayerPrefs.SetFloat("XGrid Offset", stage.editor.xGridOffset);
        PlayerPrefs.SetInt("TGrid Count", stage.editor.tGrid);
        PlayerPrefs.SetInt("Language", LanguageSelector.Language);
        Utility.PlayerPrefsSetBool("Snap To Grid", stage.editor.snapToGrid);
        Utility.PlayerPrefsSetBool("Show Indicator", stage.editor.noteIndicatorsToggler.activeSelf);
        Utility.PlayerPrefsSetBool("Show Border", stage.editor.border.activeSelf);
    }
    public void SetScreenResolution(int selection)
    {
        int width = 0, height = 0;
        switch (selection)
        {
            case 0: width = 960; height = 540; break;
            case 1: width = 1280; height = 720; break;
            case 2: width = 1920; height = 1080; break;
        }
        Screen.SetResolution(width, height, false);
    }
    private void CheckResolutionChange()
    {
        int width = Screen.width, height = Screen.height;
        if (width != screenWidth)
        {
            screenWidth = width;
            screenHeight = height;
            Utility.stageWidth = screenWidth * 3 / 4;
            Utility.stageHeight = screenHeight;
            resolutionChange.Invoke();
        }
    }
    public void SetLanguage(int language)
    {
        LanguageSelector.Language = language;
    }
    private void Start()
    {
        DragAndDropUnity.Enable(DragAndDropFileAccept);
        SetScreenResolution(1);
        LoadPlayerPrefs();
        Utility.debugText = debugText;
        project = null;
        PanelSelectionInit();
        UpdateDirectorySelectorCanvas();
        UpdateAboutCanvas();
        Application.runInBackground = true;
        fileOpener.CheckRegistry();
        fileOpener.CheckCommandLine();
    }
    private void Shortcuts()
    {
        if (Utility.DetectKeys(KeyCode.S, Utility.CTRL)) //Ctrl+S
            SaveProject();
        if (Utility.DetectKeys(KeyCode.S, Utility.CTRL + Utility.SHIFT)) //Ctrl+Shift+S
            SaveAs();
        if (Utility.DetectKeys(KeyCode.N, Utility.CTRL)) //Ctrl+N
            NewProject();
        if (Utility.DetectKeys(KeyCode.O, Utility.CTRL)) //Ctrl+O
            LoadProject();
        if (Utility.DetectKeys(KeyCode.Q, Utility.CTRL)) //Ctrl+Q
        {
            if (stage.stageActivated)
            {
                stage.StopPlaying();
                stage.editor.pianoSoundEditor.Deactivate(false);
            }
            RightScrollViewController controller = FindObjectOfType<RightScrollViewController>();
            controller.OpenQuitScreen();
        }
        if (Utility.DetectKeys(KeyCode.E, Utility.CTRL)) //Ctrl+E
        {
            if (stage.stageActivated)
            {
                stage.StopPlaying();
                stage.editor.pianoSoundEditor.Deactivate(false);
            }
            ExportAllJSONCharts(0);
        }
    }
    private void Update()
    {
        //Autosave
        lastAutoSaveTime += Time.deltaTime;
        if (autoSaveState && lastAutoSaveTime > autoSaveTime)
        {
            lastAutoSaveTime -= autoSaveTime;
            SaveProject();
        }
        CheckResolutionChange();
        Shortcuts();
    }
    private void OnApplicationQuit()
    {
        Application.CancelQuit();
        FindObjectOfType<RightScrollViewController>().OpenQuitScreen();
    }
}