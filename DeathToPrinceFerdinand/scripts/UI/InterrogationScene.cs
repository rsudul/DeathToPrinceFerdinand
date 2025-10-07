using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using DeathToPrinceFerdinand.scripts.Core.Contradictions;
using DeathToPrinceFerdinand.scripts.Core.Models;
using DeathToPrinceFerdinand.scripts.Infrastructure;

namespace DeathToPrinceFerdinand.scripts.UI
{
    public partial class InterrogationScene : Control
    {
        private Label _headerLabel;
        private VBoxContainer _evidenceList;
        private VBoxContainer _testimonyList;
        private VBoxContainer _contradictionsList;
        private Button _checkContradictionButton;
        private RichTextLabel _feedbackLabel;

        private RichTextLabel _currentStatementDisplay;
        private Button _continueButton;
        private Button _pressButton;
        private Button _presentEvidenceButton;

        private DossierState _currentDossier;
        private List<Evidence> _availableEvidence = new();
        private Evidence _selectedEvidence;
        private TestimonyStatement _selectedTestimony;

        private IContradictionService _contradictionService;
        private IContradictionQueryFactory _queryFactory;
        private IInvestigationContext _context;

        private InterrogationState _interrogationState;

        public override void _Ready()
        {
            if (!GameServices.IsInitialized)
            {
                GameServices.Initialize($"res://data/");
            }

            _contradictionService = GameServices.GetService<IContradictionService>();
            _queryFactory = GameServices.GetService<IContradictionQueryFactory>();
            _context = GameServices.GetService<IInvestigationContext>();

            BuildUI();
            LoadRealData();
        }

        private void BuildUI()
        {
            var mainContainer = new MarginContainer();
            mainContainer.SetAnchorsPreset(LayoutPreset.FullRect);
            var viewportSize = GetViewportRect().Size;
            int margin = (int)(viewportSize.X * 0.02f);
            mainContainer.AddThemeConstantOverride("margin_left", margin);
            mainContainer.AddThemeConstantOverride("margin_top", margin);
            mainContainer.AddThemeConstantOverride("margin_right", margin);
            mainContainer.AddThemeConstantOverride("margin_bottom", margin);
            AddChild(mainContainer);

            var vbox = new VBoxContainer();
            vbox.AddThemeConstantOverride("separation", 10);
            vbox.SizeFlagsVertical = SizeFlags.ExpandFill;
            vbox.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            mainContainer.AddChild(vbox);

            _headerLabel = new Label();
            _headerLabel.AddThemeColorOverride("font_color", new Color(0.9f, 0.9f, 0.9f));
            _headerLabel.AddThemeFontSizeOverride("font_size", 24);
            _headerLabel.Text = "Investigation: [Loading...]";
            vbox.AddChild(_headerLabel);

            var separator = new HSeparator();
            vbox.AddChild(separator);

            var hbox = new HBoxContainer();
            hbox.AddThemeConstantOverride("separation", 15);
            hbox.SizeFlagsVertical = SizeFlags.ExpandFill;
            hbox.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            vbox.AddChild(hbox);

            var leftPanel = CreateEvidencePanel();
            leftPanel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            leftPanel.SizeFlagsStretchRatio = 0.3f;
            hbox.AddChild(leftPanel);

            var rightPanel = CreateTestimonyPanel();
            rightPanel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            rightPanel.SizeFlagsStretchRatio = 0.7f;
            hbox.AddChild(rightPanel);
        }

        private PanelContainer CreateEvidencePanel()
        {
            var panel = new PanelContainer();
            panel.SizeFlagsVertical = SizeFlags.ExpandFill;
            panel.SizeFlagsHorizontal = SizeFlags.ExpandFill;

            var vbox = new VBoxContainer();
            vbox.AddThemeConstantOverride("separation", 8);
            vbox.SizeFlagsVertical = SizeFlags.ExpandFill;
            vbox.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            panel.AddChild(vbox);

            var header = new Label();
            header.Text = "EVIDENCE DRAWER";
            header.AddThemeFontSizeOverride("font_size", 18);
            header.AddThemeColorOverride("font_color", new Color(0.8f, 0.8f, 0.8f));
            vbox.AddChild(header);

            var separator = new HSeparator();
            vbox.AddChild(separator);

            var scrollContainer = new ScrollContainer();
            scrollContainer.SizeFlagsVertical = SizeFlags.ExpandFill;
            vbox.AddChild(scrollContainer);

            _evidenceList = new VBoxContainer();
            _evidenceList.AddThemeConstantOverride("separation", 5);
            scrollContainer.AddChild(_evidenceList);

            return panel;
        }

