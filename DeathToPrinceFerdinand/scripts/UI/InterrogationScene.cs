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

        private DossierState _currentDossier;
        private List<Evidence> _availableEvidence = new();
        private Evidence _selectedEvidence;
        private TestimonyStatement _selectedTestimony;

        private IContradictionService _contradictionService;
        private IContradictionQueryFactory _queryFactory;
        private IInvestigationContext _context;

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
            mainContainer.AddThemeConstantOverride("margin_left", 20);
            mainContainer.AddThemeConstantOverride("margin_top", 20);
            mainContainer.AddThemeConstantOverride("margin_right", 20);
            mainContainer.AddThemeConstantOverride("margin_bottom", 20);
            AddChild(mainContainer);

            var vbox = new VBoxContainer();
            vbox.AddThemeConstantOverride("separation", 10);
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
            vbox.AddChild(hbox);

            var leftPanel = CreateEvidencePanel();
            leftPanel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            leftPanel.CustomMinimumSize = new Vector2(300, 0);
            hbox.AddChild(leftPanel);

            var rightPanel = CreateTestimonyPanel();
            rightPanel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            rightPanel.CustomMinimumSize = new Vector2(400, 0);
            hbox.AddChild(rightPanel);
        }

        private PanelContainer CreateEvidencePanel()
        {
            var panel = new PanelContainer();

            var vbox = new VBoxContainer();
            vbox.AddThemeConstantOverride("separation", 8);
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

            var vbox = new VBoxContainer();
            vbox.AddThemeConstantOverride("separation", 8);
            panel.AddChild(vbox);

            var infoLabel = new Label();
            infoLabel.Text = "SUSPECT DOSSIER";
            infoLabel.AddThemeFontSizeOverride("font_size", 18);
            infoLabel.AddThemeColorOverride("font_color", new Color(0.8f, 0.8f, 0.8f));
            vbox.AddChild(infoLabel);

            var separator1 = new HSeparator();
            vbox.AddChild(separator1);

            var testimonyHeader = new Label();
            testimonyHeader.Text = "TESTIMONY";
            testimonyHeader.AddThemeFontSizeOverride("font_size", 16);
            testimonyHeader.AddThemeColorOverride("font_color", new Color(0.8f, 0.8f, 0.8f));
            vbox.AddChild(testimonyHeader);

            var scrollContainer = new ScrollContainer();
            scrollContainer.SizeFlagsVertical = SizeFlags.ExpandFill;
            scrollContainer.CustomMinimumSize = new Vector2(0, 200);
            vbox.AddChild(scrollContainer);

            _testimonyList = new VBoxContainer();
            _testimonyList.AddThemeConstantOverride("separation", 5);
            scrollContainer.AddChild(_testimonyList);

            var separator2 = new HSeparator();
            vbox.AddChild(separator2);

            var contradictionsHeader = new Label();
            contradictionsHeader.Text = "CONTRADICTIONS";
            contradictionsHeader.AddThemeFontSizeOverride("font_size", 16);
            contradictionsHeader.AddThemeColorOverride("font_color", new Color(0.8f, 0.8f, 0.8f));
            vbox.AddChild(contradictionsHeader);

            _contradictionsList = new VBoxContainer();
            _contradictionsList.AddThemeConstantOverride("separation", 5);
            vbox.AddChild(_contradictionsList);

            var separator3 = new HSeparator();
            vbox.AddChild(separator3);

            _checkContradictionButton = new Button();
            _checkContradictionButton.Text = "Check for Contradiction";
            _checkContradictionButton.Disabled = true;
            _checkContradictionButton.Pressed += OnCheckContradictionPressed;
            vbox.AddChild(_checkContradictionButton);

            _feedbackLabel = new RichTextLabel();
            _feedbackLabel.BbcodeEnabled = true;
            _feedbackLabel.FitContent = true;
            _feedbackLabel.ScrollActive = false;
            _feedbackLabel.CustomMinimumSize = new Vector2(0, 60);
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

            GD.Print($"Loaded dossier for {_currentDossier.FullDisplayName}");
            GD.Print($"  - {_currentDossier.Testimony.Count} testimony statements");
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
                button.ToggleMode = true;
                button.Alignment = HorizontalAlignment.Left;

                var evidenceRef = evidence;
                button.Toggled += (pressed) => OnEvidenceSelected(evidenceRef, pressed);

                _evidenceList.AddChild(button);
            }

            foreach (var testimony in _currentDossier.Testimony)
            {
                var button = new Button();
                button.Text = $"• {testimony.CurrentText}";
                button.ToggleMode = true;
                button.Alignment = HorizontalAlignment.Left;
                button.CustomMinimumSize = new Vector2(0, 60);

                var testimonyRef = testimony;
                button.Toggled += (pressed) => OnTestimonySelected(testimonyRef, pressed);

                _testimonyList.AddChild(button);
            }

            UpdateContradictionsList();
        }

        private void OnEvidenceSelected(Evidence evidence, bool pressed)
        {
            foreach (Button button in _evidenceList.GetChildren())
            {
                if (button.ButtonPressed && button.Text != $"□ {evidence.Title}")
                {
                    button.ButtonPressed = false;
                }
            }

            _selectedEvidence = pressed ? evidence : null;
            UpdateCheckButtonState();

            if (pressed)
            {
                _feedbackLabel.Text = $"[color=gray]Selected: {evidence.Title}[/color]";
            }
        }

        private void OnTestimonySelected(TestimonyStatement testimony, bool pressed)
        {
            foreach (Button button in _testimonyList.GetChildren())
            {
                if (button.ButtonPressed && button.Text != $"• {testimony.CurrentText}")
                {
                    button.ButtonPressed = false;
                }
            }

            _selectedTestimony = pressed ? testimony : null;
            UpdateCheckButtonState();

            if (pressed)
            {
                _feedbackLabel.Text = $"[color=gray]Selected testimony: {testimony.CurrentText.Substring(0, Math.Min(50, testimony.CurrentText.Length))}...[/color]";
            }
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
            _feedbackLabel.Text = $"[color=red][b]⚠ CONTRADICTION DETECTED![/b][/color]\n" +
                $"[b]Type:[/b] {contradiction.Type}\n" +
                $"{contradiction.Description}";

            var contradictionLabel = new Label();
            contradictionLabel.Text = $"✓ [{contradiction.Type}] {contradiction.Description}";
            contradictionLabel.AddThemeColorOverride("font_color", new Color(1.0f, 0.6f, 0.6f));
            contradictionLabel.AutowrapMode = TextServer.AutowrapMode.Word;
            _contradictionsList.AddChild(contradictionLabel);

            _currentDossier.Contradictions.Add(contradiction);

            GD.Print($"Contradiction detected: {contradiction.ContradictionId}");
            GD.Print($"  Type: {contradiction.Type}");
            GD.Print($"  Description: {contradiction.Description}");
        }

        private void DisplayNoContradiction()
        {
            _feedbackLabel.Text = "[color=green]No contradiction found. These items appear consistent.[/color]";
        }

        private void ClearSelections()
        {
            _selectedEvidence = null;
            _selectedTestimony = null;

            foreach (Button button in _evidenceList.GetChildren())
            {
                button.ButtonPressed = false;
            }

            foreach (Button button in _testimonyList.GetChildren())
            {
                button.ButtonPressed = false;
            }

            UpdateCheckButtonState();
        }

        private void UpdateContradictionsList()
        {
            foreach (var child in _contradictionsList.GetChildren())
            {
                child.QueueFree();
            }

            foreach (var contradiction in _currentDossier.Contradictions)
            {
                var label = new Label();
                label.Text = $"• {contradiction.Type}: {contradiction.Description}";
                label.AddThemeColorOverride("font_color",
                    contradiction.Resolution.HasAnyResolution
                    ? new Color(0.6f, 1.0f, 0.6f)
                    : new Color(1.0f, 0.6f, 0.6f));
                _contradictionsList.AddChild(label);
            }

            if (_currentDossier.Contradictions.Count == 0)
            {
                var label = new Label();
                label.Text = "No contradictions detected yet.";
                label.AddThemeColorOverride("font_color", new Color(0.6f, 0.6f, 0.6f));
                _contradictionsList.AddChild(label);
            }
        }
    }
}