        private PanelContainer CreateTestimonyPanel()
        {
            var panel = new PanelContainer();
            panel.SizeFlagsVertical = SizeFlags.ExpandFill;
            panel.SizeFlagsHorizontal = SizeFlags.ExpandFill;

            var vbox = new VBoxContainer();
            vbox.AddThemeConstantOverride("separation", 8);
            vbox.SizeFlagsVertical = SizeFlags.ExpandFill;
            vbox.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            panel.AddChild(vbox);

            var infoLabel = new Label();
            infoLabel.Text = "INTERROGATION";
            infoLabel.AddThemeFontSizeOverride("font_size", 18);
            infoLabel.AddThemeColorOverride("font_color", new Color(0.8f, 0.8f, 0.8f));
            vbox.AddChild(infoLabel);

            var separator1 = new HSeparator();
            vbox.AddChild(separator1);

            var currentStatementLabel = new Label();
            currentStatementLabel.Text = "CURRENT STATEMENT";
            currentStatementLabel.AddThemeFontSizeOverride("font_size", 14);
            currentStatementLabel.AddThemeColorOverride("font_color", new Color(0.8f, 0.8f, 0.8f));
            vbox.AddChild(currentStatementLabel);

            _currentStatementDisplay = new RichTextLabel();
            _currentStatementDisplay.BbcodeEnabled = true;
            _currentStatementDisplay.FitContent = true;
            _currentStatementDisplay.ScrollActive = false;
            _currentStatementDisplay.Text = "[color=#cccccc][i]Press 'Begin Interrogation' to start...[/i][/color]";
            vbox.AddChild(_currentStatementDisplay);

            var actionsHBox = new HBoxContainer();
            actionsHBox.AddThemeConstantOverride("separation", 10);
            vbox.AddChild(actionsHBox);

            _continueButton = new Button();
            _continueButton.Text = "Continue";
            _continueButton.Pressed += OnContinuePressed;
            actionsHBox.AddChild(_continueButton);

            _pressButton = new Button();
            _pressButton.Text = "Press for Details";
            _pressButton.Disabled = true;
            actionsHBox.AddChild(_pressButton);

            _presentEvidenceButton = new Button();
            _presentEvidenceButton.Text = "Present Evidence";
            _presentEvidenceButton.Disabled = true;
            actionsHBox.AddChild(_presentEvidenceButton);

            var separator2 = new HSeparator();
            vbox.AddChild(separator2);

            var transcriptHeader = new Label();
            transcriptHeader.Text = "TRANSCRIPT";
            transcriptHeader.AddThemeFontSizeOverride("font_size", 14);
            transcriptHeader.AddThemeColorOverride("font_color", new Color(0.8f, 0.8f, 0.8f));
            vbox.AddChild(transcriptHeader);

            var scrollContainer = new ScrollContainer();
            scrollContainer.SizeFlagsVertical = SizeFlags.ExpandFill;
            vbox.AddChild(scrollContainer);

            _testimonyList = new VBoxContainer();
            _testimonyList.AddThemeConstantOverride("separation", 5);
            scrollContainer.AddChild(_testimonyList);

            var separator3 = new HSeparator();
            vbox.AddChild(separator3);

            var contradictionsHeader = new Label();
            contradictionsHeader.Text = "CONTRADICTIONS";
            contradictionsHeader.AddThemeFontSizeOverride("font_size", 16);
            contradictionsHeader.AddThemeColorOverride("font_color", new Color(0.8f, 0.8f, 0.8f));
            vbox.AddChild(contradictionsHeader);

            _contradictionsList = new VBoxContainer();
            _contradictionsList.AddThemeConstantOverride("separation", 5);
            vbox.AddChild(_contradictionsList);

            var separator4 = new HSeparator();
            vbox.AddChild(separator4);

            _checkContradictionButton = new Button();
            _checkContradictionButton.Text = "Check for Contradiction";
            _checkContradictionButton.Disabled = true;
            _checkContradictionButton.Pressed += OnCheckContradictionPressed;
            vbox.AddChild(_checkContradictionButton);

            _feedbackLabel = new RichTextLabel();
            _feedbackLabel.BbcodeEnabled = true;
            _feedbackLabel.FitContent = true;
            _feedbackLabel.ScrollActive = false;
            vbox.AddChild(_feedbackLabel);

            return panel;
        }

        private void LoadRealData()
        {
            _currentDossier = _context.GetDossier("su_assassin_marko");

            if (_currentDossier == null)
            {
                GD.PrintErr("Failed to load assassin dossier!");
                return;
            }

            _availableEvidence = _currentDossier.LinkedEvidenceIds
                .Select(id => _context.GetEvidence(id))
                .Where(e => e != null)
                .ToList();

            _interrogationState = new InterrogationState
            {
                SuspectId = _currentDossier.SuspectId,
                AvailableTestimonyIds = new List<string>(_currentDossier.TestimonyIds),
                CurrentTestimonyIndex = 0
            };

            GD.Print($"Loaded dossier for {_currentDossier.FullDisplayName}");
            GD.Print($"  - {_currentDossier.TestimonyIds.Count} testimony statements");
            GD.Print($"  - {_availableEvidence.Count} evidence items");

            PopulateUI();
        }

        private void PopulateUI()
        {
            _headerLabel.Text = $"Investigation: {_currentDossier.FullDisplayName}";

            foreach (var evidence in _availableEvidence)
            {
                var button = new Button();
                button.Text = $"□ {evidence.Title}";
                button.ToggleMode = false;
                button.Alignment = HorizontalAlignment.Left;

                button.AddThemeColorOverride("font_color", new Color(0.9f, 0.9f, 0.9f));
                button.AddThemeColorOverride("font_hover_color", new Color(0.9f, 0.9f, 0.9f));
                button.AddThemeColorOverride("font_pressed_color", new Color(0.9f, 0.9f, 0.9f));
                button.AddThemeColorOverride("font_focus_color", new Color(0.9f, 0.9f, 0.9f));

                var evidenceRef = evidence;
                button.Pressed += () => OnEvidenceSelected(evidenceRef);

                _evidenceList.AddChild(button);
            }

            var emptyLabel = new Label();
            emptyLabel.Text = "Transcript is empty. Begin interrogation to see statements.";
            emptyLabel.AddThemeColorOverride("font_color", new Color(0.6f, 0.6f, 0.6f));
            _testimonyList.AddChild(emptyLabel);

            _currentStatementDisplay.Text =
                "[color=#cccccc][i]Press 'Continue' to begin the interrogation...[/i][/color]";

            UpdateContradictionsList();
        }

        private void OnEvidenceSelected(Evidence evidence)
        {
            _selectedEvidence = evidence;

            foreach (Button button in _evidenceList.GetChildren())
            {
                var cleanTitle = button.Text.Replace("□ ", "").Replace("▶ ", "");
                bool isThisButton = cleanTitle == evidence.Title;

                if (isThisButton)
                {
                    button.AddThemeColorOverride("font_color", new Color(0.9f, 0.3f, 0.3f));
                    button.AddThemeColorOverride("font_hover_color", new Color(0.9f, 0.3f, 0.3f));
                    button.AddThemeColorOverride("font_pressed_color", new Color(0.9f, 0.3f, 0.3f));
                    button.AddThemeColorOverride("font_focus_color", new Color(0.9f, 0.3f, 0.3f));
                    button.Text = $"▶ {cleanTitle}";
                }
                else
                {
                    button.AddThemeColorOverride("font_color", new Color(0.9f, 0.9f, 0.9f));
                    button.AddThemeColorOverride("font_hover_color", new Color(0.9f, 0.9f, 0.9f));
                    button.AddThemeColorOverride("font_pressed_color", new Color(0.9f, 0.9f, 0.9f));
                    button.AddThemeColorOverride("font_focus_color", new Color(0.9f, 0.9f, 0.9f));
                    button.Text = $"□ {cleanTitle}";
                }
            }

            UpdateCheckButtonState();
            _feedbackLabel.Text = $"[color=gray]Selected: {evidence.Title}[/color]";
        }

        private void OnTestimonySelected(TestimonyStatement testimony)
        {
            _selectedTestimony = testimony;

            foreach (Button button in _testimonyList.GetChildren())
            {
                var testimonyPreview = testimony.CurrentText.Substring(0, Math.Min(30, testimony.CurrentText.Length));
                bool isThisButton = button.Text.Contains(testimonyPreview);

                if (isThisButton)
                {
                    button.AddThemeColorOverride("font_color", new Color(0.9f, 0.3f, 0.3f));
                    button.AddThemeColorOverride("font_hover_color", new Color(0.9f, 0.3f, 0.3f));
                    button.AddThemeColorOverride("font_pressed_color", new Color(0.9f, 0.3f, 0.3f));
                    button.AddThemeColorOverride("font_focus_color", new Color(0.9f, 0.3f, 0.3f));
                }
                else
                {
                    button.AddThemeColorOverride("font_color", new Color(0.9f, 0.9f, 0.9f));
                    button.AddThemeColorOverride("font_hover_color", new Color(0.9f, 0.9f, 0.9f));
                    button.AddThemeColorOverride("font_pressed_color", new Color(0.9f, 0.9f, 0.9f));
                    button.AddThemeColorOverride("font_focus_color", new Color(0.9f, 0.9f, 0.9f));
                }
            }

            UpdateCheckButtonState();
            var preview = testimony.CurrentText.Substring(0, Math.Min(60, testimony.CurrentText.Length));
            _feedbackLabel.Text = $"[color=gray]Selected testimony: \"{preview}...\"[/color]";
        }

        private void UpdateCheckButtonState()
        {
            _checkContradictionButton.Disabled = _selectedEvidence == null || _selectedTestimony == null;
        }

        private async void OnCheckContradictionPressed()
        {
            if (_selectedEvidence == null || _selectedTestimony == null)
            {
                return;
            }

            _feedbackLabel.Text = "[color=yellow]Checking for contradictions...[/color]";
            _checkContradictionButton.Disabled = true;

            try
            {
                var contradictionTypes = new[]
                {
                    ContradictionType.Timeline,
                    ContradictionType.Location,
                    ContradictionType.Identity
                };

                ContradictionResult foundContradiction = null;

                foreach (var type in contradictionTypes)
                {
                    var query = _queryFactory.CreateTestimonyVsEvidence(
                        _selectedTestimony.Id,
                        _selectedEvidence.Id,
                        type);

                    var result = await _contradictionService.CheckContradictionAsync(query);

                    if (result.IsContradiction)
                    {
                        foundContradiction = result;
                        break;
                    }
                }

                if (foundContradiction != null)
                {
                    DisplayContradictionFound(foundContradiction);
                }
                else
                {
                    DisplayNoContradiction();
                }
            }
            catch (Exception ex)
            {
                GD.PrintErr($"Error checking contradiction: {ex.Message}");
                _feedbackLabel.Text = $"[color=red]Error: {ex.Message}[/color]";
            }

            ClearSelections();
        }

        private void DisplayContradictionFound(ContradictionResult contradiction)
        {
            _feedbackLabel.Text =
                $"[bgcolor=#8B0000][color=#ffff00][center] ⚠️  C O N T R A D I C T I O N  ⚠️ [/center][/color][/bgcolor]\n\n" +
                $"[color=#ff6b6b][b]TYPE:[/b] {contradiction.Type}[/color]\n" +
                $"[color=#cccccc]{contradiction.Description}[/color]";

            var contradictionLabel = new RichTextLabel();
            contradictionLabel.BbcodeEnabled = true;
            contradictionLabel.FitContent = true;
            contradictionLabel.ScrollActive = false;
            contradictionLabel.Text =
                $"[bgcolor=#4a0000][color=#ff6b6b] ⚠️ CONTRADICTION [/color][/bgcolor]\n" +
                $"[color=#ff9999][b]{contradiction.Type}:[/b][/color] {contradiction.Description}";
            contradictionLabel.Modulate = new Color(1, 1, 1, 0);
            _contradictionsList.AddChild(contradictionLabel);

            var tween = CreateTween();
            tween.SetParallel(true);
            tween.TweenProperty(contradictionLabel, "modulate:a", 1.0f, 0.4f)
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Cubic);

            contradictionLabel.Scale = new Vector2(0.9f, 0.9f);
            tween.TweenProperty(contradictionLabel, "scale", Vector2.One, 0.4f)
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Back);

            _currentDossier.Contradictions.Add(contradiction);

            GD.Print($"✓ Contradiction detected: {contradiction.ContradictionId}");
            GD.Print($"  Type: {contradiction.Type}");
            GD.Print($"  Description: {contradiction.Description}");
        }

        private void DisplayNoContradiction()
        {
            _feedbackLabel.Text =
                $"[color=#888888][i]No contradiction found.[/i][/color]\n" +
                $"[color=#666666]These items appear consistent.[/color]";
        }

        private void ClearSelections()
        {
            _selectedEvidence = null;
            _selectedTestimony = null;

            foreach (Button button in _evidenceList.GetChildren())
            {
                button.ButtonPressed = false;
                button.AddThemeColorOverride("font_color", new Color(0.9f, 0.9f, 0.9f));
                var title = button.Text.Replace("□ ", "").Replace("▶ ", "");
                button.Text = $"□ {title}";
            }

            foreach (Button button in _testimonyList.GetChildren())
            {
                button.ButtonPressed = false;
                button.AddThemeColorOverride("font_color", new Color(0.9f, 0.9f, 0.9f));
            }

            UpdateCheckButtonState();
        }

        private void UpdateContradictionsList()
        {
            foreach (var child in _contradictionsList.GetChildren())
            {
                child.QueueFree();
            }

            if (_currentDossier.Contradictions.Count == 0)
            {
                var label = new Label();
                label.Text = "No contradictions detected yet.";
                label.AddThemeColorOverride("font_color", new Color(0.6f, 0.6f, 0.6f));
                _contradictionsList.AddChild(label);
                return;
            }

            foreach (var contradiction in _currentDossier.Contradictions)
            {
                var richLabel = new RichTextLabel();
                richLabel.BbcodeEnabled = true;
                richLabel.FitContent = true;
                richLabel.ScrollActive = false;

                if (contradiction.Resolution.HasAnyResolution)
                {
                    richLabel.Text =
                        $"[bgcolor=#004a00][color=#90ee90] ✓ RESOLVED [/color][/bgcolor]\n" +
                        $"[color=#90ee90]{contradiction.Type}:[/color] [color=#cccccc]{contradiction.Description}[/color]";
                }
                else
                {
                    richLabel.Text =
                        $"[bgcolor=#4a0000][color=#ff6b6b] ⚠ ACTIVE [/color][/bgcolor]\n" +
                        $"[color=#ff9999]{contradiction.Type}:[/color] [color=#cccccc]{contradiction.Description}";
                }

                _contradictionsList.AddChild(richLabel);
            }
        }

        private void OnContinuePressed()
        {
            if (_interrogationState == null || !_interrogationState.HasMoreTestimony)
            {
                DisplayInterrogationComplete();
                return;
            }

            var currentTestimonyId = _interrogationState.GetCurrentTestimonyId();
            if (currentTestimonyId == null)
            {
                return;
            }

            var testimony = _context.GetTestimony(currentTestimonyId);
            if (testimony == null)
            {
                GD.PrintErr($"Failed to load testimony: {currentTestimonyId}");
                _interrogationState.AdvanceToNextTestimony();
                return;
            }

            DisplayCurrentStatement(testimony);

            AddToTranscript(testimony);

            _interrogationState.AdvanceToNextTestimony();

            if (!_interrogationState.HasMoreTestimony)
            {
                _continueButton.Text = "End Interrogation";
            }
        }

        private void DisplayCurrentStatement(TestimonyStatement testimony)
        {
            _currentStatementDisplay.Text =
                $"[color=#ffcc99][b]Suspect:[/b][/color] [color=#cccccc]\"{testimony.CurrentText}\"[/color]";

            GD.Print($"[Interrogation] Revealed: {testimony.Id}");
        }

        private void AddToTranscript(TestimonyStatement testimony)
        {
            if (_testimonyList.GetChildCount() > 0)
            {
                var firstChild = _testimonyList.GetChild(0);
                if (firstChild is Label)
                {
                    firstChild.QueueFree();
                }
            }

            var button = new Button();
            button.Text = $"• {testimony.CurrentText}";
            button.ToggleMode = false;
            button.Alignment = HorizontalAlignment.Left;

            button.AddThemeColorOverride("font_color", new Color(0.9f, 0.9f, 0.9f));
            button.AddThemeColorOverride("font_hover_color", new Color(0.9f, 0.9f, 0.9f));
            button.AddThemeColorOverride("font_pressed_color", new Color(0.9f, 0.9f, 0.9f));
            button.AddThemeColorOverride("font_focus_color", new Color(0.9f, 0.9f, 0.9f));

            var testimonyRef = testimony;
            button.Pressed += () => OnTestimonySelected(testimonyRef);

            _testimonyList.AddChild(button);
        }

        private void DisplayInterrogationComplete()
        {
            _currentStatementDisplay.Text =
                "[color=#90ee90][b]Interrogation Complete[/b][/color]\n" +
                "[color=#cccccc][i]You may review the transcript and check for contradictions.[/i][/color]";

            _continueButton.Disabled = true;

            GD.Print("[Interrogation] Complete - all testimony revealed");
        }
    }
}